using Microsoft.UI.Xaml;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BreezeLink.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();
        this.Suspending += OnSuspending;
        this.UnhandledException += OnUnhandledException;

        // 配置依赖注入
        ConfigureServices();
    }

    private void ConfigureServices()
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
        services.AddHttpClient<Services.ProxyServiceClient>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("http://127.0.0.1:8800");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<Services.NodeManagementClient>((serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("http://127.0.0.1:8800");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // 注册服务
        services.AddSingleton<Services.ProxyServiceClient>();
        services.AddSingleton<Services.NodeManagementClient>();
        services.AddSingleton<Services.NotificationService>();

        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    private Window? m_window;

    private void OnSuspending(object sender, SuspendingEventArgs e)
    {
        // Handle app suspension
        var logger = Services.GetService<ILogger<App>>();
        logger?.LogInformation("App is suspending");
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Handle unhandled exceptions
        var logger = Services.GetService<ILogger<App>>();
        logger?.LogError(e.Exception, "Unhandled exception occurred");

        // In a real app, you might want to log this or show an error dialog
        e.Handled = true;
    }
}
