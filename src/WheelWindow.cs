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
            HubR = 50f * scale;
            InnerIn = 58f * scale;
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
        private MouseHook _hook;
        private bool _capturing;
        private bool _leftPending;
        private bool _rightPending;
        private int _lastHookEvents;
        private bool _captureWarned;
        private Point _cursorSynced;
        private Point _lastCursorSample;
        private DateTime _lastCursorChange;
        private bool _frameDirty;
        private DateTime _lastRawDelta = DateTime.MinValue;
        private readonly System.Diagnostics.Stopwatch _frameWatch = new System.Diagnostics.Stopwatch();
        private double _frameSum;
        private int _frameCount;
        private double _frameMax;
        private bool _rendering;
        private DateTime _lastRenderAt = DateTime.MinValue;
        private Bitmap _bgCache;
        private Bitmap _labelCache;
        private Bitmap _composed;
        private bool _cacheDirty = true;
        private bool _composedDirty = true;
        private Font _innerFont;
        private Font _outerFont;
        private Font _hubFont;

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

            _timer.Interval = 10;
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
            _frameDirty = false;
            _lastRawDelta = DateTime.MinValue;
            _frameSum = 0;
            _frameCount = 0;
            _frameMax = 0;
            _cacheDirty = true;
            _composedDirty = true;
            BuildFonts();

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
            _leftPending = false;
            _rightPending = false;
            _capturing = false;
            _captureWarned = false;
            if (_config.CaptureMouse)
            {
                if (_hook == null)
                {
                    _hook = new MouseHook();
                    _hook.Moved += OnHookMouse;
                    _hook.LeftClick += OnHookLeft;
                    _hook.RightClick += OnHookRight;
                }
                if (_hook.Install())
                {
                    _hook.Capturing = true;
                    _capturing = true;
                    _relative = true;
                    _rawAccum = 0f;
                    _lastHookEvents = _hook.EventCount;
                    _cursorSynced = new Point((int)Math.Round(_vx), (int)Math.Round(_vy));
                }
            }
            Log.Info("鼠标跟踪 设置=" + MouseModeText(_config.MouseMode) + " 实际=" + (_relative ? "相对" : "绝对")
                + " 接管=" + _capturing + " 原始输入=" + _rawRegistered + " 窗口=" + _rawWindow.Handle
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
            if (MouseDebug && _frameCount > 0)
            {
                Log.Info(string.Format("渲染统计 帧={0} 平均={1:0.0}ms 最大={2:0.0}ms",
                    _frameCount, _frameSum / _frameCount, _frameMax));
            }
            if (_rawRegistered && _rawWindow != null)
            {
                _rawRegistered = false;
                _rawWindow.Unregister();
            }
            if (_hook != null) _hook.Uninstall();
            _capturing = false;
            _leftPending = false;
            _rightPending = false;
            DisposeCaches();
            DisposeFonts();
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
                if (FakeRaw && _relative && (now - _openedAt).TotalMilliseconds < 5000) OnRawMouse(5, -7);
                if (_capturing && _relative) SyncCapturedCursor();
                UpdateHover();
                if (_frameDirty)
                {
                    _frameDirty = false;
                    Render();
                }

                if (_rightPending)
                {
                    _rightPending = false;
                    Log.Info("右键取消");
                    Cancel();
                    return;
                }
                if (_leftPending)
                {
                    _leftPending = false;
                    Commit();
                    return;
                }

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
            _frameDirty = true;
            _composedDirty = true;
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
                && _relative && !_capturing && _rawRegistered && _rawAccum <= 0f)
            {
                _rawWarned = true;
                Log.Info("警告：1.6 秒内没有收到鼠标原始输入，相对位移可能不可用（可在设置『游戏与鼠标』页检查跟踪方式）");
            }
            if (_capturing)
            {
                if (!_captureWarned && _hook != null && _hook.EventCount == 0 && (now - _openedAt).TotalMilliseconds > 1500)
                {
                    _captureWarned = true;
                    Log.Info("警告：接管鼠标期间没有收到钩子事件（可能有更高权限的程序在前台），改用光标位置跟踪");
                }
                return;
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

        // 回调里只累加位移，光标同步放到每个 tick 做，避免在钩子回调里再次触发鼠标事件
        private void OnHookMouse(int dx, int dy)
        {
            // 原始输入是硬件计数，不受我们钉住光标的影响；只要它还在来，就不用钩子的光标差值
            if ((DateTime.UtcNow - _lastRawDelta).TotalMilliseconds < 400) return;
            OnRawMouse(dx, dy);
        }

        // 把真实光标钉在虚拟指针上：游戏和桌面都只看到轮盘里的位置
        // 如果钩子拿不到事件（例如前台是管理员权限的游戏），就退化为跟随真实光标移动
        private void SyncCapturedCursor()
        {
            int events = _hook == null ? 0 : _hook.EventCount;
            bool hookAlive = events != _lastHookEvents;
            Point cur = Cursor.Position;
            if (!hookAlive && (cur.X != _cursorSynced.X || cur.Y != _cursorSynced.Y))
            {
                _vx += cur.X - _cursorSynced.X;
                _vy += cur.Y - _cursorSynced.Y;
                ClampVirtual();
            }
            _lastHookEvents = events;
            int tx = (int)Math.Round(_vx);
            int ty = (int)Math.Round(_vy);
            Native.SetCursorPos(tx, ty);
            _cursorSynced = new Point(tx, ty);
            if (_hook != null) _hook.NoteCursor(tx, ty);
        }

        private void OnHookLeft()
        {
            _leftPending = true;
        }

        private void OnHookRight()
        {
            _rightPending = true;
        }

        private void OnRawMouseEvent(int dx, int dy)
        {
            _lastRawDelta = DateTime.UtcNow;
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
            _frameDirty = true;
            // 收到位移后立刻重绘（8ms 内刚画过就交给定时器），指针更跟手
            if (_shown && !_preview && (DateTime.UtcNow - _lastRenderAt).TotalMilliseconds >= 8)
            {
                UpdateHover();
                Render();
            }
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

        // 分层缓存：静态底图（底盘 + 扇区）、标签层、合成层（底图 + 悬停高亮 + 标签 + 中心）
        // 指针每帧移动时只做一次合成层 blit + 画指针，不再重画二十来个抗锯齿扇区
        private void EnsureCanvas(int size)
        {
            if (_canvas != null && _canvas.Width == size) return;
            if (_canvas != null) _canvas.Dispose();
            _canvas = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
        }

        private static void SetupQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.Default;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
        }

        private void Render()
        {
            // 鼠标输入可能比定时器更快，这里做重入保护 + 限速直绘，减少指针延迟
            if (_rendering) return;
            _rendering = true;
            try
            {
                RenderCore();
            }
            finally
            {
                _rendering = false;
                _lastRenderAt = DateTime.UtcNow;
            }
        }

        private void RenderCore()
        {
            if (!_shown) return;
            _frameWatch.Reset();
            _frameWatch.Start();

            int size = _geo.Size;
            EnsureCanvas(size);

            float vis = 1f;
            if (_config.Animations)
                vis = _animDir >= 0 ? EaseOutCubic(_anim) : _anim;
            _alpha = vis;

            if (_cacheDirty)
            {
                _cacheDirty = false;
                BuildCaches(size);
                _composedDirty = true;
            }
            if (_composedDirty) RebuildComposed();

            using (Graphics g = Graphics.FromImage(_canvas))
            {
                g.Clear(Color.Transparent);
                SetupQuality(g);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                float s = 0.92f + 0.08f * vis;
                float cx = size / 2f;
                g.TranslateTransform(cx, cx);
                g.ScaleTransform(s, s);
                g.TranslateTransform(-cx, -cx);

                if (vis >= 0.999f)
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.DrawImageUnscaled(_composed, 0, 0);
                }
                else
                {
                    // 动画期间整体淡化：用 ColorMatrix 调 alpha 一次画完
                    ColorMatrix matrix = new ColorMatrix();
                    matrix.Matrix33 = vis;
                    using (ImageAttributes attrs = new ImageAttributes())
                    {
                        attrs.SetColorMatrix(matrix);
                        Rectangle dest = new Rectangle(0, 0, size, size);
                        g.DrawImage(_composed, dest, 0, 0, size, size, GraphicsUnit.Pixel, attrs);
                    }
                }

                DrawPointer(g, _config.AccentColor);
            }

            Present(_canvas);

            _frameWatch.Stop();
            double frameMs = _frameWatch.Elapsed.TotalMilliseconds;
            _frameSum += frameMs;
            _frameCount++;
            if (frameMs > _frameMax) _frameMax = frameMs;
            if (MouseDebug && frameMs > 8.0) Log.Info(string.Format("渲染耗时 {0:0.0}ms", frameMs));
        }

        private void BuildCaches(int size)
        {
            if (_bgCache == null || _bgCache.Width != size)
            {
                DisposeCaches();
                _bgCache = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
                _labelCache = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
                _composed = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
            }

            float keep = _alpha;
            _alpha = 1f;
            try
            {
                using (Graphics g = Graphics.FromImage(_bgCache))
                {
                    SetupQuality(g);
                    g.Clear(Color.Transparent);
                    DrawBase(g);
                    DrawRingBody(g, _inner, _geo.InnerIn, _geo.InnerOut, true);
                    if (_geo.OuterCount > 0)
                        DrawRingBody(g, _outer, _geo.OuterIn, _geo.OuterOut, false);
                }
                using (Graphics g = Graphics.FromImage(_labelCache))
                {
                    SetupQuality(g);
                    g.Clear(Color.Transparent);
                    DrawRingLabels(g, _inner, _geo.InnerIn, _geo.InnerOut, _innerFont);
                    if (_geo.OuterCount > 0)
                        DrawRingLabels(g, _outer, _geo.OuterIn, _geo.OuterOut, _outerFont);
                }
            }
            finally
            {
                _alpha = keep;
            }
        }

        private void RebuildComposed()
        {
            if (_composed == null) return;
            float keep = _alpha;
            _alpha = 1f;
            try
            {
                using (Graphics g = Graphics.FromImage(_composed))
                {
                    SetupQuality(g);
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.DrawImageUnscaled(_bgCache, 0, 0);
                    DrawHover(g);
                    g.DrawImageUnscaled(_labelCache, 0, 0);
                    DrawHub(g, _hubFont, _config.AccentColor);
                }
            }
            finally
            {
                _alpha = keep;
            }
            _composedDirty = false;
        }

        private void DisposeCaches()
        {
            if (_bgCache != null) { _bgCache.Dispose(); _bgCache = null; }
            if (_labelCache != null) { _labelCache.Dispose(); _labelCache = null; }
            if (_composed != null) { _composed.Dispose(); _composed = null; }
        }

        private void DrawHover(Graphics g)
        {
            if (_hoverSlot < 0) return;
            List<WheelItem> items = _hoverRing == 0 ? _inner : (_hoverRing == 1 ? _outer : null);
            if (items == null || _hoverSlot >= items.Count) return;
            int n = items.Count;
            if (n <= 0) return;
            bool inner = _hoverRing == 0;
            float rIn = inner ? _geo.InnerIn : _geo.OuterIn;
            float rOut = inner ? _geo.InnerOut : _geo.OuterOut;
            float pad = inner ? 0.6f : 0.9f;
            float span = 360f / n;
            using (GraphicsPath path = SegmentPath(rIn, rOut, _hoverSlot * span + pad, (_hoverSlot + 1) * span - pad))
            {
                Color fill = items[_hoverSlot].Assigned ? _config.AccentColor : Color.FromArgb(255, 58, 66, 84);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(A(246), fill)))
                    g.FillPath(brush, path);
            }
        }

        private void DrawRingBody(Graphics g, List<WheelItem> items, float rIn, float rOut, bool inner)
        {
            int n = items.Count;
            if (n <= 0) return;
            float span = 360f / n;
            float pad = inner ? 0.6f : 0.9f;
            Color baseFill = inner ? Color.FromArgb(255, 27, 31, 41) : Color.FromArgb(255, 22, 26, 34);
            Color emptyFill = Color.FromArgb(255, 17, 20, 26);
            for (int i = 0; i < n; i++)
            {
                using (GraphicsPath path = SegmentPath(rIn, rOut, i * span + pad, (i + 1) * span - pad))
                {
                    WheelItem item = items[i];
                    Color fill = item.Assigned ? baseFill : emptyFill;
                    int alpha = item.Assigned ? A(240) : A(140);
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, fill)))
                        g.FillPath(brush, path);
                }
            }
        }

        private void DrawRingLabels(Graphics g, List<WheelItem> items, float rIn, float rOut, Font font)
        {
            int n = items.Count;
            for (int i = 0; i < n; i++)
                DrawSlotText(g, items[i], i, n, rIn, rOut, font);
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

        // 相对跟踪时（游戏隐藏/锁定光标，或接管鼠标后触摸板式操作），用小圆点表示当前选择位置
        private void DrawPointer(Graphics g, Color accent)
        {
            if (!_relative) return;
            float x = _vx - _bounds.X;
            float y = _vy - _bounds.Y;
            float dx = x - _geo.Center;
            float dy = y - _geo.Center;
            // 指到中心盘上时不再画点，中心盘本身会显示当前音效名
            if (dx * dx + dy * dy < _geo.HubR * _geo.HubR) return;
            float r = 4f * _geo.Scale;
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(A(255), 255, 255, 255)))
            {
                g.FillEllipse(fill, x - r, y - r, r * 2f, r * 2f);
            }
            using (Pen border = new Pen(Color.FromArgb(A(230), accent), Math.Max(1f, _geo.Scale)))
            {
                g.DrawEllipse(border, x - r, y - r, r * 2f, r * 2f);
            }
        }


        private void BuildFonts()
        {
            DisposeFonts();
            float scale = _geo.Scale;
            _innerFont = MakeFont(13f * scale, false);
            _outerFont = MakeFont(13f * scale, false);
            _hubFont = MakeFont(15f * scale, true);
        }

        private void DisposeFonts()
        {
            if (_innerFont != null) { _innerFont.Dispose(); _innerFont = null; }
            if (_outerFont != null) { _outerFont.Dispose(); _outerFont = null; }
            if (_hubFont != null) { _hubFont.Dispose(); _hubFont = null; }
        }

        private static Font MakeFont(float pixelSize, bool bold)
        {
            if (pixelSize < 6f) pixelSize = 6f;
            return new Font("Microsoft YaHei UI", pixelSize, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        }


        // 整块深色底盘：让扇区之间的缝隙、环与环之间的空隙保持深色，不会漏出桌面内容
        private void DrawBase(Graphics g)
        {
            float r = _geo.OuterCount > 0 ? _geo.OuterOut : _geo.InnerOut;
            float cx = _geo.Center;
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(A(232), 11, 14, 20)))
            {
                g.FillEllipse(brush, cx - r, cx - r, r * 2f, r * 2f);
            }
        }

        private void DrawSlotText(Graphics g, WheelItem item, int i, int n, float rIn, float rOut, Font font)
        {
            float mid = (rIn + rOut) * 0.5f;
            double angle = (i + 0.5) * (360.0 / n);
            PointF center = PointOnCircle(mid, angle);

            float chord = (float)(2.0 * mid * Math.Sin(Math.PI / n));
            float maxWidth = Math.Min(chord * 0.86f, (rOut - rIn) * 1.9f);
            if (maxWidth < 10f) return;

            string text = item.Assigned && !string.IsNullOrEmpty(item.Title) ? item.Title : "空闲";
            Color color = item.Assigned
                ? Color.FromArgb(A(232), 236, 241, 250)
                : Color.FromArgb(A(95), 200, 208, 224);

            float lineHeight = font.GetHeight(g) * 1.45f;
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.FormatFlags = StringFormatFlags.NoWrap;
                format.Trimming = StringTrimming.EllipsisCharacter;

                using (SolidBrush brush = new SolidBrush(color))
                {
                    RectangleF rect = new RectangleF(center.X - maxWidth / 2f, center.Y - lineHeight / 2f, maxWidth, lineHeight);
                    g.DrawString(text, font, brush, rect, format);
                }
            }
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

        // 中心盘只显示当前指向的音效名，没有额外提示文字
        private void DrawHub(Graphics g, Font titleFont, Color accent)
        {
            float cx = _geo.Center;
            float cy = _geo.Center;
            float r = _geo.HubR;
            RectangleF rect = new RectangleF(cx - r, cy - r, r * 2f, r * 2f);

            using (SolidBrush fill = new SolidBrush(Color.FromArgb(A(244), 14, 17, 23)))
            {
                g.FillEllipse(fill, rect);
            }
            using (Pen border = new Pen(Color.FromArgb(A(95), accent), Math.Max(1f, _geo.Scale)))
            {
                g.DrawEllipse(border, rect);
            }

            WheelItem item = HoveredItem();
            bool assigned = item != null && item.Assigned;
            string title = assigned ? item.Title : (item == null ? "选择音效" : "空闲");
            Color color = assigned
                ? Color.FromArgb(A(252), 255, 255, 255)
                : Color.FromArgb(A(170), 214, 221, 236);

            float textWidth = r * 2f * 0.80f;
            float lineHeight = titleFont.GetHeight(g) * 1.5f;
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.FormatFlags = StringFormatFlags.NoWrap;
                format.Trimming = StringTrimming.EllipsisCharacter;

                using (SolidBrush brush = new SolidBrush(color))
                {
                    RectangleF line = new RectangleF(cx - textWidth / 2f, cy - lineHeight / 2f, textWidth, lineHeight);
                    g.DrawString(title, titleFont, brush, line, format);
                }
            }
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
                DisposeCaches();
                DisposeFonts();
                if (_rawWindow != null)
                {
                    _rawWindow.Unregister();
                    _rawWindow.Dispose();
                    _rawWindow = null;
                }
                if (_hook != null)
                {
                    _hook.Dispose();
                    _hook = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}