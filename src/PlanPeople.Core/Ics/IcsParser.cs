using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PlanPeople.Core.Models;

namespace PlanPeople.Core.Ics;

public static class IcsParser
{
    public static List<CalendarEvent> Parse(string icsContent)
    {
        var events = new List<CalendarEvent>();
        if (string.IsNullOrWhiteSpace(icsContent)) return events;

        // 1. 统一换行符并按行分割
        var lines = icsContent.Replace("\r\n", "\n").Split('\n');
        
        CalendarEvent? currentEvent = null;
        bool inAlarm = false;
        int reminderMinutes = 15;
        
        // 用于处理 ICS 折行（以空格或制表符开头的行）
        StringBuilder? foldBuffer = null;

        foreach (var rawLine in lines)
        {
            // 2. 处理折行
            if (rawLine.StartsWith(" ") || rawLine.StartsWith("\t"))
            {
                if (foldBuffer != null && currentEvent != null)
                {
                    // 将当前行去掉首部空格后追加到上一行
                    foldBuffer.Append(rawLine.TrimStart());
                    continue;
                }
            }
            else if (foldBuffer != null && currentEvent != null)
            {
                // 如果不是折行，说明上一行已结束，处理 foldBuffer 的内容
                ProcessLine(foldBuffer.ToString(), currentEvent, ref inAlarm, ref reminderMinutes);
                foldBuffer = null;
            }

            var line = rawLine.TrimEnd();
            if (string.IsNullOrEmpty(line)) continue;

            // 3. 状态机解析
            if (line == "BEGIN:VEVENT")
            {
                currentEvent = new CalendarEvent { Source = "ics" };
                reminderMinutes = 15;
            }
            else if (line == "END:VEVENT")
            {
                if (currentEvent != null)
                {
                    currentEvent.Reminder = reminderMinutes;
                    events.Add(currentEvent);
                    currentEvent = null;
                }
            }
            else if (line == "BEGIN:VALARM")
            {
                inAlarm = true;
            }
            else if (line == "END:VALARM")
            {
                inAlarm = false;
            }
            else if (currentEvent != null)
            {
                // 检查这一行是否可能是一个长行，如果是，开始 buffer
                if (line.Length > 0 && !line.Contains(":") && foldBuffer == null)
                {
                    // 没有冒号，说明可能是折行的开头，或者格式错误，跳过
                    continue;
                }
                
                // 正常行（包含冒号）直接处理
                if (line.Contains(":"))
                {
                    // 如果这一行已经是完整行，且没有折行缓冲，直接处理
                    if (foldBuffer == null)
                    {
                        ProcessLine(line, currentEvent, ref inAlarm, ref reminderMinutes);
                    }
                    else
                    {
                        // 有缓冲未处理完，先处理缓冲，再处理新行
                        ProcessLine(foldBuffer.ToString(), currentEvent, ref inAlarm, ref reminderMinutes);
                        foldBuffer = null;
                        ProcessLine(line, currentEvent, ref inAlarm, ref reminderMinutes);
                    }
                }
                else
                {
                    // 开始折行缓冲
                    foldBuffer = new StringBuilder(line);
                }
            }
        }

        return events;
    }

    private static void ProcessLine(string line, CalendarEvent currentEvent, ref bool inAlarm, ref int reminderMinutes)
    {
        var parts = line.Split(new[] { ':' }, 2);
        if (parts.Length < 2) return;
        
        var key = parts[0].ToUpperInvariant();
        var value = parts[1];

        if (key == "UID") currentEvent.Id = value;
        else if (key == "SUMMARY") currentEvent.Title = IcsHelper.Unescape(value);
        else if (key == "DTSTART") currentEvent.Start = ParseDateTime(value);
        else if (key == "DTEND") currentEvent.End = ParseDateTime(value);
        else if (key == "DESCRIPTION") currentEvent.Description = IcsHelper.Unescape(value);
        else if (key == "LOCATION") currentEvent.Location = IcsHelper.Unescape(value);
        else if (key == "RRULE") currentEvent.Recurrence = value;
        else if (key == "URL") currentEvent.Url = value;
        else if (key == "CATEGORIES") currentEvent.Categories.AddRange(IcsHelper.Unescape(value).Split(','));
        else if (key == "TRIGGER" && inAlarm)
        {
            if (value.StartsWith("-PT") && value.EndsWith("M"))
            {
                if (int.TryParse(value.Substring(3, value.Length - 4), out int mins))
                {
                    reminderMinutes = mins;
                }
            }
        }
    }

    private static DateTimeOffset ParseDateTime(string value)
    {
        // 处理带 TZID 的情况，例如：TZID=Asia/Shanghai:20250101T100000
        if (value.Contains(';'))
        {
            value = value.Split(';')[1];
            if (value.Contains(':')) value = value.Split(':')[1];
        }
        else if (value.StartsWith("TZID="))
        {
            var colonIndex = value.IndexOf(':');
            if (colonIndex >= 0) value = value.Substring(colonIndex + 1);
        }

        // 1. 处理标准 UTC 格式（以 Z 结尾，长度 16，如 20250101T020000Z）
        if (value.EndsWith("Z") && value.Length >= 15)
        {
            var raw = value.Substring(0, 15);
            if (DateTime.TryParseExact(raw, "yyyyMMddTHHmmss", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out var dt))
            {
                return new DateTimeOffset(dt, TimeSpan.Zero).ToLocalTime();
            }
        }

        // 2. 处理不带 Z 的格式（如 20250101T100000），按本地时区解析
        if (value.Length >= 15 && value.Contains("T") && !value.Contains("-"))
        {
            var raw = value.Substring(0, 15);
            if (DateTime.TryParseExact(raw, "yyyyMMddTHHmmss", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out var dt))
            {
                return new DateTimeOffset(dt, TimeZoneInfo.Local.GetUtcOffset(dt));
            }
        }

        // 3. 处理普通时间格式（如 2025-01-01 10:00:00）
        if (DateTimeOffset.TryParse(value, out var result))
        {
            return result;
        }

        // 4. 兜底：解析失败返回当前时间
        return DateTimeOffset.Now;
    }
}