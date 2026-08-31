using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BreezeLink.CoreController.Models;
using Timer = System.Timers.Timer;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 通过 sing-box clash API 读取真实流量，内核未运行时保持为 0。
/// </summary>
public class TrafficMonitoringService : ITrafficMonitoringService, IDisposable
{
    private readonly ILogger<TrafficMonitoringService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProxyProcessManager _proxyManager;
    private readonly string _clashApiUrl;
    private readonly object _statsLock = new();

    private Timer? _monitoringTimer;
    private long _uploadBytes;
    private long _downloadBytes;
    private double _uploadSpeedBps;
    private double _downloadSpeedBps;
    private DateTime _lastSample = DateTime.MinValue;
    private bool _isMonitoring;

    public TrafficMonitoringService(
        ILogger<TrafficMonitoringService> logger,
        IHttpClientFactory httpClientFactory,
        IProxyProcessManager proxyManager,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _proxyManager = proxyManager;
        _clashApiUrl = configuration.GetValue("ProxySettings:ClashApiUrl", "http://127.0.0.1:9090")!.TrimEnd('/');
    }

    public void StartMonitoring()
    {
        if (_isMonitoring)
            return;

        _monitoringTimer = new Timer(1000);
        _monitoringTimer.Elapsed += OnTimerElapsed;
        _monitoringTimer.AutoReset = true;
        _monitoringTimer.Start();
        _isMonitoring = true;
        _logger.LogInformation("Traffic monitoring started ({ClashApi})", _clashApiUrl);
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring)
            return;

        _monitoringTimer?.Stop();
        _monitoringTimer?.Dispose();
        _monitoringTimer = null;
        _isMonitoring = false;
        _logger.LogInformation("Traffic monitoring stopped");
    }

    public Task<TrafficStats> GetTrafficStatsAsync()
    {
        lock (_statsLock)
        {
            return Task.FromResult(new TrafficStats
            {
                UploadBytes = _uploadBytes,
                DownloadBytes = _downloadBytes,
                UploadSpeedBps = _uploadSpeedBps,
                DownloadSpeedBps = _downloadSpeedBps,
                LastUpdateTime = DateTime.Now
            });
        }
    }

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (!_proxyManager.IsRunning)
            {
                ResetStats();
                return;
            }

            var client = _httpClientFactory.CreateClient(nameof(TrafficMonitoringService));
            client.Timeout = TimeSpan.FromSeconds(2);
            var payload = await client.GetFromJsonAsync<ClashConnectionsResponse>($"{_clashApiUrl}/connections");
            if (payload == null)
                return;

            var now = DateTime.Now;
            lock (_statsLock)
            {
                if (_lastSample != DateTime.MinValue)
                {
                    var dt = (now - _lastSample).TotalSeconds;
                    if (dt > 0)
                    {
                        _uploadSpeedBps = Math.Max(0, (payload.UploadTotal - _uploadBytes) / dt);
                        _downloadSpeedBps = Math.Max(0, (payload.DownloadTotal - _downloadBytes) / dt);
                    }
                }

                _uploadBytes = payload.UploadTotal;
                _downloadBytes = payload.DownloadTotal;
                _lastSample = now;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Clash API traffic poll failed");
        }
    }

    private void ResetStats()
    {
        lock (_statsLock)
        {
            if (_uploadBytes == 0 && _downloadBytes == 0 && _uploadSpeedBps == 0 && _downloadSpeedBps == 0)
                return;

            _uploadBytes = 0;
            _downloadBytes = 0;
            _uploadSpeedBps = 0;
            _downloadSpeedBps = 0;
            _lastSample = DateTime.MinValue;
        }
    }

    public void Dispose()
    {
        StopMonitoring();
        GC.SuppressFinalize(this);
    }

    private sealed class ClashConnectionsResponse
    {
        [JsonPropertyName("uploadTotal")]
        public long UploadTotal { get; set; }

        [JsonPropertyName("downloadTotal")]
        public long DownloadTotal { get; set; }
    }
}
