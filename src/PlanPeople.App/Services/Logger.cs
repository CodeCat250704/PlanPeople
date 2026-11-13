using System;
using System.IO;

namespace PlanPeople.App.Services;

public static class Logger
{
    private static readonly string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
    private static readonly object LockObj = new();

    static Logger()
    {
        try
        {
            if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
        }
        catch { /* 忽略创建目录失败 */ }
    }

    public static void Info(string message)
    {
        WriteLog("INFO", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var fullMsg = ex == null ? message : $"{message}\n异常信息: {ex.Message}\n堆栈: {ex.StackTrace}";
        WriteLog("ERROR", fullMsg);
    }

    private static void WriteLog(string level, string message)
    {
        try
        {
            var fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
            var filePath = Path.Combine(LogDir, fileName);
            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}\n";

            lock (LockObj)
            {
                File.AppendAllText(filePath, logLine);
            }
        }
        catch 
        {
            // 连日志都写不进去就没办法了
        }
    }
}