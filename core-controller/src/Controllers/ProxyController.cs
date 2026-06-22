using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BreezeLink.CoreController.Models;
using System.Text.Json;

namespace BreezeLink.CoreController.Controllers;

/// <summary>
/// 代理控制器
/// 提供 REST API 接口来管理 sing-box 代理进程
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProxyController : ControllerBase
{
    private readonly IProxyProcessManager _proxyManager;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(IProxyProcessManager proxyManager, ILogger<ProxyController> logger)
    {
        _proxyManager = proxyManager;
        _logger = logger;
    }

    /// <summary>
    /// 启动代理服务
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartProxyRequest request)
    {
        try
        {
            _logger.LogInformation("Starting proxy service");

            await _proxyManager.StartAsync(request.ConfigContent);

            var status = _proxyManager.GetStatus();
            var response = ApiResponse<ProxyStatusResponse>.Ok(
                new ProxyStatusResponse
                {
                    Status = status.ToString(),
                    ProcessId = _proxyManager.ProcessId,
                    StartTime = DateTime.Now
                },
                "Proxy started successfully"
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start proxy");
            return BadRequest(ApiResponse<ProxyStatusResponse>.Error($"Failed to start proxy: {ex.Message}"));
        }
    }

    /// <summary>
    /// 停止代理服务
    /// </summary>
    [HttpPost("stop")]
    public async Task<IActionResult> Stop([FromBody] StopProxyRequest request)
    {
        try
        {
            _logger.LogInformation("Stopping proxy service");

            await _proxyManager.StopAsync();

            var response = ApiResponse<object>.Ok(null, "Proxy stopped successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop proxy");
            return BadRequest(ApiResponse<object>.Error($"Failed to stop proxy: {ex.Message}"));
        }
    }

    /// <summary>
    /// 重载代理配置
    /// </summary>
    [HttpPost("reload")]
    public async Task<IActionResult> Reload([FromBody] StartProxyRequest request)
    {
        try
        {
            _logger.LogInformation("Reloading proxy configuration");

            await _proxyManager.ReloadAsync(request.ConfigContent!);

            var status = _proxyManager.GetStatus();
            var response = ApiResponse<ProxyStatusResponse>.Ok(
                new ProxyStatusResponse
                {
                    Status = status.ToString(),
                    ProcessId = _proxyManager.ProcessId,
                    StartTime = DateTime.Now
                },
                "Proxy reloaded successfully"
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload proxy");
            return BadRequest(ApiResponse<ProxyStatusResponse>.Error($"Failed to reload proxy: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取代理状态
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        try
        {
            var status = _proxyManager.GetStatus();
            var response = ApiResponse<ProxyStatusResponse>.Ok(
                new ProxyStatusResponse
                {
                    Status = status.ToString(),
                    ProcessId = _proxyManager.ProcessId,
                    StartTime = DateTime.Now
                }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get proxy status");
            return StatusCode(500, ApiResponse<ProxyStatusResponse>.Error($"Failed to get status: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取代理日志
    /// </summary>
    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int lastLines = 100, [FromQuery] bool clear = false)
    {
        try
        {
            if (clear)
            {
                _proxyManager.ClearLogs();
            }

            var logs = _proxyManager.GetLogs(lastLines);
            var response = ApiResponse<LogsResponse>.Ok(
                new LogsResponse
                {
                    Logs = logs,
                    TotalLines = logs.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
                    HasMore = false // 简化实现，实际可计算是否还有更多日志
                }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get proxy logs");
            return StatusCode(500, ApiResponse<LogsResponse>.Error($"Failed to get logs: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取代理日志流（SSE）
    /// </summary>
    [HttpGet("logs/stream")]
    public async Task<IActionResult> StreamLogs()
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        var cancellationToken = HttpContext.RequestAborted;

        // 发送初始日志
        var logs = _proxyManager.GetLogs(50);
        if (!string.IsNullOrEmpty(logs))
        {
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { logs })}\n\n");
            await Response.Body.FlushAsync();
        }

        // 监听新日志
        void LogHandler(object? sender, string logMessage)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                var data = JsonSerializer.Serialize(new { log = logMessage });
                Response.WriteAsync($"data: {data}\n\n").Wait();
            }
        }

        _proxyManager.OnLogReceived += LogHandler;

        try
        {
            // 保持连接直到取消
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            // 正常取消
        }
        finally
        {
            _proxyManager.OnLogReceived -= LogHandler;
        }

        return new EmptyResult();
    }

    /// <summary>
    /// 健康检查端点
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(ApiResponse<object>.Ok(null, "Service is healthy"));
    }
}
