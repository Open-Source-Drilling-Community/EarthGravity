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
- Returns east/north/up total-gravity components, magnitude, total potential, and model provenance.
- Defines in-memory, per-replica usage counters used by the Service.

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
