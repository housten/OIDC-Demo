# Azure Deployment Plan

This guide details the Azure resources, configuration, and deployment pipeline for the Metrics API.

## 1. Resource naming and settings

| Resource | Name | Notes |
|----------|------|-------|
| Resource Group | `rg-metrics-OIDC-demo` | Holds all demo resources. |
| App Service Plan | `asp-metrics-OIDC-demo` | Basic (B1) tier in `westeurope`. |
| Web App | `metricsapi-OIDC-demo-app` | Hosts the published Metrics API. |
| Azure AD App (API) | `Metrics Api OIDC Demo` | Exposes scope `api://metricsapi-OIDC-demo-app/Metrics.Submit` (optional) or default scope `api://metricsapi-OIDC-demo-app/.default`. |
| Azure AD App (Client) | `Metrics Api OIDC Demo Swagger` | Public client for Swagger & GitHub workflows. |

Adjust these names to comply with your naming conventions if necessary.

## 2. Azure CLI provisioning steps

```powershell
# Variables
$resourceGroup = "rg-metrics-OIDC-demo"
$location = "westeurope"
$appServicePlan = "asp-metrics-OIDC-demo"
$webAppName = "metricsapi-OIDC-demo-app"

# Create resource group
az group create --name $resourceGroup --location $location --tags Environment=Demo Owner=HeidiHousten

# Create App Service plan (B1 tier)
az appservice plan create --name $appServicePlan --resource-group $resourceGroup --sku B1 --tags Environment=Demo Owner=HeidiHousten

# Create Web App (Windows runtime for .NET)
az webapp create --name $webAppName --resource-group $resourceGroup --plan $appServicePlan --runtime "dotnet:9" --tags Environment=Demo Owner=HeidiHousten
```

> If you prefer infrastructure-as-code, translate the above into Bicep or Terraform templates.

## 3. App Service configuration (app settings)

After provisioning, configure environment settings for Azure AD:

```powershell
$tenantId = "1f39fa86-9689-41da-b81e-d60e10615bf0"
$apiClientId = "f3f36cfe-c1c7-4803-b9b8-0d704fc0354d"
$appAudience = "api://$apiClientId/.default"
$swaggerClientId = "1b86dcb0-6c9a-4c7d-9dc1-ea0fe799b443"

az webapp config appsettings set \
    --name $webAppName \
    --resource-group $resourceGroup \
    --settings \
        "AzureAd:Instance=https://login.microsoftonline.com/" \
        "AzureAd:TenantId=$tenantId" \
        "AzureAd:ClientId=$apiClientId" \
        "AzureAd:Audience=$appAudience" \
        "Swagger:ClientId=$swaggerClientId" \
        "Swagger:Scope=$appAudience" \
        "WEBSITE_RUN_FROM_PACKAGE=1" \
        "ASPNETCORE_ENVIRONMENT=Development" 
```

Optional settings:
- `Logging:LogLevel__Default=Information`
- `WEBSITE_RUN_FROM_PACKAGE=1` (when deploying via zip package)

## 4. Identity integration

Ensure the Azure AD API app registration has:
- Exposed scopes (e.g., default scope or `Metrics.Submit`).
- The Web App URL added as a redirect URI if you use Swagger Authorization Code flow locally.
- The Swagger/client app granted delegated permission to the API scope.
- Create federated credentials for GitHub Actions if using OIDC.
```
az ad app federated-credential create --id <APP_CLIENT_ID> --parameters '{
  "name": "github-main-deploy",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:housten/OIDC-Demo:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'
```
## 4. Role assignment (if using OIDC)
$spId=<APP_OBJECT_ID or APP_CLIENT_ID>
az role assignment create --assignee $spId --role "WebSite Contributor" --scope /subscriptions/<sub-id>/resourceGroups/rg-metrics-OIDC-demo

## 5. Deployment artifact

The GitHub Actions workflow will:
1. Run `dotnet publish -c Release -o publish`.
2. Zip the `publish` folder.
3. Deploy using `az webapp deploy` or the `azure/webapps-deploy` action with the zip file.

Artifact details:
- Publish directory: `MetricsApi/bin/Release/net9.0/publish/`
- Zip path (example): `MetricsApi/bin/Release/net9.0/MetricsApi.zip`

## 6. Validation checklist post-deployment

- Browse to `https://metricsapi-demo-app.azurewebsites.net/swagger` and authenticate.
- Submit a sample test result and verify summary endpoint data.
- Monitor App Service logs (`az webapp log tail`) for errors.
- Optionally enable Application Insights for richer telemetry.

## 7. GitHub Actions API smoke test workflow

The repository includes `.github/workflows/api_smoke_test.yml`, which runs a workload-identity authenticated smoke test against the deployed API. Complete the following setup before running it:

1. **Create or reuse a client application registration** with delegated access to the Metrics API scope. Add a GitHub federated credential (issuer `https://token.actions.githubusercontent.com`) scoped to `repo:housten/OIDC-Demo:ref:refs/heads/main`. Store the app's client ID in the repository secret `API_CLIENT_APP_ID`.
2. **Configure repository variables**: set `API_BASE_URL` to the deployed API root (e.g., `https://metricsapi-OIDC-demo-app.azurewebsites.net`) and `API_SCOPE` to the exposed audience (typically `api://<api-app-client-id>/.default`).
3. **Reuse the existing tenant secret** `AZURE_TENANT_ID` that already supports the deployment workflow.

When invoked (manually via *Run workflow*), the job:

- Exchanges the GitHub OIDC token for an Azure AD access token scoped to the API.
- Sends `POST /api/tests/result` with a sample payload that records contextual metadata.
- Calls `GET /api/tests/summary`, surfaces the JSON response in the workflow run summary, and uploads all request/response bodies as an artifact (`api-smoke-test`).

Review the uploaded artifacts to validate the API response. The workflow only touches the in-memory store; run `POST /api/tests/clear` afterwards if you want to reset the metrics.
