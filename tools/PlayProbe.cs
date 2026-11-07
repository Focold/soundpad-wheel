// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using SoundWheel;

internal static class PlayProbe
{
    private static int Main(string[] args)
    {
        int index = args.Length > 0 ? int.Parse(args[0]) : 8;
        using (SoundpadClient client = new SoundpadClient())
        {
            SoundEntry[] sounds = client.FetchSounds(4000);
            Console.WriteLine("sounds=" + (sounds == null ? 0 : sounds.Length) + " err=" + client.LastError);
            if (sounds != null)
            {
                for (int i = 0; i < sounds.Length && i < 3; i++) Console.WriteLine("  " + sounds[i].Display);
            }
            string error;
            bool ok = client.Play(index, true, true, out error);
            Console.WriteLine("play(" + index + ")=" + ok + " err=" + error);
        }
        return 0;
    }
}
