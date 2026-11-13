using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.App.Services;

namespace PlanPeople.App.ViewModels;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly AppSettings _settings = AppSettings.Load();

    [ObservableProperty] private string _userName;
    [ObservableProperty] private string _userInitial;

    public SettingsViewModel Settings { get; }

    public ProfileViewModel(SettingsViewModel settingsVM)
    {
        Settings = settingsVM;
        UserName = string.IsNullOrEmpty(_settings.UserName) ? "小伙伴" : _settings.UserName;
        UserInitial = UserName.Substring(0, 1).ToUpper();
    }

    // 监听昵称输入，实时保存
    partial void OnUserNameChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _settings.UserName = value;
            _settings.Save();
            UserInitial = value.Substring(0, 1).ToUpper();
        }
    }
}