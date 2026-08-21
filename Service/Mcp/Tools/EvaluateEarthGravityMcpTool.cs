using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using OSDC.Drilling.EarthGravity.Model;

namespace OSDC.Drilling.EarthGravity.Service.Mcp.Tools;

public sealed class EvaluateEarthGravityMcpTool : IMcpTool
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };
    private readonly EarthGravityEvaluator evaluator_;
    private readonly UsageStatisticsEarthGravity statistics_;
    private readonly int maximumPositions_;

    public EvaluateEarthGravityMcpTool(EarthGravityEvaluator evaluator, UsageStatisticsEarthGravity statistics,
        IOptions<EarthGravityServiceOptions> options)
    {
        evaluator_ = evaluator;
        statistics_ = statistics;
        maximumPositions_ = options.Value.MaximumPositionsPerRequest;
        InputSchema = CreateSchema(maximumPositions_);
    }

    public string Name => "earth_gravity_evaluate";
    public string Description => "Synchronously evaluates EGM96 total gravity for one or more WGS84 positions. This is stateless: results are returned by this call, and no calculation order or data is persisted. Latitude and Longitude MUST be SI radians. Depth MUST be SI metres, positive downward, with zero at the WGS84 reference ellipsoid; negative Depth is above the ellipsoid. Depth is not referenced to mean sea level, a geoid, seabed, rig datum, or local vertical datum. Samples preserve the input Positions order. Gravity components use the local east-north-up (ENU) frame: East is positive eastward, North is positive northward, and Up is positive away from Earth and is therefore normally negative for gravity. Magnitude is non-negative. Acceleration and magnitude are in m/s²; TotalPotential is in m²/s². EGM96 total gravity includes centrifugal acceleration. Validation is atomic: one invalid position rejects the complete request with isError=true, no partial result, and structuredContent shaped as {Error, Message, Errors:[{PositionIndex, Property, Code, Message}]}; PositionIndex is zero-based for an item and null for a request-level error. Example request: {\"Positions\":[{\"Latitude\":1.0471975511965976,\"Longitude\":0.17453292519943295,\"Depth\":1000.0}]}.";
    public JsonNode InputSchema { get; }
    public JsonNode OutputSchema { get; } = CreateOutputSchema();

    public Task<JsonNode?> InvokeAsync(JsonObject? arguments, CancellationToken cancellationToken)
    {
        EarthGravityEvaluationRequest request = arguments?.Deserialize<EarthGravityEvaluationRequest>(JsonOptions)
            ?? throw new ArgumentException("An object containing Positions is required.");
        statistics_.IncrementEvaluation(true, request.Positions?.Count ?? 0);
        try
        {
            EarthGravityEvaluationResponse result = evaluator_.Evaluate(request, maximumPositions_, cancellationToken);
            return Task.FromResult(JsonSerializer.SerializeToNode(result, JsonOptions));
        }
        catch
        {
            statistics_.IncrementFailedEvaluation();
            throw;
        }
    }

    private static JsonNode CreateSchema(int maximumPositions) => JsonNode.Parse($$"""
    {
      "type": "object",
      "description": "Stateless synchronous EGM96 evaluation. Public coordinates use the OSDC SI/WGS84 convention.",
      "properties": {
        "Positions": {
          "type": "array",
          "description": "Positions evaluated in order. Any invalid item rejects the complete request.",
          "minItems": 1,
          "maxItems": {{maximumPositions}},
          "items": {
            "type": "object",
            "properties": {
              "Latitude": { "type": "number", "minimum": -1.5707963267948966, "maximum": 1.5707963267948966, "description": "WGS84 geodetic latitude in SI radians. Do not supply degrees." },
              "Longitude": { "type": "number", "minimum": -3.141592653589793, "maximum": 3.141592653589793, "description": "WGS84 longitude in SI radians. Do not supply degrees." },
              "Depth": { "type": "number", "description": "Depth in SI metres, positive downward from the WGS84 reference ellipsoid. Negative means above the ellipsoid. Not MSL, geoid, seabed, rig, or local-datum depth." }
            },
            "required": ["Latitude", "Longitude", "Depth"],
            "additionalProperties": false
          }
        }
      },
      "required": ["Positions"],
      "examples": [
        {
          "Positions": [
            { "Latitude": 1.0471975511965976, "Longitude": 0.17453292519943295, "Depth": 1000.0 }
          ]
        }
      ],
      "additionalProperties": false
    }
    """)!;

    private static JsonNode CreateOutputSchema() => JsonNode.Parse("""
    {
      "type": "object",
      "description": "Successful EGM96 evaluation. Samples correspond one-for-one to request Positions and preserve their order.",
      "properties": {
        "Model": { "$ref": "#/$defs/modelInfo" },
        "Samples": {
          "type": "array",
          "description": "Evaluated positions in exactly the same order as the input Positions array.",
          "items": {
            "type": "object",
            "properties": {
              "Position": { "$ref": "#/$defs/position" },
              "Gravity": { "$ref": "#/$defs/gravity" }
            },
            "required": ["Position", "Gravity"],
            "additionalProperties": false
          }
        }
      },
      "required": ["Model", "Samples"],
      "additionalProperties": false,
      "$defs": {
        "position": {
          "type": "object",
          "description": "The corresponding input position, echoed without changing its SI values.",
          "properties": {
            "Latitude": { "type": "number", "minimum": -1.5707963267948966, "maximum": 1.5707963267948966, "description": "WGS84 geodetic latitude in SI radians." },
            "Longitude": { "type": "number", "minimum": -3.141592653589793, "maximum": 3.141592653589793, "description": "WGS84 longitude in SI radians." },
            "Depth": { "type": "number", "description": "Depth in SI metres, positive downward from the WGS84 reference ellipsoid; negative above it." }
          },
          "required": ["Latitude", "Longitude", "Depth"],
          "additionalProperties": false
        },
        "gravity": {
          "type": "object",
          "description": "EGM96 total gravity in the local east-north-up frame, including centrifugal acceleration.",
          "properties": {
            "East": { "type": "number", "description": "Eastward acceleration component in SI m/s²; positive eastward." },
            "North": { "type": "number", "description": "Northward acceleration component in SI m/s²; positive northward." },
            "Up": { "type": "number", "description": "Upward acceleration component in SI m/s²; positive away from Earth and normally negative for gravity." },
            "Magnitude": { "type": "number", "minimum": 0, "description": "Non-negative magnitude of the gravity vector in SI m/s²." },
            "TotalPotential": { "type": "number", "description": "Total gravitational plus centrifugal potential in SI m²/s²." }
          },
          "required": ["East", "North", "Up", "Magnitude", "TotalPotential"],
          "additionalProperties": false
        },
        "modelInfo": {
          "type": "object",
          "description": "Identity and provenance of the model used for this evaluation.",
          "properties": {
            "Name": { "type": "string", "description": "GeographicLib model name." },
            "ID": { "type": "string", "description": "Stable published model identifier." },
            "Publisher": { "type": "string", "description": "Model publisher." },
            "ReleaseDate": { "type": "string", "description": "Model release date (YYYY-MM-DD)." },
            "DataVersion": { "type": "string", "description": "Installed coefficient-data version." },
            "Degree": { "type": "integer", "minimum": 0, "description": "Maximum spherical-harmonic degree." },
            "Order": { "type": "integer", "minimum": 0, "description": "Maximum spherical-harmonic order." },
            "GeographicLibVersion": { "type": "string", "description": "GeographicLib runtime version." },
            "ReferenceEllipsoid": { "type": "string", "const": "WGS84", "description": "Reference ellipsoid for the evaluated positions." },
            "IncludesCentrifugalAcceleration": { "type": "boolean", "description": "Whether total gravity includes centrifugal acceleration." },
            "CoefficientSHA256": { "type": "string", "pattern": "^[0-9a-fA-F]{64}$", "description": "SHA-256 of the installed coefficient file." }
          },
          "required": ["Name", "ID", "Publisher", "ReleaseDate", "DataVersion", "Degree", "Order", "GeographicLibVersion", "ReferenceEllipsoid", "IncludesCentrifugalAcceleration", "CoefficientSHA256"],
          "additionalProperties": false
        }
      }
    }
    """)!;
}
