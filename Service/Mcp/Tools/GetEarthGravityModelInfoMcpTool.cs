using System.Text.Json;
using System.Text.Json.Nodes;
using OSDC.Drilling.EarthGravity.Model;

namespace OSDC.Drilling.EarthGravity.Service.Mcp.Tools;

public sealed class GetEarthGravityModelInfoMcpTool(EarthGravityEvaluator evaluator) : IMcpTool
{
    public string Name => "earth_gravity_get_model_info";
    public string Description => "Returns the identity and provenance of the EGM96 model currently loaded by this service. The result properties are Name, ID, Publisher, ReleaseDate, DataVersion, Degree, Order, GeographicLibVersion, ReferenceEllipsoid, IncludesCentrifugalAcceleration, and CoefficientSHA256. Use these values to record calculation provenance, verify the exact coefficient file, or compare deployments; this tool performs no gravity calculation and persists nothing.";
    public JsonNode InputSchema { get; } = JsonNode.Parse("""{"type":"object","properties":{},"additionalProperties":false}""")!;
    public JsonNode OutputSchema { get; } = JsonNode.Parse("""
    {
      "type": "object",
      "description": "Identity and reproducibility metadata for the loaded Earth gravity model.",
      "properties": {
        "Name": { "type": "string", "description": "GeographicLib model name used to load the coefficient data." },
        "ID": { "type": "string", "description": "Stable published identifier of the gravity model." },
        "Publisher": { "type": "string", "description": "Organization that published the model." },
        "ReleaseDate": { "type": "string", "description": "Model release date in ISO 8601 calendar-date form (YYYY-MM-DD)." },
        "DataVersion": { "type": "string", "description": "Version of the installed coefficient dataset." },
        "Degree": { "type": "integer", "minimum": 0, "description": "Maximum spherical-harmonic degree represented by the model." },
        "Order": { "type": "integer", "minimum": 0, "description": "Maximum spherical-harmonic order represented by the model." },
        "GeographicLibVersion": { "type": "string", "description": "Version of the GeographicLib runtime used by the service." },
        "ReferenceEllipsoid": { "type": "string", "const": "WGS84", "description": "Ellipsoid to which Latitude, Longitude, and Depth are referred." },
        "IncludesCentrifugalAcceleration": { "type": "boolean", "description": "Whether evaluated total gravity includes centrifugal acceleration from Earth rotation." },
        "CoefficientSHA256": { "type": "string", "pattern": "^[0-9a-fA-F]{64}$", "description": "SHA-256 digest of the installed coefficient file for byte-level provenance." }
      },
      "required": ["Name", "ID", "Publisher", "ReleaseDate", "DataVersion", "Degree", "Order", "GeographicLibVersion", "ReferenceEllipsoid", "IncludesCentrifugalAcceleration", "CoefficientSHA256"],
      "additionalProperties": false
    }
    """)!;
    public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken) =>
        Task.FromResult(JsonSerializer.SerializeToNode(evaluator.ModelInfo));
}
