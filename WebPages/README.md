# OSDC.Drilling.EarthGravity.WebPages

This release targets MudBlazor 9.9.0 and the matching OSDC shared web component packages.

**Author:** Eric Cayeux  
**Company:** NORCE Research

Reusable server-side Blazor pages for the OSDC Earth Gravity service. The package consumes the generated `ModelSharedOut` NSwag client and uses `OSDC.UnitConversion.DrillingRazorMudComponents` so users can work in a selected OSDC unit system while the Earth Gravity API remains strictly SI.

## Included pages

- `/Home`: service purpose and SI/WGS84 conventions.
- `/EarthGravityCalculation`: unit-aware single-position gravity evaluation.
- `/EarthGravityModel`: EGM96 provenance and coefficient hash.
- `/StatisticsEarthGravity`: cumulative operational counters retained by the service's persistent data volume.

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
dotnet pack WebPages/WebPages.csproj -c Release -p:PackageVersion=1.0.2
```

The project currently generates a package during Release builds. The GitHub workflow publishes `OSDC.Drilling.EarthGravity.WebPages` for `webpages-v*` tags or a manually supplied version.

Regenerate `ModelSharedOut` before building the package whenever the service API changes.

## Semantic quantity alignment

The shared catalogue bindings use the UI's existing PlaneAngleGeodesic, DepthDrilling and AccelerationDrilling choices. Depth is a position relative to the WGS84 ellipsoid, not an interval length. TotalPotential uses EarthGravityPotential through the standard read-only unit-adornment component. SI/Metric default to m²/s²; US/Imperial default to ft²/s². J/kg and ft·lbf/lbm are also available. The meaningful display precision is 0.01 m²/s², converted to the chosen unit; this is not model accuracy. Deploy a UnitConversion service containing the new quantity (3.3.28 or later) before enabling this UI update. EarthGravity API values remain in SI.

## Package release 1.0.4

This release packages the current WebPages and generated contracts. Update consuming WebApps to `OSDC.Drilling.EarthGravity.WebPages` version `1.0.4` after it is available on nuget.org.

## Trusted NuGet publishing

The `publish-webpages-nuget.yml` workflow authenticates with GitHub OIDC through `NuGet/login@v1`; no long-lived `NUGET_API_KEY` secret is used. Set `NUGET_USER` to the nuget.org profile username as a repository variable (a repository secret is also accepted). The publishing job requests `contents: read` and `id-token: write` and obtains its temporary credential immediately before uploading.

On nuget.org, the package owner must configure a trusted-publishing policy for `Open-Source-Drilling-Community/EarthGravity`, workflow filename `publish-webpages-nuget.yml`, with permission to push new versions of `OSDC.Drilling.EarthGravity.WebPages`. Leave the policy environment empty because the workflow does not declare a GitHub environment. Trigger the workflow manually with the intended version or push a `webpages-v*` tag. Already published versions are skipped.

## Package release 1.0.5

Requires OSDC.UnitConversion.DrillingRazorMudComponents 3.4.8 so embedded unit selectors consume bounded UnitSystem discovery pages. Rebuild consuming WebApps after upgrading this package.
