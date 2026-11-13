using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlanPeople.Core.Interfaces;
using PlanPeople.Core.Models;

namespace PlanPeople.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly SqliteConnectionFactory _factory;

    public EventRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<CalendarEvent>> GetAllAsync()
    {
        var events = new List<CalendarEvent>();
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM events WHERE deleted = 0 ORDER BY start ASC;";
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            events.Add(ReadEvent(reader));
        }
        return events;
    }

    public async Task<CalendarEvent?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM events WHERE id = $id AND deleted = 0 LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadEvent(reader);
        }
        return null;
    }

    public async Task AddAsync(CalendarEvent e)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO events (id, title, start, end, description, location, reminder, recurrence, status, url, categories, created_at, updated_at, source, ics_raw, deleted)
            VALUES ($id, $title, $start, $end, $desc, $loc, $rem, $rec, $status, $url, $cat, $created, $updated, $source, $raw, 0);";
        
        BindEventParams(cmd, e);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(CalendarEvent e)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE events SET 
                title = $title, start = $start, end = $end, description = $desc, 
                location = $loc, reminder = $rem, recurrence = $rec, status = $status, 
                url = $url, categories = $cat, updated_at = $updated, source = $source, ics_raw = $raw
            WHERE id = $id;";
        
        BindEventParams(cmd, e);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE events SET deleted = 1, updated_at = $now WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    private static void BindEventParams(Microsoft.Data.Sqlite.SqliteCommand cmd, CalendarEvent e)
    {
        cmd.Parameters.AddWithValue("$id", e.Id);
        cmd.Parameters.AddWithValue("$title", e.Title);
        cmd.Parameters.AddWithValue("$start", e.Start.ToUniversalTime().ToString("O"));
        cmd.Parameters.AddWithValue("$end", e.End.ToUniversalTime().ToString("O"));
        cmd.Parameters.AddWithValue("$desc", (object?)e.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$loc", (object?)e.Location ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$rem", e.Reminder);
        cmd.Parameters.AddWithValue("$rec", (object?)e.Recurrence ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$status", e.Status);
        cmd.Parameters.AddWithValue("$url", (object?)e.Url ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cat", JsonSerializer.Serialize(e.Categories));
        cmd.Parameters.AddWithValue("$created", e.CreatedAt.ToUniversalTime().ToString("O"));
        cmd.Parameters.AddWithValue("$updated", e.UpdatedAt.ToUniversalTime().ToString("O"));
        cmd.Parameters.AddWithValue("$source", e.Source);
        cmd.Parameters.AddWithValue("$raw", (object?)e.IcsRaw ?? DBNull.Value);
    }

    private static CalendarEvent ReadEvent(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new CalendarEvent
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Start = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("start"))).ToLocalTime(),
            End = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("end"))).ToLocalTime(),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location")),
            Reminder = reader.GetInt32(reader.GetOrdinal("reminder")),
            Recurrence = reader.IsDBNull(reader.GetOrdinal("recurrence")) ? null : reader.GetString(reader.GetOrdinal("recurrence")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            Url = reader.IsDBNull(reader.GetOrdinal("url")) ? null : reader.GetString(reader.GetOrdinal("url")),
            Categories = JsonSerializer.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("categories"))) ?? new List<string>(),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_at"))).ToLocalTime(),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("updated_at"))).ToLocalTime(),
            Source = reader.GetString(reader.GetOrdinal("source")),
            IcsRaw = reader.IsDBNull(reader.GetOrdinal("ics_raw")) ? null : reader.GetString(reader.GetOrdinal("ics_raw"))
        };
    }
}