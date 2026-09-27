# Service

**Author:** Eric Cayeux  
**Company:** NORCE Research

`Service` is the ASP.NET Core host for the OSDC Earth Gravity REST and MCP APIs. EGM96 calculations remain stateless: request positions and results are never persisted. The service restores and periodically snapshots cumulative usage counters.

## Local URLs

The checked-in launch profile listens on:

- HTTPS: `https://localhost:58943`
- HTTP: `http://localhost:58944`

It deliberately sets `launchBrowser` to `false`. Run it with:

```powershell
dotnet run --project Service
```

## REST and operational endpoints

- `GET /EarthGravity/api/EarthGravity`: microservice discovery entry point returning the loaded EGM96 model information.
- `POST /EarthGravity/api/EarthGravity/Evaluate`: synchronous batch evaluation.
- `GET /EarthGravity/api/EarthGravity/ModelInfo`: loaded EGM96 provenance.
- `GET /EarthGravity/api/EarthGravityUsageStatistics`: cumulative counters persisted by the service.
- `GET /EarthGravity/api/metrics`: Prometheus counters.
- `GET /EarthGravity/api/health/live`: liveness.
- `GET /EarthGravity/api/health/ready`: readiness and model ID.
- `/EarthGravity/api/swagger`: Swagger UI using the merged `ModelSharedOut` OpenAPI document.

The default maximum batch size is 10,000 positions. Override it with:

```text
EarthGravity__MaximumPositionsPerRequest=5000
```

An optional `EarthGravity__ModelDirectory` may point to a directory containing `egm96.egm` and `egm96.egm.cof`. Otherwise the files are loaded from `GravityModelFiles` beside the application.

Usage-counter snapshots default to `/home/EarthGravity.UsageStatistics.json` in the container. Changed counters are written atomically every 30 seconds and flushed during graceful shutdown. Configure `EarthGravity__UsageStatisticsFile` and `EarthGravity__UsageStatisticsSaveIntervalSeconds` to override these defaults.

## MCP

The streamable-HTTP MCP endpoint is `/EarthGravity/api/mcp`. HTTP transport is stateless, allowing service replicas to be load-balanced without session affinity.

Registered tools:

- `ping`: reachability check with a fixed structured response.
- `earth_gravity_get_model_info`: loaded model identity, runtime version, and coefficient-file provenance.
- `earth_gravity_evaluate`: synchronous batch evaluation with echoed positions, model provenance, and local north-east-down gravity results.

All three tools publish strict input and output JSON schemas in `tools/list`. The evaluate description and schemas state SI units, the WGS84 ellipsoid depth reference, component signs, result ordering, and the structured atomic-validation error contract. `EarthGravityUsageStatistics` is intentionally not registered as an MCP tool. This is enforced by `ServiceTest` through dependency-injection registry and HTTP discovery checks.

## JSON and validation

JSON preserves C# property names such as `Positions`, `Latitude`, and `Depth`, while deserialization remains case-insensitive. Evaluation errors return HTTP 422 with an `EarthGravityValidationProblem`. Validation occurs in `Model`, not through the automatic ASP.NET model-state filter, ensuring REST and MCP use the same atomic rules.

## OpenAPI and ModelSharedOut

Swagger uses full CLR schema identifiers during raw generation. `ModelSharedOut` shortens and merges them, generates the shared client, and writes `wwwroot/json-schema/EarthGravityMergedModel.json`. See `../ModelSharedOut/README.md` for the regeneration commands.

## Docker

Build from the repository root:

```bash
docker build -f Service/Dockerfile -t earthgravity-service .
docker run --rm -p 8080:8080 -v earthgravity-home:/home earthgravity-service
```

The container runs as the non-root .NET `app` user, declares `/home` as its persistent data volume, and stores the statistics snapshot there. The API is then available below `http://localhost:8080/EarthGravity/api`.

The publication workflow pushes this image to `docker.io/digiwells/osdcdrillingearthgravityservice` using the GitHub Actions secrets `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN`.

## Kubernetes

The chart is `charts/osdcdrillingearthgravityservice`:

```bash
helm upgrade --install earthgravity-service Service/charts/osdcdrillingearthgravityservice \
  --namespace earthgravity --create-namespace
```

The default Kubernetes Service name is `osdcearthgravityservice`, matching the WebApp production configuration. Following the original Gravitational Field chart, it defaults to one replica, the `stable` image tag, `Always` pull policy, enabled DigiWells ingress routes, optional probes/HPA, and configurable resources and security contexts. It creates a PVC mounted at `/home` by default; set `persistence.existingClaim` to reuse a managed volume. Keep one writer replica while using the JSON statistics snapshot. It creates no PodDisruptionBudget and requires no sticky session.

The chart defaults to `docker.io/digiwells/osdcdrillingearthgravityservice`. If the Docker Hub repository is private, configure `imagePullSecrets` with a Kubernetes Docker-registry secret.

## Semantic contract metadata

The OpenAPI `SemanticSchemaFilter` and MCP schema annotations consume the same Model `Semantic` attributes from the shared catalogue. `x-osdc-semantic` includes the vocabulary version, stable URN, curation status, physical-quantity identity where resolved, SI representation, role and reference. OpenAPI reference properties use an allOf wrapper so their annotations are not ignored as $ref siblings. Data payloads and tool count are unchanged. Local, CI and Docker builds all restore SemanticCatalogue 0.3.0 from NuGet.

## Swagger generation during builds

Run `dotnet tool restore` from the repository root before the first Debug build. Debug builds export OpenAPI automatically using the Swagger CLI executable matching the service target framework (net8.0), even when building with a .NET 9 SDK. This avoids the local tool resolver selecting a net9.0 CLI that cannot load into the service runtime. After a Release build, run `dotnet msbuild Service/Service.csproj -t:ExportSwaggerJson -p:Configuration=Release` from the repository root. Keep `SwaggerCliVersion` in Service.csproj and the CLI version in `.config/dotnet-tools.json` aligned.

Semantic catalogue 0.2.0 distinguishes the gravity evaluation result (acceleration vector plus scalar potential) from the vector itself. The existing DTO and JSON names remain unchanged. Geodetic position retains its ellipsoidal-depth representation under a generic parent. Input ranges, ordered output, atomic validation and synchronous/stateless evaluation remain EarthGravity provider guarantees. All builds use the published SemanticCatalogue 0.3.0 NuGet package.

All 36 concepts in semantic catalogue 0.2.0 are Reviewed following approval on 2026-09-26; REST/MCP annotations publish that status. Future additions require separate curation.

The merged OpenAPI document advertises the API root (`/EarthGravity/api`) as its server URL, excluding the schema route. Service tests verify that its advertised URL and operation path resolve to a working endpoint.
