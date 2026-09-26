using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>EGM96 results in the same order as the request positions.</summary>
[Semantic(Concepts.Response)]
public class EarthGravityEvaluationResponse
{
    [Semantic(Concepts.ModelInfo, Role = Concepts.Provenance)]
    public EarthGravityModelInfo Model { get; set; } = new();
    [Semantic(Concepts.Sample, Role = Concepts.OutputSamples)]
    public List<EarthGravitySample> Samples { get; set; } = [];
}
