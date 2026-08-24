# WebApp

**Author:** Eric Cayeux  
**Company:** NORCE Research

`WebApp` is the server-side Blazor host for `OSDC.Drilling.EarthGravity.WebPages`. It follows the established OSDC composition: navigation and layout live in the host, while reusable domain pages and the generated API client live in `WebPages`.

## Local development

The launch profile listens on:

- HTTPS: `https://localhost:58945`
- HTTP: `http://localhost:58946`

It opens a browser on launch. Start the Service first and then run:

```powershell
dotnet run --project WebApp
```

The application path is `/EarthGravity/webapp`. The development settings are:

```json
{
  "EarthGravityHostURL": "http://localhost:58944/",
  "UnitConversionHostURL": "https://dev.digiwells.no/"
}
```

The Earth Gravity URL matches the Service launch profile's HTTP port. The configured Unit Conversion service must be reachable for unit-system discovery and conversion.

The discovery entry URL `/EarthGravity/webapp/EarthGravity` redirects to `/EarthGravity/webapp/Home`. Matching is case-insensitive, and the redirect uses a host-relative location so it works unchanged on localhost and all ingress hosts.

## Production configuration

The Kubernetes defaults are:

```json
{
  "EarthGravityHostURL": "http://osdcearthgravityservice/",
  "UnitConversionHostURL": "http://osdcunitconversionservice/"
}
```

Environment variables with the same names override these settings. Each URL is a host root; `WebPages.APIUtils` appends the microservice base path.

## Hosting behavior

`Program.cs` registers Razor Pages, server-side Blazor, MudBlazor, `IEarthGravityWebPagesConfiguration`, and `IEarthGravityAPIUtils`. The router includes the WebPages assembly. Health endpoints are:

- `/EarthGravity/webapp/health/live`
- `/EarthGravity/webapp/health/ready`

Because server-side Blazor maintains a circuit, horizontally scaled WebApp replicas require session affinity or a compatible backplane. The supplied WebApp Helm chart uses Kubernetes `ClientIP` affinity. This requirement applies only to the WebApp; the REST/MCP Service is stateless.

## Docker

Build from the repository root:

```bash
docker build -f WebApp/Dockerfile -t earthgravity-webapp .
docker run --rm -p 8081:8080 \
  -e EarthGravityHostURL=http://host.docker.internal:8080/ \
  -e UnitConversionHostURL=https://dev.digiwells.no/ \
  earthgravity-webapp
```

Open `http://localhost:8081/EarthGravity/webapp`. The image runs as the non-root .NET `app` user.

The publication workflow pushes this image to `docker.io/digiwells/osdcdrillingearthgravitywebappclient` using the GitHub Actions secrets `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN`.

## Kubernetes

The chart is `charts/osdcdrillingearthgravitywebappclient`:

```bash
helm upgrade --install earthgravity-webapp WebApp/charts/osdcdrillingearthgravitywebappclient \
  --namespace earthgravity
```

Install the Service chart first. Following the original Gravitational Field WebApp chart, it defaults to one replica, the `stable` image tag, `Always` pull policy, enabled DigiWells ingress routes, optional probes/HPA, and configurable resources and security contexts. It creates no PodDisruptionBudget. The EarthGravity chart additionally retains ClientIP affinity and an ephemeral data-protection directory for server-side Blazor.

The chart defaults to `docker.io/digiwells/osdcdrillingearthgravitywebappclient`. If the Docker Hub repository is private, configure `imagePullSecrets` with a Kubernetes Docker-registry secret.
