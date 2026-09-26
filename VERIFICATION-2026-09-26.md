# Published SemanticCatalogue and deployed EarthGravity verification

Date: 2026-09-26. Environments: app.digiwells.no, dev.digiwells.no, awe.web.intra.norceresearch.no.

## Successful checks

- Downloaded SemanticCatalogue 0.2.0 directly from NuGet. Its catalogue equals the local source vocabulary: 36 Reviewed concepts. The package depends on Conversion.DrillingEngineering 3.4.2; general Conversion is transitive.
- All three deployed merged OpenAPI documents contain 30 semantic annotations, using catalogue 0.2.0 and Reviewed status. Their only difference is the environment-specific `servers` URL.
- All three deployed MCP tools/list responses publish the same three tool contracts and 46 semantic annotations. Gravity result is bound to `gravity-evaluation-result`; total potential resolves to EarthGravityPotential, quantity ID `61a8a54e-684a-4e72-bb46-b73d342bdb70`, SI m²/s².
- REST and MCP evaluation succeeded on all three environments for latitude 60 degrees, longitude 5 degrees and zero WGS84 ellipsoidal depth (submitted as SI radians/metres). Magnitude is approximately 9.81947336608409 m/s²; total potential is approximately 62637298.4115194 m²/s². MCP magnitude was also checked against the Euclidean norm of its components.
- WebApp readiness returned HTTP 200 on all three environments. The production browser calculator accepted the same location and rendered its result, including 62,637,298.41 m²/s², without the earlier rendering failure. Interactive UI calculation was checked on app only.
- EarthGravity service tests passed: 13/13, including the new regression below. Repeated with `UseLocalSemanticCatalogue=false`; project.assets.json confirms SemanticCatalogue/0.2.0 resolves as a package. The normal sibling-source development option remains available.

These are representative deployment/contract checks, not an independent scientific revalidation of EGM96 or an inspection of running image digests.

## Defect found and fixed locally

The deployed OpenAPI document advertises `/EarthGravity/api/swagger/merged/swagger.json` as its API server base. An operation such as `/EarthGravity` would therefore be appended to the schema URL rather than to `/EarthGravity/api`.

Cause: `app.Map` moves the matched schema route into Request.PathBase. SwaggerMiddlewareExtensions used that expanded PathBase when constructing its server URL.

Fix: handle the exact schema path in ordinary middleware, preserving the service PathBase. Added a regression test that checks the forwarded public host, the API root and resolution of an advertised operation to a working endpoint. The same fix and regression test were applied to EarthMagneticField's identical middleware; its service tests passed 16/16.

The changes are local source changes. Running deployments still require a new service image and redeployment to receive this correction. Direct REST calculations, MCP and the WebApp calculator are working with the existing deployments.

No package was published and no deployment was changed by this verification.
