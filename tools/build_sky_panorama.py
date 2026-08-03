"""Build the Ashwood late-afternoon panorama sky from CC0 Poly Haven sources.

Base       : syferfontein_18d_clear_puresky (sun elevation 18.5 deg - matches the
             game's 18.75 deg sun at 16:45)
Clouds     : kloofendal_48d_partly_cloudy_puresky (azimuth within 2 deg of the
             base, so cumulus highlight sides already face the same way)
Output     : assets/environment/sky/ashwood_late_afternoon_sky.hdr
"""
import numpy as np
import os
from PIL import Image

SRC = r"c:\ashwood-county-3d-prototype\assets\third_party\polyhaven_2026_08\hdris"
BASE = os.path.join(SRC, "syferfontein_18d_clear_puresky",
                    "syferfontein_18d_clear_puresky_2k.hdr")
CLOUD = os.path.join(SRC, "kloofendal_48d_partly_cloudy_puresky",
                     "kloofendal_48d_partly_cloudy_puresky_2k.hdr")
OUT_DIR = r"c:\ashwood-county-3d-prototype\assets\environment\sky"
OUT = os.path.join(OUT_DIR, "ashwood_late_afternoon_sky.hdr")
PREVIEW_DIR = (r"C:\Users\kalz9\AppData\Local\Temp\claude"
               r"\c--ashwood-county-3d-prototype"
               r"\69b8f198-324d-47b0-8967-f3a3644e3081\scratchpad")

# Godot's texturePanorama(): u = atan2(x, -z) / TAU, v = acos(y) / PI.
# The game's directional light has a fixed -28 deg yaw, so the sun's azimuth in
# Godot panorama space is a constant 28.0 deg.
GAME_SUN_AZIMUTH_DEG = 28.0
GAME_SUN_ELEVATION_DEG = 18.75


def read_hdr(path):
    with open(path, "rb") as handle:
        data = handle.read()
    pos = data.index(b"\n\n") + 2
    eol = data.index(b"\n", pos)
    res = data[pos:eol].decode().strip().split()
    pos = eol + 1
    height, width = int(res[1]), int(res[3])
    out = np.zeros((height, width, 4), dtype=np.uint8)
    p = pos
    for y in range(height):
        assert data[p] == 2 and data[p + 1] == 2, "expected RLE scanlines"
        p += 4
        for c in range(4):
            x = 0
            while x < width:
                n = data[p]
                p += 1
                if n > 128:
                    out[y, x:x + (n - 128), c] = data[p]
                    p += 1
                    x += n - 128
                else:
                    out[y, x:x + n, c] = np.frombuffer(
                        data, dtype=np.uint8, count=n, offset=p)
                    p += n
                    x += n
    rgbe = out.astype(np.float32)
    scale = np.where(rgbe[..., 3] > 0, np.exp2(rgbe[..., 3] - 136.0), 0.0)
    return rgbe[..., :3] * scale[..., None]


def write_hdr(path, rgb):
    """Radiance RGBE, flat (uncompressed) scanlines."""
    rgb = np.maximum(rgb, 0.0).astype(np.float32)
    height, width = rgb.shape[:2]
    peak = rgb.max(axis=2)
    exponent = np.zeros_like(peak, dtype=np.int32)
    nonzero = peak > 1e-32
    exponent[nonzero] = np.floor(np.log2(peak[nonzero])).astype(np.int32) + 1
    mantissa_scale = np.where(nonzero, 256.0 / np.exp2(exponent), 0.0)
    out = np.zeros((height, width, 4), dtype=np.uint8)
    out[..., :3] = np.clip(rgb * mantissa_scale[..., None], 0, 255).astype(np.uint8)
    out[..., 3] = np.where(nonzero, np.clip(exponent + 128, 0, 255), 0).astype(np.uint8)
    header = ("#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n"
              "-Y %d +X %d\n" % (height, width)).encode("ascii")
    with open(path, "wb") as handle:
        handle.write(header)
        handle.write(out.tobytes())


def luminance(rgb):
    return rgb[..., 0] * 0.2126 + rgb[..., 1] * 0.7152 + rgb[..., 2] * 0.0722


def direction_grid(width, height):
    """Per-pixel world direction under Godot's panorama mapping."""
    u = (np.arange(width, dtype=np.float32) + 0.5) / width
    v = (np.arange(height, dtype=np.float32) + 0.5) / height
    phi = u * 2.0 * np.pi
    theta = v * np.pi
    sin_t = np.sin(theta)[:, None]
    x = sin_t * np.sin(phi)[None, :]
    y = np.cos(theta)[:, None] * np.ones((1, width), dtype=np.float32)
    z = -sin_t * np.cos(phi)[None, :]
    return np.stack([x, y, z], axis=-1)


def sun_pixel(rgb):
    lum = luminance(rgb)
    y, x = np.unravel_index(np.argmax(lum), lum.shape)
    return x, y


def roll_to_azimuth(rgb, target_deg):
    height, width = rgb.shape[:2]
    x, _ = sun_pixel(rgb)
    target_x = (target_deg / 360.0) * width
    return np.roll(rgb, int(round(target_x - x)), axis=1)


def save_preview(rgb, name):
    tone = np.clip(rgb / (rgb + 1.0), 0, 1) ** (1 / 2.2)
    Image.fromarray((tone * 255).astype(np.uint8)).resize((1024, 512)).save(
        os.path.join(PREVIEW_DIR, name))


base = roll_to_azimuth(read_hdr(BASE), GAME_SUN_AZIMUTH_DEG)
clouds_src = roll_to_azimuth(read_hdr(CLOUD), GAME_SUN_AZIMUTH_DEG)
height, width = base.shape[:2]
direction = direction_grid(width, height)
elevation = np.degrees(np.arcsin(np.clip(direction[..., 1], -1, 1)))

sun_dir = np.array([
    np.cos(np.radians(GAME_SUN_ELEVATION_DEG)) * np.sin(np.radians(GAME_SUN_AZIMUTH_DEG)),
    np.sin(np.radians(GAME_SUN_ELEVATION_DEG)),
    -np.cos(np.radians(GAME_SUN_ELEVATION_DEG)) * np.cos(np.radians(GAME_SUN_AZIMUTH_DEG)),
], dtype=np.float32)
sun_cos = np.clip((direction * sun_dir).sum(axis=-1), -1.0, 1.0)
sun_angle = np.degrees(np.arccos(sun_cos))

# ---------------------------------------------------------------- 1. sun disc
# Compress the 232k-peak solar disc. The scene has a real DirectionalLight3D for
# the sun, so the panorama only needs a believable, bloom-able disc rather than
# a physically absolute one that aliases and blows out the tonemapper.
base_lum = luminance(base)
sun_core = sun_angle < 3.0
compressed = base.copy()
compressed[sun_core] = base[sun_core] * (
    (420.0 / np.maximum(base_lum[sun_core], 1.0))[:, None] ** 0.55)
base = compressed

# ------------------------------------------------------------ 2. cloud layer
cloud_lum = luminance(clouds_src)
row_floor = np.percentile(cloud_lum, 12, axis=1)[:, None]
cloud_mask = np.clip((cloud_lum - row_floor) / np.maximum(row_floor * 1.15, 1e-3), 0, 1)
red, green, blue = clouds_src[..., 0], clouds_src[..., 1], clouds_src[..., 2]
blueness = np.clip((blue - red) / np.maximum(blue + red, 1e-4), 0, 1)
cloud_mask *= np.clip(1.0 - blueness * 2.4, 0, 1)
# Only well above the horizon - the source's low-elevation haze is not cloud
# structure and reads as a smeared ring if it is composited in.
cloud_mask *= np.clip((elevation - 7.0) / 11.0, 0, 1)
cloud_mask *= np.clip((elevation - 88.0) / -3.0, 0, 1)
cloud_mask *= np.clip((sun_angle - 4.0) / 4.0, 0, 1)
cloud_mask = np.clip(cloud_mask * 1.3, 0, 1) ** 1.1

# Sunlit face warm, shadowed face cool - the separation that makes a sky read
# as photographed rather than painted.
warm = np.clip(1.0 - sun_angle / 105.0, 0, 1) ** 1.6
lit = np.array([1.00, 0.845, 0.660], dtype=np.float32)
shade = np.array([0.560, 0.615, 0.720], dtype=np.float32)
cloud_tint = shade + (lit - shade) * warm[..., None]
cloud_shape = np.clip(cloud_lum / np.maximum(row_floor * 3.4, 1e-3), 0.25, 1.9)
sky_reference = np.percentile(base_lum[(elevation > 12) & (elevation < 60)], 55)
cloud_rgb = cloud_tint * (cloud_shape * (sky_reference * 2.9))[..., None]

sky = base * (1.0 - cloud_mask[..., None]) + cloud_rgb * cloud_mask[..., None]

# ---------------------------------------------------- 3. golden-hour grading
# Warm gain concentrated near the horizon and toward the sun; the zenith keeps
# its cool blue so the scene gets warm key light and cool skylight fill.
horizon_band = np.clip(1.0 - np.abs(elevation) / 26.0, 0, 1) ** 1.35
toward_sun = np.clip(1.0 - sun_angle / 130.0, 0, 1) ** 1.8
warmth = np.clip(horizon_band * 0.85 + toward_sun * 0.80, 0, 1)
amber = np.array([1.46, 0.96, 0.55], dtype=np.float32)
neutral = np.array([1.0, 1.0, 1.0], dtype=np.float32)
sky *= (neutral + (amber - neutral) * warmth[..., None])
# Deepen and cool the zenith. A late-afternoon sky darkens noticeably overhead;
# holding it at midday brightness is what makes a sky read as generic.
zenith = np.clip((elevation - 18.0) / 62.0, 0, 1) ** 0.85
sky *= (neutral + (np.array([0.50, 0.56, 0.74], dtype=np.float32) - neutral)
        * zenith[..., None])

# ------------------------------------------------------ 4. ground hemisphere
# "puresky" HDRIs mirror the sky below the horizon. Left alone that bright
# mirror uplights everything from underneath. Replace it with a dark county
# ground bounce that still carries a little warmth near the horizon.
ground_blend = np.clip(-elevation / 9.0, 0, 1) ** 0.85
horizon_ref = np.percentile(
    luminance(sky)[(elevation > 0.5) & (elevation < 6.0)], 50)
ground_near = np.array([0.185, 0.168, 0.128], dtype=np.float32) * horizon_ref
ground_far = np.array([0.030, 0.034, 0.028], dtype=np.float32) * horizon_ref
depth = np.clip(-elevation / 55.0, 0, 1) ** 0.7
ground_rgb = ground_near + (ground_far - ground_near) * depth[..., None]
sky = sky * (1.0 - ground_blend[..., None]) + ground_rgb * ground_blend[..., None]

# ------------------------------------------------------------ 5. normalise
# Target an upper-hemisphere mean luminance near 1.0 so Godot's sky energy and
# ambient energy multipliers operate in their authored range.
upper = luminance(sky)[elevation > 0]
upper = upper[upper < 60.0]          # exclude the solar disc from the average
sky *= 1.05 / max(float(upper.mean()), 1e-4)

sky = np.maximum(sky, 0.0)
os.makedirs(OUT_DIR, exist_ok=True)
write_hdr(OUT, sky)
save_preview(sky, "ashwood_sky_preview.png")

lum = luminance(sky)
print("wrote", OUT, sky.shape)
print("  peak %.0f  upper-hemi mean %.3f" % (lum.max(), lum[elevation > 0].mean()))
for label, lo, hi in (("zenith", 70, 90), ("mid", 30, 55),
                      ("horizon", -2, 6), ("ground", -40, -12)):
    band = (elevation > lo) & (elevation < hi)
    print("  %-8s RGB %.3f %.3f %.3f" % (label, *sky[band].reshape(-1, 3).mean(axis=0)))
x, y = sun_pixel(sky)
print("  sun px x=%d y=%d -> azimuth %.1f elevation %.1f"
      % (x, y, x / width * 360.0, 90.0 - y / height * 180.0))
