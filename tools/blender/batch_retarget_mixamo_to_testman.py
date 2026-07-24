"""Batch wrapper for retarget_mixamo_to_testman.py.

Example:
blender --background --python batch_retarget_mixamo_to_testman.py -- \
  --remy assets/characters/player/Remy.fbx \
  --animations assets/characters/player/anim \
  --testman assets/characters/player/testman.fbx \
  --output generated/testman_animations --recursive
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from retarget_mixamo_to_testman import retarget_file


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--remy", type=Path, required=True)
    parser.add_argument("--animations", type=Path, required=True)
    parser.add_argument("--testman", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--pattern", default="*.fbx")
    parser.add_argument("--recursive", action="store_true")
    parser.add_argument("--fps", type=int, default=30)
    parser.add_argument(
        "--root-motion",
        choices=("preserve", "in-place"),
        default="preserve",
    )
    return parser.parse_args(argv)


def blender_args() -> list[str]:
    return sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []


def main() -> None:
    args = parse_args(blender_args())
    iterator = (
        args.animations.rglob(args.pattern)
        if args.recursive
        else args.animations.glob(args.pattern)
    )
    remy = args.remy.resolve()
    testman = args.testman.resolve()
    animations = [
        path
        for path in sorted(iterator)
        if path.resolve() not in {remy, testman}
    ]
    if not animations:
        raise RuntimeError(f"No animations matched {args.pattern!r}.")

    for animation in animations:
        relative = animation.relative_to(args.animations)
        output = (args.output / relative).with_suffix(".fbx")
        retarget_file(
            args.remy,
            animation,
            args.testman,
            output,
            preserve_root_motion=args.root_motion == "preserve",
            fps=args.fps,
        )

    print(f"BATCH_RETARGET_COMPLETE: {len(animations)} files")


if __name__ == "__main__":
    main()
