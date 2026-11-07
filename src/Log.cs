// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.IO;
using System.Text;

namespace SoundWheel
{
    internal static class Log
    {
        private static readonly object Sync = new object();
        public static string FilePath = "";

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message, Exception ex)
        {
            string suffix = ex == null ? "" : " | " + ex.GetType().Name + ": " + ex.Message;
            Write("ERROR", message + suffix);
        }

        private static void Write(string level, string message)
        {
            if (string.IsNullOrEmpty(FilePath)) return;
            try
            {
                lock (Sync)
                {
                    File.AppendAllText(FilePath,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + level + "] " + message + Environment.NewLine,
                        Encoding.UTF8);
                }
            }
            catch (Exception) { }
        }
    }
}
