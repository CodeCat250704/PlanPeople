using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.Core.Models;
using PlanPeople.App.Services;

namespace PlanPeople.App.ViewModels;

public partial class EventEditViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "";
    
    [ObservableProperty] private DateTimeOffset _startDate = DateTimeOffset.Now;
    [ObservableProperty] private TimeSpan _startTime = DateTime.Now.TimeOfDay;
    
    [ObservableProperty] private DateTimeOffset _endDate = DateTimeOffset.Now;
    [ObservableProperty] private TimeSpan _endTime = DateTime.Now.AddHours(1).TimeOfDay;

    [ObservableProperty] private string? _location;
    [ObservableProperty] private string? _description;
    
    // 优先级 (0=普通, 1=重要, 2=紧急)
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsHighPriority))]
    private int _priority;
    
    // 提醒 (UI 下拉框索引)
    [ObservableProperty] private int _reminderIndex = 2; 
    
    // 重复
    [ObservableProperty] private int _recurrenceIndex; 

    public bool IsHighPriority => Priority > 0;

    public CalendarEvent _originalEvent;
    public Action? CloseAction;

    public EventEditViewModel(CalendarEvent? ev)
    {
        if (ev == null)
        {
            _originalEvent = new CalendarEvent();
            StartDate = DateTimeOffset.Now.Date;
            StartTime = DateTime.Now.TimeOfDay;
            EndDate = DateTimeOffset.Now.Date;
            EndTime = DateTime.Now.AddHours(1).TimeOfDay;
            ReminderIndex = 2; // 默认 15 分钟
        }
        else
        {
            _originalEvent = ev;
            Title = ev.Title;
            StartDate = ev.Start.Date;
            StartTime = ev.Start.TimeOfDay;
            EndDate = ev.End.Date;
            EndTime = ev.End.TimeOfDay;
            Location = ev.Location;
            Description = ev.Description;
            Priority = ev.Priority;
            
            ReminderIndex = ev.Reminder switch { 0 => 0, 5 => 1, 15 => 2, 30 => 3, 60 => 4, _ => 2 };
            
            RecurrenceIndex = ev.Recurrence switch
            {
                "FREQ=DAILY" => 1,
                "FREQ=WEEKLY" => 2,
                "FREQ=MONTHLY" => 3,
                _ => 0
            };
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        _originalEvent.Title = Title;
        _originalEvent.Start = new DateTimeOffset(StartDate.Date + StartTime, DateTimeOffset.Now.Offset);
        _originalEvent.End = new DateTimeOffset(EndDate.Date + EndTime, DateTimeOffset.Now.Offset);
        _originalEvent.Location = Location;
        _originalEvent.Description = Description;
        _originalEvent.Priority = Priority;
        
        _originalEvent.Reminder = ReminderIndex switch { 0 => 0, 1 => 5, 2 => 15, 3 => 30, 4 => 60, _ => 15 };
        
        _originalEvent.Recurrence = RecurrenceIndex switch
        {
            1 => "FREQ=DAILY",
            2 => "FREQ=WEEKLY",
            3 => "FREQ=MONTHLY",
            _ => null
        };

        _originalEvent.UpdatedAt = DateTimeOffset.Now;

        if (string.IsNullOrEmpty(_originalEvent.Id) || !await ExistsInDbAsync(_originalEvent.Id))
            await App.EventRepo.AddAsync(_originalEvent);
        else
            await App.EventRepo.UpdateAsync(_originalEvent);

        CloseAction?.Invoke();
        await ToastService.ShowAsync("日程已保存");
    }

    // ... 保留 Delete/Cancel/Exists 等方法
    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (await ExistsInDbAsync(_originalEvent.Id))
        {
            await App.EventRepo.DeleteAsync(_originalEvent.Id);
        }
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseAction?.Invoke();

    private async Task<bool> ExistsInDbAsync(string id)
    {
        return await App.EventRepo.GetByIdAsync(id) != null;
    }
}