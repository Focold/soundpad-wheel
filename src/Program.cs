// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SoundWheel
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Native.EnableDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                Log.Error("未处理异常", e.Exception);
            };

            bool debugKeys = false;
            bool debugMouse = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--data" && i + 1 < args.Length) { AppPaths.Override = args[i + 1]; i++; }
                else if (args[i] == "--debug-keys") debugKeys = true;
                else if (args[i] == "--debug-mouse") debugMouse = true;
            }
            WheelWindow.KeyDebug = debugKeys || Environment.GetEnvironmentVariable("SOUNDWHEEL_DEBUG_KEYS") == "1";
            WheelWindow.MouseDebug = debugMouse || Environment.GetEnvironmentVariable("SOUNDWHEEL_DEBUG_MOUSE") == "1";
            WheelWindow.FakeRaw = Environment.GetEnvironmentVariable("SOUNDWHEEL_FAKE_RAW") == "1";

            Log.FilePath = AppPaths.LogFile;
            Log.Info("启动 " + string.Join(" ", args));
            Log.Info("数据目录 " + AppPaths.DataDir + (AppPaths.Note.Length > 0 ? "（" + AppPaths.Note + "）" : ""));

            int previewSeconds = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--preview")
                {
                    previewSeconds = 6;
                    if (i + 1 < args.Length) int.TryParse(args[i + 1], out previewSeconds);
                }
            }
            if (previewSeconds > 0)
            {
                Application.Run(new PreviewContext(Math.Max(2, previewSeconds)));
                return;
            }

            bool showSettings = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--settings") showSettings = true;
            }
            if (showSettings)
            {
                Application.Run(new SettingsContext());
                return;
            }

            bool created;
            using (Mutex mutex = new Mutex(true, "SoundWheel.SingleInstance", out created))
            {
                if (!created)
                {
                    MessageBox.Show("SoundWheel 已经在运行，请在系统托盘中查看。", "SoundWheel",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using (App app = new App())
                {
                    Application.Run(app);
                }
            }
        }
    }

    internal sealed class SettingsContext : ApplicationContext
    {
        private readonly AppConfig _config;
        private readonly SoundpadClient _client;
        private readonly SettingsForm _form;

        public SettingsContext()
        {
            _config = AppConfig.Load(AppPaths.ConfigFile);
            _client = new SoundpadClient();
            _form = new SettingsForm(_config, _client);
            _form.FormClosed += delegate
            {
                if (_form.DialogResult == DialogResult.OK)
                {
                    _form.ApplyTo(_config);
                    try { _config.Save(AppPaths.ConfigFile); Log.Info("配置已保存（设置窗口）"); }
                    catch (Exception ex) { Log.Error("保存配置失败", ex); }
                }
                ExitThread();
            };
            _form.Show();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class PreviewContext : ApplicationContext
    {
        private readonly WheelWindow _wheel;
        private readonly System.Windows.Forms.Timer _timer;

        public PreviewContext(int seconds)
        {
            AppConfig config = AppConfig.Load(AppPaths.ConfigFile);
            _wheel = new WheelWindow(config);
            _wheel.PlayRequested += delegate(WheelItem item) { Log.Info("预览选择: " + item.Title); };

            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            _wheel.OpenAt(new Point(bounds.Width / 2, bounds.Height / 2), false, true, seconds);

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = Math.Max(2, seconds) * 1000 + 300;
            _timer.Tick += delegate { _timer.Stop(); ExitThread(); };
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                _wheel.CloseNow();
                _wheel.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
