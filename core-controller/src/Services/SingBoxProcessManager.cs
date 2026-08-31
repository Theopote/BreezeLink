using System.Diagnostics;
using System.Collections.Generic;
using BreezeLink.CoreController.Models;
using Microsoft.Extensions.Logging;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// sing-box 进程管理器。保证同一时刻只有一个内核进程，日志有上限。
/// </summary>
public class SingBoxProcessManager : IProxyProcessManager, IDisposable
{
    private const int MaxLogLines = 2000;

    private readonly ILogger<SingBoxProcessManager> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Queue<string> _logLines = new();
    private readonly object _logLock = new();
    private readonly string _singBoxPath;
    private readonly string _configPath;

    private Process? _singBoxProcess;
    private DateTime? _startTime;
    private bool _disposed;

    public event EventHandler<string>? OnLogReceived;

    public SingBoxProcessManager(ILogger<SingBoxProcessManager> logger)
    {
        _logger = logger;
        _singBoxPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sing-box", "sing-box.exe");
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs", "config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
    }

    public bool IsRunning
    {
        get
        {
            try
            {
                return _singBoxProcess is { HasExited: false };
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    public int? ProcessId
    {
        get
        {
            try
            {
                return IsRunning ? _singBoxProcess?.Id : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    public DateTime? StartTime => IsRunning ? _startTime : null;

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

    public async Task<bool> RestartProxyAsync()
    {
        try
        {
            await StopAsync();
            await StartAsync();
            return IsRunning;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart proxy");
            return false;
        }
    }

    public bool IsProxyRunning() => IsRunning;

    public async Task StartAsync(string? configContent = null)
    {
        await _gate.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(configContent))
            {
                await File.WriteAllTextAsync(_configPath, configContent);
                _logger.LogInformation("Configuration written to {ConfigPath}", _configPath);
            }

            if (IsRunning)
            {
                if (string.IsNullOrWhiteSpace(configContent))
                {
                    _logger.LogInformation("sing-box is already running (PID {Pid})", ProcessId);
                    return;
                }

                await StopCoreAsync();
            }

            if (!File.Exists(_singBoxPath))
                throw new FileNotFoundException($"sing-box executable not found at {_singBoxPath}");

            if (!File.Exists(_configPath))
                throw new FileNotFoundException($"Configuration file not found at {_configPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = _singBoxPath,
                Arguments = $"run -c \"{_configPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                AppendToLog($"[STDOUT] {e.Data}");
                _logger.LogInformation("{Message}", e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                AppendToLog($"[STDERR] {e.Data}");
                _logger.LogWarning("{Message}", e.Data);
            };

            process.Exited += (_, _) =>
            {
                int? exitCode = null;
                try { exitCode = process.ExitCode; } catch { /* ignored */ }
                AppendToLog($"sing-box process exited with code {exitCode}");
                _logger.LogWarning("sing-box process exited with code {ExitCode}", exitCode);
                if (ReferenceEquals(_singBoxProcess, process))
                {
                    _singBoxProcess = null;
                    _startTime = null;
                }
            };

            if (!process.Start())
                throw new InvalidOperationException("Failed to start sing-box process");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _singBoxProcess = process;
            _startTime = DateTime.Now;

            await Task.Delay(800);

            if (!IsRunning)
            {
                var logs = GetLogs(30);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(logs)
                        ? "sing-box failed to start"
                        : $"sing-box failed to start:\n{logs}");
            }

            AppendToLog($"sing-box started successfully (PID {process.Id})");
            _logger.LogInformation("sing-box started successfully (PID {Pid})", process.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start sing-box");
            AppendToLog($"Error starting sing-box: {ex.Message}");
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await StopCoreAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReloadAsync(string configContent)
    {
        await StartAsync(configContent);
        if (!IsRunning)
            throw new InvalidOperationException("Failed to reload sing-box configuration");
    }

    public ProxyStatus GetStatus()
    {
        var process = _singBoxProcess;
        if (process == null)
            return ProxyStatus.Stopped;

        try
        {
            return process.HasExited ? ProxyStatus.Error : ProxyStatus.Running;
        }
        catch (InvalidOperationException)
        {
            return ProxyStatus.Error;
        }
    }

    public string GetLogs(int lastLines = 100)
    {
        lock (_logLock)
        {
            if (lastLines <= 0 || lastLines >= _logLines.Count)
                return string.Join(Environment.NewLine, _logLines);

            return string.Join(Environment.NewLine, _logLines.TakeLast(lastLines));
        }
    }

    public void ClearLogs()
    {
        lock (_logLock)
        {
            _logLines.Clear();
        }
    }

    public async Task<bool> UpdateConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configPath))
                return false;

            var content = await File.ReadAllTextAsync(_configPath);
            await ReloadAsync(content);
            return IsRunning;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configuration");
            return false;
        }
    }

    private async Task StopCoreAsync()
    {
        var process = _singBoxProcess;
        if (process == null)
            return;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Timed out waiting for sing-box to exit");
                }
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
            try { process.Dispose(); } catch { /* ignored */ }
            if (ReferenceEquals(_singBoxProcess, process))
                _singBoxProcess = null;
            _startTime = null;
        }
    }

    private void AppendToLog(string message)
    {
        var formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
        lock (_logLock)
        {
            _logLines.Enqueue(formatted);
            while (_logLines.Count > MaxLogLines)
                _logLines.Dequeue();
        }

        try
        {
            OnLogReceived?.Invoke(this, formatted);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Log listener failed");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _gate.Wait(TimeSpan.FromSeconds(3));
            StopCoreAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // ignored
        }
        finally
        {
            _gate.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
