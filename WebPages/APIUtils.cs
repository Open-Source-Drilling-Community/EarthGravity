using OSDC.Drilling.EarthGravity.ModelShared;

namespace OSDC.Drilling.EarthGravity.WebPages;

public class APIUtils : OSDC.DotnetLibraries.Drilling.WebAppUtils.APIUtils, IEarthGravityAPIUtils
{
    public APIUtils(IEarthGravityWebPagesConfiguration configuration)
    {
        HostNameEarthGravity = Require(configuration.EarthGravityHostURL, nameof(configuration.EarthGravityHostURL));
        HttpClientEarthGravity = SetHttpClient(HostNameEarthGravity, HostBasePathEarthGravity);
        ClientEarthGravity = new Client(HttpClientEarthGravity.BaseAddress!.ToString(), HttpClientEarthGravity);
        HostNameUnitConversion = Require(configuration.UnitConversionHostURL, nameof(configuration.UnitConversionHostURL));
    }

    public string HostNameEarthGravity { get; }
    public string HostBasePathEarthGravity { get; } = "EarthGravity/api/";
    public HttpClient HttpClientEarthGravity { get; }
    public Client ClientEarthGravity { get; }
    public string HostNameUnitConversion { get; }
    public string HostBasePathUnitConversion { get; } = "UnitConversion/api/";

    private static string Require(string? value, string property)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"Configuration value '{property}' is required.");
        return value;
    }
}
