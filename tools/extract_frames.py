# SPDX-License-Identifier: MIT
"""Extract the exact 73 published cells without resizing or repainting."""

import argparse
from pathlib import Path

from sprite_tools import DEFAULT_METADATA, ROOT, extract_frames, load_atlas, read_metadata


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--metadata", type=Path, default=DEFAULT_METADATA)
    parser.add_argument("--atlas", type=Path, help="Override the atlas PNG")
    parser.add_argument("--output", type=Path, default=ROOT / "build" / "frames")
    args = parser.parse_args()
    metadata = read_metadata(args.metadata)
    count = extract_frames(load_atlas(metadata, args.atlas), metadata, args.output)
    print(f"Extracted {count} RGBA PNG frames.")


if __name__ == "__main__":
    main()
