using System.ComponentModel.DataAnnotations;
using BreezeLink.CoreController.Models;

namespace BreezeLink.CoreController.Models;

/// <summary>
/// 规则类型枚举
/// </summary>
public enum RuleType
{
    Domain,        // 域名规则
    DomainSuffix,  // 域名后缀规则
    DomainKeyword, // 域名关键词规则
    IPCIDR,        // IP段规则
    IPCIDR6,       // IPv6段规则
    GeoIP,         // 地理位置IP规则
    GeoSite,       // 地理位置域名规则
    ProcessName,   // 进程名称规则
    ProcessPath,   // 进程路径规则
    UserId,        // 用户ID规则
    Network,       // 网络类型规则
    Port,          // 端口规则
    Protocol       // 协议规则
}

/// <summary>
/// 规则动作枚举
/// </summary>
public enum RuleAction
{
    Route,    // 路由到指定输出
    Reject,   // 拒绝连接
    Direct,   // 直连
    Proxy,    // 代理
    Block     // 阻止
}

/// <summary>
/// 网络类型枚举
/// </summary>
public enum NetworkType
{
    TCP,
    UDP,
    Both
}

/// <summary>
/// 路由规则模型
/// </summary>
public class RoutingRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public RuleType Type { get; set; }

    [Required]
    [StringLength(1000)]
    public string Pattern { get; set; } = string.Empty;

    public RuleAction Action { get; set; }

    public string? OutboundTag { get; set; } // 目标输出标签

    public NetworkType Network { get; set; } = NetworkType.Both;

    public int Priority { get; set; } = 0;

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 转换为 sing-box 规则格式
    public Dictionary<string, object> ToSingBoxRule()
    {
        var rule = new Dictionary<string, object>
        {
            ["type"] = Type.ToString().ToLower(),
            ["outbound"] = Action switch
            {
                RuleAction.Direct => "direct",
                RuleAction.Proxy => "proxy",
                RuleAction.Reject => "block",
                RuleAction.Block => "block",
                _ => OutboundTag ?? "proxy"
            }
        };

        // 根据规则类型添加匹配条件
        switch (Type)
        {
            case RuleType.Domain:
                rule["domain"] = new[] { Pattern };
                break;
            case RuleType.DomainSuffix:
                rule["domain_suffix"] = new[] { Pattern };
                break;
            case RuleType.DomainKeyword:
                rule["domain_keyword"] = new[] { Pattern };
                break;
            case RuleType.IPCIDR:
                rule["ip_cidr"] = new[] { Pattern };
                break;
            case RuleType.IPCIDR6:
                rule["ip_cidr"] = new[] { Pattern };
                break;
            case RuleType.GeoIP:
                rule["geoip"] = Pattern;
                break;
            case RuleType.GeoSite:
                rule["geosite"] = Pattern;
                break;
            case RuleType.ProcessName:
                rule["process_name"] = new[] { Pattern };
                break;
            case RuleType.ProcessPath:
                rule["process_path"] = new[] { Pattern };
                break;
            case RuleType.Network:
                rule["network"] = Pattern;
                break;
            case RuleType.Port:
                if (int.TryParse(Pattern, out int port))
                    rule["port"] = port;
                break;
            case RuleType.Protocol:
                rule["protocol"] = new[] { Pattern };
                break;
            case RuleType.UserId:
                if (int.TryParse(Pattern, out int userId))
                    rule["user_id"] = userId;
                break;
        }

        return rule;
    }
}

/// <summary>
/// 规则组模型
/// </summary>
public class RuleGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public List<Guid> RuleIds { get; set; } = new();

    public bool IsDefault { get; set; } = false;

    public int Priority { get; set; } = 0;

    public bool Enabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 系统托盘状态模型
/// </summary>
public class SystemTrayStatus
{
    public bool IsProxyRunning { get; set; }

    public string? CurrentNode { get; set; }

    public int CurrentLatency { get; set; } = -1;

    public long TotalUpload { get; set; }

    public long TotalDownload { get; set; }

    public DateTime LastUpdate { get; set; } = DateTime.Now;
}

/// <summary>
/// 自动切换配置模型
/// </summary>
public class AutoSwitchConfig
{
    public bool Enabled { get; set; } = false;

    public int CheckIntervalSeconds { get; set; } = 300; // 5分钟

    public int MaxLatencyThreshold { get; set; } = 1000; // 1000ms

    public int MinLatencyThreshold { get; set; } = 50; // 50ms

    public int SwitchCooldownSeconds { get; set; } = 60; // 1分钟冷却时间

    public List<string> PreferredNodeTags { get; set; } = new();

    public bool EnableHealthCheck { get; set; } = true;

    public int HealthCheckTimeout { get; set; } = 5000; // 5秒

    public DateTime LastSwitchTime { get; set; } = DateTime.MinValue;
}

/// <summary>
/// 配置导入请求模型
/// </summary>
public class ConfigImportRequest
{
    public string ConfigContent { get; set; } = string.Empty;

    public string ConfigFormat { get; set; } = "auto"; // auto, clash, v2ray, sing-box

    public bool OverwriteExisting { get; set; } = false;

    public Guid? TargetGroupId { get; set; }
}

/// <summary>
/// 配置导入结果模型
/// </summary>
public class ConfigImportResult
{
    public int TotalNodes { get; set; }

    public int ImportedCount { get; set; }

    public int SkippedNodes { get; set; }

    public int ErrorNodes { get; set; }

    public List<string> Errors { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<ProxyNode> ImportedNodes { get; set; } = new();
}

/// <summary>
/// 规则测试请求模型
/// </summary>
public class RuleTestRequest
{
    public string Domain { get; set; } = string.Empty;

    public string IP { get; set; } = string.Empty;

    public string ProcessName { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Protocol { get; set; } = string.Empty;

    public NetworkType Network { get; set; } = NetworkType.TCP;
}

/// <summary>
/// 规则测试结果模型
/// </summary>
public class RuleTestResult
{
    public bool Matched { get; set; }

    public RoutingRule? MatchedRule { get; set; }

    public string? MatchedOutbound { get; set; }

    public List<string> MatchedPatterns { get; set; } = new();

    public long ProcessTimeMs { get; set; }
}
