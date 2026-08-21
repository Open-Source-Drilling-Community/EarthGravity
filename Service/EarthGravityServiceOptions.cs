namespace OSDC.Drilling.EarthGravity.Service;

public sealed class EarthGravityServiceOptions
{
    public const string SectionName = "EarthGravity";
    public int MaximumPositionsPerRequest { get; set; } = 10_000;
    public string? ModelDirectory { get; set; }
}
