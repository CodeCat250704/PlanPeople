using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace PlanPeople.App.Services;

public static class ToastService
{
    public static async Task ShowAsync(string message)
    {
        var window = new Window
        {
            Width = 300,
            Height = 60,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true
        };

        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#333333")),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(15, 10),
            BoxShadow = BoxShadows.Parse("0 5 15 0 #66000000"),
            Child = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14
            }
        };

        window.Content = border;

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var mainWindow = desktop.MainWindow;
            // 强制转换为 int
            window.Position = new Avalonia.PixelPoint(
                (int)(mainWindow.Position.X + (mainWindow.Bounds.Width - window.Width) / 2),
                (int)(mainWindow.Position.Y + mainWindow.Bounds.Height - window.Height - 50)
            );
        }

        window.Show();
        await Task.Delay(2000);
        window.Close();
    }
}