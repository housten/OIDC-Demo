using MetricsApi.Authorization; 
using MetricsApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Azure AD auth
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// NEW: Configure the JWT Bearer to correctly map the 'roles' claim for application permissions.
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.RoleClaimType = "roles";
});

// NEW: Register the custom authorization handler.
builder.Services.AddSingleton<IAuthorizationHandler, ScopeOrRoleHandler>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;

    // NEW: Add the custom policy for checking scope OR role.
    options.AddPolicy("CanSubmitMetrics", policy =>
        policy.AddRequirements(new ScopeOrRoleRequirement("Metrics.Submit", "Metrics.ReadWrite")));
});

builder.Services.AddSingleton<IMetricsStore, MetricsStore>();

// Swagger + security definition
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Metrics API", Version = "v1" });

    var scopeConfig = builder.Configuration["Swagger:Scope"];
    var scopeDisplay = builder.Configuration["Swagger:ScopeDescription"] ?? "Access Metrics API";
    string[] scopes;

    if (string.IsNullOrWhiteSpace(scopeConfig))
    {
        var apiClientId = builder.Configuration["AzureAd:ClientId"];
        scopes = new[] { $"api://{apiClientId}/.default" };
    }
    else
    {
        scopes = scopeConfig.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    options.AddSecurityDefinition("oauth2", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
        Flows = new()
        {
            AuthorizationCode = new()
            {
                AuthorizationUrl = new Uri($"{builder.Configuration["AzureAd:Instance"]}{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"{builder.Configuration["AzureAd:Instance"]}{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/token"),
                Scopes = scopes.ToDictionary(s => s, s => scopeDisplay)
            }
        }
    });
    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new List<string>()
        }
    });
});


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Metrics API v1");

    var swaggerClientId = app.Configuration["Swagger:ClientId"];
    if (!string.IsNullOrEmpty(swaggerClientId))
    {
        options.OAuthClientId(swaggerClientId);
        options.OAuthUsePkce();
    }

    var swaggerScopes = app.Configuration["Swagger:Scope"]?.Split(' ');
    var singleScope = app.Configuration["Swagger:Scope"] ?? app.Configuration["AzureAd:Audience"];

    if (swaggerScopes is { Length: > 0 })
    {
        options.OAuthScopes(swaggerScopes);
    }
    else if (!string.IsNullOrEmpty(singleScope))
    {
        options.OAuthScopes(singleScope);
    }
});


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
