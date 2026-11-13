using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PlanPeople.Core.Interfaces;
using PlanPeople.Core.Models;

namespace PlanPeople.Data.Repositories;

public class NoteRepository : INoteRepository
{
    private readonly SqliteConnectionFactory _factory;

    public NoteRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Note>> GetAllAsync()
    {
        var notes = new List<Note>();
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM notes WHERE deleted = 0 ORDER BY updated_at DESC;";
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            notes.Add(ReadNote(reader));
        }
        return notes;
    }

    public async Task<Note?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM notes WHERE id = $id AND deleted = 0 LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadNote(reader);
        }
        return null;
    }

    public async Task AddAsync(Note note)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO notes (id, title, content, tags, archived, source, created_at, updated_at, deleted)
            VALUES ($id, $title, $content, $tags, $archived, $source, $created, $updated, 0);";
        
        BindNoteParams(cmd, note);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Note note)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE notes SET 
                title = $title, content = $content, tags = $tags, 
                archived = $archived, updated_at = $updated
            WHERE id = $id;";
        
        BindNoteParams(cmd, note);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notes SET deleted = 1, updated_at = $now WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    private static void BindNoteParams(SqliteCommand cmd, Note n)
    {
        cmd.Parameters.AddWithValue("$id", n.Id);
        cmd.Parameters.AddWithValue("$title", n.Title);
        cmd.Parameters.AddWithValue("$content", (object?)n.Content ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tags", JsonSerializer.Serialize(n.Tags));
        cmd.Parameters.AddWithValue("$archived", n.Archived ? 1 : 0);
        cmd.Parameters.AddWithValue("$source", n.Source);
        cmd.Parameters.AddWithValue("$created", n.CreatedAt.ToUniversalTime().ToString("O"));
        cmd.Parameters.AddWithValue("$updated", n.UpdatedAt.ToUniversalTime().ToString("O"));
    }

    private static Note ReadNote(SqliteDataReader reader)
    {
        return new Note
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            Content = reader.IsDBNull(reader.GetOrdinal("content")) ? "" : reader.GetString(reader.GetOrdinal("content")),
            Tags = JsonSerializer.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("tags"))) ?? new List<string>(),
            Archived = reader.GetInt32(reader.GetOrdinal("archived")) == 1,
            Source = reader.GetString(reader.GetOrdinal("source")),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_at"))).ToLocalTime(),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("updated_at"))).ToLocalTime()
        };
    }
}