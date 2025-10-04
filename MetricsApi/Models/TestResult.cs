namespace MetricsApi.Models;

public record TestResult(
    string BuildId,
    string TestName,
    TestOutcome Outcome,
    double? DurationSeconds,
    DateTimeOffset SubmittedAt);
