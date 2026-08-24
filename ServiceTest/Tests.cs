using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OSDC.Drilling.EarthGravity.ModelShared;
using OSDC.Drilling.EarthGravity.Service.Mcp;

namespace OSDC.Drilling.EarthGravity.ServiceTest;

public class Tests
{
    private WebApplicationFactory<Program> factory_ = null!;
    private HttpClient httpClient_ = null!;
    private Client generatedClient_ = null!;

    [SetUp]
    public void Setup()
    {
        factory_ = new WebApplicationFactory<Program>();
        httpClient_ = factory_.CreateClient();
        generatedClient_ = new Client("http://localhost/EarthGravity/api/", httpClient_);
    }

    [TearDown]
    public void TearDown()
    {
        httpClient_.Dispose();
        factory_.Dispose();
    }

    [Test]
    public async Task GeneratedModelSharedOutClientEvaluatesEarthGravity()
    {
        var request = PseudoConstructors.ConstructEarthGravityEvaluationRequest();
        request.Positions.First().Latitude = 0.5;
        request.Positions.First().Longitude = 1.0;
        request.Positions.First().Depth = 1000;

        EarthGravityEvaluationResponse response = await generatedClient_.EvaluateEarthGravityAsync(request);
        Assert.Multiple(() =>
        {
            Assert.That(response.Samples, Has.Count.EqualTo(1));
            Assert.That(response.Samples.First().Gravity.Magnitude, Is.GreaterThan(9));
            Assert.That(response.Model.ID, Is.EqualTo("EGM1996A"));
        });
    }

    [Test]
    public void InvalidRequestReturnsUnprocessableEntityThroughGeneratedClient()
    {
        var request = PseudoConstructors.ConstructEarthGravityEvaluationRequest();
        request.Positions.First().Latitude = Math.PI;
        ApiException exception = Assert.CatchAsync<ApiException>(async () => await generatedClient_.EvaluateEarthGravityAsync(request))!;
        Assert.That(exception.StatusCode, Is.EqualTo((int)HttpStatusCode.UnprocessableEntity));
    }

    [TestCase("/EarthGravity/api/EarthGravity")]
    [TestCase("/earthgravity/api/earthgravity")]
    public async Task ServiceEntryEndpointReturnsModelInformation(string path)
    {
        HttpResponseMessage response = await httpClient_.GetAsync(path);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(document.RootElement.GetProperty("ID").GetString(), Is.EqualTo("EGM1996A"));
            Assert.That(document.RootElement.GetProperty("ReferenceEllipsoid").GetString(), Is.EqualTo("WGS84"));
        });
    }

    [Test]
    public void UsageStatisticsAreNotRegisteredAsMCPTools()
    {
        string[] names = factory_.Services.GetServices<IMcpTool>().Select(tool => tool.Name).Order().ToArray();
        Assert.That(names, Is.EqualTo(new[] { "earth_gravity_evaluate", "earth_gravity_get_model_info", "ping" }));
    }

    [Test]
    public async Task MCPHttpToolListPublishesCompleteSchemasWithoutUsageStatistics()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/EarthGravity/api/mcp");
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        request.Content = new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}""", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await httpClient_.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        string dataLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith("data:", StringComparison.Ordinal));
        using JsonDocument document = JsonDocument.Parse(dataLine["data:".Length..].Trim());
        JsonElement tools = document.RootElement.GetProperty("result").GetProperty("tools");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(tools.GetArrayLength(), Is.EqualTo(3));
            Assert.That(tools.EnumerateArray().Select(tool => tool.GetProperty("name").GetString()),
                Is.EquivalentTo(new[] { "ping", "earth_gravity_get_model_info", "earth_gravity_evaluate" }));
            Assert.That(tools.EnumerateArray().All(tool => tool.TryGetProperty("inputSchema", out _)), Is.True);
            Assert.That(tools.EnumerateArray().All(tool => tool.TryGetProperty("outputSchema", out _)), Is.True);
            Assert.That(content, Does.Not.Contain("usage_statistics").IgnoreCase);
        });

        JsonElement evaluate = tools.EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "earth_gravity_evaluate");
        Assert.Multiple(() =>
        {
            Assert.That(evaluate.GetProperty("description").GetString(), Does.Contain("local east-north-up (ENU)"));
            Assert.That(evaluate.GetProperty("description").GetString(), Does.Contain("PositionIndex is zero-based"));
            Assert.That(evaluate.GetProperty("outputSchema").GetProperty("properties").TryGetProperty("Samples", out _), Is.True);
            Assert.That(evaluate.GetProperty("outputSchema").GetProperty("$defs").TryGetProperty("gravity", out _), Is.True);
        });

        JsonElement modelInfo = tools.EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "earth_gravity_get_model_info");
        Assert.That(modelInfo.GetProperty("outputSchema").GetProperty("properties").TryGetProperty("CoefficientSHA256", out _), Is.True);
    }

    [TestCase("/EarthGravity/api/health/live")]
    [TestCase("/EarthGravity/api/health/ready")]
    [TestCase("/EarthGravity/api/metrics")]
    [TestCase("/EarthGravity/api/swagger/merged/swagger.json")]
    public async Task OperationalEndpointsAreAvailable(string path) =>
        Assert.That((await httpClient_.GetAsync(path)).StatusCode, Is.EqualTo(HttpStatusCode.OK));
}
