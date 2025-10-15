using Microsoft.AspNetCore.Authorization;

public class ScopeOrRoleRequirement : IAuthorizationRequirement
{
    public string Scope { get; }
    public string Role { get; }

    public ScopeOrRoleRequirement(string scope, string role)
    {
        Scope = scope;
        Role = role;
    }
}
