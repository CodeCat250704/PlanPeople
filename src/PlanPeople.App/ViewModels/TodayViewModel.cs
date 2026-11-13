using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.Core.Ics;
using PlanPeople.Core.Models;
using PlanPeople.App.Services;

namespace PlanPeople.App.ViewModels;

public partial class TodayViewModel : ViewModelBase
{
    [ObservableProperty] private DateTime _currentDate = DateTime.Today;
    [ObservableProperty] private string _dateTitle = "";
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _highPriorityCount;
    
    // 搜索与标签
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string? _selectedTag = "全部";
    public ObservableCollection<string> AllTags { get; } = new() { "全部" };

    public ObservableCollection<CalendarEvent> TodayEvents { get; } = new();

    public Func<CalendarEvent?, Task>? RequestEditEvent { get; set; }
    public Func<Task<string?>>? RequestImportFile { get; set; }
    public Func<Task>? RequestImportCsv { get; set; }

    public TodayViewModel()
    {
        UpdateTitle();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadEventsAsync();
    partial void OnSelectedTagChanged(string? value) => _ = LoadEventsAsync();

    [RelayCommand] private void PreviousDay() { CurrentDate = CurrentDate.AddDays(-1); UpdateTitle(); _ = LoadEventsAsync(); }
    [RelayCommand] private void NextDay() { CurrentDate = CurrentDate.AddDays(1); UpdateTitle(); _ = LoadEventsAsync(); }
    [RelayCommand] private async Task AddEventAsync() { if (RequestEditEvent != null) await RequestEditEvent.Invoke(null); }
    [RelayCommand] private async Task EditEventAsync(CalendarEvent ev) { if (RequestEditEvent != null) await RequestEditEvent.Invoke(ev); }

    public async Task DeleteEventAsync(CalendarEvent ev)
    {
        await App.EventRepo.DeleteAsync(ev.Id);
        await LoadEventsAsync();
        await ToastService.ShowAsync("日程已删除");
    }

    [RelayCommand]
    private async Task ImportIcsAsync()
    {
        if (RequestImportFile == null) return;
        var filePath = await RequestImportFile.Invoke();
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var importedEvents = IcsParser.Parse(content);
            foreach (var e in importedEvents)
            {
                var existing = await App.EventRepo.GetByIdAsync(e.Id);
                if (existing == null) await App.EventRepo.AddAsync(e);
                else await App.EventRepo.UpdateAsync(e);
            }
            await LoadEventsAsync();
            await ToastService.ShowAsync("ICS 导入成功");
        }
        catch (Exception ex) { await ToastService.ShowAsync($"导入失败: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ImportCsvAsync()
    {
        if (RequestImportCsv != null) await RequestImportCsv.Invoke();
    }

    [RelayCommand]
    private async Task ExportIcsAsync()
    {
        try
        {
            var all = await App.EventRepo.GetAllAsync();
            var icsContent = IcsWriter.GenerateIcs(all.ToList());
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktop, $"PlanPeople_{DateTime.Now:yyyyMMddHHmmss}.ics");
            await File.WriteAllTextAsync(filePath, icsContent);
            await ToastService.ShowAsync("ICS 导出成功");
        }
        catch (Exception ex) { await ToastService.ShowAsync($"导出失败: {ex.Message}"); }
    }

    public async Task LoadEventsAsync()
    {
        try
        {
            var all = await App.EventRepo.GetAllAsync();
            
            // 动态收集标签
            var tags = all.SelectMany(e => e.Categories).Distinct().ToList();
            AllTags.Clear();
            AllTags.Add("全部");
            foreach (var t in tags) AllTags.Add(t);

            var start = CurrentDate.Date;
            var end = start.AddDays(1);

            var todays = all.Where(e => e.Start.LocalDateTime < end && e.End.LocalDateTime > start);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.ToLower();
                todays = todays.Where(e => 
                    e.Title.ToLower().Contains(query) || 
                    (e.Description?.ToLower().Contains(query) ?? false) ||
                    (e.Location?.ToLower().Contains(query) ?? false));
            }

            if (SelectedTag != "全部" && !string.IsNullOrEmpty(SelectedTag))
            {
                todays = todays.Where(e => e.Categories.Contains(SelectedTag));
            }

            var result = todays
                .OrderBy(e => e.Priority == 2 ? 0 : e.Priority == 1 ? 1 : 2)
                .ThenBy(e => e.Start)
                .ToList();

            TodayEvents.Clear();
            foreach (var e in result) TodayEvents.Add(e);
            
            TotalCount = result.Count;
            HighPriorityCount = result.Count(e => e.Priority > 0);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"加载失败: {ex.Message}"); }
    }

    private void UpdateTitle()
    {
        if (CurrentDate.Date == DateTime.Today) DateTitle = "今天";
        else if (CurrentDate.Date == DateTime.Today.AddDays(1)) DateTitle = "明天";
        else if (CurrentDate.Date == DateTime.Today.AddDays(-1)) DateTitle = "昨天";
        else DateTitle = CurrentDate.ToString("MM月dd日 dddd");
    }
}