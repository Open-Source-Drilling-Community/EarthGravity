using System.Text.Json.Nodes;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OSDC.Drilling.EarthGravity.Service;

/// <summary>Publish the same model-owned declarations in REST as in MCP, without changing payloads.</summary>
public sealed class SemanticSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (SemanticMetadata.For(context.Type) is JsonObject metadata)
            schema.Extensions[SemanticMetadata.ExtensionName] = OpenApiAnyFactory.CreateFromJson(metadata.ToJsonString());
        foreach (var property in context.Type.GetProperties())
        {
            if (!schema.Properties.TryGetValue(property.Name, out var target) || SemanticMetadata.For(property) is not JsonObject binding)
                continue;
            // OpenAPI 3.0 ignores siblings of $ref: put property-specific roles on an allOf wrapper.
            if (target.Reference is not null)
            {
                target = new OpenApiSchema { AllOf = new List<OpenApiSchema> { target } };
                schema.Properties[property.Name] = target;
            }
            target.Extensions[SemanticMetadata.ExtensionName] = OpenApiAnyFactory.CreateFromJson(binding.ToJsonString());
        }
    }
}
