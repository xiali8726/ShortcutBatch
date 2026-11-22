using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ShortcutBatch
{
    public static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                // 图形界面模式：关闭随附的控制台窗口，避免双击时闪现黑窗
                FreeConsole();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                return 0;
            }
            return RunCli(args);
        }

        private static int RunCli(string[] args)
        {
            // 统一以 UTF-8 输出，保证中文在重定向/捕获时不乱码
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }

            var sources = new List<string>();
            string target = null;
            bool recurse = false;
            string filter = null;
            bool help = false;

            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i].TrimStart('-', '/');
                switch (key.ToLowerInvariant())
                {
                    case "source":
                    case "target":
                    case "filter":
                        if (i + 1 >= args.Length)
                        {
                            Console.Error.WriteLine("参数 " + args[i] + " 缺少值。");
                            return 2;
                        }
                        string val = args[++i];
                        if (key.Equals("source", StringComparison.OrdinalIgnoreCase)) sources.AddRange(SplitList(val));
                        else if (key.Equals("target", StringComparison.OrdinalIgnoreCase)) target = val;
                        else filter = val;
                        break;
                    case "recurse":
                        recurse = true;
                        break;
                    case "help":
                    case "h":
                    case "?":
                        help = true;
                        break;
                    default:
                        Console.Error.WriteLine("未知参数: " + args[i]);
                        PrintHelp();
                        return 2;
                }
            }

            if (help)
            {
                PrintHelp();
                return 0;
            }

            if (sources.Count == 0 || string.IsNullOrWhiteSpace(target))
            {
                Console.Error.WriteLine("缺少必填参数 -Source 或 -Target。");
                PrintHelp();
                return 2;
            }

            try
            {
                Directory.CreateDirectory(target);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("无法创建目标文件夹: " + ex.Message);
                return 1;
            }

            var files = new List<string>();
            var warnings = new List<string>();
            ShortcutCreator.CollectFiles(sources, recurse, filter, files, warnings);
            foreach (var w in warnings) Console.Error.WriteLine("警告: " + w);
            if (files.Count == 0)
            {
                Console.WriteLine("没有找到任何文件。");
                return 1;
            }

            int created = 0, overwritten = 0, renamed = 0, failed = 0;
            foreach (var f in files)
            {
                var r = ShortcutCreator.CreateFor(f, target);
                string tag;
                switch (r.Outcome)
                {
                    case ShortcutOutcome.Created: tag = "新建"; created++; break;
                    case ShortcutOutcome.Overwritten: tag = "覆盖"; overwritten++; break;
                    case ShortcutOutcome.Renamed: tag = "改名"; renamed++; break;
                    default: tag = "失败"; failed++; break;
                }
                string extra = r.Outcome == ShortcutOutcome.Failed ? " （" + r.Message + "）" : "";
                Console.WriteLine(string.Format("[{0}] {1} -> {2}{3}", tag, r.SourceFile, r.LinkFile, extra));
            }

            Console.WriteLine();
            Console.WriteLine(string.Format("完成：共 {0} 个文件，新建 {1}，覆盖 {2}，改名 {3}，失败 {4}。",
                files.Count, created, overwritten, renamed, failed));
            return failed == 0 ? 0 : 1;
        }

        private static string[] SplitList(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new string[0];
            return s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void PrintHelp()
        {
            Console.WriteLine("ShortcutBatch - 批量为文件创建快捷方式");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  ShortcutBatch.exe -Source <文件或文件夹> -Target <文件夹> [选项]");
            Console.WriteLine();
            Console.WriteLine("参数:");
            Console.WriteLine("  -Source <路径>  源文件或文件夹；多个用分号分隔，或多次指定（必填）");
            Console.WriteLine("  -Target <路径>  目标文件夹，不存在时自动创建（必填）");
            Console.WriteLine("  -Recurse        源为文件夹时递归处理子文件夹");
            Console.WriteLine("  -Filter <通配>  按文件名过滤，多个用分号分隔，如 *.exe;*.txt");
            Console.WriteLine("  -Help           显示本帮助");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  ShortcutBatch.exe -Source D:\\tools -Target D:\\快捷方式 -Recurse -Filter *.exe");
            Console.WriteLine("  ShortcutBatch.exe -Source \"D:\\a.txt;D:\\b.txt\" -Target D:\\快捷方式");
            Console.WriteLine();
            Console.WriteLine("冲突规则: 目标中已有同名 .lnk 时，若其指向同一源文件则覆盖；否则自动改名（如 \"名称 (2).lnk\"）保留两者。");
            Console.WriteLine("不带任何参数启动则进入图形界面。");
        }
    }
}
