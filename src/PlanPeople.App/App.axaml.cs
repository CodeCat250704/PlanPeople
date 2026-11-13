using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using PlanPeople.App.Views;
using PlanPeople.App.ViewModels;
using PlanPeople.App.Services;
using PlanPeople.App.Localization;
using PlanPeople.Data;
using PlanPeople.Data.Migrations;
using PlanPeople.Data.Repositories;
using PlanPeople.Sync;

namespace PlanPeople.App;

public partial class App : Application
{
    public static SqliteConnectionFactory DbFactory { get; private set; } = null!;
    public static EventRepository EventRepo { get; private set; } = null!;
    public static NoteRepository NoteRepo { get; private set; } = null!;
    public static ChatRepository ChatRepo { get; private set; } = null!;

    private TrayIcon? _trayIcon;
    private DispatcherTimer? _syncTimer;
    private IslandWindow? _islandWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // 1. 全局同步代码异常拦截
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Logger.Error("AppDomain 未处理异常", ex);
            if (Dispatcher.UIThread.CheckAccess())
            {
                _ = DialogService.ShowMessageAsync("致命错误", ex?.Message ?? "发生未知错误，请查看日志。");
            }
        };

        // 2. 全局异步任务异常拦截
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved();
            Logger.Error("TaskScheduler 未观察到的任务异常", e.Exception);
            
            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await DialogService.ShowMessageAsync("后台任务异常", e.Exception?.InnerException?.Message ?? e.Exception?.Message ?? "未知错误");
                }
                catch (Exception dialogEx)
                {
                    Logger.Error("弹窗展示失败", dialogEx);
                }
            });
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            Logger.Info("应用启动");
            
            var settings = AppSettings.Load();
            ThemeService.SetTheme(settings.Theme);
            LocalizationService.Instance.ChangeLanguage(settings.Language);

            DbFactory = new SqliteConnectionFactory();
            MigrationRunner.Run(DbFactory); 
            EventRepo = new EventRepository(DbFactory);
            NoteRepo = new NoteRepository(DbFactory);
            ChatRepo = new ChatRepository(DbFactory);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (settings.IsFirstRun)
                {
                    var oobeVM = new OobeViewModel();
                    var oobeWindow = new OobeWindow { DataContext = oobeVM };
                    
                    desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel(), IsVisible = false };

                    oobeVM.FinishAction = () =>
                    {
                        oobeWindow.Close();
                        desktop.MainWindow.Show();
                        SetupTrayIcon(desktop);
                        StartBackgroundSync();
                        StartIslandWindow(); // 启动灵动岛
                    };

                    oobeWindow.Show();
                }
                else
                {
                    desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
                    SetupTrayIcon(desktop);
                    StartBackgroundSync();
                    StartIslandWindow(); // 启动灵动岛
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("主窗口初始化失败", ex);
            throw;
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private void StartIslandWindow()
    {
        try
        {
            if (_islandWindow != null) return;
            
            _islandWindow = new IslandWindow();
            _islandWindow.Show();
            Logger.Info("灵动岛启动成功");
        }
        catch (Exception ex)
        {
            Logger.Error("灵动岛启动失败", ex);
        }
    }

    private void StartBackgroundSync()
    {
        _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(15) };
        _syncTimer.Tick += async (s, e) =>
        {
            var settings = AppSettings.Load();
            if (string.IsNullOrEmpty(settings.WebDavUrl) || string.IsNullOrEmpty(settings.GetSyncEncryptPass())) return;
            if (SyncStatusService.Instance.IsSyncing) return;

            SyncStatusService.Instance.IsSyncing = true;
            SyncStatusService.Instance.StatusText = "正在同步...";
            Logger.Info("触发后台同步");

            try
            {
                var syncService = new SyncService(settings.WebDavUrl, settings.WebDavUser, settings.GetWebDavPass(), settings.GetSyncEncryptPass());
                var events = await EventRepo.GetAllAsync();
                await syncService.PushEventsAsync(events.ToList());
                
                SyncStatusService.Instance.StatusText = "同步成功";
                SyncStatusService.Instance.HasError = false;
                Logger.Info("后台同步成功");
            }
            catch (Exception ex)
            {
                SyncStatusService.Instance.StatusText = "同步失败";
                SyncStatusService.Instance.HasError = true;
                Logger.Error("后台同步失败", ex);
            }
            finally
            {
                SyncStatusService.Instance.IsSyncing = false;
            }
        };
        _syncTimer.Start();
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            if (_trayIcon != null) return;

            WindowIcon icon;
            try
            {
                var iconStream = AssetLoader.Open(new Uri("avares://PlanPeople.App/Assets/app.ico"));
                icon = new WindowIcon(iconStream);
            }
            catch (Exception ex)
            {
                Logger.Error("加载 app.ico 失败，已使用透明占位图标", ex);
                var bitmap = new Avalonia.Media.Imaging.WriteableBitmap(
                    new Avalonia.PixelSize(16, 16), 
                    new Avalonia.Vector(96, 96), 
                    Avalonia.Platform.PixelFormat.Bgra8888, 
                    Avalonia.Platform.AlphaFormat.Unpremul);
                icon = new WindowIcon(bitmap);
            }

            _trayIcon = new TrayIcon
            {
                Icon = icon,
                ToolTipText = "PlanPeople 日程助手",
                IsVisible = true
            };

            _trayIcon.Clicked += (s, e) =>
            {
                if (desktop.MainWindow != null)
                {
                    desktop.MainWindow.Show();
                    desktop.MainWindow.WindowState = WindowState.Normal;
                    desktop.MainWindow.Activate();
                }
            };

            var menu = new NativeMenu();
            var openItem = new NativeMenuItem("打开主窗口");
            openItem.Click += (s, e) => { desktop.MainWindow?.Show(); desktop.MainWindow!.WindowState = WindowState.Normal; };

            var exitItem = new NativeMenuItem("退出");
            exitItem.Click += (s, e) => 
            { 
                _syncTimer?.Stop();
                _islandWindow?.Close();
                _trayIcon?.Dispose(); 
                desktop.Shutdown(); 
                Logger.Info("应用正常退出");
            };

            menu.Items.Add(openItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exitItem);
            _trayIcon.Menu = menu;
            
            Logger.Info("托盘图标初始化成功");
        }
        catch (Exception ex)
        {
            Logger.Error("托盘初始化致命失败", ex);
        }
    }
}