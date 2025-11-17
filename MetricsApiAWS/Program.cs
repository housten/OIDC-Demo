using MetricsApi.Services;
using Amazon.Lambda.AspNetCoreServer.Hosting;
// 1. add references
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
// end 1.


var builder = WebApplication.CreateBuilder(args);
// Enable Lambda adaptation
//builder.Services.AddAWSLambdaHosting(Amazon.Lambda.AspNetCoreServer.Hosting.LambdaEventSource.HttpApi);
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
// 2. Set up configuration for JWT authentication
// Get AWS Cognito configuration
var region = builder.Configuration["AWS:Region"];
var userPoolId = builder.Configuration["AWS:UserPoolId"];
var appClientId = builder.Configuration["AWS:AppClientId"];
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
        // Check IAM principal (Function URL)
        if (ctx.User.HasClaim(c => c.Type == "iam-principal")) return true;
        
        // Check JWT scope claim (space-separated string)
        var scopeClaim = ctx.User.FindFirst("scope")?.Value;
        return scopeClaim != null && scopeClaim.Contains("metrics-api/read");
    }));
    
    options.AddPolicy("WriteAccess", p => p.RequireAssertion(ctx =>
    {
        // Check IAM principal (Function URL)
        if (ctx.User.HasClaim(c => c.Type == "iam-principal")) return true;
        
        // Check JWT scope claim (space-separated string)
        var scopeClaim = ctx.User.FindFirst("scope")?.Value;
        return scopeClaim != null && scopeClaim.Contains("metrics-api/write");
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
// 6. Enable authentication and authorization middleware
// Add authentication and authorization middleware
app.Use(async (context, next) =>
{
    // Check if request came via Lambda Function URL with IAM auth
    // Lambda context is available via ILambdaContext or headers
    var requestContext = context.Request.Headers["x-amzn-lambda-request-context"].FirstOrDefault();
    
    if (!string.IsNullOrEmpty(requestContext) && requestContext.Contains("invokedFunctionArn"))
    {
        // IAM-authenticated request via Function URL
        // Create a claims identity for authorization policies
        var claims = new List<Claim>
        {
            new Claim("iam-principal", "github-actions"),
            new Claim(ClaimTypes.Role, "automation")
        };
        var identity = new ClaimsIdentity(claims, "IAM");
        context.User = new ClaimsPrincipal(identity);
        
        Console.WriteLine("Request authenticated via IAM (Function URL)");
    }
    
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
// end 6.

app.MapControllers();

app.Run();
