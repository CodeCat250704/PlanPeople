using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace PlanPeople.App.Services;

public static class DialogService
{
    public static async Task ShowMessageAsync(string title, string message)
    {
        var window = new Window
        {
            Title = title,
            Width = 350,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            SystemDecorations = SystemDecorations.None,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            Background = Brushes.Transparent
        };

        var closeButton = new Button
        {
            Content = "确定",
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Brushes.DodgerBlue,
            Foreground = Brushes.White,
            Padding = new Avalonia.Thickness(20, 8),
            CornerRadius = new Avalonia.CornerRadius(4),
            Margin = new Avalonia.Thickness(0, 15, 0, 0)
        };

        closeButton.Click += (s, e) => window.Close();

        window.Content = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(20),
            BoxShadow = BoxShadows.Parse("0 10 30 0 #44000000"),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White },
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightGray },
                    closeButton
                }
            }
        };

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        else
        {
            window.Show();
        }
    }
}