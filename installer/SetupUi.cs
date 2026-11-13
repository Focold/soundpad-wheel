// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SoundWheelSetup
{
    internal sealed class Bar : Control
    {
        private float _value;

        public Bar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.FromArgb(30, 34, 43);
            _value = 0f;
        }

        public float Value
        {
            get { return _value; }
            set
            {
                if (value < 0f) value = 0f;
                if (value > 1f) value = 1f;
                _value = value;
                Invalidate();
            }
        }

        public Color Track = Color.FromArgb(46, 52, 64);
        public Color Fill = Color.FromArgb(91, 140, 255);
        public float Radius = 3f;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF track = new RectangleF(0.5f, Height / 2f - 2.5f, Width - 1f, 5f);
            using (GraphicsPath p = Round(track, Radius))
            using (SolidBrush b = new SolidBrush(Track)) g.FillPath(b, p);
            float w = (Width - 1f) * _value;
            if (w > 4f)
            {
                RectangleF filled = new RectangleF(0.5f, Height / 2f - 2.5f, w, 5f);
                using (GraphicsPath p = Round(filled, Radius))
                using (SolidBrush b = new SolidBrush(Fill)) g.FillPath(b, p);
            }
        }

        private static GraphicsPath Round(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0.5f) { p.AddRectangle(r); return p; }
            float d = radius * 2f;
            p.AddArc(r.X, r.Y, d, d, 180f, 90f);
            p.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            p.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
            p.CloseFigure();
            return p;
        }
    }

    internal sealed class CheckBoxAdv : Control
    {
        private bool _checked;
        private bool _hover;

        public float UiScale = 1f;
        public Color Accent = Color.FromArgb(91, 140, 255);
        public Color BoxBack = Color.FromArgb(25, 29, 37);
        public Color BoxBorder = Color.FromArgb(70, 78, 94);
        public Color TextColor = Color.FromArgb(238, 241, 247);

        public CheckBoxAdv()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            ForeColor = TextColor;
            Cursor = Cursors.Hand;
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = UiScale;
            float side = 17f * s;
            float x = 1f * s;
            float y = (Height - side) / 2f;
            RectangleF box = new RectangleF(x, y, side, side);

            using (GraphicsPath path = RoundedRect(box, 4.5f * s))
            {
                using (SolidBrush b = new SolidBrush(_checked ? Accent : BoxBack)) g.FillPath(b, path);
                Color bc = _checked ? Accent : (_hover ? Color.FromArgb(110, 120, 140) : BoxBorder);
                using (Pen p = new Pen(bc, 1.4f * s)) g.DrawPath(p, path);
            }

            if (_checked)
            {
                using (Pen p = new Pen(Color.White, 2f * s))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    PointF a = new PointF(x + side * 0.26f, y + side * 0.52f);
                    PointF b = new PointF(x + side * 0.44f, y + side * 0.70f);
                    PointF c = new PointF(x + side * 0.76f, y + side * 0.31f);
                    g.DrawLines(p, new PointF[] { a, b, c });
                }
            }

            RectangleF tr = new RectangleF(x + side + 9f * s, 0f, Width - side - 12f * s, Height);
            using (StringFormat sf = new StringFormat())
            {
                sf.LineAlignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                using (SolidBrush b = new SolidBrush(ForeColor)) g.DrawString(Text, Font, b, tr, sf);
            }
        }

        private static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = radius * 2f;
            p.AddArc(r.X, r.Y, d, d, 180f, 90f);
            p.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            p.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
            p.CloseFigure();
            return p;
        }
    }

    internal sealed class InstallerForm : Form
    {
        private static readonly Color Bg = Color.FromArgb(20, 22, 28);
        private static readonly Color Card = Color.FromArgb(30, 34, 43);
        private static readonly Color BorderColor = Color.FromArgb(52, 58, 70);
        private static readonly Color TextColor = Color.FromArgb(238, 241, 247);
        private static readonly Color Dim = Color.FromArgb(158, 166, 180);
        private static readonly Color Accent = Color.FromArgb(91, 140, 255);
        private static readonly Color Danger = Color.FromArgb(226, 110, 118);

        private readonly string _dir;
        private readonly bool _uninstallMode;
        private readonly float _s;

        private Label _title;
        private Label _close;
        private Label _lang;
        private Label _subtitle;
        private Label _installedLabel;
        private Label _latestLabel;
        private Label _pathLabel;
        private Label _hint;
        private CheckBoxAdv _launch;
        private CheckBoxAdv _desktop;
        private CheckBoxAdv _removeData;
        private Button _primary;
        private Button _check;
        private Button _cancel;
        private Bar _bar;

        private Version _installed;
        private ReleaseInfo _latest;
        private bool _busy;
        private bool _latestChecked;

        public InstallerForm(string dir, bool uninstallMode)
        {
            _dir = dir;
            _uninstallMode = uninstallMode;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) _s = Math.Max(1f, g.DpiX / 96f);

            Text = "SoundWheel Setup";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg;
            DoubleBuffered = true;
            ClientSize = new Size(S(640), S(430));
            ShowInTaskbar = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch (Exception) { }

            BuildUi();
            RebuildTexts();
            Load += delegate { OnLoaded(); };
        }

        private int S(int v) { return (int)Math.Round(v * _s); }

        private void BuildUi()
        {
            _title = MakeLabel("SoundWheel", 21f, FontStyle.Bold, TextColor);
            _title.Bounds = new Rectangle(S(30), S(22), S(320), S(36));
            Controls.Add(_title);

            _close = MakeLabel("✕", 12f, FontStyle.Regular, Dim);
            _close.TextAlign = ContentAlignment.MiddleCenter;
            _close.Bounds = new Rectangle(S(600), S(20), S(24), S(24));
            _close.Cursor = Cursors.Hand;
            _close.Click += delegate { Close(); };
            Controls.Add(_close);

            _lang = MakeLabel("English", 9f, FontStyle.Regular, Dim);
            _lang.TextAlign = ContentAlignment.MiddleRight;
            _lang.Bounds = new Rectangle(S(470), S(24), S(112), S(20));
            _lang.Cursor = Cursors.Hand;
            _lang.Click += delegate
            {
                L.English = !L.English;
                RebuildTexts();
            };
            Controls.Add(_lang);

            _subtitle = MakeLabel("", 9.5f, FontStyle.Regular, Dim);
            _subtitle.Bounds = new Rectangle(S(30), S(62), S(580), S(22));
            Controls.Add(_subtitle);

            _installedLabel = MakeLabel("", 10f, FontStyle.Regular, TextColor);
            _installedLabel.Bounds = new Rectangle(S(52), S(112), S(540), S(22));
            Controls.Add(_installedLabel);

            _latestLabel = MakeLabel("", 10f, FontStyle.Regular, TextColor);
            _latestLabel.Bounds = new Rectangle(S(52), S(142), S(540), S(22));
            Controls.Add(_latestLabel);

            _pathLabel = MakeLabel("", 9f, FontStyle.Regular, Dim);
            _pathLabel.Bounds = new Rectangle(S(52), S(170), S(536), S(22));
            _pathLabel.AutoEllipsis = true;
            Controls.Add(_pathLabel);

            _bar = new Bar();
            _bar.Bounds = new Rectangle(S(52), S(206), S(536), S(8));
            _bar.Visible = false;
            Controls.Add(_bar);

            _hint = MakeLabel("", 9f, FontStyle.Regular, Dim);
            _hint.Bounds = new Rectangle(S(52), S(222), S(536), S(22));
            Controls.Add(_hint);

            _launch = MakeCheck("", S(52), S(262));
            _launch.Checked = true;
            Controls.Add(_launch);

            _desktop = MakeCheck("", S(52), S(292));
            _desktop.Checked = true;
            Controls.Add(_desktop);

            _removeData = MakeCheck("", S(52), S(262));
            Controls.Add(_removeData);

            _primary = MakeButton("", Accent, Color.White);
            _primary.Bounds = new Rectangle(S(400), S(366), S(210), S(40));
            RoundButton(_primary, 10f);
            _primary.Click += OnPrimary;
            Controls.Add(_primary);

            _check = MakeButton("", Color.FromArgb(44, 50, 63), TextColor);
            _check.Bounds = new Rectangle(S(248), S(366), S(140), S(40));
            RoundButton(_check, 10f);
            _check.Click += OnCheckUpdate;
            Controls.Add(_check);

            _cancel = MakeButton("", Color.FromArgb(34, 38, 47), Dim);
            _cancel.Bounds = new Rectangle(S(150), S(366), S(88), S(40));
            RoundButton(_cancel, 10f);
            _cancel.Click += delegate { Close(); };
            Controls.Add(_cancel);
        }

        private void RoundButton(Control c, float radius)
        {
            using (GraphicsPath path = RoundPath(new RectangleF(0f, 0f, c.Width, c.Height), radius * _s))
            {
                c.Region = new Region(path);
            }
        }

        private static GraphicsPath RoundPath(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = radius * 2f;
            p.AddArc(r.X, r.Y, d, d, 180f, 90f);
            p.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            p.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
            p.CloseFigure();
            return p;
        }

        private Label MakeLabel(string text, float size, FontStyle style, Color color)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point);
            l.ForeColor = color;
            l.BackColor = Color.Transparent;
            l.AutoSize = false;
            return l;
        }

        private CheckBoxAdv MakeCheck(string text, int x, int y)
        {
            CheckBoxAdv c = new CheckBoxAdv();
            c.Text = text;
            c.UiScale = _s;
            c.Accent = Accent;
            c.ForeColor = TextColor;
            c.Bounds = new Rectangle(x, y, S(540), S(24));
            return c;
        }

        private Button MakeButton(string text, Color back, Color fore)
        {
            Button b = new Button();
            b.Text = text;
            b.Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
            b.BackColor = back;
            b.ForeColor = fore;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Lighten(back, 18);
            b.FlatAppearance.MouseDownBackColor = Lighten(back, -10);
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            return b;
        }

        private static Color Lighten(Color c, int delta)
        {
            int r = Math.Max(0, Math.Min(255, c.R + delta));
            int g = Math.Max(0, Math.Min(255, c.G + delta));
            int b = Math.Max(0, Math.Min(255, c.B + delta));
            return Color.FromArgb(c.A, r, g, b);
        }

        private void RebuildTexts()
        {
            _lang.Text = L.English ? "中文" : "English";
            _title.Text = "SoundWheel";
            _subtitle.Text = _uninstallMode
                ? L.T("卸载 SoundWheel 音效轮盘", "Uninstall SoundWheel")
                : L.T("安装 / 更新 SoundWheel 音效轮盘", "Install / update SoundWheel");

            _installedLabel.Text = _installed == null
                ? L.T("尚未安装", "Not installed")
                : L.T("已安装版本  ", "Installed  ") + _installed.ToString(3);
            _latestLabel.Text = _latestChecked
                ? (_latest == null
                    ? L.T("最新版本  检查失败（无法连接 GitHub）", "Latest  check failed (cannot reach GitHub)")
                    : L.T("最新版本  ", "Latest  ") + (_latest.Version == null ? _latest.Tag : _latest.Version.ToString(3)))
                : L.T("最新版本  正在检查…", "Latest  checking...");
            _pathLabel.Text = L.T("安装位置  ", "Location  ") + _dir;

            _launch.Text = L.T("安装完成后启动 SoundWheel", "Launch SoundWheel after install");
            _desktop.Text = L.T("创建桌面快捷方式", "Create a desktop shortcut");
            _removeData.Text = L.T("同时删除配置与日志（%APPDATA%\\SoundWheel）", "Also delete config and logs (%APPDATA%\\SoundWheel)");

            _launch.Visible = !_uninstallMode;
            _desktop.Visible = !_uninstallMode;
            _removeData.Visible = _uninstallMode;
            _check.Visible = !_uninstallMode;
            _latestLabel.Visible = !_uninstallMode;

            if (_uninstallMode)
            {
                _primary.Text = L.T("卸载", "Uninstall");
                _primary.BackColor = Danger;
                _primary.FlatAppearance.MouseOverBackColor = Lighten(Danger, 18);
                _primary.FlatAppearance.MouseDownBackColor = Lighten(Danger, -10);
            }
            else if (_installed != null && _latest != null && _latest.Version != null && _latest.Version > _installed)
            {
                _primary.Text = L.T("下载并更新", "Download && update");
                _primary.BackColor = Accent;
            }
            else if (_installed != null)
            {
                _primary.Text = L.T("重新安装", "Reinstall");
                _primary.BackColor = Accent;
            }
            else
            {
                _primary.Text = L.T("安装", "Install");
                _primary.BackColor = Accent;
            }

            _check.Text = L.T("检查更新", "Check for updates");
            _cancel.Text = L.T("取消", "Cancel");
            _hint.Text = "";
            Invalidate();
        }

        private void OnLoaded()
        {
            _installed = Installer.InstalledVersion(_dir);
            RebuildTexts();
            CheckLatest();
        }

        private void CheckLatest()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                ReleaseInfo info = Installer.LatestRelease(8000);
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _latest = info;
                        _latestChecked = true;
                        RebuildTexts();
                    });
                }
                catch (Exception) { }
            });
        }

        private void OnCheckUpdate(object sender, EventArgs e)
        {
            _latestChecked = false;
            _latest = null;
            RebuildTexts();
            CheckLatest();
        }

        private void OnPrimary(object sender, EventArgs e)
        {
            if (_busy) return;
            if (_uninstallMode)
            {
                if (MessageBox.Show(L.T("确定要卸载 SoundWheel 吗？", "Uninstall SoundWheel now?"), "SoundWheel",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                _busy = true;
                _primary.Enabled = false;
                _cancel.Enabled = false;
                _hint.Text = L.T("正在卸载…", "Uninstalling...");
                _bar.Visible = true;
                _bar.Value = 0.5f;
                bool removeData = _removeData.Checked;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    bool ok = Installer.DoUninstall(_dir, removeData);
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (ok)
                            {
                                MessageBox.Show(L.T("已卸载。", "Uninstalled."), "SoundWheel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                Close();
                            }
                            else
                            {
                                _busy = false;
                                _primary.Enabled = true;
                                _cancel.Enabled = true;
                                _hint.Text = L.T("卸载失败，请关闭 SoundWheel 后重试。", "Uninstall failed. Close SoundWheel and try again.");
                            }
                        });
                    }
                    catch (Exception) { }
                });
                return;
            }

            if (_installed != null && _latest != null && _latest.Version != null && _latest.Version > _installed)
            {
                string message = L.T("发现新版本 ", "New version ")
                    + _latest.Version.ToString(3)
                    + L.T("（当前 ", " (installed ")
                    + _installed.ToString(3)
                    + L.T("）。是否下载并安装？", "). Download and install?");
                if (MessageBox.Show(message, "SoundWheel", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                DownloadAndRun(_latest);
                return;
            }

            if (_installed != null)
            {
                string message = L.T("当前已是最新版本 ", "You already have the latest version ")
                    + _installed.ToString(3)
                    + L.T("。是否重新安装？", ". Reinstall?");
                if (MessageBox.Show(message, "SoundWheel", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            }
            RunInstall();
        }

        private void RunInstall()
        {
            _busy = true;
            _primary.Enabled = false;
            _check.Enabled = false;
            _cancel.Enabled = false;
            _hint.Text = L.T("正在安装…", "Installing...");
            _bar.Visible = true;
            _bar.Value = 0.35f;
            bool desktop = _desktop.Checked;
            bool launch = _launch.Checked;
            ThreadPool.QueueUserWorkItem(delegate
            {
                string error;
                bool ok = Installer.DoInstall(_dir, desktop, launch, false, out error);
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _bar.Value = ok ? 1f : 0f;
                        if (ok)
                        {
                            _installed = Installer.InstalledVersion(_dir);
                            _hint.Text = L.T("安装完成。", "Installed.");
                            MessageBox.Show(L.T("SoundWheel 已安装到：\r\n", "SoundWheel installed to:\r\n") + _dir,
                                "SoundWheel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Close();
                        }
                        else
                        {
                            _busy = false;
                            _primary.Enabled = true;
                            _check.Enabled = true;
                            _cancel.Enabled = true;
                            _hint.Text = L.T("安装失败：", "Install failed: ") + error;
                            MessageBox.Show(L.T("安装失败：", "Install failed: ") + error, "SoundWheel",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    });
                }
                catch (Exception) { }
            });
        }

        private void DownloadAndRun(ReleaseInfo info)
        {
            if (string.IsNullOrEmpty(info.SetupUrl))
            {
                try { Process.Start(info.PageUrl); }
                catch (Exception) { }
                return;
            }
            _busy = true;
            _primary.Enabled = false;
            _check.Enabled = false;
            _cancel.Enabled = false;
            _hint.Text = L.T("正在下载新版本…", "Downloading the new version...");
            _bar.Visible = true;
            _bar.Value = 0.25f;
            string url = info.SetupUrl;
            ThreadPool.QueueUserWorkItem(delegate
            {
                string path = Path.Combine(Path.GetTempPath(), "SoundWheelSetup-" + info.Version.ToString(3) + ".exe");
                string error;
                bool ok = Installer.Download(url, path, 120000, out error);
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (!ok)
                        {
                            _busy = false;
                            _primary.Enabled = true;
                            _check.Enabled = true;
                            _cancel.Enabled = true;
                            _hint.Text = L.T("下载失败：", "Download failed: ") + error;
                            return;
                        }
                        try
                        {
                            Process.Start(path, "/S --dir \"" + _dir + "\"");
                            Close();
                        }
                        catch (Exception ex)
                        {
                            _hint.Text = L.T("启动安装程序失败：", "Failed to start installer: ") + ex.Message;
                        }
                    });
                }
                catch (Exception) { }
            });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = Round(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), S(14)))
            using (SolidBrush b = new SolidBrush(Bg)) g.FillPath(b, p);
            Rectangle card = new Rectangle(S(30), S(96), S(580), S(128));
            using (GraphicsPath p = Round(card, S(10)))
            {
                using (SolidBrush b = new SolidBrush(Card)) g.FillPath(b, p);
                using (Pen pen = new Pen(BorderColor)) g.DrawPath(pen, p);
            }
            using (Pen pen = new Pen(BorderColor)) g.DrawRectangle(pen, 0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                using (GraphicsPath p = Round(new RectangleF(0f, 0f, Width, Height), S(14)))
                    Region = new Region(p);
            }
            catch (Exception) { }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084) // WM_NCHITTEST
            {
                Point pt = PointToClient(new Point((int)(long)m.LParam & 0xFFFF, (int)((long)m.LParam >> 16)));
                if (pt.Y < S(56)) { m.Result = (IntPtr)2; return; }
            }
            base.WndProc(ref m);
        }

        private static GraphicsPath Round(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = radius * 2f;
            p.AddArc(r.X, r.Y, d, d, 180f, 90f);
            p.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            p.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
            p.CloseFigure();
            return p;
        }
    }
}
