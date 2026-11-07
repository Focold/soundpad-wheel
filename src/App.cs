// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SoundWheel
{
    internal sealed class HotkeyWindow : NativeWindow
    {
        public event Action Pressed;

        public HotkeyWindow()
        {
            CreateParams cp = new CreateParams();
            cp.Caption = "SoundWheelHotkey";
            cp.Parent = new IntPtr(-3);
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                Log.Info("收到 WM_HOTKEY id=" + m.WParam + " vk=" + m.LParam);
                Action handler = Pressed;
                if (handler != null) handler();
                return;
            }
            base.WndProc(ref m);
        }

        public void Destroy()
        {
            if (Handle != IntPtr.Zero) DestroyHandle();
        }
    }

    internal sealed class App : ApplicationContext, IDisposable
    {
        private const int HotkeyId = 0xB10;
        private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

        private readonly AppConfig _config;
        private readonly SoundpadClient _client = new SoundpadClient();
        private readonly WheelWindow _wheel;
        private readonly NotifyIcon _tray;
        private readonly HotkeyWindow _hotkeyWindow;
        private bool _hotkeyRegistered;
        private bool _shutdown;
        private Icon _icon;

        public App()
        {
            _config = AppConfig.Load(AppPaths.ConfigFile);
            _client.Sounds = SoundCache.Load(AppPaths.SoundsFile);

            _wheel = new WheelWindow(_config);
            _wheel.PlayRequested += OnPlayRequested;

            _hotkeyWindow = new HotkeyWindow();
            _hotkeyWindow.Pressed += OnHotkeyPressed;

            _tray = new NotifyIcon();
            _tray.Text = "SoundWheel";
            _icon = BuildIcon();
            _tray.Icon = _icon;
            _tray.Visible = true;
            _tray.DoubleClick += delegate { OpenWheel(false, false, 0); };
            _tray.ContextMenuStrip = BuildMenu();

            RegisterHotkey();
            if (_config.AutoStart) ApplyAutoStart();
            Log.Info("App 启动，音效缓存 " + _client.Sounds.Length + " 条");
            Log.Info("环境 " + Native.EnvironmentInfo());

        }

        private ContextMenuStrip BuildMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            ToolStripMenuItem open = new ToolStripMenuItem("打开轮盘");
            open.Click += delegate { OpenWheel(false, false, 0); };

            ToolStripMenuItem settings = new ToolStripMenuItem("设置…");
            settings.Click += OnSettings;

            ToolStripMenuItem refresh = new ToolStripMenuItem("刷新音效列表");
            refresh.Click += OnRefresh;

            ToolStripMenuItem folder = new ToolStripMenuItem("打开数据文件夹");
            folder.Click += delegate
            {
                try { System.Diagnostics.Process.Start("explorer.exe", "\"" + AppPaths.DataDir + "\""); }
                catch (Exception ex) { Log.Error("打开数据文件夹失败", ex); }
            };

            ToolStripMenuItem exit = new ToolStripMenuItem("退出");
            exit.Click += delegate { Shutdown(); ExitThread(); };

            menu.Items.Add(open);
            menu.Items.Add(settings);
            menu.Items.Add(refresh);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(folder);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exit);
            return menu;
        }

        private static Icon BuildIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    Color accent = Color.FromArgb(91, 140, 255);
                    using (SolidBrush halo = new SolidBrush(Color.FromArgb(70, accent)))
                        g.FillEllipse(halo, 1, 1, 30, 30);
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4.0;
                        float x = 16f + (float)(Math.Cos(angle) * 10.5);
                        float y = 16f + (float)(Math.Sin(angle) * 10.5);
                        using (SolidBrush brush = new SolidBrush(i % 2 == 0 ? Color.FromArgb(235, 240, 248) : accent))
                            g.FillEllipse(brush, x - 3.1f, y - 3.1f, 6.2f, 6.2f);
                    }
                    using (SolidBrush center = new SolidBrush(Color.FromArgb(24, 28, 38)))
                        g.FillEllipse(center, 11, 11, 10, 10);
                    using (Pen ring = new Pen(accent, 1.4f))
                        g.DrawEllipse(ring, 11, 11, 10, 10);
                }
                IntPtr handle = bmp.GetHicon();
                return Icon.FromHandle(handle);
            }
        }

        private void RegisterHotkey()
        {
            UnregisterHotkey();
            HotkeySpec spec = _config.Hotkey;
            if (spec == null || !spec.IsValid) return;
            _hotkeyRegistered = Native.RegisterHotKey(_hotkeyWindow.Handle, HotkeyId,
                spec.Modifiers | Native.MOD_NOREPEAT, (uint)spec.VirtualKey);
            if (!_hotkeyRegistered)
            {
                Log.Info("注册热键失败: " + spec.Display);
                Balloon("快捷键 " + spec.Display + " 被其他程序占用，请在设置中更换。");
            }
            else
            {
                Log.Info("热键已注册 " + spec.Display + " hwnd=" + _hotkeyWindow.Handle);
            }
        }

        private void UnregisterHotkey()
        {
            if (!_hotkeyRegistered) return;
            Native.UnregisterHotKey(_hotkeyWindow.Handle, HotkeyId);
            _hotkeyRegistered = false;
        }

        private void OnHotkeyPressed()
        {
            if (_wheel.IsOpen)
            {
                if (_config.ToggleMode) _wheel.CloseNow();
                return;
            }
            OpenWheel(!_config.ToggleMode, false, 0);
        }

        private void OpenWheel(bool hold, bool preview, int previewSeconds)
        {
            if (_wheel.IsOpen) return;
            Native.POINT pt;
            Native.GetCursorPos(out pt);
            _wheel.OpenAt(new Point(pt.X, pt.Y), hold, preview, previewSeconds);
        }

        private void OnPlayRequested(WheelItem item)
        {
            Thread thread = new Thread(delegate() { PlayItem(item); });
            thread.IsBackground = true;
            thread.Start();
        }

        private void PlayItem(WheelItem item)
        {
            SoundEntry target = Resolve(item);
            if (target == null)
            {
                Log.Info("未找到音效: " + item.Title);
                ShowToast("未找到音效：" + item.Title + "（请刷新列表）", Color.FromArgb(214, 86, 86));
                return;
            }
            string error;
            bool ok = _client.Play(target.Index, _config.PlayToSpeaker, _config.PlayToMic, out error);
            if (!ok)
            {
                Log.Info("播放失败 " + target.Index + ": " + error);
                if (_config.ShowToast) ShowToast("播放失败：" + error, Color.FromArgb(214, 86, 86));
                return;
            }
            Log.Info("播放 " + target.Index + " · " + target.Title);
            if (_config.ShowToast)
            {
                string title = string.IsNullOrEmpty(target.Title) ? target.Url : target.Title;
                ShowToast("▶  " + title, _config.AccentColor);
            }
        }

        private SoundEntry Resolve(WheelItem item)
        {
            if (!string.IsNullOrEmpty(item.Title))
            {
                foreach (SoundEntry entry in _client.Sounds)
                    if (entry.Title == item.Title) return entry;
            }
            if (item.Index > 0)
            {
                foreach (SoundEntry entry in _client.Sounds)
                    if (entry.Index == item.Index) return entry;
                SoundEntry fallback = new SoundEntry();
                fallback.Index = item.Index;
                fallback.Title = item.Title;
                return fallback;
            }
            return null;
        }

        private void ShowToast(string text, Color color)
        {
            try
            {
                _wheel.BeginInvoke((MethodInvoker)delegate
                {
                    Native.POINT pt;
                    Native.GetCursorPos(out pt);
                    Toast.Show(text, new Point(pt.X, pt.Y), color);
                });
            }
            catch (Exception ex)
            {
                Log.Error("提示调度失败", ex);
            }
        }

        private void Balloon(string text)
        {
            try
            {
                _tray.BalloonTipTitle = "SoundWheel";
                _tray.BalloonTipText = text;
                _tray.ShowBalloonTip(4000);
            }
            catch (Exception) { }
        }

        private void OnSettings(object sender, EventArgs e)
        {
            _wheel.CloseNow();
            UnregisterHotkey();
            using (SettingsForm form = new SettingsForm(_config, _client))
            {
                form.PreviewRequested += delegate { OpenWheel(false, true, 3); };
                if (form.ShowDialog() == DialogResult.OK)
                {
                    form.ApplyTo(_config);
                    try { _config.Save(AppPaths.ConfigFile); }
                    catch (Exception ex) { Log.Error("保存配置失败", ex); }
                    ApplyAutoStart();
                    Log.Info("配置已保存，热键 " + _config.Hotkey.Display);
                }
            }
            RegisterHotkey();
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            Thread thread = new Thread(delegate()
            {
                SoundEntry[] list = _client.FetchSounds(4000);
                if (list == null)
                {
                    string error = string.IsNullOrEmpty(_client.LastError) ? "未知错误" : _client.LastError;
                    ShowToast("连接 Soundpad 失败：" + error, Color.FromArgb(214, 86, 86));
                    return;
                }
                SoundCache.Save(AppPaths.SoundsFile, list);
                ShowToast("已连接 Soundpad · " + list.Length + " 个音效", _config.AccentColor);
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void ApplyAutoStart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return;
                    object current = key.GetValue("SoundWheel");
                    string desired = "\"" + Application.ExecutablePath + "\"";
                    if (_config.AutoStart)
                    {
                        if (current == null || !string.Equals((string)current, desired))
                            key.SetValue("SoundWheel", desired);
                    }
                    else if (current != null)
                    {
                        key.DeleteValue("SoundWheel", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("设置开机启动失败", ex);
            }
        }

        private void Shutdown()
        {
            if (_shutdown) return;
            _shutdown = true;
            UnregisterHotkey();
            try { _wheel.CloseNow(); _wheel.Dispose(); } catch (Exception) { }
            try { _client.Dispose(); } catch (Exception) { }
            try { _tray.Visible = false; _tray.Dispose(); } catch (Exception) { }
            try { _hotkeyWindow.Destroy(); } catch (Exception) { }
            Log.Info("退出");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Shutdown();
            base.Dispose(disposing);
        }
    }
}
