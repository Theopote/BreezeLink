using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BreezeLink.UI.Services;
using BreezeLink.CoreController.Models;
using Microsoft.Extensions.Logging;

namespace BreezeLink.UI.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ProxyServiceClient _proxyService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly Timer _statusTimer;
    private readonly Timer _logsTimer;

    [ObservableProperty]
    private string statusText = "未连接";

    [ObservableProperty]
    private string logsText = "等待日志...";

    [ObservableProperty]
    private bool isProxyRunning = false;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string startButtonText = "启动代理";

    [ObservableProperty]
    private string stopButtonText = "停止代理";

    public MainViewModel(
        ProxyServiceClient proxyService,
        NotificationService notificationService,
        ILogger<MainViewModel> logger)
    {
        _proxyService = proxyService;
        _notificationService = notificationService;
        _logger = logger;

        // 初始化定时器
        _statusTimer = new Timer(UpdateStatus, null, Timeout.Infinite, Timeout.Infinite);
        _logsTimer = new Timer(UpdateLogs, null, Timeout.Infinite, Timeout.Infinite);

        // 启动时检查连接
        _ = CheckConnectionAsync();
    }

    /// <summary>
    /// 启动代理命令
    /// </summary>
    [RelayCommand]
    private async Task StartProxyAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        StartButtonText = "启动中...";

        try
        {
            var response = await _proxyService.StartProxyAsync();

            if (response?.Success == true)
            {
                _notificationService.ShowSuccess("代理服务", "代理启动成功");
                await UpdateStatusAsync();
            }
            else
            {
                var errorMessage = response?.Message ?? "启动失败";
                _notificationService.ShowError("代理服务", errorMessage);
                StatusText = $"启动失败: {errorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start proxy");
            _notificationService.ShowError("代理服务", $"启动异常: {ex.Message}");
            StatusText = $"启动异常: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            StartButtonText = "启动代理";
        }
    }

    /// <summary>
    /// 停止代理命令
    /// </summary>
    [RelayCommand]
    private async Task StopProxyAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        StopButtonText = "停止中...";

        try
        {
            var response = await _proxyService.StopProxyAsync();

            if (response?.Success == true)
            {
                _notificationService.ShowSuccess("代理服务", "代理停止成功");
                await UpdateStatusAsync();
            }
            else
            {
                var errorMessage = response?.Message ?? "停止失败";
                _notificationService.ShowError("代理服务", errorMessage);
                StatusText = $"停止失败: {errorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop proxy");
            _notificationService.ShowError("代理服务", $"停止异常: {ex.Message}");
            StatusText = $"停止异常: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            StopButtonText = "停止代理";
        }
    }

    /// <summary>
    /// 重载代理命令
    /// </summary>
    [RelayCommand]
    private async Task ReloadProxyAsync()
    {
        if (IsLoading) return;

        IsLoading = true;

        try
        {
            // 这里应该从配置文件读取配置内容
            var configContent = await File.ReadAllTextAsync("config.json");

            var response = await _proxyService.ReloadProxyAsync(configContent);

            if (response?.Success == true)
            {
                _notificationService.ShowSuccess("代理服务", "配置重载成功");
                await UpdateStatusAsync();
            }
            else
            {
                var errorMessage = response?.Message ?? "重载失败";
                _notificationService.ShowError("代理服务", errorMessage);
                StatusText = $"重载失败: {errorMessage}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload proxy");
            _notificationService.ShowError("代理服务", $"重载异常: {ex.Message}");
            StatusText = $"重载异常: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 刷新状态命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        await UpdateStatusAsync();
    }

    /// <summary>
    /// 清空日志命令
    /// </summary>
    [RelayCommand]
    private async Task ClearLogsAsync()
    {
        LogsText = "日志已清空";
        _notificationService.ShowSuccess("日志", "日志已清空");
    }

    /// <summary>
    /// 检查连接状态
    /// </summary>
    private async Task CheckConnectionAsync()
    {
        try
        {
            var isConnected = await _proxyService.HealthCheckAsync();

            if (isConnected)
            {
                StatusText = "已连接到代理服务";
                StartStatusTimer();
                StartLogsTimer();
            }
            else
            {
                StatusText = "无法连接到代理服务";
                _notificationService.ShowWarning("连接状态", "代理服务未运行，请先启动代理服务");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check connection");
            StatusText = "连接检查失败";
        }
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    private async void UpdateStatus(object? state)
    {
        await UpdateStatusAsync();
    }

    /// <summary>
    /// 更新状态（异步）
    /// </summary>
    private async Task UpdateStatusAsync()
    {
        try
        {
            var response = await _proxyService.GetProxyStatusAsync();

            if (response?.Success == true)
            {
                var data = response.Data;
                IsProxyRunning = data?.Status == "Running";
                StatusText = IsProxyRunning ? "代理运行中" : "代理已停止";

                if (data?.ProcessId.HasValue == true)
                {
                    StatusText += $" (PID: {data.ProcessId})";
                }
            }
            else
            {
                StatusText = "状态获取失败";
                IsProxyRunning = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status");
            StatusText = "状态更新失败";
            IsProxyRunning = false;
        }
    }

    /// <summary>
    /// 更新日志
    /// </summary>
    private async void UpdateLogs(object? state)
    {
        await UpdateLogsAsync();
    }

    /// <summary>
    /// 更新日志（异步）
    /// </summary>
    private async Task UpdateLogsAsync()
    {
        try
        {
            var response = await _proxyService.GetProxyLogsAsync(50);

            if (response?.Success == true)
            {
                var logs = response.Data?.Logs ?? "无日志";
                LogsText = logs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update logs");
            LogsText = $"日志更新失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 启动状态定时器
    /// </summary>
    private void StartStatusTimer()
    {
        _statusTimer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// 启动日志定时器
    /// </summary>
    private void StartLogsTimer()
    {
        _logsTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    private void StopTimers()
    {
        _statusTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _logsTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    public void Dispose()
    {
        StopTimers();
        _statusTimer.Dispose();
        _logsTimer.Dispose();
    }
}
