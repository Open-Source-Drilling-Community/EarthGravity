using OSDC.Drilling.EarthGravity.WebPages;

namespace OSDC.Drilling.EarthGravity.WebApp;

public class WebPagesHostConfiguration : IEarthGravityWebPagesConfiguration
{
    public string EarthGravityHostURL { get; set; } = string.Empty;
    public string? UnitConversionHostURL { get; set; } = string.Empty;
}
