using System;
using System.Collections.Generic;

namespace PlanPeople.Core.Models;

public sealed class CalendarEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    
    // 提醒（提前分钟数）
    public int Reminder { get; set; } = 15;
    
    // 重复规则 (RRULE)
    public string? Recurrence { get; set; }
    
    public string Status { get; set; } = "confirmed";
    public string? Url { get; set; }
    public List<string> Categories { get; set; } = new();
    
    // 新增：优先级 (0=无, 1=低, 2=中, 3=高)
    public int Priority { get; set; } = 0; 
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    public string Source { get; set; } = "local";
    public string? IcsRaw { get; set; }
    public bool Deleted { get; set; }
}