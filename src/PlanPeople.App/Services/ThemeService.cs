using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using System;

namespace PlanPeople.App.Services;

public static class ThemeService
{
    public static void SetTheme(string themeName)
    {
        if (Application.Current == null) return;

        // 1. 切换基础明暗模式（PleasantUI 控件依赖此属性变色）
        Application.Current.RequestedThemeVariant = (themeName == "dark" || themeName == "spruce") 
            ? ThemeVariant.Dark 
            : ThemeVariant.Light;

        // 2. 获取全局资源字典
        var res = Application.Current.Resources;

        // 3. 定义 4 套颜色方案
        var (bg, sidebar, card, text, subText, border) = themeName switch
        {
            "light" => ("#F3F3F3", "#EAEAEA", "#FFFFFF", "#1A1A1A", "#666666", "#E0E0E0"),
            "dark"  => ("#121212", "#1A1A1A", "#1E1E1E", "#FFFFFF", "#AAAAAA", "#333333"),
            "ice"   => ("#EAF3F8", "#D9E7F0", "#FFFFFF", "#1A2B3C", "#5A7B99", "#B0C9D9"),
            "spruce"=> ("#1C2A24", "#22332B", "#2A3F35", "#E8F0EC", "#90B3A2", "#3B5A4B"),
            _       => ("#F3F3F3", "#EAEAEA", "#FFFFFF", "#1A1A1A", "#666666", "#E0E0E0")
        };

        // 4. 动态替换资源（直接覆盖 Color，所有 SolidColorBrush 都会自动跟随刷新）
        res["ThemeBackgroundColor"] = Color.Parse(bg);
        res["ThemeSidebarColor"]    = Color.Parse(sidebar);
        res["ThemeCardColor"]       = Color.Parse(card);
        res["ThemeTextColor"]       = Color.Parse(text);
        res["ThemeSubTextColor"]    = Color.Parse(subText);
        res["ThemeBorderColor"]     = Color.Parse(border);
    }
}