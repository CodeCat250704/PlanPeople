using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace PlanPeople.Splash;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length < 2)
        {
            // 没有参数，直接退出
            Application.Current.Shutdown();
            return;
        }

        string appPath = args[1];

        try
        {
            // 启动主程序
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                Arguments = "--from-splash", // 加上这个参数
                UseShellExecute = true
            });

            if (process != null)
            {
                // 等待主程序的主窗口就绪（60秒超时）
                var timeout = Task.Delay(60000);
                
                // WaitForInputIdle 在 .NET Core/5+ 的 WPF 里可能引发阻塞，改用等待窗口句柄
                var waitTask = Task.Run(() =>
                {
                    while (process.MainWindowHandle == IntPtr.Zero && !process.HasExited)
                    {
                        System.Threading.Thread.Sleep(100);
                        process.Refresh();
                    }
                });

                var completed = await Task.WhenAny(timeout, waitTask);

                if (completed == timeout && process.MainWindowHandle == IntPtr.Zero)
                {
                    // 超时处理
                    MessageBox.Show("启动遇到问题，请手动打开。", "PlanPeople", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动失败: {ex.Message}", "PlanPeople", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Splash 自动退出
            Application.Current.Shutdown();
        }
    }
}