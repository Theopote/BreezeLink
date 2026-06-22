using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// sing-box 进程管理器
/// 负责启动、停止、重载 sing-box 子进程，并捕获其输出
/// </summary>
public class SingBoxProcessManager : IProxyProcessManager
{
    private readonly ILogger<SingBoxProcessManager> _logger;
    private Process? _singBoxProcess;
    private readonly StringBuilder _logBuffer = new();
    private readonly object _logLock = new();
    private readonly string _singBoxPath;
    private readonly string _configPath;

    public event EventHandler<string>? OnLogReceived;

    public SingBoxProcessManager(ILogger<SingBoxProcessManager> logger)
    {
        _logger = logger;
        _singBoxPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sing-box", "sing-box.exe");
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs", "config.json");

        // 确保目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
    }

    /// <summary>
    /// 启动代理进程
    /// </summary>
    public async Task<bool> StartProxyAsync()
    {
        try
        {
            await StartAsync();
            return IsRunning;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start proxy");
            return false;
        }
    }

    /// <summary>
    /// 停止代理进程
    /// </summary>
    public async Task<bool> StopProxyAsync()
    {
        try
        {
            await StopAsync();
            return !IsRunning;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop proxy");
            return false;
        }
    }

    /// <summary>
    /// 重启代理进程
    /// </summary>
    public async Task<bool> RestartProxyAsync()
    {
        try
        {
            var wasRunning = IsRunning;
            if (wasRunning)
            {
                await StopAsync();
                await Task.Delay(1000);
            }
            await StartAsync();
            return IsRunning;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart proxy");
            return false;
        }
    }

    /// <summary>
    /// 检查代理进程是否运行
    /// </summary>
    public bool IsProxyRunning()
    {
        return IsRunning;
    }

    private bool IsRunning => _singBoxProcess != null && !_singBoxProcess.HasExited;

    /// <summary>
    /// 启动 sing-box 进程
    /// </summary>
    public async Task StartAsync(string? configContent = null)
    {
        try
        {
            // 如果提供了配置内容，写入配置文件
            if (!string.IsNullOrEmpty(configContent))
            {
                await File.WriteAllTextAsync(_configPath, configContent);
                _logger.LogInformation("Configuration updated: {ConfigPath}", _configPath);
            }

            // 检查 sing-box 是否存在
            if (!File.Exists(_singBoxPath))
            {
                throw new FileNotFoundException($"sing-box executable not found at {_singBoxPath}");
            }

            // 启动进程
            var startInfo = new ProcessStartInfo
            {
                FileName = _singBoxPath,
                Arguments = $"run -c \"{_configPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            _singBoxProcess = Process.Start(startInfo);
            if (_singBoxProcess == null)
            {
                throw new Exception("Failed to start sing-box process");
            }

            // 设置输出处理
            _singBoxProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var logMessage = $"[STDOUT] {e.Data}";
                    AppendToLog(logMessage);
                    _logger.LogInformation(logMessage);
                }
            };

            _singBoxProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var logMessage = $"[STDERR] {e.Data}";
                    AppendToLog(logMessage);
                    _logger.LogError(logMessage);
                }
            };

            _singBoxProcess.BeginOutputReadLine();
            _singBoxProcess.BeginErrorReadLine();

            // 处理进程退出
            _singBoxProcess.Exited += (sender, e) =>
            {
                var exitCode = _singBoxProcess.ExitCode;
                var logMessage = $"sing-box process exited with code {exitCode}";
                AppendToLog(logMessage);
                _logger.LogWarning(logMessage);
                _singBoxProcess = null;
            };

            // 等待进程启动
            await Task.Delay(2000);

            if (IsRunning)
            {
                AppendToLog("sing-box started successfully");
                _logger.LogInformation("sing-box started successfully");
            }
            else
            {
                throw new Exception("sing-box failed to start");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start sing-box");
            AppendToLog($"Error starting sing-box: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 停止 sing-box 进程
    /// </summary>
    public async Task StopAsync()
    {
        if (_singBoxProcess != null && !_singBoxProcess.HasExited)
        {
            try
            {
                // 优雅地停止进程
                if (!_singBoxProcess.CloseMainWindow())
                {
                    // 如果无法优雅关闭，强制终止
                    _singBoxProcess.Kill();
                }

                // 等待进程退出
                var timeout = TimeSpan.FromSeconds(10);
                var startTime = DateTime.Now;

                while (!_singBoxProcess.HasExited && DateTime.Now - startTime < timeout)
                {
                    await Task.Delay(100);
                }

                if (!_singBoxProcess.HasExited)
                {
                    _singBoxProcess.Kill(true);
                }

                AppendToLog("sing-box stopped");
                _logger.LogInformation("sing-box stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping sing-box");
                AppendToLog($"Error stopping sing-box: {ex.Message}");
            }
            finally
            {
                _singBoxProcess = null;
            }
        }
    }

    /// <summary>
    /// 重载 sing-box 配置
    /// </summary>
    public async Task ReloadAsync(string configContent)
    {
        var wasRunning = IsRunning;
        if (wasRunning)
        {
            await StopAsync();
            await Task.Delay(1000); // 等待进程完全停止
        }

        await StartAsync(configContent);

        if (wasRunning && !IsRunning)
        {
            throw new Exception("Failed to reload sing-box configuration");
        }
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public ProxyStatus GetStatus()
    {
        if (_singBoxProcess == null)
        {
            return ProxyStatus.Stopped;
        }

        if (_singBoxProcess.HasExited)
        {
            return ProxyStatus.Error;
        }

        return ProxyStatus.Running;
    }

    /// <summary>
    /// 获取日志内容
    /// </summary>
    public string GetLogs(int lastLines = 100)
    {
        lock (_logLock)
        {
            var logs = _logBuffer.ToString();
            var lines = logs.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length <= lastLines)
            {
                return logs;
            }

            return string.Join('\n', lines.Skip(lines.Length - lastLines));
        }
    }

    /// <summary>
    /// 清空日志
    /// </summary>
    public void ClearLogs()
    {
        lock (_logLock)
        {
            _logBuffer.Clear();
        }
    }

    /// <summary>
    /// 更新代理配置
    /// </summary>
    public async Task<bool> UpdateConfigurationAsync()
    {
        try
        {
            if (IsRunning)
            {
                await ReloadAsync(File.ReadAllText(_configPath));
                return true;
            }
            else
            {
                await StartAsync();
                return IsRunning;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration");
            return false;
        }
    }

    /// <summary>
    /// 获取进程ID
    /// </summary>
    public int? ProcessId => _singBoxProcess?.Id;

    private void AppendToLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var formattedMessage = $"[{timestamp}] {message}";

        lock (_logLock)
        {
            _logBuffer.AppendLine(formattedMessage);
        }

        OnLogReceived?.Invoke(this, formattedMessage);
    }
}

/// <summary>
/// 代理进程状态枚举
/// </summary>
public enum ProxyStatus
{
    Stopped,
    Running,
    Error
}

/// <summary>
/// 代理进程管理器接口
/// </summary>
public interface IProxyProcessManager
{
    Task<bool> StartProxyAsync();
    Task<bool> StopProxyAsync();
    Task<bool> RestartProxyAsync();
    bool IsProxyRunning();
    Task<bool> UpdateConfigurationAsync();
    event EventHandler<string>? OnLogReceived;
}
