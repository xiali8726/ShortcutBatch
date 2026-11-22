using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ShortcutBatch
{
    public enum ShortcutOutcome
    {
        Created,      // 新建
        Overwritten,  // 覆盖（同名 .lnk 指向同一源文件）
        Renamed,      // 改名保留两者
        Failed        // 失败
    }

    public sealed class ShortcutResult
    {
        public string SourceFile;   // 源文件完整路径
        public string LinkFile;     // 生成的 .lnk 完整路径
        public ShortcutOutcome Outcome;
        public string Message;      // 失败原因等
    }

    public static class ShortcutCreator
    {
        /// <summary>把源（文件/文件夹）展开为文件列表，去重；按需递归、按通配符过滤。</summary>
        public static void CollectFiles(IEnumerable<string> sources, bool recurse, string filter,
            List<string> files, List<string> warnings)
        {
            var patterns = ParsePatterns(filter);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in sources)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (Directory.Exists(s))
                    Walk(s, recurse, patterns, files, warnings, seen);
                else if (File.Exists(s))
                    AddIfMatch(s, patterns, files, seen);
                else
                    warnings.Add("路径不存在，已跳过: " + s);
            }
        }

        private static void Walk(string dir, bool recurse, string[] patterns,
            List<string> files, List<string> warnings, HashSet<string> seen)
        {
            try
            {
                foreach (var f in Directory.EnumerateFiles(dir))
                    AddIfMatch(f, patterns, files, seen);
            }
            catch (Exception ex)
            {
                warnings.Add("无法读取文件夹 " + dir + " : " + ex.Message);
                return;
            }

            if (!recurse) return;
            foreach (var sub in Directory.EnumerateDirectories(dir))
                Walk(sub, true, patterns, files, warnings, seen);
        }

        private static void AddIfMatch(string path, string[] patterns, List<string> files, HashSet<string> seen)
        {
            if (patterns != null && !MatchesAny(Path.GetFileName(path), patterns)) return;
            if (seen.Add(path)) files.Add(path);
        }

        private static string[] ParsePatterns(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return null;
            var list = new List<string>();
            foreach (var part in filter.Split(';'))
            {
                var p = part.Trim();
                if (p.Length > 0) list.Add(p);
            }
            return list.Count == 0 ? null : list.ToArray();
        }

        private static bool MatchesAny(string name, string[] patterns)
        {
            foreach (var p in patterns)
                if (WildcardMatch(p, name)) return true;
            return false;
        }

        private static bool WildcardMatch(string pattern, string text)
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(text, regex, RegexOptions.IgnoreCase);
        }

        /// <summary>为单个源文件在目标文件夹中生成快捷方式，处理同名冲突。</summary>
        public static ShortcutResult CreateFor(string sourceFile, string targetDir)
        {
            string baseName = Path.GetFileName(sourceFile);
            string link = Path.Combine(targetDir, baseName + ".lnk");
            var result = new ShortcutResult { SourceFile = sourceFile };

            if (!File.Exists(link))
            {
                result.LinkFile = link;
                result.Outcome = ShortcutOutcome.Created;
            }
            else
            {
                string existing = ReadTargetPath(link);
                if (string.Equals(existing, sourceFile, StringComparison.OrdinalIgnoreCase))
                {
                    result.LinkFile = link;
                    result.Outcome = ShortcutOutcome.Overwritten;
                }
                else
                {
                    result.LinkFile = NextAvailablePath(targetDir, baseName, sourceFile);
                    result.Outcome = ShortcutOutcome.Renamed;
                }
            }

            try
            {
                WriteShortcut(sourceFile, result.LinkFile);
            }
            catch (Exception ex)
            {
                result.Outcome = ShortcutOutcome.Failed;
                result.Message = ex.Message;
            }
            return result;
        }

        /// <summary>找 "名称 (2).lnk"、"名称 (3).lnk"…；若候选已指向同一源文件则复用。</summary>
        private static string NextAvailablePath(string targetDir, string baseName, string sourceFile)
        {
            for (int i = 2; ; i++)
            {
                string cand = Path.Combine(targetDir, baseName + " (" + i + ").lnk");
                if (!File.Exists(cand)) return cand;
                if (string.Equals(ReadTargetPath(cand), sourceFile, StringComparison.OrdinalIgnoreCase)) return cand;
            }
        }

        private static object NewShell()
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            if (t == null) throw new InvalidOperationException("无法创建 WScript.Shell COM 对象。");
            return Activator.CreateInstance(t);
        }

        private static void WriteShortcut(string targetPath, string linkPath)
        {
            object shellObj = NewShell();
            dynamic shell = shellObj;
            object scObj = null;
            try
            {
                scObj = shell.CreateShortcut(linkPath);
                dynamic sc = scObj;
                sc.TargetPath = targetPath;
                sc.WorkingDirectory = Path.GetDirectoryName(targetPath);
                sc.Save();
            }
            finally
            {
                if (scObj != null) Marshal.ReleaseComObject(scObj);
                Marshal.ReleaseComObject(shellObj);
            }
        }

        private static string ReadTargetPath(string linkPath)
        {
            object shellObj = NewShell();
            dynamic shell = shellObj;
            object scObj = null;
            try
            {
                scObj = shell.CreateShortcut(linkPath);
                dynamic sc = scObj;
                object v = sc.TargetPath;
                return v == null ? null : v.ToString();
            }
            catch
            {
                return null;
            }
            finally
            {
                if (scObj != null) Marshal.ReleaseComObject(scObj);
                Marshal.ReleaseComObject(shellObj);
            }
        }
    }
}
