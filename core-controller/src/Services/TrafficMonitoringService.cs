using System.Timers;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 流量监控服务实现
/// </summary>
public class TrafficMonitoringService : ITrafficMonitoringService, IDisposable
{
    private readonly ILogger<TrafficMonitoringService> _logger;
    private Timer? _monitoringTimer;
    private long _uploadBytes;
    private long _downloadBytes;
    private bool _isMonitoring;
    private readonly object _statsLock = new();

    public TrafficMonitoringService(ILogger<TrafficMonitoringService> logger)
    {
        _logger = logger;
        _uploadBytes = 0;
        _downloadBytes = 0;
    }

    /// <summary>
    /// 开始流量监控
    /// </summary>
    public void StartMonitoring()
    {
        if (_isMonitoring)
        {
            _logger.LogWarning("Traffic monitoring is already running");
            return;
        }

        _logger.LogInformation("Starting traffic monitoring");

        _monitoringTimer = new Timer(1000); // 每秒更新一次
        _monitoringTimer.Elapsed += OnTimerElapsed;
        _monitoringTimer.Start();

        _isMonitoring = true;
        _logger.LogInformation("Traffic monitoring started");
    }

    /// <summary>
    /// 停止流量监控
    /// </summary>
    public void StopMonitoring()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _logger.LogInformation("Stopping traffic monitoring");

        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;

        _isMonitoring = false;
        _logger.LogInformation("Traffic monitoring stopped");
    }

    /// <summary>
    /// 获取当前流量统计
    /// </summary>
    public async Task<TrafficStats> GetTrafficStatsAsync()
    {
        return await Task.FromResult(new TrafficStats
        {
            UploadBytes = _uploadBytes,
            DownloadBytes = _downloadBytes,
            LastUpdateTime = DateTime.Now
        });
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            // 这里应该实现实际的流量监控逻辑
            // 目前只是一个占位符实现

            // 模拟流量数据更新
            lock (_statsLock)
            {
                // 在实际实现中，这里应该从网络接口获取真实的流量数据
                _uploadBytes += Random.Shared.Next(1000, 5000);
                _downloadBytes += Random.Shared.Next(5000, 15000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating traffic statistics");
        }
    }

    public void Dispose()
    {
        StopMonitoring();
        GC.SuppressFinalize(this);
    }
}
