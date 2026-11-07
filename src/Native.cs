// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SoundWheel
{
    internal static class Native
    {
        public const int WM_HOTKEY = 0x0312;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int MA_NOACTIVATE = 3;

        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const int SW_HIDE = 0;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int ULW_ALPHA = 0x00000002;
        public const byte AC_SRC_ALPHA = 1;

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int CX;
            public int CY;
            public SIZE(int cx, int cy) { CX = cx; CY = cy; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT pt);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("Shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst,
            ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool PeekNamedPipe(IntPtr hNamedPipe, IntPtr lpBuffer, uint nBufferSize,
            IntPtr lpBytesRead, out uint lpTotalBytesAvail, IntPtr lpBytesLeftThisMessage);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr obj);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        public static bool IsDown(int vk)
        {
            return (GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetProcessWindowStation();

        [DllImport("user32.dll")]
        public static extern IntPtr GetThreadDesktop(uint dwThreadId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex,
            System.Text.StringBuilder pvInfo, int nLength, out int lpnLengthNeeded);

        public static string ObjectName(IntPtr h)
        {
            if (h == IntPtr.Zero) return "(null)";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
            int needed;
            if (!GetUserObjectInformation(h, 2, sb, 512, out needed)) return "(err" + Marshal.GetLastWin32Error() + ")";
            return sb.ToString();
        }

        public static string EnvironmentInfo()
        {
            IntPtr input = OpenInputDesktop(0, false, 0x0100);
            return "session=" + System.Diagnostics.Process.GetCurrentProcess().SessionId
                + " winsta=" + ObjectName(GetProcessWindowStation())
                + " desk=" + ObjectName(GetThreadDesktop(GetCurrentThreadId()))
                + " inputdesk=" + (input == IntPtr.Zero ? "fail" + Marshal.GetLastWin32Error() : ObjectName(input))
                + " fg=" + GetForegroundWindow();
        }

        public static void EnableDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(new IntPtr(-4))) return;
            }
            catch (Exception) { }
            try { SetProcessDPIAware(); } catch (Exception) { }
        }

        public static float GetDpiScale(Point screenPoint)
        {
            try
            {
                IntPtr mon = MonitorFromPoint(new POINT(screenPoint.X, screenPoint.Y), 2);
                if (mon != IntPtr.Zero)
                {
                    uint dpiX;
                    uint dpiY;
                    if (GetDpiForMonitor(mon, 0, out dpiX, out dpiY) == 0 && dpiX > 0)
                        return dpiX / 96f;
                }
            }
            catch (Exception) { }
            return 1f;
        }

        public static void ShowNoActivate(IntPtr hwnd)
        {
            ShowWindow(hwnd, SW_SHOWNOACTIVATE);
        }

        public static void HideWindow(IntPtr hwnd)
        {
            ShowWindow(hwnd, SW_HIDE);
        }
    }
}
