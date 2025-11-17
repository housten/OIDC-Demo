<#
.SYNOPSIS
    Interactive script to demonstrate calling the Metrics API with Azure AD authentication.
.DESCRIPTION
    This script authenticates a user interactively, retrieves an access token,
    displays the token, submits a test result, and retrieves the metrics summary.
    Run sections of this script interactively in VS Code.
#>

#region Configuration
# Load configuration from environment variables or set defaults
$tenantId = $env:AZURE_TENANT_ID ?? "your-tenant-id"
$apiClientId = $env:AZURE_APP_ID ?? "f3f36cfe-c1c7-4803-b9b8-0d704fc0354d"
$swaggerClientId = $env:SWAGGER_CLIENT_ID ?? "1b86dcb0-6c9a-4c7d-9dc1-ea0fe799b443"
$apiBaseUrl = $env:API_BASE_URL ?? "https://metricsapi-oidc-demo-app.azurewebsites.net"
$scope = "api://$apiClientId/Metrics.Submit api://$apiClientId/Metrics.Retrieve"

Write-Host "=== Configuration ===" -ForegroundColor Cyan
Write-Host "Tenant ID: $tenantId"
Write-Host "API Client ID: $apiClientId"
Write-Host "Swagger Client ID: $swaggerClientId"
Write-Host "API Base URL: $apiBaseUrl"
Write-Host "Scopes: $scope"
Write-Host ""
#endregion

#region Step 1: Authenticate and get access token
Write-Host "=== Step 1: Authenticate and Get Access Token ===" -ForegroundColor Green

# Use Azure CLI for interactive authentication
Write-Host "Logging in with Azure CLI (interactive)..." -ForegroundColor Yellow
az login --tenant $tenantId --allow-no-subscriptions

Write-Host "`nRequesting access token for API..." -ForegroundColor Yellow
$tokenResponse = az account get-access-token `
    --resource "api://$apiClientId" `
    --query "{accessToken:accessToken, expiresOn:expiresOn}" `
    -o json | ConvertFrom-Json

$accessToken = $tokenResponse.accessToken
$expiresOn = $tokenResponse.expiresOn

Write-Host "Access token acquired successfully!" -ForegroundColor Green
Write-Host "Expires on: $expiresOn" -ForegroundColor Gray
Write-Host ""
Write-Host "=== Raw Access Token ===" -ForegroundColor Cyan
Write-Host $accessToken -ForegroundColor White
Write-Host ""
Write-Host "Copy the token above and paste it into https://jwt.ms to inspect claims." -ForegroundColor Yellow
Write-Host ""
#endregion

#region Step 2: Submit a test result
Write-Host "=== Step 2: Submit Test Result ===" -ForegroundColor Green

$testResult = @{
    buildId = "ps-demo-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    testName = "PowerShell Demo Test"
    outcome = "Passed"
    durationSeconds = 42
    completedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

Write-Host "Test result payload:" -ForegroundColor Yellow
Write-Host $testResult -ForegroundColor White
Write-Host ""

Write-Host "Submitting test result to API..." -ForegroundColor Yellow
$submitResponse = Invoke-RestMethod `
    -Uri "$apiBaseUrl/api/tests/result" `
    -Method Post `
    -Headers @{
        "Authorization" = "Bearer $accessToken"
        "Content-Type" = "application/json"
    } `
    -Body $testResult `
    -ResponseHeadersVariable responseHeaders `
    -StatusCodeVariable statusCode

Write-Host "HTTP Status: $statusCode" -ForegroundColor Green
Write-Host "Response:" -ForegroundColor Yellow
$submitResponse | ConvertTo-Json -Depth 10
Write-Host ""
#endregion

#region Step 3: Retrieve metrics summary
Write-Host "=== Step 3: Retrieve Metrics Summary ===" -ForegroundColor Green

Write-Host "Fetching metrics summary from API..." -ForegroundColor Yellow
$summaryResponse = Invoke-RestMethod `
    -Uri "$apiBaseUrl/api/tests/summary" `
    -Method Get `
    -Headers @{
        "Authorization" = "Bearer $accessToken"
    } `
    -StatusCodeVariable statusCode

Write-Host "HTTP Status: $statusCode" -ForegroundColor Green
Write-Host "Summary Response:" -ForegroundColor Yellow
$summaryResponse | ConvertTo-Json -Depth 10
Write-Host ""
#endregion

#region Summary
Write-Host "=== Script Complete ===" -ForegroundColor Cyan
Write-Host "You successfully:" -ForegroundColor Green
Write-Host "  1. Authenticated with Azure AD" -ForegroundColor White
Write-Host "  2. Obtained an access token with the required scopes" -ForegroundColor White
Write-Host "  3. Submitted a test result to the API" -ForegroundColor White
Write-Host "  4. Retrieved the metrics summary" -ForegroundColor White
Write-Host ""
#endregion