using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MetricsApiAWS;

public class StartupShim
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Program.cs already configures services using WebApplicationBuilder,
        // so this can stay light. We’ll move service config into a shared method.
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Program.cs already configures the pipeline; we’ll keep things minimal here.
    }
}