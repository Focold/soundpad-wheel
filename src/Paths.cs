// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.IO;

namespace SoundWheel
{
    internal static class AppPaths
    {
        public static string Override;
        public static string Note = "";

        private static string _resolved;

        public static string DataDir
        {
            get
            {
                if (!string.IsNullOrEmpty(Override))
                {
                    TryCreate(Override);
                    return Override;
                }
                if (_resolved != null) return _resolved;

                string custom = Environment.GetEnvironmentVariable("SOUNDWHEEL_DATA");
                if (!string.IsNullOrEmpty(custom) && TryCreate(custom))
                {
                    _resolved = custom;
                    return _resolved;
                }

                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SoundWheel");
                if (TryCreate(appData)) { _resolved = appData; return _resolved; }

                string local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoundWheel");
                if (TryCreate(local))
                {
                    _resolved = local;
                    Note = "无法写入漫游配置目录，已改用本地目录";
                    return _resolved;
                }

                string portable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SoundWheelData");
                if (TryCreate(portable))
                {
                    _resolved = portable;
                    Note = "无法写入用户配置目录，已改用程序目录（绿色模式）";
                    return _resolved;
                }

                string temp = Path.Combine(Path.GetTempPath(), "SoundWheel");
                TryCreate(temp);
                _resolved = temp;
                Note = "无法写入用户配置目录，已改用临时目录";
                return _resolved;
            }
        }

        public static string ConfigFile { get { return Path.Combine(DataDir, "config.json"); } }
        public static string SoundsFile { get { return Path.Combine(DataDir, "sounds.json"); } }
        public static string LogFile { get { return Path.Combine(DataDir, "soundwheel.log"); } }

        private static bool TryCreate(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
