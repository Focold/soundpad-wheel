// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SoundWheel
{
    internal sealed class HotkeySpec
    {
        public uint Modifiers;
        public int VirtualKey;

        public HotkeySpec() { }

        public HotkeySpec(uint modifiers, int virtualKey)
        {
            Modifiers = modifiers;
            VirtualKey = virtualKey;
        }

        public static HotkeySpec Default
        {
            get { return new HotkeySpec(Native.MOD_CONTROL | Native.MOD_ALT, (int)Keys.W); }
        }

        public bool IsValid
        {
            get { return VirtualKey > 0; }
        }

        public string Display
        {
            get { return HotkeyNames.Describe(this); }
        }

        public HotkeySpec Clone()
        {
            return new HotkeySpec(Modifiers, VirtualKey);
        }

        public bool SameAs(HotkeySpec other)
        {
            if (other == null) return false;
            return Modifiers == other.Modifiers && VirtualKey == other.VirtualKey;
        }

        public static HotkeySpec FromJson(Dictionary<string, object> d)
        {
            int vk = Json.Int(d, "vk", 0);
            uint mods = (uint)Json.Int(d, "mods", 0);
            if (vk <= 0) return null;
            return new HotkeySpec(mods, vk);
        }

        public Dictionary<string, object> ToJson()
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            d["mods"] = (int)Modifiers;
            d["vk"] = VirtualKey;
            d["text"] = Display;
            return d;
        }
    }

    internal static class HotkeyNames
    {
        public static string Describe(HotkeySpec spec)
        {
            if (spec == null || spec.VirtualKey <= 0) return "未设置";
            List<string> parts = new List<string>();
            if ((spec.Modifiers & Native.MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((spec.Modifiers & Native.MOD_ALT) != 0) parts.Add("Alt");
            if ((spec.Modifiers & Native.MOD_SHIFT) != 0) parts.Add("Shift");
            if ((spec.Modifiers & Native.MOD_WIN) != 0) parts.Add("Win");
            parts.Add(KeyText((Keys)spec.VirtualKey));
            return string.Join(" + ", parts.ToArray());
        }

        public static string KeyText(Keys key)
        {
            switch (key)
            {
                case Keys.Space: return "Space";
                case Keys.Return: return "Enter";
                case Keys.Escape: return "Esc";
                case Keys.Back: return "Backspace";
                case Keys.Delete: return "Delete";
                case Keys.Insert: return "Insert";
                case Keys.Home: return "Home";
                case Keys.End: return "End";
                case Keys.PageUp: return "PageUp";
                case Keys.PageDown: return "PageDown";
                case Keys.Up: return "↑";
                case Keys.Down: return "↓";
                case Keys.Left: return "←";
                case Keys.Right: return "→";
                case Keys.Oemtilde: return "~";
                case Keys.OemMinus: return "-";
                case Keys.Oemplus: return "=";
                case Keys.OemOpenBrackets: return "[";
                case Keys.OemCloseBrackets: return "]";
                case Keys.OemPipe: return "\\";
                case Keys.OemSemicolon: return ";";
                case Keys.OemQuotes: return "'";
                case Keys.Oemcomma: return ",";
                case Keys.OemPeriod: return ".";
                case Keys.OemQuestion: return "/";
                default: return key.ToString();
            }
        }
    }

    internal sealed class HotkeyBox : TextBox
    {
        private HotkeySpec _spec;

        public HotkeyBox()
        {
            ReadOnly = true;
            Cursor = Cursors.Hand;
            TextAlign = HorizontalAlignment.Center;
            ShortcutsEnabled = false;
            BackColor = Color.White;
        }

        public HotkeySpec Spec
        {
            get { return _spec; }
            set
            {
                _spec = value;
                Text = (value == null) ? "点击后按下快捷键" : value.Display;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!Focused) return base.ProcessCmdKey(ref msg, keyData);
            if (!CaptureKeys(ref keyData)) return true;
            return true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Focused && e.KeyCode != Keys.None)
            {
                Keys keyData = e.KeyData;
                CaptureKeys(ref keyData);
                e.SuppressKeyPress = true;
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        private bool CaptureKeys(ref Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu ||
                key == Keys.LWin || key == Keys.RWin || key == Keys.None)
                return false;

            if (key == Keys.Back || key == Keys.Delete)
            {
                Spec = null;
                return false;
            }
            if (key == Keys.Escape) return false;

            uint mods = 0;
            if ((keyData & Keys.Control) == Keys.Control) mods |= Native.MOD_CONTROL;
            if ((keyData & Keys.Alt) == Keys.Alt) mods |= Native.MOD_ALT;
            if ((keyData & Keys.Shift) == Keys.Shift) mods |= Native.MOD_SHIFT;

            Spec = new HotkeySpec(mods, (int)key);
            return true;
        }
    }
}
