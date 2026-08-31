using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BreezeLink.UI.Services;
using BreezeLink.CoreController.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace BreezeLink.UI.ViewModels;

public partial class NodeManagementViewModel : ObservableObject
{
    private readonly NodeManagementClient _nodeClient;
    private readonly NotificationService _notificationService;
    private readonly ILogger<NodeManagementViewModel> _logger;
    private List<ProxyNode> _allNodes = new();

    [ObservableProperty]
    private ObservableCollection<ProxyNode> nodes = new();

    [ObservableProperty]
    private ObservableCollection<ProxyNodeGroup> groups = new();

    [ObservableProperty]
    private ProxyNode? selectedNode;

    [ObservableProperty]
    private ProxyNodeGroup? selectedGroup;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ProxyNodeType selectedNodeType = ProxyNodeType.Shadowsocks;

    [ObservableProperty]
    private Dictionary<string, int> statistics = new();

    [ObservableProperty]
    private string newNodeName = string.Empty;

    [ObservableProperty]
    private string newNodeServer = string.Empty;

    [ObservableProperty]
    private double newNodePort = 443;

    [ObservableProperty]
    private string newNodePassword = string.Empty;

    [ObservableProperty]
    private string newNodeMethod = "aes-256-gcm";

    [ObservableProperty]
    private string newNodeUUID = string.Empty;

    public IReadOnlyList<ProxyNodeType> NodeTypes { get; } = Enum.GetValues<ProxyNodeType>();

    public string StatisticsText =>
        Statistics.Count == 0
            ? "暂无统计"
            : $"共 {GetStat("total")} 个节点，可用 {GetStat("success")}，失败 {GetStat("failed")}，未测 {GetStat("untested")}";

    public NodeManagementViewModel(
        NodeManagementClient nodeClient,
        NotificationService notificationService,
        ILogger<NodeManagementViewModel> logger)
    {
        _nodeClient = nodeClient;
        _notificationService = notificationService;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var groupsResponse = await _nodeClient.GetAllGroupsAsync();
            if (groupsResponse?.Success == true && groupsResponse.Data != null)
            {
                Groups.Clear();
                foreach (var group in groupsResponse.Data)
                    Groups.Add(group);
            }

            await LoadNodesAsync();
            await LoadStatisticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load node data");
            _notificationService.ShowError("数据加载", $"加载失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadNodesAsync()
    {
        var response = await _nodeClient.GetAllNodesAsync(SelectedGroup?.Id);
        if (response?.Success != true || response.Data == null)
            return;

        _allNodes = response.Data;
        ApplyFilter();
    }

    private async Task LoadStatisticsAsync()
    {
        var response = await _nodeClient.GetStatisticsAsync();
        if (response?.Success == true && response.Data != null)
        {
            Statistics = response.Data;
            OnPropertyChanged(nameof(StatisticsText));
        }
    }

    [RelayCommand]
    private async Task CreateNodeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewNodeName) || string.IsNullOrWhiteSpace(NewNodeServer))
        {
            _notificationService.ShowError("创建节点", "请填写名称和服务器");
            return;
        }

        var node = new ProxyNode
        {
            Name = NewNodeName.Trim(),
            Type = SelectedNodeType,
            Server = NewNodeServer.Trim(),
            Port = (int)NewNodePort,
            Password = NewNodePassword,
            Method = NewNodeMethod,
            UUID = NewNodeUUID,
            GroupId = SelectedGroup?.Id,
            IsActive = true
        };

        try
        {
            var response = await _nodeClient.CreateNodeAsync(node);
            if (response?.Success == true)
            {
                _notificationService.ShowSuccess("创建节点", "节点创建成功");
                NewNodeName = string.Empty;
                NewNodeServer = string.Empty;
                NewNodePort = 443;
                NewNodePassword = string.Empty;
                NewNodeUUID = string.Empty;
                await LoadNodesAsync();
                await LoadStatisticsAsync();
            }
            else
            {
                _notificationService.ShowError("创建节点", response?.Message ?? "创建失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create node");
            _notificationService.ShowError("创建节点", $"创建异常: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteNodeAsync()
    {
        var node = SelectedNode;
        if (node == null)
        {
            _notificationService.ShowWarning("删除节点", "请选择要删除的节点");
            return;
        }

        try
        {
            var response = await _nodeClient.DeleteNodeAsync(node.Id);
            if (response?.Success == true)
            {
                _notificationService.ShowSuccess("删除节点", $"已删除 {node.Name}");
                await LoadNodesAsync();
                await LoadStatisticsAsync();
            }
            else
            {
                _notificationService.ShowError("删除节点", response?.Message ?? "删除失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete node {NodeId}", node.Id);
            _notificationService.ShowError("删除节点", $"删除异常: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestNodeAsync()
    {
        var node = SelectedNode;
        if (node == null)
        {
            _notificationService.ShowWarning("测试节点", "请选择要测试的节点");
            return;
        }

        try
        {
            var response = await _nodeClient.TestNodeAsync(node.Id);
            if (response?.Success == true && response.Data != null)
            {
                var result = response.Data;
                if (result.Status == NodeTestStatus.Success)
                    _notificationService.ShowSuccess("节点测试", $"{node.Name} 延迟 {result.Latency} ms");
                else
                    _notificationService.ShowError("节点测试", $"{node.Name} 失败: {result.ErrorMessage}");

                await LoadNodesAsync();
                await LoadStatisticsAsync();
            }
            else
            {
                _notificationService.ShowError("节点测试", response?.Message ?? "测试失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test node {NodeId}", node.Id);
            _notificationService.ShowError("节点测试", $"测试异常: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task TestAllNodesAsync()
    {
        try
        {
            var response = await _nodeClient.TestAllNodesAsync();
            if (response?.Success == true && response.Data != null)
            {
                var result = response.Data;
                _notificationService.ShowSuccess("批量测试", $"完成: {result.SuccessCount} 成功, {result.FailedCount} 失败");
                await LoadNodesAsync();
                await LoadStatisticsAsync();
            }
            else
            {
                _notificationService.ShowError("批量测试", response?.Message ?? "测试失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test all nodes");
            _notificationService.ShowError("批量测试", $"测试异常: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        try
        {
            var group = new ProxyNodeGroup { Name = $"分组 {Groups.Count + 1}" };
            var response = await _nodeClient.CreateGroupAsync(group);
            if (response?.Success == true)
            {
                _notificationService.ShowSuccess("创建分组", "分组创建成功");
                await LoadDataAsync();
            }
            else
            {
                _notificationService.ShowError("创建分组", response?.Message ?? "创建失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create group");
            _notificationService.ShowError("创建分组", $"创建异常: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteGroupAsync()
    {
        var group = SelectedGroup;
        if (group == null)
        {
            _notificationService.ShowWarning("删除分组", "请选择要删除的分组");
            return;
        }

        if (group.IsDefault)
        {
            _notificationService.ShowError("删除分组", "不能删除默认分组");
            return;
        }

        try
        {
            var response = await _nodeClient.DeleteGroupAsync(group.Id);
            if (response?.Success == true)
            {
                SelectedGroup = null;
                _notificationService.ShowSuccess("删除分组", "分组删除成功");
                await LoadDataAsync();
            }
            else
            {
                _notificationService.ShowError("删除分组", response?.Message ?? "删除失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete group {GroupId}", group.Id);
            _notificationService.ShowError("删除分组", $"删除异常: {ex.Message}");
        }
    }

    partial void OnSelectedGroupChanged(ProxyNodeGroup? value)
    {
        _ = LoadNodesAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<ProxyNode> query = _allNodes;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(n =>
                n.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Server.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (n.Tag?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Nodes.Clear();
        foreach (var node in query)
            Nodes.Add(node);
    }

    private int GetStat(string key) => Statistics.TryGetValue(key, out var value) ? value : 0;
}
