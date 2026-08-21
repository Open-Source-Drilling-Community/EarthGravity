# OSDC.Drilling.EarthGravity.WebPages

**Author:** Eric Cayeux  
**Company:** NORCE Research

Reusable server-side Blazor pages for the OSDC Earth Gravity service. The package consumes the generated `ModelSharedOut` NSwag client and uses `OSDC.UnitConversion.DrillingRazorMudComponents` so users can work in a selected OSDC unit system while the Earth Gravity API remains strictly SI.

## Included pages

- `/Home`: service purpose and SI/WGS84 conventions.
- `/EarthGravityCalculation`: unit-aware single-position gravity evaluation.
- `/EarthGravityModel`: EGM96 provenance and coefficient hash.
- `/StatisticsEarthGravity`: per-replica operational counters.

`EarthGravityCalculation` uses:

- `MudUnitAndReferenceChoiceTag` for the selected OSDC unit system.
- `MudInputAngleWithUnitAdornment` for sexagesimal latitude/longitude entry.
- `MudInputWithUnitAdornment` with `PlaneAngleGeodesic`, `DepthDrilling`, and `AccelerationDrilling`.

The generated client always sends radians, metres, and metres per second squared. The WGS84 depth reference is fixed by the Earth Gravity contract and is not user-selectable; only display units change.

## Package dependencies

- `MudBlazor`
- `OSDC.DotnetLibraries.Drilling.WebAppUtils`
- `OSDC.UnitConversion.DrillingRazorMudComponents`
- Generated `ModelSharedOut/EarthGravityMergedModel.cs`

The generated client and `PseudoConstructors.cs` are linked into this Razor class library at build time.

## Host configuration

The host must implement `IEarthGravityWebPagesConfiguration`:

```csharp
public sealed class WebPagesHostConfiguration : IEarthGravityWebPagesConfiguration
{
    public string EarthGravityHostURL { get; set; } = string.Empty;
    public string? UnitConversionHostURL { get; set; } = string.Empty;
}
```

Both values are host roots, normally with trailing slashes. `APIUtils` appends `EarthGravity/api/` and `UnitConversion/api/`.

Register the pages and API utilities:

```csharp
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddSingleton<IEarthGravityWebPagesConfiguration>(configuration);
builder.Services.AddSingleton<IEarthGravityAPIUtils, APIUtils>();
```

Add this assembly to the Blazor router:

```razor
<Router AppAssembly="@typeof(App).Assembly"
        AdditionalAssemblies="new[] { typeof(OSDC.Drilling.EarthGravity.WebPages.EarthGravityCalculation).Assembly }">
    ...
</Router>
```

The host layout must include MudBlazor providers, CSS, and JavaScript, and must map the Blazor hub. See the repository `WebApp` project for a complete host.

## Build and package

From the repository root:

```powershell
dotnet build WebPages/WebPages.csproj -c Release
dotnet pack WebPages/WebPages.csproj -c Release -p:PackageVersion=1.0.0
```

The project currently generates a package during Release builds. The GitHub workflow publishes `OSDC.Drilling.EarthGravity.WebPages` for `webpages-v*` tags or a manually supplied version.

Regenerate `ModelSharedOut` before building the package whenever the service API changes.
