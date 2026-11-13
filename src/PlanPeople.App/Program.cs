using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;

namespace PlanPeople.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 1. 全局异常拦截：任何主线程没抓到的异常，都会写进 crash.log
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            LogCrash(ex, "AppDomain");
        };

        // 2. 任务异常拦截：所有 async Task 里没 catch 的异常
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogCrash(e.Exception, "TaskScheduler");
            e.SetObserved();
        };

        try
        {
            // 检查是否是 Splash 传过来的重启参数
            if (args.Contains("--from-splash"))
            {
                // 只是做标记，主程序正常启动即可
            }

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogCrash(ex, "Main");
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static void RestartWithSplash()
    {
        try
        {
            string appPath = Environment.ProcessPath ?? "";
            if (string.IsNullOrEmpty(appPath)) return;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string splashPath = Path.Combine(baseDir, "PlanPeople.Splash.exe");

            if (File.Exists(splashPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = splashPath,
                    Arguments = $"\"{appPath}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo(appPath) { UseShellExecute = true });
            }

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"重启失败: {ex.Message}");
        }
    }

    private static void LogCrash(Exception? ex, string source)
    {
        if (ex == null) return;
        
        try
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
            string content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n{ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logPath, content);
        }
        catch
        {
            // 如果连写日志都失败，那就只能忽略了
        }
    }
}