using System.Text.Json;
using Microsoft.Extensions.Logging;
using BreezeLink.CoreController.Models;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 节点管理服务
/// 负责代理节点的存储、管理和测试
/// </summary>
public class NodeManagementService : INodeManagementService
{
    private readonly ILogger<NodeManagementService> _logger;
    private readonly string _nodesFilePath;
    private readonly string _groupsFilePath;
    private List<ProxyNode> _nodes = new();
    private List<ProxyNodeGroup> _groups = new();

    public NodeManagementService(ILogger<NodeManagementService> logger)
    {
        _logger = logger;
        _nodesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "nodes.json");
        _groupsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "groups.json");

        Directory.CreateDirectory(Path.GetDirectoryName(_nodesFilePath)!);
        LoadData();
    }

    #region 节点管理

    /// <summary>
    /// 获取所有节点
    /// </summary>
    public async Task<List<ProxyNode>> GetAllNodesAsync()
    {
        return await Task.FromResult(_nodes.OrderBy(n => n.Name).ToList());
    }

    /// <summary>
    /// 根据ID获取节点
    /// </summary>
    public async Task<ProxyNode?> GetNodeByIdAsync(Guid id)
    {
        return await Task.FromResult(_nodes.FirstOrDefault(n => n.Id == id));
    }

    /// <summary>
    /// 根据分组获取节点
    /// </summary>
    public async Task<List<ProxyNode>> GetNodesByGroupAsync(Guid? groupId)
    {
        var nodes = groupId.HasValue
            ? _nodes.Where(n => n.GroupId == groupId.Value)
            : _nodes.AsEnumerable();

        return await Task.FromResult(nodes.OrderBy(n => n.Name).ToList());
    }

    /// <summary>
    /// 添加节点
    /// </summary>
    public async Task<bool> AddNodeAsync(ProxyNode node)
    {
        try
        {
            await CreateNodeAsync(node);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add node {NodeName}", node.Name);
            return false;
        }
    }

    /// <summary>
    /// 更新节点
    /// </summary>
    public async Task<bool> UpdateNodeAsync(ProxyNode node)
    {
        try
        {
            var result = await UpdateNodeAsync(node.Id, node);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update node {NodeName}", node.Name);
            return false;
        }
    }

    /// <summary>
    /// 添加节点组
    /// </summary>
    public async Task<bool> AddGroupAsync(ProxyNodeGroup group)
    {
        try
        {
            await CreateGroupAsync(group);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add group {GroupName}", group.Name);
            return false;
        }
    }

    /// <summary>
    /// 更新节点组
    /// </summary>
    public async Task<bool> UpdateGroupAsync(ProxyNodeGroup group)
    {
        try
        {
            var result = await UpdateGroupAsync(group.Id, group);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update group {GroupName}", group.Name);
            return false;
        }
    }

    /// <summary>
    /// 创建节点
    /// </summary>
    public async Task<ProxyNode> CreateNodeAsync(ProxyNode node)
    {
        node.Id = Guid.NewGuid();
        node.CreatedAt = DateTime.Now;
        node.UpdatedAt = DateTime.Now;

        _nodes.Add(node);
        await SaveNodesAsync();

        _logger.LogInformation("Created node: {NodeName} ({NodeId})", node.Name, node.Id);
        return node;
    }

    /// <summary>
    /// 更新节点
    /// </summary>
    public async Task<ProxyNode?> UpdateNodeAsync(Guid id, ProxyNode node)
    {
        var existingNode = _nodes.FirstOrDefault(n => n.Id == id);
        if (existingNode == null)
            return null;

        existingNode.Name = node.Name;
        existingNode.Type = node.Type;
        existingNode.Server = node.Server;
        existingNode.Port = node.Port;
        existingNode.Username = node.Username;
        existingNode.Password = node.Password;
        existingNode.Method = node.Method;
        existingNode.UUID = node.UUID;
        existingNode.AlterId = node.AlterId;
        existingNode.Security = node.Security;
        existingNode.SNI = node.SNI;
        existingNode.Alpn = node.Alpn;
        existingNode.AllowInsecure = node.AllowInsecure;
        existingNode.SkipCertVerify = node.SkipCertVerify;
        existingNode.Tag = node.Tag;
        existingNode.GroupId = node.GroupId;
        existingNode.IsActive = node.IsActive;
        existingNode.UpdatedAt = DateTime.Now;

        await SaveNodesAsync();

        _logger.LogInformation("Updated node: {NodeName} ({NodeId})", existingNode.Name, existingNode.Id);
        return existingNode;
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    public async Task<bool> DeleteNodeAsync(Guid id)
    {
        var node = _nodes.FirstOrDefault(n => n.Id == id);
        if (node == null)
            return false;

        _nodes.Remove(node);
        await SaveNodesAsync();

        _logger.LogInformation("Deleted node: {NodeName} ({NodeId})", node.Name, node.Id);
        return true;
    }

    /// <summary>
    /// 批量更新节点状态
    /// </summary>
    public async Task UpdateNodesStatusAsync(List<(Guid NodeId, int Latency, NodeTestStatus Status)> updates)
    {
        foreach (var (nodeId, latency, status) in updates)
        {
            var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.LastLatency = latency;
                node.TestStatus = status;
                node.LastTestTime = DateTime.Now;
            }
        }

        await SaveNodesAsync();
    }

    #endregion

    #region 分组管理

    /// <summary>
    /// 获取所有分组
    /// </summary>
    public async Task<List<ProxyNodeGroup>> GetAllGroupsAsync()
    {
        return await Task.FromResult(_groups.OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToList());
    }

    /// <summary>
    /// 根据ID获取分组
    /// </summary>
    public async Task<ProxyNodeGroup?> GetGroupByIdAsync(Guid id)
    {
        return await Task.FromResult(_groups.FirstOrDefault(g => g.Id == id));
    }

    /// <summary>
    /// 创建分组
    /// </summary>
    public async Task<ProxyNodeGroup> CreateGroupAsync(ProxyNodeGroup group)
    {
        group.Id = Guid.NewGuid();
        group.CreatedAt = DateTime.Now;
        group.UpdatedAt = DateTime.Now;

        _groups.Add(group);
        await SaveGroupsAsync();

        _logger.LogInformation("Created group: {GroupName} ({GroupId})", group.Name, group.Id);
        return group;
    }

    /// <summary>
    /// 更新分组
    /// </summary>
    public async Task<ProxyNodeGroup?> UpdateGroupAsync(Guid id, ProxyNodeGroup group)
    {
        var existingGroup = _groups.FirstOrDefault(g => g.Id == id);
        if (existingGroup == null)
            return null;

        existingGroup.Name = group.Name;
        existingGroup.Description = group.Description;
        existingGroup.SortOrder = group.SortOrder;
        existingGroup.UpdatedAt = DateTime.Now;

        await SaveGroupsAsync();

        _logger.LogInformation("Updated group: {GroupName} ({GroupId})", existingGroup.Name, existingGroup.Id);
        return existingGroup;
    }

    /// <summary>
    /// 删除分组
    /// </summary>
    public async Task<bool> DeleteGroupAsync(Guid id)
    {
        var group = _groups.FirstOrDefault(g => g.Id == id);
        if (group == null)
            return false;

        // 将该分组的节点移到默认分组或取消分组
        var nodesInGroup = _nodes.Where(n => n.GroupId == id).ToList();
        foreach (var node in nodesInGroup)
        {
            node.GroupId = null;
        }

        _groups.Remove(group);
        await SaveGroupsAsync();

        _logger.LogInformation("Deleted group: {GroupName} ({GroupId})", group.Name, group.Id);
        return true;
    }

    #endregion

    #region 配置管理

    /// <summary>
    /// 获取节点配置
    /// </summary>
    public async Task<NodeConfigResponse> GetNodeConfigAsync(Guid? groupId = null)
    {
        var nodes = await GetNodesByGroupAsync(groupId);
        var group = groupId.HasValue ? await GetGroupByIdAsync(groupId.Value) : null;

        return new NodeConfigResponse
        {
            Group = group,
            Nodes = nodes,
            TotalCount = nodes.Count,
            ActiveCount = nodes.Count(n => n.IsActive)
        };
    }

    /// <summary>
    /// 应用节点配置
    /// </summary>
    public async Task ApplyNodeConfigAsync(NodeConfigRequest request)
    {
        if (request.GroupId.HasValue)
        {
            // 更新或创建分组
            var group = await GetGroupByIdAsync(request.GroupId.Value);
            if (group == null)
            {
                group = new ProxyNodeGroup { Name = "Default Group" };
                await CreateGroupAsync(group);
            }
        }

        // 更新节点分组
        foreach (var node in request.Nodes)
        {
            node.GroupId = request.GroupId;
            node.UpdatedAt = DateTime.Now;

            var existingNode = _nodes.FirstOrDefault(n => n.Id == node.Id);
            if (existingNode != null)
            {
                // 更新现有节点
                existingNode.Name = node.Name;
                existingNode.Type = node.Type;
                existingNode.Server = node.Server;
                existingNode.Port = node.Port;
                existingNode.GroupId = node.GroupId;
                existingNode.IsActive = node.IsActive;
                existingNode.UpdatedAt = DateTime.Now;
            }
            else
            {
                // 添加新节点
                _nodes.Add(node);
            }
        }

        await SaveNodesAsync();
        _logger.LogInformation("Applied node configuration for group {GroupId}", request.GroupId);
    }

    #endregion

    #region 数据持久化

    private void LoadData()
    {
        try
        {
            // 加载节点数据
            if (File.Exists(_nodesFilePath))
            {
                var nodesJson = File.ReadAllText(_nodesFilePath);
                _nodes = JsonSerializer.Deserialize<List<ProxyNode>>(nodesJson) ?? new List<ProxyNode>();
                _logger.LogInformation("Loaded {NodeCount} nodes", _nodes.Count);
            }

            // 加载分组数据
            if (File.Exists(_groupsFilePath))
            {
                var groupsJson = File.ReadAllText(_groupsFilePath);
                _groups = JsonSerializer.Deserialize<List<ProxyNodeGroup>>(groupsJson) ?? new List<ProxyNodeGroup>();
                _logger.LogInformation("Loaded {GroupCount} groups", _groups.Count);
            }

            // 确保有默认分组
            if (!_groups.Any(g => g.IsDefault))
            {
                var defaultGroup = new ProxyNodeGroup
                {
                    Name = "默认分组",
                    Description = "默认节点分组",
                    IsDefault = true,
                    SortOrder = 0
                };
                _groups.Add(defaultGroup);
                SaveGroupsAsync().Wait();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load node data");
        }
    }

    private async Task SaveNodesAsync()
    {
        try
        {
            var nodesJson = JsonSerializer.Serialize(_nodes, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_nodesFilePath, nodesJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save nodes data");
        }
    }

    private async Task SaveGroupsAsync()
    {
        try
        {
            var groupsJson = JsonSerializer.Serialize(_groups, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(_groupsFilePath, groupsJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save groups data");
        }
    }

    #endregion
}
