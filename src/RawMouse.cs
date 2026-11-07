// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SoundWheel
{
    // 独立的隐藏窗口，只用来接收鼠标原始输入（WM_INPUT）。
    // 必须单独开一个窗口：轮盘本身是 WS_EX_NOACTIVATE 的分层窗口，
    // 实测这种窗口收不到 WM_INPUT，所以原始输入挂在没有该样式的独立窗口上。
    // 注意必须用 Form（WinForms 自己的窗口过程）而不是裸 NativeWindow：
    // NativeWindow 创建的窗口消息不会回到 WndProc，导致注册成功却永远收不到 WM_INPUT。
    internal sealed class RawMouseWindow : Form
    {
        public event Action<int, int> Moved;

        public RawMouseWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-32000, -32000);
            Size = new Size(1, 1);
            Text = "SoundWheelRawMouse";
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        // 只创建句柄，不显示窗口（显示会抢焦点，游戏里绝对不能做）
        public void EnsureHandle()
        {
            IntPtr h = Handle;
        }

        public bool Register()
        {
            return Native.RegisterRawMouse(Handle, true);
        }

        public void Unregister()
        {
            try { Native.RegisterRawMouse(Handle, false); }
            catch (Exception) { }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_INPUT)
            {
                int dx;
                int dy;
                bool ok = Native.ReadRawMouse(m.LParam, out dx, out dy);
                if (WheelWindow.MouseDebug) Log.Info("原始鼠标原始数据 ok=" + ok + " dx=" + dx + " dy=" + dy);
                if (ok)
                {
                    Action<int, int> handler = Moved;
                    if (handler != null) handler(dx, dy);
                }
                return;
            }
            base.WndProc(ref m);
        }
    }
}
