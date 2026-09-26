using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>Identity and provenance of the Earth gravity model used for an evaluation.</summary>
[Semantic(Concepts.ModelInfo)]
public class EarthGravityModelInfo
{
    [Semantic(Concepts.ModelName)]
    public string Name { get; set; } = string.Empty;
    [Semantic(Concepts.ModelId)]
    public string ID { get; set; } = string.Empty;
    [Semantic(Concepts.Publisher)]
    public string Publisher { get; set; } = string.Empty;
    [Semantic(Concepts.ReleaseDate)]
    public string ReleaseDate { get; set; } = string.Empty;
    [Semantic(Concepts.DataVersion)]
    public string DataVersion { get; set; } = string.Empty;
    [Semantic(Concepts.HarmonicDegree)]
    public int Degree { get; set; }
    [Semantic(Concepts.HarmonicOrder)]
    public int Order { get; set; }
    [Semantic(Concepts.RuntimeVersion)]
    public string GeographicLibVersion { get; set; } = string.Empty;
    [Semantic(Concepts.ReferenceEllipsoid)]
    public string ReferenceEllipsoid { get; set; } = "WGS84";
    [Semantic(Concepts.IncludesCentrifugal)]
    public bool IncludesCentrifugalAcceleration { get; set; } = true;
    [Semantic(Concepts.CoefficientHash)]
    public string CoefficientSHA256 { get; set; } = string.Empty;
}
