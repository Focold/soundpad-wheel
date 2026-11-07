// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static class EnvProbe
{
    [DllImport("user32.dll")] private static extern IntPtr GetProcessWindowStation();
    [DllImport("user32.dll")] private static extern IntPtr GetThreadDesktop(uint threadId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetUserObjectInformation(IntPtr hObj, int index, StringBuilder info, int length, out int needed);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll")] private static extern bool CloseDesktop(IntPtr hDesktop);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vk);

    private static string Name(IntPtr handle)
    {
        StringBuilder sb = new StringBuilder(256);
        int needed;
        if (GetUserObjectInformation(handle, 2, sb, 512, out needed)) return sb.ToString();
        return "(err " + Marshal.GetLastWin32Error() + ")";
    }

    private static void Main(string[] args)
    {
        Console.WriteLine("pid=" + Process.GetCurrentProcess().Id
            + " session=" + Process.GetCurrentProcess().SessionId
            + " bitness=" + (IntPtr.Size * 8));
        Console.WriteLine("winsta=" + Name(GetProcessWindowStation()));
        Console.WriteLine("desktop=" + Name(GetThreadDesktop(GetCurrentThreadId())));
        IntPtr input = OpenInputDesktop(0, false, 0x0001);
        Console.WriteLine("inputDesktop=" + (input == IntPtr.Zero ? "OPEN FAILED err=" + Marshal.GetLastWin32Error() : Name(input)));
        if (input != IntPtr.Zero) CloseDesktop(input);
        Console.WriteLine("asyncW=" + GetAsyncKeyState(0x57));
    }
}
