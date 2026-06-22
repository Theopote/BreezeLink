using Microsoft.Extensions.Logging;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 文件日志服务
/// 将日志写入文件系统，支持日志轮转
/// </summary>
public class FileLoggerService : ILogger, IDisposable
{
    private readonly string _logDirectory;
    private readonly string _logFileName;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private readonly int _maxFileSize = 10 * 1024 * 1024; // 10MB
    private readonly int _maxFiles = 5;

    public FileLoggerService()
    {
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        _logFileName = $"BreezeLink-{DateTime.Now:yyyy-MM-dd}.log";

        Directory.CreateDirectory(_logDirectory);
        InitializeLogFile();
    }

    private void InitializeLogFile()
    {
        var logPath = Path.Combine(_logDirectory, _logFileName);

        // 检查是否需要轮转
        if (File.Exists(logPath) && new FileInfo(logPath).Length > _maxFileSize)
        {
            RotateLogFiles();
        }

        _writer = new StreamWriter(logPath, append: true, Encoding.UTF8)
        {
            AutoFlush = true
        };
    }

    private void RotateLogFiles()
    {
        lock (_lock)
        {
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }

            // 删除最旧的日志文件
            var logFiles = Directory.GetFiles(_logDirectory, "BreezeLink-*.log")
                .OrderByDescending(File.GetLastWriteTime)
                .ToList();

            if (logFiles.Count >= _maxFiles)
            {
                for (int i = _maxFiles - 1; i < logFiles.Count; i++)
                {
                    try
                    {
                        File.Delete(logFiles[i]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete old log file {logFiles[i]}: {ex.Message}");
                    }
                }
            }

            // 重命名当前文件
            var currentLogPath = Path.Combine(_logDirectory, _logFileName);
            if (File.Exists(currentLogPath))
            {
                var newName = $"BreezeLink-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log";
                var newPath = Path.Combine(_logDirectory, newName);
                File.Move(currentLogPath, newPath);
            }
        }
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{logLevel}] {message}";

        if (exception != null)
        {
            logEntry += $"\nException: {exception}";
        }

        WriteToFile(logEntry);
    }

    private void WriteToFile(string message)
    {
        try
        {
            lock (_lock)
            {
                if (_writer == null)
                {
                    InitializeLogFile();
                }

                _writer?.WriteLine(message);

                // 检查是否需要轮转
                if (_writer != null && _writer.BaseStream.Length > _maxFileSize)
                {
                    RotateLogFiles();
                    InitializeLogFile();
                }
            }
        }
        catch (Exception ex)
        {
            // 避免递归调用
            Console.WriteLine($"Failed to write to log file: {ex.Message}");
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return new LogScope();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private class LogScope : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// 文件日志提供器
/// </summary>
public class FileLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLoggerService();
    }

    public void Dispose()
    {
        // 由 DI 容器管理
    }
}
