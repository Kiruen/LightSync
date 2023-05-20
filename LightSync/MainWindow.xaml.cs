using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace LightSync
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private HotKeysRegister hotKeysRegister;

        public MainWindow()
        {
            InitializeComponent();
        }

        private IntPtr hwnd;
        private HwndSource hwndSource;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 获取窗口句柄
            hwnd = new WindowInteropHelper(this).Handle;
            hotKeysRegister = new HotKeysRegister(hwnd);

            // 注册快捷键Alt+Shift+S：显示/隐藏主界面
            hotKeysRegister.Register(HKModifiers.Alt | HKModifiers.Shift,
                KeyInterop.VirtualKeyFromKey(Key.S), () =>
            {
                if (IsVisible)
                {
                    Hide();
                }
                else
                {
                    MoveToScreenCentre();
                    WindowState = WindowState.Normal;
                    Topmost = true;
                    Show();
                }
            }, reenterable: false);

            // 创建消息处理函数
            hwndSource = HwndSource.FromHwnd(hwnd);
            hwndSource.AddHook(new HwndSourceHook(WndProc));

            LSyncJob.LoadJobsFromFile("jobs.json");
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                if (hotKeysRegister != null)
                {
                    hotKeysRegister.Process(msg, wParam);
                }
            }

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 解除注册的全局快捷键
            hotKeysRegister.UnRegisterAll();
            hwndSource.RemoveHook(new HwndSourceHook(WndProc));

            LSyncJob.SaveJobsToFile("jobs.json");
        }

        private void MoveToScreenCentre()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;

            this.Left = (screenWidth - windowWidth) / 2;
            this.Top = (screenHeight - windowHeight) / 2;
        }


        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            if (listViewJobs.SelectedItem != null)
            {
                var job = ((KeyValuePair<string, LSyncJob>)listViewJobs.SelectedItem).Value;
                job.Start();
            }
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            if (listViewJobs.SelectedItem != null)
            {
                var job = ((KeyValuePair<string, LSyncJob>)listViewJobs.SelectedItem).Value;
                job.Stop();
            }
        }

        private void buttonFullVolume_Click(object sender, RoutedEventArgs e)
        {
            string source = comboBoxSourcePaths.Text;
            string dest = textBoxDest.Text;
            Task.Run(() =>
            {
                LSyncJob.CompareAndSync(source, dest);
                MessageBox.Show("全量备份结束");
            });
            MessageBox.Show("全量备份开始");
        }

        private void buttonBrowseSourcePath_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = Utils.BrowseAndSelectFolder();
            // 向 ComboBox 添加选项
            if (!comboBoxSourcePaths.Items.Contains(folderPath))
            {
                comboBoxSourcePaths.Items.Add(folderPath);
            }

            // 选中新添加的项
            comboBoxSourcePaths.SelectedItem = folderPath;
        }

        private void buttonBrowseDestPath_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = Utils.BrowseAndSelectFolder();
            textBoxDest.Text = folderPath;
        }

        private void buttonAddJob_Click(object sender, RoutedEventArgs e)
        {
            int itemCount = comboBoxSourcePaths.Items.Count;
            string[] paths = new string[itemCount];

            for (int i = 0; i < itemCount; i++)
            {
                if (comboBoxSourcePaths.Items[i] is ComboBoxItem item)
                {
                    paths[i] = (string)item.Content;
                }
                else
                {
                    paths[i] = comboBoxSourcePaths.Items[i].ToString();
                }
            }

            LSyncJob.Create(textBoxDest.Text, paths);

            comboBoxSourcePaths.Items.Clear();
        }

        private void comboBoxSourcePaths_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string path = comboBoxSourcePaths.Text.Trim();
                if (!string.IsNullOrEmpty(path) && !comboBoxSourcePaths.Items.Contains(path))
                {
                    comboBoxSourcePaths.Items.Add(path);
                }
            }
        }
    }

    public class CollectionToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable collection)
            {
                return string.Join(",", collection.Cast<object>());
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToSwitchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            return boolValue ? "开" : "关";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
