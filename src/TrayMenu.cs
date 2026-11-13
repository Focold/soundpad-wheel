// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace SoundWheel
{
    // 托盘右键菜单：圆角、跟随深色 / 浅色主题、显示热键与 Soundpad 状态
    internal sealed class TrayMenu : ContextMenuStrip
    {
        private readonly App _app;
        private readonly Font _titleFont;
        private readonly Font _subFont;
        private readonly ToolStripLabel _status;
        private readonly ToolStripMenuItem _open;
        private ToolStripMenuItem[] _modes;
        private ToolStripMenuItem[] _accents;

        public TrayMenu(App app)
        {
            _app = app;
            _titleFont = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold, GraphicsUnit.Point);
            _subFont = new Font("Microsoft YaHei UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            ShowImageMargin = true;
            ShowCheckMargin = false;
            DropShadowEnabled = true;
            Padding = new Padding(0, 4, 0, 4);

            ToolStripLabel title = new ToolStripLabel("SoundWheel  " + VersionText());
            title.Font = _titleFont;
            title.Tag = "title";
            title.Margin = new Padding(12, 8, 12, 0);
            Items.Add(title);

            _status = new ToolStripLabel("");
            _status.Font = _subFont;
            _status.Tag = "sub";
            _status.Margin = new Padding(12, 0, 12, 8);
            Items.Add(_status);
            Items.Add(new ToolStripSeparator());

            _open = new ToolStripMenuItem("打开轮盘");
            _open.Click += delegate { _app.TrayOpen(); };
            Items.Add(_open);

            ToolStripMenuItem settings = new ToolStripMenuItem("设置…");
            settings.Click += delegate { _app.TraySettings(); };
            Items.Add(settings);

            Items.Add(new ToolStripSeparator());

            ToolStripMenuItem theme = new ToolStripMenuItem("主题");
            _modes = new ToolStripMenuItem[3];
            string[] modeNames = new string[] { "跟随系统", "深色", "浅色" };
            for (int i = 0; i < 3; i++)
            {
                int mode = i;
                ToolStripMenuItem mi = new ToolStripMenuItem(modeNames[i]);
                mi.Click += delegate { _app.TrayThemeMode(mode); };
                _modes[i] = mi;
                theme.DropDownItems.Add(mi);
            }
            theme.DropDownItems.Add(new ToolStripSeparator());
            _accents = new ToolStripMenuItem[Theme.AccentPresets.Length];
            for (int i = 0; i < Theme.AccentPresets.Length; i++)
            {
                string hex = Theme.AccentPresets[i];
                ToolStripMenuItem mi = new ToolStripMenuItem(hex.ToUpperInvariant());
                mi.Image = SwatchImage(ColorUtil.Parse(hex, Color.Gray));
                mi.Click += delegate { _app.TrayAccent(hex); };
                _accents[i] = mi;
                theme.DropDownItems.Add(mi);
            }
            Items.Add(theme);

            ToolStripMenuItem refresh = new ToolStripMenuItem("刷新音效列表");
            refresh.Click += delegate { _app.TrayRefresh(); };
            Items.Add(refresh);

            ToolStripMenuItem folder = new ToolStripMenuItem("打开数据文件夹");
            folder.Click += delegate { _app.TrayOpenDataFolder(); };
            Items.Add(folder);

            ToolStripMenuItem update = new ToolStripMenuItem("检查更新…");
            update.Click += delegate { _app.TrayCheckUpdate(); };
            Items.Add(update);

            Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exit = new ToolStripMenuItem("退出");
            exit.Click += delegate { _app.TrayExit(); };
            Items.Add(exit);

            Opened += delegate { RefreshState(); };
            Theme.Current = Theme.Build(app.Config.ThemeMode, app.Config.Accent);
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            Theme t = Theme.Current;
            BackColor = t.Surface;
            ForeColor = t.Text;
            Renderer = new TrayRenderer(t, _titleFont, _subFont);
            foreach (ToolStripItem item in Items)
            {
                ToolStripLabel label = item as ToolStripLabel;
                if (label != null)
                    label.ForeColor = (string)label.Tag == "sub" ? t.TextDim : t.Text;
            }
            Invalidate(true);
        }

        private void RefreshState()
        {
            _status.Text = _app.TrayStatusText;
            _open.ShortcutKeyDisplayString = _app.TrayHotkeyText;
            for (int i = 0; i < _modes.Length; i++) _modes[i].Checked = _app.Config.ThemeMode == i;
            for (int i = 0; i < _accents.Length; i++)
                _accents[i].Checked = string.Equals(_accents[i].Text, _app.Config.Accent, StringComparison.OrdinalIgnoreCase);
            ApplyTheme();
        }

        private static string VersionText()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        private static Image SwatchImage(Color color)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (GraphicsPath path = Ui.Round(new RectangleF(1, 1, 14, 14), 4f))
                using (SolidBrush b = new SolidBrush(color))
                    g.FillPath(b, path);
                using (GraphicsPath path = Ui.Round(new RectangleF(1, 1, 14, 14), 4f))
                using (Pen p = new Pen(Color.FromArgb(60, 0, 0, 0), 1f))
                    g.DrawPath(p, path);
            }
            return bmp;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRegion();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (!IsHandleCreated) return;
            float s;
            using (Graphics g = CreateGraphics()) s = g.DpiX / 96f;
            if (s < 1f) s = 1f;
            using (GraphicsPath path = Ui.Round(new RectangleF(0, 0, Width, Height), 10f * s))
                Region = new Region(path);
        }

        private sealed class TrayRenderer : ToolStripProfessionalRenderer
        {
            private readonly Theme _t;
            private readonly Font _titleFont;
            private readonly Font _subFont;

            public TrayRenderer(Theme t, Font titleFont, Font subFont) : base(new TrayColors(t))
            {
                _t = t;
                _titleFont = titleFont;
                _subFont = subFont;
                RoundedEdges = false;
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = e.ToolStrip.ClientRectangle;
                using (SolidBrush b = new SolidBrush(_t.Surface))
                    g.FillRectangle(b, r);
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                using (Pen p = new Pen(Theme.Alpha(_t.Border, 230), 1f))
                    g.DrawRectangle(p, r);
            }

            protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(_t.Surface))
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                ToolStripItem item = e.Item;
                Rectangle r = new Rectangle(6, 1, e.ToolStrip.Width - 12, item.Height - 2);
                if (item.Selected || item.Pressed)
                {
                    using (SolidBrush b = new SolidBrush(_t.Dark ? Color.FromArgb(52, 60, 76) : Color.FromArgb(232, 238, 248)))
                        e.Graphics.FillRectangle(b, r);
                }
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                Theme t = _t;
                e.TextColor = e.Item.Enabled ? t.Text : t.TextFaint;
                if (e.Item is ToolStripMenuItem && ((ToolStripMenuItem)e.Item).Checked) e.TextColor = t.Accent;
                if (e.Item.Tag as string == "sub") e.TextColor = t.TextDim;
                if (e.Item.Tag as string == "title") e.TextColor = t.Text;
                base.OnRenderItemText(e);
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                int y = e.Item.Height / 2;
                using (Pen p = new Pen(Theme.Alpha(_t.Border, 170), 1f))
                    e.Graphics.DrawLine(p, 14, y, e.ToolStrip.Width - 14, y);
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                Theme t = _t;
                Rectangle b = e.ImageRectangle;
                using (Pen p = new Pen(t.Accent, 2f))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawLines(p, new Point[]
                    {
                        new Point(b.Left + 2, b.Top + b.Height / 2),
                        new Point(b.Left + b.Width / 2 - 1, b.Bottom - 3),
                        new Point(b.Right - 1, b.Top + 3)
                    });
                }
            }
        }

        private sealed class TrayColors : ProfessionalColorTable
        {
            private readonly Theme _t;
            public TrayColors(Theme t) { _t = t; }
            public override Color ToolStripDropDownBackground { get { return _t.Surface; } }
            public override Color ImageMarginGradientBegin { get { return _t.Surface; } }
            public override Color ImageMarginGradientMiddle { get { return _t.Surface; } }
            public override Color ImageMarginGradientEnd { get { return _t.Surface; } }
            public override Color MenuBorder { get { return _t.Border; } }
            public override Color MenuItemBorder { get { return Color.Transparent; } }
            public override Color MenuItemSelected { get { return _t.CardHover; } }
            public override Color SeparatorDark { get { return _t.Border; } }
            public override Color SeparatorLight { get { return _t.Border; } }
        }
    }
}
