using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

public static class SwaggerMiddlewareExtensions
{
    public static void UseCustomSwagger(this IApplicationBuilder app, OpenApiDocument document, string relativePath)
    {
        app.Map(relativePath, branch => branch.Run(async context =>
        {
            string scheme = context.Request.Headers.ContainsKey("X-Forwarded-Host") ? "https" : context.Request.Scheme;
            string host = context.Request.Headers.ContainsKey("X-Forwarded-Host")
                ? context.Request.Headers["X-Forwarded-Host"].ToString()
                : context.Request.Host.Value;
            document.Servers = [new OpenApiServer { Url = $"{scheme}://{host}{context.Request.PathBase}" }];
            context.Response.ContentType = "application/json";
            string json = document.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json)
                .Replace("\"openapi\": \"3.0.4\"", "\"openapi\": \"3.0.3\"");
            await context.Response.WriteAsync(json);
        }));
    }

    public static OpenApiDocument ReadOpenApiDocument(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return new OpenApiStreamReader().Read(stream, out _);
    }
}
