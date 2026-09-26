using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Writers;
using NUnit.Framework;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.Drilling.EarthGravity.Model;
using OSDC.Drilling.EarthGravity.Service.Mcp;
using Swashbuckle.AspNetCore.Swagger;

namespace OSDC.Drilling.EarthGravity.ServiceTest;

public class SemanticContractTests
{
    private string statisticsFile = null!;
    private WebApplicationFactory<Program> factory = null!;

    [SetUp]
    public void Setup()
    {
        statisticsFile = Path.Combine(Path.GetTempPath(), "earthgravity-semantics-" + Guid.NewGuid() + ".json");
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("EarthGravity:UsageStatisticsFile", statisticsFile));
    }

    [TearDown]
    public void TearDown()
    {
        factory.Dispose();
        File.Delete(statisticsFile);
    }

    [Test]
    public void RestAndMcpPublishTheSameModelOwnedSemantics()
    {
        var tools = factory.Services.GetServices<IMcpTool>().ToDictionary(t => t.Name);
        var evaluate = tools["earth_gravity_evaluate"];
        var provider = factory.Services.GetRequiredService<ISwaggerProvider>();
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        provider.GetSwagger("v1").SerializeAsV3(new OpenApiJsonWriter(writer));
        var schemas = JsonNode.Parse(writer.ToString())!["components"]!["schemas"]!;
        foreach (var (type, mcpSchema) in new[] {
            (typeof(EarthGravityPosition), evaluate.OutputSchema["$defs"]!["position"]!),
            (typeof(EarthGravityVector), evaluate.OutputSchema["$defs"]!["gravity"]!),
            (typeof(EarthGravityModelInfo), tools["earth_gravity_get_model_info"].OutputSchema) })
        {
            var rest = schemas[type.FullName!]!;
            foreach (var property in type.GetProperties())
            {
                var expected = SemanticMetadata.For(property);
                Assert.That(expected, Is.Not.Null, property.Name);
                Assert.That(JsonNode.DeepEquals(rest["properties"]![property.Name]![SemanticMetadata.ExtensionName], expected), Is.True, "REST " + property.Name);
                Assert.That(JsonNode.DeepEquals(mcpSchema["properties"]![property.Name]![SemanticMetadata.ExtensionName], expected), Is.True, "MCP " + property.Name);
            }
        }
        var inputDepth = evaluate.InputSchema["properties"]!["Positions"]!["items"]!["properties"]!["Depth"]![SemanticMetadata.ExtensionName];
        Assert.That(JsonNode.DeepEquals(inputDepth, SemanticMetadata.For(typeof(EarthGravityPosition).GetProperty("Depth")!)), Is.True);
        Assert.That(tools, Has.Count.EqualTo(3));
    }
}
