using System.Text.Json;
using System.Text.Json.Serialization;

namespace BreezeLink.CoreController.Models;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions FileOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
