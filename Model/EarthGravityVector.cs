using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>Total gravity acceleration in the local north-east-down frame.</summary>
[Semantic(Concepts.GravityVector)]
public class EarthGravityVector
{
    /// <summary>Northerly acceleration component in SI metres per second squared.</summary>
    [Semantic(Concepts.GravityAcceleration, Role = Concepts.North, Reference = Concepts.Ned)]
    public double North { get; set; }

    /// <summary>Easterly acceleration component in SI metres per second squared.</summary>
    [Semantic(Concepts.GravityAcceleration, Role = Concepts.East, Reference = Concepts.Ned)]
    public double East { get; set; }

    /// <summary>Downward acceleration component in SI metres per second squared; normally positive.</summary>
    [Semantic(Concepts.GravityAcceleration, Role = Concepts.Down, Reference = Concepts.Ned)]
    public double Down { get; set; }

    /// <summary>Magnitude of the gravity vector in SI metres per second squared.</summary>
    [Semantic(Concepts.GravityAcceleration, Role = Concepts.Magnitude, Reference = Concepts.Ned)]
    public double Magnitude => Math.Sqrt(North * North + East * East + Down * Down);

    /// <summary>Total gravitational plus centrifugal potential in SI square metres per square second.</summary>
    [Semantic(Concepts.TotalPotential)]
    public double TotalPotential { get; set; }
}
