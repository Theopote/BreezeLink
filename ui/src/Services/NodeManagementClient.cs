using System.Net.Http.Json;
using BreezeLink.CoreController.Models;
using Microsoft.Extensions.Logging;

namespace BreezeLink.UI.Services;

public class NodeManagementClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NodeManagementClient> _logger;

    public NodeManagementClient(HttpClient httpClient, ILogger<NodeManagementClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress ??= new Uri("http://127.0.0.1:8800");
        if (_httpClient.Timeout == Timeout.InfiniteTimeSpan)
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public Task<ApiResponse<List<ProxyNode>>?> GetAllNodesAsync(Guid? groupId = null)
        => GetAsync<List<ProxyNode>>(groupId.HasValue ? $"/api/nodes?groupId={groupId.Value}" : "/api/nodes", "get nodes");

    public Task<ApiResponse<ProxyNode>?> GetNodeByIdAsync(Guid id)
        => GetAsync<ProxyNode>($"/api/nodes/{id}", "get node");

    public Task<ApiResponse<ProxyNode>?> CreateNodeAsync(ProxyNode node)
        => SendAsync<ProxyNode, ProxyNode>(HttpMethod.Post, "/api/nodes", node, "create node");

    public Task<ApiResponse<ProxyNode>?> UpdateNodeAsync(Guid id, ProxyNode node)
        => SendAsync<ProxyNode, ProxyNode>(HttpMethod.Put, $"/api/nodes/{id}", node, "update node");

    public Task<ApiResponse<object>?> DeleteNodeAsync(Guid id)
        => SendAsync<object, object>(HttpMethod.Delete, $"/api/nodes/{id}", null, "delete node");

    public Task<ApiResponse<object>?> DeleteNodesAsync(List<Guid> nodeIds)
        => SendAsync<List<Guid>, object>(HttpMethod.Delete, "/api/nodes", nodeIds, "delete nodes");

    public Task<ApiResponse<List<ProxyNodeGroup>>?> GetAllGroupsAsync()
        => GetAsync<List<ProxyNodeGroup>>("/api/nodes/groups", "get groups");

    public Task<ApiResponse<ProxyNodeGroup>?> CreateGroupAsync(ProxyNodeGroup group)
        => SendAsync<ProxyNodeGroup, ProxyNodeGroup>(HttpMethod.Post, "/api/nodes/groups", group, "create group");

    public Task<ApiResponse<ProxyNodeGroup>?> UpdateGroupAsync(Guid id, ProxyNodeGroup group)
        => SendAsync<ProxyNodeGroup, ProxyNodeGroup>(HttpMethod.Put, $"/api/nodes/groups/{id}", group, "update group");

    public Task<ApiResponse<object>?> DeleteGroupAsync(Guid id)
        => SendAsync<object, object>(HttpMethod.Delete, $"/api/nodes/groups/{id}", null, "delete group");

    public Task<ApiResponse<NodeConfigResponse>?> GetNodeConfigAsync(Guid? groupId = null)
        => GetAsync<NodeConfigResponse>(groupId.HasValue ? $"/api/nodes/config?groupId={groupId.Value}" : "/api/nodes/config", "get node config");

    public Task<ApiResponse<object>?> ApplyNodeConfigAsync(NodeConfigRequest request)
        => SendAsync<NodeConfigRequest, object>(HttpMethod.Post, "/api/nodes/config", request, "apply node config");

    public Task<ApiResponse<NodeTestResult>?> TestNodeAsync(Guid nodeId, NodeTestRequest? request = null)
        => SendAsync<NodeTestRequest, NodeTestResult>(HttpMethod.Post, $"/api/nodes/{nodeId}/test", request ?? new NodeTestRequest(), "test node");

    public Task<ApiResponse<BatchTestResult>?> TestNodesAsync(NodeTestRequest request)
        => SendAsync<NodeTestRequest, BatchTestResult>(HttpMethod.Post, "/api/nodes/test", request, "test nodes");

    public Task<ApiResponse<BatchTestResult>?> TestAllNodesAsync(NodeTestRequest? request = null)
        => SendAsync<NodeTestRequest, BatchTestResult>(HttpMethod.Post, "/api/nodes/test/all", request ?? new NodeTestRequest(), "test all nodes");

    public Task<ApiResponse<List<ProxyNode>>?> GetBestNodesAsync(int count = 5)
        => GetAsync<List<ProxyNode>>($"/api/nodes/best?count={count}", "get best nodes");

    public Task<ApiResponse<Dictionary<string, int>>?> GetStatisticsAsync()
        => GetAsync<Dictionary<string, int>>("/api/nodes/statistics", "get statistics");

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

    private async Task<ApiResponse<TResponse>?> SendAsync<TRequest, TResponse>(HttpMethod method, string url, TRequest? body, string action)
    {
        try
        {
            var request = new HttpRequestMessage(method, url);
            if (body is not null)
                request.Content = JsonContent.Create(body, options: JsonOptions.Default);

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
