using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BreezeLink.UI.Services;
using BreezeLink.CoreController.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace BreezeLink.UI.ViewModels;

/// <summary>
/// 节点管理视图模型
/// </summary>
public partial class NodeManagementViewModel : ObservableObject
{
    private readonly NodeManagementClient _nodeClient;
    private readonly NotificationService _notificationService;
    private readonly ILogger<NodeManagementViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<ProxyNode> nodes = new();

    [ObservableProperty]
    private ObservableCollection<ProxyNodeGroup> groups = new();

    [ObservableProperty]
    private ProxyNode? selectedNode;

    [ObservableProperty]
    private ProxyNodeGroup? selectedGroup;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ProxyNodeType selectedNodeType = ProxyNodeType.Shadowsocks;

    [ObservableProperty]
    private Dictionary<string, int> statistics = new();

    // 新节点表单
    [ObservableProperty]
    private string newNodeName = string.Empty;

    [ObservableProperty]
    private string newNodeServer = string.Empty;

    [ObservableProperty]
    private int newNodePort = 1080;

    [ObservableProperty]
    private string newNodePassword = string.Empty;

    [ObservableProperty]
    private string newNodeMethod = "aes-256-gcm";

    [ObservableProperty]
    private string newNodeUUID = string.Empty;

    public NodeManagementViewModel(
        NodeManagementClient nodeClient,
        NotificationService notificationService,
        ILogger<NodeManagementViewModel> logger)
    {
        _nodeClient = nodeClient;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// 加载所有数据
    /// </summary>
    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;

        try
        {
            // 加载分组
            var groupsResponse = await _nodeClient.GetAllGroupsAsync();
            if (groupsResponse?.Success == true)
            {
                Groups.Clear();
                foreach (var group in groupsResponse.Data!)
                {
                    Groups.Add(group);
                }
            }

            // 加载节点
            await LoadNodesAsync();

            // 加载统计信息
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

    /// <summary>
    /// 加载节点
    /// </summary>
    private async Task LoadNodesAsync()
    {
        var response = await _nodeClient.GetAllNodesAsync(SelectedGroup?.Id);
        if (response?.Success == true)
        {
            Nodes.Clear();
            foreach (var node in response.Data!)
            {
                Nodes.Add(node);
            }
        }
    }

    /// <summary>
    /// 加载统计信息
    /// </summary>
    private async Task LoadStatisticsAsync()
    {
        var response = await _nodeClient.GetStatisticsAsync();
        if (response?.Success == true)
        {
            Statistics = response.Data!;
        }
    }

    /// <summary>
    /// 创建节点
    /// </summary>
    [RelayCommand]
    private async Task CreateNodeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewNodeName) || string.IsNullOrWhiteSpace(NewNodeServer))
        {
            _notificationService.ShowError("创建节点", "请填写必要的字段");
            return;
        }

        var node = new ProxyNode
        {
            Name = NewNodeName,
            Type = SelectedNodeType,
            Server = NewNodeServer,
            Port = NewNodePort,
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
                await LoadNodesAsync();
                await LoadStatisticsAsync();

                // 清空表单
                NewNodeName = string.Empty;
                NewNodeServer = string.Empty;
                NewNodePort = 1080;
                NewNodePassword = string.Empty;
                NewNodeMethod = "aes-256-gcm";
                NewNodeUUID = string.Empty;
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

    /// <summary>
    /// 删除节点
    /// </summary>
    [RelayCommand]
    private async Task DeleteNodeAsync(ProxyNode? node)
    {
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
                _notificationService.ShowSuccess("删除节点", "节点删除成功");
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

    /// <summary>
    /// 测试节点
    /// </summary>
    [RelayCommand]
    private async Task TestNodeAsync(ProxyNode? node)
    {
        if (node == null)
        {
            _notificationService.ShowWarning("测试节点", "请选择要测试的节点");
            return;
        }

        try
        {
            var response = await _nodeClient.TestNodeAsync(node.Id);
            if (response?.Success == true)
            {
                var result = response.Data!;
                var message = result.Status == NodeTestStatus.Success
                    ? $"测试成功，延迟: {result.Latency}ms"
                    : $"测试失败: {result.ErrorMessage}";

                _notificationService.ShowSuccess("节点测试", message);

                // 更新节点状态
                await LoadNodesAsync();
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

    /// <summary>
    /// 批量测试节点
    /// </summary>
    [RelayCommand]
    private async Task TestAllNodesAsync()
    {
        try
        {
            var response = await _nodeClient.TestAllNodesAsync();
            if (response?.Success == true)
            {
                var result = response.Data!;
                var message = $"批量测试完成: {result.SuccessCount} 成功, {result.FailedCount} 失败";
                _notificationService.ShowSuccess("批量测试", message);

                // 更新节点状态
                await LoadNodesAsync();
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

    /// <summary>
    /// 创建分组
    /// </summary>
    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        const string groupName = "新分组";

        try
        {
            var group = new ProxyNodeGroup { Name = groupName };
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

    /// <summary>
    /// 删除分组
    /// </summary>
    [RelayCommand]
    private async Task DeleteGroupAsync(ProxyNodeGroup? group)
    {
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

    /// <summary>
    /// 当分组选择改变时
    /// </summary>
    partial void OnSelectedGroupChanged(ProxyNodeGroup? value)
    {
        if (value != null)
        {
            _ = LoadNodesAsync();
        }
    }

    /// <summary>
    /// 搜索节点
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = LoadNodesAsync();
        }
        else
        {
            var filteredNodes = Nodes.Where(n =>
                n.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                n.Server.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                n.Tag?.Contains(value, StringComparison.OrdinalIgnoreCase) == true
            ).ToList();

            Nodes.Clear();
            foreach (var node in filteredNodes)
            {
                Nodes.Add(node);
            }
        }
    }

    /// <summary>
    /// 导入节点配置
    /// </summary>
    [RelayCommand]
    private async Task ImportNodesAsync()
    {
        // TODO: 实现节点配置导入功能
        _notificationService.ShowWarning("导入节点", "功能开发中...");
    }

    /// <summary>
    /// 导出节点配置
    /// </summary>
    [RelayCommand]
    private async Task ExportNodesAsync()
    {
        // TODO: 实现节点配置导出功能
        _notificationService.ShowWarning("导出节点", "功能开发中...");
    }
}
