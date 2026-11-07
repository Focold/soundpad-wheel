# 贡献指南

感谢你愿意帮忙！本项目由 AI 生成（见 [AI-DISCLOSURE.md](AI-DISCLOSURE.md)），欢迎人类接手改进。

## 环境

- Windows 10 / 11（x64）
- 任意文本编辑器
- **不需要** Visual Studio / .NET SDK：构建使用系统自带的 .NET Framework 4.x 编译器

```cmd
build.cmd
```

产物在 `dist/SoundWheel.exe` 与 `dist/MockSoundpad.exe`。

## 重要：语言版本限制

编译器是 .NET Framework 4.x 自带的 **C# 5**，请勿使用：

- 字符串插值 `$"..."`
- `?.` / `?[]`
- `nameof`
- 表达式体成员 `=> ...`
- 元组、`out var`、自动属性初始化器、`using static`
- `async`/`await` 之外的现代语法糖（await 本身可用）

保持 `/warn:4` 零警告。

## 代码风格

- 4 空格缩进，K&R 大括号，文件用 UTF-8（无 BOM 也可）
- 类内部字段 `_camelCase`，公开成员 PascalCase
- 不引入 NuGet 依赖，只用 .NET Framework 自带类库
- 新增 P/Invoke 放进 `src/Native.cs`，常量集中定义
- 用户可见的文案用中文；注释可中英混排，但保持简洁

## 开发与自测

1. 起假服务器：`dist\MockSoundpad.exe --log %TEMP%\mock.log`
2. 用独立数据目录启动：`dist\SoundWheel.exe --data examples\testdata`
3. 打开设置 → 轮盘布局 → 刷新音效列表（应看到 20 条假音效）
4. `%TEMP%\mock.log` 里应出现 `PLAY n`

方便调试的参数：`--preview [秒]`、`--settings`、`--data <目录>`、`--debug-keys`、`--debug-mouse`。

两个只在开发期使用的环境变量（**不要写进用户文档**）：

- `SOUNDWHEEL_DEBUG_MOUSE=1` —— 等价于 `--debug-mouse`
- `SOUNDWHEEL_FAKE_RAW=1` —— 打开轮盘后 700 ms 内每帧注入合成位移 `OnRawMouse(5, -7)`，
  用于在没有真实原始输入事件的环境里验证相对位移 → 命中 → 高亮 → 提交的整条链路

验证相对模式的一条捷径：以 `SOUNDWHEEL_FAKE_RAW=1` 启动 → 用 `FindWindow(null, "SoundWheelRawMouse")` 找到原始输入窗口 →
`PostMessage(hwnd, WM_INPUT, 0, 0)` 确认消息链路 → 对轮盘窗口 `PostMessage(hwnd, WM_LBUTTONUP, 0, 0)` 确认提交（日志里应有 `rel=True`）。

## 开发工具（`tools/`）

- `MockSoundpad.cs` —— 假 Soundpad 命名管道服务器，打印收到的命令（自测必用）
- `MouseLock.cs` —— 把光标锁在屏幕中心并注入相对位移，用来手工复现 FPS / 指针锁定场景
- `MakeIcon.cs` —— 用 GDI+ 程序化生成 `assets/app.ico`（多尺寸 DIB 条目）

注意：原始输入的对照探针（旧 `RawProbe`）已被删除——它会触发部分杀毒软件的启发式拦截，
现在改用产品内置的诊断（`--debug-mouse` 与 `SOUNDWHEEL_FAKE_RAW`）。

## 提交 PR

- 一个 PR 只做一件事，附上"为什么"和验证方式（截图/日志）
- UI 改动请附 100% 与 150% 缩放下的截图
- 不要提交 `dist/`、日志、截图等构建产物（已在 `.gitignore`）
- 提交信息用祈使句，例如 `fix: 修正 150% DPI 下按钮被裁切`
- 提交即表示你同意以 MIT 许可发布你的贡献

## 报告 issue

用 `.github/ISSUE_TEMPLATE` 中的模板，并尽量附上：

- Windows 版本与缩放比例、Soundpad 版本
- `soundwheel.log` 中相关片段（注意隐去个人信息）
- 复现步骤与期望行为
