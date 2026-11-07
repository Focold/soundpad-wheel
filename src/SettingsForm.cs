// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace SoundWheel
{
    internal sealed class SoundOption
    {
        public int Index;
        public string Title = "";

        public SoundOption() { }

        public SoundOption(SoundEntry e)
        {
            Index = e.Index;
            Title = e.Title;
        }

        public override string ToString()
        {
            if (Index <= 0) return "（空）";
            string title = string.IsNullOrEmpty(Title) ? "未命名" : Title;
            return Index + "  ·  " + title;
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly AppConfig _config;
        private readonly SoundpadClient _client;

        private HotkeyBox _hotkey;
        private CheckBox _toggle;
        private CheckBox _mic;
        private CheckBox _speaker;
        private CheckBox _toast;
        private CheckBox _animations;
        private CheckBox _autoStart;
        private TrackBar _scale;
        private Label _scaleLabel;
        private NumericUpDown _innerCount;
        private NumericUpDown _outerCount;
        private Panel _swatch;
        private Label _status;
        private Label _statusLibrary;
        private ListBox _soundList;
        private ComboBox _mouseMode;
        private NumericUpDown _mouseSensitivity;
        private CheckBox _capture;
        private Label _mouseProbe;
        private bool _busy;
        private float _dpiScale = 1f;

        private readonly ComboBox[] _inner = new ComboBox[12];
        private readonly ComboBox[] _outer = new ComboBox[16];
        private readonly Label[] _innerLabels = new Label[12];
        private readonly Label[] _outerLabels = new Label[16];
        private readonly FlowLayoutPanel _innerFlow = new FlowLayoutPanel();
        private readonly FlowLayoutPanel _outerFlow = new FlowLayoutPanel();

        public event Action PreviewRequested;

        public SettingsForm(AppConfig config, SoundpadClient client)
        {
            _config = config;
            _client = client;

            Text = "SoundWheel 设置";
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.FromArgb(250, 250, 252);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(1000, 660);
            KeyPreview = true;

            BuildUi();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyDpiScale();
        }

        private void ApplyDpiScale()
        {
            float s;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                s = g.DpiX / 96f;
            if (s < 1.02f) return;

            _dpiScale = s;
            SuspendLayout();
            ClearAnchors(Controls);
            ScaleTree(Controls, s);
            ClientSize = new Size((int)(1000 * s), (int)(660 * s));
            ResumeLayout(true);
        }

        // 手动缩放下不能让 WinForms 的锚点再叠加一次偏移（否则右侧按钮会跑出窗口）
        private static void ClearAnchors(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                ClearAnchors(c.Controls);
            }
        }

        private static void ScaleTree(Control.ControlCollection controls, float s)
        {
            foreach (Control c in controls)
            {
                Point loc = c.Location;
                Size size = c.Size;
                c.Location = new Point((int)(loc.X * s), (int)(loc.Y * s));
                c.Size = new Size((int)(size.Width * s), (int)(size.Height * s));
                ScaleTree(c.Controls, s);
            }
        }

        private void BuildUi()
        {
            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            tabs.Padding = new Point(14, 5);

            TabPage general = new TabPage("常规");
            general.BackColor = Color.FromArgb(250, 250, 252);
            general.AutoScroll = true;
            BuildGeneral(general);

            TabPage layout = new TabPage("轮盘布局");
            layout.BackColor = Color.FromArgb(250, 250, 252);
            BuildLayout(layout);

            TabPage library = new TabPage("音效库");
            library.BackColor = Color.FromArgb(250, 250, 252);
            BuildLibrary(library);

            TabPage mouse = new TabPage("游戏与鼠标");
            mouse.BackColor = Color.FromArgb(250, 250, 252);
            BuildMouse(mouse);

            TabPage about = new TabPage("关于");
            about.BackColor = Color.FromArgb(250, 250, 252);
            BuildAbout(about);

            tabs.TabPages.Add(general);
            tabs.TabPages.Add(layout);
            tabs.TabPages.Add(library);
            tabs.TabPages.Add(mouse);
            tabs.TabPages.Add(about);

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 60;
            bottom.BackColor = Color.FromArgb(243, 245, 249);

            Button preview = new Button();
            preview.Text = "预览轮盘";
            preview.Location = new Point(18, 14);
            preview.Size = new Size(104, 32);
            preview.FlatStyle = FlatStyle.System;
            preview.Click += delegate
            {
                Action handler = PreviewRequested;
                if (handler != null) handler();
            };

            Button save = new Button();
            save.Name = "save";
            save.Text = "保存";
            save.Location = new Point(782, 14);
            save.Size = new Size(96, 32);
            save.Click += OnSave;

            Button cancel = new Button();
            cancel.Name = "cancel";
            cancel.Text = "取消";
            cancel.Location = new Point(886, 14);
            cancel.Size = new Size(96, 32);
            cancel.DialogResult = DialogResult.Cancel;

            Label hint = new Label();
            hint.Text = "提示：按住快捷键唤出，松开即播放";
            hint.ForeColor = Color.FromArgb(120, 126, 138);
            hint.AutoSize = true;
            hint.Location = new Point(136, 22);

            bottom.Controls.Add(preview);
            bottom.Controls.Add(hint);
            bottom.Controls.Add(save);
            bottom.Controls.Add(cancel);
            bottom.Layout += delegate
            {
                cancel.Left = bottom.ClientSize.Width - (int)Math.Round(18 * _dpiScale) - cancel.Width;
                save.Left = cancel.Left - (int)Math.Round(8 * _dpiScale) - save.Width;
            };

            Controls.Add(tabs);
            Controls.Add(bottom);
            AcceptButton = save;
            CancelButton = cancel;

            FillCombos();
            UpdateSlotStates();
        }

        private void BuildGeneral(TabPage page)
        {
            GroupBox invoke = Group("唤出方式", 18, 14, 470, 148);
            Label hkLabel = new Label();
            hkLabel.Text = "快捷键";
            hkLabel.Location = new Point(16, 34);
            hkLabel.Size = new Size(56, 22);

            _hotkey = new HotkeyBox();
            _hotkey.Location = new Point(78, 29);
            _hotkey.Size = new Size(230, 28);
            _hotkey.Spec = _config.Hotkey == null ? null : _config.Hotkey.Clone();

            Label hkHint = new Label();
            hkHint.Text = "点击输入框后按下组合键（Backspace 清除）";
            hkHint.ForeColor = Color.FromArgb(128, 134, 148);
            hkHint.Location = new Point(16, 66);
            hkHint.Size = new Size(430, 20);

            _toggle = new CheckBox();
            _toggle.Text = "点按模式：按一下打开，左键单击音效后关闭";
            _toggle.Location = new Point(16, 96);
            _toggle.Size = new Size(430, 24);
            _toggle.Checked = _config.ToggleMode;

            invoke.Controls.Add(hkLabel);
            invoke.Controls.Add(_hotkey);
            invoke.Controls.Add(hkHint);
            invoke.Controls.Add(_toggle);

            GroupBox playback = Group("播放", 18, 172, 470, 138);
            _mic = new CheckBox();
            _mic.Text = "发送到麦克风（游戏或语音里能听到）";
            _mic.Location = new Point(16, 28);
            _mic.Size = new Size(430, 24);
            _mic.Checked = _config.PlayToMic;

            _speaker = new CheckBox();
            _speaker.Text = "同时从扬声器播放";
            _speaker.Location = new Point(16, 58);
            _speaker.Size = new Size(430, 24);
            _speaker.Checked = _config.PlayToSpeaker;

            _toast = new CheckBox();
            _toast.Text = "显示播放提示";
            _toast.Location = new Point(16, 88);
            _toast.Size = new Size(430, 24);
            _toast.Checked = _config.ShowToast;

            playback.Controls.Add(_mic);
            playback.Controls.Add(_speaker);
            playback.Controls.Add(_toast);

            GroupBox appearance = Group("外观与布局", 18, 320, 470, 214);
            Label sizeLabel = new Label();
            sizeLabel.Text = "整体大小";
            sizeLabel.Location = new Point(16, 32);
            sizeLabel.Size = new Size(70, 22);

            _scale = new TrackBar();
            _scale.Minimum = 70;
            _scale.Maximum = 160;
            _scale.TickFrequency = 10;
            _scale.SmallChange = 5;
            _scale.LargeChange = 10;
            _scale.Location = new Point(88, 24);
            _scale.Size = new Size(250, 45);
            _scale.Value = (int)Math.Round(_config.Scale * 100.0);
            _scale.ValueChanged += delegate { UpdateScaleLabel(); };

            _scaleLabel = new Label();
            _scaleLabel.Text = "100%";
            _scaleLabel.Location = new Point(346, 34);
            _scaleLabel.Size = new Size(70, 22);

            Label innerLabel = new Label();
            innerLabel.Text = "内圈格数";
            innerLabel.Location = new Point(16, 78);
            innerLabel.Size = new Size(70, 22);

            _innerCount = new NumericUpDown();
            _innerCount.Minimum = 1;
            _innerCount.Maximum = 12;
            _innerCount.Value = _config.InnerCount;
            _innerCount.Location = new Point(88, 74);
            _innerCount.Size = new Size(62, 26);
            _innerCount.ValueChanged += delegate { UpdateSlotStates(); };

            Label outerLabel = new Label();
            outerLabel.Text = "外圈格数";
            outerLabel.Location = new Point(176, 78);
            outerLabel.Size = new Size(70, 22);

            _outerCount = new NumericUpDown();
            _outerCount.Minimum = 0;
            _outerCount.Maximum = 16;
            _outerCount.Value = _config.OuterCount;
            _outerCount.Location = new Point(248, 74);
            _outerCount.Size = new Size(62, 26);
            _outerCount.ValueChanged += delegate { UpdateSlotStates(); };

            Label colorLabel = new Label();
            colorLabel.Text = "主题色";
            colorLabel.Location = new Point(16, 116);
            colorLabel.Size = new Size(56, 22);

            _swatch = new Panel();
            _swatch.Location = new Point(78, 114);
            _swatch.Size = new Size(44, 24);
            _swatch.BackColor = _config.AccentColor;
            _swatch.BorderStyle = BorderStyle.FixedSingle;

            Button pickColor = new Button();
            pickColor.Text = "更改…";
            pickColor.Location = new Point(132, 112);
            pickColor.Size = new Size(76, 28);
            pickColor.Click += OnPickColor;

            _animations = new CheckBox();
            _animations.Text = "动画效果";
            _animations.Location = new Point(230, 114);
            _animations.Size = new Size(180, 24);
            _animations.Checked = _config.Animations;

            appearance.Controls.Add(sizeLabel);
            appearance.Controls.Add(_scale);
            appearance.Controls.Add(_scaleLabel);
            appearance.Controls.Add(innerLabel);
            appearance.Controls.Add(_innerCount);
            appearance.Controls.Add(outerLabel);
            appearance.Controls.Add(_outerCount);
            appearance.Controls.Add(colorLabel);
            appearance.Controls.Add(_swatch);
            appearance.Controls.Add(pickColor);
            appearance.Controls.Add(_animations);

            GroupBox startup = Group("启动与数据", 510, 14, 450, 148);
            _autoStart = new CheckBox();
            _autoStart.Text = "登录 Windows 时自动启动";
            _autoStart.Location = new Point(16, 28);
            _autoStart.Size = new Size(400, 24);
            _autoStart.Checked = _config.AutoStart;

            Label dataLabel = new Label();
            dataLabel.Text = "配置与日志保存在：" + Environment.NewLine + AppPaths.DataDir;
            dataLabel.ForeColor = Color.FromArgb(128, 134, 148);
            dataLabel.Location = new Point(16, 62);
            dataLabel.Size = new Size(410, 60);

            startup.Controls.Add(_autoStart);
            startup.Controls.Add(dataLabel);

            GroupBox tips = Group("使用说明", 510, 172, 450, 362);
            Label tipLabel = new Label();
            tipLabel.Location = new Point(16, 28);
            tipLabel.Size = new Size(418, 320);
            tipLabel.Text = "① 按住快捷键唤出轮盘，轮盘出现在鼠标位置。" + Environment.NewLine + Environment.NewLine +
                "② 移动鼠标高亮音效，松开快捷键立即播放。" + Environment.NewLine + Environment.NewLine +
                "③ 右键或 Esc 取消，不会播放。" + Environment.NewLine + Environment.NewLine +
                "④ 点按模式下：按一下打开，左键单击播放。" + Environment.NewLine + Environment.NewLine +
                "⑤ 内圈装 1–12 个音效，外圈装 0–16 个；在『轮盘布局』里分配。" + Environment.NewLine + Environment.NewLine +
                "⑥ 使用前请启动 Soundpad，并允许远程控制（Remote Control）。";
            tips.Controls.Add(tipLabel);

            page.Controls.Add(invoke);
            page.Controls.Add(playback);
            page.Controls.Add(appearance);
            page.Controls.Add(startup);
            page.Controls.Add(tips);
        }

        private void BuildLayout(TabPage page)
        {
            Button refresh = new Button();
            refresh.Text = "刷新音效列表";
            refresh.Location = new Point(18, 12);
            refresh.Size = new Size(124, 30);
            refresh.Click += OnRefresh;

            _status = new Label();
            _status.Text = "音效数量：" + _client.Sounds.Length;
            _status.Location = new Point(156, 18);
            _status.Size = new Size(700, 22);
            _status.ForeColor = Color.FromArgb(120, 126, 138);

            GroupBox innerBox = Group("内圈（1–12）", 18, 52, 466, 540);
            _innerFlow.Dock = DockStyle.Fill;
            _innerFlow.FlowDirection = FlowDirection.TopDown;
            _innerFlow.WrapContents = false;
            _innerFlow.AutoScroll = true;
            _innerFlow.Padding = new Padding(10, 8, 10, 8);
            innerBox.Controls.Add(_innerFlow);

            GroupBox outerBox = Group("外圈（0–16）", 500, 52, 466, 540);
            _outerFlow.Dock = DockStyle.Fill;
            _outerFlow.FlowDirection = FlowDirection.TopDown;
            _outerFlow.WrapContents = false;
            _outerFlow.AutoScroll = true;
            _outerFlow.Padding = new Padding(10, 8, 10, 8);
            outerBox.Controls.Add(_outerFlow);

            for (int i = 0; i < _inner.Length; i++)
                _inner[i] = MakeRow(_innerFlow, i + 1, out _innerLabels[i]);
            for (int i = 0; i < _outer.Length; i++)
                _outer[i] = MakeRow(_outerFlow, i + 1, out _outerLabels[i]);

            page.Controls.Add(refresh);
            page.Controls.Add(_status);
            page.Controls.Add(innerBox);
            page.Controls.Add(outerBox);
        }

        private ComboBox MakeRow(FlowLayoutPanel flow, int number, out Label label)
        {
            Panel row = new Panel();
            row.Size = new Size(420, 32);
            row.Margin = new Padding(0, 0, 0, 3);

            Label l = new Label();
            l.Text = number.ToString();
            l.Location = new Point(0, 7);
            l.Size = new Size(34, 22);
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.ForeColor = Color.FromArgb(110, 116, 130);
            label = l;

            ComboBox combo = new ComboBox();
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Location = new Point(36, 3);
            combo.Size = new Size(370, 26);
            combo.MaxDropDownItems = 22;
            combo.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

            row.Controls.Add(l);
            row.Controls.Add(combo);
            flow.Controls.Add(row);
            return combo;
        }

        private void BuildLibrary(TabPage page)
        {
            Button refresh = new Button();
            refresh.Text = "刷新列表";
            refresh.Location = new Point(852, 12);
            refresh.Size = new Size(112, 30);
            refresh.Click += OnRefresh;

            _statusLibrary = new Label();
            _statusLibrary.Text = "读取 Soundpad 的 All sounds 分类";
            _statusLibrary.Location = new Point(18, 18);
            _statusLibrary.Size = new Size(800, 22);
            _statusLibrary.ForeColor = Color.FromArgb(120, 126, 138);

            _soundList = new ListBox();
            _soundList.Location = new Point(18, 54);
            _soundList.Size = new Size(946, 500);
            _soundList.IntegralHeight = false;

            page.Layout += delegate
            {
                refresh.Left = page.ClientSize.Width - (int)Math.Round(36 * _dpiScale) - refresh.Width;
                int w = page.ClientSize.Width - (int)Math.Round(18 * _dpiScale) - (int)Math.Round(36 * _dpiScale);
                int h = page.ClientSize.Height - (int)Math.Round(54 * _dpiScale) - (int)Math.Round(46 * _dpiScale);
                if (w > 120) _soundList.Width = w;
                if (h > 80) _soundList.Height = h;
            };

            page.Controls.Add(refresh);
            page.Controls.Add(_statusLibrary);
            page.Controls.Add(_soundList);
            FillSoundList();
        }

        private void BuildMouse(TabPage page)
        {
            GroupBox modeBox = Group("鼠标跟踪", 18, 14, 470, 286);

            Label modeLabel = new Label();
            modeLabel.Text = "方式";
            modeLabel.Location = new Point(16, 36);
            modeLabel.Size = new Size(56, 22);

            _mouseMode = new ComboBox();
            _mouseMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _mouseMode.Location = new Point(80, 32);
            _mouseMode.Size = new Size(360, 26);
            _mouseMode.Items.Add("自动判断（推荐）");
            _mouseMode.Items.Add("绝对坐标（桌面程序）");
            _mouseMode.Items.Add("相对位移（游戏 / 锁定鼠标）");
            int mode = _config.MouseMode;
            if (mode < 0 || mode > 2) mode = 0;
            _mouseMode.SelectedIndex = mode;

            Label sensLabel = new Label();
            sensLabel.Text = "灵敏度";
            sensLabel.Location = new Point(16, 82);
            sensLabel.Size = new Size(56, 22);

            _mouseSensitivity = new NumericUpDown();
            _mouseSensitivity.Location = new Point(80, 78);
            _mouseSensitivity.Size = new Size(90, 26);
            _mouseSensitivity.DecimalPlaces = 1;
            _mouseSensitivity.Increment = 0.1m;
            _mouseSensitivity.Minimum = 0.2m;
            _mouseSensitivity.Maximum = 4.0m;
            decimal sens = (decimal)_config.MouseSensitivity;
            if (sens < 0.2m) sens = 0.2m;
            if (sens > 4.0m) sens = 4.0m;
            _mouseSensitivity.Value = sens;

            _capture = new CheckBox();
            _capture.Text = "打开轮盘时接管鼠标（点击 / 移动不再传给游戏）";
            _capture.Location = new Point(16, 112);
            _capture.Size = new Size(434, 24);
            _capture.Checked = _config.CaptureMouse;

            Label modeHint = new Label();
            modeHint.Location = new Point(16, 144);
            modeHint.Size = new Size(434, 128);
            modeHint.ForeColor = Color.FromArgb(110, 116, 130);
            modeHint.Text = "钩子打开时，轮盘的指针跟着鼠标走，游戏的视角不会转、也不会开枪；" +
                "松开热键（或点一下）即播放。" + Environment.NewLine +
                Environment.NewLine +
                "自动判断：检查光标是否被隐藏、被限制在屏幕中心、是否收到原始位移，发现游戏接管了就切到相对位移。" + Environment.NewLine +
                Environment.NewLine +
                "灵敏度越大，鼠标移动同样的距离，轮盘上的指针走得更快。";

            modeBox.Controls.Add(modeLabel);
            modeBox.Controls.Add(_mouseMode);
            modeBox.Controls.Add(sensLabel);
            modeBox.Controls.Add(_mouseSensitivity);
            modeBox.Controls.Add(_capture);
            modeBox.Controls.Add(modeHint);

            GroupBox probeBox = Group("自检", 510, 14, 450, 286);

            Button probe = new Button();
            probe.Text = "检测鼠标状态";
            probe.Location = new Point(16, 32);
            probe.Size = new Size(128, 30);
            probe.FlatStyle = FlatStyle.System;
            probe.Click += OnProbeMouse;

            _mouseProbe = new Label();
            _mouseProbe.Location = new Point(16, 74);
            _mouseProbe.Size = new Size(418, 196);
            _mouseProbe.ForeColor = Color.FromArgb(80, 88, 104);
            _mouseProbe.Text = "在目标程序（例如游戏）里打开轮盘后点这里，可以查看系统光标是否被隐藏或锁定。";

            probeBox.Controls.Add(probe);
            probeBox.Controls.Add(_mouseProbe);

            GroupBox tipsBox = Group("说明", 18, 316, 942, 206);

            Label tipsLabel = new Label();
            tipsLabel.Location = new Point(16, 26);
            tipsLabel.Size = new Size(910, 164);
            tipsLabel.Text = "① 接管鼠标后，轮盘打开期间的移动和点击都只作用于轮盘：游戏不会开枪，也不会跟着转视角。" + Environment.NewLine +
                Environment.NewLine +
                "② 仍有个别游戏直接读原始输入（Raw Input）来转视角，这类游戏可能还会跟着动；" +
                "此时优先用『按住模式』唤出并快速甩到目标扇区，或在游戏里改用窗口化模式。" + Environment.NewLine +
                Environment.NewLine +
                "③ 如果游戏或反作弊屏蔽了原始输入、或不允许安装鼠标钩子，接管会失败并退回普通跟踪；" +
                "可以关掉『接管鼠标』再试，或在『常规』页改用点按模式。";

            tipsBox.Controls.Add(tipsLabel);

            page.Controls.Add(modeBox);
            page.Controls.Add(probeBox);
            page.Controls.Add(tipsBox);
        }

        private void OnProbeMouse(object sender, EventArgs e)
        {
            bool hidden = Native.CursorHidden();
            bool clipped = Native.CursorClipped();
            _mouseProbe.Text = "系统光标：" + (hidden ? "隐藏（程序可能正在锁定鼠标）" : "显示")
                + Environment.NewLine
                + "裁剪矩形：" + Native.ClipRectText()
                + Environment.NewLine
                + "锁定状态：" + (clipped ? "已锁定到屏幕中心（建议用相对位移）" : "未锁定")
                + Environment.NewLine
                + "数据目录：" + AppPaths.DataDir;
        }

        private void BuildAbout(TabPage page)
        {
            Label about = new Label();
            about.Location = new Point(24, 24);
            about.Size = new Size(920, 500);
            about.Text = "SoundWheel 1.2" + Environment.NewLine + Environment.NewLine +
                "Soundpad 音效轮盘：按住热键在鼠标位置弹出轮盘，移动鼠标选择音效，松开即播放。" +
                Environment.NewLine + Environment.NewLine +
                "本程序通过 Soundpad 官方远程控制接口（命名管道）工作，不修改 Soundpad 安装文件。" +
                Environment.NewLine + Environment.NewLine +
                "数据目录：" + AppPaths.DataDir +
                Environment.NewLine + Environment.NewLine +
                "如果快捷键无效，请检查是否与其他程序冲突，或在设置里换一个组合键。";
            page.Controls.Add(about);
        }

        private static GroupBox Group(string text, int x, int y, int width, int height)
        {
            GroupBox box = new GroupBox();
            box.Text = text;
            box.Location = new Point(x, y);
            box.Size = new Size(width, height);
            return box;
        }

        private void UpdateScaleLabel()
        {
            _scaleLabel.Text = _scale.Value + "%";
        }

        private void UpdateSlotStates()
        {
            int innerCount = (int)_innerCount.Value;
            int outerCount = (int)_outerCount.Value;
            for (int i = 0; i < _inner.Length; i++)
            {
                bool on = i < innerCount;
                _inner[i].Enabled = on;
                _innerLabels[i].ForeColor = on ? Color.FromArgb(110, 116, 130) : Color.FromArgb(196, 200, 210);
            }
            for (int i = 0; i < _outer.Length; i++)
            {
                bool on = i < outerCount;
                _outer[i].Enabled = on;
                _outerLabels[i].ForeColor = on ? Color.FromArgb(110, 116, 130) : Color.FromArgb(196, 200, 210);
            }
        }

        private void OnPickColor(object sender, EventArgs e)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.Color = _swatch.BackColor;
                dialog.FullOpen = true;
                dialog.CustomColors = new int[] { 0x5B8CFF, 0xFF6B8C, 0x35D0A5, 0xFFB25B };
                if (dialog.ShowDialog(this) == DialogResult.OK) _swatch.BackColor = dialog.Color;
            }
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (_hotkey.Spec == null || !_hotkey.Spec.IsValid)
            {
                MessageBox.Show(this, "请先设置一个唤出快捷键。", "SoundWheel",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            if (_busy) return;
            _busy = true;
            SetStatus("正在读取音效列表…", false);
            Thread thread = new Thread(delegate()
            {
                SoundEntry[] list = _client.FetchSounds(4000);
                try
                {
                    BeginInvoke((MethodInvoker)delegate { OnRefreshed(list); });
                }
                catch (Exception) { }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void OnRefreshed(SoundEntry[] list)
        {
            _busy = false;
            if (IsDisposed) return;
            if (list == null)
            {
                string error = string.IsNullOrEmpty(_client.LastError) ? "未知错误" : _client.LastError;
                SetStatus("读取失败：" + error + "（请确认 Soundpad 已启动并允许远程控制）", true);
                return;
            }
            SoundCache.Save(AppPaths.SoundsFile, list);
            FillCombos();
            FillSoundList();
            SetStatus("已连接 Soundpad，共 " + list.Length + " 个音效", false);
        }

        private void SetStatus(string text, bool error)
        {
            Color color = error ? Color.FromArgb(196, 68, 68) : Color.FromArgb(110, 150, 120);
            if (_status != null) { _status.Text = text; _status.ForeColor = color; }
            if (_statusLibrary != null) { _statusLibrary.Text = text; _statusLibrary.ForeColor = color; }
        }

        private void FillSoundList()
        {
            if (_soundList == null) return;
            _soundList.BeginUpdate();
            _soundList.Items.Clear();
            foreach (SoundEntry entry in _client.Sounds)
                _soundList.Items.Add(entry.Display);
            if (_soundList.Items.Count == 0)
                _soundList.Items.Add("（暂无缓存，点击右上角『刷新列表』从 Soundpad 读取）");
            _soundList.EndUpdate();
        }

        private void FillCombos()
        {
            for (int i = 0; i < _inner.Length; i++)
                FillCombo(_inner[i], i < _config.Inner.Count ? _config.Inner[i] : null);
            for (int i = 0; i < _outer.Length; i++)
                FillCombo(_outer[i], i < _config.Outer.Count ? _config.Outer[i] : null);
        }

        private void FillCombo(ComboBox combo, SlotConfig slot)
        {
            SoundOption keep = combo.SelectedItem as SoundOption;
            combo.BeginUpdate();
            combo.Items.Clear();
            combo.Items.Add(new SoundOption());
            foreach (SoundEntry entry in _client.Sounds)
                combo.Items.Add(new SoundOption(entry));

            int selected = 0;
            if (keep != null && keep.Index > 0)
            {
                selected = FindOption(combo, keep.Index, keep.Title);
            }
            else if (slot != null && slot.IsSet)
            {
                selected = FindOption(combo, slot.Index, slot.Title);
            }
            combo.SelectedIndex = selected < 0 ? 0 : selected;
            combo.EndUpdate();
        }

        private static int FindOption(ComboBox combo, int index, string title)
        {
            int byTitle = -1;
            int byIndex = -1;
            for (int i = 1; i < combo.Items.Count; i++)
            {
                SoundOption option = combo.Items[i] as SoundOption;
                if (option == null) continue;
                if (byTitle < 0 && !string.IsNullOrEmpty(title) && option.Title == title) byTitle = i;
                if (byIndex < 0 && index > 0 && option.Index == index) byIndex = i;
            }
            return byTitle >= 0 ? byTitle : byIndex;
        }

        public void ApplyTo(AppConfig config)
        {
            config.Hotkey = _hotkey.Spec == null ? null : _hotkey.Spec.Clone();
            config.ToggleMode = _toggle.Checked;
            config.PlayToMic = _mic.Checked;
            config.PlayToSpeaker = _speaker.Checked;
            config.ShowToast = _toast.Checked;
            config.AutoStart = _autoStart.Checked;
            config.Animations = _animations.Checked;
            config.Scale = _scale.Value / 100.0;
            config.InnerCount = (int)_innerCount.Value;
            config.OuterCount = (int)_outerCount.Value;
            config.Accent = ColorUtil.ToHex(_swatch.BackColor);
            config.MouseMode = _mouseMode.SelectedIndex < 0 ? 0 : _mouseMode.SelectedIndex;
            config.MouseSensitivity = (double)_mouseSensitivity.Value;
            config.CaptureMouse = _capture.Checked;

            for (int i = 0; i < _inner.Length; i++)
            {
                SlotConfig slot = config.Inner[i];
                ApplyOption(slot, _inner[i]);
            }
            for (int i = 0; i < _outer.Length; i++)
            {
                SlotConfig slot = config.Outer[i];
                ApplyOption(slot, _outer[i]);
            }
            config.Normalize();
        }

        private static void ApplyOption(SlotConfig slot, ComboBox combo)
        {
            SoundOption option = combo.SelectedItem as SoundOption;
            if (option == null || option.Index <= 0)
            {
                slot.Index = 0;
                slot.Title = "";
                return;
            }
            slot.Index = option.Index;
            slot.Title = option.Title;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateScaleLabel();
            UpdateSlotStates();
            if (_client.Sounds.Length == 0) OnRefresh(this, EventArgs.Empty);
        }
    }
}
