# AI 生成声明 / AI Disclosure

> 本文件是本项目最重要的元信息之一。请在使用、转载或二次开发前读完。

## 一句话

**SoundWheel 的全部代码、脚本、文档与图标，几乎都是由 AI 编码代理在单次会话中生成的**，人类负责提出需求、提供测试环境、审阅并验证结果。

## 生成信息

| 项目 | 内容 |
| --- | --- |
| 生成时间 | 2026-11-07（Asia/Shanghai） |
| 生成工具 | DeepSeek Harness（DSH）编码代理 |
| 模型 | `deepseek-flash`（界面显示为 DeepSeek-V4.1-Flash） |
| 会话性质 | 一次连续会话：需求 → 调研 → 实现 → 编译 → 实机验证 → 修复 → 交付 |
| 需求来源 | 仓库所有者用自然语言描述：为 Soundpad 做一个"热键唤出圆形音效轮盘、鼠标选择播放、带外圈"的工具 |

### AI 直接产出的内容

- `src/*.cs` —— 全部 15 个源文件（约 6,300 行）
- `tools/*.cs` —— MockSoundpad 假服务器、图标生成器与开发期测试工具
- `build.cmd` / `build.ps1` —— 构建脚本
- `assets/app.ico` —— 由 `tools/MakeIcon.cs` 程序化绘制（GDI+ 画轮盘 + 手写 ICO 编码器），非第三方素材
- `README.md` / `README.en.md` / 本文档等全部文字材料
- `assets/wheel.png` / `assets/settings.png` / `assets/settings-mouse.png` —— 实机运行截图

### 人类的参与

- 提出功能需求与交互偏好（按住模式 / 点按模式、内外双环、主题色等）
- 提供目标机器环境事实（Windows 11 26200、150% 缩放、无 .NET SDK）
- 审阅每一步的编译输出、截图与日志，指出"一开就自动提交""设置窗口被裁切"等现象
- 决定发布形态（单文件 exe、MIT、开源）

## 第三方代码与素材

- **没有复制任何开源项目的代码。** 播放能力仅依赖 Soundpad 官方公开的 Remote Control API 文档（命名管道 `\\.\pipe\sp_remote_control` + 文本命令），客户端为从零实现。
- 图标、截图、Mock 假服务器、文档均为本项目原创内容。
- 依赖：仅 .NET Framework 4.x 自带类库（System.Drawing / System.Windows.Forms / System.IO.Pipes），无 NuGet 包、无外部二进制。

## v1.1.0 增补（FPS / 锁定鼠标兼容）

- 新增 `src/RawMouse.cs`（隐藏窗口 + `RegisterRawInputDevices` / `WM_INPUT`）、`mouseMode` / `mouseSensitivity` 配置项、设置页「游戏与鼠标」、`--debug-mouse` 参数
- 验证方式（实机，Windows 11 150% DPI）：
  - 用 `PostMessage(hwnd, WM_INPUT, 0, 0)` 合成消息，确认隐藏窗口的 `WndProc` 确实收到并解析原始鼠标数据
  - 用内部开关（环境变量 `SOUNDWHEEL_FAKE_RAW=1`，仅用于自动化验证）注入合成位移，截图确认虚拟指针移动到外圈扇区、高亮、中心显示音效名
  - 用 `PostMessage(hwnd, WM_LBUTTONUP, ...)` 触发提交，日志为 `提交 cursor=1450,562 rel=True ... ring=1 slot=1 item=10 倒计时` → `预览选择: 倒计时`
  - 设置页「游戏与鼠标」在 150% DPI 下截图确认无裁切
- 未验证：真实游戏里的原始输入行为（测试机无法注入真实设备事件，且反作弊环境不可控）、光标被 `ClipCursor` 锁定时的自动切换（代码路径已实现并截图验证渲染，但真实锁定场景未复现）

## v1.2.0 增补（接管鼠标 + 简化 UI）

- 新增 `src/MouseHook.cs`（`WH_MOUSE_LL` 低级鼠标钩子）；`WheelWindow` 增加接管逻辑、光标同步（`SyncCapturedCursor`）、深色底盘（`DrawBase`）与单行文字绘制；`Config` 新增 `captureMouse`；设置页「游戏与鼠标」增加接管复选框
- 验证方式（实机，Windows 11 150% DPI）：
  - 应用日志确认钩子安装成功（`鼠标钩子已安装`、`接管=True`），并且一次真实右键被钩子捕获后正确触发 `右键取消`
  - 关闭接管时用 `SOUNDWHEEL_FAKE_RAW=1` 合成位移，截图确认简化后的扇区、单行标签、中心单行标题、指针小圆点、底盘遮住缝隙
  - 用"整屏深色底板 + 显式把轮盘窗口提到最前"的方式截取新版界面（`assets/wheel.png`）；设置页用 `PrintWindow` 截取（`assets/settings-mouse.png`）
  - 用 Z 序枚举确认轮盘窗口（TOPMOST）位于游戏窗口之上
- 未验证：真实游戏内的接管手感（测试机无法稳定注入鼠标事件）、与各反作弊/独占全屏游戏的兼容性

## v1.2.2 增补（无边框全屏游戏看得见轮盘）

- 问题：无边框全屏游戏（典型是 Java 版《我的世界》）会把自身设为置顶，轮盘虽然带 `WS_EX_TOPMOST`，但句柄创建更早、打开时又不重新置顶，于是被压在游戏下面（"打开了但看不见"）
- 改动：`src/Native.cs` 新增 `TopMostNoActivate`（`SetWindowPos(HWND_TOPMOST, SWP_NOACTIVATE|SWP_NOMOVE|SWP_NOSIZE)`）、`WindowCoveredAt`（沿 `GW_HWNDPREV` 检查上层窗口是否盖住轮盘中心）、`ForegroundFullscreen`；`src/WheelWindow.cs` 打开时置顶一次、每 250ms 重新置顶、被遮挡时立即补一次，连续约 3 秒被遮挡写一条告警；`src/Toast.cs` 同样周期性置顶；`src/Program.cs` 支持 `SOUNDWHEEL_TOPMOST_MS`
- 验证：`tools/FakeGame.cs`（编译为 `dist/SimGame.exe`）模拟"无边框全屏 + 每 1200ms 抢回置顶"的游戏窗口，用屏幕像素对照：关闭重试时轮盘中心 = 游戏绿色（被盖住，复现缺陷），开启重试时轮盘中心 = 轮盘深色、轮盘外 = 游戏绿色（可见，修复生效）；截图 `testdataT/B1_crop.png` 目视确认
- 未验证：真实 Java 版 MC / GLFW 全屏下的表现、与其它全屏游戏的实际叠加兼容性

## 验证情况（截至 v1.0.0）

已在 Windows 11 + 150% DPI 的实机上验证：

- 分层窗口逐像素透明渲染、抗锯齿、内外圈扇形与文字排版、悬停高亮与发光
- 命中测试（半径带 + 角度）、点击提交、右键 / Esc 取消、预览模式
- Soundpad 协议链路：`GetSoundlist()` 解析 20 条音效 → `DoPlaySound(n, true, true)` → 模拟端收到 `PLAY n`
- 设置窗口四页、150% DPI 布局与保存、Toast 提示、exe 图标、单实例、数据目录回退
- 已知环境局限：测试机上存在输入注入不稳定现象（同一份代码，`GetAsyncKeyState` 有时读到按下、有时恒为 0），因此"按住 → 移动 → 松开"的真实按键链路只成功复现过部分次数；程序为此内置了"读不到按键状态时自动降级为点按模式"的兜底。

未验证：真实游戏/语音软件中的长时稳定性、多显示器混合 DPI、Windows 10、非中文系统区域设置。

## 风险与免责

- AI 生成的代码**可能包含错误、遗漏或不符合你预期的行为**。请自行审阅后再用于重要场合。
- 本项目与 Leppsoft / Soundpad 官方无任何关联，未获其背书；Soundpad 是各自权利人的商标。
- 软件按 MIT 许可"原样"提供，不附带任何担保。使用本软件造成的任何后果由使用者自行承担。

## 许可与版权说明

本仓库以 [MIT](LICENSE) 许可发布。由于各地法律对 AI 生成内容的版权认定不同，部分内容可能不受版权保护；MIT 条款在上述权利存在的范围内适用，不额外限制你对这些内容的使用。

## 如何指出问题

如果发现文档中的表述不准确、代码有明显错误，或你认为某处内容侵犯了你的权利，请开 issue（模板见 `.github/ISSUE_TEMPLATE`），或直接联系仓库所有者。我们会尽快更正。
