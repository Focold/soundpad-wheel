// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class HookTest
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr extraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    private static string _log;
    private static LowLevelProc _proc;

    private static void Main(string[] args)
    {
        int ms = 4000;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--ms") ms = int.Parse(args[++i]);
            else if (args[i] == "--log") _log = args[++i];
        }
        _proc = Hook;
        IntPtr hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        Write("hook installed=" + (hook != IntPtr.Zero) + " err=" + Marshal.GetLastWin32Error());
        if (hook == IntPtr.Zero) return;
        DateTime end = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < end)
        {
            Application.DoEvents();
            Thread.Sleep(5);
        }
        UnhookWindowsHookEx(hook);
        Write("hook removed");
    }

    private static IntPtr Hook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            KBDLLHOOKSTRUCT info = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
            if (msg == WM_KEYDOWN || msg == WM_KEYUP || msg == WM_SYSKEYDOWN || msg == WM_SYSKEYUP)
            {
                bool up = msg == WM_KEYUP || msg == WM_SYSKEYUP;
                Write("vk=" + info.vkCode + (up ? " up" : " down") + " flags=0x" + info.flags.ToString("X"));
            }
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private static void Write(string message)
    {
        string line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine;
        if (_log != null) File.AppendAllText(_log, line, Encoding.UTF8);
        else Console.Write(line);
    }
}
