using System.ComponentModel.DataAnnotations;

namespace MetricsApi.Models;

public class TestResultRequest
{
    [Required]
    public string BuildId { get; set; } = string.Empty;

    [Required]
    public string TestName { get; set; } = string.Empty;

    [Required]
    public string Outcome { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double? DurationSeconds { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
