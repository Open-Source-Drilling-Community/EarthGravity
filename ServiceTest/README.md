# ServiceTest

**Author:** Eric Cayeux  
**Company:** NORCE Research

Integration and contract tests for `Service` using `WebApplicationFactory` and the generated `OSDC.Drilling.EarthGravity.ModelShared.Client`.

Coverage includes:

- Successful evaluation through the generated `ModelSharedOut` client.
- Typed HTTP 422 validation through the generated client.
- The exact three-tool MCP dependency-injection registry.
- MCP HTTP `tools/list`, including the absence of usage statistics.
- Liveness, readiness, Prometheus metrics, and merged Swagger endpoints.

Run from the repository root:

```powershell
dotnet test ServiceTest/ServiceTest.csproj -c Release
```

Regenerate `ModelSharedOut` before testing if the public Service contract changed. CI performs this check automatically.
