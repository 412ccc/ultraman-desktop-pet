# SPDX-License-Identifier: MIT
"""Check pixel-preserving extraction, grid boundaries, and real GIF playback data."""

import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "tools"))
from sprite_tools import (DEFAULT_METADATA, check_pack, crop_cell, extract_frames,
                          iter_cells, load_atlas, read_metadata, save_gif)


class SpritePackTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.metadata = read_metadata()
        cls.atlas = load_atlas(cls.metadata)

    def test_atlas_identity_occupied_and_unused_cells(self):
        report = check_pack(self.atlas, self.metadata,
                            ROOT / self.metadata["spritesheetPath"])
        self.assertTrue(report["ok"], report["errors"])
        self.assertEqual(report["validFrameCount"], 73)
        self.assertEqual(report["unusedCellCount"], 15)

    def test_every_extracted_png_preserves_source_pixels(self):
        with tempfile.TemporaryDirectory() as folder:
            output = Path(folder)
            self.assertEqual(extract_frames(self.atlas, self.metadata, output), 73)
            self.assertEqual(len(list(output.rglob("*.png"))), 73)
            for group, index, row, column in iter_cells(self.metadata):
                expected = crop_cell(self.atlas, self.metadata, row, column)
                with Image.open(output / group / f"{index:02d}.png") as actual:
                    self.assertEqual(actual.mode, "RGBA")
                    self.assertEqual(actual.size, (192, 208))
                    self.assertEqual(actual.tobytes(), expected.tobytes())

    def test_crop_rejects_outside_grid(self):
        for row, column in ((-1, 0), (0, -1), (11, 0), (0, 8)):
            with self.assertRaises(ValueError):
                crop_cell(self.atlas, self.metadata, row, column)

    def test_metadata_rejects_duplicate_and_outside_cells(self):
        for changed in ("duplicate", "outside"):
            altered = json.loads(DEFAULT_METADATA.read_text(encoding="utf-8"))
            altered["lookDirections"][0]["rowIndex"] = 0 if changed == "duplicate" else 11
            with tempfile.TemporaryDirectory() as folder:
                path = Path(folder) / "metadata.json"
                path.write_text(json.dumps(altered), encoding="utf-8")
                with self.assertRaises(ValueError):
                    read_metadata(path)

    def test_jump_gif_retains_all_five_frames_and_timing(self):
        frames = [crop_cell(self.atlas, self.metadata, 4, column) for column in range(5)]
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "jump.gif"
            save_gif(path, frames, 140)
            with Image.open(path) as gif:
                self.assertEqual(gif.n_frames, 5)
                self.assertEqual(gif.info["loop"], 0)
                decoded = []
                for index in range(gif.n_frames):
                    gif.seek(index)
                    self.assertEqual(gif.size, (192, 208))
                    self.assertEqual(gif.info["duration"], 140)
                    decoded.append(gif.convert("RGB").tobytes())
                self.assertEqual(len(set(decoded)), 5)


if __name__ == "__main__":
    unittest.main()
