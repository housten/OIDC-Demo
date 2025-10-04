using System.Collections.Concurrent;
using System.Linq;
using MetricsApi.Models;

namespace MetricsApi.Services;

public class MetricsStore : IMetricsStore
{
    private readonly ConcurrentBag<TestResult> _results = new();

    public void Add(TestResult result) => _results.Add(result);

    public TestSummary GetSummary()
    {
        var snapshot = _results.ToArray();
        if (snapshot.Length == 0)
        {
            return new TestSummary(0, 0, 0, 0, null, null);
        }

        var passed = snapshot.Count(r => r.Outcome == TestOutcome.Passed);
        var failed = snapshot.Count(r => r.Outcome == TestOutcome.Failed);
        var skipped = snapshot.Count(r => r.Outcome == TestOutcome.Skipped);
        var latest = snapshot.OrderByDescending(r => r.SubmittedAt).First();

        return new TestSummary(
            snapshot.Length,
            passed,
            failed,
            skipped,
            latest.BuildId,
            latest.SubmittedAt);
    }

    public void Clear()
    {
        while (_results.TryTake(out _))
        {
        }
    }
}