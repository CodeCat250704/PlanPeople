using CommunityToolkit.Mvvm.ComponentModel;

namespace PlanPeople.App.Services;

public partial class SyncStatusService : ObservableObject
{
    public static SyncStatusService Instance { get; } = new();

    [ObservableProperty] private bool _isSyncing;
    [ObservableProperty] private string _statusText = "已就绪";
    [ObservableProperty] private bool _hasError;
}