using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OSDC.Drilling.EarthGravity.Model;

namespace OSDC.Drilling.EarthGravity.Service.Mcp;

internal sealed class McpServerToolAdapter : McpServerTool
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null,
        PropertyNameCaseInsensitive = true
    };
    private readonly IMcpTool tool_;
    private readonly ILogger logger_;
    private readonly Tool protocolTool_;

    public McpServerToolAdapter(IMcpTool tool, ILoggerFactory loggerFactory)
    {
        tool_ = tool;
        logger_ = loggerFactory.CreateLogger(tool.GetType());
        protocolTool_ = new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = JsonSerializer.SerializeToElement(tool.InputSchema, JsonOptions),
            OutputSchema = JsonSerializer.SerializeToElement(tool.OutputSchema, JsonOptions)
        };
    }

    public override Tool ProtocolTool => protocolTool_;
    public override IReadOnlyList<object> Metadata { get; } = Array.Empty<object>();

    public override async ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
    {
        var arguments = new JsonObject();
        if (request.Params?.Arguments is { } suppliedArguments)
        {
            foreach ((string name, JsonElement value) in suppliedArguments)
                arguments[name] = JsonNode.Parse(value.GetRawText());
        }

        try
        {
            JsonNode? result = await tool_.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
            return new CallToolResult
            {
                StructuredContent = result is null
                    ? null
                    : JsonSerializer.SerializeToElement(result, JsonOptions)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (EarthGravityValidationException exception)
        {
            JsonNode problem = JsonSerializer.SerializeToNode(new EarthGravityValidationProblem
            {
                Message = exception.Message,
                Errors = exception.Errors.ToList()
            }, JsonOptions)!;
            return new CallToolResult
            {
                IsError = true,
                StructuredContent = JsonSerializer.SerializeToElement(problem, JsonOptions),
                Content = { new TextContentBlock { Text = problem.ToJsonString(JsonOptions) } }
            };
        }
        catch (Exception exception)
        {
            logger_.LogWarning(exception, "MCP tool {ToolName} failed.", tool_.Name);
            return new CallToolResult
            {
                IsError = true,
                Content = { new TextContentBlock { Text = exception.Message } }
            };
        }
    }
}
