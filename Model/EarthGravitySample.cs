namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>An evaluated WGS84 position and its corresponding EGM96 gravity vector.</summary>
public class EarthGravitySample
{
    public EarthGravityPosition Position { get; set; } = new();
    public EarthGravityVector Gravity { get; set; } = new();
}
