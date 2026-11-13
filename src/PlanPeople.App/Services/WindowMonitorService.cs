using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PlanPeople.App.Services;

public static class WindowMonitorService
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    public static string GetActiveWindowTitle()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return "";
        
        GetWindowThreadProcessId(hWnd, out var pid);
        var title = new StringBuilder(256);
        GetWindowText(hWnd, title, 256);

        try
        {
            var process = Process.GetProcessById((int)pid);
            return $"{process.ProcessName}: {title}";
        }
        catch { return title.ToString(); }
    }

    public static string GetActiveAppIconPath()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return "";
        GetWindowThreadProcessId(hWnd, out var pid);
        
        try
        {
            var process = Process.GetProcessById((int)pid);
            // 获取真实 exe 路径，后续可从 C 盘直接加载提取图标
            return process.MainModule?.FileName ?? "";
        }
        catch { return ""; }
    }
}