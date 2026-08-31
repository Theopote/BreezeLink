using System.Net.Http.Json;
using BreezeLink.CoreController.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace BreezeLink.UI.Services;

public class ProxyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProxyServiceClient> _logger;

    public ProxyServiceClient(HttpClient httpClient, ILogger<ProxyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress ??= new Uri("http://127.0.0.1:8800");
        if (_httpClient.Timeout == Timeout.InfiniteTimeSpan)
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<ApiResponse<ProxyStatusResponse>?> StartProxyAsync(string? configContent = null)
    {
        return await SendAsync<StartProxyRequest, ProxyStatusResponse>(
            HttpMethod.Post, "/api/proxy/start", new StartProxyRequest { ConfigContent = configContent }, "start proxy");
    }

    public async Task<ApiResponse<object>?> StopProxyAsync()
    {
        return await SendAsync<StopProxyRequest, object>(
            HttpMethod.Post, "/api/proxy/stop", new StopProxyRequest(), "stop proxy");
    }

    public async Task<ApiResponse<ProxyStatusResponse>?> ReloadProxyAsync(string? configContent = null)
    {
        return await SendAsync<StartProxyRequest, ProxyStatusResponse>(
            HttpMethod.Post, "/api/proxy/reload", new StartProxyRequest { ConfigContent = configContent }, "reload proxy");
    }

    public Task<ApiResponse<ProxyStatusResponse>?> GetProxyStatusAsync()
        => GetAsync<ProxyStatusResponse>("/api/proxy/status", "get proxy status");

    public Task<ApiResponse<LogsResponse>?> GetProxyLogsAsync(int lastLines = 100)
        => GetAsync<LogsResponse>($"/api/proxy/logs?lastLines={lastLines}", "get proxy logs");

    public Task<ApiResponse<TrafficStats>?> GetTrafficAsync()
        => GetAsync<TrafficStats>("/api/proxy/traffic", "get traffic");

    public async Task<ApiResponse<object>?> ClearLogsAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("/api/proxy/logs");
            return await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing logs");
            return ApiResponse<object>.Error($"Connection error: {ex.Message}");
        }
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = await _httpClient.GetAsync("/api/proxy/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<ApiResponse<T>?> GetAsync<T>(string url, string action)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: {Action}", action);
            return ApiResponse<T>.Error($"Connection error: {ex.Message}");
        }
    }

    private async Task<ApiResponse<TResponse>?> SendAsync<TRequest, TResponse>(HttpMethod method, string url, TRequest body, string action)
    {
        try
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(body, options: JsonOptions.Default)
            };
            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error: {Action}", action);
            return ApiResponse<TResponse>.Error($"Connection error: {ex.Message}");
        }
    }
}

public class AppNotificationEventArgs : EventArgs
{
    public string Title { get; }
    public string Message { get; }
    public InfoBarSeverity Severity { get; }

    public AppNotificationEventArgs(string title, string message, InfoBarSeverity severity)
    {
        Title = title;
        Message = message;
        Severity = severity;
    }
}

public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public event EventHandler<AppNotificationEventArgs>? NotificationRaised;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public void ShowSuccess(string title, string message) => Raise(title, message, InfoBarSeverity.Success);

    public void ShowError(string title, string message) => Raise(title, message, InfoBarSeverity.Error);

    public void ShowWarning(string title, string message) => Raise(title, message, InfoBarSeverity.Warning);

    public void ShowInfo(string title, string message) => Raise(title, message, InfoBarSeverity.Informational);

    private void Raise(string title, string message, InfoBarSeverity severity)
    {
        switch (severity)
        {
            case InfoBarSeverity.Error:
                _logger.LogError("{Title} - {Message}", title, message);
                break;
            case InfoBarSeverity.Warning:
                _logger.LogWarning("{Title} - {Message}", title, message);
                break;
            default:
                _logger.LogInformation("{Title} - {Message}", title, message);
                break;
        }

        NotificationRaised?.Invoke(this, new AppNotificationEventArgs(title, message, severity));
    }
}
