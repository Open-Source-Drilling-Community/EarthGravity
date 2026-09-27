# Model

**Author:** Eric Cayeux  
**Company:** NORCE Research

`Model` is the source of truth for the OSDC Earth Gravity domain contract and calculation. It has no web, MCP, database, or Kubernetes dependencies.

## Responsibilities

- Defines `EarthGravityEvaluationRequest`, `EarthGravityPosition`, response, sample, vector, model-information, and validation types.
- Validates every position before calculation and rejects the complete batch if any position is invalid.
- Loads the GeographicLib EGM96 model once per `EarthGravityEvaluator`.
- Converts public SI radians to GeographicLib degrees.
- Converts positive-down WGS84 `Depth` to positive-up ellipsoidal height using `height = -Depth`.
- Converts GeographicLib east/north/up output and returns public north/east/down total-gravity components, magnitude, total potential, and model provenance.
- Defines thread-safe cumulative usage counters that the Service can snapshot and restore.

## Coordinate and unit contract

- Latitude: WGS84 geodetic latitude in radians, `[-pi/2, pi/2]`.
- Longitude: WGS84 longitude in radians, `[-pi, pi]`.
- Depth: metres, positive downward from the WGS84 reference ellipsoid. Negative values are above it.
- Vector components and magnitude: metres per second squared.
- Total potential: square metres per square second.

Depth is not referenced to mean sea level, a geoid, seabed, rig datum, or local vertical datum. The EGM96 result includes centrifugal acceleration.

## Gravity model files

The project copies `../GravityModelFiles/egm96.egm` and `egm96.egm.cof` into output and publish directories. `EarthGravityEvaluator` fails during construction if either file is missing. `EarthGravityModelInfo` reports model degree/order, GeographicLib version, and the coefficient-file SHA-256.

## Build and test

From the repository root:

```powershell
dotnet build Model/Model.csproj -c Release
dotnet test ModelTest/ModelTest.csproj -c Release
```

## Semantic declarations

`Semantic` attributes bind model classes and properties to the shared OSDC semantic catalogue 0.4.0. Concept specialization, physical quantity, field role and reference convention are distinct relationships. REST and MCP use these same attributes; they do not change JSON payloads or calculation behavior. The EarthGravity vocabulary is Reviewed following curation approval on 2026-09-26. Quantity names match WebPages: PlaneAngleGeodesic, DepthDrilling and AccelerationDrilling. TotalPotential binds to EarthGravityPotential in UnitConversion 3.3.28, with SI m²/s² and meaningful display precision 0.01 m²/s². Display precision does not round API values or express model accuracy. Local development and standalone consumers use the same published 0.4.0 NuGet package.

Semantic catalogue 0.2.0 distinguishes the gravity evaluation result (acceleration vector plus scalar potential) from the vector itself. The existing DTO and JSON names remain unchanged. Geodetic position retains its ellipsoidal-depth representation under a generic parent. Input ranges, ordered output, atomic validation and synchronous/stateless evaluation remain EarthGravity provider guarantees. All builds use the published SemanticCatalogue 0.4.0 NuGet package.

All 36 concepts in semantic catalogue 0.2.0 are Reviewed following approval on 2026-09-26; REST/MCP annotations publish that status. Future additions require separate curation.
