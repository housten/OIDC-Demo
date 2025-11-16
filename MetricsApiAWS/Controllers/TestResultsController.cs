using System;
using MetricsApi.Models;
using MetricsApi.Services;

using Microsoft.Identity.Web.Resource;
// 1. add references
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// end 1.

namespace MetricsApi.Controllers;

[ApiController]
[Route("api/testresults")] //[Route("api/tests")]
[Authorize] // 2. protect the controller with [Authorize]
// end 2.   
public class TestResultsController : ControllerBase
{
    private static readonly string[] SupportedOutcomes = Enum.GetNames<TestOutcome>();
    private readonly IMetricsStore _metricsStore;
    public TestResultsController(IMetricsStore metricsStore)
    {
        _metricsStore = metricsStore;
    }

    [HttpPost("result")]
    [Authorize(Policy = "WriteAccess")] // 3. apply the "WriteAccess" policy to this endpoint
    public ActionResult SubmitResult([FromBody] TestResultRequest request)
    {
        Console.WriteLine("Lambda function invoked - result");

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
    [Authorize(Policy = "ReadAccess")] // 4. apply the "ReadAccess" policy to this endpoint
    public ActionResult<TestSummaryResponse> GetSummary()
    {
        Console.WriteLine("Lambda function invoked - summary");
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
    [Authorize(Policy = "WriteAccess")] // 5. apply the "WriteAccess" policy to this endpoint
    public IActionResult Clear()
    {
        _metricsStore.Clear();
        return NoContent();
    }
}
