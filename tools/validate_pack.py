# SPDX-License-Identifier: MIT
"""Verify the published atlas identity, transparency, and occupied cells."""

import argparse
import json
from pathlib import Path

from sprite_tools import DEFAULT_METADATA, ROOT, check_pack, load_atlas, read_metadata


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--metadata", type=Path, default=DEFAULT_METADATA)
    parser.add_argument("--atlas", type=Path, help="Override the atlas PNG")
    args = parser.parse_args()
    metadata = read_metadata(args.metadata)
    atlas_path = args.atlas or ROOT / metadata["spritesheetPath"]
    report = check_pack(load_atlas(metadata, atlas_path), metadata, atlas_path)
    print(json.dumps(report, ensure_ascii=False, indent=2))
    raise SystemExit(0 if report["ok"] else 1)


if __name__ == "__main__":
    main()
