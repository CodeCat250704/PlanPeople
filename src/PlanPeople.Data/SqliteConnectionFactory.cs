using Microsoft.Data.Sqlite;
using System.IO;

namespace PlanPeople.Data;

public class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory()
    {
        // 数据库保存在 用户本地应用数据 目录下
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(appData, "PlanPeople");
        if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);

        var dbPath = Path.Combine(appDir, "planpeople.db");
        
        // 启用 WAL 模式和共享缓存
        _connectionString = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared;";
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}