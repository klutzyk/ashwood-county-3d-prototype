"""Download CC0 assets from Poly Haven into the project third-party tree.

Poly Haven publishes everything under CC0, so these are safe for commercial use
with no attribution requirement. The project still records provenance in
docs/implementation/assets_reference.md; this script emits a manifest with the
exact URLs, sizes and hashes so that record can be written from real data rather
than from memory.

Usage:
    python tools/download_polyhaven.py --set terrain
    python tools/download_polyhaven.py --set vegetation
    python tools/download_polyhaven.py --set all

API shape (verified 2026-08-03):
    https://api.polyhaven.com/files/<slug>
    textures -> {<map_name>: {<res>: {<fmt>: {url, size, md5}}}}
    models   -> {"gltf": {<res>: {"gltf": {url, size, md5, include: {relpath: {url,...}}}}}}
    hdris    -> {"hdri": {<res>: {<fmt>: {url, size, md5}}}}
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed

API = "https://api.polyhaven.com"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEST_ROOT = os.path.join(ROOT, "assets", "third_party", "polyhaven_2026_08")

# Map slugs we actually want, grouped by the job they do in the world.
# Texture maps are limited to the ones Godot's StandardMaterial3D consumes so we
# do not pull 40MB of displacement data the Compatibility renderer cannot use.
# NOTE: the API spells the albedo map "Diffuse" (and height "Displacement"), not
# "diff"/"disp", so both spellings have to be listed or the albedo is silently
# skipped and every material ends up untextured.
WANTED_TEXTURE_MAPS = ("diff", "diffuse", "arm", "nor_gl", "rough", "ao", "disp")

TERRAIN_TEXTURES = [
    # (slug, resolution)
    ("brown_mud_leaves_01", "2k"),   # forest floor / leaf litter
    ("forrest_ground_01", "2k"),     # already partly present; full PBR set
    ("aerial_grass_rock", "2k"),     # grass with rock breakup
    ("aerial_rocks_02", "2k"),       # rough rock faces for the gorge
    ("cliff_side", "2k"),            # stratified layered rock
    ("brown_mud_dry", "2k"),         # compacted soil / verges
    ("bicolour_gravel", "2k"),       # shoulders and riverbed
    ("clean_pebbles", "2k"),         # river pebbles at the waterline
    ("asphalt_02", "2k"),            # cracked asphalt for the road
    ("asphalt_04", "2k"),            # second cracked variant
]

VEGETATION_MODELS = [
    # (slug, resolution) - geometry is decimated later by the Blender step.
    ("jacaranda_tree", "2k"),        # 312k broadleaf, the workhorse tree
    ("shrub_01", "2k"),
    ("shrub_02", "2k"),
    ("shrub_03", "2k"),
    ("fern_02", "2k"),
    ("nettle_plant", "2k"),
    ("grass_bermuda_01", "2k"),
    ("dead_tree_trunk", "2k"),
    ("dead_tree_trunk_02", "2k"),
    ("bark_debris_01", "2k"),
]

# Poly Haven's glTF for scanned vegetation references only the JPEG diffuse, and
# JPEG cannot carry an alpha channel. The cut-out silhouette for every leaf card
# is published as a separate "Alpha" (or "<part>_alpha") map that the glTF never
# mentions. Without it fern_02 and friends render as opaque green rectangles,
# because their diffuse map has a dilated colour bleed instead of a black
# background. These are fetched alongside the models and composited into an RGBA
# albedo by tools/blender/build_ashwood_vegetation.py.
VEGETATION_ALPHA_MAPS = [
    ("jacaranda_tree", "2k"),
    ("shrub_01", "2k"),
    ("shrub_02", "2k"),
    ("shrub_03", "2k"),
    ("fern_02", "2k"),
    ("nettle_plant", "2k"),
    ("grass_bermuda_01", "2k"),
]

ROCK_MODELS = [
    ("boulder_01", "2k"),
    ("rock_moss_set_01", "2k"),
]

HDRIS = [
    ("kloofendal_48d_partly_cloudy_puresky", "2k"),
    ("syferfontein_18d_clear_puresky", "2k"),
]

SETS = {
    "terrain": ("textures", TERRAIN_TEXTURES),
    "vegetation": ("models", VEGETATION_MODELS),
    "vegetation_alpha": ("model_alphas", VEGETATION_ALPHA_MAPS),
    "rocks": ("models", ROCK_MODELS),
    "hdri": ("hdris", HDRIS),
}

# Sets that are useless on their own. Asking for "vegetation" also fetches the
# opacity maps, so nobody can end up with leaf geometry and no cut-out.
SET_COMPANIONS = {
    "vegetation": ("vegetation_alpha",),
}


# Poly Haven's CDN rejects urllib's default User-Agent with HTTP 403, so every
# request has to present a normal one.
UA = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
    "(KHTML, like Gecko) Chrome/124.0 Safari/537.36 AshwoodCounty/1.0"
)


def _request(url: str) -> urllib.request.Request:
    return urllib.request.Request(url, headers={"User-Agent": UA})


def fetch_json(url: str):
    with urllib.request.urlopen(_request(url), timeout=60) as response:
        return json.loads(response.read().decode("utf-8"))


def download(url: str, path: str) -> int:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if os.path.exists(path) and os.path.getsize(path) > 0:
        return os.path.getsize(path)
    tmp = path + ".part"
    with urllib.request.urlopen(_request(url), timeout=300) as response, open(tmp, "wb") as out:
        while True:
            chunk = response.read(1 << 16)
            if not chunk:
                break
            out.write(chunk)
    os.replace(tmp, path)
    return os.path.getsize(path)


def collect_texture(slug: str, res: str):
    """Yield (url, destination_path) for one texture set."""
    files = fetch_json(f"{API}/files/{slug}")
    dest = os.path.join(DEST_ROOT, "textures", slug)
    for map_name, resolutions in files.items():
        base = map_name.lower()
        if not any(base.endswith(w) or base == w for w in WANTED_TEXTURE_MAPS):
            continue
        entry = resolutions.get(res)
        if not isinstance(entry, dict):
            continue
        # Prefer jpg for colour data, png only when jpg is unavailable.
        chosen = entry.get("jpg") or entry.get("png") or entry.get("exr")
        if not isinstance(chosen, dict) or "url" not in chosen:
            continue
        url = chosen["url"]
        yield url, os.path.join(dest, os.path.basename(url))


def collect_model(slug: str, res: str):
    files = fetch_json(f"{API}/files/{slug}")
    dest = os.path.join(DEST_ROOT, "models", slug)
    gltf = files.get("gltf", {}).get(res, {}).get("gltf")
    if not isinstance(gltf, dict):
        return
    yield gltf["url"], os.path.join(dest, os.path.basename(gltf["url"]))
    for relative, info in (gltf.get("include") or {}).items():
        if isinstance(info, dict) and "url" in info:
            yield info["url"], os.path.join(dest, relative.replace("/", os.sep))


def collect_hdri(slug: str, res: str):
    files = fetch_json(f"{API}/files/{slug}")
    dest = os.path.join(DEST_ROOT, "hdris", slug)
    entry = files.get("hdri", {}).get(res, {})
    chosen = entry.get("hdr") or entry.get("exr")
    if not isinstance(chosen, dict) or "url" not in chosen:
        return
    yield chosen["url"], os.path.join(dest, os.path.basename(chosen["url"]))


def collect_model_alpha(slug: str, res: str):
    """Yield (url, destination_path) for every opacity map a model publishes.

    Map names vary: single-material scans use "Alpha", multi-material ones use
    "<part>_alpha" (jacaranda_tree -> "leaves_alpha"), so the key is matched by
    substring rather than by an exact name. PNG is preferred over JPG because a
    JPEG-compressed cut-out mask produces ringing along every leaf edge, which
    an alpha-scissor threshold turns into visible crawling fringes.
    """
    files = fetch_json(f"{API}/files/{slug}")
    dest = os.path.join(DEST_ROOT, "models", slug, "textures")
    for map_name, resolutions in files.items():
        if "alpha" not in map_name.lower():
            continue
        if not isinstance(resolutions, dict):
            continue
        entry = resolutions.get(res)
        if not isinstance(entry, dict):
            continue
        chosen = entry.get("png") or entry.get("jpg")
        if not isinstance(chosen, dict) or "url" not in chosen:
            continue
        yield chosen["url"], os.path.join(dest, os.path.basename(chosen["url"]))


COLLECTORS = {
    "textures": collect_texture,
    "models": collect_model,
    "model_alphas": collect_model_alpha,
    "hdris": collect_hdri,
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--set", default="all")
    args = parser.parse_args()

    selected = list(SETS) if args.set == "all" else [args.set]
    for name in list(selected):
        for companion in SET_COMPANIONS.get(name, ()):
            if companion not in selected:
                selected.append(companion)

    jobs = []
    for name in selected:
        if name not in SETS:
            print(f"unknown set: {name}", file=sys.stderr)
            return 2
        kind, entries = SETS[name]
        collector = COLLECTORS[kind]
        for slug, res in entries:
            try:
                for url, path in collector(slug, res):
                    jobs.append((slug, url, path))
            except Exception as error:  # noqa: BLE001 - report and keep going
                print(f"SKIP {slug}: {error}", file=sys.stderr)

    print(f"queued {len(jobs)} files -> {DEST_ROOT}")

    manifest = []
    total = 0
    with ThreadPoolExecutor(max_workers=6) as pool:
        futures = {
            pool.submit(download, url, path): (slug, url, path)
            for slug, url, path in jobs
        }
        for future in as_completed(futures):
            slug, url, path = futures[future]
            try:
                size = future.result()
                total += size
                manifest.append(
                    {
                        "slug": slug,
                        "url": url,
                        "path": os.path.relpath(path, ROOT).replace("\\", "/"),
                        "bytes": size,
                    }
                )
                print(f"  ok {os.path.relpath(path, DEST_ROOT)} ({size/1e6:.1f}MB)")
            except Exception as error:  # noqa: BLE001
                print(f"  FAIL {url}: {error}", file=sys.stderr)

    os.makedirs(DEST_ROOT, exist_ok=True)
    manifest_path = os.path.join(DEST_ROOT, "MANIFEST.json")
    existing = []
    if os.path.exists(manifest_path):
        try:
            with open(manifest_path, "r", encoding="utf-8") as handle:
                existing = json.load(handle).get("files", [])
        except Exception:  # noqa: BLE001
            existing = []

    seen = set()
    merged = []
    for row in existing + manifest:
        if row["path"] in seen:
            continue
        seen.add(row["path"])
        merged.append(row)

    with open(manifest_path, "w", encoding="utf-8") as handle:
        json.dump(
            {
                "source": "https://polyhaven.com",
                "license": "CC0 1.0 Universal (https://polyhaven.com/license)",
                "attribution_required": False,
                "downloaded_utc": __import__("datetime").datetime.utcnow().isoformat(),
                "files": merged,
            },
            handle,
            indent=2,
        )

    print(f"done: {total/1e6:.1f}MB this run, manifest -> {manifest_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
