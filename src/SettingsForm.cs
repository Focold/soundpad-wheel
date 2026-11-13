// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace SoundWheel
{
    internal sealed class SettingsForm : Form
    {
        private readonly AppConfig _config;
        private readonly AppConfig _work;
        private readonly SoundpadClient _client;
        private readonly float _fontScale;
        private readonly float _s;
        private readonly Theme _originalTheme;

        private Panel _sidebar;
        private Panel _content;
        private Panel _footer;
        private Panel[] _pages;
        private RoundedButton[] _nav;

        private RoundedButton _saveButton;
        private RoundedButton _cancelButton;
        private RoundedButton _previewButton;
        private RoundedButton _refreshButton;
        private RoundedButton _checkButton;
        private RoundedButton _downloadButton;
        private RoundedButton _dataButton;
        private RoundedButton _repoButton;
        private RoundedButton _customAccentButton;
        private RoundedButton _probeButton;
        private RoundedButton _closeButton;

        private HotkeyBox _hotkeyBox;
        private RoundedCheck _toggleCheck;
        private RoundedCheck _micCheck;
        private RoundedCheck _speakerCheck;
        private RoundedCheck _toastCheck;
        private RoundedCheck _animCheck;
        private RoundedCheck _autoStartCheck;
        private RoundedCheck _captureCheck;
        private Slider _scaleBar;
        private Slider _fontBar;
        private Label _scaleValue;
        private Label _fontValue;
        private Label _footerHint;
        private Label _selectionLabel;
        private Label _libraryHint;
        private NumberBox _innerCount;
        private NumberBox _outerCount;
        private NumberBox _sensitivity;
        private ComboBox _mouseMode;
        private Label _probeLabel;
        private Label _statusLabel;
        private SegmentedControl _themeSegment;
        private PaletteSwatch[] _swatches;
        private Label _accentHexLabel;
        private Label _themeCaption;
        private WheelPreview _preview;
        private WheelPreview _themePreview;
        private ListBox _library;
        private Label _updateStatus;

        private Font _fontH1;
        private Font _fontH2;
        private Font _fontBody;
        private Font _fontBodyBold;
        private Font _fontSmall;
        private Font _fontTiny;

        private SoundEntry[] _sounds = new SoundEntry[0];
        private bool _busy;
        private int _page;
        private int _selectedRing = -1;
        private int _selectedSlot = -1;
        private int _hoverIndex = -1;
        private Point _dragStart = Point.Empty;
        private int _dragIndex = -1;
        private int _workThemeMode;
        private string _workAccent;
        private Version _latestVersion;
        private string _latestSetupUrl = "";

        public event Action PreviewRequested;

        public SettingsForm(AppConfig config, SoundpadClient client)
        {
            _config = config;
            _client = client;
            _work = CloneConfig(config);
            _originalTheme = Theme.Build(config.ThemeMode, config.Accent);

            float dpi;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) dpi = g.DpiX / 96f;
            _fontScale = (float)config.FontScale;
            if (_fontScale < 0.85f) _fontScale = 0.85f;
            if (_fontScale > 1.35f) _fontScale = 1.35f;
            _s = dpi * _fontScale;

            _workThemeMode = _work.ThemeMode;
            _workAccent = _work.Accent;
            Theme.Current = Theme.Build(_workThemeMode, _workAccent);

            Text = "SoundWheel 设置";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;
            DoubleBuffered = true;
            BackColor = Theme.Current.WindowBg;
            ClientSize = new Size(S(1120), S(720));
            MinimumSize = Size;
            MaximumSize = Size;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch (Exception) { }

            BuildFonts();
            BuildUi();
            int startPage;
            if (int.TryParse(Environment.GetEnvironmentVariable("SOUNDWHEEL_SETTINGS_PAGE"), out startPage) && startPage >= 0 && startPage < 5)
                SetPage(startPage);

            _sounds = _client != null && _client.Sounds != null ? _client.Sounds : new SoundEntry[0];
            if (_sounds.Length == 0) _sounds = SoundCache.Load(AppPaths.SoundsFile);
            FillLibrary();

            Shown += delegate
            {
                UpdateChrome();
                if (_sounds.Length == 0) OnRefresh();
            };
        }

        private AppConfig CloneConfig(AppConfig src)
        {
            AppConfig c = new AppConfig();
            c.Hotkey = src.Hotkey != null ? src.Hotkey.Clone() : HotkeySpec.Default;
            c.ToggleMode = src.ToggleMode;
            c.PlayToMic = src.PlayToMic;
            c.PlayToSpeaker = src.PlayToSpeaker;
            c.ShowToast = src.ShowToast;
            c.AutoStart = src.AutoStart;
            c.Animations = src.Animations;
            c.Scale = src.Scale;
            c.Accent = src.Accent;
            c.InnerCount = src.InnerCount;
            c.OuterCount = src.OuterCount;
            c.MouseMode = src.MouseMode;
            c.MouseSensitivity = src.MouseSensitivity;
            c.CaptureMouse = src.CaptureMouse;
            c.ThemeMode = src.ThemeMode;
            c.FontScale = src.FontScale;
            c.Inner = new List<SlotConfig>();
            for (int i = 0; i < src.Inner.Count; i++) c.Inner.Add(src.Inner[i].Clone());
            c.Outer = new List<SlotConfig>();
            for (int i = 0; i < src.Outer.Count; i++) c.Outer.Add(src.Outer[i].Clone());
            c.Normalize();
            return c;
        }

        private int S(float v)
        {
            return (int)Math.Round(v * _s);
        }

        private Font MakeFont(float pt, FontStyle style)
        {
            try { return new Font("Microsoft YaHei UI", pt * _fontScale, style, GraphicsUnit.Point); }
            catch (Exception) { return new Font(FontFamily.GenericSansSerif, pt * _fontScale, style, GraphicsUnit.Point); }
        }

        private void BuildFonts()
        {
            _fontH1 = MakeFont(14f, FontStyle.Bold);
            _fontH2 = MakeFont(10.5f, FontStyle.Bold);
            _fontBody = MakeFont(9.5f, FontStyle.Regular);
            _fontBodyBold = MakeFont(9.5f, FontStyle.Bold);
            _fontSmall = MakeFont(8.8f, FontStyle.Regular);
            _fontTiny = MakeFont(8.2f, FontStyle.Regular);
        }

        private void BuildUi()
        {
            SuspendLayout();

            _sidebar = new Panel();
            _sidebar.BackColor = Theme.Current.SidebarBg;
            _sidebar.Bounds = new Rectangle(0, 0, S(210), ClientSize.Height);
            Controls.Add(_sidebar);
            _sidebar.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.Current.Border, 1f))
                    e.Graphics.DrawLine(p, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
            };

            Label brand = new Label();
            brand.Text = "SoundWheel";
            brand.AutoSize = false;
            brand.Bounds = new Rectangle(S(22), S(18), S(140), S(30));
            brand.Font = _fontH1;
            brand.ForeColor = Theme.Current.Text;
            brand.BackColor = Theme.Current.SidebarBg;
            _sidebar.Controls.Add(brand);

            Label ver = new Label();
            ver.Text = UpdateService.CurrentVersion.ToString(3);
            ver.AutoSize = false;
            ver.Bounds = new Rectangle(S(23), S(48), S(100), S(20));
            ver.Font = _fontTiny;
            ver.ForeColor = Theme.Current.TextFaint;
            ver.BackColor = Theme.Current.SidebarBg;
            _sidebar.Controls.Add(ver);

            _closeButton = MakeIconButton("✕");
            _closeButton.Bounds = new Rectangle(S(210 - 44), S(10), S(32), S(32));
            _closeButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            _sidebar.Controls.Add(_closeButton);

            string[] titles = new string[] { "常规", "轮盘", "主题", "游戏与鼠标", "关于" };
            _nav = new RoundedButton[titles.Length];
            for (int i = 0; i < titles.Length; i++)
            {
                RoundedButton b = new RoundedButton();
                b.Text = titles[i];
                b.Variant = 4;
                b.AlignLeft = true;
                b.Bounds = new Rectangle(S(14), S(92 + i * 48), S(182), S(42));
                b.Font = _fontBody;
                b.UiScale = _s;
                b.Radius = 10f;
                int idx = i;
                b.Click += delegate { SetPage(idx); };
                _sidebar.Controls.Add(b);
                _nav[i] = b;
            }

            _footer = new Panel();
            _footer.BackColor = Theme.Current.Surface;
            _footer.Bounds = new Rectangle(S(210), ClientSize.Height - S(66), ClientSize.Width - S(210), S(66));
            _footer.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen p = new Pen(Theme.Current.Border, 1f))
                    e.Graphics.DrawLine(p, 0, 0, _footer.Width, 0);
            };
            Controls.Add(_footer);

            _content = new Panel();
            _content.BackColor = Theme.Current.WindowBg;
            _content.Bounds = new Rectangle(S(210), 0, ClientSize.Width - S(210), ClientSize.Height - S(66));
            Controls.Add(_content);

            BuildFooter();

            int pw = _content.Width - S(48);
            int ph = _content.Height - S(36);
            _pages = new Panel[5];
            for (int i = 0; i < 5; i++)
            {
                Panel p = new Panel();
                p.BackColor = Theme.Current.WindowBg;
                p.Bounds = new Rectangle(S(24), S(18), pw, ph);
                p.AutoScroll = true;
                p.Visible = false;
                _content.Controls.Add(p);
                _pages[i] = p;
            }
            BuildGeneralPage(_pages[0]);
            BuildWheelPage(_pages[1]);
            BuildThemePage(_pages[2]);
            BuildMousePage(_pages[3]);
            BuildAboutPage(_pages[4]);

            SetPage(0);
            ResumeLayout(false);
            PerformLayout();
        }

        private RoundedButton MakeIconButton(string text)
        {
            RoundedButton b = new RoundedButton();
            b.Text = text;
            b.Variant = 4;
            b.UiScale = _s;
            b.Radius = 8f;
            b.Font = _fontBody;
            return b;
        }

        private void BuildFooter()
        {
            int fw = _footer.Width;
            _previewButton = new RoundedButton();
            _previewButton.Text = "预览轮盘";
            _previewButton.Variant = 0;
            _previewButton.Bounds = new Rectangle(S(20), S(15), S(104), S(36));
            _previewButton.Font = _fontBody;
            _previewButton.UiScale = _s;
            _previewButton.Click += delegate { if (PreviewRequested != null) PreviewRequested(); };
            _footer.Controls.Add(_previewButton);

            _footerHint = AddLabel(_footer, "提示：按住快捷键唤出轮盘，松开即播放", 138, 15, 340, 36, _fontSmall, "dim");
            _footerHint.TextAlign = ContentAlignment.MiddleLeft;

            _cancelButton = new RoundedButton();
            _cancelButton.Text = "取消";
            _cancelButton.Variant = 0;
            _cancelButton.Bounds = new Rectangle(fw - S(20) - S(96), S(15), S(96), S(36));
            _cancelButton.Font = _fontBody;
            _cancelButton.UiScale = _s;
            _cancelButton.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            _footer.Controls.Add(_cancelButton);

            _saveButton = new RoundedButton();
            _saveButton.Text = "保存";
            _saveButton.Variant = 1;
            _saveButton.Bounds = new Rectangle(_cancelButton.Left - S(10) - S(100), S(15), S(100), S(36));
            _saveButton.Font = _fontBodyBold;
            _saveButton.UiScale = _s;
            _saveButton.Click += delegate { OnSave(); };
            _footer.Controls.Add(_saveButton);
        }

        private Card AddCard(Control page, string title, int x, int y, int w, int h)
        {
            Card c = new Card();
            c.Title = title;
            c.TitleFont = _fontH2;
            c.UiScale = _s;
            c.Radius = 14f;
            c.Bounds = new Rectangle(S(x), S(y), S(w), S(h));
            c.BackColor = Theme.Current.Card;
            page.Controls.Add(c);
            return c;
        }

        private Label AddLabel(Control parent, string text, int x, int y, int w, int h, Font font, string kind)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = false;
            l.Bounds = new Rectangle(S(x), S(y), S(w), S(h));
            l.Font = font;
            l.Tag = kind;
            l.BackColor = parent.BackColor;
            l.ForeColor = kind == "dim" ? Theme.Current.TextDim : (kind == "accent" ? Theme.Current.Accent : (kind == "danger" ? Theme.Current.Danger : (kind == "faint" ? Theme.Current.TextFaint : Theme.Current.Text)));
            l.TextAlign = ContentAlignment.MiddleLeft;
            parent.Controls.Add(l);
            return l;
        }

        private RoundedCheck AddCheck(Control parent, string text, int x, int y, int w, int h, bool value)
        {
            RoundedCheck c = new RoundedCheck();
            c.Text = text;
            c.Checked = value;
            c.Bounds = new Rectangle(S(x), S(y), S(w), S(h));
            c.Font = _fontBody;
            c.UiScale = _s;
            c.BackColor = parent.BackColor;
            parent.Controls.Add(c);
            return c;
        }

        private RoundedButton AddButton(Control parent, string text, int x, int y, int w, int h, int variant)
        {
            RoundedButton b = new RoundedButton();
            b.Text = text;
            b.Variant = variant;
            b.Bounds = new Rectangle(S(x), S(y), S(w), S(h));
            b.Font = _fontBody;
            b.UiScale = _s;
            b.Radius = 9f;
            b.BackColor = Color.Transparent;
            parent.Controls.Add(b);
            return b;
        }

        private void BuildGeneralPage(Panel page)
        {
            Card c1 = AddCard(page, "唤出方式", 0, 0, 496, 168);
            AddLabel(c1, "快捷键", 20, 54, 70, 32, _fontBody, "");
            _hotkeyBox = new HotkeyBox();
            _hotkeyBox.Bounds = new Rectangle(S(96), S(52), S(220), S(34));
            _hotkeyBox.Font = _fontBody;
            _hotkeyBox.BorderStyle = BorderStyle.FixedSingle;
            _hotkeyBox.BackColor = Theme.Current.Dark ? Color.FromArgb(24, 28, 36) : Color.White;
            _hotkeyBox.ForeColor = Theme.Current.Text;
            _hotkeyBox.Spec = _work.Hotkey;
            c1.Controls.Add(_hotkeyBox);
            AddLabel(c1, "点击输入框后按下组合键（Backspace 清除）", 20, 90, 450, 20, _fontTiny, "faint");
            _toggleCheck = AddCheck(c1, "点按模式：按一下打开，左键单击音效后关闭", 20, 118, 456, 28, _work.ToggleMode);

            Card c2 = AddCard(page, "播放", 0, 184, 496, 150);
            _micCheck = AddCheck(c2, "发送到麦克风（游戏或语音里能听到）", 20, 52, 456, 28, _work.PlayToMic);
            _speakerCheck = AddCheck(c2, "同时从扬声器播放", 20, 84, 456, 28, _work.PlayToSpeaker);
            _toastCheck = AddCheck(c2, "显示播放提示", 20, 116, 456, 28, _work.ShowToast);

            Card c3 = AddCard(page, "外观与布局", 0, 350, 496, 214);
            AddLabel(c3, "轮盘大小", 20, 54, 84, 32, _fontBody, "");
            _scaleBar = new Slider();
            _scaleBar.Minimum = 70;
            _scaleBar.Maximum = 160;
            _scaleBar.Step = 1;
            _scaleBar.UiScale = _s;
            _scaleBar.Value = (int)Math.Round(_work.Scale * 100);
            _scaleBar.Bounds = new Rectangle(S(108), S(50), S(270), S(30));
            _scaleBar.ValueChanged += delegate { _scaleValue.Text = ((int)Math.Round(_scaleBar.Value)).ToString() + "%"; };
            c3.Controls.Add(_scaleBar);
            _scaleValue = AddLabel(c3, ((int)Math.Round(_scaleBar.Value)).ToString() + "%", 386, 50, 70, 32, _fontBody, "");

            AddLabel(c3, "内圈格数", 20, 106, 84, 32, _fontBody, "");
            _innerCount = new NumberBox();
            _innerCount.Minimum = 1;
            _innerCount.Maximum = 12;
            _innerCount.Step = 1;
            _innerCount.UiScale = _s;
            _innerCount.Font = _fontBody;
            _innerCount.Value = _work.InnerCount;
            _innerCount.Bounds = new Rectangle(S(108), S(104), S(104), S(34));
            _innerCount.ValueChanged += delegate { OnCountChanged(); };
            c3.Controls.Add(_innerCount);

            AddLabel(c3, "外圈格数", 216, 106, 84, 32, _fontBody, "");
            _outerCount = new NumberBox();
            _outerCount.Minimum = 0;
            _outerCount.Maximum = 16;
            _outerCount.Step = 1;
            _outerCount.UiScale = _s;
            _outerCount.Font = _fontBody;
            _outerCount.Value = _work.OuterCount;
            _outerCount.Bounds = new Rectangle(S(304), S(104), S(104), S(34));
            _outerCount.ValueChanged += delegate { OnCountChanged(); };
            c3.Controls.Add(_outerCount);

            _animCheck = AddCheck(c3, "动画效果（打开 / 关闭的缩放淡入）", 20, 148, 400, 28, _work.Animations);
            AddLabel(c3, "主题色、深浅色和字体在『主题』页调整。", 20, 180, 440, 22, _fontTiny, "faint");

            Card c4 = AddCard(page, "启动与数据", 518, 0, 344, 180);
            _autoStartCheck = AddCheck(c4, "登录 Windows 时自动启动", 20, 52, 304, 28, _work.AutoStart);
            AddLabel(c4, "配置与日志保存在：", 20, 90, 304, 20, _fontTiny, "faint");
            AddLabel(c4, AppPaths.DataDir, 20, 112, 304, 50, _fontTiny, "dim");

            Card c5 = AddCard(page, "使用说明", 518, 196, 344, 368);
            AddLabel(c5,
                "① 按住快捷键唤出轮盘，轮盘出现在鼠标位置。\r\n\r\n" +
                "② 移动鼠标高亮音效，松开快捷键立即播放。\r\n\r\n" +
                "③ 右键或 Esc 取消，不会播放。\r\n\r\n" +
                "④ 点按模式下：按一下打开，左键单击播放。\r\n\r\n" +
                "⑤ 内圈 1–12、外圈 0–16 个音效，在『轮盘』页拖拽装配。\r\n\r\n" +
                "⑥ 使用前请启动 Soundpad。",
                20, 50, 304, 300, _fontBody, "dim");
        }

        private void BuildWheelPage(Panel page)
        {
            Card c1 = AddCard(page, "轮盘", 0, 0, 486, 564);
            _preview = new WheelPreview();
            _preview.Bounds = new Rectangle(S(16), S(44), S(454), S(440));
            _preview.BackColor = Theme.Current.Card;
            _preview.SetConfig(_work);
            _preview.SlotClicked += delegate(int ring, int slot) { SelectSlot(ring, slot); };
            _preview.SlotCleared += delegate(int ring, int slot) { ClearSlot(ring, slot); };
            _preview.SlotDropped += delegate(int ring, int slot, int index, string title) { AssignSlot(ring, slot, index, title); };
            c1.Controls.Add(_preview);

            _selectionLabel = AddLabel(c1, "拖动右侧音效到扇区装配 · 右键扇区清除", 20, 492, 446, 24, _fontSmall, "dim");
            _selectionLabel.TextAlign = ContentAlignment.MiddleCenter;
            AddLabel(c1, "空扇区显示『空闲』，中心圆显示当前指向的音效。", 20, 520, 446, 24, _fontTiny, "faint").TextAlign = ContentAlignment.MiddleCenter;

            Card c2 = AddCard(page, "音效库", 508, 0, 354, 564);
            _statusLabel = AddLabel(c2, "尚未读取音效列表", 18, 44, 210, 24, _fontTiny, "dim");
            _refreshButton = AddButton(c2, "刷新列表", 232, 38, 104, 32, 0);
            _refreshButton.Font = _fontSmall;
            _refreshButton.Click += delegate { OnRefresh(); };

            _library = new ListBox();
            _library.Bounds = new Rectangle(S(14), S(80), S(326), S(420));
            _library.DrawMode = DrawMode.OwnerDrawFixed;
            _library.ItemHeight = S(34);
            _library.BorderStyle = BorderStyle.None;
            _library.IntegralHeight = false;
            _library.BackColor = Theme.Current.Card;
            _library.ForeColor = Theme.Current.Text;
            _library.Font = _fontBody;
            _library.DrawItem += LibraryDrawItem;
            _library.MouseMove += LibraryMouseMove;
            _library.MouseDown += LibraryMouseDown;
            _library.MouseUp += delegate { _dragStart = Point.Empty; _dragIndex = -1; };
            _library.MouseLeave += delegate { _hoverIndex = -1; _library.Invalidate(); };
            _library.DoubleClick += LibraryDoubleClick;
            _library.HandleCreated += delegate { Native.UseDarkScrollbars(_library.Handle, Theme.Current.Dark); };
            c2.Controls.Add(_library);

            _libraryHint = AddLabel(c2, "把音效拖到左侧扇区即可装配；也可以先点扇区，再双击这里的音效。", 18, 508, 318, 44, _fontTiny, "faint");
        }

        private void BuildThemePage(Panel page)
        {
            Card c1 = AddCard(page, "外观模式", 0, 0, 420, 150);
            _themeSegment = new SegmentedControl();
            _themeSegment.Segments = new string[] { "跟随系统", "深色", "浅色" };
            _themeSegment.Bounds = new Rectangle(S(20), S(54), S(380), S(38));
            _themeSegment.Font = _fontBody;
            _themeSegment.UiScale = _s;
            _themeSegment.SelectedIndex = _work.ThemeMode;
            _themeSegment.SelectionChanged += delegate
            {
                _workThemeMode = _themeSegment.SelectedIndex;
                ApplyLiveTheme();
            };
            c1.Controls.Add(_themeSegment);
            AddLabel(c1, "更改后立即预览，保存后写入配置。", 20, 104, 380, 22, _fontTiny, "faint");

            Card c2 = AddCard(page, "主题色", 0, 166, 420, 240);
            _swatches = new PaletteSwatch[Theme.AccentPresets.Length];
            for (int i = 0; i < _swatches.Length; i++)
            {
                PaletteSwatch sw = new PaletteSwatch();
                sw.Swatch = ColorUtil.Parse(Theme.AccentPresets[i], Color.White);
                sw.Bounds = new Rectangle(S(20 + (i % 6) * 52), S(54 + (i / 6) * 52), S(40), S(40));
                sw.UiScale = _s;
                sw.BackColor = Theme.Current.Card;
                string hex = Theme.AccentPresets[i];
                sw.Click += delegate { _workAccent = hex; ApplyLiveTheme(); };
                c2.Controls.Add(sw);
                _swatches[i] = sw;
            }
            _customAccentButton = AddButton(c2, "自定义…", 20, 170, 110, 32, 0);
            _customAccentButton.Font = _fontSmall;
            _customAccentButton.Click += delegate { PickCustomAccent(); };
            _accentHexLabel = AddLabel(c2, _workAccent.ToUpperInvariant(), 142, 170, 120, 32, _fontBody, "accent");

            Card c3 = AddCard(page, "字体与缩放", 0, 422, 420, 142);
            AddLabel(c3, "界面字体", 20, 52, 80, 32, _fontBody, "");
            _fontBar = new Slider();
            _fontBar.Minimum = 85;
            _fontBar.Maximum = 135;
            _fontBar.Step = 1;
            _fontBar.UiScale = _s;
            _fontBar.Value = (int)Math.Round(_work.FontScale * 100);
            _fontBar.Bounds = new Rectangle(S(104), S(48), S(220), S(30));
            _fontBar.ValueChanged += delegate { _fontValue.Text = ((int)Math.Round(_fontBar.Value)).ToString() + "%"; };
            c3.Controls.Add(_fontBar);
            _fontValue = AddLabel(c3, ((int)Math.Round(_fontBar.Value)).ToString() + "%", 334, 48, 70, 32, _fontBody, "");
            AddLabel(c3, "保存后重新打开设置窗口生效。", 20, 96, 380, 22, _fontTiny, "faint");

            Card c4 = AddCard(page, "预览", 442, 0, 420, 564);
            _themePreview = new WheelPreview();
            _themePreview.Bounds = new Rectangle(S(16), S(44), S(388), S(388));
            _themePreview.BackColor = Theme.Current.Card;
            _themePreview.SetConfig(_work);
            _themePreview.AllowDrop = false;
            c4.Controls.Add(_themePreview);
            _themeCaption = AddLabel(c4, "", 20, 448, 380, 26, _fontBody, "dim");
            _themeCaption.TextAlign = ContentAlignment.MiddleCenter;
            AddLabel(c4, "轮盘颜色会跟随这里的主题，游戏里看到的也是同一套配色。", 20, 480, 380, 50, _fontTiny, "faint").TextAlign = ContentAlignment.MiddleCenter;
            UpdateThemeCaption();
        }

        private void BuildMousePage(Panel page)
        {
            Card c1 = AddCard(page, "鼠标跟踪", 0, 0, 496, 300);
            AddLabel(c1, "方式", 20, 54, 60, 32, _fontBody, "");
            _mouseMode = new ComboBox();
            _mouseMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _mouseMode.FlatStyle = FlatStyle.Flat;
            _mouseMode.Bounds = new Rectangle(S(88), S(52), S(300), S(32));
            _mouseMode.Font = _fontBody;
            _mouseMode.Items.Add("自动判断（推荐）");
            _mouseMode.Items.Add("绝对坐标（桌面程序）");
            _mouseMode.Items.Add("相对位移（游戏 / 锁定鼠标）");
            _mouseMode.SelectedIndex = _work.MouseMode;
            _mouseMode.BackColor = Theme.Current.Card;
            _mouseMode.ForeColor = Theme.Current.Text;
            c1.Controls.Add(_mouseMode);

            AddLabel(c1, "灵敏度", 20, 102, 60, 32, _fontBody, "");
            _sensitivity = new NumberBox();
            _sensitivity.Decimals = 1;
            _sensitivity.Minimum = 0.2;
            _sensitivity.Maximum = 4.0;
            _sensitivity.Step = 0.1;
            _sensitivity.UiScale = _s;
            _sensitivity.Font = _fontBody;
            _sensitivity.Value = _work.MouseSensitivity;
            _sensitivity.Bounds = new Rectangle(S(88), S(100), S(112), S(34));
            c1.Controls.Add(_sensitivity);

            _captureCheck = AddCheck(c1, "打开轮盘时接管鼠标（点击 / 移动不再传给游戏）", 20, 142, 456, 28, _work.CaptureMouse);
            AddLabel(c1,
                "钩子打开时，轮盘的指针跟着鼠标走，游戏的视角不会转、也不会开枪；松开热键（或点一下）即播放。\r\n" +
                "自动判断会检查光标是否被隐藏、被限制在屏幕中心，或是否开始收到原始位移。\r\n" +
                "灵敏度越大，鼠标移动同样的距离，轮盘上的指针走得越快。",
                20, 178, 456, 100, _fontSmall, "dim");

            Card c2 = AddCard(page, "自检", 518, 0, 344, 300);
            _probeButton = AddButton(c2, "检测鼠标状态", 20, 50, 140, 36, 0);
            _probeButton.Font = _fontSmall;
            _probeButton.Click += delegate { OnProbeMouse(); };
            _probeLabel = AddLabel(c2,
                "在目标程序（例如游戏）里打开轮盘后点这里，可以查看系统光标是否被隐藏或锁定。\r\n\r\n" +
                "如果在游戏里看不到轮盘：程序会自动把轮盘重新提到最前面；仍然看不到时，多半是游戏用了独占全屏，请改成无边框 / 窗口化全屏。",
                20, 100, 304, 180, _fontSmall, "dim");

            Card c3 = AddCard(page, "说明", 0, 316, 862, 248);
            AddLabel(c3,
                "① 接管鼠标后，轮盘打开期间的移动和点击都只作用于轮盘：游戏不会开枪，也不会跟着转视角。\r\n\r\n" +
                "② 仍有游戏直接读取原始输入（Raw Input）来转视角，这类游戏可能还会跟着动；此时优先用『按住模式』唤出并快速甩到目标扇区，或在游戏里改用窗口化模式。\r\n\r\n" +
                "③ 如果游戏或反作弊屏蔽了原始输入、或不允许安装鼠标钩子，接管会失败并退回普通跟踪；可以关掉『接管鼠标』，或在『常规』页改用点按模式。",
                20, 50, 822, 180, _fontBody, "dim");
        }

        private void BuildAboutPage(Panel page)
        {
            Version v = UpdateService.CurrentVersion;
            Card c1 = AddCard(page, "更新", 0, 0, 496, 190);
            AddLabel(c1, "当前版本 " + v.ToString(3), 20, 50, 456, 30, _fontBodyBold, "");
            _updateStatus = AddLabel(c1, "启动时会静默检查更新，也可以现在手动检查。", 20, 84, 456, 40, _fontSmall, "dim");
            _checkButton = AddButton(c1, "检查更新", 20, 132, 120, 36, 1);
            _checkButton.Font = _fontBodyBold;
            _checkButton.Click += delegate { OnCheckUpdate(); };
            _downloadButton = AddButton(c1, "下载并安装", 150, 132, 130, 36, 3);
            _downloadButton.Enabled = false;
            _downloadButton.Click += delegate { OnDownloadUpdate(); };

            Card c2 = AddCard(page, "数据", 0, 206, 496, 130);
            AddLabel(c2, AppPaths.DataDir, 20, 48, 456, 44, _fontTiny, "dim");
            _dataButton = AddButton(c2, "打开数据文件夹", 20, 96, 140, 32, 0);
            _dataButton.Font = _fontSmall;
            _dataButton.Click += delegate
            {
                try { Process.Start("explorer.exe", "\"" + AppPaths.DataDir + "\""); }
                catch (Exception) { }
            };

            Card c3 = AddCard(page, "关于", 0, 352, 496, 212);
            AddLabel(c3, "SoundWheel " + v.ToString(3), 20, 48, 300, 32, _fontH2, "");
            AddLabel(c3, "Soundpad 音效轮盘 · MIT License", 20, 82, 400, 24, _fontSmall, "dim");
            AddLabel(c3, "内容由 AI 编码代理生成，说明见仓库 AI-DISCLOSURE.md。", 20, 108, 456, 40, _fontTiny, "faint");
            _repoButton = AddButton(c3, "打开 GitHub 仓库", 20, 152, 150, 34, 0);
            _repoButton.Font = _fontSmall;
            _repoButton.Click += delegate
            {
                try { Process.Start(UpdateService.ReleasesPage); }
                catch (Exception) { }
            };

            Card c4 = AddCard(page, "小贴士", 518, 0, 344, 564);
            AddLabel(c4,
                "• 托盘图标右键可打开快捷菜单。\r\n\r\n" +
                "• 快捷键被占用时，程序会在托盘气泡里提示，可在『常规』页换一个。\r\n\r\n" +
                "• 『预览轮盘』会临时在屏幕中央显示轮盘。\r\n\r\n" +
                "• 配置、音效缓存和日志都在数据目录里，重装不会删除。\r\n\r\n" +
                "• 升级到新版本可以在本页检查更新，安装向导会自动关闭正在运行的旧版本。",
                20, 50, 304, 440, _fontBody, "dim");
        }

        private void SetPage(int index)
        {
            if (index < 0 || index >= _pages.Length) return;
            _page = index;
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].Visible = i == index;
                _nav[i].Variant = i == index ? 3 : 4;
                _nav[i].Invalidate();
            }
            if (index == 1)
            {
                _preview.RefreshData();
                FillLibrary();
            }
        }

        private void SelectSlot(int ring, int slot)
        {
            _selectedRing = ring;
            _selectedSlot = slot;
            SlotConfig sc = SlotOf(ring, slot);
            string name = sc != null && sc.IsSet ? sc.Title : "空闲";
            _selectionLabel.Text = "已选择：" + (ring == 0 ? "内圈" : "外圈") + " " + (slot + 1) + " · " + name;
            _selectionLabel.ForeColor = Theme.Current.Accent;
        }

        private SlotConfig SlotOf(int ring, int slot)
        {
            if (ring == 0 && slot >= 0 && slot < _work.Inner.Count) return _work.Inner[slot];
            if (ring == 1 && slot >= 0 && slot < _work.Outer.Count) return _work.Outer[slot];
            return null;
        }

        private void AssignSlot(int ring, int slot, int index, string title)
        {
            SlotConfig sc = SlotOf(ring, slot);
            if (sc == null) return;
            sc.Index = index;
            sc.Title = title;
            _preview.RefreshData();
            _themePreview.RefreshData();
            SelectSlot(ring, slot);
            _selectionLabel.Text = "已装配：" + title + " → " + (ring == 0 ? "内圈" : "外圈") + " " + (slot + 1);
        }

        private void ClearSlot(int ring, int slot)
        {
            SlotConfig sc = SlotOf(ring, slot);
            if (sc == null) return;
            sc.Title = "";
            sc.Index = 0;
            _preview.RefreshData();
            _themePreview.RefreshData();
            SelectSlot(ring, slot);
        }

        private void AssignFromLibrary(SoundEntry se, int ring, int slot)
        {
            AssignSlot(ring, slot, se.Index, se.Title);
        }

        private void LibraryMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragStart = e.Location;
            _dragIndex = _library.IndexFromPoint(e.Location);
        }

        private void LibraryMouseMove(object sender, MouseEventArgs e)
        {
            if (_hoverIndex != _library.IndexFromPoint(e.Location))
            {
                _hoverIndex = _library.IndexFromPoint(e.Location);
                _library.Invalidate();
            }
            if (e.Button != MouseButtons.Left || _dragStart == Point.Empty || _dragIndex < 0) return;
            if (Math.Abs(e.X - _dragStart.X) < 4 && Math.Abs(e.Y - _dragStart.Y) < 4) return;
            if (_dragIndex >= _library.Items.Count) return;
            SoundEntry se = (SoundEntry)_library.Items[_dragIndex];
            DataObject data = new DataObject();
            data.SetData(WheelPreview.DragFormat, se.Index.ToString() + "\n" + se.Title);
            _dragStart = Point.Empty;
            _dragIndex = -1;
            _library.DoDragDrop(data, DragDropEffects.Copy);
        }

        private void LibraryDoubleClick(object sender, EventArgs e)
        {
            int idx = _library.SelectedIndex;
            if (idx < 0 || idx >= _library.Items.Count) return;
            SoundEntry se = (SoundEntry)_library.Items[idx];
            int ring = _selectedRing;
            int slot = _selectedSlot;
            if (ring < 0)
            {
                ring = 0;
                slot = -1;
                for (int i = 0; i < _work.InnerCount; i++)
                {
                    if (!_work.Inner[i].IsSet) { slot = i; break; }
                }
                if (slot < 0 && _work.OuterCount > 0)
                {
                    ring = 1;
                    for (int i = 0; i < _work.OuterCount; i++)
                    {
                        if (!_work.Outer[i].IsSet) { slot = i; break; }
                    }
                }
            }
            if (slot < 0) { _libraryHint.Text = "轮盘已经装满，先清空一个扇区再试。"; return; }
            AssignFromLibrary(se, ring, slot);
        }

        private void LibraryDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _library.Items.Count) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme t = Theme.Current;
            using (SolidBrush bg = new SolidBrush(t.Card)) g.FillRectangle(bg, e.Bounds);
            Rectangle r = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 3, e.Bounds.Width - 8, e.Bounds.Height - 6);
            bool sel = (e.State & DrawItemState.Selected) != 0;
            if (sel) Ui.FillRound(g, r, 8f * _s, t.AccentSoft);
            else if (_hoverIndex == e.Index) Ui.FillRound(g, r, 8f * _s, t.CardHover);
            SoundEntry se = (SoundEntry)_library.Items[e.Index];
            RectangleF badge = new RectangleF(r.X + 8f, r.Y + (r.Height - 22f * _s) / 2f, 28f * _s, 22f * _s);
            Ui.FillRound(g, badge, 6f * _s, sel ? Ui.Blend(t.Accent, t.Card, 0.55) : (t.Dark ? Color.FromArgb(44, 50, 63) : Color.FromArgb(238, 242, 249)));
            Ui.Text(g, se.Index.ToString(), _fontTiny, sel ? t.Accent : t.TextDim, badge, StringAlignment.Center, StringAlignment.Center);
            string title = string.IsNullOrEmpty(se.Title) ? "（未命名）" : se.Title;
            Ui.Text(g, title, _fontBody, t.Text, new RectangleF(badge.Right + 9f, r.Y, r.Width - badge.Width - 78f, r.Height), StringAlignment.Near, StringAlignment.Center);
            if (!string.IsNullOrEmpty(se.Duration))
                Ui.Text(g, se.Duration, _fontTiny, t.TextFaint, new RectangleF(r.Right - 66f, r.Y, 58f, r.Height), StringAlignment.Far, StringAlignment.Center);
        }

        private void FillLibrary()
        {
            if (_library == null) return;
            _library.BeginUpdate();
            _library.Items.Clear();
            for (int i = 0; i < _sounds.Length; i++) _library.Items.Add(_sounds[i]);
            _library.EndUpdate();
            _library.Invalidate();
            if (_statusLabel != null)
            {
                if (_sounds.Length > 0) _statusLabel.Text = "共 " + _sounds.Length + " 个音效";
                else _statusLabel.Text = "尚未读取音效列表";
            }
        }

        private void OnRefresh()
        {
            if (_busy || _client == null) return;
            _busy = true;
            if (_refreshButton != null) _refreshButton.Enabled = false;
            if (_statusLabel != null) _statusLabel.Text = "正在读取 Soundpad 音效…";
            Thread t = new Thread(delegate()
            {
                SoundEntry[] list = null;
                string err = null;
                try
                {
                    list = _client.FetchSounds(5000);
                    err = _client.LastError;
                }
                catch (Exception ex) { err = ex.Message; }
                SoundEntry[] final = list;
                string ferr = err;
                try
                {
                    BeginInvoke((MethodInvoker)delegate { OnRefreshDone(final, ferr); });
                }
                catch (Exception) { }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void OnRefreshDone(SoundEntry[] list, string err)
        {
            _busy = false;
            if (_refreshButton != null) _refreshButton.Enabled = true;
            if (IsDisposed) return;
            if (list != null && list.Length > 0)
            {
                _sounds = list;
                _client.Sounds = list;
                try { SoundCache.Save(AppPaths.SoundsFile, list); }
                catch (Exception) { }
                FillLibrary();
                if (_statusLabel != null) _statusLabel.Text = "已连接 Soundpad，共 " + list.Length + " 个音效";
            }
            else
            {
                if (_statusLabel != null)
                    _statusLabel.Text = "读取失败：" + (string.IsNullOrEmpty(err) ? "Soundpad 未运行？" : err);
            }
        }

        private void OnCountChanged()
        {
            _work.InnerCount = (int)_innerCount.Value;
            _work.OuterCount = (int)_outerCount.Value;
            _work.Normalize();
            _preview.RefreshData();
            _themePreview.RefreshData();
            if (_selectedRing == 1 && _selectedSlot >= _work.OuterCount) { _selectedRing = -1; _selectedSlot = -1; }
            if (_selectedRing == 0 && _selectedSlot >= _work.InnerCount) { _selectedRing = -1; _selectedSlot = -1; }
        }

        private void PickCustomAccent()
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.FullOpen = true;
                dlg.Color = ColorUtil.Parse(_workAccent, Theme.Current.Accent);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _workAccent = ColorUtil.ToHex(dlg.Color);
                    ApplyLiveTheme();
                }
            }
        }

        private void ApplyLiveTheme()
        {
            Theme.Current = Theme.Build(_workThemeMode, _workAccent);
            Theme t = Theme.Current;
            BackColor = t.WindowBg;
            _sidebar.BackColor = t.SidebarBg;
            _footer.BackColor = t.Surface;
            _content.BackColor = t.WindowBg;
            Recolor(this);
            if (_library != null && _library.IsHandleCreated) Native.UseDarkScrollbars(_library.Handle, t.Dark);
            UpdateThemeCaption();
            if (_accentHexLabel != null) _accentHexLabel.Text = _workAccent.ToUpperInvariant();
            if (_swatches != null)
            {
                for (int i = 0; i < _swatches.Length; i++)
                {
                    _swatches[i].Selected = string.Equals(Theme.AccentPresets[i], _workAccent, StringComparison.OrdinalIgnoreCase);
                    _swatches[i].Invalidate();
                }
            }
            if (_preview != null) _preview.RefreshData();
            if (_themePreview != null) _themePreview.RefreshData();
            Invalidate(true);
        }

        private void UpdateThemeCaption()
        {
            if (_themeCaption == null) return;
            string mode = _workThemeMode == 1 ? "深色" : (_workThemeMode == 2 ? "浅色" : (Theme.SystemIsDark() ? "跟随系统（深色）" : "跟随系统（浅色）"));
            _themeCaption.Text = mode + " · " + _workAccent.ToUpperInvariant();
        }

        private void Recolor(Control parent)
        {
            Theme t = Theme.Current;
            foreach (Control c in parent.Controls)
            {
                if (c is Card)
                {
                    c.BackColor = t.Card;
                    c.Invalidate();
                }
                else if (c is Label)
                {
                    c.BackColor = c.Parent != null ? c.Parent.BackColor : t.WindowBg;
                    string kind = c.Tag as string;
                    c.ForeColor = kind == "dim" ? t.TextDim : (kind == "accent" ? t.Accent : (kind == "danger" ? t.Danger : (kind == "faint" ? t.TextFaint : t.Text)));
                }
                else if (c is RoundedCheck)
                {
                    c.BackColor = c.Parent != null ? c.Parent.BackColor : t.Card;
                    c.Invalidate();
                }
                else if (c is WheelPreview)
                {
                    c.BackColor = c.Parent != null ? c.Parent.BackColor : t.Card;
                    c.Invalidate();
                }
                else if (c is Slider || c is NumberBox)
                {
                    c.BackColor = c.Parent != null ? c.Parent.BackColor : t.Card;
                    c.Invalidate();
                }
                else if (c is SegmentedControl || c is PaletteSwatch)
                {
                    c.BackColor = c.Parent != null ? c.Parent.BackColor : t.Card;
                    c.Invalidate();
                }
                else if (c is RoundedButton)
                {
                    c.Invalidate();
                }
                else if (c is ListBox)
                {
                    c.BackColor = t.Card;
                    c.ForeColor = t.Text;
                    c.Invalidate();
                }
                else if (c is TextBox || c is ComboBox || c is NumericUpDown)
                {
                    c.BackColor = t.Dark ? Color.FromArgb(24, 28, 36) : Color.White;
                    c.ForeColor = t.Text;
                }
                else if (c is TrackBar)
                {
                    c.BackColor = t.Card;
                }
                else if (c is Panel)
                {
                    if (c == _sidebar) c.BackColor = t.SidebarBg;
                    else if (c == _footer) c.BackColor = t.Surface;
                    else c.BackColor = t.WindowBg;
                }
                if (c.Controls != null && c.Controls.Count > 0) Recolor(c);
            }
        }

        private void OnProbeMouse()
        {
            if (_probeLabel == null) return;
            bool hidden = Native.CursorHidden();
            bool clipped = Native.CursorClipped();
            string info = "光标：" + (hidden ? "已隐藏（游戏在接管）" : "可见") + "\r\n" +
                          "裁剪区域：" + Native.ClipRectText() + "\r\n" +
                          "原始输入：" + (_work.CaptureMouse ? "接管鼠标已开启" : "接管鼠标已关闭") + "\r\n" +
                          "数据目录：" + AppPaths.DataDir;
            _probeLabel.Text = info;
        }

        private void OnCheckUpdate()
        {
            if (_checkButton == null) return;
            _checkButton.Enabled = false;
            _updateStatus.Text = "正在检查更新…";
            _updateStatus.ForeColor = Theme.Current.TextDim;
            Thread t = new Thread(delegate()
            {
                UpdateInfo info = UpdateService.Latest(8000);
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed) return;
                        _checkButton.Enabled = true;
                        if (info == null)
                        {
                            _updateStatus.Text = "检查失败：无法连接 GitHub。可以稍后再试。";
                            _updateStatus.ForeColor = Theme.Current.Danger;
                            return;
                        }
                        if (info.Version > UpdateService.CurrentVersion)
                        {
                            _latestVersion = info.Version;
                            _latestSetupUrl = info.SetupUrl;
                            _updateStatus.Text = "发现新版本 v" + info.Version.ToString(3) + "，当前是 v" + UpdateService.CurrentVersion.ToString(3) + "。";
                            _updateStatus.ForeColor = Theme.Current.Accent;
                            _downloadButton.Enabled = true;
                        }
                        else
                        {
                            _updateStatus.Text = "已是最新版本（v" + UpdateService.CurrentVersion.ToString(3) + "）。";
                            _updateStatus.ForeColor = Theme.Current.Ok;
                        }
                    });
                }
                catch (Exception) { }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void OnDownloadUpdate()
        {
            if (string.IsNullOrEmpty(_latestSetupUrl)) return;
            _downloadButton.Enabled = false;
            _updateStatus.Text = "正在下载安装包…";
            Thread t = new Thread(delegate()
            {
                string path = Path.Combine(Path.GetTempPath(), "SoundWheelSetup-" + _latestVersion.ToString(3) + ".exe");
                string error;
                bool ok = UpdateService.Download(_latestSetupUrl, path, 120000, out error);
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed) return;
                        if (!ok)
                        {
                            _updateStatus.Text = "下载失败：" + error;
                            _updateStatus.ForeColor = Theme.Current.Danger;
                            _downloadButton.Enabled = true;
                            return;
                        }
                        _updateStatus.Text = "安装包已下载，正在启动安装向导…";
                        _updateStatus.ForeColor = Theme.Current.Ok;
                        try { Process.Start(path); }
                        catch (Exception ex) { _updateStatus.Text = "无法启动安装包：" + ex.Message; _downloadButton.Enabled = true; }
                    });
                }
                catch (Exception) { }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void OnSave()
        {
            if (_hotkeyBox.Spec == null || !_hotkeyBox.Spec.IsValid)
            {
                _footerHint.Text = "请先设置一个有效的快捷键（例如 Ctrl + Alt + W）。";
                _footerHint.ForeColor = Theme.Current.Danger;
                _hotkeyBox.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        public void ApplyTo(AppConfig target)
        {
            target.Hotkey = _hotkeyBox.Spec != null ? _hotkeyBox.Spec.Clone() : HotkeySpec.Default;
            target.ToggleMode = _toggleCheck.Checked;
            target.PlayToMic = _micCheck.Checked;
            target.PlayToSpeaker = _speakerCheck.Checked;
            target.ShowToast = _toastCheck.Checked;
            target.Animations = _animCheck.Checked;
            target.AutoStart = _autoStartCheck.Checked;
            target.Scale = _scaleBar.Value / 100.0;
            target.InnerCount = (int)_innerCount.Value;
            target.OuterCount = (int)_outerCount.Value;
            target.ThemeMode = _workThemeMode;
            target.Accent = _workAccent;
            target.FontScale = _fontBar.Value / 100.0;
            target.MouseMode = _mouseMode.SelectedIndex >= 0 ? _mouseMode.SelectedIndex : 0;
            target.MouseSensitivity = (double)_sensitivity.Value;
            target.CaptureMouse = _captureCheck.Checked;
            target.Inner = new List<SlotConfig>();
            for (int i = 0; i < _work.Inner.Count; i++) target.Inner.Add(_work.Inner[i].Clone());
            target.Outer = new List<SlotConfig>();
            for (int i = 0; i < _work.Outer.Count; i++) target.Outer.Add(_work.Outer[i].Clone());
            target.Normalize();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateChrome();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateChrome();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateChrome();
        }

        private void UpdateChrome()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            using (GraphicsPath path = Ui.Round(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), 16f * _s))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Ui.Round(new RectangleF(0.5f, 0.5f, ClientSize.Width - 1f, ClientSize.Height - 1f), 16f * _s))
            using (Pen p = new Pen(Theme.Current.Border, 1f))
                e.Graphics.DrawPath(p, path);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84) // WM_NCHITTEST
            {
                if ((int)m.Result == 1) // HTCLIENT
                {
                    int lp = unchecked((int)(long)m.LParam);
                    Point screen = new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF));
                    Point client = PointToClient(screen);
                    if (client.Y >= 0 && client.Y < S(46) && client.X >= 0 && client.X < S(210))
                        m.Result = (IntPtr)2; // HTCAPTION
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (DialogResult != DialogResult.OK) Theme.Current = _originalTheme;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_fontH1 != null) _fontH1.Dispose();
                if (_fontH2 != null) _fontH2.Dispose();
                if (_fontBody != null) _fontBody.Dispose();
                if (_fontBodyBold != null) _fontBodyBold.Dispose();
                if (_fontSmall != null) _fontSmall.Dispose();
                if (_fontTiny != null) _fontTiny.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
