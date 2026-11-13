using System;
using Avalonia.Controls;
using Avalonia.Input;
using PlanPeople.App.ViewModels;

namespace PlanPeople.App.Views;

public partial class MainWindow : Window
{
    public MainWindow() 
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        var settings = PlanPeople.App.Services.AppSettings.Load();
        if (settings.CloseToTray)
        {
            e.Cancel = true;
            this.Hide();
        }
        base.OnClosing(e);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        var vm = DataContext as MainWindowViewModel;
        if (vm == null) return;

        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.N)
        {
            vm.TodayVM.AddEventCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.F)
        {
            if (vm.CurrentPage == vm.TodayVM)
            {
                // 此处由于 TodayView 在 DataTemplate 内部，直接 FindControl 可能有点绕
                // 最稳妥的做法是用 FocusManager，或者直接把文本框命名并在 TodayView 的代码后台处理
                var searchBox = this.FindControl<TextBox>("SearchBox"); // 如果 SearchBox 在 TodayView 内部，可能需要修改
                searchBox?.Focus();
                e.Handled = true;
            }
        }
    }
}