// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SoundWheel
{
    internal sealed class SoundEntry
    {
        public int Index;
        public string Title = "";
        public string Artist = "";
        public string Url = "";
        public string Duration = "";

        public string Display
        {
            get
            {
                string t = string.IsNullOrEmpty(Title) ? Path.GetFileNameWithoutExtension(Url) : Title;
                if (!string.IsNullOrEmpty(Duration)) return Index + "  ·  " + t + "  (" + Duration + ")";
                return Index + "  ·  " + t;
            }
        }

        public override string ToString()
        {
            return Display;
        }
    }

    internal sealed class SoundpadClient : IDisposable
    {
        private const string PipeName = "sp_remote_control";

        private readonly object _sync = new object();
        private NamedPipeClientStream _pipe;
        private bool _messageMode;
        private DateTime _lastConnectFail = DateTime.MinValue;

        public SoundEntry[] Sounds = new SoundEntry[0];
        public string LastError = "";

        public bool IsConnected
        {
            get
            {
                lock (_sync)
                {
                    return _pipe != null && _pipe.IsConnected;
                }
            }
        }

        public void Dispose()
        {
            lock (_sync) { ClosePipe(); }
        }

        private void ClosePipe()
        {
            try { if (_pipe != null) _pipe.Dispose(); }
            catch (Exception) { }
            _pipe = null;
        }

        private bool EnsureConnected(int timeoutMs)
        {
            if (_pipe != null && _pipe.IsConnected) return true;
            ClosePipe();
            if ((DateTime.UtcNow - _lastConnectFail).TotalMilliseconds < 700) return false;

            try
            {
                NamedPipeClientStream p = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.None);
                p.Connect(timeoutMs);
                bool message = true;
                try { p.ReadMode = PipeTransmissionMode.Message; }
                catch (Exception) { message = false; }
                _messageMode = message;
                _pipe = p;
                return true;
            }
            catch (Exception ex)
            {
                _lastConnectFail = DateTime.UtcNow;
                LastError = ex is TimeoutException ? "Soundpad 未运行或远程控制未开启" : ex.Message;
                ClosePipe();
                return false;
            }
        }

        private string ReadResponse()
        {
            byte[] buffer = new byte[65536];
            MemoryStream ms = new MemoryStream();
            if (_messageMode)
            {
                while (true)
                {
                    int n = _pipe.Read(buffer, 0, buffer.Length);
                    if (n <= 0) break;
                    ms.Write(buffer, 0, n);
                    if (_pipe.IsMessageComplete) break;
                }
            }
            else
            {
                DateTime lastData = DateTime.UtcNow;
                while ((DateTime.UtcNow - lastData).TotalMilliseconds < 2500)
                {
                    uint available;
                    IntPtr handle = _pipe.SafePipeHandle.DangerousGetHandle();
                    if (!Native.PeekNamedPipe(handle, IntPtr.Zero, 0, IntPtr.Zero, out available, IntPtr.Zero)) break;
                    if (available == 0)
                    {
                        if (ms.Length > 0) Thread.Sleep(8);
                        else Thread.Sleep(2);
                        continue;
                    }
                    int want = (int)Math.Min((long)buffer.Length, (long)available);
                    int n = _pipe.Read(buffer, 0, want);
                    if (n <= 0) break;
                    ms.Write(buffer, 0, n);
                    lastData = DateTime.UtcNow;
                }
            }
            return Encoding.UTF8.GetString(ms.ToArray()).TrimEnd('\0').Trim();
        }

        private string Send(string command, int timeoutMs)
        {
            lock (_sync)
            {
                if (!EnsureConnected(Math.Min(timeoutMs, 500))) return null;
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(command);
                    _pipe.Write(data, 0, data.Length);
                    _pipe.Flush();

                    Task<string> task = Task.Factory.StartNew(delegate { return ReadResponse(); });
                    if (!task.Wait(timeoutMs))
                    {
                        LastError = "Soundpad 响应超时";
                        ClosePipe();
                        return null;
                    }
                    return task.Result;
                }
                catch (AggregateException ex)
                {
                    ClosePipe();
                    LastError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    return null;
                }
                catch (Exception ex)
                {
                    ClosePipe();
                    LastError = ex.Message;
                    return null;
                }
            }
        }

        public SoundEntry[] FetchSounds(int timeoutMs)
        {
            string xml = Send("GetSoundlist()", timeoutMs);
            if (string.IsNullOrEmpty(xml)) return null;
            if (xml[0] != '<')
            {
                LastError = "Soundpad 返回: " + Clip(xml, 120);
                return null;
            }
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                List<SoundEntry> list = new List<SoundEntry>();
                XmlNodeList nodes = doc.SelectNodes("/Soundlist/Sound");
                if (nodes != null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        if (node.Attributes == null) continue;
                        SoundEntry e = new SoundEntry();
                        int idx;
                        if (!int.TryParse(Attr(node, "index"), out idx)) continue;
                        e.Index = idx;
                        e.Title = Attr(node, "title");
                        e.Artist = Attr(node, "artist");
                        e.Url = Attr(node, "url");
                        e.Duration = Attr(node, "duration");
                        if (string.IsNullOrEmpty(e.Title)) e.Title = Path.GetFileNameWithoutExtension(e.Url);
                        list.Add(e);
                    }
                }
                list.Sort(delegate(SoundEntry a, SoundEntry b) { return a.Index.CompareTo(b.Index); });
                Sounds = list.ToArray();
                return Sounds;
            }
            catch (Exception ex)
            {
                LastError = "解析音效列表失败: " + ex.Message;
                return null;
            }
        }

        public bool Play(int index, bool render, bool capture, out string error)
        {
            error = null;
            string command = "DoPlaySound(" + index.ToString() + ", " + (render ? "true" : "false") + ", " + (capture ? "true" : "false") + ")";
            string response = Send(command, 1200);
            if (string.IsNullOrEmpty(response))
            {
                error = string.IsNullOrEmpty(LastError) ? "无法连接 Soundpad" : LastError;
                return false;
            }
            if (!response.StartsWith("R-200"))
            {
                error = "Soundpad 拒绝播放: " + Clip(response, 120);
                return false;
            }
            return true;
        }

        public bool Stop(out string error)
        {
            error = null;
            string response = Send("DoStopSound()", 1000);
            if (string.IsNullOrEmpty(response) || !response.StartsWith("R-200"))
            {
                error = string.IsNullOrEmpty(LastError) ? "无法连接 Soundpad" : LastError;
                return false;
            }
            return true;
        }

        private static string Attr(XmlNode node, string name)
        {
            if (node.Attributes == null) return "";
            XmlAttribute a = node.Attributes[name];
            return a == null ? "" : a.Value;
        }

        private static string Clip(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ");
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }

    internal static class SoundCache
    {
        public static SoundEntry[] Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return new SoundEntry[0];
                Dictionary<string, object> root = Json.Obj(Json.Parse(File.ReadAllText(path, Encoding.UTF8)));
                List<SoundEntry> list = new List<SoundEntry>();
                foreach (object o in Json.Arr(root.ContainsKey("sounds") ? root["sounds"] : null))
                {
                    Dictionary<string, object> d = Json.Obj(o);
                    SoundEntry e = new SoundEntry();
                    e.Index = Json.Int(d, "index", 0);
                    e.Title = Json.Str(d, "title", "");
                    e.Artist = Json.Str(d, "artist", "");
                    e.Url = Json.Str(d, "url", "");
                    e.Duration = Json.Str(d, "duration", "");
                    if (e.Index > 0) list.Add(e);
                }
                return list.ToArray();
            }
            catch (Exception ex)
            {
                Log.Error("读取音效缓存失败", ex);
                return new SoundEntry[0];
            }
        }

        public static void Save(string path, SoundEntry[] sounds)
        {
            try
            {
                List<object> list = new List<object>();
                if (sounds != null)
                {
                    foreach (SoundEntry e in sounds)
                    {
                        Dictionary<string, object> d = new Dictionary<string, object>();
                        d["index"] = e.Index;
                        d["title"] = e.Title;
                        d["artist"] = e.Artist;
                        d["url"] = e.Url;
                        d["duration"] = e.Duration;
                        list.Add(d);
                    }
                }
                Dictionary<string, object> root = new Dictionary<string, object>();
                root["sounds"] = list;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, Json.Stringify(root), new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Log.Error("写入音效缓存失败", ex);
            }
        }
    }
}
