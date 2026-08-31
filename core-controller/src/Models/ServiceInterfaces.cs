using System.Threading.Tasks;
using BreezeLink.CoreController.Models;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 节点测试服务接口
/// </summary>
public interface INodeTestingService
{
    Task<List<string>> ValidateNodeAsync(ProxyNode node);
    Task<NodeTestResult> TestNodeAsync(ProxyNode node, int timeout = 5000);
    Task<NodeTestResult> TestNodeAsync(Guid nodeId, int timeout = 5000, string? testUrl = null);
    Task<BatchTestResult> TestNodesAsync(List<ProxyNode> nodes, int timeout = 5000);
    Task<BatchTestResult> TestNodesAsync(List<Guid> nodeIds, int timeout = 5000, string? testUrl = null);
    Task<BatchTestResult> TestAllNodesAsync(int timeout = 5000, string? testUrl = null);
    Task<List<ProxyNode>> GetBestNodesAsync(int count = 5);
    Task<Dictionary<string, int>> GetNodeStatisticsAsync();
    Task<bool> IsNodeAvailableAsync(ProxyNode node, int timeout = 5000);
}

/// <summary>
/// 流量监控服务接口
/// </summary>
public interface ITrafficMonitoringService
{
    /// <summary>
    /// 开始流量监控
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// 停止流量监控
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// 获取当前流量统计
    /// </summary>
    Task<TrafficStats> GetTrafficStatsAsync();
}

/// <summary>
/// 系统托盘服务接口
/// </summary>
public interface ISystemTrayService
{
    /// <summary>
    /// 初始化系统托盘
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 显示通知
    /// </summary>
    void ShowNotification(string title, string message);

    /// <summary>
    /// 更新托盘图标状态
    /// </summary>
    void UpdateTrayStatus(bool isConnected);
}

/// <summary>
/// 代理进程状态
/// </summary>
public enum ProxyStatus
{
    Stopped,
    Running,
    Error
}

/// <summary>
/// 代理进程管理器接口
/// </summary>
public interface IProxyProcessManager
{
    Task StartAsync(string? configContent = null);
    Task StopAsync();
    Task ReloadAsync(string configContent);
    ProxyStatus GetStatus();
    string GetLogs(int lastLines = 100);
    void ClearLogs();
    int? ProcessId { get; }
    bool IsRunning { get; }
    Task<bool> StartProxyAsync();
    Task<bool> StopProxyAsync();
    Task<bool> RestartProxyAsync();
    bool IsProxyRunning();
    Task<bool> UpdateConfigurationAsync();
    DateTime? StartTime { get; }
    event EventHandler<string>? OnLogReceived;
}

/// <summary>
/// 节点管理服务接口
/// </summary>
public interface INodeManagementService
{
    Task<List<ProxyNode>> GetAllNodesAsync();
    Task<ProxyNode?> GetNodeByIdAsync(Guid id);
    Task<List<ProxyNode>> GetNodesByGroupAsync(Guid? groupId);
    Task<ProxyNode> CreateNodeAsync(ProxyNode node);
    Task<ProxyNode?> UpdateNodeAsync(Guid id, ProxyNode node);
    Task<bool> AddNodeAsync(ProxyNode node);
    Task<bool> UpdateNodeAsync(ProxyNode node);
    Task<bool> DeleteNodeAsync(Guid id);
    Task<List<ProxyNodeGroup>> GetAllGroupsAsync();
    Task<ProxyNodeGroup?> GetGroupByIdAsync(Guid id);
    Task<ProxyNodeGroup> CreateGroupAsync(ProxyNodeGroup group);
    Task<ProxyNodeGroup?> UpdateGroupAsync(Guid id, ProxyNodeGroup group);
    Task<bool> AddGroupAsync(ProxyNodeGroup group);
    Task<bool> UpdateGroupAsync(ProxyNodeGroup group);
    Task<bool> DeleteGroupAsync(Guid id);
    Task<NodeConfigResponse> GetNodeConfigAsync(Guid? groupId = null);
    Task ApplyNodeConfigAsync(NodeConfigRequest request);
    Task UpdateNodesStatusAsync(List<(Guid NodeId, int Latency, NodeTestStatus Status)> updates);
}

/// <summary>
/// 根据节点生成 sing-box 配置
/// </summary>
public interface ISingBoxConfigService
{
    Task<string> BuildConfigAsync();
}
