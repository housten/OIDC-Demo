using Amazon.Lambda.AspNetCoreServer.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MetricsApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Run ASP.NET Core inside Lambda for API Gateway HTTP API
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// GitHub OIDC configuration
var githubIssuer   = "https://token.actions.githubusercontent.com";
var githubAudience = "metrics-api";

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

        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadAccess",  p => p.RequireAuthenticatedUser());
    options.AddPolicy("WriteAccess", p => p.RequireAuthenticatedUser());
});

builder.Services.AddControllers();
builder.Services.AddSingleton<IMetricsStore, MetricsStore>();

// Swagger (optional but convenient)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.Use(async (ctx, next) =>
{
    Console.WriteLine($"Request: {ctx.Request.Method} {ctx.Request.Path}");
    foreach (var h in ctx.Request.Headers)
        Console.WriteLine($"HDR {h.Key}={h.Value}");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();