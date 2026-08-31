using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 代理服务后台任务
/// 负责启动和维护代理服务
/// </summary>
public class ProxyService : BackgroundService
{
    private readonly ILogger<ProxyService> _logger;
    private readonly IProxyProcessManager _proxyManager;
    private readonly INodeManagementService _nodeManagement;
    private readonly ITrafficMonitoringService _trafficMonitoring;
    private readonly ISystemTrayService _systemTrayService;

    public ProxyService(
        ILogger<ProxyService> logger,
        IProxyProcessManager proxyManager,
        INodeManagementService nodeManagement,
        ITrafficMonitoringService trafficMonitoring,
        ISystemTrayService systemTrayService)
    {
        _logger = logger;
        _proxyManager = proxyManager;
        _nodeManagement = nodeManagement;
        _trafficMonitoring = trafficMonitoring;
        _systemTrayService = systemTrayService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProxyService is starting...");

        try
        {
            // 启动流量监控
            _trafficMonitoring.StartMonitoring();

            // 初始化系统托盘
            await _systemTrayService.InitializeAsync();

            _logger.LogInformation("ProxyService started successfully");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProxyService");
        }
        finally
        {
            _logger.LogInformation("ProxyService is stopping...");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProxyService is stopping...");

        try
        {
            // 停止流量监控
            _trafficMonitoring.StopMonitoring();

            // 停止代理进程
            if (_proxyManager.IsRunning)
            {
                await _proxyManager.StopAsync();
            }

            _logger.LogInformation("ProxyService stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping ProxyService");
        }

        await base.StopAsync(cancellationToken);
    }
}
