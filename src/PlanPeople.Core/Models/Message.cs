using System;

namespace PlanPeople.Core.Models;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ThreadId { get; set; } = "";
    public string Role { get; set; } = "user"; 
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    // --- 新增：专门用于 UI 渲染的解析字段 ---
    public bool IsParsedEvent { get; set; } = false;
    public string? ParsedEventTitle { get; set; }
    public string? ParsedEventTime { get; set; }
    public string? ParsedEventLocation { get; set; }
    public string? ParsedEventDescription { get; set; }
    public string? ParsedEventRawJson { get; set; } // 隐藏的原始数据，供点击按钮时用
}