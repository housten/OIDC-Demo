# Local Development Guide

> **Reminder:** Update the resource names, region, and identifiers below to match your environment. The plan currently assumes:
> - Resource group: `rg-metrics-OIDC-demo`
> - App Service plan: `asp-metrics-OIDC-demo` (B1, `westeurope`)
> - Web App: `metricsapi-OIDC-demo-app`
> - Azure AD app registration (API): `Metrics Api OIDC Demo` (`api://metricsapi-OIDC-demo-app/.default`)
> - Swagger client app registration: `Metrics Api OIDC Demo Swagger`

Follow these steps to configure and run the Metrics API locally with Azure AD protection.

## 1. Prerequisites
- .NET SDK 9.0 (or the SDK that matches `TargetFramework` in `MetricsApi.csproj`).
- Azure subscription with permissions to create app registrations.
- Azure CLI (`az`) logged into the tenant that will host the app registrations.

## 2. Collect Azure AD identifiers
Create (or reuse) the following Azure AD app registrations:

1. **Metrics API (protected resource)**
   - Expose an API (`api://<api-client-id>`)
   - Define a delegated permission scope (e.g., `Metrics.Submit`) and/or rely on the default scope `api://<api-client-id>/.default`.
2. **Metrics API Client (for Swagger / GitHub Actions)**
   - Public client or daemon app granted access to the Metrics API scope.

Record these values:

| Setting | Description |
|---------|-------------|
| `TenantId` | Directory (tenant) ID that owns the app registrations. |
| `AzureAd:ClientId` | The Metrics API application (App registration) client ID (GUID). |
| `AzureAd:Audience` | The Application ID URI of the API (e.g., `api://<api-client-id>/.default`). |
| `Swagger:ClientId` | Client ID of the Swagger/public client registration. |
| `Swagger:Scope` | Scope granted to the Swagger client (e.g., `api://<api-client-id>/.default`). |

Update `appsettings.json` placeholders if you prefer static values, or keep them as-is and override with user secrets.

## 3. Configure user secrets (preferred)
Use the ASP.NET Core secrets store to avoid storing sensitive values in files:

```powershell
cd c:\Workspaces\oidcDemo\MetricsApi

dotnet user-secrets init

dotnet user-secrets set "AzureAd:TenantId" "<tenant-guid>"
dotnet user-secrets set "AzureAd:ClientId" "<api-app-client-id>"
dotnet user-secrets set "AzureAd:Audience" "api://<api-app-client-id>/.default"
dotnet user-secrets set "Swagger:ClientId" "<swagger-client-id>"
dotnet user-secrets set "Swagger:Scope" "api://<api-app-client-id>/.default"
```

If you expose a custom scope (e.g., `api://<api-id>/Metrics.Submit`), set `Swagger:Scope` to that value.

## 4. Run the API locally
```powershell
cd c:\Workspaces\oidcDemo\MetricsApi

dotnet run
```

By default the app listens on HTTPS (see console output for the exact port).

## 5. Acquire an access token for testing
Use Azure CLI to request a delegated token for the Swagger/client registration:

```powershell
az account get-access-token --tenant <tenant-guid> --client-id <swagger-client-id> --scopes "api://<api-app-client-id>/.default"
```

Copy the `accessToken` from the output for use in HTTP requests.

Alternatively, open `https://localhost:<port>/swagger` and authenticate via the Swagger UI using the same client ID.

## 6. Call the API endpoints
Update `MetricsApi.http` or use your preferred REST client with the bearer token:

```
POST https://localhost:<port>/api/tests/result
Authorization: Bearer <token>
Content-Type: application/json

{
  "buildId": "build-123",
  "testName": "Example.Test",
  "outcome": "Passed",
  "durationSeconds": 12.34
}
```

Then fetch the summary:

```
GET https://localhost:<port>/api/tests/summary
Authorization: Bearer <token>
```

Use `POST /api/tests/clear` to reset the in-memory store when needed.
