namespace BreezeLink.CoreController.Models;

/// <summary>
/// API 响应模型
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message };
    }

    public static ApiResponse<T> Error(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message };
    }
}

/// <summary>
/// 启动代理请求模型
/// </summary>
public class StartProxyRequest
{
    public string? ConfigContent { get; set; }
    public string? ConfigPath { get; set; }
}

/// <summary>
/// 停止代理请求模型
/// </summary>
public class StopProxyRequest
{
    public bool Force { get; set; } = false;
}

/// <summary>
/// 代理状态响应模型
/// </summary>
public class ProxyStatusResponse
{
    public string Status { get; set; } = "Stopped";
    public int? ProcessId { get; set; }
    public DateTime? StartTime { get; set; }
    public string? Version { get; set; }
}

/// <summary>
/// 日志响应模型
/// </summary>
public class LogsResponse
{
    public string Logs { get; set; } = string.Empty;
    public int TotalLines { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>
/// 错误响应模型
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
