// 本项目由 AI 编码代理生成（DeepSeek Harness / deepseek-flash），说明见仓库根目录 AI-DISCLOSURE.md。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SoundWheelSetup
{
    internal static class L
    {
        public static bool English;

        public static string T(string zh, string en)
        {
            return English ? en : zh;
        }
    }

    internal static class Program
    {
        public const string AppName = "SoundWheel";
        public const string Repo = "Focold/soundpad-wheel";
        public const string PayloadResource = "payload.bin";

        public static string DefaultDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoundWheel"); }
        }

        public static string LogPath
        {
            get { return Path.Combine(Path.GetTempPath(), "SoundWheelSetup.log"); }
        }

        public static void Log(string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine;
            string[] targets = new string[]
            {
                Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "SoundWheelSetup.log"),
                Path.Combine(Path.GetTempPath(), "SoundWheelSetup.log"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoundWheelSetup.log")
            };
            for (int i = 0; i < targets.Length; i++)
            {
                try { File.AppendAllText(targets[i], line, Encoding.UTF8); }
                catch (Exception) { }
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            bool silent = false;
            bool uninstall = false;
            string dir = DefaultDir;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                if (a == "/s" || a == "--silent" || a == "/silent") silent = true;
                else if (a == "--uninstall" || a == "/uninstall" || a == "-u") uninstall = true;
                else if ((a == "--dir" || a == "/dir") && i + 1 < args.Length) { dir = args[++i]; }
            }

            if (Environment.GetEnvironmentVariable("SOUNDWHEEL_SETUP_EN") == "1") L.English = true;
            Log("start silent=" + silent + " uninstall=" + uninstall + " dir=" + dir + " args=" + string.Join(" ", args));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException); }
            catch (Exception) { }

            if (uninstall)
            {
                if (silent) { bool ok = Installer.DoUninstall(dir, false); Log("uninstall silent ok=" + ok); if (!ok) Environment.ExitCode = 1; return; }
                Application.Run(new InstallerForm(dir, true));
                return;
            }

            if (silent)
            {
                string error;
                if (!Installer.DoInstall(dir, true, true, true, out error))
                {
                    Log("install failed: " + error);
                    Environment.ExitCode = 1;
                }
                else Log("install ok");
                return;
            }

            Application.Run(new InstallerForm(dir, false));
        }
    }

    internal sealed class ReleaseInfo
    {
        public Version Version;
        public string Tag = "";
        public string PageUrl = "";
        public string SetupUrl = "";
    }

    internal static class Installer
    {
        public static string TargetExe(string dir) { return Path.Combine(dir, "SoundWheel.exe"); }
        public static string TargetSetup(string dir) { return Path.Combine(dir, "SoundWheelSetup.exe"); }

        public static Version InstalledVersion(string dir)
        {
            try
            {
                string exe = TargetExe(dir);
                if (!File.Exists(exe)) return null;
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(exe);
                if (string.IsNullOrEmpty(info.FileVersion)) return null;
                return new Version(info.FileVersion);
            }
            catch (Exception) { return null; }
        }

        public static Version SetupVersion()
        {
            try { return Assembly.GetExecutingAssembly().GetName().Version; }
            catch (Exception) { return new Version(2, 0, 0, 0); }
        }

        public static byte[] ReadPayload()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(Program.PayloadResource))
            {
                if (s == null) throw new InvalidOperationException("安装包损坏：找不到内嵌程序");
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buffer = new byte[65536];
                    while (true)
                    {
                        int n = s.Read(buffer, 0, buffer.Length);
                        if (n <= 0) break;
                        ms.Write(buffer, 0, n);
                    }
                    return ms.ToArray();
                }
            }
        }

        public static void KillRunning(string name)
        {
            try
            {
                Process[] list = Process.GetProcessesByName(name);
                for (int i = 0; i < list.Length; i++)
                {
                    try
                    {
                        if (list[i].Id == Process.GetCurrentProcess().Id) continue;
                        if (list[i].WorkingSet64 < 5L * 1024 * 1024) continue;
                        list[i].Kill();
                        list[i].WaitForExit(3000);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        public static bool DoInstall(string dir, bool desktopShortcut, bool launch, bool silent, out string error)
        {
            error = null;
            try
            {
                Program.Log("DoInstall dir=" + dir);
                Directory.CreateDirectory(dir);
                Program.Log("dir ok");
                KillRunning(Program.AppName);
                byte[] payload = ReadPayload();
                Program.Log("payload bytes=" + payload.Length);
                string exe = TargetExe(dir);
                bool written = false;
                for (int attempt = 0; attempt < 12 && !written; attempt++)
                {
                    try
                    {
                        File.WriteAllBytes(exe, payload);
                        written = true;
                    }
                    catch (IOException)
                    {
                        KillRunning(Program.AppName);
                        Thread.Sleep(250);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        KillRunning(Program.AppName);
                        Thread.Sleep(250);
                    }
                }
                if (!written) throw new IOException("无法写入 " + exe);
                Program.Log("payload written " + exe);

                string self = TargetSetup(dir);
                try
                {
                    if (!string.Equals(Application.ExecutablePath, self, StringComparison.OrdinalIgnoreCase))
                        File.Copy(Application.ExecutablePath, self, true);
                }
                catch (Exception) { }

                try { RegisterUninstall(dir, exe); Program.Log("registry ok"); }
                catch (Exception ex) { Program.Log("registry failed: " + ex.Message); }
                try { CreateShortcuts(dir, exe, desktopShortcut); Program.Log("shortcuts ok"); }
                catch (Exception ex) { Program.Log("shortcuts failed: " + ex.Message); }

                if (launch)
                {
                    try { Process.Start(exe); }
                    catch (Exception) { }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Program.Log("DoInstall exception: " + ex);
                return false;
            }
        }

        public static bool DoUninstall(string dir, bool removeData)
        {
            try
            {
                KillRunning(Program.AppName);
                Thread.Sleep(200);
                try { UnregisterUninstall(); }
                catch (Exception) { }
                RemoveShortcuts();
                try
                {
                    string exe = TargetExe(dir);
                    if (File.Exists(exe)) File.Delete(exe);
                }
                catch (Exception) { }
                if (removeData) RemoveData();
                try
                {
                    string self = TargetSetup(dir);
                    if (File.Exists(self) && !string.Equals(Application.ExecutablePath, self, StringComparison.OrdinalIgnoreCase))
                        File.Delete(self);
                }
                catch (Exception) { }
                try
                {
                    if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0) Directory.Delete(dir);
                }
                catch (Exception) { }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void RemoveData()
        {
            string[] dirs = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SoundWheel"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoundWheelData")
            };
            for (int i = 0; i < dirs.Length; i++)
            {
                try { if (Directory.Exists(dirs[i])) Directory.Delete(dirs[i], true); }
                catch (Exception) { }
            }
        }

        public static void RegisterUninstall(string dir, string exe)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\SoundWheel"))
            {
                if (key == null) return;
                Version v = InstalledVersion(dir);
                key.SetValue("DisplayName", "SoundWheel");
                key.SetValue("DisplayVersion", v == null ? "2.0.0" : v.ToString(3));
                key.SetValue("Publisher", "Focold");
                key.SetValue("InstallLocation", dir);
                key.SetValue("DisplayIcon", exe + ",0");
                key.SetValue("UninstallString", "\"" + TargetSetup(dir) + "\" --uninstall");
                key.SetValue("QuietUninstallString", "\"" + TargetSetup(dir) + "\" --uninstall --silent");
                key.SetValue("URLInfoAbout", "https://github.com/" + Program.Repo);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        public static void UnregisterUninstall()
        {
            try { Registry.CurrentUser.DeleteSubKeyTree("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\SoundWheel", false); }
            catch (Exception) { }
        }

        public static string StartMenuLink()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "SoundWheel.lnk");
        }

        public static string DesktopLink()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "SoundWheel.lnk");
        }

        public static void CreateShortcuts(string dir, string exe, bool desktop)
        {
            try { ShellLink.Create(StartMenuLink(), exe, dir, "SoundWheel 音效轮盘"); }
            catch (Exception) { }
            if (desktop)
            {
                try { ShellLink.Create(DesktopLink(), exe, dir, "SoundWheel 音效轮盘"); }
                catch (Exception) { }
            }
        }

        public static void RemoveShortcuts()
        {
            try { if (File.Exists(StartMenuLink())) File.Delete(StartMenuLink()); } catch (Exception) { }
            try { if (File.Exists(DesktopLink())) File.Delete(DesktopLink()); } catch (Exception) { }
        }

        public static ReleaseInfo LatestRelease(int timeoutMs)
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/" + Program.Repo + "/releases/latest");
                req.UserAgent = "SoundWheel-Setup";
                req.Accept = "application/vnd.github+json";
                req.Timeout = timeoutMs;
                req.ReadWriteTimeout = timeoutMs;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    ReleaseInfo info = new ReleaseInfo();
                    info.Tag = FindJsonString(json, "tag_name");
                    info.PageUrl = FindJsonString(json, "html_url");
                    string tag = info.Tag;
                    if (tag.StartsWith("v") || tag.StartsWith("V")) tag = tag.Substring(1);
                    try { info.Version = new Version(tag); }
                    catch (Exception) { info.Version = null; }
                    info.SetupUrl = FindAssetUrl(json);
                    return info;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FindJsonString(string json, string key)
        {
            string token = "\"" + key + "\"";
            int i = json.IndexOf(token, StringComparison.Ordinal);
            if (i < 0) return "";
            i = json.IndexOf(':', i + token.Length);
            if (i < 0) return "";
            int q1 = json.IndexOf('"', i + 1);
            if (q1 < 0) return "";
            int q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0) return "";
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }

        private static string FindAssetUrl(string json)
        {
            int i = json.IndexOf("SoundWheelSetup.exe", StringComparison.OrdinalIgnoreCase);
            if (i < 0) return "";
            int start = json.LastIndexOf("browser_download_url", i, StringComparison.Ordinal);
            if (start < 0) return "";
            return FindJsonString(json.Substring(start - 40 < 0 ? start : start - 40), "browser_download_url");
        }

        public static bool Download(string url, string path, int timeoutMs, out string error)
        {
            error = null;
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = "SoundWheel-Setup";
                req.Timeout = timeoutMs;
                req.ReadWriteTimeout = timeoutMs;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream s = resp.GetResponseStream())
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[65536];
                    while (true)
                    {
                        int n = s.Read(buffer, 0, buffer.Length);
                        if (n <= 0) break;
                        fs.Write(buffer, 0, n);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }

    internal static class ShellLink
    {
        public static void Create(string linkPath, string target, string workDir, string description)
        {
            object obj = new ShellLinkClass();
            try
            {
                IShellLinkW link = (IShellLinkW)obj;
                link.SetPath(target);
                link.SetWorkingDirectory(workDir);
                link.SetDescription(description);
                link.SetIconLocation(target, 0);
                IPersistFile file = (IPersistFile)obj;
                file.Save(linkPath, true);
            }
            finally
            {
                try { Marshal.FinalReleaseComObject(obj); }
                catch (Exception) { }
            }
        }

        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLinkClass { }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            [PreserveSig] int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }
    }
}
