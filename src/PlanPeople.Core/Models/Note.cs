using System;
using System.Collections.Generic;

namespace PlanPeople.Core.Models;

public sealed class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public bool Archived { get; set; }
    public string Source { get; set; } = "local";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    public bool Deleted { get; set; }
}