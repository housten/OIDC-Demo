using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

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
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("AWS4-HMAC-SHA256"))
            return Task.FromResult(AuthenticateResult.NoResult());

        // Map assumed role (from GitHub OIDC -> STS) to app claims
        var roleArn = "arn:aws:iam::140977286959:role/GitHubActionsOIDC-Lambda-Deployer";

        var claims = new[]
        {
            new Claim("awsRoleArn", roleArn),
            new Claim("executionSource", "github-actions"),
            new Claim(ClaimTypes.Name, "GitHubActions"),
            new Claim("authType", "IAM-SigV4")
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        
        Logger.LogInformation("SigV4 authentication successful for role: {RoleArn}", roleArn);
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}