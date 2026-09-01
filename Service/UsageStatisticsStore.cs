using System.Text.Json;
using OSDC.Drilling.EarthGravity.Model;

namespace OSDC.Drilling.EarthGravity.Service;

public sealed class UsageStatisticsStore : BackgroundService
{
    private readonly string filePath_;
    private readonly TimeSpan saveInterval_;
    private readonly ILogger<UsageStatisticsStore> logger_;
    private readonly SemaphoreSlim saveLock_ = new(1, 1);
    private StatisticsSnapshot lastSaved_;

    public UsageStatisticsStore(EarthGravityServiceOptions options, IHostEnvironment environment,
        ILogger<UsageStatisticsStore> logger)
    {
        filePath_ = Path.IsPathRooted(options.UsageStatisticsFile)
            ? options.UsageStatisticsFile
            : Path.Combine(environment.ContentRootPath, options.UsageStatisticsFile);
        saveInterval_ = TimeSpan.FromSeconds(options.UsageStatisticsSaveIntervalSeconds);
        logger_ = logger;
        Statistics = Load();
        lastSaved_ = StatisticsSnapshot.From(Statistics);
    }

    public UsageStatisticsEarthGravity Statistics { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(saveInterval_);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync(CancellationToken.None);
    }

    internal async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        StatisticsSnapshot snapshot = StatisticsSnapshot.From(Statistics);
        if (snapshot == lastSaved_)
        {
            return;
        }

        await saveLock_.WaitAsync(cancellationToken);
        try
        {
            snapshot = StatisticsSnapshot.From(Statistics);
            if (snapshot == lastSaved_)
            {
                return;
            }

            string? directory = Path.GetDirectoryName(filePath_);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = filePath_ + ".tmp";
            await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(snapshot, JsonSettings.Options), cancellationToken);
            File.Move(temporaryPath, filePath_, true);
            lastSaved_ = snapshot;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger_.LogError(ex, "Unable to persist Earth Gravity usage statistics to {StatisticsFile}", filePath_);
        }
        finally
        {
            saveLock_.Release();
        }
    }

    private UsageStatisticsEarthGravity Load()
    {
        if (!File.Exists(filePath_))
        {
            return new UsageStatisticsEarthGravity();
        }

        try
        {
            StatisticsSnapshot? snapshot = JsonSerializer.Deserialize<StatisticsSnapshot>(File.ReadAllText(filePath_), JsonSettings.Options);
            return snapshot is null
                ? new UsageStatisticsEarthGravity()
                : UsageStatisticsEarthGravity.FromTotals(snapshot.StartedAt, snapshot.RestEvaluations,
                    snapshot.MCPEvaluations, snapshot.FailedEvaluations, snapshot.PositionsEvaluated,
                    snapshot.ModelInfoRequests, snapshot.StatisticsRequests);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger_.LogWarning(ex, "Unable to load Earth Gravity usage statistics from {StatisticsFile}; counters will start at zero", filePath_);
            return new UsageStatisticsEarthGravity();
        }
    }

    private sealed record StatisticsSnapshot(DateTimeOffset StartedAt, long RestEvaluations, long MCPEvaluations,
        long FailedEvaluations, long PositionsEvaluated, long ModelInfoRequests, long StatisticsRequests)
    {
        public static StatisticsSnapshot From(UsageStatisticsEarthGravity value) =>
            new(value.StartedAt, value.RestEvaluations, value.MCPEvaluations, value.FailedEvaluations,
                value.PositionsEvaluated, value.ModelInfoRequests, value.StatisticsRequests);
    }
}
