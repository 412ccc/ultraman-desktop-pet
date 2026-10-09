# 奥特曼桌宠素材包 · Ultraman Pet Sprite Pack

Q 版银红奥特曼同人桌宠：**9 组动作 + 16 个看向鼠标的姿势**，共 73 个有效帧。提供透明图集、动画预览和可复用的提取工具，采用 ChatGPT Work 自定义 Pets 的 v2 图集布局。

这是一个**桌宠素材包**。实际桌面显示、鼠标追踪和任务状态切换由兼容的桌宠宿主负责；仓库附带的网页用于预览动作。

![动作预览](docs/previews/all-states.gif)

## 快速体验

下载仓库后，直接在浏览器打开 [`index.html`](index.html)。切换动作，或把鼠标移到预览区域内，查看角色转头；点击角色会挥手。网页完全离线运行，没有外部依赖或追踪代码。

| 文件 | 用途 |
| --- | --- |
| [`assets/ultraman-spritesheet.png`](assets/ultraman-spritesheet.png) | 最终 RGBA 透明 PNG 图集 |
| [`assets/pet-metadata.json`](assets/pet-metadata.json) | 可移植布局、动作帧数和鼠标方向定义 |
| [`docs/previews/`](docs/previews/) | 九组 GIF、综合 GIF/MP4、鼠标方向和静态证明图 |
| [`docs/import.md`](docs/import.md) | 导入与接入说明 |
| [`docs/quality-summary.md`](docs/quality-summary.md) | 图集检查与已知限制 |

## 布局

图集为 **1536 × 2288 px**，8 列 × 11 行，每格 **192 × 208 px**。下表的行号、列号均从 0 开始；每组动作的有效帧从第 0 列向右排列，其余格子保持透明。

| 行 | 状态名 | 动作 | 帧数 |
| --- | --- | --- | --- |
| 0 | `idle` | 待机 | 6 |
| 1 | `running-right` | 向右跑 | 8 |
| 2 | `running-left` | 向左跑 | 8 |
| 3 | `waving` | 挥手 | 4 |
| 4 | `jumping` | 跳跃 | 5 |
| 5 | `failed` | 失落 | 8 |
| 6 | `waiting` | 等待回应 | 6 |
| 7 | `running` | 思考工作 | 6 |
| 8 | `review` | 检查结果 | 6 |
| 9–10 | `look` | 看向鼠标 | 16 |

鼠标方向以屏幕上方为 0°，顺时针每 22.5° 一个姿势：90° 向右、180° 向下、270° 向左。第 9 行为 0°–157.5°，第 10 行为 180°–337.5°。鼠标接近中心时使用第 0 行第 0 列的中立姿势。

## 素材工具

需要 Python 3.10+；只有 Pillow 是第三方依赖。

```sh
python -m pip install -r requirements.txt
python tools/extract_frames.py
python tools/export_previews.py
python tools/validate_pack.py
python -m unittest discover -s tests
```

工具将 73 个格子原样提取到 `build/frames/`，并在 `build/previews/` 导出九组动作 GIF 和鼠标方向 GIF。原始 PNG 不会被修改。预览使用浅色背景；逐帧 PNG 保留透明通道。使用 `--help` 查看输出目录和播放速度选项。

## 许可与角色权利

项目原创的 Python/JavaScript 工具和网页代码采用 [MIT License](LICENSE)。**角色图片、图集以及 GIF/MP4 等衍生预览不在 MIT 授权范围内**；请阅读 [素材与第三方权利说明](ASSET_TERMS.md)。奥特曼角色、名称与商标的权利归相应权利人，本项目不声称获得其授权，也不声称与官方有关联。

---

## English

A chibi Ultraman fan-art sprite pack with **nine animation states and sixteen cursor-facing poses**, using the ChatGPT Work custom Pets v2 atlas layout. It includes a transparent atlas, animation proofs, an offline browser preview, and original Pillow-based extraction helpers. It is an asset pack; a compatible host supplies desktop display and interaction.

Open `index.html` locally to preview states and cursor tracking. For integration, see [import notes](docs/import.md). The atlas is 1536 × 2288 px, arranged as 8 columns and 11 rows with 192 × 208 px cells. It contains 57 state-animation frames and 16 look poses. Look angles start at screen-up (0°) and progress clockwise in 22.5° steps.

The original helper and preview code is MIT-licensed. Artwork and derived previews are excluded from MIT; Ultraman character and trademark rights remain with their respective holders. No third-party permission, official endorsement, or affiliation is claimed. See [asset terms](ASSET_TERMS.md) and the [quality summary](docs/quality-summary.md).
