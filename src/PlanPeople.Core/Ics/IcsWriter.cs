using System.Text;
using PlanPeople.Core.Models;

namespace PlanPeople.Core.Ics;

public static class IcsWriter
{
    public static string GenerateIcs(List<CalendarEvent> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//PlanPeople//CN");
        
        foreach (var e in events)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{e.Id}");
            sb.AppendLine($"SUMMARY:{IcsHelper.Escape(e.Title)}");
            
            // 强制 UTC 时间
            sb.AppendLine($"DTSTART:{e.Start.ToUniversalTime():yyyyMMddTHHmmssZ}");
            sb.AppendLine($"DTEND:{e.End.ToUniversalTime():yyyyMMddTHHmmssZ}");
            
            if (!string.IsNullOrEmpty(e.Description))
                sb.AppendLine($"DESCRIPTION:{IcsHelper.Escape(e.Description)}");
            
            if (!string.IsNullOrEmpty(e.Location))
                sb.AppendLine($"LOCATION:{IcsHelper.Escape(e.Location)}");
            
            if (!string.IsNullOrEmpty(e.Recurrence))
                sb.AppendLine($"RRULE:{e.Recurrence}");
            
            if (!string.IsNullOrEmpty(e.Url))
                sb.AppendLine($"URL:{e.Url}");

            if (e.Categories.Count > 0)
                sb.AppendLine($"CATEGORIES:{IcsHelper.Escape(string.Join(",", e.Categories))}");

            // 提醒 VALARM
            if (e.Reminder > 0)
            {
                sb.AppendLine("BEGIN:VALARM");
                sb.AppendLine("ACTION:DISPLAY");
                sb.AppendLine($"DESCRIPTION:{IcsHelper.Escape(e.Title)}");
                sb.AppendLine($"TRIGGER:-PT{e.Reminder}M");
                sb.AppendLine("END:VALARM");
            }

            sb.AppendLine("END:VEVENT");
        }
        
        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }
}