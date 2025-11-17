namespace MetricsApi.Models;

public record TestSummary(
    int Total,
    int Passed,
    int Failed,
    int Skipped,
    string? LatestBuildId,
    DateTimeOffset? LastSubmittedAt);
