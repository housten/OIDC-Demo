using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore.Hosting;

namespace MetricsApiAWS;

public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        // Use your Program.cs bootstrapping
        builder.UseStartup<StartupShim>();
    }
}

