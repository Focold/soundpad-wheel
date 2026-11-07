// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SoundWheel
{
    // 低级鼠标钩子（WH_MOUSE_LL）：打开轮盘时把鼠标接管过来。
    // 钩子回调里只做"吞掉消息 + 记位移"，绝不重绘或干重活，避免超过系统的 LowLevelHooksTimeout 被摘除。
    internal sealed class MouseHook : IDisposable
    {
        private Native.LowLevelMouseProc _proc;
        private IntPtr _hook = IntPtr.Zero;
        private Point _last;
        private bool _hasLast;
        private int _swallowed;
        private int _events;

        // 为 true 时吞掉所有鼠标消息，不再转发给任何窗口（游戏收不到，也不会开枪/转视角）
        public bool Capturing;

        // 程序自己调用 SetCursorPos 时的抑制标记，避免把自己的移动又累加一次
        public bool Suppress;

        public event Action<int, int> Moved;
        public event Action LeftClick;
        public event Action RightClick;

        public bool Installed { get { return _hook != IntPtr.Zero; } }
        public int SwallowedCount { get { return _swallowed; } }
        public int EventCount { get { return _events; } }

        public bool Install()
        {
            if (_hook != IntPtr.Zero) return true;
            _proc = HookProc;
            try
            {
                _hook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _proc, Native.GetModuleHandle(null), 0);
            }
            catch (Exception)
            {
                _hook = IntPtr.Zero;
            }
            if (_hook == IntPtr.Zero)
            {
                Log.Info("鼠标钩子安装失败 err=" + Marshal.GetLastWin32Error());
                return false;
            }
            _hasLast = false;
            _swallowed = 0;
            _events = 0;
            Log.Info("鼠标钩子已安装");
            return true;
        }

        // 我们自己用 SetCursorPos 把光标钉到虚拟指针后调用：把基准点设成我们刚设的位置，
        // 这样下一次真实移动算出来的差值仍然是真实位移，不会丢掉一条事件（旧实现直接清空基准会丢位移，手感发粘）
        public void NoteCursor(int x, int y)
        {
            _last = new Point(x, y);
            _hasLast = true;
        }

        public void Uninstall()
        {
            Capturing = false;
            Suppress = false;
            if (_hook == IntPtr.Zero) return;
            try { Native.UnhookWindowsHookEx(_hook); }
            catch (Exception) { }
            _hook = IntPtr.Zero;
        }

        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0 || !Capturing) return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

            int msg = Native.LowWord(wParam);
            _events++;

            if (msg == 0x0200) // WM_MOUSEMOVE
            {
                Native.MSLLHOOKSTRUCT info;
                try { info = (Native.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.MSLLHOOKSTRUCT)); }
                catch (Exception) { return (IntPtr)1; }

                if (!Suppress)
                {
                    int dx = 0;
                    int dy = 0;
                    if (_hasLast)
                    {
                        dx = info.pt.X - _last.X;
                        dy = info.pt.Y - _last.Y;
                    }
                    _last = new Point(info.pt.X, info.pt.Y);
                    _hasLast = true;
                    if (dx != 0 || dy != 0)
                    {
                        Action<int, int> handler = Moved;
                        if (handler != null) handler(dx, dy);
                    }
                }
                _swallowed++;
                return (IntPtr)1;
            }

            if (msg == 0x0202 || msg == 0x0205) // 左键/右键抬起
            {
                _swallowed++;
                if (!Suppress)
                {
                    Action handler = msg == 0x0202 ? LeftClick : RightClick;
                    if (handler != null) handler();
                }
                return (IntPtr)1;
            }

            if (msg == 0x0201 || msg == 0x0204 || msg == 0x0207 || msg == 0x0208 || msg == 0x020B || msg == 0x020C
                || msg == 0x020A || msg == 0x020E) // 按下 / 中键 / 侧键 / 滚轮
            {
                _swallowed++;
                return (IntPtr)1;
            }

            return (IntPtr)1;
        }

        public void Dispose()
        {
            Uninstall();
        }
    }
}
