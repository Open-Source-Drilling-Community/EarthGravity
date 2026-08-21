using System.Text.Json.Nodes;

namespace OSDC.Drilling.EarthGravity.Service.Mcp;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    JsonNode InputSchema { get; }
    JsonNode OutputSchema { get; }
    Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken);
}
