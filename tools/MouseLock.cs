// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
// 测试工具：把光标 ClipCursor 到屏幕中心（模拟 FPS 游戏的锁定鼠标），并按需注入相对位移。
using System;
using System.Runtime.InteropServices;
using System.Threading;

internal static class MouseLock
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public MOUSEINPUT mi;
    }

    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool ClipCursor(ref RECT rect);
    [DllImport("user32.dll")] private static extern bool ClipCursor(IntPtr rect);
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    private static void Move(int dx, int dy)
    {
        INPUT[] list = new INPUT[1];
        list[0].type = 0;
        list[0].mi.dx = dx;
        list[0].mi.dy = dy;
        list[0].mi.dwFlags = 0x0001;
        uint sent = SendInput(1, list, Marshal.SizeOf(typeof(INPUT)));
        Console.WriteLine("send dx=" + dx + " dy=" + dy + " sent=" + sent + " err=" + Marshal.GetLastWin32Error());
    }

    private static void Main(string[] args)
    {
        int ms = 6000;
        int dx = 0;
        int dy = 0;
        int steps = 6;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--ms" && i + 1 < args.Length) { int.TryParse(args[i + 1], out ms); i++; }
            else if (args[i] == "--dx" && i + 1 < args.Length) { int.TryParse(args[i + 1], out dx); i++; }
            else if (args[i] == "--dy" && i + 1 < args.Length) { int.TryParse(args[i + 1], out dy); i++; }
            else if (args[i] == "--steps" && i + 1 < args.Length) { int.TryParse(args[i + 1], out steps); i++; }
        }

        try { SetProcessDPIAware(); } catch (Exception) { }
        int cx = GetSystemMetrics(0) / 2;
        int cy = GetSystemMetrics(1) / 2;
        Console.WriteLine("screen=" + GetSystemMetrics(0) + "x" + GetSystemMetrics(1) + " center=" + cx + "," + cy);

        SetCursorPos(cx, cy);
        RECT clip = new RECT();
        clip.Left = cx;
        clip.Top = cy;
        clip.Right = cx + 1;
        clip.Bottom = cy + 1;
        bool locked = ClipCursor(ref clip);
        Console.WriteLine("clipped=" + locked);
        Console.Out.Flush();

        int per = steps > 0 ? (int)Math.Round((double)ms / steps) : ms;
        for (int k = 0; k < steps; k++)
        {
            if (dx != 0 || dy != 0) Move(dx, dy);
            Thread.Sleep(Math.Max(20, per));
        }
        Thread.Sleep(600);
        ClipCursor(IntPtr.Zero);
        Console.WriteLine("released");
    }
}
