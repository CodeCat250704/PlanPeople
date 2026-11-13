using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.App.Views;
using PlanPeople.Core.Models;

namespace PlanPeople.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;

    public TodayViewModel TodayVM { get; }
    public WeekViewModel WeekVM { get; }
    public DocsViewModel DocsVM { get; }
    public AiChatViewModel AiChatVM { get; }
    public ProfileViewModel ProfileVM { get; } // 改为个人中心

    public MainWindowViewModel()
    {
        TodayVM = new TodayViewModel();
        WeekVM = new WeekViewModel();
        DocsVM = new DocsViewModel();
        AiChatVM = new AiChatViewModel();
        
        // 初始化设置和个人中心
        var settingsVM = new SettingsViewModel();
        ProfileVM = new ProfileViewModel(settingsVM);
        
        _currentPage = TodayVM;

        // 绑定事件
        TodayVM.RequestEditEvent = OnRequestEditEventAsync;
        TodayVM.RequestImportCsv = OnRequestImportCsvAsync;
        WeekVM.RequestEditEvent = OnRequestEditEventAsync;
        
        // 初始化时加载数据
        _ = TodayVM.LoadEventsAsync().ContinueWith(t => { }, TaskContinuationOptions.OnlyOnFaulted);
    }

    [RelayCommand]
    private async Task NavigateTodayAsync()
    {
        CurrentPage = TodayVM;
        await TodayVM.LoadEventsAsync();
    }

    [RelayCommand]
    private void NavigateProfile() => CurrentPage = ProfileVM; // 导航到个人中心

    [RelayCommand]
    private void NavigateDocs() => CurrentPage = DocsVM;

    [RelayCommand]
    private void NavigateAiChat() => CurrentPage = AiChatVM;

    [RelayCommand]
    private async Task NavigateWeekAsync()
    {
        CurrentPage = WeekVM;
        await WeekVM.LoadEventsAsync();
    }

    private async Task OnRequestEditEventAsync(CalendarEvent? ev)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var editVM = new EventEditViewModel(ev);
        var window = new Window
        {
            Content = new EventEditView { DataContext = editVM },
            Width = 480,
            Height = 650,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            Background = Avalonia.Media.Brushes.Transparent,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent }
        };

        var tcs = new TaskCompletionSource();
        editVM.CloseAction = () => { window.Close(); tcs.TrySetResult(); };

        window.ShowDialog(mainWindow);
        await tcs.Task;
        
        await TodayVM.LoadEventsAsync();
    }

    private async Task OnRequestImportCsvAsync()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return;

        var vm = new CsvImportViewModel();
        var window = new Window
        {
            Content = new CsvImportView { DataContext = vm },
            Width = 650, 
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            Background = Avalonia.Media.Brushes.Transparent,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent }
        };
        
        vm.RequestPickFile = async () =>
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null) return null;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions 
            { 
                Title = "选择 CSV 文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("CSV 文件") { Patterns = new[] { "*.csv" } } } 
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        };

        var tcs = new TaskCompletionSource();
        vm.CloseAction = () => { window.Close(); tcs.TrySetResult(); };
        
        window.ShowDialog(mainWindow);
        await tcs.Task;
        
        await TodayVM.LoadEventsAsync();
    }

    private Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }
}