using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.Core.Models;

namespace PlanPeople.App.ViewModels;

public partial class WeekViewModel : ViewModelBase
{
    [ObservableProperty] private DateTime _startOfWeek = DateTime.Today;
    [ObservableProperty] private string _weekTitle = "";
    public ObservableCollection<DayGroup> Days { get; } = new();
    
    public Func<CalendarEvent?, Task>? RequestEditEvent { get; set; }

    public WeekViewModel() => UpdateTitle();

    [RelayCommand]
    private void PreviousWeek()
    {
        StartOfWeek = StartOfWeek.AddDays(-7);
        UpdateTitle();
        _ = LoadEventsAsync();
    }

    [RelayCommand]
    private void NextWeek()
    {
        StartOfWeek = StartOfWeek.AddDays(7);
        UpdateTitle();
        _ = LoadEventsAsync();
    }

    public async Task LoadEventsAsync()
    {
        Days.Clear();
        var all = await App.EventRepo.GetAllAsync();

        for (int i = 0; i < 7; i++)
        {
            var currentDay = StartOfWeek.AddDays(i);
            var nextDay = currentDay.AddDays(1);

            var dayEvents = all.Where(e => e.Start.LocalDateTime < nextDay && e.End.LocalDateTime > currentDay)
                               .OrderBy(e => e.Priority == 2 ? 0 : e.Priority == 1 ? 1 : 2)
                               .ThenBy(e => e.Start)
                               .ToList();

            Days.Add(new DayGroup
            {
                Date = currentDay,
                DateText = currentDay.ToString("MM月dd日 dddd"),
                IsToday = currentDay.Date == DateTime.Today,
                Events = new ObservableCollection<CalendarEvent>(dayEvents)
            });
        }
    }

    private void UpdateTitle() => WeekTitle = $"{StartOfWeek:MM月dd日} - {StartOfWeek.AddDays(6):MM月dd日}";

    [RelayCommand]
    private async Task EditEventAsync(CalendarEvent ev)
    {
        if (RequestEditEvent != null) await RequestEditEvent.Invoke(ev);
    }

    public async Task DeleteEventAsync(CalendarEvent ev)
    {
        await App.EventRepo.DeleteAsync(ev.Id);
        await LoadEventsAsync();
    }
}

public class DayGroup
{
    public DateTime Date { get; set; }
    public string DateText { get; set; } = "";
    public bool IsToday { get; set; }
    public ObservableCollection<CalendarEvent> Events { get; set; } = new();
}