using MetricsApi.Services;

using Microsoft.AspNetCore.OpenApi;
// 1. add references
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
// end 1.


var builder = WebApplication.CreateBuilder(args);
// Enable Lambda adaptation
//builder.Services.AddAWSLambdaHosting(Amazon.Lambda.AspNetCoreServer.Hosting.LambdaEventSource.HttpApi);
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
// 2. Set up configuration for JWT authentication
// Get AWS Cognito configuration 
var region = builder.Configuration["AWS:Region"] ?? builder.Configuration["AWS__Region"];
var userPoolId = builder.Configuration["AWS:UserPoolId"] ?? builder.Configuration["AWS__UserPoolId"];
var appClientId = builder.Configuration["AWS:AppClientId"] ?? builder.Configuration["AWS__AppClientId"];
var issuer = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

// end 2.

// 3. Add JWT Bearer authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = issuer;
        options.MetadataAddress = $"{issuer}/.well-known/openid-configuration";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = false, // Client credentials flow doesn't include audience
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
        // Enable for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully");
                return Task.CompletedTask;
            }
        };
    });
// end 3.

// 4. Add authorization policies based on scopes
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadAccess", p => p.RequireAssertion(ctx =>
    {
        var scopeClaim = ctx.User.FindFirst("scope")?.Value;
        if (scopeClaim?.Contains("metrics-api/read") == true) return true;
        return ctx.User.HasClaim(c => c.Type == "awsRoleArn"); // IAM path
    }));
    options.AddPolicy("WriteAccess", p => p.RequireAssertion(ctx =>
    {
        var scopeClaim = ctx.User.FindFirst("scope")?.Value;
        if (scopeClaim?.Contains("metrics-api/write") == true) return true;
        return ctx.User.HasClaim(c => c.Type == "awsRoleArn");
    }));
});
// end 4.

builder.Services.AddControllers();

builder.Services.AddSingleton<IMetricsStore, MetricsStore>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Metrics API", Version = "v1" });
    // 5. Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    // end 5.
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Metrics API v1");
});

//app.UseHttpsRedirection();
app.UseRouting();

// Middleware: enrich principal from Function URL or test header
app.Use(async (context, next) =>
{
    // If already authenticated (JWT), skip
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        await next();
        return;
    }

    // Function URL supplies this header (JSON)
    var lambdaCtxHeader = context.Request.Headers["x-amzn-lambda-request-context"].FirstOrDefault();


    if (!string.IsNullOrEmpty(lambdaCtxHeader))
    {
        string roleArn = "arn:aws:iam::140977286959:role/GitHubActionsOIDC-Lambda-Deployer";
        string accountId = "140977286959";
        string executionSource = "github-actions";

        try
        {
            // Parse Lambda context (JSON)
            using var doc = JsonDocument.Parse(lambdaCtxHeader);
            
            if (doc.RootElement.TryGetProperty("accountId", out var acct))
                accountId = acct.GetString() ?? accountId;
            
            if (doc.RootElement.TryGetProperty("invokedFunctionArn", out var fnArn))
            {
                var arn = fnArn.GetString();
                // Extract account and region from function ARN if needed
                Console.WriteLine($"Invoked via Function ARN: {arn}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse Lambda context: {ex.Message}");
        }

        var claims = new List<Claim>
        {
            new Claim("awsRoleArn", roleArn),
            new Claim("awsAccountId", accountId),
            new Claim("executionSource", executionSource),
            new Claim("authType", "IAM"),
            new Claim(ClaimTypes.Name, "GitHubActions")  // Add name claim
        };
                // KEY FIX: Mark identity as authenticated
        var identity = new ClaimsIdentity(claims, authenticationType: "IAM");
        Console.WriteLine($"Identity IsAuthenticated: {identity.IsAuthenticated}"); // Should be true
        context.User = new ClaimsPrincipal(identity);
        Console.WriteLine($"✅ Request authenticated via IAM (Role: {roleArn}, IsAuthenticated: {identity.IsAuthenticated})");

    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
// end 6.

app.MapControllers();

app.Run();
