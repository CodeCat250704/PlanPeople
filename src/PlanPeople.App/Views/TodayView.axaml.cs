using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PlanPeople.App.ViewModels;
using PlanPeople.Core.Models;

namespace PlanPeople.App.Views;

public partial class TodayView : UserControl
{
    public TodayView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is TodayViewModel vm)
        {
            vm.RequestImportFile = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return null;
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "选择 ICS 文件",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("iCalendar") { Patterns = new[] { "*.ics" } } }
                });
                return files.Count > 0 ? files[0].Path.LocalPath : null;
            };
        }
    }

    private void EditEventMenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: CalendarEvent ev } && DataContext is TodayViewModel vm)
        {
            vm.EditEventCommand.Execute(ev);
        }
    }

    private async void DeleteEventMenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: CalendarEvent ev } && DataContext is TodayViewModel vm)
        {
            await vm.DeleteEventAsync(ev);
        }
    }
}