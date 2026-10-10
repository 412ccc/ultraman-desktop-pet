# Windows 独立桌宠

下载 [最新发布](https://github.com/412ccc/ultraman-desktop-pet/releases/latest) 中的 `UltramanPet-Setup-1.0.1.exe` 安装，或解压 `UltramanPet-Portable-1.0.1.zip` 到 D 盘或其他非 C 盘后双击 `UltramanPet.exe`。支持 Windows 10 / 11 的 .NET Framework 4.x，运行时不需要 Python、ChatGPT、浏览器或网络。暂未提供 macOS / Linux 版本。二进制文件未做数字签名。

鼠标靠近时挥手并转头看向鼠标；单击跳跃、双击挥手、按住左键拖动。右键菜单提供九种动作、小中大三种尺寸、鼠标追踪、自动走动和退出。默认关闭自动走动。托盘图标也有菜单，双击图标可找回桌宠。程序不会添加开机自启。

## 安装、更新和卸载

安装到 `D:\Apps\UltramanDesktopPet`，需要可写的 D 盘。桌面和开始菜单快捷方式默认不创建，仅在用户勾选时创建；这两种快捷方式位于 Windows 管理的目录，可能在 C 盘。安装记录注册到当前用户，无需管理员权限。安装器不会自动回退到 C 盘；没有 D 盘时，可把免安装版放到其他非 C 盘位置使用。

更新前先退出桌宠，再运行安装器。卸载前也先退出桌宠，然后从 Windows“已安装的应用”卸载，或运行安装目录中的 `Uninstall.exe`。卸载只移除已标记安装目录中的程序文件和指向本程序的快捷方式，不递归删除目录；无关文件和已被改为指向其他程序的快捷方式会保留。位置及大小保存在程序旁的 `data\position.txt`，卸载时保留；从 C 盘启动时，数据改用 `D:\Apps\UltramanDesktopPet\data`。卸载临时助手保存在 `D:\Apps\.UltramanPetTemp`。免安装版退出后可自行删除解压目录。Windows 注册表、系统缓存以及宿主软件自身的状态仍由 Windows 或对应软件管理。

1.0.0 的安装与设置目录在用户的 LocalAppData 中；新版不会自动删除旧版目录或移动用户文件。升级旧版时可先将原位置设置复制到新版的 `data\position.txt`，退出旧版并卸载旧版后，再安装新版。

## 从源码构建

在非 C 盘的 Windows 工作目录中安装 Python 3.10+ 和项目的 Pillow 依赖，然后在仓库根目录运行：

```text
python -m pip install -r requirements.txt
python tools/build_windows.py
```

脚本使用 Windows .NET Framework 自带的 `csc.exe` 编译器；优先使用 Framework64，再使用 Framework。不下载编译器、运行库或安装器工具，不需要 Visual Studio。Python 和 Pillow 只用于构建及素材工具。

输出为 `dist/UltramanPet-Setup-1.0.1.exe`、`dist/UltramanPet-Portable-1.0.1.zip` 和 `dist/SHA256SUMS.txt`。构建中间文件位于 `build/windows/`，子进程的 `TEMP/TMP` 指向 `build/windows/tmp/`，不使用 C 盘临时目录。这些目录均不提交到 Git。

## 实现与验证

- `Pet.cs`：WinForms 窗口与托盘，内嵌 73 帧透明图集，九种动作和十六个鼠标方向。通过 `UpdateLayeredWindow` 逐像素合成透明背景，完全透明位置可穿透点击；不用色键移除背景。窗口为无边框、置顶工具窗口。
- `Setup.cs`：当前用户安装器、快捷方式与卸载。安装目录须有本程序标记才能更新或删除。卸载器先在临时目录启动自身副本，等原进程退出后移除已知程序文件；临时副本留待系统或用户清理。
- `Storage.cs`：程序安装、设置和卸载助手的非 C 盘路径规则；从 C 盘启动时将设置重定向到 D 盘。
- `Tests.cs`：178 项检查覆盖 73 帧的透明度与尺寸、16 个方向及边界、动画循环、多显示器坐标限制、安装与更新的载荷一致性、真实快捷方式目标、卸载保护无关文件以及拒绝操作未标记目录；新增检查覆盖 D 盘默认目录、数据路径、C 盘启动重定向、临时助手位置和默认不创建快捷方式。

安装、更新及卸载测试在独立测试目录运行；不创建真实用户的卸载注册表条目，也不覆盖用户桌面快捷方式。发布前在当前 Windows 桌面观察了透明显示、单击跳跃、双击挥手及安装器布局。没有在每种 Windows 版本和显示缩放组合上验证，也没有在真实用户安装目录执行完整安装/卸载界面流程。

构建后可运行 `build/windows/UltramanPetPreview.exe` 做临时预览：使用相同图集和交互，显示可观察的任务栏窗口，60 秒后关闭，不保存位置设置。正式 `UltramanPet.exe` 长期运行，右键退出；该预览程序不会包含在发布包里。

原生透明窗口实现参考微软的 [窗口特性说明](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features) 与 [UpdateLayeredWindow API](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow)。原创代码采用 MIT；内嵌图片、图标等素材另见 [权利说明](../ASSET_TERMS.md)。
