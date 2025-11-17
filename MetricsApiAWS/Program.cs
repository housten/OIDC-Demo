using MetricsApi.Services;
using MetricsApi.Authentication;
using Microsoft.AspNetCore.Authentication;


// 1. add references
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
// end 1.


var builder = WebApplication.CreateBuilder(args);

// 2. Set up configuration for JWT authentication
// Get AWS Cognito configuration 
var region = builder.Configuration["AWS:Region"] ?? builder.Configuration["AWS__Region"];
var userPoolId = builder.Configuration["AWS:UserPoolId"] ?? builder.Configuration["AWS__UserPoolId"];
//var appClientId = builder.Configuration["AWS:AppClientId"] ?? builder.Configuration["AWS__AppClientId"];
var issuer = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

// end 2.

// 3. Add JWT Bearer authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    .AddPolicyScheme("Smart", "Smart", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            // IAM-authenticated Function URL adds x-amzn-iam-identity
            if (context.Request.Headers.ContainsKey("x-amzn-iam-identity"))
                return "SigV4";
            return JwtBearerDefaults.AuthenticationScheme;
        };
    })

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
    })
    .AddScheme<AuthenticationSchemeOptions, SigV4AuthHandler>("SigV4", _ => { });
// end 3.

// 4. Add authorization policies based on scopes
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadAccess",  p => p.RequireAssertion(ctx =>
        ctx.User.FindFirst("scope")?.Value?.Contains("metrics-api/read") == true ||
        ctx.User.HasClaim(c => c.Type == "awsRoleArn")));
    options.AddPolicy("WriteAccess", p => p.RequireAssertion(ctx =>
        ctx.User.FindFirst("scope")?.Value?.Contains("metrics-api/write") == true ||
        ctx.User.HasClaim(c => c.Type == "awsRoleArn")));
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

// 6. Enable authentication and authorization middleware
app.UseAuthentication(); 
app.UseAuthorization();
// end 6.

app.MapControllers();
app.Run();
