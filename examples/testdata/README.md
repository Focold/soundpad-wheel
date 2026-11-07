# 示例数据目录

这份 `config.json` 与 `dist/MockSoundpad.exe` 返回的 20 条假音效一一对应：

| 环 | 槽位 | 音效 |
| --- | --- | --- |
| 内圈 | 1–8 | 开场音效、笑死我了、鼓掌、尴尬、胜利、失败、汪汪、喵喵 |
| 外圈 | 1–12 | 警报、倒计时、掌声、嘘声、叮、爆炸、鼓点、雨声、枪声、脚步、门铃、欢呼 |

用法（无需安装 Soundpad）：

```cmd
start dist\MockSoundpad.exe --log %TEMP%\mock.log
start dist\SoundWheel.exe --data examples\testdata
```

然后按住 `Ctrl + Alt + W` 选一个扇区，`%TEMP%\mock.log` 里会出现 `PLAY n`。

> 这是纯测试用的假数据：假音效名不会出现在真实 Soundpad 里，所以真实环境下请用设置窗口「刷新音效列表」后重新分配。
