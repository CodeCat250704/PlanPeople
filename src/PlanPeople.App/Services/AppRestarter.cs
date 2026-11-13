using System;
using System.Diagnostics; // 补充这个
using System.IO;
// 删掉 using System.Windows;

namespace PlanPeople.App.Services;

public static class AppRestarter
{
    public static void RestartWithSplash()
    {
        try
        {
            // 1. 拿到当前主程序的路径
            string appPath = Environment.ProcessPath ?? "";
            if (string.IsNullOrEmpty(appPath)) return;

            // 2. 拿到 Splash 的路径（默认和主程序在同一目录）
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string splashPath = Path.Combine(baseDir, "PlanPeople.Splash.exe");

            if (File.Exists(splashPath))
            {
                // 3. 启动 Splash，把主程序路径传给它
                Process.Start(new ProcessStartInfo
                {
                    FileName = splashPath,
                    Arguments = $"\"{appPath}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                // 如果找不到 Splash，直接重启自己
                Process.Start(new ProcessStartInfo(appPath) { UseShellExecute = true });
            }

            // 4. 结束当前进程
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"重启失败: {ex.Message}");
        }
    }
}