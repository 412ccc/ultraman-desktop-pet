// SPDX-License-Identifier: MIT
// Original offline preview helpers. No host runtime or plugin code is included.
(function (root, make) {
  const api = make();
  if (typeof module === "object" && module.exports) module.exports = api;
  else root.PetSpritePreview = api;
})(typeof globalThis !== "undefined" ? globalThis : this, function () {
  "use strict";
  const states = [
    { name: "idle", label: "待机 · Idle", rowIndex: 0, frameCount: 6 },
    { name: "running-right", label: "向右跑 · Run right", rowIndex: 1, frameCount: 8 },
    { name: "running-left", label: "向左跑 · Run left", rowIndex: 2, frameCount: 8 },
    { name: "waving", label: "挥手 · Wave", rowIndex: 3, frameCount: 4 },
    { name: "jumping", label: "跳跃 · Jump", rowIndex: 4, frameCount: 5 },
    { name: "failed", label: "失落 · Sad", rowIndex: 5, frameCount: 8 },
    { name: "waiting", label: "等待回应 · Waiting", rowIndex: 6, frameCount: 6 },
    { name: "running", label: "思考工作 · Working", rowIndex: 7, frameCount: 6 },
    { name: "review", label: "检查结果 · Review", rowIndex: 8, frameCount: 6 }
  ];
  const layout = { columns: 8, rows: 11, cellWidth: 192, cellHeight: 208 };

  function directionIndexFromDegrees(degrees) {
    if (!Number.isFinite(degrees)) throw new TypeError("Angle must be finite");
    const normalized = ((degrees % 360) + 360) % 360;
    return Math.floor((normalized + 11.25) / 22.5) % 16;
  }

  function directionForVector(dx, dy, deadzone = 0) {
    if (![dx, dy, deadzone].every(Number.isFinite) || deadzone < 0)
      throw new TypeError("Vector and nonnegative deadzone must be finite");
    if (Math.hypot(dx, dy) <= deadzone) return null;
    return directionIndexFromDegrees(Math.atan2(dx, -dy) * 180 / Math.PI);
  }

  function lookCell(index) {
    if (!Number.isInteger(index) || index < 0 || index >= 16)
      throw new RangeError("Look index must be between 0 and 15");
    return { rowIndex: 9 + Math.floor(index / 8), columnIndex: index % 8 };
  }

  function mount() {
    const stage = document.getElementById("stage");
    const canvas = document.getElementById("pet");
    const context = canvas.getContext("2d");
    const select = document.getElementById("state");
    const follow = document.getElementById("follow");
    const speed = document.getElementById("speed");
    const status = document.getElementById("status");
    const image = new Image();
    let selected = states[0];
    let pointer = null;
    let started = performance.now();
    let waveUntil = 0;
    let loaded = false;

    for (const state of states) {
      const option = document.createElement("option");
      option.value = state.name;
      option.textContent = state.label;
      select.appendChild(option);
    }
    select.addEventListener("change", function () {
      selected = states.find(state => state.name === select.value);
      started = performance.now();
      waveUntil = 0;
    });
    stage.addEventListener("pointermove", event => {
      pointer = { x: event.clientX, y: event.clientY };
    });
    stage.addEventListener("pointerleave", () => { pointer = null; });
    document.getElementById("wave").addEventListener("click", startWave);
    document.getElementById("pet-button").addEventListener("click", startWave);
    function startWave() {
      started = performance.now();
      waveUntil = started + 1120 / Number(speed.value);
    }

    function draw(now) {
      if (loaded) {
        let row = selected.rowIndex;
        let column = Math.floor((now - started) * Number(speed.value) / 140) % selected.frameCount;
        let label = selected.label;
        if (now < waveUntil) {
          row = 3;
          column = Math.floor((now - started) * Number(speed.value) / 140) % 4;
          label = "点击挥手 · Hello!";
        } else if (follow.checked && pointer) {
          const box = canvas.getBoundingClientRect();
          const headX = box.left + box.width / 2;
          const headY = box.top + box.height * 0.30;
          const index = directionForVector(pointer.x - headX, pointer.y - headY, 18);
          if (index === null) {
            row = 0;
            column = 0;
            label = "看向中心 · Neutral";
          } else {
            const cell = lookCell(index);
            row = cell.rowIndex;
            column = cell.columnIndex;
            label = "看向鼠标 · " + (index * 22.5) + "°";
          }
        }
        context.clearRect(0, 0, canvas.width, canvas.height);
        context.drawImage(image, column * 192, row * 208, 192, 208, 0, 0, 192, 208);
        if (status.textContent !== label) status.textContent = label;
      }
      requestAnimationFrame(draw);
    }
    image.onload = function () {
      if (image.naturalWidth !== 1536 || image.naturalHeight !== 2288) {
        status.textContent = "图集尺寸不正确 · Unexpected atlas size";
        return;
      }
      loaded = true;
      requestAnimationFrame(draw);
    };
    image.onerror = function () {
      status.textContent = "找不到图集，请保留 assets 文件夹 · Atlas not found";
    };
    image.src = "assets/ultraman-spritesheet.png";
  }

  return { states, layout, directionIndexFromDegrees, directionForVector, lookCell, mount };
});
