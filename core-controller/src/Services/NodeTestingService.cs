using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using BreezeLink.CoreController.Models;

namespace BreezeLink.CoreController.Services;

public class NodeTestingService : INodeTestingService
{
    private const int MaxConcurrency = 8;
    private readonly ILogger<NodeTestingService> _logger;
    private readonly INodeManagementService _nodeManagement;

    public NodeTestingService(
        ILogger<NodeTestingService> logger,
        INodeManagementService nodeManagement)
    {
        _logger = logger;
        _nodeManagement = nodeManagement;
    }

    public Task<List<string>> ValidateNodeAsync(ProxyNode node)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(node.Name))
            errors.Add("节点名称不能为空");

        if (string.IsNullOrWhiteSpace(node.Server))
            errors.Add("服务器地址不能为空");

        if (node.Port is <= 0 or > 65535)
            errors.Add("端口必须在 1 到 65535 之间");

        switch (node.Type)
        {
            case ProxyNodeType.Shadowsocks:
            case ProxyNodeType.ShadowsocksR:
            case ProxyNodeType.Trojan:
            case ProxyNodeType.Hysteria:
            case ProxyNodeType.Hysteria2:
                if (string.IsNullOrWhiteSpace(node.Password))
                    errors.Add("该协议需要填写密码");
                break;
            case ProxyNodeType.VMess:
            case ProxyNodeType.VLESS:
            case ProxyNodeType.TUIC:
                if (string.IsNullOrWhiteSpace(node.UUID))
                    errors.Add("该协议需要填写 UUID");
                break;
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
                ErrorMessage = "节点不存在"
            };
        }

        return await TestNodeAsync(node, timeout);
    }

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
            var (ok, timedOut, error) = await TestBasicConnectionAsync(node, timeout);
            stopwatch.Stop();

            if (ok)
            {
                result.Status = NodeTestStatus.Success;
                result.Latency = (int)stopwatch.ElapsedMilliseconds;
            }
            else if (timedOut)
            {
                result.Status = NodeTestStatus.Timeout;
                result.ErrorMessage = "连接超时";
            }
            else
            {
                result.Status = NodeTestStatus.Failed;
                result.ErrorMessage = error ?? "连接失败";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing node {NodeName}", node.Name);
            result.Status = NodeTestStatus.Failed;
            result.ErrorMessage = ex.Message;
        }

        try
        {
            await _nodeManagement.UpdateNodesStatusAsync(
            [
                (node.Id, result.Latency, result.Status)
            ]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist test result for {NodeName}", node.Name);
        }

        return result;
    }

    public async Task<BatchTestResult> TestNodesAsync(List<ProxyNode> nodes, int timeout = 5000)
    {
        var batchResult = new BatchTestResult
        {
            TestTime = DateTime.Now,
            TotalCount = nodes.Count
        };

        using var semaphore = new SemaphoreSlim(MaxConcurrency);
        var tasks = nodes.Select(async node =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await TestNodeAsync(node, timeout);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        batchResult.Results = results.ToList();
        batchResult.SuccessCount = results.Count(r => r.Status == NodeTestStatus.Success);
        batchResult.FailedCount = results.Count(r => r.Status is NodeTestStatus.Failed or NodeTestStatus.Timeout);
        return batchResult;
    }

    public async Task<BatchTestResult> TestNodesAsync(List<Guid> nodeIds, int timeout = 5000, string? testUrl = null)
    {
        var nodes = new List<ProxyNode>();
        foreach (var nodeId in nodeIds)
        {
            var node = await _nodeManagement.GetNodeByIdAsync(nodeId);
            if (node != null)
                nodes.Add(node);
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
            .Take(Math.Max(1, count))
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
            ["timeout"] = nodes.Count(n => n.TestStatus == NodeTestStatus.Timeout),
            ["untested"] = nodes.Count(n => n.TestStatus == NodeTestStatus.Pending)
        };
    }

    public async Task<bool> IsNodeAvailableAsync(ProxyNode node, int timeout = 5000)
    {
        var result = await TestNodeAsync(node, timeout);
        return result.Status == NodeTestStatus.Success;
    }

    private static async Task<(bool Ok, bool TimedOut, string? Error)> TestBasicConnectionAsync(ProxyNode node, int timeout)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Math.Max(500, timeout)));
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(node.Server, node.Port, cts.Token);
            return (tcpClient.Connected, false, null);
        }
        catch (OperationCanceledException)
        {
            return (false, true, "连接超时");
        }
        catch (Exception ex)
        {
            return (false, false, ex.Message);
        }
    }
}
