namespace OSDC.Drilling.EarthGravity.Model;

public sealed class UsageStatisticsEarthGravity
{
    private long restEvaluations_;
    private long mcpEvaluations_;
    private long failedEvaluations_;
    private long positionsEvaluated_;
    private long modelInfoRequests_;
    private long statisticsRequests_;

    public UsageStatisticsEarthGravity() : this(DateTimeOffset.UtcNow, 0, 0, 0, 0, 0, 0) { }

    private UsageStatisticsEarthGravity(DateTimeOffset startedAt, long restEvaluations, long mcpEvaluations,
        long failedEvaluations, long positionsEvaluated, long modelInfoRequests, long statisticsRequests)
    {
        StartedAt = startedAt;
        restEvaluations_ = restEvaluations;
        mcpEvaluations_ = mcpEvaluations;
        failedEvaluations_ = failedEvaluations;
        positionsEvaluated_ = positionsEvaluated;
        modelInfoRequests_ = modelInfoRequests;
        statisticsRequests_ = statisticsRequests;
    }

    public DateTimeOffset StartedAt { get; }
    public string Scope => "persistent-service";
    public long RestEvaluations => Interlocked.Read(ref restEvaluations_);
    public long MCPEvaluations => Interlocked.Read(ref mcpEvaluations_);
    public long FailedEvaluations => Interlocked.Read(ref failedEvaluations_);
    public long PositionsEvaluated => Interlocked.Read(ref positionsEvaluated_);
    public long ModelInfoRequests => Interlocked.Read(ref modelInfoRequests_);
    public long StatisticsRequests => Interlocked.Read(ref statisticsRequests_);

    public void IncrementEvaluation(bool mcp, int positions)
    {
        if (mcp) Interlocked.Increment(ref mcpEvaluations_);
        else Interlocked.Increment(ref restEvaluations_);
        Interlocked.Add(ref positionsEvaluated_, Math.Max(positions, 0));
    }

    public void IncrementFailedEvaluation() => Interlocked.Increment(ref failedEvaluations_);
    public void IncrementModelInfo() => Interlocked.Increment(ref modelInfoRequests_);
    public void IncrementStatistics() => Interlocked.Increment(ref statisticsRequests_);

    public static UsageStatisticsEarthGravity FromTotals(DateTimeOffset startedAt, long restEvaluations,
        long mcpEvaluations, long failedEvaluations, long positionsEvaluated, long modelInfoRequests,
        long statisticsRequests) => new(startedAt, restEvaluations, mcpEvaluations, failedEvaluations,
            positionsEvaluated, modelInfoRequests, statisticsRequests);
}
