using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using BreezeLink.UI.ViewModels;
using BreezeLink.UI.Services;
using BreezeLink.UI.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;
using System;
using System.Linq;
using Windows.System;

namespace BreezeLink.UI;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();

        // 创建服务提供商
        var serviceProvider = ConfigureServices();

        // 获取服务实例
        var logger = serviceProvider.GetRequiredService<ILogger<MainWindow>>();
        var proxyService = serviceProvider.GetRequiredService<ProxyServiceClient>();
        var notificationService = serviceProvider.GetRequiredService<NotificationService>();

        // 创建视图模型
        ViewModel = new MainViewModel(proxyService, notificationService, logger);

        Title = "BreezeLink - 智能代理客户端";

        // 窗口关闭时清理资源
        this.Closed += (sender, e) => ViewModel.Dispose();

        // 初始化
        InitializeWindow();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 注册日志
        services.AddLogging(logging =>
        {
            logging.AddDebug();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // 注册 HTTP 客户端
        services.AddHttpClient<ProxyServiceClient>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("http://127.0.0.1:8800");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // 注册服务
        services.AddSingleton<ProxyServiceClient>();
        services.AddSingleton<NotificationService>();

        return services.BuildServiceProvider();
    }

    private void InitializeWindow()
    {
        // 设置窗口大小
        this.ExtendsContentIntoTitleBar = true;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Win32Interop.GetAppWindowFromWindowId(windowId);

        // 设置窗口图标
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Square44x44Logo.png");
        if (File.Exists(iconPath))
        {
            var icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(iconPath));
            // 设置图标逻辑
        }

        // 启动定时器更新系统信息
        StartSystemInfoTimer();
    }

    private void StartSystemInfoTimer()
    {
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(5);
        timer.Tick += async (sender, e) =>
        {
            await ViewModel.UpdateSystemInfoAsync();
        };
        timer.Start();
    }

    // 导航事件处理
    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            NavigateToPage(tag);
        }
    }

    private void NavigateToPage(string pageTag)
    {
        switch (pageTag)
        {
            case "Dashboard":
                MainContentControl.Content = CreateDashboardContent();
                break;
            case "Nodes":
                MainContentControl.Content = CreateNodesContent();
                break;
            case "Rules":
                MainContentControl.Content = CreateRulesContent();
                break;
            case "Settings":
                MainContentControl.Content = CreateSettingsContent();
                break;
            case "Help":
                MainContentControl.Content = CreateHelpContent();
                break;
            case "About":
                ShowAboutDialog();
                break;
            default:
                MainContentControl.Content = CreateDashboardContent();
                break;
        }
    }

    private UIElement CreateDashboardContent()
    {
        return new DashboardContent { DataContext = ViewModel };
    }

    private UIElement CreateNodesContent()
    {
        return new NodesContent { DataContext = ViewModel };
    }

    private UIElement CreateRulesContent()
    {
        return new RulesContent { DataContext = ViewModel };
    }

    private UIElement CreateSettingsContent()
    {
        return new SettingsContent { DataContext = ViewModel };
    }

    private UIElement CreateHelpContent()
    {
        return new HelpContent { DataContext = ViewModel };
    }

    private async void ShowAboutDialog()
    {
        var dialog = new ContentDialog()
        {
            Title = "关于 BreezeLink",
            Content = new AboutContent(),
            CloseButtonText = "确定",
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    // 事件处理方法
    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateToPage("Settings");
    }

    private async void OpenConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            if (File.Exists(configPath))
            {
                // 打开配置文件
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "notepad.exe";
                process.StartInfo.Arguments = configPath;
                process.Start();
            }
            else
            {
                var dialog = new ContentDialog()
                {
                    Title = "配置文件不存在",
                    Content = $"配置文件不存在: {configPath}",
                    CloseButtonText = "确定",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog()
            {
                Title = "打开配置失败",
                Content = $"无法打开配置文件: {ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var proxyService = App.Current.Services.GetService<ProxyServiceClient>();
            if (proxyService == null) return;

            var isConnected = await proxyService.HealthCheckAsync();

            var dialog = new ContentDialog()
            {
                Title = "连接测试",
                Content = isConnected ? "✅ 连接成功" : "❌ 连接失败",
                CloseButtonText = "确定",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog()
            {
                Title = "连接测试失败",
                Content = $"测试异常: {ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    private async void NodeManagementButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var nodeClient = App.Current.Services.GetService<NodeManagementClient>();
            var notificationService = App.Current.Services.GetService<NotificationService>();
            var logger = App.Current.Services.GetService<ILogger<MainWindow>>();

            if (nodeClient == null || notificationService == null || logger == null)
            {
                var dialog = new ContentDialog()
                {
                    Title = "服务未就绪",
                    Content = "节点管理服务尚未初始化",
                    CloseButtonText = "确定",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
                return;
            }

            var nodeViewModel = new NodeManagementViewModel(nodeClient, notificationService, logger);

            var nodeManagementDialog = new NodeManagementDialog
            {
                ViewModel = nodeViewModel,
                XamlRoot = this.Content.XamlRoot
            };

            await nodeManagementDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog()
            {
                Title = "打开节点管理失败",
                Content = $"无法打开节点管理: {ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    private async void QuickConnectButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.QuickConnectAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
    }

    private async void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ToggleThemeAsync();
    }

    private async void OpenGitHubCommand_Execute()
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/your-repo/breezelink"));
    }

    private async void OpenDocumentationCommand_Execute()
    {
        await Launcher.LaunchUriAsync(new Uri("https://docs.breezelink.com"));
    }

    private async void OpenSupportCommand_Execute()
    {
        await Launcher.LaunchUriAsync(new Uri("https://support.breezelink.com"));
    }
}
