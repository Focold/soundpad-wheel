// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SoundWheel
{
    internal sealed class WheelItem
    {
        public string Title = "";
        public bool Assigned;
        public int Index;
    }

    internal sealed class WheelGeometry
    {
        public float Scale = 1f;
        public float HubR;
        public float InnerIn;
        public float InnerOut;
        public float OuterIn;
        public float OuterOut;
        public float Margin;
        public int Size;
        public int InnerCount;
        public int OuterCount;

        public float Center { get { return Size / 2f; } }

        public void Build(float scale, int innerCount, int outerCount)
        {
            Scale = scale;
            HubR = 54f * scale;
            InnerIn = 62f * scale;
            InnerOut = 150f * scale;
            OuterIn = 159f * scale;
            OuterOut = 247f * scale;
            Margin = 26f * scale;
            InnerCount = innerCount;
            OuterCount = outerCount;
            float maxR = outerCount > 0 ? OuterOut : InnerOut;
            Size = (int)Math.Ceiling((maxR + Margin) * 2f);
        }
    }

    internal sealed class WheelWindow : Form
    {
        private readonly AppConfig _config;
        private readonly Timer _timer = new Timer();
        private readonly WheelGeometry _geo = new WheelGeometry();
        private List<WheelItem> _inner = new List<WheelItem>();
        private List<WheelItem> _outer = new List<WheelItem>();
        private Bitmap _canvas;
        private Rectangle _bounds;
        private int _hoverRing = -1;
        private int _hoverSlot = -1;
        private float _anim;
        private int _animDir;
        private float _animSpeed;
        private float _alpha = 1f;
        private bool _closing;
        private bool _shown;
        private bool _trackHold;
        private bool _preview;
        private DateTime _previewUntil = DateTime.MinValue;
        private DateTime _lastTick;
        private DateTime _openedAt;
        private Point _openCursor;
        private bool _armed;
        private bool _seenDown;
        private bool _toggleFallback;
        private short _lastRaw = -1;
        internal static bool KeyDebug;
        internal static bool MouseDebug;
        internal static bool FakeRaw;
        private bool _escDown;
        private bool _relative;
        private float _vx;
        private float _vy;
        private float _rawAccum;
        private bool _rawWarned;
        private bool _rawRegistered;
        private RawMouseWindow _rawWindow;
        private Point _lastCursorSample;
        private DateTime _lastCursorChange;

        public event Action<WheelItem> PlayRequested;

        public WheelWindow(AppConfig config)
        {
            _config = config;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            TopMost = true;
            BackColor = Color.Black;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            _timer.Interval = 15;
            _timer.Tick += OnTick;

            IntPtr h = Handle;
            Native.HideWindow(h);
        }

        public bool IsOpen
        {
            get { return _shown; }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        public void OpenAt(Point anchor, bool trackHold, bool preview, int previewSeconds)
        {
            if (_shown) return;

            _trackHold = trackHold;
            _preview = preview;
            _previewUntil = preview ? DateTime.UtcNow.AddSeconds(Math.Max(1, previewSeconds)) : DateTime.MinValue;

            BuildItems();
            float dpiScale = Native.GetDpiScale(anchor);
            _geo.Build(dpiScale * (float)_config.Scale, _config.InnerCount, _config.OuterCount);

            int size = _geo.Size;
            Rectangle wa = Screen.FromPoint(anchor).WorkingArea;
            int x = anchor.X - size / 2;
            int y = anchor.Y - size / 2;
            if (size <= wa.Width) x = Math.Max(wa.Left, Math.Min(wa.Right - size, x));
            if (size <= wa.Height) y = Math.Max(wa.Top, Math.Min(wa.Bottom - size, y));
            _bounds = new Rectangle(x, y, size, size);

            _hoverRing = -1;
            _hoverSlot = -1;
            _openedAt = DateTime.UtcNow;
            _openCursor = anchor;
            _armed = preview;
            _seenDown = false;
            _toggleFallback = false;
            _closing = false;
            _shown = true;
            _escDown = Native.IsDown((int)Keys.Escape);
            _anim = _config.Animations ? 0f : 1f;
            _animDir = 1;
            _animSpeed = 1000f / 140f;
            _alpha = _anim;
            _lastTick = DateTime.UtcNow;
            _relative = _config.MouseMode == 2;
            _vx = anchor.X;
            _vy = anchor.Y;
            _rawAccum = 0f;
            _rawWarned = false;
            _lastCursorSample = anchor;
            _lastCursorChange = DateTime.UtcNow;

            UpdateHover();

            Log.Info(string.Format("打开轮盘 anchor={0},{1} bounds={2},{3} {4} dpi={5:0.00} inner={6} outer={7}",
                anchor.X, anchor.Y, _bounds.X, _bounds.Y, _bounds.Width, dpiScale, _config.InnerCount, _config.OuterCount));

            Native.ShowNoActivate(Handle);
            if (_rawWindow == null)
            {
                _rawWindow = new RawMouseWindow();
                _rawWindow.Moved += OnRawMouseEvent;
                _rawWindow.EnsureHandle();
                Native.ShowNoActivate(_rawWindow.Handle);
            }
            _rawRegistered = _rawWindow.Register();
            Log.Info("鼠标跟踪 设置=" + MouseModeText(_config.MouseMode) + " 实际=" + (_relative ? "相对" : "绝对")
                + " 原始输入=" + _rawRegistered + " 窗口=" + _rawWindow.Handle
                + " 灵敏度=" + _config.MouseSensitivity.ToString("0.0"));
            _timer.Start();
            Render();
        }

        public void CloseNow()
        {
            if (!_shown) return;
            _shown = false;
            _closing = false;
            _timer.Stop();
            if (_rawRegistered && _rawWindow != null)
            {
                _rawRegistered = false;
                _rawWindow.Unregister();
            }
            Native.HideWindow(Handle);
        }

        private void BuildItems()
        {
            _inner = BuildList(_config.Inner, Math.Max(1, _config.InnerCount));
            _outer = BuildList(_config.Outer, Math.Max(0, _config.OuterCount));
        }

        private static List<WheelItem> BuildList(List<SlotConfig> slots, int count)
        {
            List<WheelItem> list = new List<WheelItem>();
            for (int i = 0; i < count; i++)
            {
                WheelItem item = new WheelItem();
                SlotConfig slot = i < slots.Count ? slots[i] : null;
                if (slot != null && slot.IsSet)
                {
                    item.Assigned = true;
                    item.Index = slot.Index;
                    item.Title = string.IsNullOrEmpty(slot.Title) ? ("音效 " + slot.Index) : slot.Title;
                }
                list.Add(item);
            }
            return list;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_shown) return;

            DateTime now = DateTime.UtcNow;
            float dt = (float)(now - _lastTick).TotalMilliseconds;
            _lastTick = now;
            if (dt < 1f) dt = 1f;
            if (dt > 60f) dt = 60f;

            if (!_closing)
            {
                PollMouse(now);
                if (FakeRaw && _relative && (now - _openedAt).TotalMilliseconds < 700) OnRawMouse(5, -7);
                UpdateHover();

                if (_preview)
                {
                    if (now >= _previewUntil) { CloseWheel(); return; }
                }
                else if (_trackHold)
                {
                    int vk = _config.Hotkey.VirtualKey;
                    short raw = Native.GetAsyncKeyState(vk);
                    bool down = (raw & 0x8000) != 0;
                    double sinceOpen = (now - _openedAt).TotalMilliseconds;
                    if (down) _seenDown = true;
                    if (raw != _lastRaw) { _lastRaw = raw; Log.Info("key raw state vk=" + vk + " raw=" + raw); }
                    if (!_seenDown && !_toggleFallback && sinceOpen > 2500)
                    {
                        _toggleFallback = true;
                        Log.Info("键状态不可见，切换为点按模式（点击播放 / Esc 或右键关闭）");
                    }
                    if (down) _toggleFallback = false;
                    if (!down && _seenDown && sinceOpen > 60)
                    {
                        Log.Info(string.Format("hold-release vk={0} sinceOpen={1:0}ms armed={2}", vk, sinceOpen, _armed));
                        Commit();
                        return;
                    }
                    if (_toggleFallback && sinceOpen > 15000)
                    {
                        Log.Info("点按模式超时关闭");
                        Cancel();
                        return;
                    }
                }

                bool esc = Native.IsDown((int)Keys.Escape);
                if (esc && !_escDown) { Cancel(); return; }
                _escDown = esc;
            }

            if (_config.Animations && Math.Abs(_anim - (float)(_animDir > 0 ? 1 : 0)) > 0.001f)
            {
                _anim += _animDir * _animSpeed * dt;
                if (_anim >= 1f) _anim = 1f;
                if (_anim <= 0f)
                {
                    _anim = 0f;
                    if (_closing) { CloseNow(); return; }
                }
                Render();
            }
            else if (_closing)
            {
                CloseNow();
            }
        }

        private void UpdateHover()
        {
            Point cursor = CurrentPointer();
            if (!_armed)
            {
                float mdx = cursor.X - _openCursor.X;
                float mdy = cursor.Y - _openCursor.Y;
                float dead = 6f * _geo.Scale;
                if (mdx * mdx + mdy * mdy < dead * dead) return;
                _armed = true;
            }
            int ring;
            int slot;
            HitTest(new Point(cursor.X - _bounds.X, cursor.Y - _bounds.Y), out ring, out slot);
            if (ring == _hoverRing && slot == _hoverSlot) return;
            _hoverRing = ring;
            _hoverSlot = slot;
            Render();
        }

        private Point CurrentPointer()
        {
            if (_relative) return new Point((int)Math.Round(_vx), (int)Math.Round(_vy));
            return Cursor.Position;
        }

        private static string MouseModeText(int mode)
        {
            if (mode == 1) return "绝对";
            if (mode == 2) return "相对";
            return "自动";
        }

        // 每个 tick 采样系统光标，并在"自动"模式下判断是否需要改用相对位移跟踪
        private void PollMouse(DateTime now)
        {
            Point p = Cursor.Position;
            if (p.X != _lastCursorSample.X || p.Y != _lastCursorSample.Y)
            {
                _lastCursorSample = p;
                _lastCursorChange = now;
            }
            if (!_preview && _openedAt != DateTime.MinValue && !_rawWarned && (now - _openedAt).TotalMilliseconds > 1600
                && _relative && _rawRegistered && _rawAccum <= 0f)
            {
                _rawWarned = true;
                Log.Info("警告：1.6 秒内没有收到鼠标原始输入，相对位移可能不可用（可在设置『游戏与鼠标』页检查跟踪方式）");
            }
            if (_relative || _preview || _config.MouseMode != 0) return;
            if ((now - _openedAt).TotalMilliseconds < 150) return;

            double stall = (now - _lastCursorChange).TotalMilliseconds;
            if (Native.CursorHidden() && stall > 120)
            {
                SwitchToRelative("光标已被程序隐藏", p);
                return;
            }
            if (Native.CursorClipped() && stall > 120)
            {
                SwitchToRelative("光标被限制在小范围内（游戏锁定）", p);
                return;
            }
            if (_rawAccum > 30 && stall > 180)
            {
                SwitchToRelative("收到鼠标原始位移但系统光标未移动", p);
            }
        }

        private void SwitchToRelative(string reason, Point from)
        {
            if (_relative) return;
            _relative = true;
            _vx = from.X;
            _vy = from.Y;
            Log.Info("鼠标改为相对跟踪：" + reason + "（起点 " + from.X + "," + from.Y + "）");
        }

        private void OnRawMouseEvent(int dx, int dy)
        {
            if (MouseDebug) Log.Info(string.Format("原始鼠标 dx={0} dy={1} rel={2}", dx, dy, _relative));
            if (dx == 0 && dy == 0) return;
            OnRawMouse(dx, dy);
        }

        private void OnRawMouse(int dx, int dy)
        {
            _rawAccum += Math.Abs(dx) + Math.Abs(dy);
            if (!_relative) return;

            float k = (float)_config.MouseSensitivity;
            if (k < 0.05f) k = 1f;
            _vx += dx * k;
            _vy += dy * k;
            ClampVirtual();
        }

        // 相对模式下把虚拟指针限制在轮盘范围内，避免越走越远
        private void ClampVirtual()
        {
            float maxR = (_geo.OuterCount > 0 ? _geo.OuterOut : _geo.InnerOut) - 2f * _geo.Scale;
            float cx = _bounds.X + _geo.Center;
            float cy = _bounds.Y + _geo.Center;
            float dx = _vx - cx;
            float dy = _vy - cy;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d <= maxR || d <= 0.01f) return;
            _vx = cx + dx * maxR / d;
            _vy = cy + dy * maxR / d;
        }

        private void HitTest(Point local, out int ring, out int slot)
        {
            ring = -1;
            slot = -1;

            float dx = local.X - _geo.Center;
            float dy = local.Y - _geo.Center;
            float r = (float)Math.Sqrt(dx * dx + dy * dy);
            float maxR = _geo.OuterCount > 0 ? _geo.OuterOut : _geo.InnerOut;
            if (r < _geo.HubR || r > maxR + 4f * _geo.Scale) return;

            float angle = (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI);
            if (angle < 0) angle += 360f;

            if (r <= _geo.InnerOut) ring = 0;
            else if (r >= _geo.OuterIn && _geo.OuterCount > 0) ring = 1;
            else return;

            int count = ring == 0 ? _geo.InnerCount : _geo.OuterCount;
            if (count <= 0) { ring = -1; return; }
            float span = 360f / count;
            slot = (int)(angle / span);
            if (slot < 0) slot = 0;
            if (slot >= count) slot = count - 1;
        }

        private void Commit()
        {
            WheelItem item = HoveredItem();
            Point cursor = CurrentPointer();
            Log.Info(string.Format("提交 cursor={0},{1} rel={2} bounds={3},{4},{5} ring={6} slot={7} item={8}",
                cursor.X, cursor.Y, _relative, _bounds.X, _bounds.Y, _bounds.Width, _hoverRing, _hoverSlot,
                item == null ? "null" : item.Index + " " + item.Title));
            bool play = item != null && item.Assigned;
            CloseWheel();
            if (play)
            {
                Action<WheelItem> handler = PlayRequested;
                if (handler != null) handler(item);
            }
        }

        private void Cancel()
        {
            CloseWheel();
        }

        private void CloseWheel()
        {
            if (!_shown || _closing) return;
            if (!_config.Animations)
            {
                CloseNow();
                return;
            }
            _closing = true;
            _animDir = -1;
            _animSpeed = 1000f / 90f;
        }

        private WheelItem HoveredItem()
        {
            if (_hoverSlot < 0) return null;
            if (_hoverRing == 0) return _hoverSlot < _inner.Count ? _inner[_hoverSlot] : null;
            if (_hoverRing == 1) return _hoverSlot < _outer.Count ? _outer[_hoverSlot] : null;
            return null;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)Native.MA_NOACTIVATE;
                return;
            }
            if (m.Msg == 0x0202)
            {
                Point cursor = CurrentPointer();
                int ring;
                int slot;
                HitTest(new Point(cursor.X - _bounds.X, cursor.Y - _bounds.Y), out ring, out slot);
                _hoverRing = ring;
                _hoverSlot = slot;
                Commit();
                return;
            }
            if (m.Msg == 0x0204)
            {
                Cancel();
                return;
            }
            base.WndProc(ref m);
        }

        private void Render()
        {
            if (!_shown) return;
            int size = _geo.Size;
            if (_canvas == null || _canvas.Width != size)
            {
                if (_canvas != null) _canvas.Dispose();
                _canvas = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
            }

            float vis = 1f;
            if (_config.Animations)
                vis = _animDir >= 0 ? EaseOutCubic(_anim) : _anim;
            _alpha = vis;

            using (Graphics g = Graphics.FromImage(_canvas))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                float s = 0.92f + 0.08f * vis;
                float cx = size / 2f;
                g.TranslateTransform(cx, cx);
                g.ScaleTransform(s, s);
                g.TranslateTransform(-cx, -cx);

                DrawScene(g);
            }

            Present(_canvas);
        }

        private static float EaseOutCubic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float u = 1f - t;
            return 1f - u * u * u;
        }

        private int A(int alpha)
        {
            int v = (int)(alpha * _alpha);
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
        }

        private void DrawScene(Graphics g)
        {
            Color accent = _config.AccentColor;
            float maxR = _geo.OuterCount > 0 ? _geo.OuterOut : _geo.InnerOut;
            DrawShadow(g, maxR);

            using (Font innerFont = MakeFont((15f * _geo.Scale) / 1f, false))
            using (Font outerFont = MakeFont(13f * _geo.Scale, false))
            using (Font hubFont = MakeFont(15f * _geo.Scale, true))
            using (Font hintFont = MakeFont(9.5f * _geo.Scale, false))
            {
                DrawRing(g, _inner, _geo.InnerIn, _geo.InnerOut, 0, true, innerFont);
                if (_geo.OuterCount > 0)
                    DrawRing(g, _outer, _geo.OuterIn, _geo.OuterOut, 1, false, outerFont);
                DrawHub(g, hubFont, hintFont, accent);
                DrawPointer(g, accent);
            }
        }

        // 相对模式下系统光标可能被游戏锁住，这里画一个虚拟指针表示当前选择位置
        private void DrawPointer(Graphics g, Color accent)
        {
            if (!_relative) return;
            float x = _vx - _bounds.X;
            float y = _vy - _bounds.Y;
            float r = 5f * _geo.Scale;

            using (GraphicsPath halo = new GraphicsPath())
            {
                halo.AddEllipse(x - r * 3f, y - r * 3f, r * 6f, r * 6f);
                using (PathGradientBrush brush = new PathGradientBrush(halo))
                {
                    brush.CenterColor = Color.FromArgb(A(110), accent);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, accent) };
                    g.FillPath(brush, halo);
                }
            }
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(A(255), 250, 252, 255)))
            {
                g.FillEllipse(fill, x - r, y - r, r * 2f, r * 2f);
            }
            using (Pen border = new Pen(Color.FromArgb(A(220), accent), Math.Max(1.4f, 1.2f * _geo.Scale)))
            {
                g.DrawEllipse(border, x - r, y - r, r * 2f, r * 2f);
            }
        }

        private static Font MakeFont(float pixelSize, bool bold)
        {
            if (pixelSize < 6f) pixelSize = 6f;
            return new Font("Microsoft YaHei UI", pixelSize, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        }

        private void DrawShadow(Graphics g, float maxR)
        {
            float r = maxR + _geo.Margin * 0.55f;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(_geo.Center - r, _geo.Center - r + 5f * _geo.Scale, r * 2f, r * 2f);
                using (PathGradientBrush brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(A(90), 0, 0, 0);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                    brush.SetSigmaBellShape(0.45f);
                    g.FillPath(brush, path);
                }
            }
        }

        private void DrawRing(Graphics g, List<WheelItem> items, float rIn, float rOut, int ringIndex, bool inner, Font font)
        {
            int n = items.Count;
            if (n <= 0) return;
            float span = 360f / n;
            float pad = Math.Min(1.4f, span * 0.05f);
            int hovered = _hoverRing == ringIndex ? _hoverSlot : -1;

            for (int i = 0; i < n; i++)
            {
                if (i == hovered) continue;
                using (GraphicsPath path = SegmentPath(rIn, rOut, i * span + pad, (i + 1) * span - pad))
                {
                    WheelItem item = items[i];
                    Color fill;
                    if (item.Assigned)
                    {
                        Color hue = ColorUtil.FromHsl((215 + i * 37) % 360, 0.5, 0.55);
                        Color baseFill = inner ? Color.FromArgb(255, 20, 24, 32) : Color.FromArgb(255, 25, 29, 38);
                        fill = ColorUtil.Mix(baseFill, hue, 0.15);
                    }
                    else
                    {
                        fill = Color.FromArgb(255, 26, 30, 38);
                    }
                    int alpha = item.Assigned ? A(252) : A(200);
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, fill)))
                        g.FillPath(brush, path);
                }
            }

            if (hovered >= 0 && hovered < n)
            {
                using (GraphicsPath path = SegmentPath(rIn, rOut, hovered * span + pad, (hovered + 1) * span - pad))
                {
                    Color accent = _config.AccentColor;
                    using (Pen glow = new Pen(Color.FromArgb(A(60), accent), 7f))
                    using (Pen glow2 = new Pen(Color.FromArgb(A(40), accent), 13f))
                    {
                        glow.LineJoin = LineJoin.Round;
                        glow2.LineJoin = LineJoin.Round;
                        g.DrawPath(glow2, path);
                        g.DrawPath(glow, path);
                    }

                    RectangleF bounds = path.GetBounds();
                    if (bounds.Width < 1f) bounds.Width = 1f;
                    if (bounds.Height < 1f) bounds.Height = 1f;
                    using (LinearGradientBrush fill = new LinearGradientBrush(bounds,
                        Color.FromArgb(A(255), ColorUtil.Mix(accent, Color.White, 0.18)),
                        Color.FromArgb(A(255), ColorUtil.Mix(accent, Color.Black, 0.28)),
                        55f))
                    {
                        g.FillPath(fill, path);
                    }
                    using (Pen border = new Pen(Color.FromArgb(A(200), 255, 255, 255), 1.4f))
                    {
                        border.LineJoin = LineJoin.Round;
                        g.DrawPath(border, path);
                    }
                }
            }

            for (int i = 0; i < n; i++)
            {
                if (i == hovered) continue;
                DrawSlotText(g, items[i], i, n, rIn, rOut, false, font);
            }
            if (hovered >= 0 && hovered < n)
                DrawSlotText(g, items[hovered], hovered, n, rIn, rOut, true, font);
        }

        private void DrawSlotText(Graphics g, WheelItem item, int i, int n, float rIn, float rOut, bool hovered, Font font)
        {
            float mid = (rIn + rOut) * 0.5f;
            double angle = (i + 0.5) * (360.0 / n);
            PointF center = PointOnCircle(mid, angle);

            float chord = (float)(2.0 * mid * Math.Sin(Math.PI / n));
            float maxWidth = Math.Min(chord * 0.84f, (rOut - rIn) * 1.7f);
            if (maxWidth < 12f) return;

            string text = item.Assigned && !string.IsNullOrEmpty(item.Title) ? item.Title : "空闲";
            Color color = item.Assigned
                ? (hovered ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(235, 240, 244, 252))
                : Color.FromArgb(105, 200, 208, 224);

            List<string> lines = WrapText(g, text, font, maxWidth, item.Assigned ? 2 : 1);
            if (lines.Count == 0) return;

            float lineHeight = font.GetHeight(g) * 1.05f;
            float totalHeight = lines.Count * lineHeight;
            float top = center.Y - totalHeight / 2f;

            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            format.FormatFlags = StringFormatFlags.NoWrap;
            format.Trimming = StringTrimming.EllipsisCharacter;

            using (SolidBrush brush = new SolidBrush(color))
            {
                for (int k = 0; k < lines.Count; k++)
                {
                    RectangleF rect = new RectangleF(center.X - maxWidth / 2f, top + k * lineHeight, maxWidth, lineHeight);
                    g.DrawString(lines[k], font, brush, rect, format);
                }
            }
            format.Dispose();
        }

        private static List<string> WrapText(Graphics g, string text, Font font, float maxWidth, int maxLines)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;

            StringFormat measure = StringFormat.GenericTypographic;
            string rest = text;
            for (int line = 0; line < maxLines; line++)
            {
                if (rest.Length == 0) break;
                int take = 0;
                for (int i = 1; i <= rest.Length; i++)
                {
                    string candidate = rest.Substring(0, i);
                    if (g.MeasureString(candidate, font, PointF.Empty, measure).Width > maxWidth) break;
                    take = i;
                }
                if (take <= 0) take = 1;
                if (take >= rest.Length)
                {
                    lines.Add(rest);
                    rest = "";
                    break;
                }
                if (line == maxLines - 1)
                {
                    lines.Add(FitWithEllipsis(g, rest, font, maxWidth, measure));
                    rest = "";
                    break;
                }
                lines.Add(rest.Substring(0, take));
                rest = rest.Substring(take).TrimStart();
            }
            return lines;
        }

        private static string FitWithEllipsis(Graphics g, string text, Font font, float maxWidth, StringFormat measure)
        {
            string ellipsis = "…";
            if (g.MeasureString(ellipsis, font, PointF.Empty, measure).Width > maxWidth) return "";
            string s = text;
            while (s.Length > 0 && g.MeasureString(s + ellipsis, font, PointF.Empty, measure).Width > maxWidth)
                s = s.Substring(0, s.Length - 1);
            return s + ellipsis;
        }

        private void DrawHub(Graphics g, Font titleFont, Font hintFont, Color accent)
        {
            float cx = _geo.Center;
            float cy = _geo.Center;
            float r = _geo.HubR;
            RectangleF rect = new RectangleF(cx - r, cy - r, r * 2f, r * 2f);

            using (GraphicsPath halo = new GraphicsPath())
            {
                halo.AddEllipse(cx - r - 6f * _geo.Scale, cy - r - 6f * _geo.Scale, (r + 6f * _geo.Scale) * 2f, (r + 6f * _geo.Scale) * 2f);
                using (PathGradientBrush brush = new PathGradientBrush(halo))
                {
                    brush.CenterColor = Color.FromArgb(A(70), accent);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, accent) };
                    g.FillPath(brush, halo);
                }
            }

            using (LinearGradientBrush fill = new LinearGradientBrush(rect,
                Color.FromArgb(A(250), 33, 38, 50), Color.FromArgb(A(252), 11, 13, 18), 100f))
            {
                g.FillEllipse(fill, rect);
            }
            using (Pen border = new Pen(Color.FromArgb(A(150), accent), 1.5f))
            {
                g.DrawEllipse(border, rect);
            }

            WheelItem item = HoveredItem();
            string title;
            string hint;
            string hint2;
            if (item != null && item.Assigned)
            {
                title = item.Title;
                hint = (_config.ToggleMode || _toggleFallback) ? "点击播放" : "松开播放";
                hint2 = "右键取消";
            }
            else if (item != null)
            {
                title = "空闲位置";
                hint = "右键或 Esc 关闭";
                hint2 = "";
            }
            else
            {
                title = "选择音效";
                hint = "移动鼠标";
                hint2 = "Esc 取消";
            }

            float textWidth = r * 2f * 0.76f;
            List<string> lines = WrapText(g, title, titleFont, textWidth, 2);
            float lineHeight = titleFont.GetHeight(g) * 1.05f;
            float total = lines.Count * lineHeight;
            float top = cy - total / 2f - 7f * _geo.Scale;

            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            format.FormatFlags = StringFormatFlags.NoWrap;
            format.Trimming = StringTrimming.EllipsisCharacter;

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(A(246), 244, 247, 255)))
            {
                for (int k = 0; k < lines.Count; k++)
                {
                    RectangleF line = new RectangleF(cx - textWidth / 2f, top + k * lineHeight, textWidth, lineHeight);
                    g.DrawString(lines[k], titleFont, brush, line, format);
                }
            }

            float hintLineHeight = hintFont.GetHeight(g) * 1.32f;
            RectangleF hintRect = new RectangleF(cx - textWidth / 2f, top + total + 1f * _geo.Scale, textWidth, hintLineHeight);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(A(200), accent)))
            {
                g.DrawString(hint, hintFont, brush, hintRect, format);
            }
            if (hint2.Length > 0)
            {
                hintRect.Y += hintLineHeight - 3f * _geo.Scale;
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(A(135), 222, 228, 240)))
                {
                    g.DrawString(hint2, hintFont, brush, hintRect, format);
                }
            }
            format.Dispose();
        }

        private PointF PointOnCircle(float radius, double angleDegrees)
        {
            double rad = angleDegrees * Math.PI / 180.0;
            return new PointF(
                _geo.Center + (float)(Math.Sin(rad) * radius),
                _geo.Center - (float)(Math.Cos(rad) * radius));
        }

        private GraphicsPath SegmentPath(float rIn, float rOut, float a0, float a1)
        {
            GraphicsPath path = new GraphicsPath();
            float cx = _geo.Center;
            float cy = _geo.Center;
            RectangleF outer = new RectangleF(cx - rOut, cy - rOut, rOut * 2f, rOut * 2f);
            RectangleF inner = new RectangleF(cx - rIn, cy - rIn, rIn * 2f, rIn * 2f);
            float sweep = a1 - a0;

            path.AddArc(outer, a0 - 90f, sweep);
            path.AddLine(PointOnCircle(rOut, a1), PointOnCircle(rIn, a1));
            path.AddArc(inner, a1 - 90f, -sweep);
            path.CloseFigure();
            return path;
        }

        private void Present(Bitmap bitmap)
        {
            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            IntPtr memDc = Native.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = Native.SelectObject(memDc, hBitmap);

                Native.POINT top = new Native.POINT(_bounds.X, _bounds.Y);
                Native.SIZE size = new Native.SIZE(bitmap.Width, bitmap.Height);
                Native.POINT src = new Native.POINT(0, 0);
                Native.BLENDFUNCTION blend = new Native.BLENDFUNCTION();
                blend.BlendOp = 0;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = Native.AC_SRC_ALPHA;

                Native.UpdateLayeredWindow(Handle, screenDc, ref top, ref size, memDc, ref src, 0, ref blend, Native.ULW_ALPHA);
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero) Native.SelectObject(memDc, oldBitmap);
                if (hBitmap != IntPtr.Zero) Native.DeleteObject(hBitmap);
                Native.DeleteDC(memDc);
                Native.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                if (_canvas != null) { _canvas.Dispose(); _canvas = null; }
                if (_rawWindow != null)
                {
                    _rawWindow.Unregister();
                    _rawWindow.Dispose();
                    _rawWindow = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
