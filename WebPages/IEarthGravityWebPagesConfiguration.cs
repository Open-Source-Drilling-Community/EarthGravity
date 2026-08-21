using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace OSDC.Drilling.EarthGravity.WebPages;

public interface IEarthGravityWebPagesConfiguration : IUnitConversionHostURL
{
    string EarthGravityHostURL { get; }
}
