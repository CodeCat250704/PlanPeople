using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using PlanPeople.App.ViewModels;
using PlanPeople.Core.Models;
using Thread = PlanPeople.Core.Models.Thread;

namespace PlanPeople.App.Views;

public partial class AiChatView : UserControl
{
    public static readonly IValueConverter IsUserConverter = new FuncConverter();

    public AiChatView()
    {
        InitializeComponent();
    }

    private void ThreadSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem is Thread t && DataContext is AiChatViewModel vm)
        {
            vm.SelectThreadCommand.Execute(t);
        }
    }

    private async void AddScheduleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Message msg } && DataContext is AiChatViewModel vm)
        {
            await vm.AddToScheduleAsync(msg);
        }
    }
}

public class FuncConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value?.ToString() == "user";
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}