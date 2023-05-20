using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LightSync
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 订阅 AppDomain.CurrentDomain.UnhandledException 事件
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // 订阅 Application.DispatcherUnhandledException 事件
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // 处理未处理的异常，例如记录日志等

            // 重新启动应用程序
            RestartApplication();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 处理未处理的异常，例如记录日志等

            // 重新启动应用程序
            RestartApplication();

            // 标记异常已处理，防止应用程序关闭
            e.Handled = true;
        }

        private void RestartApplication()
        {
            // 获取当前应用程序的可执行文件路径
            string appPath = Process.GetCurrentProcess().MainModule.FileName;

            // 启动新的进程来重新运行应用程序
            Process.Start(appPath);

            // 关闭当前进程
            Environment.Exit(0);
        }
    }
}
