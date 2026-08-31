using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BreezeLink.CoreController.Models;
using BreezeLink.CoreController.Services;

namespace BreezeLink.CoreController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProxyController : ControllerBase
{
    private readonly IProxyProcessManager _proxyManager;
    private readonly ISingBoxConfigService _configService;
    private readonly ITrafficMonitoringService _trafficMonitoring;
    private readonly ILogger<ProxyController> _logger;

    public ProxyController(
        IProxyProcessManager proxyManager,
        ISingBoxConfigService configService,
        ITrafficMonitoringService trafficMonitoring,
        ILogger<ProxyController> logger)
    {
        _proxyManager = proxyManager;
        _configService = configService;
        _trafficMonitoring = trafficMonitoring;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartProxyRequest? request)
    {
        try
        {
            var config = request?.ConfigContent;
            if (string.IsNullOrWhiteSpace(config))
                config = await _configService.BuildConfigAsync();

            await _proxyManager.StartAsync(config);
            return Ok(ApiResponse<ProxyStatusResponse>.Ok(BuildStatus(), "Proxy started successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start proxy");
            return BadRequest(ApiResponse<ProxyStatusResponse>.Error($"Failed to start proxy: {ex.Message}"));
        }
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop([FromBody] StopProxyRequest? request)
    {
        try
        {
            await _proxyManager.StopAsync();
            return Ok(ApiResponse<object>.Ok(null, "Proxy stopped successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop proxy");
            return BadRequest(ApiResponse<object>.Error($"Failed to stop proxy: {ex.Message}"));
        }
    }

    [HttpPost("reload")]
    public async Task<IActionResult> Reload([FromBody] StartProxyRequest? request)
    {
        try
        {
            var config = request?.ConfigContent;
            if (string.IsNullOrWhiteSpace(config))
                config = await _configService.BuildConfigAsync();

            await _proxyManager.ReloadAsync(config);
            return Ok(ApiResponse<ProxyStatusResponse>.Ok(BuildStatus(), "Proxy reloaded successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload proxy");
            return BadRequest(ApiResponse<ProxyStatusResponse>.Error($"Failed to reload proxy: {ex.Message}"));
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        try
        {
            return Ok(ApiResponse<ProxyStatusResponse>.Ok(BuildStatus()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get proxy status");
            return StatusCode(500, ApiResponse<ProxyStatusResponse>.Error($"Failed to get status: {ex.Message}"));
        }
    }

    [HttpGet("traffic")]
    public async Task<IActionResult> GetTraffic()
    {
        try
        {
            var stats = await _trafficMonitoring.GetTrafficStatsAsync();
            return Ok(ApiResponse<TrafficStats>.Ok(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get traffic stats");
            return StatusCode(500, ApiResponse<TrafficStats>.Error($"Failed to get traffic: {ex.Message}"));
        }
    }

    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int lastLines = 100)
    {
        try
        {
            lastLines = Math.Clamp(lastLines, 1, 2000);
            var logs = _proxyManager.GetLogs(lastLines);
            var lineCount = string.IsNullOrEmpty(logs)
                ? 0
                : logs.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

            return Ok(ApiResponse<LogsResponse>.Ok(new LogsResponse
            {
                Logs = logs,
                TotalLines = lineCount,
                HasMore = lineCount >= lastLines
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get proxy logs");
            return StatusCode(500, ApiResponse<LogsResponse>.Error($"Failed to get logs: {ex.Message}"));
        }
    }

    [HttpDelete("logs")]
    public IActionResult ClearLogs()
    {
        _proxyManager.ClearLogs();
        return Ok(ApiResponse<object>.Ok(null, "Logs cleared"));
    }

    [HttpGet("logs/stream")]
    public async Task StreamLogs(CancellationToken cancellationToken)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        void LogHandler(object? sender, string logMessage)
        {
            channel.Writer.TryWrite(logMessage);
        }

        _proxyManager.OnLogReceived += LogHandler;
        try
        {
            var existing = _proxyManager.GetLogs(50);
            if (!string.IsNullOrEmpty(existing))
            {
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { logs = existing })}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { log = message })}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // client disconnected
        }
        finally
        {
            _proxyManager.OnLogReceived -= LogHandler;
            channel.Writer.TryComplete();
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(ApiResponse<object>.Ok(new
        {
            status = "ok",
            proxy = _proxyManager.GetStatus().ToString()
        }, "Service is healthy"));
    }

    private ProxyStatusResponse BuildStatus()
    {
        return new ProxyStatusResponse
        {
            Status = _proxyManager.GetStatus().ToString(),
            ProcessId = _proxyManager.ProcessId,
            StartTime = _proxyManager.StartTime
        };
    }
}
