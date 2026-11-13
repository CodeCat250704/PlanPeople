using Avalonia.Controls;
using Avalonia.Interactivity;
using PlanPeople.App.ViewModels;
using PlanPeople.Core.Models;

namespace PlanPeople.App.Views;

public partial class WeekView : UserControl
{
    public WeekView() => InitializeComponent();

    private void EditEventMenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: CalendarEvent ev } && DataContext is WeekViewModel vm)
        {
            vm.EditEventCommand.Execute(ev);
        }
    }

    private async void DeleteEventMenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: CalendarEvent ev } && DataContext is WeekViewModel vm)
        {
            await vm.DeleteEventAsync(ev);
        }
    }
}