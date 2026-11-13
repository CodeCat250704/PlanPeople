using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PlanPeople.App.Services;
using PlanPeople.App.ViewModels;

namespace PlanPeople.App.Views;

public partial class IslandWindow : Window
{
    public static IslandWindow? Instance { get; private set; }
    private bool _isHovered;

    public IslandWindow()
    {
        InitializeComponent();
        Instance = this;
        DataContext = new IslandViewModel();
        
        // 初始应用尺寸和位置
        ApplySettings();

        // 鼠标移入 -> 扩展变大
        PointerEntered += (s, e) => 
        {
            if (!_isHovered) { _isHovered = true; ApplyHoverState(); }
        };
        
        // 鼠标移出 -> 收缩复原
        PointerExited += (s, e) => 
        {
            if (_isHovered) { _isHovered = false; ApplyHoverState(); }
        };
    }

    public void ApplySettings()
    {
        ApplyHoverState();
    }

    private void ApplyHoverState()
    {
        var settings = AppSettings.Load();
        
        // 根据是否悬停决定窗口尺寸
        if (_isHovered)
        {
            // 悬停时展开的详情尺寸
            Width = 360;
            Height = 400;
        }
        else
        {
            // 默认的胶囊/顶部栏尺寸（从设置读取）
            Width = settings.IslandWidth;
            Height = settings.IslandHeight;
        }

        CenterOnScreen(settings.IslandStyle);
    }

    private void CenterOnScreen(string style)
    {
        var screen = Screens.Primary;
        if (screen == null) return;

        var workArea = screen.WorkingArea;
        
        int x, y;

        if (style == "TopBar")
        {
            // 顶部栏模式：强制拉伸为全屏宽度，贴紧屏幕顶部
            Width = workArea.Width;
            x = workArea.X;
            y = workArea.Y;
        }
        else
        {
            // 灵动岛模式：居中悬浮，距离屏幕顶部留出 10px
            x = (int)(workArea.X + (workArea.Width - Width) / 2);
            y = (int)(workArea.Y + 10);
        }
        
        Position = new PixelPoint(x, y);
    }
}