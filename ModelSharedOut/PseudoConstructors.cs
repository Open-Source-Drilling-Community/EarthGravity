namespace OSDC.Drilling.EarthGravity.ModelShared;

/// <summary>Convenience constructors for the generated stateless Earth Gravity DTOs.</summary>
public static class PseudoConstructors
{
    public static EarthGravityPosition ConstructEarthGravityPosition() => new()
    {
        Latitude = 0,
        Longitude = 0,
        Depth = 0
    };

    public static EarthGravityEvaluationRequest ConstructEarthGravityEvaluationRequest() => new()
    {
        Positions = [ConstructEarthGravityPosition()]
    };
}
