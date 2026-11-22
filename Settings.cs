using System;
using System.IO;
using System.Text;

namespace ShortcutBatch
{
    /// <summary>
    /// 用户设置的持久化：写入 exe 同目录的 ShortcutBatch.ini（纯文本，UTF-8）。
    /// 读写失败一律静默忽略，不影响主流程。
    /// </summary>
    public static class Settings
    {
        public const string ModeSource = "source"; // 源文件所在文件夹
        public const string ModeFixed  = "fixed";  // 固定文件夹
        public const string ModeLast   = "last";   // 上次的目标文件夹

        public static string DefaultTargetMode = ModeSource;
        public static string FixedTargetDir = "";
        public static string LastTargetDir = "";
        public static string LastFileDir = "";
        public static string LastFolderDir = "";

        private static string IniPath
        {
            get
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(dir, "ShortcutBatch.ini");
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(IniPath)) return;
                foreach (var raw in File.ReadAllLines(IniPath, new UTF8Encoding(false)))
                {
                    string s = raw.Trim();
                    if (s.Length == 0 || s[0] == '#' || s[0] == ';') continue;
                    int eq = s.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = s.Substring(0, eq).Trim();
                    string val = s.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "DefaultTargetMode": DefaultTargetMode = val; break;
                        case "FixedTargetDir": FixedTargetDir = val; break;
                        case "LastTargetDir": LastTargetDir = val; break;
                        case "LastFileDir": LastFileDir = val; break;
                        case "LastFolderDir": LastFolderDir = val; break;
                    }
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("DefaultTargetMode=" + DefaultTargetMode);
                sb.AppendLine("FixedTargetDir=" + FixedTargetDir);
                sb.AppendLine("LastTargetDir=" + LastTargetDir);
                sb.AppendLine("LastFileDir=" + LastFileDir);
                sb.AppendLine("LastFolderDir=" + LastFolderDir);
                File.WriteAllText(IniPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        /// <summary>把 ~ 展开为用户主目录（~ 或 ~\xxx、~/xxx）。</summary>
        public static string ExpandTilde(string p)
        {
            if (string.IsNullOrEmpty(p)) return p;
            if (p == "~" || p.StartsWith("~\\", StringComparison.Ordinal) || p.StartsWith("~/", StringComparison.Ordinal))
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    p.Substring(1).TrimStart('\\', '/'));
            return p;
        }
    }
}
