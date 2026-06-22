using System.ComponentModel.DataAnnotations;

namespace BreezeLink.CoreController.Models;

/// <summary>
/// 代理节点类型枚举
/// </summary>
public enum ProxyNodeType
{
    Shadowsocks,
    ShadowsocksR,
    VMess,
    VLESS,
    Trojan,
    Hysteria,
    Hysteria2,
    TUIC,
    SOCKS5,
    HTTP
}

/// <summary>
/// 节点测试状态枚举
/// </summary>
public enum NodeTestStatus
{
    Pending,
    Testing,
    Success,
    Failed,
    Timeout
}

/// <summary>
/// 代理节点模型
/// </summary>
public class ProxyNode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ProxyNodeType Type { get; set; }

    [Required]
    [StringLength(500)]
    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 1080;

    [StringLength(100)]
    public string? Username { get; set; }

    [StringLength(200)]
    public string? Password { get; set; }

    [StringLength(100)]
    public string? Method { get; set; } // 加密方法

    [StringLength(200)]
    public string? UUID { get; set; } // VMess/VLESS UUID

    [StringLength(200)]
    public string? AlterId { get; set; } // VMess alterId

    [StringLength(200)]
    public string? Security { get; set; } // VLESS security

    [StringLength(500)]
    public string? SNI { get; set; } // Server Name Indication

    [StringLength(500)]
    public string? Alpn { get; set; } // Application Layer Protocol Negotiation

    public bool AllowInsecure { get; set; } = false;

    public bool SkipCertVerify { get; set; } = false;

    [StringLength(100)]
    public string? Tag { get; set; }

    public Guid? GroupId { get; set; }

    // 测试相关
    public int LastLatency { get; set; } = -1; // 延迟时间（毫秒）

    public DateTime? LastTestTime { get; set; }

    public NodeTestStatus TestStatus { get; set; } = NodeTestStatus.Pending;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 转换为 sing-box 配置
    public Dictionary<string, object> ToSingBoxOutbound()
    {
        var outbound = new Dictionary<string, object>
        {
            ["type"] = Type.ToString().ToLower(),
            ["tag"] = Tag ?? Name,
            ["server"] = Server,
            ["server_port"] = Port
        };

        switch (Type)
        {
            case ProxyNodeType.Shadowsocks:
                outbound["method"] = Method ?? "aes-256-gcm";
                outbound["password"] = Password ?? "";
                break;

            case ProxyNodeType.VMess:
                outbound["uuid"] = UUID ?? "";
                outbound["alter_id"] = int.TryParse(AlterId, out int alterId) ? alterId : 0;
                outbound["security"] = Security ?? "auto";
                break;

            case ProxyNodeType.VLESS:
                outbound["uuid"] = UUID ?? "";
                outbound["flow"] = "";
                if (!string.IsNullOrEmpty(SNI))
                    outbound["tls"] = new Dictionary<string, object>
                    {
                        ["enabled"] = true,
                        ["server_name"] = SNI,
                        ["insecure"] = AllowInsecure
                    };
                break;

            case ProxyNodeType.Trojan:
                outbound["password"] = Password ?? "";
                if (!string.IsNullOrEmpty(SNI))
                    outbound["tls"] = new Dictionary<string, object>
                    {
                        ["enabled"] = true,
                        ["server_name"] = SNI,
                        ["insecure"] = AllowInsecure
                    };
                break;

            case ProxyNodeType.SOCKS5:
                if (!string.IsNullOrEmpty(Username))
                    outbound["username"] = Username;
                if (!string.IsNullOrEmpty(Password))
                    outbound["password"] = Password;
                break;

            case ProxyNodeType.HTTP:
                if (!string.IsNullOrEmpty(Username))
                    outbound["username"] = Username;
                if (!string.IsNullOrEmpty(Password))
                    outbound["password"] = Password;
                break;
        }

        return outbound;
    }
}

/// <summary>
/// 节点组模型
/// </summary>
public class ProxyNodeGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public List<Guid> NodeIds { get; set; } = new();

    public bool IsDefault { get; set; } = false;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 节点测试请求模型
/// </summary>
public class NodeTestRequest
{
    public List<Guid> NodeIds { get; set; } = new();

    public int Timeout { get; set; } = 5000; // 毫秒

    public string TestUrl { get; set; } = "http://www.gstatic.com/generate_204";
}

/// <summary>
/// 节点测试结果模型
/// </summary>
public class NodeTestResult
{
    public Guid NodeId { get; set; }

    public string NodeName { get; set; } = string.Empty;

    public int Latency { get; set; } = -1;

    public NodeTestStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime TestTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 批量测试结果模型
/// </summary>
public class BatchTestResult
{
    public List<NodeTestResult> Results { get; set; } = new();

    public int TotalCount { get; set; }

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    public DateTime TestTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 节点配置请求模型
/// </summary>
public class NodeConfigRequest
{
    public Guid? GroupId { get; set; }

    public List<ProxyNode> Nodes { get; set; } = new();
}

/// <summary>
/// 节点配置响应模型
/// </summary>
public class NodeConfigResponse
{
    public ProxyNodeGroup? Group { get; set; }

    public List<ProxyNode> Nodes { get; set; } = new();

    public int TotalCount { get; set; }

    public int ActiveCount { get; set; }
}
