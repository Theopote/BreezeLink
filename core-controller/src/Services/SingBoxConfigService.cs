using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BreezeLink.CoreController.Models;
using Microsoft.Extensions.Logging;

namespace BreezeLink.CoreController.Services;

/// <summary>
/// 把当前节点列表编译成 sing-box 可用的 JSON 配置。
/// 保留 configs/config.json 中的 inbound / dns / log，只替换 outbound 与 clash API。
/// </summary>
public class SingBoxConfigService : ISingBoxConfigService
{
    private readonly ILogger<SingBoxConfigService> _logger;
    private readonly INodeManagementService _nodes;
    private readonly string _baseConfigPath;

    public SingBoxConfigService(ILogger<SingBoxConfigService> logger, INodeManagementService nodes)
    {
        _logger = logger;
        _nodes = nodes;
        _baseConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs", "config.json");
    }

    public async Task<string> BuildConfigAsync()
    {
        var root = await LoadBaseConfigAsync();
        var nodes = (await _nodes.GetAllNodesAsync()).Where(n => n.IsActive).ToList();

        var usedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nodeTags = new List<string>();
        var nodeOutbounds = new List<JsonObject>();

        foreach (var node in nodes)
        {
            var tag = EnsureUniqueTag(SanitizeTag(string.IsNullOrWhiteSpace(node.Tag) ? node.Name : node.Tag!), usedTags);
            var outbound = node.ToSingBoxOutbound();
            outbound["tag"] = tag;
            nodeOutbounds.Add(outbound);
            nodeTags.Add(tag);
        }

        var outbounds = new JsonArray();

        if (nodeTags.Count > 0)
        {
            var defaultTag = PickDefaultTag(nodes, nodeTags);
            var selectorOutbounds = new JsonArray();
            foreach (var tag in nodeTags)
                selectorOutbounds.Add(tag);
            selectorOutbounds.Add("direct");

            outbounds.Add(new JsonObject
            {
                ["type"] = "selector",
                ["tag"] = "proxy",
                ["outbounds"] = selectorOutbounds,
                ["default"] = defaultTag
            });
        }

        foreach (var outbound in nodeOutbounds)
            outbounds.Add(outbound);

        outbounds.Add(new JsonObject { ["type"] = "direct", ["tag"] = "direct" });
        outbounds.Add(new JsonObject { ["type"] = "block", ["tag"] = "block" });

        root["outbounds"] = outbounds;
        MigrateDns(root);

        if (root["route"] is not JsonObject route)
        {
            route = new JsonObject();
            root["route"] = route;
        }

        route["final"] = nodeTags.Count > 0 ? "proxy" : "direct";

        EnsureClashApi(root);

        var json = JsonSerializer.Serialize(root, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });
        _logger.LogInformation("Built sing-box config with {NodeCount} active node(s)", nodeTags.Count);
        return json;
    }

    private async Task<JsonObject> LoadBaseConfigAsync()
    {
        try
        {
            if (File.Exists(_baseConfigPath))
            {
                var text = await File.ReadAllTextAsync(_baseConfigPath);
                if (JsonNode.Parse(text) is JsonObject parsed)
                    return parsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse base config, using built-in defaults");
        }

        return CreateMinimalConfig();
    }

    private static JsonObject CreateMinimalConfig()
    {
        return new JsonObject
        {
            ["log"] = new JsonObject { ["level"] = "info", ["timestamp"] = true },
            ["dns"] = new JsonObject
            {
                ["servers"] = new JsonArray
                {
                    new JsonObject { ["type"] = "local", ["tag"] = "local" }
                }
            },
            ["inbounds"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "mixed",
                    ["tag"] = "mixed-in",
                    ["listen"] = "127.0.0.1",
                    ["listen_port"] = 1080,
                    ["sniff"] = true
                }
            },
            ["route"] = new JsonObject { ["final"] = "direct" }
        };
    }

    private static void MigrateDns(JsonObject root)
    {
        if (root["dns"] is not JsonObject dns || dns["servers"] is not JsonArray servers)
            return;

        var migrated = new JsonArray();
        foreach (var item in servers)
        {
            if (item is not JsonObject server)
                continue;

            if (server["type"] is not null)
            {
                var clone = server.DeepClone()!.AsObject();
                if (clone["detour"]?.GetValue<string>()?.Equals("direct", StringComparison.OrdinalIgnoreCase) == true)
                    clone.Remove("detour");
                migrated.Add(clone);
                continue;
            }

            var address = server["address"]?.GetValue<string>() ?? "local";
            var converted = new JsonObject();
            if (server["tag"] is { } tag)
                converted["tag"] = tag.GetValue<string>();

            if (address.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                converted["type"] = "local";
            }
            else if (address.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
            {
                converted["type"] = "tcp";
                converted["server"] = address["tcp://".Length..];
            }
            else if (address.StartsWith("tls://", StringComparison.OrdinalIgnoreCase))
            {
                converted["type"] = "tls";
                converted["server"] = address["tls://".Length..];
            }
            else if (address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                converted["type"] = "https";
                converted["server"] = new Uri(address).Host;
            }
            else
            {
                converted["type"] = "udp";
                converted["server"] = address;
            }

            var detour = server["detour"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(detour) &&
                !detour.Equals("direct", StringComparison.OrdinalIgnoreCase))
            {
                converted["detour"] = detour;
            }

            migrated.Add(converted);
        }

        dns["servers"] = migrated;
    }

    private static void EnsureClashApi(JsonObject root)
    {
        if (root["experimental"] is not JsonObject experimental)
        {
            experimental = new JsonObject();
            root["experimental"] = experimental;
        }

        if (experimental["clash_api"] is not JsonObject clashApi)
        {
            clashApi = new JsonObject();
            experimental["clash_api"] = clashApi;
        }

        clashApi["external_controller"] ??= "127.0.0.1:9090";
    }

    private static string PickDefaultTag(List<ProxyNode> nodes, List<string> tags)
    {
        var bestIndex = -1;
        var bestLatency = int.MaxValue;
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.TestStatus == NodeTestStatus.Success && node.LastLatency >= 0 && node.LastLatency < bestLatency)
            {
                bestLatency = node.LastLatency;
                bestIndex = i;
            }
        }

        return bestIndex >= 0 ? tags[bestIndex] : tags[0];
    }

    private static string SanitizeTag(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name.Trim())
        {
            sb.Append(char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_');
        }

        var tag = sb.ToString().Trim('_');
        return string.IsNullOrEmpty(tag) ? "node" : tag;
    }

    private static string EnsureUniqueTag(string tag, HashSet<string> used)
    {
        if (used.Add(tag))
            return tag;

        var i = 2;
        while (!used.Add($"{tag}-{i}"))
            i++;
        return $"{tag}-{i}";
    }
}
