using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.EarthGravity.Service;

public static class JsonSettings
{
    public static void ApplyTo(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = null;
        options.DictionaryKeyPolicy = null;
        options.PropertyNameCaseInsensitive = true;
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
