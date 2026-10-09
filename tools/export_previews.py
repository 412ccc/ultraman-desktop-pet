# SPDX-License-Identifier: MIT
"""Export GIF previews from atlas cells, using a solid display background."""

import argparse
from pathlib import Path

from sprite_tools import DEFAULT_METADATA, ROOT, crop_cell, load_atlas, read_metadata, save_gif


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--metadata", type=Path, default=DEFAULT_METADATA)
    parser.add_argument("--atlas", type=Path, help="Override the atlas PNG")
    parser.add_argument("--output", type=Path, default=ROOT / "build" / "previews")
    parser.add_argument("--duration", type=int, help="Frame time in ms (multiple of 10; minimum 20)")
    args = parser.parse_args()
    metadata = read_metadata(args.metadata)
    atlas = load_atlas(metadata, args.atlas)
    duration = args.duration if args.duration is not None else metadata["previewFrameDurationMs"]
    all_cells = []
    for state in metadata["states"]:
        cells = [crop_cell(atlas, metadata, state["rowIndex"], column)
                 for column in range(state["frameCount"])]
        save_gif(args.output / f"{state['name']}.gif", cells, duration)
        all_cells.extend(cells)
    looks = [crop_cell(atlas, metadata, item["rowIndex"], item["columnIndex"])
             for item in metadata["lookDirections"]]
    save_gif(args.output / "cursor-look-loop.gif", looks, duration)
    save_gif(args.output / "all-states.gif", all_cells, duration)
    print(f"Exported {len(metadata['states']) + 2} GIF previews from final atlas cells.")


if __name__ == "__main__":
    main()
