using OSDC.Drilling.EarthGravity.ModelShared;

namespace OSDC.Drilling.EarthGravity.WebPages;

public interface IEarthGravityAPIUtils
{
    string HostNameEarthGravity { get; }
    string HostBasePathEarthGravity { get; }
    HttpClient HttpClientEarthGravity { get; }
    Client ClientEarthGravity { get; }
    string HostNameUnitConversion { get; }
    string HostBasePathUnitConversion { get; }
}
