using System.Net.Http.Json;
using System.Text.Json;
using BreezeLink.CoreController.Models;

namespace BreezeLink.UI.Services;

/// <summary>
/// 代理服务客户端
/// 负责与后端 API 通信
/// </summary>
public class ProxyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProxyServiceClient> _logger;

    public ProxyServiceClient(HttpClient httpClient, ILogger<ProxyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://127.0.0.1:8800");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 启动代理
    /// </summary>
    public async Task<ApiResponse<ProxyStatusResponse>?> StartProxyAsync(string? configContent = null)
    {
        try
        {
            var request = new StartProxyRequest { ConfigContent = configContent };
            var response = await _httpClient.PostAsJsonAsync("/api/proxy/start", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<ProxyStatusResponse>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProxyStatusResponse>>();
                _logger.LogError("Failed to start proxy: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting proxy");
            return ApiResponse<ProxyStatusResponse>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 停止代理
    /// </summary>
    public async Task<ApiResponse<object>?> StopProxyAsync()
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/proxy/stop", new StopProxyRequest());

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                _logger.LogError("Failed to stop proxy: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping proxy");
            return ApiResponse<object>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 重载代理配置
    /// </summary>
    public async Task<ApiResponse<ProxyStatusResponse>?> ReloadProxyAsync(string configContent)
    {
        try
        {
            var request = new StartProxyRequest { ConfigContent = configContent };
            var response = await _httpClient.PostAsJsonAsync("/api/proxy/reload", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<ProxyStatusResponse>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProxyStatusResponse>>();
                _logger.LogError("Failed to reload proxy: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading proxy");
            return ApiResponse<ProxyStatusResponse>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取代理状态
    /// </summary>
    public async Task<ApiResponse<ProxyStatusResponse>?> GetProxyStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/proxy/status");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<ProxyStatusResponse>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProxyStatusResponse>>();
                _logger.LogError("Failed to get proxy status: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proxy status");
            return ApiResponse<ProxyStatusResponse>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取代理日志
    /// </summary>
    public async Task<ApiResponse<LogsResponse>?> GetProxyLogsAsync(int lastLines = 100)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/proxy/logs?lastLines={lastLines}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<LogsResponse>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LogsResponse>>();
                _logger.LogError("Failed to get proxy logs: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proxy logs");
            return ApiResponse<LogsResponse>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/proxy/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 通知服务
/// 负责显示系统通知
/// </summary>
public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 显示成功通知
    /// </summary>
    public void ShowSuccess(string title, string message)
    {
        try
        {
            var notification = new CommunityToolkit.WinUI.Notifications.ToastNotificationManager();
            // 简化实现，实际应该使用 ToastNotification
            _logger.LogInformation("Success: {Title} - {Message}", title, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show success notification");
        }
    }

    /// <summary>
    /// 显示错误通知
    /// </summary>
    public void ShowError(string title, string message)
    {
        try
        {
            var notification = new CommunityToolkit.WinUI.Notifications.ToastNotificationManager();
            // 简化实现，实际应该使用 ToastNotification
            _logger.LogError("Error: {Title} - {Message}", title, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show error notification");
        }
    }

    /// <summary>
    /// 显示警告通知
    /// </summary>
    public void ShowWarning(string title, string message)
    {
        try
        {
            var notification = new CommunityToolkit.WinUI.Notifications.ToastNotificationManager();
            // 简化实现，实际应该使用 ToastNotification
            _logger.LogWarning("Warning: {Title} - {Message}", title, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show warning notification");
        }
    }
}
