using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using BreezeLink.CoreController.Models;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 节点测试服务实现
/// </summary>
public class NodeTestingService : INodeTestingService
{
    private readonly ILogger<NodeTestingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly INodeManagementService _nodeManagement;

    public NodeTestingService(
        ILogger<NodeTestingService> logger,
        HttpClient httpClient,
        INodeManagementService nodeManagement)
    {
        _logger = logger;
        _httpClient = httpClient;
        _nodeManagement = nodeManagement;
    }

    public Task<List<string>> ValidateNodeAsync(ProxyNode node)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(node.Name))
        {
            errors.Add("Node name is required");
        }

        if (string.IsNullOrWhiteSpace(node.Server))
        {
            errors.Add("Server address is required");
        }

        if (node.Port <= 0 || node.Port > 65535)
        {
            errors.Add("Port must be between 1 and 65535");
        }

        return Task.FromResult(errors);
    }

    public async Task<NodeTestResult> TestNodeAsync(Guid nodeId, int timeout = 5000, string? testUrl = null)
    {
        var node = await _nodeManagement.GetNodeByIdAsync(nodeId);
        if (node == null)
        {
            return new NodeTestResult
            {
                NodeId = nodeId,
                Status = NodeTestStatus.Failed,
                ErrorMessage = "Node not found"
            };
        }

        return await TestNodeAsync(node, timeout);
    }

    /// <summary>
    /// 测试单个节点的连接性
    /// </summary>
    public async Task<NodeTestResult> TestNodeAsync(ProxyNode node, int timeout = 5000)
    {
        var result = new NodeTestResult
        {
            NodeId = node.Id,
            NodeName = node.Name,
            TestTime = DateTime.Now
        };

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 基础连接测试
            if (await TestBasicConnectionAsync(node, timeout))
            {
                result.Status = NodeTestStatus.Success;
                result.Latency = (int)stopwatch.ElapsedMilliseconds;
            }
            else
            {
                result.Status = NodeTestStatus.Failed;
                result.ErrorMessage = "Connection failed";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing node {NodeName}", node.Name);
            result.Status = NodeTestStatus.Failed;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 批量测试节点
    /// </summary>
    public async Task<BatchTestResult> TestNodesAsync(List<ProxyNode> nodes, int timeout = 5000)
    {
        var batchResult = new BatchTestResult
        {
            TestTime = DateTime.Now,
            TotalCount = nodes.Count
        };

        var testTasks = nodes.Select(node => TestNodeAsync(node, timeout));
        var results = await Task.WhenAll(testTasks);

        batchResult.Results = results.ToList();
        batchResult.SuccessCount = results.Count(r => r.Status == NodeTestStatus.Success);
        batchResult.FailedCount = results.Count(r => r.Status == NodeTestStatus.Failed);

        return batchResult;
    }

    public async Task<BatchTestResult> TestNodesAsync(List<Guid> nodeIds, int timeout = 5000, string? testUrl = null)
    {
        var nodes = new List<ProxyNode>();
        foreach (var nodeId in nodeIds)
        {
            var node = await _nodeManagement.GetNodeByIdAsync(nodeId);
            if (node != null)
            {
                nodes.Add(node);
            }
        }

        return await TestNodesAsync(nodes, timeout);
    }

    public async Task<BatchTestResult> TestAllNodesAsync(int timeout = 5000, string? testUrl = null)
    {
        var nodes = await _nodeManagement.GetAllNodesAsync();
        return await TestNodesAsync(nodes, timeout);
    }

    public async Task<List<ProxyNode>> GetBestNodesAsync(int count = 5)
    {
        var nodes = await _nodeManagement.GetAllNodesAsync();
        return nodes
            .Where(n => n.IsActive && n.TestStatus == NodeTestStatus.Success && n.LastLatency >= 0)
            .OrderBy(n => n.LastLatency)
            .Take(count)
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetNodeStatisticsAsync()
    {
        var nodes = await _nodeManagement.GetAllNodesAsync();
        return new Dictionary<string, int>
        {
            ["total"] = nodes.Count,
            ["active"] = nodes.Count(n => n.IsActive),
            ["success"] = nodes.Count(n => n.TestStatus == NodeTestStatus.Success),
            ["failed"] = nodes.Count(n => n.TestStatus == NodeTestStatus.Failed),
            ["untested"] = nodes.Count(n => n.TestStatus == NodeTestStatus.Pending)
        };
    }

    /// <summary>
    /// 测试节点是否可用
    /// </summary>
    public async Task<bool> IsNodeAvailableAsync(ProxyNode node, int timeout = 5000)
    {
        var result = await TestNodeAsync(node, timeout);
        return result.Status == NodeTestStatus.Success;
    }

    private async Task<bool> TestBasicConnectionAsync(ProxyNode node, int timeout)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(node.Server, node.Port);
            var timeoutTask = Task.Delay(timeout);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return false; // 连接超时
            }

            await connectTask; // 确保连接成功
            tcpClient.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
