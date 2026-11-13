using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.Core.Models;

namespace PlanPeople.App.ViewModels;

public partial class CsvImportViewModel : ViewModelBase
{
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private ObservableCollection<string> _headers = new();
    [ObservableProperty] private ObservableCollection<string[]> _previewRows = new();
    [ObservableProperty] private string _status = "";

    // 映射属性
    [ObservableProperty] private string? _selectedTitleColumn;
    [ObservableProperty] private string? _selectedStartColumn;
    [ObservableProperty] private string? _selectedEndColumn;
    [ObservableProperty] private string? _selectedLocationColumn;
    [ObservableProperty] private string? _selectedDescriptionColumn;

    public Func<Task<string?>>? RequestPickFile { get; set; }
    public Action? CloseAction { get; set; }

    private List<string[]> _allData = new();

    public CsvImportViewModel()
    {
        _ = LoadFileAsync(); // 启动时主动拉起文件选择
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        if (RequestPickFile == null) return;
        var path = await RequestPickFile.Invoke();
        if (string.IsNullOrEmpty(path)) return;

        FilePath = path;
        LoadData();
    }

    private async Task LoadFileAsync()
    {
        await PickFileAsync();
    }

    private void LoadData()
    {
        try
        {
            var lines = File.ReadAllLines(FilePath);
            if (lines.Length < 2) { Status = "文件内容不足"; return; }

            // 解析表头
            Headers.Clear();
            var headerLine = lines[0].Split(',');
            foreach (var h in headerLine) Headers.Add(h.Trim());

            // 预览前 5 行
            PreviewRows.Clear();
            _allData.Clear();
            for (int i = 1; i < lines.Length; i++)
            {
                var row = lines[i].Split(',');
                _allData.Add(row);
                if (i <= 5) PreviewRows.Add(row);
            }

            Status = $"共加载 {_allData.Count} 行数据";
        }
        catch (Exception ex)
        {
            Status = $"读取失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (string.IsNullOrEmpty(SelectedTitleColumn) || string.IsNullOrEmpty(SelectedStartColumn))
        {
            Status = "标题和开始时间是必填项";
            return;
        }

        int successCount = 0;
        int titleIdx = Headers.IndexOf(SelectedTitleColumn);
        int startIdx = Headers.IndexOf(SelectedStartColumn);
        int endIdx = SelectedEndColumn != null ? Headers.IndexOf(SelectedEndColumn) : -1;
        int locIdx = SelectedLocationColumn != null ? Headers.IndexOf(SelectedLocationColumn) : -1;
        int descIdx = SelectedDescriptionColumn != null ? Headers.IndexOf(SelectedDescriptionColumn) : -1;

        foreach (var row in _allData)
        {
            try
            {
                if (row.Length <= Math.Max(titleIdx, startIdx)) continue;

                var ev = new CalendarEvent
                {
                    Title = row[titleIdx].Trim(),
                    Start = DateTimeOffset.TryParse(row[startIdx], out var s) ? s : DateTimeOffset.Now,
                    End = endIdx >= 0 && DateTimeOffset.TryParse(row[endIdx], out var e) ? e : DateTimeOffset.Now.AddHours(1),
                    Location = locIdx >= 0 && row.Length > locIdx ? row[locIdx].Trim() : null,
                    Description = descIdx >= 0 && row.Length > descIdx ? row[descIdx].Trim() : null,
                    Source = "csv"
                };

                await App.EventRepo.AddAsync(ev);
                successCount++;
            }
            catch { /* 忽略单行解析失败 */ }
        }

        Status = $"导入完成，成功 {successCount} 条";
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseAction?.Invoke();
}