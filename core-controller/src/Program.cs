using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BreezeLink.CoreController.Controllers;
using BreezeLink.CoreController.Services;
using BreezeLink.CoreController.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BreezeLink Core Controller API",
        Version = "v1",
        Description = "REST API for managing sing-box proxy service with advanced security features"
    });
});

// 注册核心服务
builder.Services.AddSingleton<IProxyProcessManager, SingBoxProcessManager>();
builder.Services.AddSingleton<INodeManagementService, NodeManagementService>();
builder.Services.AddSingleton<INodeTestingService, NodeTestingService>();
builder.Services.AddSingleton<ITrafficMonitoringService, TrafficMonitoringService>();
builder.Services.AddSingleton<ISystemTrayService, SystemTrayService>();

// 注册后台服务
builder.Services.AddHostedService<ProxyService>();

// 注册 HTTP 客户端
builder.Services.AddHttpClient();

// 注册 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => "BreezeLink Core Controller is running!");

// 启动消息
Console.WriteLine("🚀 Starting BreezeLink Core Controller...");
Console.WriteLine("📡 API will be available at: http://localhost:8800");
Console.WriteLine("📚 Swagger UI: http://localhost:8800/swagger");
Console.WriteLine("Press Ctrl+C to stop the service");
Console.WriteLine();

app.Run();
