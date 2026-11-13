using System;

namespace PlanPeople.Core.Models;

public class Thread
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "新对话";
    public string Category { get; set; } = "default";
    public bool Archived { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}