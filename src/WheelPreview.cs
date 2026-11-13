// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace SoundWheel
{
    // 设置界面左半的轮盘预览：可直接把音效拖进扇区，右键清除
    internal sealed class WheelPreview : Control
    {
        private AppConfig _config;
        private readonly WheelGeometry _geo = new WheelGeometry();
        private int _hoverRing = -1;
        private int _hoverSlot = -1;
        private int _dragRing = -1;
        private int _dragSlot = -1;
        private float _offX;
        private float _offY;
        private Font _innerFont;
        private Font _outerFont;
        private Font _hubFont;

        public const string DragFormat = "SoundWheel.Sound";

        public event Action<int, int> SlotClicked;
        public event Action<int, int> SlotCleared;
        public event Action<int, int, int, string> SlotDropped;

        public WheelPreview()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            AllowDrop = true;
            DoubleBuffered = true;
        }

        public void SetConfig(AppConfig config)
        {
            _config = config;
            Rebuild();
        }

        public void RefreshData()
        {
            Rebuild();
        }

        private void Rebuild()
        {
            if (_config == null) return;
            float side = Math.Min(ClientSize.Width, ClientSize.Height);
            if (side < 80f) side = 80f;
            side -= 10f;
            float maxR = _config.OuterCount > 0 ? 247f : 150f;
            float design = (maxR + 26f) * 2f;
            float scale = side / design;
            _geo.Build(scale, _config.InnerCount, _config.OuterCount);
            _offX = (ClientSize.Width - _geo.Size) / 2f;
            _offY = (ClientSize.Height - _geo.Size) / 2f;
            DisposeFonts();
            float fs = (float)_config.FontScale;
            if (fs < 0.85f) fs = 0.85f;
            if (fs > 1.35f) fs = 1.35f;
            _innerFont = MakeFont(13f * scale * fs, false);
            _outerFont = MakeFont(13f * scale * fs, false);
            _hubFont = MakeFont(14f * scale * fs, true);
            Invalidate();
        }

        private static Font MakeFont(float size, bool bold)
        {
            if (size < 7f) size = 7f;
            try
            {
                return new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
            }
            catch (Exception)
            {
                return new Font(FontFamily.GenericSansSerif, size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
            }
        }

        private void DisposeFonts()
        {
            if (_innerFont != null) { _innerFont.Dispose(); _innerFont = null; }
            if (_outerFont != null) { _outerFont.Dispose(); _outerFont = null; }
            if (_hubFont != null) { _hubFont.Dispose(); _hubFont = null; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeFonts();
            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Rebuild();
        }

        private PointF PointOnCircle(float cx, float cy, float radius, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            return new PointF(cx + (float)(Math.Cos(rad) * radius), cy + (float)(Math.Sin(rad) * radius));
        }

        private GraphicsPath SegmentPath(float cx, float cy, float rIn, float rOut, double startAngle, double sweep)
        {
            GraphicsPath path = new GraphicsPath();
            RectangleF outer = new RectangleF(cx - rOut, cy - rOut, rOut * 2f, rOut * 2f);
            RectangleF inner = new RectangleF(cx - rIn, cy - rIn, rIn * 2f, rIn * 2f);
            path.AddArc(outer, (float)startAngle, (float)sweep);
            path.AddArc(inner, (float)(startAngle + sweep), (float)(-sweep));
            path.CloseFigure();
            return path;
        }

        private void HitTest(Point p, out int ring, out int slot)
        {
            ring = -1;
            slot = -1;
            float dx = p.X - (_offX + _geo.Center);
            float dy = p.Y - (_offY + _geo.Center);
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            double angle = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
            if (angle < 0) angle += 360.0;
            if (dist <= _geo.HubR) return;
            if (dist >= _geo.InnerIn && dist <= _geo.InnerOut && _geo.InnerCount > 0)
            {
                ring = 0;
                slot = (int)(angle / (360.0 / _geo.InnerCount));
                if (slot >= _geo.InnerCount) slot = _geo.InnerCount - 1;
                return;
            }
            if (_geo.OuterCount > 0 && dist >= _geo.OuterIn && dist <= _geo.OuterOut)
            {
                ring = 1;
                slot = (int)(angle / (360.0 / _geo.OuterCount));
                if (slot >= _geo.OuterCount) slot = _geo.OuterCount - 1;
            }
        }

        private SlotConfig SlotOf(int ring, int slot)
        {
            if (_config == null) return null;
            if (ring == 0 && slot >= 0 && slot < _config.Inner.Count && slot < _config.InnerCount) return _config.Inner[slot];
            if (ring == 1 && slot >= 0 && slot < _config.Outer.Count && slot < _config.OuterCount) return _config.Outer[slot];
            return null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_config == null)
            {
                base.OnPaint(e);
                return;
            }
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Theme t = Theme.Current;
            g.TranslateTransform(_offX, _offY);
            float cx = _geo.Center;
            float cy = _geo.Center;

            float plateR = _geo.OuterCount > 0 ? _geo.OuterOut : _geo.InnerOut;
            using (SolidBrush plate = new SolidBrush(Theme.Alpha(t.WheelBase, t.Dark ? 235 : 245)))
                g.FillEllipse(plate, cx - plateR, cy - plateR, plateR * 2f, plateR * 2f);

            DrawRing(g, 0, _geo.InnerIn, _geo.InnerOut, _geo.InnerCount, _innerFont);
            if (_geo.OuterCount > 0)
                DrawRing(g, 1, _geo.OuterIn, _geo.OuterOut, _geo.OuterCount, _outerFont);

            DrawHub(g, cx, cy);
            g.TranslateTransform(-_offX, -_offY);
        }

        private void DrawRing(Graphics g, int ring, float rIn, float rOut, int count, Font font)
        {
            if (count <= 0) return;
            Theme t = Theme.Current;
            float cx = _geo.Center;
            float cy = _geo.Center;
            float per = 360f / count;
            for (int i = 0; i < count; i++)
            {
                SlotConfig sc = SlotOf(ring, i);
                bool assigned = sc != null && sc.IsSet;
                bool active = (ring == _hoverRing && i == _hoverSlot) || (ring == _dragRing && i == _dragSlot);
                double start = -90.0 + i * per + Math.Min(0.9, per * 0.08);
                double sweep = per - 2 * Math.Min(0.9, per * 0.08);
                Color fill;
                if (active) fill = Theme.Alpha(t.Accent, 235);
                else if (assigned) fill = ring == 0 ? t.WheelInner : t.WheelOuter;
                else fill = t.WheelEmpty;

                using (GraphicsPath path = SegmentPath(cx, cy, rIn, rOut, start, sweep))
                using (SolidBrush b = new SolidBrush(fill))
                    g.FillPath(b, path);

                if (active)
                {
                    using (GraphicsPath path = SegmentPath(cx, cy, rIn + 1.5f, rOut - 1.5f, start, sweep))
                    using (Pen p = new Pen(Theme.Alpha(t.Dark ? Color.White : t.Accent, 220), 1.6f))
                        g.DrawPath(p, path);
                }

                string label = assigned && !string.IsNullOrEmpty(sc.Title) ? sc.Title : "空闲";
                Color textColor = active ? t.OnAccent : (assigned ? t.WheelText : t.WheelTextDim);
                float midR = (rIn + rOut) / 2f;
                PointF center = PointOnCircle(cx, cy, midR, start + sweep / 2.0);
                float band = rOut - rIn;
                float width = (float)(2.0 * Math.PI * midR / count * 0.78);
                if (width > band * 2.1f) width = band * 2.1f;
                float h = font.GetHeight(g) * 1.25f;
                RectangleF rect = new RectangleF(center.X - width / 2f, center.Y - h / 2f, width, h);
                Ui.Text(g, label, font, textColor, rect, StringAlignment.Center, StringAlignment.Center);
            }
        }

        private void DrawHub(Graphics g, float cx, float cy)
        {
            Theme t = Theme.Current;
            float r = _geo.HubR;
            using (SolidBrush b = new SolidBrush(Theme.Alpha(t.HubBg, 250)))
                g.FillEllipse(b, cx - r, cy - r, r * 2f, r * 2f);
            using (Pen p = new Pen(Theme.Alpha(t.Accent, 130), 1.4f))
                g.DrawEllipse(p, cx - r + 0.7f, cy - r + 0.7f, r * 2f - 1.4f, r * 2f - 1.4f);

            string title = "拖入音效";
            SlotConfig hov = SlotOf(_dragRing >= 0 ? _dragRing : _hoverRing, _dragRing >= 0 ? _dragSlot : _hoverSlot);
            if (hov != null && hov.IsSet && !string.IsNullOrEmpty(hov.Title)) title = hov.Title;
            else if (hov != null && !hov.IsSet) title = "空闲位置";
            RectangleF rect = new RectangleF(cx - r + 6f, cy - 16f, r * 2f - 12f, 32f);
            Ui.Text(g, title, _hubFont, t.Text, rect, StringAlignment.Center, StringAlignment.Center);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int ring, slot;
            HitTest(e.Location, out ring, out slot);
            if (ring != _hoverRing || slot != _hoverSlot)
            {
                _hoverRing = ring;
                _hoverSlot = slot;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoverRing != -1 || _hoverSlot != -1)
            {
                _hoverRing = -1;
                _hoverSlot = -1;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int ring, slot;
            HitTest(e.Location, out ring, out slot);
            if (ring >= 0 && slot >= 0)
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (SlotCleared != null) SlotCleared(ring, slot);
                }
                else if (e.Button == MouseButtons.Left)
                {
                    if (SlotClicked != null) SlotClicked(ring, slot);
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DragFormat))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
            base.OnDragEnter(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            int ring, slot;
            HitTest(PointToClient(new Point(e.X, e.Y)), out ring, out slot);
            _dragRing = ring;
            _dragSlot = slot;
            Invalidate();
            e.Effect = (ring >= 0 && slot >= 0 && e.Data.GetDataPresent(DragFormat)) ? DragDropEffects.Copy : DragDropEffects.None;
            base.OnDragOver(e);
        }

        protected override void OnDragLeave(EventArgs e)
        {
            _dragRing = -1;
            _dragSlot = -1;
            Invalidate();
            base.OnDragLeave(e);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            int ring, slot;
            HitTest(PointToClient(new Point(e.X, e.Y)), out ring, out slot);
            _dragRing = -1;
            _dragSlot = -1;
            Invalidate();
            string raw = e.Data != null && e.Data.GetDataPresent(DragFormat) ? (e.Data.GetData(DragFormat) as string) : null;
            if (ring >= 0 && slot >= 0 && !string.IsNullOrEmpty(raw))
            {
                int nl = raw.IndexOf('\n');
                string idxText = nl >= 0 ? raw.Substring(0, nl) : raw;
                string title = nl >= 0 ? raw.Substring(nl + 1) : "";
                int index;
                if (int.TryParse(idxText, out index) && SlotDropped != null)
                    SlotDropped(ring, slot, index, title);
            }
            base.OnDragDrop(e);
        }
    }
}
