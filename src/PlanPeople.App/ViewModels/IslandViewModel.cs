using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.App.Services;
using PlanPeople.Core.Models;

namespace PlanPeople.App.ViewModels;

public partial class IslandViewModel : ViewModelBase
{
    [ObservableProperty] private string _currentTime = DateTime.Now.ToString("HH:mm:ss");
    [ObservableProperty] private string _currentDate = DateTime.Now.ToString("MM月dd日 dddd");
    
    // 天气数据
    [ObservableProperty] private string _weatherTemp = "--°C";
    [ObservableProperty] private string _weatherCondition = "获取中...";
    
    // 新闻数据
    [ObservableProperty] private string _latestNews = "加载新闻中...";
    
    // 系统状态
    [ObservableProperty] private string _cpuUsage = "CPU --%";
    [ObservableProperty] private string _ramUsage = "内存 --%";

    public ObservableCollection<CalendarEvent> TodayEvents { get; } = new();
    private readonly HttpClient _httpClient = new();

    public IslandViewModel()
    {
        // 启动时间刷新
        var timeTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timeTimer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        timeTimer.Start();

        // 加载数据
        _ = LoadTodayEventsAsync();
        _ = LoadNetworkDataAsync();
        _ = LoadSystemStatusAsync();
    }

    private async Task LoadNetworkDataAsync()
    {
        try
        {
            // 真实天气获取 (wttr.in)
            var weatherJson = await _httpClient.GetStringAsync("https://wttr.in/?format=j1");
            using var weatherDoc = JsonDocument.Parse(weatherJson);
            var current = weatherDoc.RootElement.GetProperty("current_condition")[0];
            WeatherTemp = $"{current.GetProperty("temp_C").GetString()}°C";
            WeatherCondition = current.GetProperty("weatherDesc")[0].GetProperty("value").GetString() ?? "未知";

            // 真实热搜/新闻获取 (百度热搜 JSON 接口)
            var newsRes = await _httpClient.GetStringAsync("https://api.vvhan.com/api/hotlist/baiduRD");
            using var newsDoc = JsonDocument.Parse(newsRes);
            if (newsDoc.RootElement.GetProperty("data").GetArrayLength() > 0)
            {
                LatestNews = "热点: " + newsDoc.RootElement.GetProperty("data")[0].GetProperty("title").GetString();
            }
        }
        catch
        {
            WeatherCondition = "网络异常";
            LatestNews = "获取新闻失败";
        }
    }

    private async Task LoadSystemStatusAsync()
    {
        while (true)
        {
            try
            {
                // PerformanceCounter 在非 Windows 或部分环境可能不可用，用 try-catch 保护
                var cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                var ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "% Committed Bytes In Use");
                
                cpuCounter.NextValue();
                ramCounter.NextValue();
                await Task.Delay(1000);
                
                CpuUsage = $"CPU {Math.Round(cpuCounter.NextValue())}%";
                RamUsage = $"内存 {Math.Round(ramCounter.NextValue())}%";
            }
            catch 
            {
                CpuUsage = "CPU --%";
                RamUsage = "内存 --%";
            }

            await Task.Delay(3000);
        }
    }

    public async Task LoadTodayEventsAsync()
    {
        var all = await App.EventRepo.GetAllAsync();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var todaysEvents = all.Where(e => e.Start.LocalDateTime >= today && e.Start.LocalDateTime < tomorrow)
                              .OrderBy(e => e.Start)
                              .ToList();

        TodayEvents.Clear();
        foreach (var ev in todaysEvents) TodayEvents.Add(ev);
    }

    [RelayCommand]
    private void OpenMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Show();
            desktop.MainWindow!.WindowState = Avalonia.Controls.WindowState.Normal;
            desktop.MainWindow.Activate();
        }
    }
}