namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>EGM96 results in the same order as the request positions.</summary>
public class EarthGravityEvaluationResponse
{
    public EarthGravityModelInfo Model { get; set; } = new();
    public List<EarthGravitySample> Samples { get; set; } = [];
}
