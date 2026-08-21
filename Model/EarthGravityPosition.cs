using System.ComponentModel.DataAnnotations;

namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>A position referred to the WGS84 reference ellipsoid and expressed using OSDC SI conventions.</summary>
public class EarthGravityPosition
{
    /// <summary>WGS84 geodetic latitude in SI radians, between -π/2 and π/2.</summary>
    [Range(-1.5707963267948966, 1.5707963267948966)]
    public double Latitude { get; set; }

    /// <summary>WGS84 geodetic longitude in SI radians, between -π and π.</summary>
    [Range(-3.141592653589793, 3.141592653589793)]
    public double Longitude { get; set; }

    /// <summary>Depth in SI metres, positive downward, with zero at the WGS84 reference ellipsoid. A negative value is above the ellipsoid. This is not a mean-sea-level, geoid, seabed, rig, or local-datum depth.</summary>
    public double Depth { get; set; }
}
