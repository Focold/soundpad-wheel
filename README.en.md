# SoundWheel — a sound wheel for Soundpad

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![AI Generated](https://img.shields.io/badge/content-AI--generated-8A2BE2.svg)](AI-DISCLOSURE.md)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-lightgrey.svg)](#requirements)

> [!IMPORTANT]
> **AI-generated project.** Almost everything here — the C# sources, scripts, docs and the icon — was produced by an **AI coding agent** (DeepSeek Harness, model `deepseek-flash`) in a single session on 2026-11-07. A human supplied the requirements, the test machine and the verification. See **[AI-DISCLOSURE.md](AI-DISCLOSURE.md)** for scope, validation and disclaimers.

[中文说明](README.md)

![Wheel](assets/wheel.png)

Hold a hotkey, a ring of sounds appears at the cursor, flick the mouse, release — the sound plays.
The inner ring holds 8 favourites, the outer ring 12 more: 20 sounds without memorising 20 hotkeys.

SoundWheel is a standalone single-file `.exe` (no runtime install). It drives Soundpad through the official
**Remote Control API** (named pipe + text commands), so Soundpad itself is never modified.

---

## Features

- **Hotkey summon** — default `Ctrl + Alt + W`, fully rebindable; the wheel opens at the cursor
- **Two rings** — inner 1–12 slots, outer 0–16 slots, assigned independently
- **Two modes** — hold-to-play (release = play) or click-to-play
- **Cancel with right click / Esc**
- **FPS / first-person friendly** — when a game hides the cursor or clips it to the screen centre, the wheel switches to **Raw Input relative movement** and still works (see [Using it in FPS games](#using-it-in-fps--first-person-games))
- **Hover feedback** — highlighted sector, glow, sound name in the centre hub
- **Playback target** — microphone only / speakers only / both
- **Tray resident** — open wheel, settings, refresh list, open data folder, quit
- **Appearance** — overall scale 70–160 %, accent colour, slot counts, animations
- **Optional run at login** (per-user `HKCU\...\Run` entry, no admin rights)
- **Optional toast** after playback

---

## Requirements

| Item | Requirement |
| --- | --- |
| OS | Windows 10 / 11 (x64) |
| Soundpad | Installed **and running** |
| Runtime | None — uses the built-in .NET Framework 4.x |
| Privileges | Standard user. If Soundpad runs elevated, run SoundWheel elevated too (UIPI) |

Soundpad's remote control is enabled by default; no option has to be ticked.

---

## Quick start

1. Run `SoundWheel.exe` (download from Releases, or build it yourself with `build.cmd`); a wheel icon appears in the tray
2. Right-click the tray icon → **设置…** (Settings)
3. Go to the **轮盘布局** (Wheel layout) tab → **刷新音效列表** (Refresh) → assign sounds to inner/outer slots → **保存** (Save)
4. Back in your game, hold `Ctrl + Alt + W`, move to a sector, release

![Settings](assets/settings.png)

> The UI is currently Chinese-only. The five tabs are: General · Wheel layout · Sound library · Game & mouse · About.
> Bottom bar: **Preview wheel** · **Save** · **Cancel**.

---

## Using it in FPS / first-person games

Many first-person games **hide the system cursor**, **lock the pointer to the middle of the screen** (`ClipCursor`)
and rotate the camera from raw input. A wheel that simply reads the cursor position would sit frozen in the centre.

SoundWheel creates a second, 1×1 hidden window that registers for `WM_INPUT` and accumulates the raw mouse deltas
into a virtual pointer.

- **Tracking mode** (Settings → **游戏与鼠标** / Game & mouse)
  - **Auto** (default) — within 120–180 ms of opening it checks whether the cursor is hidden, clipped to a tiny
    rectangle, or whether raw movement arrives, and switches to relative mode when the game is in control
  - **Absolute** — cursor position only (ordinary desktop apps)
  - **Relative** — raw input only (FPS / pointer-locked games)
- **Sensitivity** 0.2–4.0 — the virtual pointer travels further per mouse count
- **Self-check** — press **检测鼠标状态** while the wheel is open in the game to see whether the cursor is hidden or clipped

![Game & mouse settings](assets/settings-mouse.png)

Trade-offs:

1. In relative mode the **in-game camera still turns** — the app only listens to raw input, it cannot block the
   game. Prefer the **hold mode** (hold hotkey → flick → release) instead of left-clicking, so you never fire by accident.
2. Some games / anti-cheat stacks block raw input completely; then the pointer may not move at all. Try windowed or
   borderless mode, click-to-play, or absolute mode.
3. If no raw input event arrives within 1.6 s, the log records a warning to make this easy to diagnose.

---

## How it works

- **Playback** — Soundpad ships no plugin SDK; its official extension point is the **Remote Control API**:
  named pipe `\\.\pipe\sp_remote_control`, message mode, UTF-8 text commands such as
  `DoPlaySound(3, true, true)` and `GetSoundlist()`, answered with `R-200` and an XML sound list.
- **Wheel** — a borderless layered window rendered into a 32bpp premultiplied bitmap via `UpdateLayeredWindow`
  (per-pixel transparency + antialiasing), covering only the wheel's bounding box, with
  `WS_EX_NOACTIVATE | TOOLWINDOW | TOPMOST` so it never steals focus from the game.
- **DPI** — Per-Monitor V2 aware; all geometry scaled by the monitor DPI (100 %/150 %/200 %).
- **Hotkey** — `RegisterHotKey` + `MOD_NOREPEAT`; "is it still held?" is polled with `GetAsyncKeyState`
  (15 ms) with a 120 ms grace period and a 6 px dead zone, so the wheel never plays something by accident.
- **Raw input** — a separate hidden window registers `RegisterRawInputDevices` (`RIDEV_INPUTSINK`) and turns `WM_INPUT`
  `lLastX/lLastY` into the virtual pointer. That window must **not** carry `WS_EX_NOACTIVATE` — with that style it
  receives no `WM_INPUT` at all (measured on Windows 11). The wheel itself stays `NOACTIVATE` so focus is never stolen.
  The virtual pointer is clamped to the outermost ring radius so it can always hit a sector.
- **Drift-proof slots** — each slot stores `title` + `index`; on play the title is resolved against the live
  sound list and only falls back to the stored index.

---

## Data & configuration

Default folder: `%APPDATA%\SoundWheel\`

| File | Purpose |
| --- | --- |
| `config.json` | hotkey, slot assignments, appearance, playback options |
| `sounds.json` | cache of the last sound list read from Soundpad |
| `soundwheel.log` | runtime log — check this first when troubleshooting |

Override with `--data <dir>` or the `SOUNDWHEEL_DATA` environment variable (portable use).
If `%APPDATA%` is not writable the app falls back to `%LOCALAPPDATA%\SoundWheel` → `<exe dir>\SoundWheelData`
→ `%TEMP%\SoundWheel`, and states the chosen folder in the first log line.

---

## Command line

```
SoundWheel.exe [--data <dir>] [--preview [seconds]] [--settings] [--debug-keys] [--debug-mouse]
```

| Option | Meaning |
| --- | --- |
| `--preview [seconds]` | Skip the hotkey, open the wheel in the screen centre (default 6 s) — handy for screenshots |
| `--settings` | Open the settings window directly |
| `--data <dir>` | Data folder (takes precedence over `SOUNDWHEEL_DATA`) |
| `--debug-keys` | Log raw key states (diagnose hold/release problems) |
| `--debug-mouse` | Log every raw mouse delta and tracking-mode switch (diagnose a frozen pointer in games) |

Only one instance may run at a time.

---

## Build

No Visual Studio / .NET SDK needed — only the C# 5 compiler bundled with the .NET Framework:

```cmd
build.cmd
```

Outputs `dist\SoundWheel.exe` (single file, icon embedded) and `dist\MockSoundpad.exe` (a fake Soundpad for
development). A PowerShell equivalent exists as `build.ps1` (requires
`Set-ExecutionPolicy -Scope Process Bypass`).

**Language constraint:** the build must stay C# 5 compatible — no string interpolation, `?.`, `nameof`,
expression-bodied members, tuples, `out var` or auto-property initialisers.

---

## Self-test without Soundpad

```cmd
start dist\MockSoundpad.exe --log %TEMP%\mock.log
start dist\SoundWheel.exe --data examples\testdata
```

`MockSoundpad` serves the same named pipe with 20 fake sounds and logs `PLAY n` for every play command.
A ready-made example configuration is included in `examples\testdata`.

---

## Troubleshooting

- **Hotkey does nothing** — another app owns it (OBS, IME, GPU drivers…); pick another combo.
  Or the foreground app is elevated while SoundWheel is not (match the privilege level).
- **Wheel says "click to play" instead of "release to play"** — the key state could not be read in this
  environment (RDP, exclusive fullscreen, input-hooked games). The wheel automatically degrades to
  click-to-play, so it never gets stuck.
- **Nothing audible** — is Soundpad running? Refresh the sound list; check `soundwheel.log`;
  check the "send to microphone" option and Soundpad's own device settings.
- **Wrong sound plays** — the order changed inside Soundpad; refresh the list (matching is title-first).
- **Wheel opens in a game but the pointer will not move** — Settings → **Game & mouse** → **检测鼠标状态** to see
  whether the cursor is hidden or clipped; switch tracking to **Relative**; try windowed/borderless mode; run with
  `--debug-mouse` and check the log for the "no raw input within 1.6 s" warning.

---

## Known limitations

- Only the **All sounds** category index is used (that is what the Remote Control API exposes)
- Max 12 inner + 16 outer slots, one configuration at a time
- Relative mode only listens to raw input — it cannot stop the in-game camera from turning; if anti-cheat blocks
  raw input entirely the pointer cannot be tracked
- The wheel does not capture the mouse itself; the pointer is derived from polling/raw input, so a badly stuttering
  game may lag it by a frame or two
- Playback/stop only — it never edits audio or Soundpad's own data
- UI is Chinese-only; tested on Windows 11 at 150 % DPI

---

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) first. The two rules that matter: keep it **C# 5** and never commit
`dist/`. By submitting a PR you agree to license your contribution under MIT. Be kind:
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Security issues: [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE). Because most of this repository is AI-generated, copyright may not subsist in every
jurisdiction; the MIT grant applies to the extent such rights exist. Full disclosure:
[AI-DISCLOSURE.md](AI-DISCLOSURE.md).

SoundWheel is an unofficial third-party tool, not affiliated with or endorsed by Leppsoft / Soundpad.
Soundpad is a trademark of its respective owner.

Provided **as is**, without warranty of any kind.
