using MetricsApi.Services;

using Microsoft.AspNetCore.Authentication;


// 1. add references
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
using Amazon.Lambda.AspNetCoreServer.Hosting; // <== important
// end 1.


var builder = WebApplication.CreateBuilder(args);
// Use Lambda hosting for HTTP API / Function URL
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
// 2. Set up configuration for JWT authentication
// Get AWS Cognito configuration 
var region = builder.Configuration["AWS:Region"] ?? builder.Configuration["AWS__Region"];
var userPoolId = builder.Configuration["AWS:UserPoolId"] ?? builder.Configuration["AWS__UserPoolId"];
//var appClientId = builder.Configuration["AWS:AppClientId"] ?? builder.Configuration["AWS__AppClientId"];

var issuer = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";
var githubIssuer   = "https://token.actions.githubusercontent.com";
var githubAudience = "metrics-api";

// end 2.

// 3. Add JWT Bearer authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = githubIssuer;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = githubIssuer,

            ValidateAudience         = true,
            ValidAudience            = githubAudience,

            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true
        };

        // GitHub uses "sub", "repository", "aud", etc.
        options.MapInboundClaims = false;
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
    // For now: any valid GitHub OIDC token can do both read and write.
    // You can tighten this later to specific repo/environment.
    options.AddPolicy("ReadAccess",  p => p.RequireAuthenticatedUser());
    options.AddPolicy("WriteAccess", p => p.RequireAuthenticatedUser());
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
//app.UseLambdaServer();  
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Metrics API v1");
});

//app.UseHttpsRedirection();
app.UseRouting();

// 6. Enable authentication and authorization middleware
app.Use(async (ctx, next) =>
{
    foreach (var h in ctx.Request.Headers)
        Console.WriteLine($"HDR {h.Key}={h.Value}");
    await next();
});
app.UseAuthentication(); 
app.UseAuthorization();
// end 6.

app.MapControllers();
app.Run();
