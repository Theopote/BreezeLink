using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BreezeLink.UI.ViewModels;
using BreezeLink.UI.Services;
using BreezeLink.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BreezeLink.UI;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        var serviceProvider = App.Services;
        var logger = serviceProvider.GetRequiredService<ILogger<MainViewModel>>();
        var proxyService = serviceProvider.GetRequiredService<ProxyServiceClient>();
        var notificationService = serviceProvider.GetRequiredService<NotificationService>();

        ViewModel = new MainViewModel(proxyService, notificationService, logger);
        InitializeComponent();
        Title = "BreezeLink - 智能代理客户端";

        Closed += (_, _) => ViewModel.Dispose();
    }

    private async void NodeManagementButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var nodeClient = App.Services.GetRequiredService<NodeManagementClient>();
            var notificationService = App.Services.GetRequiredService<NotificationService>();
            var logger = App.Services.GetRequiredService<ILogger<NodeManagementViewModel>>();
            var nodeViewModel = new NodeManagementViewModel(nodeClient, notificationService, logger);

            var dialog = new NodeManagementDialog
            {
                ViewModel = nodeViewModel,
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "打开节点管理失败",
                Content = ex.Message,
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
