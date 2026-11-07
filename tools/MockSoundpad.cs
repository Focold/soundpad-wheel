// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace MockSoundpad
{
    internal static class Program
    {
        private static string _logPath = Path.Combine(Path.GetTempPath(), "soundwheel-mock.log");
        private static readonly object Sync = new object();

        private static readonly string[] Titles =
        {
            "开场音效", "笑死我了", "鼓掌", "尴尬", "胜利", "失败",
            "汪汪", "喵喵", "警报", "倒计时", "掌声", "嘘声",
            "叮", "爆炸", "鼓点", "雨声", "枪声", "脚步", "门铃", "欢呼"
        };

        private static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "--log" && i + 1 < args.Length) _logPath = args[i + 1];
            Log("mock start pid=" + System.Diagnostics.Process.GetCurrentProcess().Id);

            while (true)
            {
                try
                {
                    using (NamedPipeServerStream server = new NamedPipeServerStream("sp_remote_control",
                        PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None))
                    {
                        server.WaitForConnection();
                        server.ReadMode = PipeTransmissionMode.Message;
                        Log("connected");
                        Serve(server);
                    }
                }
                catch (Exception ex)
                {
                    Log("server error: " + ex.Message);
                    Thread.Sleep(400);
                }
            }
        }

        private static void Serve(NamedPipeServerStream server)
        {
            byte[] buffer = new byte[65536];
            while (true)
            {
                int n = server.Read(buffer, 0, buffer.Length);
                if (n <= 0) { Log("disconnected"); return; }
                string command = Encoding.UTF8.GetString(buffer, 0, n).Trim();
                Log("cmd " + command);
                string response = Handle(command);
                byte[] data = Encoding.UTF8.GetBytes(response);
                server.Write(data, 0, data.Length);
                server.Flush();
            }
        }

        private static string Handle(string command)
        {
            if (command.StartsWith("GetSoundlist")) return SoundList();
            if (command.StartsWith("DoPlaySound"))
            {
                int open = command.IndexOf('(');
                int comma = command.IndexOf(',', open + 1);
                int close = command.IndexOf(')', open + 1);
                int end = comma >= 0 ? comma : close;
                string index = command.Substring(open + 1, end - open - 1).Trim();
                Log("PLAY " + index);
                return "R-200";
            }
            if (command.StartsWith("DoStopSound")) { Log("STOP"); return "R-200"; }
            if (command.StartsWith("GetVersion")) return "R-200 Soundpad 4.0 (mock)";
            return "R-400";
        }

        private static string SoundList()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<Soundlist rel=\"true\">");
            for (int i = 0; i < Titles.Length; i++)
            {
                sb.Append("<Sound index=\"").Append(i + 1).Append("\"");
                sb.Append(" url=\"C:\\Sounds\\mock_").Append(i + 1).Append(".wav\"");
                sb.Append(" artist=\"Mock\"");
                sb.Append(" title=\"").Append(Titles[i]).Append("\"");
                sb.Append(" color=\"#FF5B8CFF\"");
                sb.Append(" tag=\"\"");
                sb.Append(" duration=\"0:0").Append(i % 9 + 1).Append("\"");
                sb.Append(" addedOn=\"1700000000\"");
                sb.Append(" lastPlayedOn=\"0\"");
                sb.Append(" playCount=\"0\"");
                sb.Append("/>");
            }
            sb.Append("</Soundlist>");
            return sb.ToString();
        }

        private static void Log(string message)
        {
            try
            {
                lock (Sync)
                {
                    File.AppendAllText(_logPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception) { }
        }
    }
}
