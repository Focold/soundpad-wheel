// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

internal static class KeySim
{
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
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION u;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vk);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint code, uint mapType);

    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static void Main(string[] args)
    {
        SetProcessDPIAware();
        int hold = 1500, mx = -1, my = -1, pre = 400, ax = -1, ay = -1;
        string shot = null, log = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--hold") hold = int.Parse(args[++i]);
            else if (args[i] == "--move") { mx = int.Parse(args[++i]); my = int.Parse(args[++i]); }
            else if (args[i] == "--pre") pre = int.Parse(args[++i]);
            else if (args[i] == "--at") { ax = int.Parse(args[++i]); ay = int.Parse(args[++i]); }
            else if (args[i] == "--shot") shot = args[++i];
            else if (args[i] == "--log") log = args[++i];
        }

        Note(log, "start");
        if (ax >= 0) { SetCursorPos(ax, ay); Thread.Sleep(120); }
        Send(0x11, false);
        Thread.Sleep(80);
        Send(0x12, false);
        Thread.Sleep(80);
        Send(0x57, false);
        Thread.Sleep(150);
        Note(log, "down ctrl=" + GetAsyncKeyState(0x11) + " alt=" + GetAsyncKeyState(0x12) + " w=" + GetAsyncKeyState(0x57));
        Thread.Sleep(pre);
        if (mx >= 0) SetCursorPos(mx, my);
        Note(log, "moved " + mx + "," + my + " w=" + GetAsyncKeyState(0x57));
        Thread.Sleep(pre);
        if (shot != null) Capture(shot);
        Thread.Sleep(hold);
        Send(0x57, true);
        Send(0x12, true);
        Send(0x11, true);
        Note(log, "up");
        Thread.Sleep(700);
    }

    private static void Note(string path, string message)
    {
        if (path == null) return;
        try { System.IO.File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine); }
        catch (Exception) { }
    }

    private static void Send(ushort vk, bool up)
    {
        INPUT input = new INPUT();
        input.type = 1;
        input.u.ki.wVk = vk;
        input.u.ki.wScan = (ushort)MapVirtualKey(vk, 0);
        input.u.ki.dwFlags = up ? KEYEVENTF_KEYUP : 0;
        INPUT[] inputs = new INPUT[] { input };
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
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
}
