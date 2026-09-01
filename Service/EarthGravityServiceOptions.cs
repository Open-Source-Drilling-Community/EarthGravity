namespace OSDC.Drilling.EarthGravity.Service;

public sealed class EarthGravityServiceOptions
{
    public const string SectionName = "EarthGravity";
    public int MaximumPositionsPerRequest { get; set; } = 10_000;
    public string? ModelDirectory { get; set; }
    public string UsageStatisticsFile { get; set; } = "home/EarthGravity.UsageStatistics.json";
    public int UsageStatisticsSaveIntervalSeconds { get; set; } = 30;
}
