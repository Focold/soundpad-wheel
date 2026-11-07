// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static class KeyWatch
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vk);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int vk);

    private static string _log;

    private static void Main(string[] args)
    {
        int vk = 0x57;
        int ms = 3000;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--vk") vk = Convert.ToInt32(args[++i], 16);
            else if (args[i] == "--ms") ms = int.Parse(args[++i]);
            else if (args[i] == "--log") _log = args[++i];
        }
        Write("watch start vk=" + vk + " pid=" + System.Diagnostics.Process.GetCurrentProcess().Id);
        short last = -999;
        DateTime end = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < end)
        {
            short cur = GetAsyncKeyState(vk);
            if (cur != last)
            {
                Write(string.Format("async={0} sync={1}", cur, GetKeyState(vk)));
                last = cur;
            }
            Thread.Sleep(5);
        }
        Write("watch end");
    }

    private static void Write(string message)
    {
        string line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine;
        if (_log != null) File.AppendAllText(_log, line, Encoding.UTF8);
        else Console.Write(line);
    }
}
