# Service

**Author:** Eric Cayeux  
**Company:** NORCE Research

`Service` is the ASP.NET Core host for the stateless OSDC Earth Gravity REST and MCP APIs. It references `Model`, loads EGM96 during startup, and performs no persistence.

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
- `GET /EarthGravity/api/EarthGravityUsageStatistics`: in-memory counters for this replica.
- `GET /EarthGravity/api/metrics`: Prometheus counters.
- `GET /EarthGravity/api/health/live`: liveness.
- `GET /EarthGravity/api/health/ready`: readiness and model ID.
- `/EarthGravity/api/swagger`: Swagger UI using the merged `ModelSharedOut` OpenAPI document.

The default maximum batch size is 10,000 positions. Override it with:

```text
EarthGravity__MaximumPositionsPerRequest=5000
```

An optional `EarthGravity__ModelDirectory` may point to a directory containing `egm96.egm` and `egm96.egm.cof`. Otherwise the files are loaded from `GravityModelFiles` beside the application.

## MCP

The streamable-HTTP MCP endpoint is `/EarthGravity/api/mcp`. HTTP transport is stateless, allowing service replicas to be load-balanced without session affinity.

Registered tools:

- `ping`: reachability check with a fixed structured response.
- `earth_gravity_get_model_info`: loaded model identity, runtime version, and coefficient-file provenance.
- `earth_gravity_evaluate`: synchronous batch evaluation with echoed positions, model provenance, and local east-north-up gravity results.

All three tools publish strict input and output JSON schemas in `tools/list`. The evaluate description and schemas state SI units, the WGS84 ellipsoid depth reference, component signs, result ordering, and the structured atomic-validation error contract. `EarthGravityUsageStatistics` is intentionally not registered as an MCP tool. This is enforced by `ServiceTest` through dependency-injection registry and HTTP discovery checks.

## JSON and validation

JSON preserves C# property names such as `Positions`, `Latitude`, and `Depth`, while deserialization remains case-insensitive. Evaluation errors return HTTP 422 with an `EarthGravityValidationProblem`. Validation occurs in `Model`, not through the automatic ASP.NET model-state filter, ensuring REST and MCP use the same atomic rules.

## OpenAPI and ModelSharedOut

Swagger uses full CLR schema identifiers during raw generation. `ModelSharedOut` shortens and merges them, generates the shared client, and writes `wwwroot/json-schema/EarthGravityMergedModel.json`. See `../ModelSharedOut/README.md` for the regeneration commands.

## Docker

Build from the repository root:

```bash
docker build -f Service/Dockerfile -t earthgravity-service .
docker run --rm -p 8080:8080 earthgravity-service
```

The container runs as the non-root .NET `app` user. The API is then available below `http://localhost:8080/EarthGravity/api`.

The publication workflow pushes this image to `docker.io/digiwells/osdcdrillingearthgravityservice` using the GitHub Actions secrets `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN`.

## Kubernetes

The chart is `charts/osdcdrillingearthgravityservice`:

```bash
helm upgrade --install earthgravity-service Service/charts/osdcdrillingearthgravityservice \
  --namespace earthgravity --create-namespace
```

The default Kubernetes Service name is `osdcearthgravityservice`, matching the WebApp production configuration. Following the original Gravitational Field chart, it defaults to one replica, the `stable` image tag, `Always` pull policy, enabled DigiWells ingress routes, optional probes/HPA, and configurable resources and security contexts. It creates no PodDisruptionBudget and requires no persistence volume or sticky session.

The chart defaults to `docker.io/digiwells/osdcdrillingearthgravityservice`. If the Docker Hub repository is private, configure `imagePullSecrets` with a Kubernetes Docker-registry secret.
