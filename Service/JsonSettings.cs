using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.EarthGravity.Service;

public static class JsonSettings
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static void ApplyTo(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = null;
        options.DictionaryKeyPolicy = null;
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new JsonStringEnumConverter());
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ApplyTo(options);
        return options;
    }
}
