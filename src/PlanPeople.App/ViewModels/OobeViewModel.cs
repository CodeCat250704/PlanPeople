using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.App.Services;
using PlanPeople.AI;

namespace PlanPeople.App.ViewModels;

public partial class OobeViewModel : ViewModelBase
{
    private readonly AppSettings _settings = AppSettings.Load();

    [ObservableProperty] private int _currentStep = 0; 
    [ObservableProperty] private bool _isStep0 = true;
    [ObservableProperty] private bool _isStep1;
    [ObservableProperty] private bool _isStep2;
    [ObservableProperty] private bool _isStep3;

    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private int _selectedThemeIndex;
    [ObservableProperty] private string _aiBaseUrl = "";
    [ObservableProperty] private string _aiModel = "";
    [ObservableProperty] private string _aiApiKey = "";
    [ObservableProperty] private string _statusText = "";

    public Action? FinishAction { get; set; }

    public OobeViewModel()
    {
        SelectedThemeIndex = _settings.Theme switch { "light" => 0, "dark" => 1, "ice" => 2, "spruce" => 3, _ => 0 };
        AiBaseUrl = _settings.AiBaseUrl;
        AiModel = _settings.AiModel;
        if (!string.IsNullOrEmpty(_settings.AiApiKeyEnc)) AiApiKey = "********";
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        string theme = value switch { 0 => "light", 1 => "dark", 2 => "ice", 3 => "spruce", _ => "light" };
        ThemeService.SetTheme(theme);
    }

    partial void OnCurrentStepChanged(int value)
    {
        IsStep0 = value == 0;
        IsStep1 = value == 1;
        IsStep2 = value == 2;
        IsStep3 = value == 3;
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep < 3) CurrentStep++;
    }

    [RelayCommand]
    private void PrevStep()
    {
        if (CurrentStep > 0) CurrentStep--;
    }

    [RelayCommand]
    private async Task TestAiAsync()
    {
        StatusText = "测试中...";
        var key = AiApiKey == "********" ? _settings.GetAiApiKey() : AiApiKey;
        
        if (string.IsNullOrEmpty(key))
        {
            StatusText = "请输入 API Key";
            return;
        }

        var client = new OpenAiClient();
        var reply = await client.SendMessageAsync(AiBaseUrl, key, AiModel, "test", "hi");
        
        if (reply.StartsWith("[调用失败]")) StatusText = $"失败: {reply}";
        else StatusText = "连接成功！";
    }

    [RelayCommand]
    private void Finish()
    {
        _settings.UserName = string.IsNullOrWhiteSpace(UserName) ? "小伙伴" : UserName.Trim();
        _settings.Theme = SelectedThemeIndex switch { 0 => "light", 1 => "dark", 2 => "ice", 3 => "spruce", _ => "light" };
        _settings.AiBaseUrl = AiBaseUrl;
        _settings.AiModel = AiModel;
        if (AiApiKey != "********") _settings.SetAiApiKey(AiApiKey);
        
        _settings.IsFirstRun = false;
        _settings.Save();

        FinishAction?.Invoke();
    }
}