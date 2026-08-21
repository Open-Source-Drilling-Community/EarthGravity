namespace OSDC.Drilling.EarthGravity.WebPages;

public static class DataUtils
{
    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
    }

    public static void UpdateUnitSystemName(string value) => UnitAndReferenceParameters.UnitSystemName = value;
}
