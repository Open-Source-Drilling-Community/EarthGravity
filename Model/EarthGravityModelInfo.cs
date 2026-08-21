namespace OSDC.Drilling.EarthGravity.Model;

/// <summary>Identity and provenance of the Earth gravity model used for an evaluation.</summary>
public class EarthGravityModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string ID { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string DataVersion { get; set; } = string.Empty;
    public int Degree { get; set; }
    public int Order { get; set; }
    public string GeographicLibVersion { get; set; } = string.Empty;
    public string ReferenceEllipsoid { get; set; } = "WGS84";
    public bool IncludesCentrifugalAcceleration { get; set; } = true;
    public string CoefficientSHA256 { get; set; } = string.Empty;
}
