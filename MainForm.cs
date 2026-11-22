using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ShortcutBatch
{
    public sealed class MainForm : Form
    {
        private readonly ListBox sourceList = new ListBox();
        private readonly Button addFilesBtn = new Button();
        private readonly Button addDirBtn = new Button();
        private readonly Button importBtn = new Button();
        private readonly Button exportBtn = new Button();
        private readonly Button removeBtn = new Button();
        private readonly Button clearBtn = new Button();
        private readonly CheckBox recurseBox = new CheckBox();
        private readonly TextBox filterBox = new TextBox();
        private readonly TextBox targetBox = new TextBox();
        private readonly Button browseBtn = new Button();
        private readonly ComboBox targetModeCombo = new ComboBox();
        private readonly TextBox fixedDirBox = new TextBox();
        private readonly Button startBtn = new Button();
        private readonly ListView resultView = new ListView();
        private readonly Label statusLabel = new Label();
        private readonly BackgroundWorker worker = new BackgroundWorker();

        public MainForm()
        {
            Settings.Load();
            Text = "ShortcutBatch - 批量创建快捷方式";
            ClientSize = new Size(760, 650);
            MinimumSize = new Size(680, 560);
            Font = new Font("Microsoft YaHei UI", 9F);
            StartPosition = FormStartPosition.CenterScreen;
            BuildUi();

            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_Progress;
            worker.RunWorkerCompleted += Worker_Completed;
        }

        private void BuildUi()
        {
            var srcGroup = new GroupBox
            {
                Text = "源文件 / 文件夹",
                Location = new Point(12, 8),
                Size = new Size(736, 210),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            sourceList.Location = new Point(16, 24);
            sourceList.Size = new Size(552, 172);
            sourceList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            sourceList.HorizontalScrollbar = true;
            sourceList.SelectionMode = SelectionMode.MultiExtended;

            addFilesBtn.Text = "添加文件…";
            addFilesBtn.Location = new Point(578, 22);
            addFilesBtn.Size = new Size(144, 26);
            addFilesBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            addFilesBtn.Click += (s, e) => AddFiles();

            addDirBtn.Text = "添加文件夹…";
            addDirBtn.Location = new Point(578, 52);
            addDirBtn.Size = new Size(144, 26);
            addDirBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            addDirBtn.Click += (s, e) => AddDirectory();

            importBtn.Text = "导入列表…";
            importBtn.Location = new Point(578, 82);
            importBtn.Size = new Size(144, 26);
            importBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            importBtn.Click += (s, e) => ImportList();

            exportBtn.Text = "导出列表…";
            exportBtn.Location = new Point(578, 112);
            exportBtn.Size = new Size(144, 26);
            exportBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            exportBtn.Click += (s, e) => ExportList();

            removeBtn.Text = "移除选中";
            removeBtn.Location = new Point(578, 142);
            removeBtn.Size = new Size(144, 26);
            removeBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            removeBtn.Click += (s, e) => RemoveSelected();

            clearBtn.Text = "清空";
            clearBtn.Location = new Point(578, 172);
            clearBtn.Size = new Size(144, 26);
            clearBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            clearBtn.Click += (s, e) => sourceList.Items.Clear();

            srcGroup.Controls.AddRange(new Control[] { sourceList, addFilesBtn, addDirBtn, importBtn, exportBtn, removeBtn, clearBtn });

            var optGroup = new GroupBox
            {
                Text = "选项",
                Location = new Point(12, 224),
                Size = new Size(736, 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            recurseBox.Text = "递归处理子文件夹";
            recurseBox.AutoSize = true;
            recurseBox.Location = new Point(16, 22);

            var filterLabel = new Label
            {
                Text = "扩展名过滤（如 *.exe;*.txt，留空=全部）",
                AutoSize = true,
                Location = new Point(178, 24)
            };
            filterBox.Location = new Point(416, 20);
            filterBox.Size = new Size(306, 23);
            filterBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            optGroup.Controls.AddRange(new Control[] { recurseBox, filterLabel, filterBox });

            var tgtGroup = new GroupBox
            {
                Text = "目标文件夹",
                Location = new Point(12, 282),
                Size = new Size(736, 96),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            targetBox.Location = new Point(16, 20);
            targetBox.Size = new Size(560, 23);
            targetBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            browseBtn.Text = "浏览…";
            browseBtn.Location = new Point(586, 19);
            browseBtn.Size = new Size(136, 26);
            browseBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browseBtn.Click += (s, e) => BrowseTarget();

            var modeLabel = new Label
            {
                Text = "默认值：",
                AutoSize = true,
                Location = new Point(16, 56)
            };
            targetModeCombo.Location = new Point(72, 52);
            targetModeCombo.Size = new Size(180, 23);
            targetModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            targetModeCombo.Items.AddRange(new object[] { "源文件所在文件夹", "固定文件夹", "上次的目标文件夹" });
            targetModeCombo.SelectedIndex = ModeToIndex(Settings.DefaultTargetMode);

            fixedDirBox.Location = new Point(262, 53);
            fixedDirBox.Size = new Size(330, 23);
            fixedDirBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            fixedDirBox.Text = Settings.FixedTargetDir;

            var fixedHint = new Label
            {
                Text = "支持 ~ 表示用户目录",
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Location = new Point(598, 56)
            };

            tgtGroup.Controls.AddRange(new Control[] { targetBox, browseBtn, modeLabel, targetModeCombo, fixedDirBox, fixedHint });

            startBtn.Text = "开始创建";
            startBtn.Location = new Point(12, 386);
            startBtn.Size = new Size(150, 34);
            startBtn.Click += (s, e) => Start();

            statusLabel.Text = "就绪";
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(175, 396);
            statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            resultView.Location = new Point(12, 428);
            resultView.Size = new Size(736, 210);
            resultView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            resultView.View = View.Details;
            resultView.FullRowSelect = true;
            resultView.GridLines = true;
            resultView.Columns.Add("源文件", 300);
            resultView.Columns.Add("结果", 70);
            resultView.Columns.Add("快捷方式", 280);
            resultView.Columns.Add("说明", 120);

            Controls.AddRange(new Control[] { srcGroup, optGroup, tgtGroup, startBtn, statusLabel, resultView });
        }

        private static int ModeToIndex(string mode)
        {
            if (mode == Settings.ModeFixed) return 1;
            if (mode == Settings.ModeLast) return 2;
            return 0; // ModeSource
        }

        private static string IndexToMode(int idx)
        {
            switch (idx)
            {
                case 1: return Settings.ModeFixed;
                case 2: return Settings.ModeLast;
                default: return Settings.ModeSource;
            }
        }

        private void AddFiles()
        {
            using (var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Title = "选择源文件",
                InitialDirectory = Directory.Exists(Settings.LastFileDir) ? Settings.LastFileDir : null
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                foreach (var f in dlg.FileNames) AddUnique(f);
                Settings.LastFileDir = Path.GetDirectoryName(dlg.FileNames[0]);
                Settings.Save();
            }
        }

        private void AddDirectory()
        {
            using (var dlg = new FolderBrowserDialog { Description = "选择源文件夹" })
            {
                if (Directory.Exists(Settings.LastFolderDir)) dlg.SelectedPath = Settings.LastFolderDir;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                AddUnique(dlg.SelectedPath);
                Settings.LastFolderDir = dlg.SelectedPath;
                Settings.Save();
            }
        }

        private void ImportList()
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "导入文件表",
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                InitialDirectory = Directory.Exists(Settings.LastFileDir) ? Settings.LastFileDir : null
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                int added = 0;
                try
                {
                    foreach (var raw in File.ReadAllLines(dlg.FileName))
                    {
                        var line = raw.Trim();
                        if (line.Length == 0 || line[0] == '#') continue;
                        if (AddUnique(line)) added++;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "读取文件失败：" + ex.Message, "ShortcutBatch",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Settings.LastFileDir = Path.GetDirectoryName(dlg.FileName);
                Settings.Save();
                statusLabel.Text = string.Format("已导入 {0} 条（跳过重复与空行）。", added);
            }
        }

        private void ExportList()
        {
            if (sourceList.Items.Count == 0)
            {
                MessageBox.Show(this, "列表为空，没有可导出的内容。", "ShortcutBatch",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog
            {
                Title = "导出文件表",
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                FileName = "ShortcutBatch列表.txt"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var sb = new StringBuilder();
                foreach (var it in sourceList.Items) sb.AppendLine(it as string);
                try
                {
                    File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(false));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "写入文件失败：" + ex.Message, "ShortcutBatch",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                statusLabel.Text = string.Format("已导出 {0} 条。", sourceList.Items.Count);
            }
        }

        private bool AddUnique(string path)
        {
            path = path.Trim();
            if (path.Length == 0) return false;
            foreach (var it in sourceList.Items)
                if (string.Equals(it as string, path, StringComparison.OrdinalIgnoreCase)) return false;
            sourceList.Items.Add(path);
            return true;
        }

        private void RemoveSelected()
        {
            for (int i = sourceList.SelectedIndices.Count - 1; i >= 0; i--)
                sourceList.Items.RemoveAt(sourceList.SelectedIndices[i]);
        }

        private void BrowseTarget()
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description = "选择目标文件夹",
                SelectedPath = targetBox.Text.Length > 0 ? targetBox.Text : null
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                targetBox.Text = dlg.SelectedPath;
            }
        }

        private void Start()
        {
            var sources = new List<string>();
            foreach (var it in sourceList.Items)
            {
                var t = (it as string ?? "").Trim();
                if (t.Length > 0) sources.Add(t);
            }
            if (sources.Count == 0)
            {
                MessageBox.Show(this, "请先添加至少一个源文件或文件夹。", "ShortcutBatch",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string target = ResolveTarget(sources);
            if (target.Length == 0)
            {
                MessageBox.Show(this, "请填写目标文件夹，或选择一个可用的默认值来源。", "ShortcutBatch",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            targetBox.Text = target;

            // 若目标默认落到了源文件夹本身，在说明里提示用户注意
            string note = "";
            if (targetModeCombo.SelectedIndex == 0 && Directory.Exists(sources[0])
                && string.Equals(target.TrimEnd('\\'), sources[0].TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                note = "（目标为源文件夹本身）";
            }

            resultView.Items.Clear();
            statusLabel.Text = "处理中…";
            startBtn.Enabled = false;
            worker.RunWorkerAsync(new StartArgs
            {
                Sources = sources,
                Target = target,
                Recurse = recurseBox.Checked,
                Filter = filterBox.Text,
                Note = note
            });
        }

        private string ResolveTarget(List<string> sources)
        {
            string typed = targetBox.Text.Trim();
            if (typed.Length > 0) return typed;

            switch (IndexToMode(targetModeCombo.SelectedIndex))
            {
                case Settings.ModeFixed:
                    return Settings.ExpandTilde(fixedDirBox.Text.Trim());
                case Settings.ModeLast:
                    if (!string.IsNullOrEmpty(Settings.LastTargetDir)) return Settings.LastTargetDir;
                    break;
            }
            // ModeSource：源文件所在文件夹（源是文件夹则取文件夹本身）
            if (sources.Count == 0) return "";
            string first = sources[0];
            return Directory.Exists(first) ? first : Path.GetDirectoryName(first);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = (StartArgs)e.Argument;
            try
            {
                Directory.CreateDirectory(args.Target);
            }
            catch (Exception ex)
            {
                throw new Exception("无法创建目标文件夹 " + args.Target + "：" + ex.Message);
            }

            var files = new List<string>();
            var warnings = new List<string>();
            ShortcutCreator.CollectFiles(args.Sources, args.Recurse, args.Filter, files, warnings);
            foreach (var w in warnings)
                worker.ReportProgress(0, new UiItem("", "", "警告", w));

            int created = 0, overwritten = 0, renamed = 0, failed = 0;
            for (int i = 0; i < files.Count; i++)
            {
                var r = ShortcutCreator.CreateFor(files[i], args.Target);
                switch (r.Outcome)
                {
                    case ShortcutOutcome.Created: created++; break;
                    case ShortcutOutcome.Overwritten: overwritten++; break;
                    case ShortcutOutcome.Renamed: renamed++; break;
                    default: failed++; break;
                }
                worker.ReportProgress((int)Math.Round((i + 1) * 100.0 / files.Count),
                    new UiItem(r.SourceFile, r.LinkFile, OutcomeText(r.Outcome), r.Message));
            }

            // 记录本次使用的目标文件夹，供「上次的目标文件夹」默认值使用
            Settings.LastTargetDir = args.Target;
            Settings.DefaultTargetMode = IndexToMode(targetModeCombo.SelectedIndex);
            Settings.FixedTargetDir = fixedDirBox.Text.Trim();
            Settings.Save();

            e.Result = new Summary
            {
                Total = files.Count,
                Created = created,
                Overwritten = overwritten,
                Renamed = renamed,
                Failed = failed,
                Note = args.Note
            };
        }

        private void Worker_Progress(object sender, ProgressChangedEventArgs e)
        {
            var item = e.UserState as UiItem;
            if (item == null) return;
            var lvi = new ListViewItem(item.Source ?? "");
            lvi.SubItems.Add(item.Outcome ?? "");
            lvi.SubItems.Add(item.Link ?? "");
            lvi.SubItems.Add(item.Message ?? "");
            resultView.Items.Add(lvi);
            if (resultView.Items.Count > 0)
                resultView.Items[resultView.Items.Count - 1].EnsureVisible();
        }

        private void Worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            startBtn.Enabled = true;
            if (e.Error != null)
            {
                statusLabel.Text = "出错：" + e.Error.Message;
                MessageBox.Show(this, "处理失败：" + e.Error.Message, "ShortcutBatch",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var s = (Summary)e.Result;
            statusLabel.Text = string.Format("完成：共 {0} 个，新建 {1}，覆盖 {2}，改名 {3}，失败 {4}。{5}",
                s.Total, s.Created, s.Overwritten, s.Renamed, s.Failed, s.Note);
        }

        private static string OutcomeText(ShortcutOutcome o)
        {
            switch (o)
            {
                case ShortcutOutcome.Created: return "新建";
                case ShortcutOutcome.Overwritten: return "覆盖";
                case ShortcutOutcome.Renamed: return "改名";
                default: return "失败";
            }
        }

        private sealed class StartArgs
        {
            public List<string> Sources;
            public string Target;
            public bool Recurse;
            public string Filter;
            public string Note;
        }

        private sealed class UiItem
        {
            public readonly string Source, Outcome, Link, Message;
            public UiItem(string s, string l, string o, string m)
            {
                Source = s; Link = l; Outcome = o; Message = m;
            }
        }

        private sealed class Summary
        {
            public int Total, Created, Overwritten, Renamed, Failed;
            public string Note;
        }
    }
}
