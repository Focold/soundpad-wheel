// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

internal static class ImgTool
{
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();

    private static void Main(string[] args)
    {
        SetProcessDPIAware();
        if (args.Length == 0) { Usage(); return; }
        switch (args[0])
        {
            case "capture":
                Capture(args[1]);
                break;
            case "diff":
                Diff(args[1], args[2]);
                break;
            case "crop":
                Crop(args[1], int.Parse(args[2]), int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]), args[6]);
                break;
            default:
                Usage();
                break;
        }
    }

    private static void Usage()
    {
        Console.WriteLine("capture <out.png> | diff <a.png> <b.png> | crop <src> <x> <y> <w> <h> <out>");
    }

    private static void Capture(string path)
    {
        Rectangle r = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
        using (Bitmap bmp = new Bitmap(r.Width, r.Height))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(r.X, r.Y, 0, 0, bmp.Size);
            bmp.Save(path, ImageFormat.Png);
        }
        Console.WriteLine("captured " + r.Width + "x" + r.Height);
    }

    private static void Crop(string src, int x, int y, int w, int h, string outPath)
    {
        using (Bitmap bmp = new Bitmap(src))
        {
            x = Math.Max(0, Math.Min(bmp.Width - 1, x));
            y = Math.Max(0, Math.Min(bmp.Height - 1, y));
            w = Math.Min(w, bmp.Width - x);
            h = Math.Min(h, bmp.Height - y);
            using (Bitmap part = new Bitmap(w, h))
            {
                using (Graphics g = Graphics.FromImage(part)) g.DrawImage(bmp, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                part.Save(outPath, ImageFormat.Png);
            }
        }
        Console.WriteLine("cropped " + w + "x" + h);
    }

    private static void Diff(string pa, string pb)
    {
        using (Bitmap a = new Bitmap(pa))
        using (Bitmap b = new Bitmap(pb))
        {
            int w = Math.Min(a.Width, b.Width), h = Math.Min(a.Height, b.Height);
            BitmapData da = a.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData db = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] ba = new byte[da.Stride * h];
            byte[] bb = new byte[db.Stride * h];
            Marshal.Copy(da.Scan0, ba, 0, ba.Length);
            Marshal.Copy(db.Scan0, bb, 0, bb.Length);
            a.UnlockBits(da);
            b.UnlockBits(db);

            int minx = w, miny = h, maxx = -1, maxy = -1, bs = 8, changed = 0;
            for (int by = 0; by < h; by += bs)
            {
                for (int bx = 0; bx < w; bx += bs)
                {
                    int cnt = 0;
                    for (int y = by; y < Math.Min(by + bs, h) && cnt < 6; y++)
                    {
                        int off = y * da.Stride + bx * 4;
                        for (int x = bx; x < Math.Min(bx + bs, w); x++, off += 4)
                        {
                            int d = Math.Abs(ba[off] - bb[off]) + Math.Abs(ba[off + 1] - bb[off + 1]) + Math.Abs(ba[off + 2] - bb[off + 2]);
                            if (d > 30) { cnt++; if (cnt >= 6) break; }
                        }
                    }
                    if (cnt >= 6)
                    {
                        changed++;
                        if (bx < minx) minx = bx;
                        if (bx > maxx) maxx = bx;
                        if (by < miny) miny = by;
                        if (by > maxy) maxy = by;
                    }
                }
            }
            if (maxx < 0) { Console.WriteLine("BBOX none"); return; }
            Console.WriteLine("BBOX " + minx + " " + miny + " " + (maxx - minx + bs) + " " + (maxy - miny + bs) + " blocks=" + changed);
        }
    }
}
