using Microsoft.Data.Sqlite;
using HttpsMessenger.Models.Mes;

namespace HttpsMessenger.Services.Mes;

/// <summary>
/// 离线补传队列：网络失败时入队，恢复后补传。
/// </summary>
public sealed class MesOfflineStore
{
    private readonly string _connectionString;

    public MesOfflineStore(string? dbPath = null)
    {
        var path = dbPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HttpsMessenger",
            "mes_offline.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        Initialize();
    }

    private void Initialize()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS OfflineQueue (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ApiName TEXT NOT NULL,
                JsonData TEXT NOT NULL,
                ProductSn TEXT NOT NULL DEFAULT '',
                MoNumber TEXT NOT NULL DEFAULT '',
                Status TEXT NOT NULL DEFAULT 'Pending',
                RetryCount INTEGER NOT NULL DEFAULT 0,
                LastError TEXT NOT NULL DEFAULT '',
                ResponseRaw TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                UploadedAt TEXT NOT NULL DEFAULT ''
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public long Enqueue(string apiName, string jsonData, string productSn = "", string moNumber = "")
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO OfflineQueue (ApiName, JsonData, ProductSn, MoNumber, Status, CreatedAt, UpdatedAt)
            VALUES ($api, $json, $sn, $mo, 'Pending', $c, $u);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$api", apiName);
        cmd.Parameters.AddWithValue("$json", jsonData);
        cmd.Parameters.AddWithValue("$sn", productSn ?? "");
        cmd.Parameters.AddWithValue("$mo", moNumber ?? "");
        cmd.Parameters.AddWithValue("$c", DateTime.Now.ToString("o"));
        cmd.Parameters.AddWithValue("$u", DateTime.Now.ToString("o"));
        return (long)(cmd.ExecuteScalar() ?? 0L);
    }

    public List<MesOfflineItem> GetPending(int take = 100)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT Id, ApiName, JsonData, ProductSn, MoNumber, Status, RetryCount, LastError, CreatedAt
            FROM OfflineQueue WHERE Status IN ('Pending','Failed') ORDER BY Id LIMIT $take;
            """;
        cmd.Parameters.AddWithValue("$take", take);
        var list = new List<MesOfflineItem>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new MesOfflineItem
            {
                Id = r.GetInt64(0),
                ApiName = r.GetString(1),
                JsonData = r.GetString(2),
                ProductSn = r.GetString(3),
                MoNumber = r.GetString(4),
                Status = r.GetString(5),
                RetryCount = r.GetInt32(6),
                LastError = r.GetString(7),
                CreatedAt = DateTime.TryParse(r.GetString(8), out var dt) ? dt : DateTime.MinValue
            });
        }

        return list;
    }

    public void MarkUploaded(long id, string responseRaw)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            UPDATE OfflineQueue SET Status='Uploaded', ResponseRaw=$resp, UploadedAt=$u, UpdatedAt=$u
            WHERE Id=$id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$resp", responseRaw);
        cmd.Parameters.AddWithValue("$u", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void MarkFailed(long id, string error, int retryCount)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            UPDATE OfflineQueue SET Status='Failed', LastError=$err, RetryCount=$rc, UpdatedAt=$u WHERE Id=$id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$err", error);
        cmd.Parameters.AddWithValue("$rc", retryCount);
        cmd.Parameters.AddWithValue("$u", DateTime.Now.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM OfflineQueue WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public int CountPending()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM OfflineQueue WHERE Status IN ('Pending','Failed');";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private SqliteConnection Open()
    {
        var c = new SqliteConnection(_connectionString);
        c.Open();
        return c;
    }
}

public sealed class MesOfflineItem
{
    public long Id { get; set; }
    public string ApiName { get; set; } = "";
    public string JsonData { get; set; } = "";
    public string ProductSn { get; set; } = "";
    public string MoNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public int RetryCount { get; set; }
    public string LastError { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
