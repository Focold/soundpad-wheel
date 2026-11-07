# 安全策略

## 报告问题

如果你发现了安全问题，请**不要**直接开公开 issue。请通过以下任一方式私下联系维护者：

1. GitHub 的 **Security → Report a vulnerability**（推荐，仓库需开启 Private vulnerability reporting）
2. 仓库所有者公开的联系方式（见 GitHub 个人主页）

我们会在确认后尽快修复，并在 `CHANGELOG.md` 中致谢（如果你同意）。

## 安全边界

SoundWheel 是一个本地小工具，正常情况下：

- **不联网**：没有任何 HTTP 请求；只在必要时与本机 Soundpad 的命名管道通信
- **不改系统**：仅在你开启"登录时自动启动"时写入
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SoundWheel`
- **不改游戏/输入**：不安装全局钩子、不注入其他进程；热键用 `RegisterHotKey`，松开判定用 `GetAsyncKeyState` 轮询
- **数据**：配置、缓存与日志写在 `%APPDATA%\SoundWheel`（可用 `--data` 改），不收集、不上传任何信息

需要留意的点：

- 程序以你的用户权限运行；若以管理员运行，它能做的事情等同于管理员权限的任何程序
- 托盘菜单的"打开数据文件夹"会调用 `explorer.exe`
- 命名管道客户端只连接固定的 `sp_remote_control`，不创建服务端、不监听网络

## 支持范围

仅在 Windows 10/11 x64 上测试。其他平台/架构不受支持。
