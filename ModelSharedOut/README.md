# ModelSharedOut

**Author:** Eric Cayeux  
**Company:** NORCE Research

`ModelSharedOut` implements the distributed shared-model pattern used by the OSDC microservices. It turns the Service OpenAPI contract into the generated DTOs and HTTP `Client` consumed by `WebPages` and `ServiceTest`.

## Inputs and outputs

Input:

- `json-schemas/EarthGravityFullName.json`: OpenAPI generated from `Service.dll`.
- Additional dependency OpenAPI JSON files can be placed in `json-schemas` if the public service contract later depends on another microservice schema.

Outputs:

- `EarthGravityMergedModel.cs`: generated NSwag client and DTO classes in `OSDC.Drilling.EarthGravity.ModelShared`.
- `../Service/wwwroot/json-schema/EarthGravityMergedModel.json`: merged OpenAPI exposed by the Service Swagger UI.

`PseudoConstructors.cs` contains hand-maintained convenience constructors for generated request DTOs. The generated client file and JSON documents must not be edited manually.

## Regeneration

Run from the repository root after any public controller or `Model` change:

```powershell
dotnet tool restore
dotnet build Service/Service.csproj -c Release
dotnet swagger tofile --output ModelSharedOut/json-schemas/EarthGravityFullName.json Service/bin/Release/net8.0/Service.dll v1
dotnet run --project ModelSharedOut/ModelSharedOut.csproj -c Release
```

A Debug build of `Service` also refreshes `EarthGravityFullName.json` through its `CreateSwaggerJson` MSBuild target. Running `ModelSharedOut` is still required to regenerate the C# client and merged public JSON.

Generation is deterministic. The CI workflow regenerates all three artifacts and uses `git diff --exit-code` to detect stale committed output.

## Build

```powershell
dotnet build ModelSharedOut/ModelSharedOut.csproj -c Release
```

The project is both a generator executable and a compiled assembly containing the generated types for `ServiceTest`. `WebPages` links the generated source file directly, following the established solution pattern.
