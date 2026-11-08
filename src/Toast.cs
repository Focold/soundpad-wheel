// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SoundWheel
{
    internal sealed class Toast : Form
    {
        private static Toast _current;
        private readonly Timer _timer = new Timer();
        private readonly string _text;
        private float _opacity;
        private int _phase;
        private DateTime _phaseAt;
        private DateTime _lastTop = DateTime.MinValue;

        private Toast(string text, Color accent)
        {
            _text = text;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(18, 20, 26);
            ForeColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
            Padding = new Padding(0);
            Opacity = 0;
            _accent = accent;

            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                _scale = g.DpiX / 96f;
            if (_scale < 1f) _scale = 1f;

            Size measured = TextRenderer.MeasureText(text, Font);
            int width = (int)Math.Max(160 * _scale, Math.Min(560 * _scale, measured.Width + 44 * _scale));
            int height = (int)(46 * _scale);
            Size = new Size(width, height);

            using (GraphicsPath path = RoundedRect(new Rectangle(0, 0, width, height), (int)(12 * _scale)))
                Region = new Region(path);

            _timer.Interval = 16;
            _timer.Tick += OnTick;
        }

        private readonly Color _accent;
        private readonly float _scale = 1f;

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        public static void Show(string text, Point near, Color accent)
        {
            try
            {
                if (_current != null && !_current.IsDisposed)
                {
                    _current.CloseNow();
                }
                Toast t = new Toast(text, accent);
                Rectangle wa = Screen.FromPoint(near).WorkingArea;
                int x = near.X - t.Width / 2;
                int y = near.Y - t.Height - (int)(34 * t._scale);
                x = Math.Max(wa.Left + 8, Math.Min(wa.Right - t.Width - 8, x));
                y = Math.Max(wa.Top + 8, Math.Min(wa.Bottom - t.Height - 8, y));
                t.Location = new Point(x, y);
                _current = t;
                t.Show();
                Native.TopMostNoActivate(t.Handle);
                t._lastTop = DateTime.UtcNow;
                t._phase = 1;
                t._phaseAt = DateTime.UtcNow;
                t._timer.Start();
            }
            catch (Exception ex)
            {
                Log.Error("显示提示失败", ex);
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            // 无边框全屏游戏会抢 topmost，提示窗也要周期性顶上去
            if ((DateTime.UtcNow - _lastTop).TotalMilliseconds >= 150)
            {
                _lastTop = DateTime.UtcNow;
                Native.TopMostNoActivate(Handle);
            }
            double elapsed = (DateTime.UtcNow - _phaseAt).TotalMilliseconds;
            if (_phase == 1)
            {
                _opacity = (float)Math.Min(0.97, elapsed / 120.0);
                Opacity = _opacity;
                if (elapsed >= 120) { _phase = 2; _phaseAt = DateTime.UtcNow; }
            }
            else if (_phase == 2)
            {
                Opacity = 0.97;
                if (elapsed >= 1500) { _phase = 3; _phaseAt = DateTime.UtcNow; }
            }
            else
            {
                _opacity = (float)Math.Max(0, 0.97 * (1 - elapsed / 220.0));
                Opacity = _opacity;
                if (elapsed >= 220) CloseNow();
            }
        }

        private void CloseNow()
        {
            _timer.Stop();
            _timer.Dispose();
            if (_current == this) _current = null;
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), (int)(12 * _scale)))
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(18, 20, 26))) g.FillPath(b, path);
                using (Pen p = new Pen(Color.FromArgb(70, _accent), 1.2f)) g.DrawPath(p, path);
            }
            using (SolidBrush bar = new SolidBrush(_accent))
                g.FillRectangle(bar, 0, (int)(12 * _scale), Math.Max(3, (int)(3 * _scale)), Height - (int)(24 * _scale));

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Near;
            sf.LineAlignment = StringAlignment.Center;
            sf.Trimming = StringTrimming.EllipsisCharacter;
            sf.FormatFlags = StringFormatFlags.NoWrap;
            using (SolidBrush text = new SolidBrush(Color.FromArgb(245, 245, 250)))
                g.DrawString(_text, Font, text, new RectangleF(16 * _scale, 0, Width - 26 * _scale, Height), sf);
        }

        internal static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            TopMost = true;
        }
    }
}
