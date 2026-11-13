// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Drawing;
using Microsoft.Win32;

namespace SoundWheel
{
    // 2.0 主题：深色 / 浅色两套配色，主题色来自配置，所有自绘控件都从这里取色
    internal sealed class Theme
    {
        public bool Dark;
        public Color WindowBg;
        public Color SidebarBg;
        public Color Surface;
        public Color Card;
        public Color CardHover;
        public Color Border;
        public Color Text;
        public Color TextDim;
        public Color TextFaint;
        public Color Accent;
        public Color AccentSoft;
        public Color OnAccent;
        public Color Danger;
        public Color Ok;
        public Color WheelBase;
        public Color WheelInner;
        public Color WheelOuter;
        public Color WheelEmpty;
        public Color WheelText;
        public Color WheelTextDim;
        public Color HubBg;

        public static Theme Current = Build(1, "#5B8CFF");

        public static readonly string[] AccentPresets = new string[]
        {
            "#5B8CFF", "#7C5CFF", "#B15CFF", "#FF5C8A", "#FF6B5B", "#FF9F43",
            "#FFC53D", "#A8D24A", "#35C98A", "#22C3B8", "#38BDF8", "#94A3B8"
        };

        public static Theme Build(int mode, string accentHex)
        {
            bool dark = IsDarkMode(mode);
            Theme t = new Theme();
            t.Dark = dark;
            Color accent = ColorUtil.Parse(accentHex, Color.FromArgb(91, 140, 255));
            t.Accent = accent;
            if (dark)
            {
                t.WindowBg = Color.FromArgb(20, 22, 28);
                t.SidebarBg = Color.FromArgb(26, 29, 37);
                t.Surface = Color.FromArgb(30, 34, 43);
                t.Card = Color.FromArgb(34, 39, 49);
                t.CardHover = Color.FromArgb(44, 50, 63);
                t.Border = Color.FromArgb(50, 56, 70);
                t.Text = Color.FromArgb(238, 241, 247);
                t.TextDim = Color.FromArgb(166, 173, 187);
                t.TextFaint = Color.FromArgb(112, 119, 134);
                t.AccentSoft = ColorUtil.Mix(t.Card, accent, 0.22);
                t.OnAccent = Color.White;
                t.Danger = Color.FromArgb(226, 110, 118);
                t.Ok = Color.FromArgb(92, 200, 152);
                t.WheelBase = Color.FromArgb(11, 14, 20);
                t.WheelInner = Color.FromArgb(27, 31, 41);
                t.WheelOuter = Color.FromArgb(22, 26, 34);
                t.WheelEmpty = Color.FromArgb(17, 20, 26);
                t.WheelText = Color.FromArgb(236, 241, 250);
                t.WheelTextDim = Color.FromArgb(200, 208, 224);
                t.HubBg = Color.FromArgb(14, 17, 23);
            }
            else
            {
                t.WindowBg = Color.FromArgb(244, 246, 250);
                t.SidebarBg = Color.FromArgb(250, 251, 253);
                t.Surface = Color.FromArgb(255, 255, 255);
                t.Card = Color.FromArgb(255, 255, 255);
                t.CardHover = Color.FromArgb(240, 244, 251);
                t.Border = Color.FromArgb(226, 230, 238);
                t.Text = Color.FromArgb(26, 30, 40);
                t.TextDim = Color.FromArgb(92, 100, 116);
                t.TextFaint = Color.FromArgb(142, 150, 166);
                t.AccentSoft = ColorUtil.Mix(t.Card, accent, 0.14);
                t.OnAccent = Color.White;
                t.Danger = Color.FromArgb(202, 68, 76);
                t.Ok = Color.FromArgb(30, 150, 110);
                t.WheelBase = Color.FromArgb(214, 221, 234);
                t.WheelInner = Color.FromArgb(255, 255, 255);
                t.WheelOuter = Color.FromArgb(238, 242, 249);
                t.WheelEmpty = Color.FromArgb(250, 251, 254);
                t.WheelText = Color.FromArgb(34, 40, 52);
                t.WheelTextDim = Color.FromArgb(120, 128, 142);
                t.HubBg = Color.FromArgb(255, 255, 255);
            }
            return t;
        }

        public static bool IsDarkMode(int mode)
        {
            if (mode == 1) return true;
            if (mode == 2) return false;
            return SystemIsDark();
        }

        public static bool SystemIsDark()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
                {
                    if (key != null)
                    {
                        object v = key.GetValue("AppsUseLightTheme");
                        if (v is int) return (int)v == 0;
                    }
                }
            }
            catch (Exception) { }
            return true;
        }

        public static Color Alpha(Color c, int alpha)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            return Color.FromArgb(alpha, c.R, c.G, c.B);
        }

        public Color MixAccent(double amount)
        {
            return ColorUtil.Mix(Card, Accent, amount);
        }
    }
}
