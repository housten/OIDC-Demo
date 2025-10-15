namespace MetricsApi.Authorization; 

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public class ScopeOrRoleHandler : AuthorizationHandler<ScopeOrRoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeOrRoleRequirement requirement)
    {
        // Check for the 'scp' claim for delegated permissions
        var scopeClaim = context.User.FindFirst(c => c.Type == "http://schemas.microsoft.com/identity/claims/scope");
        if (scopeClaim != null && scopeClaim.Value.Split(' ').Contains(requirement.Scope))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check for the 'roles' claim for application permissions
        var roleClaim = context.User.FindFirst(c => c.Type == ClaimTypes.Role || c.Type == "roles");
        if (roleClaim != null && roleClaim.Value.Split(' ').Contains(requirement.Role))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        // If neither claim is present and valid, the requirement fails.
        return Task.CompletedTask;
    }
}
