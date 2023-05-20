using ControlzEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightSync
{
    internal static class Utils
    {
        public static void CopyFolder(string sourceFolder, string destinationFolder)
        {
            // 创建目标文件夹
            Directory.CreateDirectory(destinationFolder);

            // 获取源文件夹中的文件列表
            string[] files = Directory.GetFiles(sourceFolder);

            // 获取源文件夹中的子文件夹列表
            string[] folders = Directory.GetDirectories(sourceFolder);

            // 使用多线程并行处理子文件夹的拷贝
            Parallel.ForEach(folders, folder =>
            {
                string folderName = Path.GetFileName(folder);
                string destinationSubFolder = Path.Combine(destinationFolder, folderName);
                CopyFolder(folder, destinationSubFolder);
            });

            // 处理文件拷贝
            foreach (string file in files)
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    string destinationFilePath = Path.Combine(destinationFolder, fileName);
                    if (!File.Exists(destinationFilePath))
                    {
                        File.Copy(file, destinationFilePath);
                        File.SetLastWriteTime(destinationFilePath, File.GetLastWriteTime(file));
                    }
                }
                catch (Exception exc)
                {
                    Debug.WriteLine(exc);
                }
            };
        }

        public static string GetDestPath(string sourceFilePath, string destDirPath)
        {
            string destinationFilePath = Path.Combine(destDirPath, sourceFilePath.Remove(0, Path.GetPathRoot(sourceFilePath).Length));
            return destinationFilePath;
        }

        public static string BrowseAndSelectFolder()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.ValidateNames = false;
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;
            dialog.FileName = "文件夹选择";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return Path.GetDirectoryName(dialog.FileName);
            }

            return string.Empty;
        }
    }
}
