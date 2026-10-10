# 导入与接入 / Import and Integration

Windows 独立桌宠已包含在本项目中，直接下载 [安装版或免安装版](https://github.com/412ccc/ultraman-desktop-pet/releases/latest) 即可使用；不需要先导入其他宿主。下面的说明用于将图集接入 ChatGPT Work 或其他桌宠宿主。

## ChatGPT Work 自定义桌宠

在支持自定义 Pets 的 ChatGPT Work 环境中，把 `assets/ultraman-spritesheet.png` 提供给桌宠创建或更新工具，并提供 `assets/pet-metadata.json` 的布局参数：

```json
{
  "spriteVersionNumber": 2,
  "spritesheetLayout": {
    "columns": 8,
    "rows": 11,
    "cellWidth": 192,
    "cellHeight": 208,
    "lookDirectionCount": 16,
    "neutralLookFrame": { "rowIndex": 0, "columnIndex": 0 }
  }
}
```

完成宿主的素材检查、创建或更新、激活流程后，由宿主显示和控制桌宠。不同环境的导入入口可能不同。仓库中的 JSON 是公开的布局说明，不是账号状态或可直接提交的上传凭证；宿主可能只接受其中部分字段。

## 其他宿主

按 `states` 的 `rowIndex` 和 `frameCount` 顺序裁切并播放；忽略每行末尾的透明空格。注意 `running` 在此布局中表示“思考工作”，移动跑步对应 `running-left` 和 `running-right`。

鼠标方向可按以下逻辑选择：

1. 取得从角色头部中心到鼠标的屏幕坐标差 `(dx, dy)`，屏幕 y 轴向下。
2. 计算 `degrees = (atan2(dx, -dy) * 180 / π + 360) % 360`。
3. 取最近的 22.5° 扇区，`index = floor((degrees + 11.25) / 22.5) % 16`。
4. 使用 `lookDirections[index]` 的格子；鼠标非常接近角色中心时使用中立格子。

角色躯干保持正面，头部随鼠标偏转。浏览器预览只演示此方向选择和动作播放；实际桌面窗口、拖拽、移动及宿主事件接入需由相应宿主实现。

## English

In a ChatGPT Work environment that supports custom Pets, supply the transparent atlas to the pet creation/update tool along with the v2 layout above. Follow the host's asset validation, creation/update, and activation workflow. The included metadata describes the public layout; it is not account state or an upload credential, and a host may accept only a subset of its fields.

For other compatible hosts, crop the cells identified by `states` and `lookDirections`. Ignore unused transparent cells. `running` means working/thinking in this layout; movement states are `running-left` and `running-right`. Pointer angles start at screen-up and advance clockwise. The offline browser page demonstrates playback and direction selection. Desktop windows, dragging, movement, and host event handling belong to the host implementation.
