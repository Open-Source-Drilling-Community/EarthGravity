namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>Total gravity acceleration in the local east-north-up frame.</summary>
public class EarthGravityVector
{
    /// <summary>Easterly acceleration component in SI metres per second squared.</summary>
    public double East { get; set; }

    /// <summary>Northerly acceleration component in SI metres per second squared.</summary>
    public double North { get; set; }

    /// <summary>Upward acceleration component in SI metres per second squared; normally negative.</summary>
    public double Up { get; set; }

    /// <summary>Magnitude of the gravity vector in SI metres per second squared.</summary>
    public double Magnitude => Math.Sqrt(East * East + North * North + Up * Up);

    /// <summary>Total gravitational plus centrifugal potential in SI square metres per square second.</summary>
    public double TotalPotential { get; set; }
}
