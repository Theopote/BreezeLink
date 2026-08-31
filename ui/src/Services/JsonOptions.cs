using System.Text.Json;
using System.Text.Json.Serialization;

namespace BreezeLink.UI.Services;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
