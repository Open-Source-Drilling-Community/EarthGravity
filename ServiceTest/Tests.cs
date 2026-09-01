using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
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
    private string statisticsFile_ = null!;

    [SetUp]
    public void Setup()
    {
        statisticsFile_ = Path.Combine(Path.GetTempPath(), "earthgravity-tests", Guid.NewGuid().ToString(), "statistics.json");
        factory_ = CreateFactory(statisticsFile_);
        httpClient_ = factory_.CreateClient();
        generatedClient_ = new Client("http://localhost/EarthGravity/api/", httpClient_);
    }

    [TearDown]
    public void TearDown()
    {
        httpClient_.Dispose();
        factory_.Dispose();
        string? directory = Path.GetDirectoryName(statisticsFile_);
        if (directory is not null && Directory.Exists(directory) &&
            Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
        {
            Directory.Delete(directory, true);
        }
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
            Assert.That(response.Samples.First().Gravity.Down, Is.GreaterThan(9));
            Assert.That(response.Model.ID, Is.EqualTo("EGM1996A"));
        });
    }

    [Test]
    public async Task UsageStatisticsSurviveServiceRestart()
    {
        string statisticsFile = Path.Combine(Path.GetTempPath(), "earthgravity-restart-tests",
            Guid.NewGuid().ToString(), "statistics.json");
        try
        {
            DateTimeOffset startedAt;
            using (var firstFactory = CreateFactory(statisticsFile))
            using (HttpClient firstHttpClient = firstFactory.CreateClient())
            {
                var firstClient = new Client("http://localhost/EarthGravity/api/", firstHttpClient);
                await firstClient.GetEarthGravityEntryAsync();
                UsageStatisticsEarthGravity beforeRestart = await firstClient.GetEarthGravityUsageStatisticsAsync();
                startedAt = beforeRestart.StartedAt;
                Assert.That(beforeRestart.ModelInfoRequests, Is.GreaterThanOrEqualTo(1));
            }

            Assert.That(File.Exists(statisticsFile), Is.True);

            using var secondFactory = CreateFactory(statisticsFile);
            using HttpClient secondHttpClient = secondFactory.CreateClient();
            var secondClient = new Client("http://localhost/EarthGravity/api/", secondHttpClient);
            UsageStatisticsEarthGravity restored = await secondClient.GetEarthGravityUsageStatisticsAsync();
            Assert.Multiple(() =>
            {
                Assert.That(restored.ModelInfoRequests, Is.GreaterThanOrEqualTo(1));
                Assert.That(restored.StartedAt, Is.EqualTo(startedAt));
                Assert.That(restored.Scope, Is.EqualTo("persistent-service"));
            });
        }
        finally
        {
            string? directory = Path.GetDirectoryName(statisticsFile);
            if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory, true);
        }
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
            Assert.That(evaluate.GetProperty("description").GetString(), Does.Contain("local north-east-down (NED)"));
            Assert.That(evaluate.GetProperty("description").GetString(), Does.Contain("PositionIndex is zero-based"));
            Assert.That(evaluate.GetProperty("outputSchema").GetProperty("properties").TryGetProperty("Samples", out _), Is.True);
            Assert.That(evaluate.GetProperty("outputSchema").GetProperty("$defs").TryGetProperty("gravity", out _), Is.True);
        });
        JsonElement gravityProperties = evaluate.GetProperty("outputSchema").GetProperty("$defs")
            .GetProperty("gravity").GetProperty("properties");
        Assert.Multiple(() =>
        {
            Assert.That(gravityProperties.TryGetProperty("North", out _), Is.True);
            Assert.That(gravityProperties.TryGetProperty("East", out _), Is.True);
            Assert.That(gravityProperties.TryGetProperty("Down", out _), Is.True);
            Assert.That(gravityProperties.TryGetProperty("Up", out _), Is.False);
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

    private static WebApplicationFactory<Program> CreateFactory(string statisticsFile) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("EarthGravity:UsageStatisticsFile", statisticsFile);
            builder.UseSetting("EarthGravity:UsageStatisticsSaveIntervalSeconds", "3600");
        });
}
