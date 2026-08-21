using ModelContextProtocol.Server;

namespace OSDC.Drilling.EarthGravity.Service.Mcp;

internal static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddEarthGravityMcpTool<T>(this IServiceCollection services)
        where T : class, IMcpTool
    {
        services.AddSingleton<T>();
        services.AddSingleton<IMcpTool>(provider => provider.GetRequiredService<T>());
        services.AddSingleton<McpServerTool>(provider => new McpServerToolAdapter(
            provider.GetRequiredService<T>(), provider.GetRequiredService<ILoggerFactory>()));
        return services;
    }
}
