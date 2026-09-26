using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>An evaluated WGS84 position and its corresponding EGM96 acceleration and potential.</summary>
[Semantic(Concepts.Sample)]
public class EarthGravitySample
{
    [Semantic(Concepts.Position, Reference = Concepts.Wgs84)]
    public EarthGravityPosition Position { get; set; } = new();
    [Semantic(Concepts.GravityResult, Reference = Concepts.Ned)]
    public EarthGravityVector Gravity { get; set; } = new();
}
