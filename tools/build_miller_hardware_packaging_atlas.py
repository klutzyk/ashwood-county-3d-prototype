"""Generate the compact, project-authored Miller Hardware packaging atlas.

The late-1970s/1980s label designs are deterministic and use only locally
available system fonts.  Run with the project's regular Python interpreter:

    python tools/build_miller_hardware_packaging_atlas.py
"""

from __future__ import annotations

import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


REPO_ROOT = Path(__file__).resolve().parents[1]
OUTPUT = (
    REPO_ROOT
    / "assets"
    / "environment"
    / "buildings"
    / "MillerHardware"
    / "fixtures"
    / "miller_packaging_labels_atlas.png"
)

ATLAS_SIZE = (1024, 512)
TILE_SIZE = (256, 128)
TILES_X = 4

PRODUCTS = (
    ("HAND TOOLS", "FORGED STEEL", "TOOLS"),
    ("DRILL BITS", "13 PIECE SET", "BITS"),
    ("SAW BLADES", "CARBIDE EDGE", "BLADE"),
    ("UTILITY KNIVES", "HEAVY DUTY", "KNIFE"),
    ("WOOD SCREWS", "NO. 8 • 1 1/2 IN", "SCREW"),
    ("STEEL SCREWS", "NO. 10 • 2 IN", "SCREW"),
    ("COMMON NAILS", "8D • 1 LB NET", "NAIL"),
    ("BOLTS & NUTS", "ZINC PLATED", "BOLT"),
    ("WALL PAINT", "INTERIOR FLAT", "PAINT"),
    ("MOTOR OIL", "SAE 10W-30", "OIL"),
    ("PLUMBING KIT", "HOME REPAIR", "PIPE"),
    ("WORK GLOVES", "LEATHER PALM", "GLOVE"),
    ("HOUSE FUSES", "15 AMP • 4 PACK", "FUSE"),
    ("DOOR HINGES", "3 1/2 IN • PAIR", "HINGE"),
    ("UTILITY ROPE", "50 FT • 3/8 IN", "ROPE"),
    ("GARDEN SUPPLY", "ALL PURPOSE", "GARDEN"),
)

PALETTES = (
    ((226, 213, 174), (119, 39, 29), (31, 35, 31)),
    ((210, 216, 191), (30, 72, 57), (27, 31, 27)),
    ((221, 205, 150), (40, 68, 105), (31, 33, 31)),
    ((201, 210, 208), (139, 74, 25), (31, 32, 30)),
)


def load_font(size: int, *, condensed: bool = False) -> ImageFont.FreeTypeFont:
    candidates = (
        Path("C:/Windows/Fonts/bahnschrift.ttf"),
        Path("C:/Windows/Fonts/arialbd.ttf"),
        Path("C:/Windows/Fonts/arial.ttf"),
    )
    if condensed:
        candidates = (
            Path("C:/Windows/Fonts/arialnb.ttf"),
            Path("C:/Windows/Fonts/bahnschrift.ttf"),
            *candidates,
        )
    for path in candidates:
        if path.is_file():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def draw_icon(
    draw: ImageDraw.ImageDraw,
    icon: str,
    origin: tuple[int, int],
    ink: tuple[int, int, int],
) -> None:
    x, y = origin
    width = 4
    if icon in {"TOOLS", "BITS", "BLADE", "KNIFE"}:
        draw.line((x + 2, y + 24, x + 25, y + 1), fill=ink, width=width)
        draw.rounded_rectangle((x + 18, y, x + 31, y + 9), 2, outline=ink, width=3)
        draw.line((x + 1, y + 25, x + 8, y + 32), fill=ink, width=5)
    elif icon in {"SCREW", "NAIL", "BOLT", "HINGE", "FUSE"}:
        draw.line((x + 4, y + 28, x + 28, y + 4), fill=ink, width=4)
        draw.line((x + 1, y + 23, x + 9, y + 31), fill=ink, width=3)
        for offset in (10, 16, 22):
            draw.line(
                (x + offset - 2, y + 28 - offset, x + offset + 4, y + 34 - offset),
                fill=ink,
                width=2,
            )
    elif icon in {"PAINT", "OIL"}:
        draw.rounded_rectangle((x + 4, y + 7, x + 29, y + 31), 3, outline=ink, width=3)
        draw.arc((x + 8, y, x + 26, y + 16), 180, 360, fill=ink, width=3)
        draw.line((x + 6, y + 15, x + 27, y + 15), fill=ink, width=2)
    elif icon == "PIPE":
        draw.line((x + 4, y + 27, x + 4, y + 8, x + 25, y + 8), fill=ink, width=6)
        draw.arc((x + 18, y + 2, x + 32, y + 16), 270, 90, fill=ink, width=5)
    elif icon == "GLOVE":
        draw.polygon(
            ((x + 7, y + 30), (x + 3, y + 13), (x + 8, y + 11),
             (x + 12, y + 20), (x + 11, y + 5), (x + 16, y + 4),
             (x + 19, y + 18), (x + 21, y + 7), (x + 26, y + 8),
             (x + 28, y + 23), (x + 20, y + 32)),
            outline=ink,
        )
    elif icon == "ROPE":
        draw.ellipse((x + 3, y + 4, x + 30, y + 31), outline=ink, width=4)
        draw.ellipse((x + 9, y + 10, x + 24, y + 25), outline=ink, width=3)
        draw.line((x + 23, y + 26, x + 31, y + 33), fill=ink, width=3)
    else:
        draw.line((x + 16, y + 31, x + 16, y + 8), fill=ink, width=4)
        draw.line((x + 16, y + 12, x + 5, y + 3), fill=ink, width=3)
        draw.line((x + 16, y + 14, x + 29, y + 6), fill=ink, width=3)
        draw.line((x + 16, y + 20, x + 7, y + 14), fill=ink, width=3)


def draw_barcode(
    draw: ImageDraw.ImageDraw,
    origin: tuple[int, int],
    seed: int,
    ink: tuple[int, int, int],
) -> None:
    rng = random.Random(seed)
    x, y = origin
    for _ in range(18):
        stripe = rng.choice((1, 1, 2, 3))
        draw.rectangle((x, y, x + stripe - 1, y + 18), fill=ink)
        x += stripe + rng.choice((1, 2))


def main() -> None:
    rng = random.Random(1979)
    atlas = Image.new("RGB", ATLAS_SIZE, (214, 203, 171))
    draw = ImageDraw.Draw(atlas)
    brand_font = load_font(15, condensed=True)
    product_font = load_font(22, condensed=True)
    detail_font = load_font(11)
    tiny_font = load_font(8)

    for index, (product, detail, icon) in enumerate(PRODUCTS):
        column = index % TILES_X
        row = index // TILES_X
        left = column * TILE_SIZE[0]
        top = row * TILE_SIZE[1]
        right = left + TILE_SIZE[0] - 1
        bottom = top + TILE_SIZE[1] - 1
        paper, accent, ink = PALETTES[index % len(PALETTES)]

        draw.rectangle((left, top, right, bottom), fill=paper)
        draw.rectangle((left + 4, top + 4, right - 4, bottom - 4), outline=ink, width=2)
        draw.rectangle((left + 8, top + 8, right - 8, bottom - 8), outline=accent, width=2)
        draw.rectangle((left + 10, top + 10, right - 10, top + 32), fill=accent)
        draw.text((left + 17, top + 12), "MILLER'S", fill=paper, font=brand_font)
        draw.text((left + 53, top + 44), product, fill=ink, font=product_font)
        draw.text((left + 54, top + 72), detail, fill=accent, font=detail_font)
        draw.text((left + 54, top + 91), "ASHWOOD COUNTY • GUARANTEED", fill=ink, font=tiny_font)
        draw_icon(draw, icon, (left + 16, top + 48), accent)
        draw_barcode(draw, (right - 52, top + 96), index + 31, ink)
        draw.text((left + 14, bottom - 17), f"NO. {4070 + index * 13}", fill=ink, font=tiny_font)

        for _ in range(150):
            x = rng.randint(left + 5, right - 5)
            y = rng.randint(top + 5, bottom - 5)
            if rng.random() < 0.72:
                colour = tuple(max(channel - rng.randint(8, 26), 0) for channel in paper)
            else:
                colour = tuple(min(channel + rng.randint(4, 16), 255) for channel in paper)
            draw.point((x, y), fill=colour)
        for _ in range(3):
            x = rng.randint(left + 12, right - 28)
            y = rng.randint(top + 36, bottom - 12)
            draw.line((x, y, x + rng.randint(8, 24), y + rng.randint(-2, 2)),
                      fill=tuple(max(channel - 18, 0) for channel in paper), width=1)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(OUTPUT, optimize=True)
    print(f"MILLER_PACKAGING_ATLAS output={OUTPUT} bytes={OUTPUT.stat().st_size}")


if __name__ == "__main__":
    main()
