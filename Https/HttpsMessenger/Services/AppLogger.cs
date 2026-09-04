namespace HttpsMessenger.Services;

/// <summary>
/// 应用运行日志：内存缓冲 + 按日滚动文件。
/// </summary>
public sealed class AppLogger
{
    private readonly object _sync = new();
    private readonly List<LogEntry> _entries = new();
    private readonly string _logDirectory;
    private readonly int _maxMemoryEntries;

    public string LogDirectory => _logDirectory;

    public event Action<LogEntry>? EntryAdded;

    public AppLogger(string? logDirectory = null, int maxMemoryEntries = 2000)
    {
        _logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HttpsMessenger",
            "logs");
        Directory.CreateDirectory(_logDirectory);
        _maxMemoryEntries = Math.Max(100, maxMemoryEntries);
    }

    public string TodayLogPath => Path.Combine(_logDirectory, $"HttpsMessenger_{DateTime.Now:yyyyMMdd}.log");

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);
    public void Debug(string message) => Write("DEBUG", message);

    public void Write(string level, string message)
    {
        var entry = new LogEntry
        {
            Time = DateTime.Now,
            Level = level,
            Message = message ?? string.Empty
        };

        lock (_sync)
        {
            _entries.Add(entry);
            if (_entries.Count > _maxMemoryEntries)
            {
                _entries.RemoveRange(0, _entries.Count - _maxMemoryEntries);
            }

            try
            {
                File.AppendAllText(
                    TodayLogPath,
                    entry.ToLine() + Environment.NewLine,
                    System.Text.Encoding.UTF8);
            }
            catch
            {
                // 写文件失败不影响主流程
            }
        }

        EntryAdded?.Invoke(entry);
    }

    public IReadOnlyList<LogEntry> GetEntries(string? levelFilter = null)
    {
        lock (_sync)
        {
            IEnumerable<LogEntry> query = _entries;
            if (!string.IsNullOrWhiteSpace(levelFilter)
                && !string.Equals(levelFilter, "全部", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(levelFilter, "ALL", StringComparison.OrdinalIgnoreCase)
                && !levelFilter.Contains("全部", StringComparison.OrdinalIgnoreCase)
                && !levelFilter.StartsWith("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(e =>
                    string.Equals(e.Level, levelFilter, StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
        }
    }

    public void ClearMemory()
    {
        lock (_sync)
        {
            _entries.Clear();
        }
    }

    public void OpenLogFolder()
    {
        Directory.CreateDirectory(_logDirectory);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _logDirectory,
            UseShellExecute = true
        });
    }
}

public sealed class LogEntry
{
    public DateTime Time { get; set; }
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = string.Empty;

    public string ToLine() => $"[{Time:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] {Message}";

    public Color GetColor() => Level.ToUpperInvariant() switch
    {
        "ERROR" => Color.Firebrick,
        "WARN" => Color.DarkOrange,
        "DEBUG" => Color.Gray,
        _ => Color.DimGray
    };
}
