# SPDX-License-Identifier: MIT
"""Small original helpers for the published sprite pack; requires Pillow only."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
DEFAULT_METADATA = ROOT / "assets" / "pet-metadata.json"


def read_metadata(path: Path = DEFAULT_METADATA) -> dict:
    metadata = json.loads(path.read_text(encoding="utf-8"))
    layout = metadata["spritesheetLayout"]
    for key in ("columns", "rows", "cellWidth", "cellHeight"):
        if not isinstance(layout[key], int) or layout[key] <= 0:
            raise ValueError(f"{key} must be a positive integer")
    cells = list(iter_cells(metadata))
    positions = [(row, column) for _, _, row, column in cells]
    if len(positions) != len(set(positions)):
        raise ValueError("Metadata defines the same cell more than once")
    if len(cells) != metadata["validFrameCount"]:
        raise ValueError("validFrameCount does not match the cell definitions")
    for row, column in positions:
        if not (0 <= row < layout["rows"] and 0 <= column < layout["columns"]):
            raise ValueError("A frame lies outside the declared atlas grid")
    directions = metadata["lookDirections"]
    if len(directions) != layout["lookDirectionCount"]:
        raise ValueError("lookDirectionCount does not match the direction list")
    return metadata


def iter_cells(metadata: dict):
    """Yield (group, index, row, column) for valid cells only."""
    for state in metadata["states"]:
        if not isinstance(state["frameCount"], int) or state["frameCount"] <= 0:
            raise ValueError("Every animation must contain at least one frame")
        for column in range(state["frameCount"]):
            yield state["name"], column, state["rowIndex"], column
    for index, direction in enumerate(metadata["lookDirections"]):
        yield "look", index, direction["rowIndex"], direction["columnIndex"]


def load_atlas(metadata: dict, atlas_path: Path | None = None) -> Image.Image:
    source = atlas_path or ROOT / metadata["spritesheetPath"]
    with Image.open(source) as image:
        layout = metadata["spritesheetLayout"]
        expected = (layout["columns"] * layout["cellWidth"],
                    layout["rows"] * layout["cellHeight"])
        if image.size != expected:
            raise ValueError(f"Atlas size {image.size} does not match {expected}")
        if image.format != "PNG" or image.mode != "RGBA":
            raise ValueError("Expected a transparent RGBA PNG atlas")
        return image.copy()


def crop_cell(atlas: Image.Image, metadata: dict, row: int, column: int) -> Image.Image:
    layout = metadata["spritesheetLayout"]
    if not (0 <= row < layout["rows"] and 0 <= column < layout["columns"]):
        raise ValueError("Cell lies outside the atlas")
    width, height = layout["cellWidth"], layout["cellHeight"]
    left, top = column * width, row * height
    return atlas.crop((left, top, left + width, top + height))


def extract_frames(atlas: Image.Image, metadata: dict, output: Path) -> int:
    count = 0
    for group, index, row, column in iter_cells(metadata):
        folder = output / group
        folder.mkdir(parents=True, exist_ok=True)
        crop_cell(atlas, metadata, row, column).save(folder / f"{index:02d}.png")
        count += 1
    return count


def save_gif(path: Path, cells: list[Image.Image], duration_ms: int,
             background: tuple[int, int, int] = (238, 241, 245)) -> None:
    """GIF previews flatten alpha; extracted PNG cells retain original alpha."""
    if not cells:
        raise ValueError("Cannot export an empty animation")
    if duration_ms < 20 or duration_ms % 10:
        raise ValueError("GIF duration must be at least 20 ms and a multiple of 10")
    frames = []
    for cell in cells:
        frame = Image.new("RGB", cell.size, background)
        frame.paste(cell, mask=cell.getchannel("A"))
        frames.append(frame)
    path.parent.mkdir(parents=True, exist_ok=True)
    frames[0].save(path, format="GIF", save_all=True, append_images=frames[1:],
                   duration=duration_ms, loop=0, disposal=2, optimize=False)


def check_pack(atlas: Image.Image, metadata: dict, atlas_path: Path) -> dict:
    layout = metadata["spritesheetLayout"]
    used = {(row, column) for _, _, row, column in iter_cells(metadata)}
    empty_used, filled_unused = [], []
    for row in range(layout["rows"]):
        for column in range(layout["columns"]):
            bounds = crop_cell(atlas, metadata, row, column).getchannel("A").getbbox()
            if (row, column) in used and bounds is None:
                empty_used.append([row, column])
            if (row, column) not in used and bounds is not None:
                filled_unused.append([row, column])
    digest = hashlib.sha256(atlas_path.read_bytes()).hexdigest()
    raw = atlas.tobytes()
    residue = sum(1 for offset in range(0, len(raw), 4)
                  if raw[offset + 3] == 0 and any(raw[offset:offset + 3]))
    errors = []
    if digest != metadata["sha256"]:
        errors.append("SHA-256 differs from the published atlas")
    if empty_used:
        errors.append("Some declared frames are fully transparent")
    if filled_unused:
        errors.append("Some unused cells contain visible pixels")
    if residue:
        errors.append("Fully transparent pixels contain RGB residue")
    return {"ok": not errors, "sha256": digest,
            "dimensions": list(atlas.size), "mode": atlas.mode,
            "stateCount": len(metadata["states"]),
            "lookDirectionCount": len(metadata["lookDirections"]),
            "validFrameCount": len(used),
            "unusedCellCount": layout["columns"] * layout["rows"] - len(used),
            "transparentRgbResiduePixels": residue,
            "emptyUsedCells": empty_used, "filledUnusedCells": filled_unused,
            "errors": errors}
