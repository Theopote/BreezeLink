using System.Text.Json;
using System.Text.Json.Serialization;
using BreezeLink.CoreController.Models;
using BreezeLink.CoreController.Services;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration.GetValue("ProxySettings:ApiPort", 8800);
var url = $"http://127.0.0.1:{port}";
builder.WebHost.UseUrls(url);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BreezeLink Core Controller API",
        Version = "v1",
        Description = "REST API for managing the sing-box proxy service"
    });
});

builder.Services.AddHttpClient(nameof(TrafficMonitoringService), client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddSingleton<IProxyProcessManager, SingBoxProcessManager>();
builder.Services.AddSingleton<INodeManagementService, NodeManagementService>();
builder.Services.AddSingleton<INodeTestingService, NodeTestingService>();
builder.Services.AddSingleton<ISingBoxConfigService, SingBoxConfigService>();
builder.Services.AddSingleton<ITrafficMonitoringService, TrafficMonitoringService>();
builder.Services.AddSingleton<ISystemTrayService, SystemTrayService>();
builder.Services.AddHostedService<ProxyService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalUi", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                origin.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                origin.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase))
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LocalUi");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(ApiResponse<object>.Ok(new { status = "ok" }, "BreezeLink Core Controller is running")));

Console.WriteLine("Starting BreezeLink Core Controller...");
Console.WriteLine($"API: {url}");
if (app.Environment.IsDevelopment())
    Console.WriteLine($"Swagger: {url}/swagger");
Console.WriteLine("Press Ctrl+C to stop");
Console.WriteLine();

try
{
    app.Run();
}
catch (IOException ex) when (ex.InnerException is Microsoft.AspNetCore.Connections.AddressInUseException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Failed to start: port {port} is already in use.");
    Console.WriteLine($"  netstat -ano | findstr :{port}");
    Console.WriteLine($"  taskkill /PID <PID> /F");
    Console.WriteLine("Or change ProxySettings:ApiPort in appsettings.json");
    Console.ResetColor();
    Environment.Exit(1);
}
