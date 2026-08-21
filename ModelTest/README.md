# ModelTest

**Author:** Eric Cayeux  
**Company:** NORCE Research

Unit tests for the domain and GeographicLib calculation in `Model`.

Coverage includes:

- Conversion from public SI radians to GeographicLib degrees.
- Conversion from positive-down WGS84 `Depth` to negative ellipsoidal height.
- Agreement with a direct GeographicLib EGM96 call.
- EGM96 provenance, degree/order, WGS84 convention, centrifugal flag, and coefficient SHA-256.
- Invalid latitude, longitude, and non-finite depth.
- Maximum batch-size and atomic-rejection behavior.

Run from the repository root:

```powershell
dotnet test ModelTest/ModelTest.csproj -c Release
```

The EGM96 files are copied into the test output by `ModelTest.csproj`.
