// SPDX-License-Identifier: MIT
// Optional developer check: node tests/test_preview.cjs
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const preview = require("../assets/preview.js");
const metadata = JSON.parse(fs.readFileSync(path.join(__dirname, "../assets/pet-metadata.json"), "utf8"));

assert.equal(preview.states.length, 9);
assert.equal(metadata.lookDirections.length, 16);
assert.equal(preview.states.reduce((total, state) => total + state.frameCount, 0) + 16, 73);
for (const state of preview.states) {
  const canonical = metadata.states.find(item => item.name === state.name);
  assert.equal(state.rowIndex, canonical.rowIndex);
  assert.equal(state.frameCount, canonical.frameCount);
}
for (const [index, direction] of metadata.lookDirections.entries()) {
  assert.deepEqual(preview.lookCell(index), {
    rowIndex: direction.rowIndex, columnIndex: direction.columnIndex
  });
  assert.equal(preview.directionIndexFromDegrees(direction.degrees), index);
  const radians = direction.degrees * Math.PI / 180;
  assert.equal(preview.directionForVector(Math.sin(radians) * 100, -Math.cos(radians) * 100), index);
  const boundary = direction.degrees + 11.25;
  assert.equal(preview.directionIndexFromDegrees(boundary - 1e-7), index);
  assert.equal(preview.directionIndexFromDegrees(boundary), (index + 1) % 16);
  assert.equal(preview.directionIndexFromDegrees(boundary + 1e-7), (index + 1) % 16);
}
assert.equal(preview.directionIndexFromDegrees(-22.5), 15);
assert.equal(preview.directionIndexFromDegrees(720), 0);
assert.equal(preview.directionForVector(0, 0), null);
assert.equal(preview.directionForVector(18, 0, 18), null);
assert.equal(preview.directionForVector(18.01, 0, 18), 4);
assert.throws(() => preview.lookCell(-1), RangeError);
assert.throws(() => preview.lookCell(16), RangeError);
assert.throws(() => preview.directionForVector(NaN, 0), TypeError);
assert.throws(() => preview.directionIndexFromDegrees(Infinity), TypeError);
console.log("Preview checks passed: nine states, 73 cells, sixteen directions, all sector boundaries and neutral deadzone.");
