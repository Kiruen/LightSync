using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LightSync
{
    public class LSyncJob : INotifyPropertyChanged
    {
        //public static ObservableCollection<Dictionary<string, LSyncJob>> JobsView = new ObservableCollection<Dictionary<string, LSyncJob>>();


        public static ObservableDictionary<string, LSyncJob> Jobs { get; } = new ObservableDictionary<string, LSyncJob>()
        {
            { string.Empty, new LSyncJob(string.Empty, string.Empty) }
        };

        //public static ObservableCollection<KeyValuePair<string, LSyncJob>> Jobs = new ObservableCollection<KeyValuePair<string, LSyncJob>>()
        //{
        //    new KeyValuePair<string, LSyncJob>(string.Empty, new LSyncJob(string.Empty, string.Empty))
        //};

        //public static Dictionary<string, LSyncJob> JobsView { get; } = Jobs;

        public string Id { get; private set; }

        public string Dest { get; private set; }
        public string[] Sources { get; private set; }

        private bool running;
        public bool Running
        {
            get { return running; }
            set
            {
                if (running != value)
                {
                    running = value;
                    OnPropertyChanged(nameof(Running));
                }
            }
        }

        Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();


        public LSyncJob(string dest, params string[] sources) 
        {
            this.Dest = dest;
            this.Sources = sources;
            this.Id = (dest + string.Join("", sources)).GetHashCode().ToString();
        }

        public static string Create(string dest, params string[] sources)
        {
            var job = new LSyncJob(dest, sources);
            if (!Jobs.ContainsKey(job.Id))
            {
                Jobs.Add(job.Id, job);
                return job.Id;
            }

            return string.Empty;
        }        
        
        public static void Start(string id)
        {
            if (Jobs.ContainsKey(id))
            {
                Jobs[id].Start();
            }
        }

        public static void Stop(string id)
        {
            if (Jobs.ContainsKey(id))
            {
                Jobs[id].Stop();
            }
        }

        public void Start() 
        {
            if (!Directory.Exists(Dest))
            {
                Debug.WriteLine("Invalid destination directory.");
            }
            else
            {
                foreach (string sourcePath in Sources)
                {
                    if (!Directory.Exists(sourcePath))
                    {
                        Debug.WriteLine("Invalid source directory.");
                        continue;
                    }

                    var watcher = new FileSystemWatcher
                    {
                        Path = sourcePath,
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true
                    };
                    watcher.Created += Watcher_Changed;
                    watcher.Changed += Watcher_Changed;
                    watcher.Renamed += Watcher_Renamed;

                    watchers.Add(sourcePath, watcher);
                }

                Running = true;
            }
        }

        public void Stop() 
        {
            foreach (var watcher in watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Created -= Watcher_Changed;
                watcher.Changed -= Watcher_Changed;
                watcher.Renamed -= Watcher_Renamed;
                watcher.Dispose();
            }

            watchers.Clear();
            Running = false;
        }


        public static void CompareAndSync(string sourcePath, string destDirPath)
        {
            // 使用 Everything 命令行接口查询源目录下的最新状态
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = @"es\es.exe",
                Arguments = $"-dm -attributes \"{sourcePath}\"",
                RedirectStandardOutput = true,
                //es.exe的目录下运行命令行
                WorkingDirectory = @"C:\Program Files\Everything",
                //是否使用操作系统flash启动
                UseShellExecute = false,
                //接受来自调用程序的输入信息
                RedirectStandardInput = true,
                //重定向标准错误输出
                RedirectStandardError = true,
                //不显示程序窗口
                CreateNoWindow = true,
            };

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 解析最新文件列表
                var latestRecords = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(raw => new LSyncFileInfo(raw)).ToHashSet();

                // 读取目标目录下的记录文件
                string oldRecordsFilePath = Path.Combine(sourcePath, "records.txt");
                var oldRecordRaws = File.Exists(oldRecordsFilePath) ? File.ReadAllLines(oldRecordsFilePath) : new string[0];
                var oldRecords = oldRecordRaws.Select(raw => new LSyncFileInfo(raw)).ToHashSet();

                // 找出有变动的文件
                var modifiedFiles = latestRecords.Except(oldRecords).ToArray();
                // 同步修改的文件到目标目录
                foreach (var fileInfo in modifiedFiles)
                {
                    string sourceFilePath = fileInfo.FilePath;
                    string destinationFilePath = Utils.GetDestPath(sourceFilePath, destDirPath);

                    try
                    {
                        HandleFileEvent(fileInfo.Name, sourceFilePath, destinationFilePath, fileInfo.IsFile);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error occurred during backup: {ex.Message}");
                    }
                }

                // 更新记录文件
                File.WriteAllLines(oldRecordsFilePath, latestRecords.Select(f => f.ToString()).ToArray());
            }
        }

        public static void HandleFileEvent(string oldFileName, string sourcePath, string destinationPath, bool isFile = true)
        {
            oldFileName = Path.GetFileName(oldFileName);
            string fileName = Path.GetFileName(sourcePath);
            string destDir = Path.GetDirectoryName(destinationPath);

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            if (Directory.Exists(destDir))
            {
                if (!isFile)
                {
                    // TODO:处理文件夹的变更，尤其是重命名，避免文件夹的整体复制
                    if (fileName != oldFileName)
                    {
                        var oldDestPath = Path.Combine(Path.GetDirectoryName(destinationPath), oldFileName);
                        if (Directory.Exists(oldDestPath))
                        {
                            Directory.Move(oldDestPath, destinationPath);
                        }
                        else
                        {
                            Utils.CopyFolder(sourcePath, destinationPath);
                        }
                    }
                    else if (fileName == oldFileName)
                    {
                        Utils.CopyFolder(sourcePath, destinationPath);
                    }
                }
                else
                {
                    if (fileName != oldFileName)
                    {
                        try
                        {
                            File.Copy(sourcePath, destinationPath, true);
                            File.Delete(Path.Combine(Path.GetDirectoryName(destinationPath), oldFileName));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error occurred during backup: " + ex.Message);
                        }
                    }
                    else if (File.Exists(destinationPath))
                    {
                        DateTime sourceModifiedTime = File.GetLastWriteTime(sourcePath);
                        DateTime destinationModifiedTime = File.GetLastWriteTime(destinationPath);

                        // 如果源文件的修改时间晚于目标文件的修改时间，则进行覆盖备份
                        if (sourceModifiedTime > destinationModifiedTime)
                        {
                            try
                            {
                                File.Copy(sourcePath, destinationPath, true);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Error occurred during backup: " + ex.Message);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            File.Copy(sourcePath, destinationPath, true);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error occurred during backup: " + ex.Message);
                        }
                    }
                }
            }
            else
            {
                Debug.WriteLine("Invalid destination directory.");
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            string destinationFilePath = Utils.GetDestPath(e.FullPath, Dest);

            HandleFileEvent(Path.GetFileName(e.Name), e.FullPath, destinationFilePath, isFile: !Directory.Exists(e.FullPath));
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            string destinationFilePath = Utils.GetDestPath(e.FullPath, Dest);

            HandleFileEvent(Path.GetFileName(e.OldName), e.FullPath, destinationFilePath, isFile: !Directory.Exists(e.FullPath));
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
