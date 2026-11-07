# SoundWheel — Soundpad 音效轮盘

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![AI Generated](https://img.shields.io/badge/content-AI--generated-8A2BE2.svg)](AI-DISCLOSURE.md)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-lightgrey.svg)](#系统要求)
[![Version](https://img.shields.io/badge/version-1.0.0-success.svg)](CHANGELOG.md)
[![Build](https://github.com/Focold/soundpad-wheel/actions/workflows/build.yml/badge.svg)](https://github.com/Focold/soundpad-wheel/actions/workflows/build.yml)

> [!IMPORTANT]
> **AI 生成声明**：本项目的代码、脚本、文档与图标几乎全部由 **AI 编码代理**（DeepSeek Harness，模型 `deepseek-flash`）在 2026-11-07 的单次会话中生成，人类负责提出需求、提供测试环境与验证结果。
> 详细说明、验证范围与免责条款见 **[AI-DISCLOSURE.md](AI-DISCLOSURE.md)**。

📖 [English version](README.en.md) · [更新日志](CHANGELOG.md) · [贡献指南](CONTRIBUTING.md) · [安全策略](SECURITY.md) · [行为准则](CODE_OF_CONDUCT.md) · [许可](LICENSE)

![轮盘](assets/wheel.png)

按住一个热键，屏幕中心（光标处）弹出一圈音效；鼠标一甩、手指一松，音效就播出去了。
内圈放常用的 8 个，外圈再挂 12 个，一共 20 个音效不用记快捷键。

SoundWheel 是一个独立的小程序（单文件 exe，无需安装运行时），它通过 **Soundpad 官方的 Remote Control API** 控制 Soundpad 播放指定音效，因此不会修改 Soundpad 本体，也不会影响它的任何设置。

---

## 功能一览

- **热键唤出**：默认 `Ctrl + Alt + W`，可改成任意组合键；轮盘出现在鼠标位置
- **内外双环**：内圈 1–12 格，外圈 0–16 格，两圈独立分配
- **两种操作方式**
  - 按住模式：按住热键 → 移动鼠标选中 → 松开即播放
  - 点按模式：按一下打开 → 左键点击扇区播放 → 自动关闭
- **右键 / Esc 取消**，取消不会播放
- **悬停反馈**：高亮扇区、发光描边、中心显示音效名称
- **播放目标可配**：只发麦克风 / 只走扬声器 / 两者同时（Soundpad 侧需要在"麦克风"设备上正常工作）
- **托盘常驻**：托盘菜单可打开轮盘、改设置、刷新列表、打开数据目录、退出
- **外观可调**：整体大小 70%–160%、主题色、内外圈格数、开关打开/关闭动画
- **登录时自动启动**（写 HKCU 的 Run 项，不需要管理员权限）
- **播放提示条**（可选）：播放时在光标下方弹出一个小药丸提示

---

## 系统要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows 10 / 11（64 位） |
| Soundpad | 已安装并**正在运行**（Steam 版或独立版均可） |
| 运行时 | 不需要额外安装 —— 使用系统自带的 .NET Framework 4.x（Win10/11 自带 4.8） |
| 权限 | 普通用户即可。若 Soundpad 以管理员身份运行，本程序也要以管理员身份运行（Windows 的 UIPI 限制） |

> Soundpad 的远程控制默认就是开启的，不用在它里面勾任何选项，只要它正在运行即可。

---

## 快速开始

1. 双击 `SoundWheel.exe`（从 [Releases](https://github.com/Focold/soundpad-wheel/releases) 下载，或自行 `build.cmd` 后的 `dist\SoundWheel.exe`）；首次启动会在托盘显示一个轮盘小图标
2. 右键托盘图标 → **设置…**
3. 切到 **轮盘布局** 页 → 点 **刷新音效列表** → 给内圈 / 外圈逐格选音效 → **保存**
4. 回到游戏或语音软件，按住 `Ctrl + Alt + W`，鼠标移到想要的扇区，松开

第一次运行还没有配置时，直接把想要的声音先分配好即可；**音效库** 页可以看到从 Soundpad 读到的完整列表（行首数字就是 Soundpad 的 `All sounds` 序号）。

![设置界面](assets/settings.png)

---

## 使用方式

| 操作 | 效果 |
| --- | --- |
| 按住热键 | 轮盘在光标处打开，中心显示"选择音效 / 移动鼠标" |
| 移动鼠标 | 高亮所在扇区，中心显示该音效名称；移到空白格显示"空闲位置" |
| 松开热键 | 播放当前高亮的音效，然后关闭（中心圆形区域不播放） |
| 右键 / Esc | 取消，不播放 |
| 点按模式：左键点击扇区 | 播放该音效并关闭 |
| 点按模式：15 秒无操作 | 自动关闭 |

**关于"自动切换点按模式"**：程序靠轮询按键状态判断"松开了没有"。极少数情况下（远程桌面、独占全屏、被反作弊/输入法/其他输入软件抢占等情况）读不到按键状态，此时轮盘打开约 2.5 秒后会自动切换到点按模式，中心提示会从"松开播放"变成"点击播放"，此时用鼠标左键点选即可 —— 功能不会被卡死。

---

## 设置界面

| 页 | 内容 |
| --- | --- |
| **常规** | 快捷键（点输入框后按下组合键，Backspace 清除）、点按模式、播放目标（麦克风 / 扬声器 / 播放提示）、外观与布局（整体大小、内圈格数、外圈格数、主题色、动画）、启动与数据（登录时自动启动、配置文件路径）、使用说明 |
| **轮盘布局** | 刷新音效列表；内圈 1–12、外圈 0–16 逐格下拉分配（显示为 `序号 · 名称`，`（空）` 表示不分配） |
| **音效库** | 只读列表，确认 Soundpad 里读到了什么（编号 = `All sounds` 顺序 = `DoPlaySound` 的索引） |
| **关于** | 版本与说明 |

底部固定栏：**预览轮盘**（不用热键、直接在屏幕中心打开轮盘 3 秒）、**保存**、**取消**。

---

## 数据与配置

默认位置：`%APPDATA%\SoundWheel\`

| 文件 | 说明 |
| --- | --- |
| `config.json` | 热键、内外圈分配、外观、播放选项 |
| `sounds.json` | 最近一次从 Soundpad 读到的音效列表缓存（启动时先用它，连上后自动刷新） |
| `soundwheel.log` | 运行日志（排查问题时看这个） |

可用 `--data <目录>` 参数或环境变量 `SOUNDWHEEL_DATA` 覆盖数据目录（例如做成绿色版放在 U 盘里）。

如果 `%APPDATA%` 不可写（受限账户、漫游配置被策略锁定等），程序会依次回退到 `%LOCALAPPDATA%\SoundWheel` → `程序目录\SoundWheelData` → 临时目录，并在日志第一行写明实际使用的目录，而不是直接报错退出。

**抗索引漂移**：每个槽位同时保存 `title` 和 `index`。播放时优先在当前音效列表里按标题重新定位真实索引，找不到才退回保存的索引 —— 所以你在 Soundpad 里增删音效后，通常不需要重新配置。

---

## 命令行参数

```
SoundWheel.exe [--data <目录>] [--preview [秒]] [--settings] [--debug-keys]
```

| 参数 | 说明 |
| --- | --- |
| `--preview [秒]` | 不注册热键，直接在屏幕中心打开轮盘做预览（默认 6 秒），适合截图/检查外观 |
| `--settings` | 直接打开设置窗口（不起托盘、不注册热键） |
| `--data <目录>` | 指定数据目录（优先级高于 `SOUNDWHEEL_DATA`） |
| `--debug-keys` | 把按键原始状态写进日志（排查"按住/松开"不生效时使用） |

单实例：同一时间只允许一个 SoundWheel 进程，重复启动会提示"SoundWheel 已经在运行，请在系统托盘中查看。"

---

## 托盘菜单

**打开轮盘** · **设置…** · **刷新音效列表** · **打开数据文件夹** · **退出**

（双击托盘图标 = 打开轮盘）

---

## 构建

不需要 Visual Studio / .NET SDK，只用到系统自带的 .NET Framework 4.x 编译器（C# 5）。

```
build.cmd
```

输出：

- `dist\SoundWheel.exe` —— 主程序（带图标，单文件，无外部依赖）
- `dist\MockSoundpad.exe` —— 开发用的假 Soundpad（见下）

等价 PowerShell 脚本 `build.ps1` 也可以，但需要允许脚本执行：

```powershell
Set-ExecutionPolicy -Scope Process Bypass -Force
.\build.ps1
```

源码结构：

| 文件 | 职责 |
| --- | --- |
| `src\Program.cs` | 入口、DPI 感知、命令行解析、单实例、预览/设置模式 |
| `src\App.cs` | 托盘、热键注册、播放调度、Soundpad 重连、开机自启、退出清理 |
| `src\WheelWindow.cs` | 轮盘窗口：分层窗口绘制、扇区几何、命中测试、动画、提交逻辑 |
| `src\SettingsForm.cs` | 设置窗口（四页 + 预览 + 保存） |
| `src\SoundpadClient.cs` | 命名管道客户端、`GetSoundlist` / `DoPlaySound` 命令、音效缓存 |
| `src\Hotkey.cs` | 热键数据结构、组合键显示名、热键输入框控件 |
| `src\Config.cs` | 配置模型（槽位、外观、播放选项）、JSON 读写、颜色工具 |
| `src\Json.cs` | 极简 JSON 解析/序列化（不引用外部库） |
| `src\Native.cs` | Win32 P/Invoke（热键、分层窗口、DPI、鼠标、管道） |
| `src\Toast.cs` | 播放提示条窗口 |
| `src\Log.cs` / `src\Paths.cs` | 日志与路径 |

---

## 开发自测：MockSoundpad

`dist\MockSoundpad.exe` 会模拟 Soundpad 的命名管道（`\\.\pipe\sp_remote_control`），返回 20 条假音效：

- `--log <文件>` 指定日志（默认 `%TEMP%\soundwheel-mock.log`），每次收到播放命令会写一行 `PLAY n`
- 配合 `--data examples\testdata` 这样的独立数据目录，就能在没有 Soundpad 的机器上完整验证：读列表、分配、播放、日志

```cmd
start dist\MockSoundpad.exe --log %TEMP%\mock.log
start dist\SoundWheel.exe --data examples\testdata
```

仓库里已附带一份 20 个假音效的示例配置（`examples\testdata\config.json`），内外圈都分配好了，开箱即可点着玩。

---

## 工作原理（简述）

- **播放**：Soundpad 没有提供插件 SDK，官方扩展点是 **Remote Control API** —— 命名管道 `\\.\pipe\sp_remote_control`，消息模式（message mode），UTF-8 文本命令，形如 `DoPlaySound(3, true, true)`、`GetSoundlist()`，返回如 `R-200` 或 XML 音效列表。SoundWheel 就是一个这样的客户端（也可用 `Soundpad.exe -rc <命令>` 手动验证）。
- **轮盘**：无边框分层窗口 + 32bpp 预乘位图 `UpdateLayeredWindow`，实现逐像素透明和抗锯齿；窗口只覆盖轮盘包围盒（不是全屏），并带 `WS_EX_NOACTIVATE | TOOLWINDOW | TOPMOST`，不会抢走游戏焦点。
- **DPI**：进程启用 Per-Monitor V2，所有几何按显示器 DPI 手动换算，150% / 200% 缩放下不会糊也不会错位。
- **热键**：`RegisterHotKey` + `MOD_NOREPEAT`；"是否还按着"用 `GetAsyncKeyState` 轮询（15ms），带 120ms 宽限与 6px 死区，避免"按下瞬间误播"。

---

## 故障排查

**热键按了没反应**
- 热键被别的程序占用（OBS、输入法、显卡驱动、Office 等）→ 设置里换一个组合键
- 前台程序以管理员权限运行，而本程序不是 → 让两者权限一致（都用管理员，或都不用）
- 看 `soundwheel.log` 里有没有 `热键已注册 ...` / `收到 WM_HOTKEY ...`

**轮盘出现了，但中心显示"点击播放"**
- 环境读不到按键状态，已自动降级为点按模式，用左键点选即可；如需排查可加 `--debug-keys` 看日志

**能打开，点击/松开后没有声音**
- 确认 Soundpad 正在运行；托盘 → 刷新音效列表，看音效库页是否有内容
- 看 `soundwheel.log` 里是否有 `播放失败` / `连接` 相关记录
- 语音软件里听不到 → 检查本程序是否勾选"发送到麦克风"，以及 Soundpad 的麦克风设备设置

**播放了错的声音**
- Soundpad 里音效顺序变了 → 托盘 刷新音效列表，或重新保存一次布局（按标题重新匹配）

**连接 Soundpad 失败**
- 必须让 Soundpad 先启动；若 Soundpad 卡死，重启它
- Soundpad 的远程控制日志在它的数据目录里（`%APPDATA%\Soundpad\...`），对照本程序日志里的命令即可确认

---

## 已知限制

- 只使用 Soundpad **All sounds** 分类的序号（这是 Remote Control API 的索引定义），不支持直接引用其他分类
- 内圈最多 12 格、外圈最多 16 格
- 同一时间只有一个配置（没有多套轮盘/多套配置文件）
- 只做"播放/停止"，不修改 Soundpad 本身、不做音频编辑
- 仅在 Windows 10/11 上测试

---

## 仓库结构

```
SoundWheel/
├─ src/            主程序源码（13 个 .cs，C# 5）
├─ tools/          MockSoundpad 假服务器、图标生成器、开发期探针
├─ assets/         图标与文档截图
├─ examples/       示例数据目录（可直接 --data 使用）
├─ .github/        issue 模板、PR 模板、CI 工作流
├─ build.cmd       一键构建（系统自带 csc，无需 SDK）
├─ build.ps1       同上（PowerShell 版）
└─ README.md
```

文档索引：`AI-DISCLOSURE.md`（AI 生成声明）· `CHANGELOG.md`（版本记录）· `CONTRIBUTING.md`（开发与提交流程）· `SECURITY.md`（安全边界）· `CODE_OF_CONDUCT.md`（行为准则）· `LICENSE`（MIT）。

---

## 贡献

欢迎 issue 与 PR。提 PR 前请先读 [CONTRIBUTING.md](CONTRIBUTING.md)，其中最重要的两条是：

1. **必须保持 C# 5 语法**（构建用的是 Windows 自带的 csc，不支持 `$""`、`?.`、`nameof`、表达式体成员等）
2. 不要提交 `dist/`、日志与截图

提交即表示你同意以 MIT 许可发布你的贡献。参与本项目请遵守 [行为准则](CODE_OF_CONDUCT.md)。

---

## 许可与致谢

以 [MIT](LICENSE) 许可发布，版权归 "SoundWheel contributors" 所有。

由于本项目内容主要由 AI 生成，各地法律对 AI 生成内容的版权认定不同；MIT 条款在上述权利存在的范围内适用，不额外限制你对这些内容的使用。详见 [AI-DISCLOSURE.md](AI-DISCLOSURE.md)。

SoundWheel 是第三方工具，与 Leppsoft / Soundpad 官方无关联、未获其背书；Soundpad 是其各自权利人的商标。感谢 Soundpad 提供的 Remote Control API。

本项目按"原样"提供，不附带任何担保 —— 打开音效前请先确认场合 🙂
