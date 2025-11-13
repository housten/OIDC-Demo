using System;
using MetricsApi.Models;
using MetricsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace MetricsApi.Controllers;

[ApiController]
[Route("api/tests")]
public class TestResultsController : ControllerBase
{
    private static readonly string[] SupportedOutcomes = Enum.GetNames<TestOutcome>();
    private readonly IMetricsStore _metricsStore;

    public TestResultsController(IMetricsStore metricsStore)
    {
        _metricsStore = metricsStore;
    }

    [HttpPost("result")]
    public ActionResult SubmitResult([FromBody] TestResultRequest request)
    {
        if (!Enum.TryParse<TestOutcome>(request.Outcome, true, out var outcome))
        {
            return BadRequest($"Unsupported outcome '{request.Outcome}'. Valid values: {string.Join(", ", SupportedOutcomes)}");
        }

        var result = new TestResult(
            request.BuildId,
            request.TestName,
            outcome,
            request.DurationSeconds,
            request.CompletedAt ?? DateTimeOffset.UtcNow);

        _metricsStore.Add(result);

        return Accepted();
    }

    [HttpGet("summary")]
    public ActionResult<TestSummaryResponse> GetSummary()
    {
        var summary = _metricsStore.GetSummary();

        var response = new TestSummaryResponse
        {
            Total = summary.Total,
            Passed = summary.Passed,
            Failed = summary.Failed,
            Skipped = summary.Skipped,
            LatestBuildId = summary.LatestBuildId,
            LastSubmittedAt = summary.LastSubmittedAt
        };

        return Ok(response);
    }

    [HttpPost("clear")]
    public IActionResult Clear()
    {
        _metricsStore.Clear();
        return NoContent();
    }
}
