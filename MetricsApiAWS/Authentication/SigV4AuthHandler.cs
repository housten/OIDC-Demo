using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MetricsApi.Authentication;

public sealed class SigV4AuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public SigV4AuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var iamHeader = Request.Headers["x-amzn-iam-identity"].FirstOrDefault();

        if (string.IsNullOrEmpty(iamHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        string roleArn   = "unknown";
        string accountId = "unknown";

        try
        {
            using var doc = JsonDocument.Parse(iamHeader);
            if (doc.RootElement.TryGetProperty("userArn", out var userArn))
                roleArn = userArn.GetString() ?? roleArn;
            if (doc.RootElement.TryGetProperty("accountId", out var acct))
                accountId = acct.GetString() ?? accountId;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse x-amzn-iam-identity header.");
        }

        var claims = new[]
        {
            new Claim("awsRoleArn", roleArn),
            new Claim("awsAccountId", accountId),
            new Claim("executionSource", "github-actions"),
            new Claim(ClaimTypes.Name, roleArn),
            new Claim("authType", "IAM-SigV4")
        };

        var identity  = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation("IAM identity authenticated: {RoleArn}", roleArn);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}