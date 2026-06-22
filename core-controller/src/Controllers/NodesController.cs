using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BreezeLink.CoreController.Models;
using BreezeLink.CoreController.Services;

namespace BreezeLink.CoreController.Controllers;

/// <summary>
/// 节点管理控制器
/// 提供代理节点的 CRUD 操作和测试功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly INodeManagementService _nodeManagement;
    private readonly INodeTestingService _nodeTesting;
    private readonly ILogger<NodesController> _logger;

    public NodesController(
        INodeManagementService nodeManagement,
        INodeTestingService nodeTesting,
        ILogger<NodesController> logger)
    {
        _nodeManagement = nodeManagement;
        _nodeTesting = nodeTesting;
        _logger = logger;
    }

    #region 节点管理

    /// <summary>
    /// 获取所有节点
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllNodes([FromQuery] Guid? groupId = null)
    {
        try
        {
            var nodes = await _nodeManagement.GetNodesByGroupAsync(groupId);
            return Ok(ApiResponse<List<ProxyNode>>.Ok(nodes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get nodes");
            return StatusCode(500, ApiResponse<List<ProxyNode>>.Error($"Failed to get nodes: {ex.Message}"));
        }
    }

    /// <summary>
    /// 根据ID获取节点
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNodeById(Guid id)
    {
        try
        {
            var node = await _nodeManagement.GetNodeByIdAsync(id);
            if (node == null)
            {
                return NotFound(ApiResponse<ProxyNode>.Error("Node not found"));
            }

            return Ok(ApiResponse<ProxyNode>.Ok(node));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get node {NodeId}", id);
            return StatusCode(500, ApiResponse<ProxyNode>.Error($"Failed to get node: {ex.Message}"));
        }
    }

    /// <summary>
    /// 创建节点
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateNode([FromBody] ProxyNode node)
    {
        try
        {
            // 验证节点配置
            var validationErrors = await _nodeTesting.ValidateNodeAsync(node);
            if (validationErrors.Any())
            {
                return BadRequest(ApiResponse<ProxyNode>.Error($"Validation failed: {string.Join(", ", validationErrors)}"));
            }

            var createdNode = await _nodeManagement.CreateNodeAsync(node);
            return CreatedAtAction(nameof(GetNodeById), new { id = createdNode.Id }, ApiResponse<ProxyNode>.Ok(createdNode, "Node created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create node");
            return StatusCode(500, ApiResponse<ProxyNode>.Error($"Failed to create node: {ex.Message}"));
        }
    }

    /// <summary>
    /// 更新节点
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNode(Guid id, [FromBody] ProxyNode node)
    {
        try
        {
            // 验证节点配置
            var validationErrors = await _nodeTesting.ValidateNodeAsync(node);
            if (validationErrors.Any())
            {
                return BadRequest(ApiResponse<ProxyNode>.Error($"Validation failed: {string.Join(", ", validationErrors)}"));
            }

            var updatedNode = await _nodeManagement.UpdateNodeAsync(id, node);
            if (updatedNode == null)
            {
                return NotFound(ApiResponse<ProxyNode>.Error("Node not found"));
            }

            return Ok(ApiResponse<ProxyNode>.Ok(updatedNode, "Node updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update node {NodeId}", id);
            return StatusCode(500, ApiResponse<ProxyNode>.Error($"Failed to update node: {ex.Message}"));
        }
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNode(Guid id)
    {
        try
        {
            var result = await _nodeManagement.DeleteNodeAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<object>.Error("Node not found"));
            }

            return Ok(ApiResponse<object>.Ok(null, "Node deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete node {NodeId}", id);
            return StatusCode(500, ApiResponse<object>.Error($"Failed to delete node: {ex.Message}"));
        }
    }

    /// <summary>
    /// 批量删除节点
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteNodes([FromBody] List<Guid> nodeIds)
    {
        try
        {
            var deleteTasks = nodeIds.Select(id => _nodeManagement.DeleteNodeAsync(id));
            var results = await Task.WhenAll(deleteTasks);

            var successCount = results.Count(r => r);
            var failCount = results.Length - successCount;

            var message = $"Deleted {successCount} nodes";
            if (failCount > 0)
            {
                message += $", {failCount} failed";
            }

            return Ok(ApiResponse<object>.Ok(null, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete nodes");
            return StatusCode(500, ApiResponse<object>.Error($"Failed to delete nodes: {ex.Message}"));
        }
    }

    #endregion

    #region 分组管理

    /// <summary>
    /// 获取所有分组
    /// </summary>
    [HttpGet("groups")]
    public async Task<IActionResult> GetAllGroups()
    {
        try
        {
            var groups = await _nodeManagement.GetAllGroupsAsync();
            return Ok(ApiResponse<List<ProxyNodeGroup>>.Ok(groups));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get groups");
            return StatusCode(500, ApiResponse<List<ProxyNodeGroup>>.Error($"Failed to get groups: {ex.Message}"));
        }
    }

    /// <summary>
    /// 根据ID获取分组
    /// </summary>
    [HttpGet("groups/{id}")]
    public async Task<IActionResult> GetGroupById(Guid id)
    {
        try
        {
            var group = await _nodeManagement.GetGroupByIdAsync(id);
            if (group == null)
            {
                return NotFound(ApiResponse<ProxyNodeGroup>.Error("Group not found"));
            }

            return Ok(ApiResponse<ProxyNodeGroup>.Ok(group));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get group {GroupId}", id);
            return StatusCode(500, ApiResponse<ProxyNodeGroup>.Error($"Failed to get group: {ex.Message}"));
        }
    }

    /// <summary>
    /// 创建分组
    /// </summary>
    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] ProxyNodeGroup group)
    {
        try
        {
            var createdGroup = await _nodeManagement.CreateGroupAsync(group);
            return CreatedAtAction(nameof(GetGroupById), new { id = createdGroup.Id }, ApiResponse<ProxyNodeGroup>.Ok(createdGroup, "Group created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create group");
            return StatusCode(500, ApiResponse<ProxyNodeGroup>.Error($"Failed to create group: {ex.Message}"));
        }
    }

    /// <summary>
    /// 更新分组
    /// </summary>
    [HttpPut("groups/{id}")]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] ProxyNodeGroup group)
    {
        try
        {
            var updatedGroup = await _nodeManagement.UpdateGroupAsync(id, group);
            if (updatedGroup == null)
            {
                return NotFound(ApiResponse<ProxyNodeGroup>.Error("Group not found"));
            }

            return Ok(ApiResponse<ProxyNodeGroup>.Ok(updatedGroup, "Group updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update group {GroupId}", id);
            return StatusCode(500, ApiResponse<ProxyNodeGroup>.Error($"Failed to update group: {ex.Message}"));
        }
    }

    /// <summary>
    /// 删除分组
    /// </summary>
    [HttpDelete("groups/{id}")]
    public async Task<IActionResult> DeleteGroup(Guid id)
    {
        try
        {
            var result = await _nodeManagement.DeleteGroupAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<object>.Error("Group not found"));
            }

            return Ok(ApiResponse<object>.Ok(null, "Group deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete group {GroupId}", id);
            return StatusCode(500, ApiResponse<object>.Error($"Failed to delete group: {ex.Message}"));
        }
    }

    #endregion

    #region 配置管理

    /// <summary>
    /// 获取节点配置
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetNodeConfig([FromQuery] Guid? groupId = null)
    {
        try
        {
            var config = await _nodeManagement.GetNodeConfigAsync(groupId);
            return Ok(ApiResponse<NodeConfigResponse>.Ok(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get node config");
            return StatusCode(500, ApiResponse<NodeConfigResponse>.Error($"Failed to get config: {ex.Message}"));
        }
    }

    /// <summary>
    /// 应用节点配置
    /// </summary>
    [HttpPost("config")]
    public async Task<IActionResult> ApplyNodeConfig([FromBody] NodeConfigRequest request)
    {
        try
        {
            await _nodeManagement.ApplyNodeConfigAsync(request);
            return Ok(ApiResponse<object>.Ok(null, "Configuration applied successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply node config");
            return StatusCode(500, ApiResponse<object>.Error($"Failed to apply config: {ex.Message}"));
        }
    }

    #endregion

    #region 节点测试

    /// <summary>
    /// 测试单个节点
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestNode(Guid id, [FromBody] NodeTestRequest? request = null)
    {
        try
        {
            var timeout = request?.Timeout ?? 5000;
            var testUrl = request?.TestUrl ?? "http://www.gstatic.com/generate_204";

            var result = await _nodeTesting.TestNodeAsync(id, timeout, testUrl);
            return Ok(ApiResponse<NodeTestResult>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test node {NodeId}", id);
            return StatusCode(500, ApiResponse<NodeTestResult>.Error($"Failed to test node: {ex.Message}"));
        }
    }

    /// <summary>
    /// 批量测试节点
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestNodes([FromBody] NodeTestRequest request)
    {
        try
        {
            var result = await _nodeTesting.TestNodesAsync(request.NodeIds, request.Timeout, request.TestUrl);
            return Ok(ApiResponse<BatchTestResult>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test nodes");
            return StatusCode(500, ApiResponse<BatchTestResult>.Error($"Failed to test nodes: {ex.Message}"));
        }
    }

    /// <summary>
    /// 测试所有节点
    /// </summary>
    [HttpPost("test/all")]
    public async Task<IActionResult> TestAllNodes([FromBody] NodeTestRequest? request = null)
    {
        try
        {
            var timeout = request?.Timeout ?? 5000;
            var testUrl = request?.TestUrl ?? "http://www.gstatic.com/generate_204";

            var result = await _nodeTesting.TestAllNodesAsync(timeout, testUrl);
            return Ok(ApiResponse<BatchTestResult>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test all nodes");
            return StatusCode(500, ApiResponse<BatchTestResult>.Error($"Failed to test all nodes: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取最佳节点
    /// </summary>
    [HttpGet("best")]
    public async Task<IActionResult> GetBestNodes([FromQuery] int count = 5)
    {
        try
        {
            var nodes = await _nodeTesting.GetBestNodesAsync(count);
            return Ok(ApiResponse<List<ProxyNode>>.Ok(nodes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get best nodes");
            return StatusCode(500, ApiResponse<List<ProxyNode>>.Error($"Failed to get best nodes: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取节点统计信息
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _nodeTesting.GetNodeStatisticsAsync();
            return Ok(ApiResponse<Dictionary<string, int>>.Ok(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get node statistics");
            return StatusCode(500, ApiResponse<Dictionary<string, int>>.Error($"Failed to get statistics: {ex.Message}"));
        }
    }

    #endregion
}
