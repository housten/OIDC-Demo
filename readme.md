# Project Overview

## Objective
Create a .NET API secured with Azure AD OIDC to manage and report test metrics (pass/fail counts, durations).

## Endpoints
- **POST /api/tests/result**: Submit test results.
- **GET /api/tests/summary**: Retrieve summary metrics.
- **POST /api/tests/clear**: Clear stored results.

## Dependencies
The project uses several NuGet packages, including:
- `Microsoft.AspNetCore.Authentication.OpenIdConnect`
- `Swashbuckle.AspNetCore` for Swagger integration.
- `Microsoft.Identity.Web` for Azure AD authentication.

## Configuration Files
- **appsettings.json**: Contains configuration settings for logging and Azure AD.
- **appsettings.Development.json**: Similar to `appsettings.json`, but may contain development-specific settings.

## Local Development
- **User Secrets**: Recommended for storing sensitive values like Azure AD credentials.
- **Running the API**: Use the command `dotnet run` to start the API locally.

## Azure Deployment
### Resource Naming
- **Resource Group**: `rg-metrics-OIDC-demo`
- **App Service Plan**: `asp-metrics-OIDC-demo`
- **Web App**: `metricsapi-OIDC-demo-app`

### Azure CLI Commands
Provision resources using Azure CLI:
```bash
az group create --name $resourceGroup --location $location
az appservice plan create --name $appServicePlan --resource-group $resourceGroup --sku B1
az webapp create --name $webAppName --resource-group $resourceGroup --plan $appServicePlan --runtime "dotnet:9"
```