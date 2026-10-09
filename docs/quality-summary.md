# 图集质量摘要 / Atlas Quality Summary

此公开包使用制作时确认的最终透明图集；动画证明图从该图集的格子生成，不包含淘汰版本、账号记录或上传日志。

| 检查项 | 记录结果 |
| --- | --- |
| 格式、尺寸 | PNG / RGBA，1536 × 2288 px |
| 布局 | 8 × 11 格，每格 192 × 208 px |
| 有效格子 | 73：57 个动作帧 + 16 个方向姿势 |
| 未使用格子 | 15 个，全部透明 |
| 原始文件大小 | 2,390,278 bytes |
| 完全透明像素中的 RGB 残留 | 0 |
| 格子检查的色键残留和色边 | 0 |
| 跳跃离地 / 落地偏差 | 13 px / 0 px |
| 方向姿势中心最大漂移 | 1.5 px |
| 方向盲测 | 28 个轴向判断中 26 个清晰吻合、2 个俯仰判断不明确 |

SHA-256：`70e2de7d9c163cda4da08b2e93b37d1f953864bb689719246938243784b91666`。

已知限制：接近水平的部分方向，头部俯仰差异较细微；67.5° 和 292.5° 的向上俯仰在盲测中不明确。157.5°→180°、292.5°→315°、337.5°→0° 的相邻姿势变化比邻近方向明显。主要上下左右姿势通过了检查；这些细微方向和连续性差异保留在成品中。

可运行 `python tools/validate_pack.py` 重新检查文件哈希、尺寸、有效格子和透明空格，运行测试验证裁切和预览导出。上述运动与方向评审数值来自成品制作阶段的检查，不代表后续所有宿主上的显示效果。

The package contains the final confirmed atlas and proofs made from its cells. It contains 73 valid cells (57 animation frames and 16 look poses), with 15 fully transparent unused cells. Recorded jump lift is 13 px with zero landing offset; maximum look-pose center drift is 1.5 px. Direction review found 26 clearly matching axis judgments out of 28, with two ambiguous pitch judgments near horizontal.

Some near-horizontal pitch changes are subtle, and three adjacent direction transitions are more pronounced than their neighbors. The cardinal directions passed review. These limitations are retained in the final artwork. Run the included validator and tests to recheck file identity, layout, cropping, and GIF export; recorded motion and semantic assessments describe the creation-stage review.
