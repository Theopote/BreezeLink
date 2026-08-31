using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BreezeLink.UI.Services;

namespace BreezeLink.UI;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(logging =>
        {
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        services.AddHttpClient<ProxyServiceClient>(client =>
        {
            client.BaseAddress = new Uri("http://127.0.0.1:8800");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<NodeManagementClient>(client =>
        {
            client.BaseAddress = new Uri("http://127.0.0.1:8800");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<NotificationService>();

        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    private Window? _window;
}
