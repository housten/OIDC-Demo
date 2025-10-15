namespace MetricsApi.Authorization; 

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public class ScopeOrRoleHandler : AuthorizationHandler<ScopeOrRoleRequirement>
{
    private readonly ILogger<ScopeOrRoleHandler> _logger;

    public ScopeOrRoleHandler(ILogger<ScopeOrRoleHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeOrRoleRequirement requirement)
    {
        _logger.LogInformation("Checking authorization for scope: {Scope} or role: {Role}", requirement.Scope, requirement.Role);
        
        // Log all claims for debugging
        foreach (var claim in context.User.Claims)
        {
            _logger.LogDebug("Claim - Type: {Type}, Value: {Value}", claim.Type, claim.Value);
        }

        // Check for the 'scp' claim for delegated permissions (user context)
        var scopeClaim = context.User.FindFirst(c => c.Type == "http://schemas.microsoft.com/identity/claims/scope" || c.Type == "scp");
        if (scopeClaim != null)
        {
            var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _logger.LogInformation("Found scope claim with values: {Scopes}", string.Join(", ", scopes));
            
            if (scopes.Contains(requirement.Scope))
            {
                _logger.LogInformation("Authorization succeeded via scope: {Scope}", requirement.Scope);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // Check for the 'roles' claim for application permissions (app context)
        var roleClaim = context.User.FindFirst(c => c.Type == ClaimTypes.Role || c.Type == "roles");
        if (roleClaim != null)
        {
            var roles = roleClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _logger.LogInformation("Found role claim with values: {Roles}", string.Join(", ", roles));
            
            if (roles.Contains(requirement.Role))
            {
                _logger.LogInformation("Authorization succeeded via role: {Role}", requirement.Role);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }
        
        _logger.LogWarning("Authorization failed - no matching scope ({Scope}) or role ({Role}) found", requirement.Scope, requirement.Role);
        return Task.CompletedTask;
    }
}
