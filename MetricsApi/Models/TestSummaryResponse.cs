namespace MetricsApi.Models;

public class TestSummaryResponse
{
    public int Total { get; init; }

    public int Passed { get; init; }

    public int Failed { get; init; }

    public int Skipped { get; init; }

    public string? LatestBuildId { get; init; }

    public DateTimeOffset? LastSubmittedAt { get; init; }
}
