using System.Text.Json;
using Microsoft.Extensions.Logging;
using BreezeLink.CoreController.Models;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 节点管理服务。内存列表 + JSON 文件持久化，所有读写都经过同一把锁。
/// </summary>
public class NodeManagementService : INodeManagementService
{
    private readonly ILogger<NodeManagementService> _logger;
    private readonly string _nodesFilePath;
    private readonly string _groupsFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<ProxyNode> _nodes = new();
    private List<ProxyNodeGroup> _groups = new();

    public NodeManagementService(ILogger<NodeManagementService> logger)
    {
        _logger = logger;
        var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        Directory.CreateDirectory(dataDir);
        _nodesFilePath = Path.Combine(dataDir, "nodes.json");
        _groupsFilePath = Path.Combine(dataDir, "groups.json");
        LoadData();
    }

    public async Task<List<ProxyNode>> GetAllNodesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _nodes.OrderBy(n => n.Name).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ProxyNode?> GetNodeByIdAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            return Clone(_nodes.FirstOrDefault(n => n.Id == id));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<ProxyNode>> GetNodesByGroupAsync(Guid? groupId)
    {
        await _lock.WaitAsync();
        try
        {
            var query = groupId.HasValue
                ? _nodes.Where(n => n.GroupId == groupId.Value)
                : _nodes.AsEnumerable();
            return query.OrderBy(n => n.Name).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

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

    public async Task<bool> UpdateNodeAsync(ProxyNode node)
    {
        try
        {
            return await UpdateNodeAsync(node.Id, node) != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update node {NodeName}", node.Name);
            return false;
        }
    }

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

    public async Task<bool> UpdateGroupAsync(ProxyNodeGroup group)
    {
        try
        {
            return await UpdateGroupAsync(group.Id, group) != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update group {GroupName}", group.Name);
            return false;
        }
    }

    public async Task<ProxyNode> CreateNodeAsync(ProxyNode node)
    {
        await _lock.WaitAsync();
        try
        {
            if (node.Id == Guid.Empty)
                node.Id = Guid.NewGuid();
            node.CreatedAt = DateTime.Now;
            node.UpdatedAt = DateTime.Now;

            if (!node.GroupId.HasValue)
            {
                var defaultGroup = _groups.FirstOrDefault(g => g.IsDefault);
                if (defaultGroup != null)
                    node.GroupId = defaultGroup.Id;
            }

            _nodes.Add(node);
            await SaveNodesUnlockedAsync();
            _logger.LogInformation("Created node: {NodeName} ({NodeId})", node.Name, node.Id);
            return node;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ProxyNode?> UpdateNodeAsync(Guid id, ProxyNode node)
    {
        await _lock.WaitAsync();
        try
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

            await SaveNodesUnlockedAsync();
            _logger.LogInformation("Updated node: {NodeName} ({NodeId})", existingNode.Name, existingNode.Id);
            return existingNode;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteNodeAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var node = _nodes.FirstOrDefault(n => n.Id == id);
            if (node == null)
                return false;

            _nodes.Remove(node);
            await SaveNodesUnlockedAsync();
            _logger.LogInformation("Deleted node: {NodeName} ({NodeId})", node.Name, node.Id);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateNodesStatusAsync(List<(Guid NodeId, int Latency, NodeTestStatus Status)> updates)
    {
        await _lock.WaitAsync();
        try
        {
            var changed = false;
            foreach (var (nodeId, latency, status) in updates)
            {
                var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null) continue;
                node.LastLatency = latency;
                node.TestStatus = status;
                node.LastTestTime = DateTime.Now;
                changed = true;
            }

            if (changed)
                await SaveNodesUnlockedAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<ProxyNodeGroup>> GetAllGroupsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _groups.OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ProxyNodeGroup?> GetGroupByIdAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            return _groups.FirstOrDefault(g => g.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ProxyNodeGroup> CreateGroupAsync(ProxyNodeGroup group)
    {
        await _lock.WaitAsync();
        try
        {
            if (group.Id == Guid.Empty)
                group.Id = Guid.NewGuid();
            group.CreatedAt = DateTime.Now;
            group.UpdatedAt = DateTime.Now;

            _groups.Add(group);
            await SaveGroupsUnlockedAsync();
            _logger.LogInformation("Created group: {GroupName} ({GroupId})", group.Name, group.Id);
            return group;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ProxyNodeGroup?> UpdateGroupAsync(Guid id, ProxyNodeGroup group)
    {
        await _lock.WaitAsync();
        try
        {
            var existingGroup = _groups.FirstOrDefault(g => g.Id == id);
            if (existingGroup == null)
                return null;

            existingGroup.Name = group.Name;
            existingGroup.Description = group.Description;
            existingGroup.SortOrder = group.SortOrder;
            existingGroup.UpdatedAt = DateTime.Now;

            await SaveGroupsUnlockedAsync();
            _logger.LogInformation("Updated group: {GroupName} ({GroupId})", existingGroup.Name, existingGroup.Id);
            return existingGroup;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteGroupAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var group = _groups.FirstOrDefault(g => g.Id == id);
            if (group == null)
                return false;

            if (group.IsDefault)
                throw new InvalidOperationException("Cannot delete the default group");

            var nodesInGroup = _nodes.Where(n => n.GroupId == id).ToList();
            var defaultGroup = _groups.FirstOrDefault(g => g.IsDefault && g.Id != id);
            foreach (var node in nodesInGroup)
                node.GroupId = defaultGroup?.Id;

            _groups.Remove(group);
            await SaveGroupsUnlockedAsync();
            if (nodesInGroup.Count > 0)
                await SaveNodesUnlockedAsync();

            _logger.LogInformation("Deleted group: {GroupName} ({GroupId})", group.Name, group.Id);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

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

    public async Task ApplyNodeConfigAsync(NodeConfigRequest request)
    {
        await _lock.WaitAsync();
        try
        {
            if (request.GroupId.HasValue)
            {
                var group = _groups.FirstOrDefault(g => g.Id == request.GroupId.Value);
                if (group == null)
                {
                    group = new ProxyNodeGroup
                    {
                        Id = request.GroupId.Value,
                        Name = "Default Group"
                    };
                    _groups.Add(group);
                    await SaveGroupsUnlockedAsync();
                }
            }

            foreach (var node in request.Nodes)
            {
                node.GroupId = request.GroupId;
                node.UpdatedAt = DateTime.Now;

                var existingNode = node.Id != Guid.Empty
                    ? _nodes.FirstOrDefault(n => n.Id == node.Id)
                    : null;

                if (existingNode != null)
                {
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
                    if (node.Id == Guid.Empty)
                        node.Id = Guid.NewGuid();
                    _nodes.Add(node);
                }
            }

            await SaveNodesUnlockedAsync();
            _logger.LogInformation("Applied node configuration for group {GroupId}", request.GroupId);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void LoadData()
    {
        try
        {
            _nodes = ReadList<ProxyNode>(_nodesFilePath);
            _groups = ReadList<ProxyNodeGroup>(_groupsFilePath);

            var dirtyGroups = _groups.RemoveAll(g => string.IsNullOrWhiteSpace(g.Name));
            var defaultGroups = _groups.Where(g => g.IsDefault).ToList();
            if (defaultGroups.Count > 1)
            {
                foreach (var extra in defaultGroups.Skip(1))
                    extra.IsDefault = false;
            }

            var groupsChanged = dirtyGroups > 0 || defaultGroups.Count > 1;
            if (!_groups.Any(g => g.IsDefault))
            {
                _groups.Add(new ProxyNodeGroup
                {
                    Name = "默认分组",
                    Description = "默认节点分组",
                    IsDefault = true,
                    SortOrder = 0
                });
                groupsChanged = true;
            }

            if (groupsChanged)
                SaveGroupsUnlockedAsync().GetAwaiter().GetResult();

            _logger.LogInformation("Loaded {NodeCount} nodes and {GroupCount} groups", _nodes.Count, _groups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load node data");
        }
    }

    private static List<T> ReadList<T>(string path)
    {
        if (!File.Exists(path))
            return new List<T>();

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();

        return JsonSerializer.Deserialize<List<T>>(json, JsonDefaults.FileOptions) ?? new List<T>();
    }

    private async Task SaveNodesUnlockedAsync()
    {
        await WriteAtomicAsync(_nodesFilePath, _nodes);
    }

    private async Task SaveGroupsUnlockedAsync()
    {
        await WriteAtomicAsync(_groupsFilePath, _groups);
    }

    private async Task WriteAtomicAsync<T>(string path, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, JsonDefaults.FileOptions);
            var temp = path + ".tmp";
            await File.WriteAllTextAsync(temp, json);
            File.Copy(temp, path, overwrite: true);
            File.Delete(temp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {Path}", path);
            throw;
        }
    }

    private static ProxyNode? Clone(ProxyNode? node) => node;
}
