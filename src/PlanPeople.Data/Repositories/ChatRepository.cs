using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PlanPeople.Core.Interfaces;
using PlanPeople.Core.Models;
using Thread = PlanPeople.Core.Models.Thread; // 解决 Thread 命名冲突

namespace PlanPeople.Data.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ChatRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Thread>> GetThreadsAsync()
    {
        var list = new List<Thread>();
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM threads ORDER BY updated_at DESC;";
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Thread
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                Category = reader.GetString(2),
                Archived = reader.GetInt32(3) == 1,
                CreatedAt = DateTimeOffset.Parse(reader.GetString(4)),
                UpdatedAt = DateTimeOffset.Parse(reader.GetString(5))
            });
        }
        return list;
    }

    public async Task AddThreadAsync(Thread thread)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO threads (id, title, category, archived, created_at, updated_at) VALUES ($id, $title, $cat, $arch, $created, $updated);";
        cmd.Parameters.AddWithValue("$id", thread.Id);
        cmd.Parameters.AddWithValue("$title", thread.Title);
        cmd.Parameters.AddWithValue("$cat", thread.Category);
        cmd.Parameters.AddWithValue("$arch", thread.Archived ? 1 : 0);
        cmd.Parameters.AddWithValue("$created", thread.CreatedAt.ToUniversalTime().ToString("O"));
        cmd.Parameters.AddWithValue("$updated", thread.UpdatedAt.ToUniversalTime().ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(string threadId)
    {
        var list = new List<Message>();
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM messages WHERE thread_id = $tid ORDER BY created_at ASC;";
        cmd.Parameters.AddWithValue("$tid", threadId);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Message
            {
                Id = reader.GetString(0),
                ThreadId = reader.GetString(1),
                Role = reader.GetString(2),
                Content = reader.GetString(3),
                CreatedAt = DateTimeOffset.Parse(reader.GetString(4)),
                UpdatedAt = DateTimeOffset.Parse(reader.GetString(5))
            });
        }
        return list;
    }

    public async Task AddMessageAsync(Message msg)
    {
        using var conn = _factory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO messages (id, thread_id, role, content, created_at, updated_at) VALUES ($id, $tid, $role, $content, $created, $updated);";
        cmd.Parameters.AddWithValue("$id", msg.Id);
        cmd.Parameters.AddWithValue("$tid", msg.ThreadId);
        cmd.Parameters.AddWithValue("$role", msg.Role);
        cmd.Parameters.AddWithValue("$content", msg.Content);
        cmd.Parameters.AddWithValue("$created", msg.CreatedAt.ToUniversalTime().ToString("O"));
        cmd.Parameters.AddWithValue("$updated", msg.UpdatedAt.ToUniversalTime().ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }
}