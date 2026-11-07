// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class MakeIcon
{
    private static readonly int[] Sizes = new int[] { 16, 32, 48, 64, 128, 256 };

    private static void Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "app.ico";
        List<byte[]> images = new List<byte[]>();
        foreach (int s in Sizes)
        {
            using (Bitmap bmp = Render(s))
                images.Add(DibBytes(bmp));
        }

        using (MemoryStream ms = new MemoryStream())
        {
            BinaryWriter w = new BinaryWriter(ms);
            w.Write((short)0);
            w.Write((short)1);
            w.Write((short)images.Count);
            int offset = 6 + 16 * images.Count;
            for (int i = 0; i < images.Count; i++)
            {
                int s = Sizes[i];
                w.Write((byte)(s == 256 ? 0 : s));
                w.Write((byte)(s == 256 ? 0 : s));
                w.Write((byte)0);
                w.Write((byte)0);
                w.Write((short)1);
                w.Write((short)32);
                w.Write(images[i].Length);
                w.Write(offset);
                offset += images[i].Length;
            }
            foreach (byte[] img in images) w.Write(img);
            w.Flush();
            File.WriteAllBytes(outPath, ms.ToArray());
        }
        Console.WriteLine("icon written: " + outPath);
    }

    private static Bitmap Render(int size)
    {
        Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            float pad = size * 0.035f;
            float c = size / 2f;
            float outerR = size / 2f - pad;
            RectangleF disk = new RectangleF(c - outerR, c - outerR, outerR * 2, outerR * 2);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(255, 24, 29, 42)))
                g.FillEllipse(bg, disk);

            int wedgeCount = 8;
            float rOut = outerR * 0.97f;
            float rIn = outerR * 0.46f;
            for (int i = 0; i < wedgeCount; i++)
            {
                float a0 = -90f + i * (360f / wedgeCount) + (size > 32 ? 1.5f : 0f);
                float a1 = -90f + (i + 1) * (360f / wedgeCount) - (size > 32 ? 1.5f : 0f);
                bool hot = i == 1;
                Color col = hot ? Color.FromArgb(255, 91, 140, 255) : Color.FromArgb(255, 58, 70, 100);
                using (GraphicsPath p = Segment(c, c, rOut, rIn, a0, a1))
                using (SolidBrush br = new SolidBrush(col))
                    g.FillPath(br, p);
            }

            float coreR = rIn * 0.82f;
            using (SolidBrush core = new SolidBrush(Color.FromArgb(255, 24, 29, 42)))
                g.FillEllipse(core, new RectangleF(c - coreR, c - coreR, coreR * 2, coreR * 2));
            if (size >= 32)
            {
                using (Pen ring = new Pen(Color.FromArgb(235, 110, 158, 255), Math.Max(1.5f, size * 0.028f)))
                    g.DrawEllipse(ring, new RectangleF(c - coreR * 0.72f, c - coreR * 0.72f, coreR * 1.44f, coreR * 1.44f));
            }
        }
        return bmp;
    }

    private static GraphicsPath Segment(float cx, float cy, float rOut, float rIn, float a0, float a1)
    {
        GraphicsPath p = new GraphicsPath();
        float sw = a1 - a0;
        p.AddArc(cx - rOut, cy - rOut, rOut * 2, rOut * 2, a0, sw);
        p.AddArc(cx - rIn, cy - rIn, rIn * 2, rIn * 2, a1, -sw);
        p.CloseFigure();
        return p;
    }

    private static byte[] DibBytes(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        int maskStride = ((w + 31) / 32) * 4;
        int imageSize = w * h * 4 + maskStride * h;
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(40);
            bw.Write(w);
            bw.Write(h * 2);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(0);
            bw.Write(imageSize);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            for (int y = h - 1; y >= 0; y--)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    bw.Write(c.B);
                    bw.Write(c.G);
                    bw.Write(c.R);
                    bw.Write(c.A);
                }
            }
            for (int y = h - 1; y >= 0; y--)
            {
                byte[] row = new byte[maskStride];
                for (int x = 0; x < w; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    if (c.A < 128) row[x / 8] |= (byte)(0x80 >> (x % 8));
                }
                bw.Write(row);
            }
            bw.Flush();
            return ms.ToArray();
        }
    }
}
