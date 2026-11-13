using System.ComponentModel;
using System.Globalization;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PlanPeople.App.Localization;

public partial class LocalizationService : ObservableObject
{
    public static LocalizationService Instance { get; } = new();

    private Dictionary<string, string> _currentDict = new();

    public string this[string key] => _currentDict.TryGetValue(key, out var val) ? val : key;

    private LocalizationService()
    {
        ChangeLanguage("zh-CN");
    }

    public void ChangeLanguage(string langCode)
    {
        var culture = new CultureInfo(langCode);
        CultureInfo.CurrentUICulture = culture;

        // 实际项目中应从 Assets/Localization/{langCode}.json 读取
        // 这里用硬编码演示
        _currentDict = langCode switch
        {
            "en" => new() { ["AppTitle"] = "PlanPeople", ["Today"] = "Today", ["Week"] = "Week", ["Settings"] = "Settings" },
            "ja" => new() { ["AppTitle"] = "PlanPeople", ["Today"] = "今日", ["Week"] = "週間", ["Settings"] = "設定" },
            "zh-TW" => new() { ["AppTitle"] = "PlanPeople", ["Today"] = "今日", ["Week"] = "週", ["Settings"] = "設定" },
            _ => new() { ["AppTitle"] = "PlanPeople", ["Today"] = "今日", ["Week"] = "周", ["Settings"] = "设置" }
        };

        OnPropertyChanged("Item[]");
    }
}
