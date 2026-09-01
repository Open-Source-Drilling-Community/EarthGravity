namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>Total gravity acceleration in the local north-east-down frame.</summary>
public class EarthGravityVector
{
    /// <summary>Northerly acceleration component in SI metres per second squared.</summary>
    public double North { get; set; }

    /// <summary>Easterly acceleration component in SI metres per second squared.</summary>
    public double East { get; set; }

    /// <summary>Downward acceleration component in SI metres per second squared; normally positive.</summary>
    public double Down { get; set; }

    /// <summary>Magnitude of the gravity vector in SI metres per second squared.</summary>
    public double Magnitude => Math.Sqrt(North * North + East * East + Down * Down);

    /// <summary>Total gravitational plus centrifugal potential in SI square metres per square second.</summary>
    public double TotalPotential { get; set; }
}
