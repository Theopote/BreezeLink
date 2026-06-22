using System.Threading.Tasks;
using BreezeLink.CoreController.Models;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 节点测试服务接口
/// </summary>
public interface INodeTestingService
{
    /// <summary>
    /// 测试单个节点的连接性
    /// </summary>
    Task<NodeTestResult> TestNodeAsync(ProxyNode node, int timeout = 5000);

    /// <summary>
    /// 批量测试节点
    /// </summary>
    Task<BatchTestResult> TestNodesAsync(List<ProxyNode> nodes, int timeout = 5000);

    /// <summary>
    /// 测试节点是否可用
    /// </summary>
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
/// 代理进程管理器接口
/// </summary>
public interface IProxyProcessManager
{
    /// <summary>
    /// 启动代理进程
    /// </summary>
    Task<bool> StartProxyAsync();

    /// <summary>
    /// 停止代理进程
    /// </summary>
    Task<bool> StopProxyAsync();

    /// <summary>
    /// 重启代理进程
    /// </summary>
    Task<bool> RestartProxyAsync();

    /// <summary>
    /// 检查代理进程是否运行
    /// </summary>
    bool IsProxyRunning();

    /// <summary>
    /// 更新代理配置
    /// </summary>
    Task<bool> UpdateConfigurationAsync();
}

/// <summary>
/// 节点管理服务接口
/// </summary>
public interface INodeManagementService
{
    /// <summary>
    /// 获取所有节点
    /// </summary>
    Task<List<ProxyNode>> GetAllNodesAsync();

    /// <summary>
    /// 根据ID获取节点
    /// </summary>
    Task<ProxyNode?> GetNodeByIdAsync(Guid id);

    /// <summary>
    /// 根据分组获取节点
    /// </summary>
    Task<List<ProxyNode>> GetNodesByGroupAsync(Guid? groupId);

    /// <summary>
    /// 添加节点
    /// </summary>
    Task<bool> AddNodeAsync(ProxyNode node);

    /// <summary>
    /// 更新节点
    /// </summary>
    Task<bool> UpdateNodeAsync(ProxyNode node);

    /// <summary>
    /// 删除节点
    /// </summary>
    Task<bool> DeleteNodeAsync(Guid id);

    /// <summary>
    /// 获取所有节点组
    /// </summary>
    Task<List<ProxyNodeGroup>> GetAllGroupsAsync();

    /// <summary>
    /// 添加节点组
    /// </summary>
    Task<bool> AddGroupAsync(ProxyNodeGroup group);

    /// <summary>
    /// 更新节点组
    /// </summary>
    Task<bool> UpdateGroupAsync(ProxyNodeGroup group);

    /// <summary>
    /// 删除节点组
    /// </summary>
    Task<bool> DeleteGroupAsync(Guid id);
}

/// <summary>
/// 流量统计模型
/// </summary>
public class TrafficStats
{
    public long UploadBytes { get; set; }
    public long DownloadBytes { get; set; }
    public DateTime LastUpdateTime { get; set; } = DateTime.Now;
}
