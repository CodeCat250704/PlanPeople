using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.AI;
using PlanPeople.App.Services;
using PlanPeople.Core.Models;

namespace PlanPeople.App.ViewModels;

public partial class DocsViewModel : ViewModelBase
{
    public ObservableCollection<Note> Notes { get; } = new();

    [ObservableProperty] private Note? _selectedNote;
    [ObservableProperty] private bool _hasSelectedNote;
    [ObservableProperty] private string _editTitle = "";
    [ObservableProperty] private string _editContent = "";
    [ObservableProperty] private string _aiStatus = "";

    private string _lastSavedContent = "";
    private DispatcherTimer? _autoSaveTimer;

    public DocsViewModel()
    {
        _ = LoadNotesAsync();
    }

    public async Task LoadNotesAsync()
    {
        Notes.Clear();
        var all = await App.NoteRepo.GetAllAsync();
        foreach (var n in all) Notes.Add(n);
    }

    partial void OnSelectedNoteChanged(Note? value)
    {
        HasSelectedNote = value != null;
        if (value != null)
        {
            EditTitle = value.Title;
            EditContent = value.Content;
            _lastSavedContent = value.Content;
        }
        else
        {
            EditTitle = "";
            EditContent = "";
            _lastSavedContent = "";
        }
    }

    partial void OnEditContentChanged(string value)
    {
        if (SelectedNote == null || value == _lastSavedContent) return;

        _autoSaveTimer?.Stop();
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _autoSaveTimer.Tick += async (s, e) =>
        {
            _autoSaveTimer.Stop();
            await SaveNoteInternalAsync(false);
        };
        _autoSaveTimer.Start();
    }

    [RelayCommand]
    private async Task AddNoteAsync()
    {
        var note = new Note { Title = "未命名文档", Content = "# 开始输入..." };
        await App.NoteRepo.AddAsync(note);
        await LoadNotesAsync();
        SelectedNote = Notes[0];
    }

    [RelayCommand]
    private async Task SaveNoteAsync() => await SaveNoteInternalAsync(true);

    private async Task SaveNoteInternalAsync(bool showToast)
    {
        if (SelectedNote == null) return;

        SelectedNote.Title = string.IsNullOrWhiteSpace(EditTitle) ? "未命名文档" : EditTitle;
        SelectedNote.Content = EditContent;
        SelectedNote.UpdatedAt = DateTimeOffset.Now;
        _lastSavedContent = EditContent;

        await App.NoteRepo.UpdateAsync(SelectedNote);
        if (showToast) await ToastService.ShowAsync("文档已保存");
    }

    [RelayCommand]
    private async Task DeleteNoteAsync()
    {
        if (SelectedNote == null) return;
        await App.NoteRepo.DeleteAsync(SelectedNote.Id);
        SelectedNote = null;
        await LoadNotesAsync();
        await ToastService.ShowAsync("文档已删除");
    }

    public Func<Task<string?>>? RequestImportFile { get; set; }

    [RelayCommand]
    private async Task ExportMarkdownAsync()
    {
        if (SelectedNote == null) return;
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var filePath = Path.Combine(desktop, $"{SelectedNote.Title}.md");
        await File.WriteAllTextAsync(filePath, EditContent);
        await ToastService.ShowAsync("已导出到桌面");
    }

    [RelayCommand]
    private async Task ImportMarkdownAsync()
    {
        if (RequestImportFile == null) return;
        var path = await RequestImportFile.Invoke();
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var content = await File.ReadAllTextAsync(path);
            var title = Path.GetFileNameWithoutExtension(path);
            var note = new Note { Title = title, Content = content };
            await App.NoteRepo.AddAsync(note);
            await LoadNotesAsync();
            SelectedNote = Notes[0];
            await ToastService.ShowAsync("导入成功");
        }
        catch (Exception ex)
        {
            await ToastService.ShowAsync($"导入失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AiPolishAsync()
    {
        if (SelectedNote == null || string.IsNullOrWhiteSpace(EditContent)) return;

        var settings = AppSettings.Load();
        var apiKey = settings.GetAiApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            await ToastService.ShowAsync("请先在设置中配置 API Key");
            return;
        }

        AiStatus = "AI 正在思考...";
        try
        {
            var client = new OpenAiClient();
            var prompt = $"请帮我润色并补全以下 Markdown 文档。保持原有的语法格式，直接输出最终文本，不要包含代码块包裹：\n\n{EditContent}";
            
            var result = await client.SendMessageAsync(settings.AiBaseUrl, apiKey, settings.AiModel, "你是专业文档助手", prompt);
            
            if (result.StartsWith("```markdown")) result = result.Substring(11);
            if (result.EndsWith("```")) result = result.Substring(0, result.Length - 3);

            // 使用 Dispatcher 将结果回传 UI
            Dispatcher.UIThread.Post(() =>
            {
                EditContent = result.Trim();
                AiStatus = "润色完成";
            });
            
            await ToastService.ShowAsync("AI 润色完成");
        }
        catch (Exception ex)
        {
            AiStatus = "失败";
            await ToastService.ShowAsync($"AI 失败: {ex.Message}");
        }
    }
}