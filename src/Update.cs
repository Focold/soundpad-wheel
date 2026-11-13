// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace SoundWheel
{
    internal sealed class UpdateInfo
    {
        public Version Version;
        public string Tag = "";
        public string SetupUrl = "";
        public string PageUrl = "";
        public string Notes = "";
    }

    // 从 GitHub Releases 读取最新版本，并下载安装包
    internal static class UpdateService
    {
        public const string Repo = "Focold/soundpad-wheel";
        public const string ReleasesPage = "https://github.com/Focold/soundpad-wheel/releases";

        public static Version CurrentVersion
        {
            get
            {
                try { return typeof(UpdateService).Assembly.GetName().Version; }
                catch (Exception) { return new Version(1, 0, 0, 0); }
            }
        }

        public static UpdateInfo Latest(int timeoutMs)
        {
            try
            {
                string url = "https://api.github.com/repos/" + Repo + "/releases/latest";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = "SoundWheel/" + CurrentVersion.ToString();
                req.Accept = "application/vnd.github+json";
                req.Timeout = timeoutMs;
                req.ReadWriteTimeout = timeoutMs;
                req.KeepAlive = false;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream stream = resp.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string text = reader.ReadToEnd();
                    return Parse(text);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static UpdateInfo Parse(string text)
        {
            object root = Json.Parse(text);
            if (!(root is Dictionary<string, object>)) return null;
            Dictionary<string, object> obj = (Dictionary<string, object>)root;
            string tag = Json.Str(obj, "tag_name", "");
            if (tag.Length == 0) return null;
            string bare = tag.Trim();
            if (bare.StartsWith("v") || bare.StartsWith("V")) bare = bare.Substring(1);
            Version ver;
            if (!Version.TryParse(bare, out ver)) return null;

            UpdateInfo info = new UpdateInfo();
            info.Version = ver;
            info.Tag = tag;
            info.PageUrl = Json.Str(obj, "html_url", ReleasesPage);
            info.Notes = Json.Str(obj, "body", "");

            object assetsObj;
            if (obj.TryGetValue("assets", out assetsObj))
            {
                object[] assets = assetsObj as object[];
                if (assets != null)
                {
                    string fallback = "";
                    for (int i = 0; i < assets.Length; i++)
                    {
                        Dictionary<string, object> a = assets[i] as Dictionary<string, object>;
                        if (a == null) continue;
                        string name = Json.Str(a, "name", "");
                        string url = Json.Str(a, "browser_download_url", "");
                        if (url.Length == 0) continue;
                        if (string.Equals(name, "SoundWheelSetup.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            info.SetupUrl = url;
                            break;
                        }
                        if (fallback.Length == 0 && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            fallback = url;
                    }
                    if (info.SetupUrl.Length == 0) info.SetupUrl = fallback;
                }
            }
            if (info.SetupUrl.Length == 0) info.SetupUrl = info.PageUrl;
            return info;
        }

        public static bool Download(string url, string path, int timeoutMs, out string error)
        {
            error = "";
            return DownloadCore(url, path, timeoutMs, out error);
        }

        private static bool DownloadCore(string url, string path, int timeoutMs, out string error)
        {
            error = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = "SoundWheel/" + CurrentVersion.ToString();
                req.Timeout = timeoutMs;
                req.ReadWriteTimeout = timeoutMs;
                req.KeepAlive = false;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream input = resp.GetResponseStream())
                using (FileStream output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[65536];
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        output.Write(buffer, 0, read);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                try { if (File.Exists(path)) File.Delete(path); } catch (Exception) { }
                return false;
            }
        }
    }
}
