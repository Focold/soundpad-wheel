// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace SoundWheel
{
    internal sealed class SlotConfig
    {
        public int Index;
        public string Title = "";

        public bool IsSet
        {
            get { return Index > 0 || (Title != null && Title.Length > 0); }
        }

        public SlotConfig Clone()
        {
            SlotConfig c = new SlotConfig();
            c.Index = Index;
            c.Title = Title;
            return c;
        }
    }

    internal sealed class AppConfig
    {
        public HotkeySpec Hotkey = HotkeySpec.Default;
        public bool ToggleMode = false;
        public bool PlayToMic = true;
        public bool PlayToSpeaker = true;
        public bool ShowToast = false;
        public bool AutoStart = false;
        public bool Animations = true;
        public double Scale = 1.0;
        public string Accent = "#5B8CFF";
        public int InnerCount = 8;
        public int OuterCount = 12;
        public int MouseMode = 0;              // 0=自动 1=绝对坐标 2=相对位移
        public double MouseSensitivity = 1.0;  // 相对位移灵敏度
        public bool CaptureMouse = true;       // 打开轮盘时用低级鼠标钩子接管鼠标（吞掉移动/点击）
        public List<SlotConfig> Inner = new List<SlotConfig>();
        public List<SlotConfig> Outer = new List<SlotConfig>();

        public AppConfig()
        {
            for (int i = 0; i < 12; i++) Inner.Add(new SlotConfig());
            for (int i = 0; i < 16; i++) Outer.Add(new SlotConfig());
        }

        public void Normalize()
        {
            if (Hotkey == null) Hotkey = HotkeySpec.Default;
            if (Scale < 0.7) Scale = 0.7;
            if (Scale > 1.6) Scale = 1.6;
            if (InnerCount < 1) InnerCount = 1;
            if (InnerCount > 12) InnerCount = 12;
            if (OuterCount < 0) OuterCount = 0;
            if (OuterCount > 16) OuterCount = 16;
            if (MouseMode < 0) MouseMode = 0;
            if (MouseMode > 2) MouseMode = 2;
            if (MouseSensitivity < 0.2) MouseSensitivity = 0.2;
            if (MouseSensitivity > 4.0) MouseSensitivity = 4.0;
            while (Inner.Count < 12) Inner.Add(new SlotConfig());
            while (Outer.Count < 16) Outer.Add(new SlotConfig());
            if (Accent == null) Accent = "#5B8CFF";
        }

        public Color AccentColor
        {
            get { return ColorUtil.Parse(Accent, Color.FromArgb(91, 140, 255)); }
        }

        public static AppConfig Load(string path)
        {
            AppConfig cfg = new AppConfig();
            try
            {
                if (!File.Exists(path)) return cfg;
                Dictionary<string, object> root = Json.Obj(Json.Parse(File.ReadAllText(path, Encoding.UTF8)));
                cfg.Hotkey = HotkeySpec.FromJson(Json.Obj(root.ContainsKey("hotkey") ? root["hotkey"] : null));
                cfg.ToggleMode = Json.Bool(root, "toggleMode", false);
                cfg.PlayToMic = Json.Bool(root, "playToMic", true);
                cfg.PlayToSpeaker = Json.Bool(root, "playToSpeaker", true);
                cfg.ShowToast = Json.Bool(root, "showToast", false);
                cfg.AutoStart = Json.Bool(root, "autoStart", false);
                cfg.Animations = Json.Bool(root, "animations", true);
                cfg.Scale = Json.Dbl(root, "scale", 1.0);
                cfg.Accent = Json.Str(root, "accent", "#5B8CFF");
                cfg.InnerCount = Json.Int(root, "innerCount", 8);
                cfg.OuterCount = Json.Int(root, "outerCount", 12);
                cfg.MouseMode = Json.Int(root, "mouseMode", 0);
                cfg.MouseSensitivity = Json.Dbl(root, "mouseSensitivity", 1.0);
                cfg.CaptureMouse = Json.Bool(root, "captureMouse", true);

                ReadSlots(cfg.Inner, Json.GetArr(root, "inner"));
                ReadSlots(cfg.Outer, Json.GetArr(root, "outer"));
            }
            catch (Exception ex)
            {
                Log.Error("读取配置失败", ex);
            }
            cfg.Normalize();
            return cfg;
        }

        private static void ReadSlots(List<SlotConfig> target, Array items)
        {
            int i = 0;
            foreach (object o in items)
            {
                if (i >= target.Count) break;
                Dictionary<string, object> d = Json.Obj(o);
                target[i].Index = Json.Int(d, "index", 0);
                target[i].Title = Json.Str(d, "title", "");
                i++;
            }
        }

        public void Save(string path)
        {
            Normalize();
            Dictionary<string, object> root = new Dictionary<string, object>();
            root["hotkey"] = Hotkey == null ? new Dictionary<string, object>() : Hotkey.ToJson();
            root["toggleMode"] = ToggleMode;
            root["playToMic"] = PlayToMic;
            root["playToSpeaker"] = PlayToSpeaker;
            root["showToast"] = ShowToast;
            root["autoStart"] = AutoStart;
            root["animations"] = Animations;
            root["scale"] = Scale;
            root["accent"] = Accent;
            root["innerCount"] = InnerCount;
            root["outerCount"] = OuterCount;
            root["mouseMode"] = MouseMode;
            root["mouseSensitivity"] = Math.Round(MouseSensitivity, 2);
            root["captureMouse"] = CaptureMouse;
            root["inner"] = SlotsToJson(Inner);
            root["outer"] = SlotsToJson(Outer);

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string temp = path + ".tmp";
            File.WriteAllText(temp, Json.Stringify(root), new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        private static List<object> SlotsToJson(List<SlotConfig> slots)
        {
            List<object> list = new List<object>();
            foreach (SlotConfig s in slots)
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["index"] = s.Index;
                d["title"] = s.Title == null ? "" : s.Title;
                list.Add(d);
            }
            return list;
        }
    }

    internal static class ColorUtil
    {
        public static Color Parse(string text, Color fallback)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    string t = text.Trim();
                    if (t.StartsWith("#")) t = t.Substring(1);
                    if (t.Length == 6)
                        return Color.FromArgb(255,
                            Convert.ToInt32(t.Substring(0, 2), 16),
                            Convert.ToInt32(t.Substring(2, 2), 16),
                            Convert.ToInt32(t.Substring(4, 2), 16));
                }
            }
            catch (Exception) { }
            return fallback;
        }

        public static string ToHex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        public static Color Mix(Color a, Color b, double amount)
        {
            if (amount < 0) amount = 0;
            if (amount > 1) amount = 1;
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * amount),
                (int)Math.Round(a.R + (b.R - a.R) * amount),
                (int)Math.Round(a.G + (b.G - a.G) * amount),
                (int)Math.Round(a.B + (b.B - a.B) * amount));
        }

        public static Color WithAlpha(Color c, int alpha)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            return Color.FromArgb(alpha, c.R, c.G, c.B);
        }

        public static Color FromHsl(double h, double s, double l)
        {
            h = h % 360; if (h < 0) h += 360;
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = l - c / 2;
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return Color.FromArgb(255,
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255));
        }
    }
}
