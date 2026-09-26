# OSDC Earth Gravity

OSDC Earth Gravity is an EGM96 microservice with stateless calculations, REST, Model Context Protocol (MCP), a generated shared API client, reusable unit-aware WebPages, and a server-side Blazor WebApp. It follows the established OSDC microservice solution structure while intentionally omitting the database, calculation-order resources, temporary GUID workflow, and CRUD endpoints from `NORCE.Drilling.GravitationalField`. Cumulative usage counters are persisted separately from calculation inputs and results.

This project replaces https://github.com/Open-Source-Drilling-Community/GravitationalField. The repository https://github.com/Open-Source-Drilling-Community/GravitationalField is therefore switched to be archived.

## Solution structure

- `Model`: public domain types, validation, usage counters, and the GeographicLib EGM96 evaluator.
- `Service`: controller-based REST API, Swagger, stateless streamable-HTTP MCP, probes, metrics, Dockerfile, and Helm chart.
- `ModelSharedOut`: OpenAPI merger and NSwag generator, plus the committed generated DTOs and `Client`.
- `WebPages`: reusable Razor class library that consumes `ModelSharedOut` and the OSDC unit-reference components.
- `WebApp`: server-side Blazor host for `WebPages`, with its own Dockerfile and Helm chart.
- `ModelTest`: calculation, convention, provenance, and validation tests.
- `ServiceTest`: generated-client, REST, MCP-discovery, statistics-restart, Swagger, metrics, and health tests.

The main dependency and generation flow is:

```text
Model -> Service -> OpenAPI -> ModelSharedOut -> WebPages -> WebApp
                                      `-------> ServiceTest
```

## API contract and units

`POST /EarthGravity/api/EarthGravity/Evaluate` accepts a batch of WGS84 positions and returns the EGM96 results synchronously. The operation stores nothing. Validation is atomic: if one position is invalid, the complete batch is rejected and no partial response is returned.

All API and MCP values use OSDC internal SI conventions:

- `Latitude`: WGS84 geodetic latitude in radians, from `-pi/2` to `pi/2`.
- `Longitude`: WGS84 longitude in radians, from `-pi` to `pi`.
- `Depth`: metres, positive downward, with zero at the WGS84 reference ellipsoid. A negative value denotes height above the ellipsoid. It is not referenced to mean sea level, a geoid, seabed, rig datum, or local vertical datum.
- `North`, `East`, `Down`, and `Magnitude`: metres per second squared. `Down` is positive toward Earth and is normally positive.
- `TotalPotential`: square metres per square second.

GeographicLib expects degrees and ellipsoidal height positive upward, and returns east-north-up components. These are private implementation details inside `Model`: angles are converted from radians, `ellipsoidalHeight = -Depth`, the horizontal components are reordered, and the upward component is negated to produce the public north-east-down vector. EGM96 total gravity includes centrifugal acceleration.

Example using the checked-in local HTTP port:

```bash
curl -X POST http://localhost:58944/EarthGravity/api/EarthGravity/Evaluate \
  -H "Content-Type: application/json" \
  -d '{"Positions":[{"Latitude":1.0471975511965976,"Longitude":0.17453292519943295,"Depth":1000}]}'
```

## HTTP endpoints

- `GET /EarthGravity/api/EarthGravity`: microservice discovery entry point; returns the same loaded EGM96 model information as `ModelInfo`.
- `POST /EarthGravity/api/EarthGravity/Evaluate`: evaluate one or more positions.
- `GET /EarthGravity/api/EarthGravity/ModelInfo`: EGM96 identity, provenance, degree/order, GeographicLib version, and coefficient SHA-256.
- `GET /EarthGravity/api/EarthGravityUsageStatistics`: cumulative counters persisted by the service.
- `GET /EarthGravity/api/metrics`: Prometheus text metrics.
- `GET /EarthGravity/api/health/live`: liveness probe.
- `GET /EarthGravity/api/health/ready`: readiness probe, including the loaded model ID.
- `/EarthGravity/api/swagger`: Swagger UI backed by the merged public OpenAPI document.

Usage counters have `persistent-service` scope and retain their original `StartedAt` value when restored. Prometheus may scrape the same cumulative totals, but the JSON snapshot is a single-writer design, so the supplied persistent deployment should remain at one service replica.

## MCP

The stateless streamable-HTTP endpoint is `/EarthGravity/api/mcp`. It exposes exactly three underscore-safe tools:

- `ping`
- `earth_gravity_get_model_info`
- `earth_gravity_evaluate`

Every tool publishes strict JSON input and output schemas through MCP `tools/list`. The evaluate metadata also documents the SI/WGS84 contract, positive-down `Depth`, local north-east-down component signs, input-order preservation, output units, model provenance, EGM96 behavior, stateless execution, batch limit, and atomic structured validation errors. Usage statistics are intentionally available only through REST and metrics; they are not registered as an MCP tool. `ServiceTest` verifies the published discovery metadata as well as the exclusion of usage statistics.

Usage-counter snapshots are stored atomically in `/home/EarthGravity.UsageStatistics.json`, restored during startup, written every 30 seconds when changed, and flushed during graceful shutdown. They survive pod replacement when the `/home` persistent volume is retained; an abrupt process or node failure can lose changes since the last snapshot. Override the defaults with `EarthGravity__UsageStatisticsFile` and `EarthGravity__UsageStatisticsSaveIntervalSeconds`.

## ModelSharedOut generation

The generated NSwag client and DTOs are committed so `WebPages`, tests, and downstream consumers build reproducibly. Regenerate them whenever a public controller or `Model` contract changes:

```powershell
dotnet tool restore
dotnet build Service/Service.csproj -c Release
dotnet msbuild Service/Service.csproj -t:ExportSwaggerJson -p:Configuration=Release
dotnet run --project ModelSharedOut/ModelSharedOut.csproj -c Release
```

This updates:

- `ModelSharedOut/json-schemas/EarthGravityFullName.json`
- `ModelSharedOut/EarthGravityMergedModel.cs`
- `Service/wwwroot/json-schema/EarthGravityMergedModel.json`

Do not hand-edit generated files. CI repeats generation and fails if the committed outputs are stale.

## WebApp and the OSDC unit system

`WebPages/EarthGravityCalculation.razor` uses `MudUnitAndReferenceChoiceTag`, `MudInputAngleWithUnitAdornment`, and `MudInputWithUnitAdornment`. Users can select units through the Unit Conversion microservice, while the generated Earth Gravity client continues to exchange SI values.

The WGS84 vertical reference is fixed by the Earth Gravity contract; the WebApp converts display units but does not offer a different depth reference.

Development configuration:

```json
{
  "EarthGravityHostURL": "http://localhost:58944/",
  "UnitConversionHostURL": "https://dev.digiwells.no/"
}
```

Kubernetes production defaults:

```json
{
  "EarthGravityHostURL": "http://osdcearthgravityservice/",
  "UnitConversionHostURL": "http://osdcunitconversionservice/"
}
```

Both values are host roots with trailing slashes. `WebPages.APIUtils` appends `EarthGravity/api/` or `UnitConversion/api/` as appropriate.

## Build, test, and run locally

The projects target .NET 8. `global.json` allows the next installed major SDK when a compatible .NET 8 SDK is unavailable; SDK 9.0.317 is supported for local builds. Reload the solution if Visual Studio retains an SDK-resolution error after this setting changes.

```powershell
dotnet tool restore
dotnet restore EarthGravity.sln
dotnet build EarthGravity.sln -c Release
dotnet test EarthGravity.sln -c Release
```

Start the applications in separate terminals:

```powershell
dotnet run --project Service
dotnet run --project WebApp
```

The checked-in launch profiles use:

- Service HTTPS: `https://localhost:58943`
- Service HTTP: `http://localhost:58944`
- WebApp HTTPS: `https://localhost:58945`
- WebApp HTTP: `http://localhost:58946`

The Service does not open a browser. The WebApp does. Open the WebApp manually at `http://localhost:58946/EarthGravity/webapp` if needed. The Unit Conversion service configured for the selected environment must be reachable for unit selection.

For compatibility with autogenerated microservice discovery pages, `GET /EarthGravity/webapp/EarthGravity` redirects to `/EarthGravity/webapp/Home`. Routing is case-insensitive, and the redirect is host-relative, so the same behavior applies on every configured host.

## Docker

Build from the repository root:

```bash
docker build -f Service/Dockerfile -t earthgravity-service .
docker build -f WebApp/Dockerfile -t earthgravity-webapp .
```

Both final images use the non-root `app` user from the .NET 8 runtime image and listen on container port 8080. The Service image includes the EGM96 model files and declares `/home` as its statistics data volume. Mount a named or managed volume at `/home` when running the service container so counters survive container replacement.

GitHub Actions publishes the images to the `digiwells` organization on Docker Hub:

- `docker.io/digiwells/osdcdrillingearthgravityservice`
- `docker.io/digiwells/osdcdrillingearthgravitywebappclient`

Configure the GitHub Actions repository secrets `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN`. The username must belong to a Docker Hub account with permission to push to the `digiwells` organization; the token must be a Docker Hub personal access token with write permission. Do not store a Docker Hub password in the repository.

## Kubernetes

Each deployable project owns its Helm chart:

- `Service/charts/osdcdrillingearthgravityservice`
- `WebApp/charts/osdcdrillingearthgravitywebappclient`

Install the service first, followed by the WebApp:

```bash
helm upgrade --install earthgravity-service Service/charts/osdcdrillingearthgravityservice \
  --namespace earthgravity --create-namespace
helm upgrade --install earthgravity-webapp WebApp/charts/osdcdrillingearthgravitywebappclient \
  --namespace earthgravity
```

Both charts follow the established `GravitationalField` chart pattern: one replica, the Docker Hub `stable` tag with `Always` pull policy, DigiWells ingress hosts, an optional HPA, optional health probes, configurable security/resources, and a Helm connection test. The Service chart creates a PVC mounted at `/home` by default for the usage-statistics snapshot; set `persistence.existingClaim` to reuse a managed volume. Keep the Service at one writer replica while using the JSON snapshot. The charts do not create PodDisruptionBudgets and therefore do not require `policy/v1` permissions. The WebApp retains `ClientIP` affinity because server-side Blazor maintains a circuit per user.

The charts pull their default images from `docker.io/digiwells`. No pull secret is required when the Docker Hub repositories are public. For private repositories, create a Kubernetes Docker-registry secret and pass it to both charts, for example with `--set 'imagePullSecrets[0].name=dockerhub-credentials'`.

## Automation and attribution

GitHub Actions build and test the solution, verify generated contracts, lint/render both Helm charts, publish service/WebApp images to the `digiwells` organization on Docker Hub, and publish `OSDC.Drilling.EarthGravity.WebPages` to NuGet when requested or tagged.

EGM96 and GeographicLib attribution is recorded in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

**Author:** Eric Cayeux  
**Company:** NORCE Research

## Shared semantic catalogue

The Model now binds all evaluation, position, gravity and model-provenance properties to `OSDC.DotnetLibraries.Drilling.SemanticCatalogue` 0.1.0. Shared model attributes publish `x-osdc-semantic` declarations in both REST/OpenAPI and MCP schemas, including stable concept IDs, catalogue version, component roles, WGS84/NED references and authoritative physical-quantity identities. The initial vocabulary is marked Proposed for curation. Payload names, numeric values, validation and the three-tool MCP surface are unchanged.

Latitude/longitude use PlaneAngleGeodesic, depth uses DepthDrilling, and acceleration uses AccelerationDrilling, matching WebPages. TotalPotential binds to EarthGravityPotential (UnitConversion 3.3.28): API values remain m²/s², while the UI supports the selected unit system and a meaningful display precision of 0.01 m²/s². This precision does not express model accuracy; potential is energy per mass, not energy density. Coordinate positions are not additive lengths. Spherical-harmonic Degree and Order are integers, not angular quantities.

A sibling DotNetLibraries checkout supplies the catalogue project for local development. Set `UseLocalSemanticCatalogue=false` to verify the NuGet dependency instead. Standalone/CI/Docker builds consume package 0.1.0 and require it to be available on their configured feed. This change prepares and tests that package locally; it does not publish it. The Docker build context remains the EarthGravity repository because the release path uses NuGet, not a cross-repository source copy.

Semantic catalogue 0.2.0 distinguishes the gravity evaluation result (acceleration vector plus scalar potential) from the vector itself. The existing DTO and JSON names remain unchanged. Geodetic position retains its ellipsoidal-depth representation under a generic parent. Input ranges, ordered output, atomic validation and synchronous/stateless evaluation remain EarthGravity provider guarantees. Publish the catalogue 0.2.0 NuGet before standalone CI or Docker builds; local sibling-source builds can validate this increment before publication.

All 36 concepts in semantic catalogue 0.2.0 are Reviewed following approval on 2026-09-26; REST/MCP annotations publish that status. Future additions require separate curation.
