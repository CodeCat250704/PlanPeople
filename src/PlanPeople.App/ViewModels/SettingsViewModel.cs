using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.App.Localization;
using PlanPeople.App.Services;
using PlanPeople.AI;
using PlanPeople.Sync;
using QRCoder;
using PlanPeople.Core.Models;

namespace PlanPeople.App.ViewModels;

[SupportedOSPlatform("windows")]
public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings = AppSettings.Load();

    [ObservableProperty] private int _currentCategoryIndex = 0;
    [ObservableProperty] private int _selectedThemeIndex;
    [ObservableProperty] private int _selectedLanguageIndex;
    
    // 灵动岛配置
    [ObservableProperty] private int _islandStyleIndex;
    [ObservableProperty] private double _islandWidth;
    [ObservableProperty] private double _islandHeight;

    // AI 配置
    [ObservableProperty] private string _aiBaseUrl = "";
    [ObservableProperty] private string _aiModel = "";
    [ObservableProperty] private string _aiApiKeyInput = "";
    [ObservableProperty] private string _aiStatus = "";

    // WebDAV 同步配置
    [ObservableProperty] private string _webDavUrl = "";
    [ObservableProperty] private string _webDavUser = "";
    [ObservableProperty] private string _webDavPassInput = "";
    [ObservableProperty] private string _syncPassInput = "";
    [ObservableProperty] private string _syncStatus = "";
    [ObservableProperty] private string _syncTestResult = "";

    // 系统与设备
    [ObservableProperty] private bool _runAtLogin;
    [ObservableProperty] private bool _closeToTray;
    [ObservableProperty] private Bitmap? _qrCodeImage;
    [ObservableProperty] private string _dataStatus = "";

    public SettingsViewModel()
    {
        // 加载配置
        SelectedThemeIndex = _settings.Theme switch { "light" => 0, "dark" => 1, "ice" => 2, "spruce" => 3, _ => 0 };
        SelectedLanguageIndex = _settings.Language switch { "zh-CN" => 0, "zh-TW" => 1, "en" => 2, "ja" => 3, _ => 0 };

        IslandStyleIndex = _settings.IslandStyle == "TopBar" ? 1 : 0;
        IslandWidth = _settings.IslandWidth;
        IslandHeight = _settings.IslandHeight;

        AiBaseUrl = _settings.AiBaseUrl;
        AiModel = _settings.AiModel;
        if (!string.IsNullOrEmpty(_settings.AiApiKeyEnc)) AiApiKeyInput = "********";

        WebDavUrl = _settings.WebDavUrl;
        WebDavUser = _settings.WebDavUser;
        if (!string.IsNullOrEmpty(_settings.WebDavPassEnc)) WebDavPassInput = "********";
        if (!string.IsNullOrEmpty(_settings.SyncEncryptPassEnc)) SyncPassInput = "********";

        RunAtLogin = _settings.RunAtLogin;
        CloseToTray = _settings.CloseToTray;
    }

    // ====== 主题与语言 ======
    partial void OnSelectedThemeIndexChanged(int value)
    {
        string theme = value switch { 0 => "light", 1 => "dark", 2 => "ice", 3 => "spruce", _ => "light" };
        ThemeService.SetTheme(theme);
        _settings.Theme = theme;
        _settings.Save();
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        string lang = value switch { 0 => "zh-CN", 1 => "zh-TW", 2 => "en", 3 => "ja", _ => "zh-CN" };
        LocalizationService.Instance.ChangeLanguage(lang);
        _settings.Language = lang;
        _settings.Save();
    }

    // ====== 灵动岛实时生效 ======
    partial void OnIslandStyleIndexChanged(int value)
    {
        _settings.IslandStyle = value == 0 ? "Island" : "TopBar";
        _settings.Save();
        PlanPeople.App.Views.IslandWindow.Instance?.ApplySettings();
    }

    partial void OnIslandWidthChanged(double value)
    {
        _settings.IslandWidth = value;
        _settings.Save();
        PlanPeople.App.Views.IslandWindow.Instance?.ApplySettings();
    }

    partial void OnIslandHeightChanged(double value)
    {
        _settings.IslandHeight = value;
        _settings.Save();
        PlanPeople.App.Views.IslandWindow.Instance?.ApplySettings();
    }

    // ====== 开机自启与托盘 ======
    partial void OnRunAtLoginChanged(bool value) { StartupService.SetStartup(value); _settings.RunAtLogin = value; _settings.Save(); }
    partial void OnCloseToTrayChanged(bool value) { _settings.CloseToTray = value; _settings.Save(); }

    // ====== AI 动作 ======
    [RelayCommand]
    private async Task SaveAiConfigAsync()
    {
        _settings.AiBaseUrl = AiBaseUrl;
        _settings.AiModel = AiModel;
        if (AiApiKeyInput != "********") _settings.SetAiApiKey(AiApiKeyInput);
        _settings.Save();
        AiStatus = "保存成功";
        await ToastService.ShowAsync("AI 配置已保存");
    }

    [RelayCommand]
    private async Task TestAiConnectionAsync()
    {
        AiStatus = "测试中...";
        var settings = AppSettings.Load();
        var key = AiApiKeyInput == "********" ? settings.GetAiApiKey() : AiApiKeyInput;

        if (string.IsNullOrEmpty(key)) { AiStatus = "请先输入 API Key"; return; }

        var client = new OpenAiClient();
        var reply = await client.SendMessageAsync(AiBaseUrl, key, AiModel, "You are a test.", "Hello");
        
        if (reply.StartsWith("[调用失败]")) { AiStatus = "连接失败"; await DialogService.ShowMessageAsync("AI 连接失败", reply); }
        else { AiStatus = "测试连接成功"; await DialogService.ShowMessageAsync("成功", "AI 服务配置正确，连接成功！"); }
    }

    // ====== WebDAV 动作 ======
    [RelayCommand]
    private void SaveSyncConfig()
    {
        _settings.WebDavUrl = WebDavUrl;
        _settings.WebDavUser = WebDavUser;
        if (WebDavPassInput != "********") _settings.SetWebDavPass(WebDavPassInput);
        if (SyncPassInput != "********") _settings.SetSyncEncryptPass(SyncPassInput);
        _settings.Save();
        SyncStatus = "同步配置保存成功";
    }

    [RelayCommand]
    private async Task TestSyncPushAsync()
    {
        SyncTestResult = "正在推送...";
        var settings = AppSettings.Load();
        var pass = settings.GetWebDavPass();
        var encPass = settings.GetSyncEncryptPass();
        
        if (string.IsNullOrEmpty(settings.WebDavUrl) || string.IsNullOrEmpty(encPass)) { SyncTestResult = "请先填写 WebDAV 地址和加密口令并保存"; return; }

        var syncService = new SyncService(settings.WebDavUrl, settings.WebDavUser, pass, encPass);
        var events = await App.EventRepo.GetAllAsync();
        var success = await syncService.PushEventsAsync(events.ToList());
        SyncTestResult = success ? "推送成功" : "推送失败";
        if (!success) await DialogService.ShowMessageAsync("同步失败", "推送失败，请检查账号、密码或网络状态。");
    }

    [RelayCommand]
    private async Task TestSyncPullAsync()
    {
        SyncTestResult = "正在拉取...";
        var settings = AppSettings.Load();
        var pass = settings.GetWebDavPass();
        var encPass = settings.GetSyncEncryptPass();
        
        if (string.IsNullOrEmpty(settings.WebDavUrl) || string.IsNullOrEmpty(encPass)) { SyncTestResult = "请先填写 WebDAV 地址和加密口令并保存"; return; }

        var syncService = new SyncService(settings.WebDavUrl, settings.WebDavUser, pass, encPass);
        var pulledEvents = await syncService.PullEventsAsync();
        
        if (pulledEvents.Count > 0)
        {
            foreach (var ev in pulledEvents)
            {
                var existing = await App.EventRepo.GetByIdAsync(ev.Id);
                if (existing == null) await App.EventRepo.AddAsync(ev);
                else await App.EventRepo.UpdateAsync(ev);
            }
            SyncTestResult = $"拉取成功，共 {pulledEvents.Count} 条数据";
        }
        else SyncTestResult = "拉取成功，但未找到数据（或解密失败）";
    }

    // ====== 二维码与备份 ======
    [RelayCommand]
    private void GenerateQrCode()
    {
        try
        {
            var config = new { v = 1, webdavUrl = WebDavUrl, webdavUser = WebDavUser, aiBaseUrl = AiBaseUrl, aiModel = AiModel, syncPath = "/PlanPeople/", cipher = "AES-256-GCM" };
            var json = JsonSerializer.Serialize(config);
            var base64Str = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(base64Str, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var pngBytes = qrCode.GetGraphic(10);

            using var ms = new MemoryStream(pngBytes);
            QrCodeImage = new Bitmap(ms);
            SyncTestResult = "二维码生成成功";
        }
        catch (Exception ex) { SyncTestResult = $"生成失败: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        try
        {
            var events = await App.EventRepo.GetAllAsync();
            var notes = await App.NoteRepo.GetAllAsync();
            var backup = new { Version = 1, ExportTime = DateTimeOffset.Now, Events = events, Notes = notes };
            var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
            
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var defaultPath = Path.Combine(desktop, $"PlanPeople_Backup_{DateTime.Now:yyyyMMddHHmmss}.json");
            await File.WriteAllTextAsync(defaultPath, json);
            DataStatus = $"备份已导出到桌面: {Path.GetFileName(defaultPath)}";
            await DialogService.ShowMessageAsync("导出成功", $"备份文件已保存至：\n{defaultPath}");
        }
        catch (Exception ex) { DataStatus = "导出失败"; await DialogService.ShowMessageAsync("导出失败", ex.Message); }
    }

    [RelayCommand]
    private async Task ImportBackupAsync()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var files = Directory.GetFiles(desktop, "PlanPeople_Backup_*.json").OrderByDescending(f => f).ToList();
            
            if (files.Count == 0) { await DialogService.ShowMessageAsync("导入失败", "桌面上未找到任何备份文件"); return; }

            var json = await File.ReadAllTextAsync(files[0]);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            int eventCount = 0, noteCount = 0;

            if (root.TryGetProperty("Events", out var eventsArray))
            {
                foreach (var item in eventsArray.EnumerateArray())
                {
                    var ev = JsonSerializer.Deserialize<CalendarEvent>(item.GetRawText());
                    if (ev != null)
                    {
                        if (await App.EventRepo.GetByIdAsync(ev.Id) == null) await App.EventRepo.AddAsync(ev);
                        else await App.EventRepo.UpdateAsync(ev);
                        eventCount++;
                    }
                }
            }

            if (root.TryGetProperty("Notes", out var notesArray))
            {
                foreach (var item in notesArray.EnumerateArray())
                {
                    var note = JsonSerializer.Deserialize<Note>(item.GetRawText());
                    if (note != null)
                    {
                        if (await App.NoteRepo.GetByIdAsync(note.Id) == null) await App.NoteRepo.AddAsync(note);
                        else await App.NoteRepo.UpdateAsync(note);
                        noteCount++;
                    }
                }
            }
            DataStatus = $"导入完成: {eventCount} 条日程, {noteCount} 篇文档";
            await DialogService.ShowMessageAsync("导入成功", $"成功恢复 {eventCount} 条日程和 {noteCount} 篇文档！");
        }
        catch (Exception ex) { DataStatus = "导入失败"; await DialogService.ShowMessageAsync("导入失败", ex.Message); }
    }
}