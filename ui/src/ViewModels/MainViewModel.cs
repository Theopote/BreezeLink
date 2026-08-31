using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BreezeLink.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace BreezeLink.UI.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ProxyServiceClient _proxyService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly DispatcherQueueTimer _statusTimer;
    private readonly DispatcherQueueTimer _logsTimer;
    private readonly DispatcherQueueTimer _reconnectTimer;
    private bool _disposed;

    [ObservableProperty]
    private string statusText = "正在连接核心服务...";

    [ObservableProperty]
    private string logsText = "等待日志...";

    [ObservableProperty]
    private string trafficText = "↑ 0 B/s  ↓ 0 B/s";

    [ObservableProperty]
    private bool isProxyRunning;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isServiceConnected;

    [ObservableProperty]
    private string startButtonText = "启动代理";

    [ObservableProperty]
    private string stopButtonText = "停止代理";

    [ObservableProperty]
    private bool isNotificationOpen;

    [ObservableProperty]
    private string notificationTitle = string.Empty;

    [ObservableProperty]
    private string notificationMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity notificationSeverity = InfoBarSeverity.Informational;

    public bool CanStart => !IsLoading && IsServiceConnected && !IsProxyRunning;
    public bool CanStop => !IsLoading && IsServiceConnected && IsProxyRunning;
    public bool CanReload => !IsLoading && IsServiceConnected;

    public MainViewModel(
        ProxyServiceClient proxyService,
        NotificationService notificationService,
        ILogger<MainViewModel> logger)
    {
        _proxyService = proxyService;
        _notificationService = notificationService;
        _logger = logger;

        var dispatcher = DispatcherQueue.GetForCurrentThread();
        _statusTimer = dispatcher.CreateTimer();
        _statusTimer.Interval = TimeSpan.FromSeconds(5);
        _statusTimer.Tick += async (_, _) =>
        {
            await UpdateStatusAsync();
            await UpdateTrafficAsync();
        };

        _logsTimer = dispatcher.CreateTimer();
        _logsTimer.Interval = TimeSpan.FromSeconds(3);
        _logsTimer.Tick += async (_, _) => await UpdateLogsAsync();

        _reconnectTimer = dispatcher.CreateTimer();
        _reconnectTimer.Interval = TimeSpan.FromSeconds(3);
        _reconnectTimer.Tick += async (_, _) => await CheckConnectionAsync();

        _notificationService.NotificationRaised += OnNotificationRaised;
        _ = CheckConnectionAsync();
    }

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
                await UpdateLogsAsync();
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
                _notificationService.ShowSuccess("代理服务", "代理已停止");
                await UpdateStatusAsync();
                await UpdateTrafficAsync();
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

    [RelayCommand]
    private async Task ReloadProxyAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var response = await _proxyService.ReloadProxyAsync();
            if (response?.Success == true)
            {
                _notificationService.ShowSuccess("代理服务", "已按当前节点重新生成并重载配置");
                await UpdateStatusAsync();
                await UpdateLogsAsync();
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

    [RelayCommand]
    private async Task RefreshStatusAsync()
    {
        await CheckConnectionAsync();
        await UpdateStatusAsync();
        await UpdateLogsAsync();
        await UpdateTrafficAsync();
    }

    [RelayCommand]
    private async Task ClearLogsAsync()
    {
        var response = await _proxyService.ClearLogsAsync();
        LogsText = response?.Success == true ? "日志已清空" : (response?.Message ?? "清空失败");
        if (response?.Success == true)
            _notificationService.ShowSuccess("日志", "日志已清空");
        else
            _notificationService.ShowError("日志", LogsText);
    }

    private async Task CheckConnectionAsync()
    {
        try
        {
            var isConnected = await _proxyService.HealthCheckAsync();
            IsServiceConnected = isConnected;

            if (isConnected)
            {
                _reconnectTimer.Stop();
                if (!_statusTimer.IsRunning) _statusTimer.Start();
                if (!_logsTimer.IsRunning) _logsTimer.Start();
                await UpdateStatusAsync();
                await UpdateLogsAsync();
                await UpdateTrafficAsync();
            }
            else
            {
                _statusTimer.Stop();
                _logsTimer.Stop();
                IsProxyRunning = false;
                StatusText = "无法连接到核心服务，正在重试...";
                if (!_reconnectTimer.IsRunning)
                    _reconnectTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check connection");
            IsServiceConnected = false;
            StatusText = "连接检查失败";
        }
    }

    private async Task UpdateStatusAsync()
    {
        try
        {
            var response = await _proxyService.GetProxyStatusAsync();
            if (response?.Success == true)
            {
                IsServiceConnected = true;
                var data = response.Data;
                IsProxyRunning = data?.Status == "Running";
                StatusText = IsProxyRunning ? "代理运行中" : "代理已停止";

                if (data?.ProcessId.HasValue == true)
                    StatusText += $"  (PID: {data.ProcessId})";
            }
            else if (response != null && !response.Success)
            {
                StatusText = response.Message ?? "状态获取失败";
                IsProxyRunning = false;
            }
            else
            {
                IsServiceConnected = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status");
            IsServiceConnected = false;
            StatusText = "状态更新失败";
            IsProxyRunning = false;
        }
    }

    private async Task UpdateLogsAsync()
    {
        try
        {
            var response = await _proxyService.GetProxyLogsAsync(80);
            if (response?.Success == true)
                LogsText = string.IsNullOrWhiteSpace(response.Data?.Logs) ? "暂无日志" : response.Data.Logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update logs");
        }
    }

    private async Task UpdateTrafficAsync()
    {
        try
        {
            if (!IsProxyRunning)
            {
                TrafficText = "↑ 0 B/s  ↓ 0 B/s";
                return;
            }

            var response = await _proxyService.GetTrafficAsync();
            if (response?.Success == true && response.Data != null)
            {
                var stats = response.Data;
                TrafficText = $"↑ {stats.UploadSpeedText}  ↓ {stats.DownloadSpeedText}    累计 {stats.UploadText} / {stats.DownloadText}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to update traffic");
        }
    }

    private void OnNotificationRaised(object? sender, AppNotificationEventArgs e)
    {
        NotificationTitle = e.Title;
        NotificationMessage = e.Message;
        NotificationSeverity = e.Severity;
        IsNotificationOpen = true;
    }

    partial void OnIsLoadingChanged(bool value) => NotifyCommandStates();
    partial void OnIsProxyRunningChanged(bool value) => NotifyCommandStates();
    partial void OnIsServiceConnectedChanged(bool value) => NotifyCommandStates();

    private void NotifyCommandStates()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(CanReload));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notificationService.NotificationRaised -= OnNotificationRaised;
        _statusTimer.Stop();
        _logsTimer.Stop();
        _reconnectTimer.Stop();
    }
}
