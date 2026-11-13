// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SoundWheel
{
    // 2.0 自绘控件：圆角按钮 / 勾选框 / 卡片 / 主题色块 / 分段选择
    internal static class Ui
    {
        public static GraphicsPath Round(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (radius < 0.5f) radius = 0.5f;
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 180f, 90f);
            path.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            path.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void FillRound(Graphics g, RectangleF r, float radius, Color color)
        {
            using (GraphicsPath path = Round(r, radius))
            using (SolidBrush b = new SolidBrush(color))
                g.FillPath(b, path);
        }

        public static void DrawRound(Graphics g, RectangleF r, float radius, Color color, float width)
        {
            using (GraphicsPath path = Round(r, radius))
            using (Pen p = new Pen(color, width))
                g.DrawPath(p, path);
        }

        public static Color Blend(Color a, Color b, double t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * t),
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        public static void Text(Graphics g, string text, Font font, Color color, RectangleF rect, StringAlignment align, StringAlignment line)
        {
            using (StringFormat f = new StringFormat())
            {
                f.Alignment = align;
                f.LineAlignment = line;
                f.Trimming = StringTrimming.EllipsisCharacter;
                f.FormatFlags = StringFormatFlags.NoWrap;
                using (SolidBrush b = new SolidBrush(color))
                    g.DrawString(text, font, b, rect, f);
            }
        }

        public static bool Inside(RectangleF rect, Point p)
        {
            return p.X >= rect.Left && p.X <= rect.Right && p.Y >= rect.Top && p.Y <= rect.Bottom;
        }
    }

    internal class RoundedButton : Button
    {
        public int Variant;        // 0 幽灵 / 1 主色 / 2 危险 / 3 柔和 / 4 图标
        public float Radius = 10f;
        public float UiScale = 1f;
        public bool AlignLeft;
        private bool _hover;
        private bool _down;

        public RoundedButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme t = Theme.Current;
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = Radius * UiScale;
            if (radius > Height / 2f) radius = Height / 2f;

            Color fill, border, text;
            int v = Variant;
            if (!Enabled)
            {
                fill = t.Dark ? Color.FromArgb(38, 43, 54) : Color.FromArgb(236, 239, 245);
                border = Color.Transparent;
                text = t.TextFaint;
            }
            else if (v == 1)
            {
                fill = _down ? Ui.Blend(t.Accent, Color.Black, 0.18) : (_hover ? Ui.Blend(t.Accent, Color.White, 0.10) : t.Accent);
                border = Color.Transparent;
                text = t.OnAccent;
            }
            else if (v == 2)
            {
                fill = _down ? Ui.Blend(t.Danger, Color.Black, 0.18) : (_hover ? Ui.Blend(t.Danger, Color.White, 0.08) : Ui.Blend(t.Danger, t.Card, 0.12));
                border = Ui.Blend(t.Danger, t.Card, 0.45);
                text = t.Danger;
            }
            else if (v == 3)
            {
                fill = _down ? Ui.Blend(t.AccentSoft, Color.Black, 0.10) : (_hover ? Ui.Blend(t.Accent, t.Card, 0.30) : t.AccentSoft);
                border = Color.Transparent;
                text = t.Accent;
            }
            else if (v == 4)
            {
                fill = _down ? t.CardHover : (_hover ? t.CardHover : Color.Transparent);
                border = Color.Transparent;
                text = t.TextDim;
            }
            else
            {
                fill = _down ? t.Border : (_hover ? t.CardHover : t.Card);
                border = t.Border;
                text = t.Text;
            }

            if (fill.A > 0) Ui.FillRound(g, r, radius, fill);
            if (border.A > 0) Ui.DrawRound(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), radius, border, 1f);
            if (Focused && Enabled && v != 4)
                Ui.DrawRound(g, new RectangleF(1.5f, 1.5f, Width - 3.5f, Height - 3.5f), radius - 1f, Theme.Alpha(t.Accent, 170), 1.2f);

            if (!string.IsNullOrEmpty(Text))
            {
                RectangleF tr = AlignLeft
                    ? new RectangleF(16f * UiScale, 0, Width - 24f * UiScale, Height)
                    : new RectangleF(4, 0, Width - 8, Height);
                Ui.Text(g, Text, Font, text, tr, AlignLeft ? StringAlignment.Near : StringAlignment.Center, StringAlignment.Center);
            }
        }
    }

    internal class RoundedCheck : CheckBox
    {
        public float UiScale = 1f;
        private bool _hover;

        public RoundedCheck()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            AutoSize = false;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme t = Theme.Current;
            float box = 18f * UiScale;
            float top = (Height - box) / 2f;
            RectangleF boxRect = new RectangleF(0.5f, top, box, box);
            float radius = 5.5f * UiScale;

            if (Checked)
            {
                Ui.FillRound(g, boxRect, radius, t.Accent);
                using (Pen p = new Pen(Color.White, 2.1f * UiScale))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawLines(p, new PointF[]
                    {
                        new PointF(boxRect.X + box * 0.26f, boxRect.Y + box * 0.53f),
                        new PointF(boxRect.X + box * 0.45f, boxRect.Y + box * 0.72f),
                        new PointF(boxRect.X + box * 0.76f, boxRect.Y + box * 0.30f)
                    });
                }
            }
            else
            {
                Ui.FillRound(g, boxRect, radius, _hover ? Ui.Blend(t.Card, t.Accent, 0.10) : t.Card);
                Ui.DrawRound(g, boxRect, radius, _hover ? Ui.Blend(t.Border, t.Accent, 0.55) : t.Border, 1.2f);
            }

            Color text = Enabled ? t.Text : t.TextFaint;
            Ui.Text(g, Text, Font, text, new RectangleF(box + 9f * UiScale, 0, Width - box - 9f * UiScale, Height), StringAlignment.Near, StringAlignment.Center);
        }
    }

    internal class Card : Panel
    {
        public string Title = "";
        public Font TitleFont;
        public float Radius = 14f;
        public float UiScale = 1f;
        public bool DrawBorder = true;

        public Card()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Current.Card;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath path = Ui.Round(new RectangleF(0, 0, Width, Height), Radius * UiScale))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme t = Theme.Current;
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            if (DrawBorder) Ui.DrawRound(g, r, Radius * UiScale, t.Border, 1f);
            if (!string.IsNullOrEmpty(Title))
            {
                Font f = TitleFont != null ? TitleFont : Font;
                Ui.Text(g, Title, f, t.TextDim, new RectangleF(18f * UiScale, 14f * UiScale, Width - 36f * UiScale, 24f * UiScale), StringAlignment.Near, StringAlignment.Center);
            }
        }
    }

    internal class PaletteSwatch : Control
    {
        public Color Swatch = Color.White;
        public bool Selected;
        public float UiScale = 1f;
        private bool _hover;

        public PaletteSwatch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme t = Theme.Current;
            float pad = Selected ? 4f * UiScale : 2f * UiScale;
            RectangleF r = new RectangleF(pad, pad, Width - pad * 2f, Height - pad * 2f);
            if (_hover) r.Inflate(1f, 1f);
            Ui.FillRound(g, r, 9f * UiScale, Swatch);
            Ui.DrawRound(g, r, 9f * UiScale, Theme.Alpha(Color.Black, 40), 1f);
            if (Selected)
                Ui.DrawRound(g, new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f), 11f * UiScale, t.Accent, 2f * UiScale);
            else if (_hover)
                Ui.DrawRound(g, new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f), 11f * UiScale, Theme.Alpha(t.Text, 70), 1f);
        }
    }

    internal class SegmentedControl : Control
    {
        public string[] Segments = new string[0];
        public float UiScale = 1f;
        public event EventHandler SelectionChanged;
        private int _selected;
        private int _hover = -1;

        public SegmentedControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        public int SelectedIndex
        {
            get { return _selected; }
            set
            {
                if (value < 0) value = 0;
                if (Segments.Length > 0 && value >= Segments.Length) value = Segments.Length - 1;
                if (_selected != value) { _selected = value; Invalidate(); }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = IndexAt(e.X);
            if (idx != _hover) { _hover = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int idx = IndexAt(e.X);
            if (idx >= 0 && idx != _selected)
            {
                _selected = idx;
                Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
            base.OnMouseClick(e);
        }

        private int IndexAt(int x)
        {
            if (Segments.Length == 0) return -1;
            float w = (float)Width / Segments.Length;
            int idx = (int)(x / w);
            if (idx < 0) idx = 0;
            if (idx >= Segments.Length) idx = Segments.Length - 1;
            return idx;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme t = Theme.Current;
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = 10f * UiScale;
            Ui.FillRound(g, r, radius, t.Dark ? Color.FromArgb(28, 32, 41) : Color.FromArgb(238, 241, 247));
            Ui.DrawRound(g, r, radius, Theme.Alpha(t.Border, 160), 1f);
            if (Segments.Length == 0) return;
            float w = (float)Width / Segments.Length;
            for (int i = 0; i < Segments.Length; i++)
            {
                RectangleF seg = new RectangleF(i * w + 2f, 2f, w - 4f, Height - 4f);
                if (i == _selected)
                {
                    Ui.FillRound(g, seg, radius - 2f, t.AccentSoft);
                    Ui.DrawRound(g, seg, radius - 2f, Theme.Alpha(t.Accent, 150), 1f);
                }
                else if (i == _hover)
                {
                    Ui.FillRound(g, seg, radius - 2f, Theme.Alpha(t.CardHover, 150));
                }
                Color text = i == _selected ? t.Accent : t.TextDim;
                Font f = Font;
                Ui.Text(g, Segments[i], f, text, seg, StringAlignment.Center, StringAlignment.Center);
            }
        }
    }

    internal class Slider : Control
    {
        public double Minimum = 0;
        public double Maximum = 100;
        public double Step = 0;
        public float UiScale = 1f;
        public event EventHandler ValueChanged;
        private double _value;
        private bool _drag;
        private bool _hover;

        public Slider()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Height = 30;
        }

        public double Value
        {
            get { return _value; }
            set
            {
                double v = value;
                if (v < Minimum) v = Minimum;
                if (v > Maximum) v = Maximum;
                if (Math.Abs(v - _value) < 0.0001) return;
                _value = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        private RectangleF TrackRect()
        {
            float inset = 9f * UiScale;
            return new RectangleF(inset, Height / 2f - 3f * UiScale, Width - inset * 2f, 6f * UiScale);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme t = Theme.Current;
            RectangleF track = TrackRect();
            float radius = 3f * UiScale;
            Ui.FillRound(g, track, radius, t.Dark ? Color.FromArgb(52, 58, 72) : Color.FromArgb(224, 229, 239));
            double span = Maximum - Minimum;
            float frac = span <= 0 ? 0f : (float)((_value - Minimum) / span);
            float tx = track.X + track.Width * frac;
            if (tx - track.X > 1f)
                Ui.FillRound(g, new RectangleF(track.X, track.Y, tx - track.X, track.Height), radius, t.Accent);
            float tr = (_hover || _drag ? 9.5f : 8f) * UiScale;
            RectangleF thumb = new RectangleF(tx - tr, Height / 2f - tr, tr * 2f, tr * 2f);
            Ui.FillRound(g, new RectangleF(thumb.X + 1f, thumb.Y + 2f, thumb.Width, thumb.Height), tr, Theme.Alpha(Color.Black, 40));
            Ui.FillRound(g, thumb, tr, t.Accent);
            float dot = 2.4f * UiScale;
            Ui.FillRound(g, new RectangleF(tx - dot, Height / 2f - dot, dot * 2f, dot * 2f), dot, Theme.Alpha(Color.White, 220));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _drag = true;
                SetFromX(e.X);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_hover) { _hover = true; Invalidate(); }
            if (_drag) SetFromX(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_drag) { _drag = false; Invalidate(); }
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (!_drag) { _hover = false; Invalidate(); }
            base.OnMouseLeave(e);
        }

        private void SetFromX(int x)
        {
            RectangleF track = TrackRect();
            double frac = track.Width <= 0f ? 0.0 : (x - track.X) / (double)track.Width;
            if (frac < 0) frac = 0;
            if (frac > 1) frac = 1;
            double v = Minimum + (Maximum - Minimum) * frac;
            if (Step > 0) v = Math.Round(v / Step) * Step;
            Value = v;
        }
    }

    internal class NumberBox : Control
    {
        public double Minimum = 0;
        public double Maximum = 100;
        public double Step = 1;
        public int Decimals = 0;
        public float UiScale = 1f;
        public event EventHandler ValueChanged;
        private double _value;
        private int _hoverPart;

        public NumberBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Height = 34;
        }

        public double Value
        {
            get { return _value; }
            set
            {
                double v = value;
                if (v < Minimum) v = Minimum;
                if (v > Maximum) v = Maximum;
                v = Decimals <= 0 ? Math.Round(v) : Math.Round(v, Decimals);
                if (Math.Abs(v - _value) < 0.0000001) return;
                _value = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        private float BtnWidth { get { return 27f * UiScale; } }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme t = Theme.Current;
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = 8f * UiScale;
            Color fill = t.Dark ? Color.FromArgb(25, 29, 37) : Color.FromArgb(245, 247, 251);
            Ui.FillRound(g, r, radius, fill);
            float bw = BtnWidth;
            float split = Width - bw * 2f;
            Color hoverFill = t.Dark ? Color.FromArgb(43, 49, 62) : Color.FromArgb(232, 237, 245);
            if (_hoverPart == 1)
                Ui.FillRound(g, new RectangleF(split + 1f, 1f, bw - 1f, Height - 2f), radius - 1f, hoverFill);
            if (_hoverPart == 2)
                Ui.FillRound(g, new RectangleF(split + bw + 1f, 1f, bw - 1f, Height - 2f), radius - 1f, hoverFill);
            Ui.DrawRound(g, r, radius, Theme.Alpha(t.Border, 210), 1f);
            Pen sep = new Pen(Theme.Alpha(t.Border, 150), 1f);
            g.DrawLine(sep, split, 5f * UiScale, split, Height - 5f * UiScale);
            g.DrawLine(sep, split + bw, 5f * UiScale, split + bw, Height - 5f * UiScale);
            sep.Dispose();
            string text = Decimals <= 0 ? ((long)Math.Round(_value)).ToString() : _value.ToString("F" + Decimals);
            RectangleF tr = new RectangleF(12f * UiScale, 0f, split - 18f * UiScale, Height);
            Ui.Text(g, text, Font, t.Text, tr, StringAlignment.Near, StringAlignment.Center);
            bool canMinus = _value > Minimum + 0.0001;
            bool canPlus = _value < Maximum - 0.0001;
            DrawGlyph(g, Font, split + bw / 2f, "\u2212", canMinus ? (_hoverPart == 1 ? t.Accent : t.TextDim) : Theme.Alpha(t.TextFaint, 90));
            DrawGlyph(g, Font, split + bw * 1.5f, "+", canPlus ? (_hoverPart == 2 ? t.Accent : t.TextDim) : Theme.Alpha(t.TextFaint, 90));
        }

        private void DrawGlyph(Graphics g, Font f, float cx, string glyph, Color color)
        {
            if (f == null) return;
            SizeF sz = g.MeasureString(glyph, f);
            using (SolidBrush b = new SolidBrush(color))
                g.DrawString(glyph, f, b, cx - sz.Width / 2f, (Height - sz.Height) / 2f + 1f);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int part = PartAt(e.X);
            if (part != _hoverPart) { _hoverPart = part; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoverPart != 0) { _hoverPart = 0; Invalidate(); }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int part = PartAt(e.X);
                if (part == 1) Nudge(-1);
                else if (part == 2) Nudge(1);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Nudge(e.Delta > 0 ? 1 : -1);
            base.OnMouseWheel(e);
        }

        private int PartAt(int x)
        {
            float bw = BtnWidth;
            float split = Width - bw * 2f;
            if (x < split) return 0;
            return x < split + bw ? 1 : 2;
        }

        private void Nudge(int dir)
        {
            double v = _value + Step * dir;
            if (v > Maximum + 0.0001 || v < Minimum - 0.0001) return;
            Value = v;
        }
    }
}
