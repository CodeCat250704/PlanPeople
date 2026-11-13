using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlanPeople.AI;
using PlanPeople.App.Services;
using PlanPeople.Core.Models;
using Thread = PlanPeople.Core.Models.Thread;

namespace PlanPeople.App.ViewModels;

public partial class AiChatViewModel : ViewModelBase
{
    private readonly OpenAiClient _aiClient = new();
    private Thread? _currentThread;

    public ObservableCollection<Thread> Threads { get; } = new();
    public ObservableCollection<Message> Messages { get; } = new();

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private bool _isBusy;

    public AiChatViewModel() => _ = LoadThreadsAsync();

    private async Task LoadThreadsAsync()
    {
        Threads.Clear();
        foreach (var t in await App.ChatRepo.GetThreadsAsync()) Threads.Add(t);
    }

    [RelayCommand]
    private async Task NewChatAsync()
    {
        _currentThread = new Thread { Title = "新对话 " + DateTime.Now.ToString("MM-dd HH:mm") };
        await App.ChatRepo.AddThreadAsync(_currentThread);
        await LoadThreadsAsync();
        Messages.Clear();
    }

    [RelayCommand]
    private async Task SelectThreadAsync(Thread t)
    {
        if (t == null) return;
        _currentThread = t;
        Messages.Clear();
        foreach (var m in await App.ChatRepo.GetMessagesAsync(t.Id)) Messages.Add(m);
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsBusy) return;
        if (_currentThread == null) await NewChatAsync();

        var settings = AppSettings.Load();
        string apiKey = settings.GetAiApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            Messages.Add(new Message { ThreadId = _currentThread!.Id, Role = "assistant", Content = "请先在【设置】页面配置您的 API Key。" });
            return;
        }

        var userMsg = new Message { ThreadId = _currentThread!.Id, Role = "user", Content = InputText };
        await App.ChatRepo.AddMessageAsync(userMsg);
        Messages.Add(userMsg);
        
        string currentInput = InputText;
        InputText = "";
        IsBusy = true;

        // 插入正在思考的占位气泡
        var thinkingMsg = new Message { ThreadId = _currentThread.Id, Role = "assistant", Content = "正在思考..." };
        Messages.Add(thinkingMsg);

        var systemPrompt = $@"你是日程助手。当前时间：{DateTimeOffset.Now:O}。时区：Asia/Shanghai。
请分析用户输入，如果是创建日程，请严格输出 JSON 格式：
{{""title"":""日程标题"",""start"":""ISO8601时间"",""end"":""ISO8601时间"",""location"":""地点"",""description"":""描述""}}。
不要使用 markdown 代码块包裹，不要输出其他多余的解释文字。
如果不是日程相关的指令，直接用中文简短且拟人化地回复即可。";

        try
        {
            var aiResponse = await _aiClient.SendMessageAsync(settings.AiBaseUrl, apiKey, settings.AiModel, systemPrompt, currentInput);
            
            // 将 JSON 转化为人类能看懂的文本
            var aiMsg = ParseAiResponse(aiResponse, _currentThread.Id);
            
            Messages.Remove(thinkingMsg);
            await App.ChatRepo.AddMessageAsync(aiMsg);
            Messages.Add(aiMsg);
        }
        catch (Exception ex)
        {
            Messages.Remove(thinkingMsg);
            Messages.Add(new Message { ThreadId = _currentThread.Id, Role = "assistant", Content = $"调用失败: {ex.Message}" });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Message ParseAiResponse(string rawResponse, string threadId)
    {
        var msg = new Message { ThreadId = threadId, Role = "assistant", Content = rawResponse };

        try
        {
            var cleanJson = rawResponse.Trim();
            if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
            if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
            cleanJson = cleanJson.Trim();

            if (cleanJson.StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("title", out var title) && root.TryGetProperty("start", out var start))
                {
                    var s = DateTimeOffset.Parse(start.GetString()!);
                    var e = DateTimeOffset.Parse(root.GetProperty("end").GetString()!);
                    
                    msg.IsParsedEvent = true;
                    msg.ParsedEventTitle = title.GetString();
                    msg.ParsedEventTime = $"{s:MM月dd日 HH:mm} - {e:HH:mm}";
                    msg.ParsedEventLocation = root.TryGetProperty("location", out var loc) ? loc.GetString() : null;
                    msg.ParsedEventRawJson = cleanJson;

                    // 伪装成极其自然的人类回复
                    msg.Content = $"好的，我已经为您提取到日程信息：\n{msg.ParsedEventTitle}\n{msg.ParsedEventTime}\n{msg.ParsedEventLocation}";
                }
            }
        }
        catch { /* 解析失败，保留原始文本 */ }

        return msg;
    }

    [RelayCommand]
    public async Task AddToScheduleAsync(Message msg)
    {
        if (!msg.IsParsedEvent || string.IsNullOrEmpty(msg.ParsedEventRawJson)) return;

        try
        {
            using var doc = JsonDocument.Parse(msg.ParsedEventRawJson);
            var root = doc.RootElement;
            
            var ev = new CalendarEvent
            {
                Title = root.GetProperty("title").GetString() ?? "未命名日程",
                Start = DateTimeOffset.Parse(root.GetProperty("start").GetString()!),
                End = DateTimeOffset.Parse(root.GetProperty("end").GetString()!),
                Location = root.TryGetProperty("location", out var loc) ? loc.GetString() : null,
                Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Source = "ai"
            };

            await App.EventRepo.AddAsync(ev);
            
            // 成功反馈
            msg.Content = msg.Content + "\n\n已成功加入日历。";
            msg.IsParsedEvent = false; // 隐藏按钮
        }
        catch { }
    }

    public static bool IsUser(string role) => role == "user";
}