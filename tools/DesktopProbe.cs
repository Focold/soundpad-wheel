// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public static class DesktopProbe
{
    [DllImport("user32.dll")] static extern IntPtr GetProcessWindowStation();
    [DllImport("user32.dll")] static extern IntPtr GetThreadDesktop(uint tid);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll", SetLastError = true)] static extern bool GetUserObjectInformation(IntPtr h, int index, StringBuilder info, int len, out int needed);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vk);
    [DllImport("user32.dll")] static extern bool GetKeyboardState(byte[] keys);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool EnumDesktops(IntPtr hwinsta, EnumProc proc, IntPtr lparam);
    [DllImport("user32.dll")] static extern IntPtr OpenWindowStation(string name, bool inherit, uint access);
    [DllImport("user32.dll")] static extern bool CloseDesktop(IntPtr h);
    delegate bool EnumProc(string name, IntPtr lparam);

    static string DeskName(IntPtr h)
    {
        StringBuilder sb = new StringBuilder(256);
        int needed;
        if (!GetUserObjectInformation(h, 2, sb, 512, out needed)) return "ERR" + Marshal.GetLastWin32Error();
        return sb.ToString();
    }

    public static void Main(string[] args)
    {
        string log = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "deskprobe.log");
        using (StreamWriter w = new StreamWriter(log, false, Encoding.UTF8))
        {
            w.AutoFlush = true;
            w.WriteLine("pid=" + System.Diagnostics.Process.GetCurrentProcess().Id + " session=" + System.Diagnostics.Process.GetCurrentProcess().SessionId);
            w.WriteLine("winsta=" + DeskName(GetProcessWindowStation()));
            w.WriteLine("threaddesk=" + DeskName(GetThreadDesktop(GetCurrentThreadId())));
            IntPtr input = OpenInputDesktop(0, false, 0x0100);
            w.WriteLine("inputdesk=" + (input == IntPtr.Zero ? "FAIL " + Marshal.GetLastWin32Error() : DeskName(input)));
            w.WriteLine("fg=" + GetForegroundWindow());
            IntPtr st = OpenWindowStation("WinSta0", false, 0x0001);
            w.WriteLine("winsta0 handlers=" + st);
            StringBuilder acc = new StringBuilder();
            EnumDesktops(IntPtr.Zero, delegate(string name, IntPtr l) { acc.Append(name).Append(","); return true; }, IntPtr.Zero);
            w.WriteLine("desktops(in own winsta)=" + acc);
            byte[] ks = new byte[256];
            for (int i = 0; i < 60; i++)
            {
                short a = GetAsyncKeyState(0x57);
                GetKeyboardState(ks);
                w.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " async=" + a + " sync57=" + ks[0x57] + " fg=" + GetForegroundWindow());
                Thread.Sleep(100);
            }
        }
    }
}
