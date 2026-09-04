using Microsoft.Data.Sqlite;
using HttpsMessenger.Models;

namespace HttpsMessenger.Services;

/// <summary>
/// SQLite 数据访问：请求/响应字段规则与通讯日志。
/// </summary>
public sealed class SqliteStore
{
    private readonly string _connectionString;

    public string DatabasePath { get; }

    public SqliteStore(string? databasePath = null)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HttpsMessenger");
        Directory.CreateDirectory(dir);
        DatabasePath = databasePath ?? Path.Combine(dir, "https_messenger.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        Initialize();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void Initialize()
    {
        using var conn = Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS FieldRules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    RequestField TEXT NOT NULL,
                    MatchMode TEXT NOT NULL DEFAULT 'Equals',
                    RequestValue TEXT NOT NULL DEFAULT '',
                    ResponseField TEXT NOT NULL,
                    ResponseValue TEXT NOT NULL,
                    HttpStatus INTEGER NOT NULL DEFAULT 200,
                    ResponseMessage TEXT NOT NULL DEFAULT 'OK',
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    Remark TEXT NOT NULL DEFAULT '',
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS CommunicationLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Direction TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    ProcessText TEXT NOT NULL,
                    RequestBody TEXT NOT NULL DEFAULT '',
                    ResponseBody TEXT NOT NULL DEFAULT '',
                    Success INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }

        using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(1) FROM FieldRules;";
            var count = Convert.ToInt64(countCmd.ExecuteScalar());
            if (count == 0)
            {
                SeedDefaults(conn);
            }
        }
    }

    private static void SeedDefaults(SqliteConnection conn)
    {
        var seeds = new[]
        {
            new FieldRule
            {
                Name = "问候语规则",
                RequestField = "content",
                MatchMode = "Equals",
                RequestValue = "你好",
                ResponseField = "reply",
                ResponseValue = "你好！我是 HttpsMessenger 服务端。",
                ResponseMessage = "问候已处理",
                Remark = "当 content 等于“你好”时返回问候"
            },
            new FieldRule
            {
                Name = "帮助规则",
                RequestField = "content",
                MatchMode = "Contains",
                RequestValue = "帮助",
                ResponseField = "help",
                ResponseValue = "可用命令：你好 / 时间 / 状态 / ping",
                ResponseMessage = "帮助信息",
                Remark = "content 包含“帮助”时返回帮助文本"
            },
            new FieldRule
            {
                Name = "时间查询",
                RequestField = "content",
                MatchMode = "Equals",
                RequestValue = "时间",
                ResponseField = "currentTime",
                ResponseValue = "{now}",
                ResponseMessage = "返回服务器时间占位符",
                Remark = "ResponseValue={now} 会在匹配时替换为当前时间"
            },
            new FieldRule
            {
                Name = "类型 ping",
                RequestField = "type",
                MatchMode = "Equals",
                RequestValue = "ping",
                ResponseField = "status",
                ResponseValue = "alive",
                ResponseMessage = "类型 ping 探测成功",
                Remark = "当 type=ping 时返回 status=alive"
            },
            new FieldRule
            {
                Name = "自定义字段 echo",
                RequestField = "requestField",
                MatchMode = "Equals",
                RequestValue = "demoKey",
                ResponseField = "demoKey",
                ResponseValue = "demoValue-FromServer",
                ResponseMessage = "自定义字段已回显",
                Remark = "请求 requestField=demoKey 时，响应写入 demoKey"
            },
            new FieldRule
            {
                Name = "任意发送者欢迎",
                RequestField = "sender",
                MatchMode = "Any",
                RequestValue = "",
                ResponseField = "welcome",
                ResponseValue = "欢迎通过 HTTPS 进行安全通讯",
                HttpStatus = 200,
                ResponseMessage = "欢迎语",
                Remark = "只要存在 sender 字段即附加 welcome"
            }
        };

        foreach (var rule in seeds)
        {
            InsertRule(conn, rule);
        }
    }

    public List<FieldRule> GetAllRules()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT Id, Name, RequestField, MatchMode, RequestValue, ResponseField, ResponseValue,
                   HttpStatus, ResponseMessage, IsEnabled, Remark, CreatedAt, UpdatedAt
            FROM FieldRules
            ORDER BY Id;
            """;

        var list = new List<FieldRule>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadRule(reader));
        }

        return list;
    }

    public List<FieldRule> GetEnabledRules()
        => GetAllRules().Where(r => r.IsEnabled).ToList();

    public FieldRule? GetRule(long id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT Id, Name, RequestField, MatchMode, RequestValue, ResponseField, ResponseValue,
                   HttpStatus, ResponseMessage, IsEnabled, Remark, CreatedAt, UpdatedAt
            FROM FieldRules WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadRule(reader) : null;
    }

    public long AddRule(FieldRule rule)
    {
        using var conn = Open();
        return InsertRule(conn, rule);
    }

    private static long InsertRule(SqliteConnection conn, FieldRule rule)
    {
        rule.CreatedAt = DateTime.Now;
        rule.UpdatedAt = DateTime.Now;
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO FieldRules
            (Name, RequestField, MatchMode, RequestValue, ResponseField, ResponseValue,
             HttpStatus, ResponseMessage, IsEnabled, Remark, CreatedAt, UpdatedAt)
            VALUES
            ($name, $rf, $mode, $rv, $resf, $resv, $status, $msg, $enabled, $remark, $c, $u);
            SELECT last_insert_rowid();
            """;
        BindRule(cmd, rule);
        return (long)(cmd.ExecuteScalar() ?? 0L);
    }

    public void UpdateRule(FieldRule rule)
    {
        rule.UpdatedAt = DateTime.Now;
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            UPDATE FieldRules SET
                Name=$name, RequestField=$rf, MatchMode=$mode, RequestValue=$rv,
                ResponseField=$resf, ResponseValue=$resv, HttpStatus=$status,
                ResponseMessage=$msg, IsEnabled=$enabled, Remark=$remark, UpdatedAt=$u
            WHERE Id=$id;
            """;
        BindRule(cmd, rule);
        cmd.Parameters.AddWithValue("$id", rule.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteRule(long id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM FieldRules WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void SaveCommunicationLog(CommunicationTrace trace)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO CommunicationLogs
            (Direction, Title, ProcessText, RequestBody, ResponseBody, Success, CreatedAt)
            VALUES ($d, $t, $p, $req, $res, $ok, $c);
            """;
        cmd.Parameters.AddWithValue("$d", trace.Direction);
        cmd.Parameters.AddWithValue("$t", trace.Title);
        cmd.Parameters.AddWithValue("$p", trace.FormatProcessText());
        cmd.Parameters.AddWithValue("$req", trace.RequestBody ?? "");
        cmd.Parameters.AddWithValue("$res", trace.ResponseBody ?? "");
        cmd.Parameters.AddWithValue("$ok", trace.Success ? 1 : 0);
        cmd.Parameters.AddWithValue("$c", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public List<(long Id, string Direction, string Title, bool Success, DateTime CreatedAt, string ProcessText)> GetRecentLogs(int take = 50)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT Id, Direction, Title, Success, CreatedAt, ProcessText
            FROM CommunicationLogs
            ORDER BY Id DESC
            LIMIT $take;
            """;
        cmd.Parameters.AddWithValue("$take", take);

        var list = new List<(long, string, string, bool, DateTime, string)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            _ = DateTime.TryParse(reader.GetString(4), out var dt);
            list.Add((
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt64(3) == 1,
                dt,
                reader.GetString(5)));
        }

        return list;
    }

    public void ClearLogs()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM CommunicationLogs;";
        cmd.ExecuteNonQuery();
    }

    private static void BindRule(SqliteCommand cmd, FieldRule rule)
    {
        cmd.Parameters.AddWithValue("$name", rule.Name);
        cmd.Parameters.AddWithValue("$rf", rule.RequestField);
        cmd.Parameters.AddWithValue("$mode", rule.MatchMode);
        cmd.Parameters.AddWithValue("$rv", rule.RequestValue);
        cmd.Parameters.AddWithValue("$resf", rule.ResponseField);
        cmd.Parameters.AddWithValue("$resv", rule.ResponseValue);
        cmd.Parameters.AddWithValue("$status", rule.HttpStatus);
        cmd.Parameters.AddWithValue("$msg", rule.ResponseMessage);
        cmd.Parameters.AddWithValue("$enabled", rule.IsEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$remark", rule.Remark);
        cmd.Parameters.AddWithValue("$c", rule.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$u", rule.UpdatedAt.ToString("o"));
    }

    private static FieldRule ReadRule(SqliteDataReader reader)
    {
        _ = DateTime.TryParse(reader.GetString(11), out var created);
        _ = DateTime.TryParse(reader.GetString(12), out var updated);
        return new FieldRule
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            RequestField = reader.GetString(2),
            MatchMode = reader.GetString(3),
            RequestValue = reader.GetString(4),
            ResponseField = reader.GetString(5),
            ResponseValue = reader.GetString(6),
            HttpStatus = reader.GetInt32(7),
            ResponseMessage = reader.GetString(8),
            IsEnabled = reader.GetInt64(9) == 1,
            Remark = reader.GetString(10),
            CreatedAt = created,
            UpdatedAt = updated
        };
    }
}
