Objective: Create a simple .NET API secured with Azure AD OIDC, deploy it to Azure, and set up GitHub Actions for deployment and authenticated API calls.
The api will manage and report test metrics (pass/fail counts, durations) for demo purposes. Results will be stored in-memory for simplicity.
- The api should have endpoints to submit test results, get summary metrics, and clear results.

High-Level Steps:
[x] 1. Clarify Azure hosting choice (App Service chosen) for .NET API.
[x] 2. Decide API subject/domain ( testmetrics service) and endpoints.
[x] 3. Outline .NET project setup with Azure AD OIDC authentication.
[ ] 4. Define local development and configuration steps (appsettings, secrets).
[ ] 5. Draft Azure deployment approach (resource creation, configuration).
[ ] 6. Design GitHub Actions workflow for deployment.
[ ] 7. Design GitHub Actions workflow for authenticated API call using Azure AD OIDC token.
[ ] 8. Summarize testing and validation steps.

Task Breakdown:
Step 3 – .NET project setup tasks
- [x] Create solution and Web API project (`dotnet new webapi`).
- [x] Add Azure AD auth packages (`Microsoft.Identity.Web`).
- [x] Configure `Program.cs`:
  - [x] Add `AddMicrosoftIdentityWebApi`.
  - [x] Require authorization globally (e.g., `options.FallbackPolicy = options.DefaultPolicy;` or `[Authorize]` on controllers).
  - [x] Register metrics store singleton service.
  - [x] Ensure Swagger is enabled in development and configured for OAuth2 (security definition + requirement).
- [x] Implement in-memory metrics service (`IMetricsStore` + `MetricsStore` with thread-safe collection and reset method).
- [x] Add controllers:
  - [x] `POST /tests/result` to add entry via metrics service.
  - [x] `GET /tests/summary` returning aggregate counts (passed, failed, total).
  - [x] `POST /tests/clear` to clear current results.
- [x] Create DTOs:
  - [x] `TestResultRequest` (BuildId, TestName, Status, Duration, Timestamp).
  - [x] `TestSummaryResponse` (Total, Passed, Failed, LatestBuildId, etc.).
- [ ] Configure `appsettings.json` with Azure AD placeholders (`Instance`, `TenantId`, `ClientId`, `Audience`). → Handle in Step 4.
- [x] Wire up Swagger OAuth2 configuration to use Azure AD endpoints for local testing.

Step 4 – Local development & configuration
- [x] Capture required Azure AD values (TenantId, ClientId, Audience/AppIdUri).
- [x] Add placeholder entries to `appsettings.json` (AzureAd section + API metadata).
- [x] Document using `dotnet user-secrets` for local override of sensitive values.
- [x] Provide local run instructions (obtain token, call API via Swagger/http file).

Step 5 – Azure deployment approach
- [x] Select Azure resource names, SKU, and region (App Service Plan + Web App).
- [x] Define infrastructure creation steps (Azure CLI/Bicep/Terraform/manual portal).
- [x] Outline API App Service configuration (app settings, connection to Azure AD).
- [x] Plan for appsettings overrides in App Service (AzureAd config, logging).
- [x] Identify deployment artifact (dotnet publish zip vs. container) and paths.

Step 6 – GitHub Actions deployment workflow
- [x] Decide on authentication method (OIDC federated credentials vs. publish profile).
- [x] Grant workflow permissions in Azure (service principal / federated identity).
- [x] Author workflow YAML (build, publish, deploy to App Service).
- [x] Document required GitHub secrets/variables.
- [ ] Note rollback/cleanup considerations (optional).

Step 7 – GitHub Actions API call workflow
- [x] Register a separate Azure AD app (client credentials) or reuse existing as needed.
- [x] Configure workflow to obtain access token (Azure CLI `az account get-access-token` or `azure/login`).

- [x] Add sample payload for `POST /api/tests/result`.
- [x] Implement job that hits protected endpoint (`GET /api/tests/summary`).
- [x] Capture response artifacts or log output for visibility.

Step 8 – Testing and validation
- [ ] Define manual/local test cases (happy path, invalid token, bad outcome value).
- [ ] Outline automated tests (unit/integration) or rationale if omitted.
- [ ] Describe validation steps post-deployment (Smoke test, GitHub workflow run).
- [ ] Plan monitoring/logging checks (App Service logs, Application Insights optional).
- [ ] Document cleanup/reset procedure (`POST /api/tests/clear`).
