using MetricsApi.Models;

namespace MetricsApi.Services;

public interface IMetricsStore
{
    void Add(TestResult result);
    TestSummary GetSummary();
    void Clear();
}