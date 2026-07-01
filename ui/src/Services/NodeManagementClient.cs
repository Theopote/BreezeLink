using System.Net.Http.Json;
using System.Text.Json;
using BreezeLink.CoreController.Models;
using Microsoft.Extensions.Logging;

namespace BreezeLink.UI.Services;

/// <summary>
/// 节点管理服务客户端
/// 负责与后端节点管理 API 通信
/// </summary>
public class NodeManagementClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NodeManagementClient> _logger;

    public NodeManagementClient(HttpClient httpClient, ILogger<NodeManagementClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://127.0.0.1:8800");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    #region 节点管理

    /// <summary>
    /// 获取所有节点
    /// </summary>
    public async Task<ApiResponse<List<ProxyNode>>?> GetAllNodesAsync(Guid? groupId = null)
    {
        try
        {
            var url = "/api/nodes";
            if (groupId.HasValue)
            {
                url += $"?groupId={groupId.Value}";
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<List<ProxyNode>>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProxyNode>>>();
                _logger.LogError("Failed to get nodes: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting nodes");
            return ApiResponse<List<ProxyNode>>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据ID获取节点
    /// </summary>
    public async Task<ApiResponse<ProxyNode>?> GetNodeByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/nodes/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNode>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNode>>();
                _logger.LogError("Failed to get node {NodeId}: {Error}", id, errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting node {NodeId}", id);
            return ApiResponse<ProxyNode>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建节点
    /// </summary>
    public async Task<ApiResponse<ProxyNode>?> CreateNodeAsync(ProxyNode node)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/nodes", node);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNode>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNode>>();
                _logger.LogError("Failed to create node: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating node");
            return ApiResponse<ProxyNode>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新节点
    /// </summary>
    public async Task<ApiResponse<ProxyNode>?> UpdateNodeAsync(Guid id, ProxyNode node)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/nodes/{id}", node);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNode>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNode>>();
                _logger.LogError("Failed to update node {NodeId}: {Error}", id, errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating node {NodeId}", id);
            return ApiResponse<ProxyNode>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    public async Task<ApiResponse<object>?> DeleteNodeAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/nodes/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                _logger.LogError("Failed to delete node {NodeId}: {Error}", id, errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting node {NodeId}", id);
            return ApiResponse<object>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 批量删除节点
    /// </summary>
    public async Task<ApiResponse<object>?> DeleteNodesAsync(List<Guid> nodeIds)
    {
        try
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri("/api/nodes", UriKind.Relative),
                Content = JsonContent.Create(nodeIds)
            });

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                _logger.LogError("Failed to delete nodes: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting nodes");
            return ApiResponse<object>.Error($"Connection error: {ex.Message}");
        }
    }

    #endregion

    #region 分组管理

    /// <summary>
    /// 获取所有分组
    /// </summary>
    public async Task<ApiResponse<List<ProxyNodeGroup>>?> GetAllGroupsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/nodes/groups");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<List<ProxyNodeGroup>>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProxyNodeGroup>>>();
                _logger.LogError("Failed to get groups: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups");
            return ApiResponse<List<ProxyNodeGroup>>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建分组
    /// </summary>
    public async Task<ApiResponse<ProxyNodeGroup>?> CreateGroupAsync(ProxyNodeGroup group)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/nodes/groups", group);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNodeGroup>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNodeGroup>>();
                _logger.LogError("Failed to create group: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return ApiResponse<ProxyNodeGroup>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新分组
    /// </summary>
    public async Task<ApiResponse<ProxyNodeGroup>?> UpdateGroupAsync(Guid id, ProxyNodeGroup group)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/nodes/groups/{id}", group);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNodeGroup>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<ProxyNodeGroup>>();
                _logger.LogError("Failed to update group {GroupId}: {Error}", id, errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", id);
            return ApiResponse<ProxyNodeGroup>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除分组
    /// </summary>
    public async Task<ApiResponse<object>?> DeleteGroupAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/nodes/groups/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                _logger.LogError("Failed to delete group {GroupId}: {Error}", id, errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", id);
            return ApiResponse<object>.Error($"Connection error: {ex.Message}");
        }
    }

    #endregion

    #region 配置管理

    /// <summary>
    /// 获取节点配置
    /// </summary>
    public async Task<ApiResponse<NodeConfigResponse>?> GetNodeConfigAsync(Guid? groupId = null)
    {
        try
        {
            var url = "/api/nodes/config";
            if (groupId.HasValue)
            {
                url += $"?groupId={groupId.Value}";
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<NodeConfigResponse>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<NodeConfigResponse>>();
                _logger.LogError("Failed to get node config: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting node config");
            return ApiResponse<NodeConfigResponse>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用节点配置
    /// </summary>
    public async Task<ApiResponse<object>?> ApplyNodeConfigAsync(NodeConfigRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/nodes/config", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                _logger.LogError("Failed to apply node config: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying node config");
            return ApiResponse<object>.Error($"Connection error: {ex.Message}");
        }
    }

    #endregion

    #region 节点测试

    /// <summary>
    /// 测试单个节点
    /// </summary>
    public async Task<ApiResponse<NodeTestResult>?> TestNodeAsync(Guid nodeId, NodeTestRequest? request = null)
    {
        try
        {
            var testRequest = request ?? new NodeTestRequest();
            var response = await _httpClient.PostAsJsonAsync($"/api/nodes/{nodeId}/test", testRequest);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<NodeTestResult>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<NodeTestResult>>();
                _logger.LogError("Failed to test node {NodeId}: {Error}", nodeId, errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing node {NodeId}", nodeId);
            return ApiResponse<NodeTestResult>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 批量测试节点
    /// </summary>
    public async Task<ApiResponse<BatchTestResult>?> TestNodesAsync(NodeTestRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/nodes/test", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<BatchTestResult>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BatchTestResult>>();
                _logger.LogError("Failed to test nodes: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing nodes");
            return ApiResponse<BatchTestResult>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试所有节点
    /// </summary>
    public async Task<ApiResponse<BatchTestResult>?> TestAllNodesAsync(NodeTestRequest? request = null)
    {
        try
        {
            var testRequest = request ?? new NodeTestRequest();
            var response = await _httpClient.PostAsJsonAsync("/api/nodes/test/all", testRequest);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<BatchTestResult>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BatchTestResult>>();
                _logger.LogError("Failed to test all nodes: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing all nodes");
            return ApiResponse<BatchTestResult>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取最佳节点
    /// </summary>
    public async Task<ApiResponse<List<ProxyNode>>?> GetBestNodesAsync(int count = 5)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/nodes/best?count={count}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<List<ProxyNode>>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProxyNode>>>();
                _logger.LogError("Failed to get best nodes: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting best nodes");
            return ApiResponse<List<ProxyNode>>.Error($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取节点统计信息
    /// </summary>
    public async Task<ApiResponse<Dictionary<string, int>>?> GetStatisticsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/nodes/statistics");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<Dictionary<string, int>>>();
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Dictionary<string, int>>>();
                _logger.LogError("Failed to get statistics: {Error}", errorResponse?.Message);
                return errorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return ApiResponse<Dictionary<string, int>>.Error($"Connection error: {ex.Message}");
        }
    }

    #endregion
}
