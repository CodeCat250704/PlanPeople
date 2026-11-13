using System;
using System.IO;
using System.Reflection;

namespace PlanPeople.Data.Migrations;

public static class MigrationRunner
{
    public static void Run(SqliteConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        
        // 检查当前 user_version
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "PRAGMA user_version;";
        var currentVersion = Convert.ToInt32(checkCmd.ExecuteScalar());

        // 如果版本是 0，执行初始化脚本
        if (currentVersion < 1)
        {
            // 由于单文件发布下 Assembly.Location 可能为空，这里为了简单直接内嵌 SQL
            // 或者使用 AvaloniaResource 的方式读取
            var schemaSql = @"
                CREATE TABLE IF NOT EXISTS events (
                    id TEXT PRIMARY KEY, title TEXT NOT NULL, start TEXT NOT NULL, end TEXT NOT NULL,
                    description TEXT, location TEXT, reminder INTEGER DEFAULT 15, recurrence TEXT,
                    status TEXT DEFAULT 'confirmed', url TEXT, categories TEXT,
                    created_at TEXT NOT NULL, updated_at TEXT NOT NULL, source TEXT DEFAULT 'local',
                    ics_raw TEXT, deleted INTEGER DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_events_start ON events(start);
                CREATE INDEX IF NOT EXISTS idx_events_updated_at ON events(updated_at);
                CREATE INDEX IF NOT EXISTS idx_events_deleted ON events(deleted);

                CREATE TABLE IF NOT EXISTS notes (
                    id TEXT PRIMARY KEY, title TEXT NOT NULL, content TEXT, tags TEXT,
                    archived INTEGER DEFAULT 0, source TEXT DEFAULT 'local',
                    created_at TEXT NOT NULL, updated_at TEXT NOT NULL, deleted INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS threads (
                    id TEXT PRIMARY KEY, title TEXT NOT NULL, category TEXT DEFAULT 'default',
                    archived INTEGER DEFAULT 0, created_at TEXT NOT NULL, updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS messages (
                    id TEXT PRIMARY KEY, thread_id TEXT NOT NULL, role TEXT NOT NULL, content TEXT NOT NULL,
                    created_at TEXT NOT NULL, updated_at TEXT NOT NULL,
                    FOREIGN KEY(thread_id) REFERENCES threads(id) ON DELETE CASCADE
                );
            ";

            using var cmd = conn.CreateCommand();
            cmd.CommandText = schemaSql;
            cmd.ExecuteNonQuery();

            // 更新版本号
            using var versionCmd = conn.CreateCommand();
            versionCmd.CommandText = "PRAGMA user_version = 1;";
            versionCmd.ExecuteNonQuery();
        }
    }
}