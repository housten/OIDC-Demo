<#
.SYNOPSIS
    OIDC demonstration script using MSAL for interactive authentication.
.DESCRIPTION
    This script demonstrates OAuth 2.0/OIDC best practices:
    - Interactive authentication with device code flow
    - Token acquisition with explicit client app and scopes
    - Token claims inspection
    - Calling protected API endpoints
#>

#region Prerequisites
# Install MSAL.PS if not already installed
if (-not (Get-Module -ListAvailable -Name MSAL.PS)) {
    Write-Host "Installing MSAL.PS module..." -ForegroundColor Yellow
    Install-Module -Name MSAL.PS -Scope CurrentUser -Force
}
Import-Module MSAL.PS
#endregion

#region Configuration
$tenantId = $env:AZURE_TENANT_ID ?? "1f39fa86-9689-41da-b81e-d60e10615bf0"
$apiClientId = $env:AZURE_APP_ID ?? "f3f36cfe-c1c7-4803-b9b8-0d704fc0354d"
$swaggerClientId = $env:SWAGGER_CLIENT_ID ?? "1b86dcb0-6c9a-4c7d-9dc1-ea0fe799b443"
$apiBaseUrl = $env:API_BASE_URL ?? "https://metricsapi-oidc-demo-app.azurewebsites.net"

# OIDC-specific: Define scopes explicitly
$scopes = @(
    "api://$apiClientId/Metrics.Submit",
    "api://$apiClientId/Metrics.Retrieve"
)

Write-Host "=== OIDC Configuration ===" -ForegroundColor Cyan
Write-Host "Tenant ID: $tenantId"
Write-Host "API (Resource) Client ID: $apiClientId"
Write-Host "Swagger (Client) App ID: $swaggerClientId"
Write-Host "API Base URL: $apiBaseUrl"
Write-Host "Scopes: $($scopes -join ', ')"
Write-Host ""
#endregion

#region Step 1: OIDC Authentication with Device Code Flow
Write-Host "=== Step 1: OIDC Authentication (Device Code Flow) ===" -ForegroundColor Green
Write-Host "This demonstrates OAuth 2.0 Device Authorization Grant (RFC 8628)" -ForegroundColor Yellow
Write-Host ""

# BEST PRACTICE: Use device code flow for devices/scripts without a browser
$authResult = Get-MsalToken `
    -ClientId $swaggerClientId `
    -TenantId $tenantId `
    -Scopes $scopes `
    -DeviceCode `
    -Verbose

#region Step 1: OIDC Authentication with Interactive Browser Flow
Write-Host "=== Step 1: OIDC Authentication (Interactive Browser Flow) ===" -ForegroundColor Green
Write-Host "This demonstrates OAuth 2.0 Authorization Code Flow with PKCE" -ForegroundColor Yellow
Write-Host ""

# BEST PRACTICE: Use interactive browser flow for user authentication with PKCE
$authResult = Get-MsalToken `
    -ClientId $swaggerClientId `
    -TenantId $tenantId `
    -Scopes $scopes `
    -Interactive `
    -Verbose


$authResult = Invoke-RestMethod `
    -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" `
    -Method Post `
    -ContentType "application/x-www-form-urlencoded" `
    -Body $body


$accessToken = $authResult.AccessToken
$idToken = $authResult.IdToken
$expiresOn = $authResult.ExpiresOn

Write-Host "Authentication successful!" -ForegroundColor Green
Write-Host "Token expires on: $expiresOn" -ForegroundColor Gray
Write-Host ""
#endregion

#region Step 2: Inspect Access Token (OIDC Claims)
Write-Host "=== Step 2: Inspect Access Token Claims ===" -ForegroundColor Green
Write-Host "OIDC tokens are JWTs with claims about the user and authorization" -ForegroundColor Yellow
Write-Host ""

Write-Host "=== Raw Access Token ===" -ForegroundColor Cyan
Write-Host $accessToken -ForegroundColor White
Write-Host ""

# BEST PRACTICE: Parse and display key claims
$tokenParts = $accessToken.Split('.')
$payloadJson = [System.Text.Encoding]::UTF8.GetString(
    [Convert]::FromBase64String(
        $tokenParts[1].Replace('-', '+').Replace('_', '/').PadRight(
            $tokenParts[1].Length + (4 - $tokenParts[1].Length % 4) % 4, '='
        )
    )
)
$claims = $payloadJson | ConvertFrom-Json

Write-Host "=== Key OIDC Claims ===" -ForegroundColor Cyan
Write-Host "Audience (aud): $($claims.aud)" -ForegroundColor White
Write-Host "Issuer (iss): $($claims.iss)" -ForegroundColor White
Write-Host "Subject (sub): $($claims.sub)" -ForegroundColor White
Write-Host "Scopes (scp): $($claims.scp)" -ForegroundColor White
Write-Host "App ID (azp/appid): $($claims.azp ?? $claims.appid)" -ForegroundColor White
Write-Host "Issued At (iat): $(([DateTimeOffset]::FromUnixTimeSeconds($claims.iat)).ToString())" -ForegroundColor White
Write-Host "Expires At (exp): $(([DateTimeOffset]::FromUnixTimeSeconds($claims.exp)).ToString())" -ForegroundColor White
Write-Host ""
Write-Host "Copy the token above and paste it into https://jwt.ms for full inspection." -ForegroundColor Yellow
Write-Host ""
#endregion

#region Step 3: Inspect ID Token (OIDC User Identity)
Write-Host "=== Step 3: Inspect ID Token (User Identity) ===" -ForegroundColor Green
Write-Host "ID tokens contain claims about the authenticated user (OIDC spec)" -ForegroundColor Yellow
Write-Host ""

if ($idToken) {
    $idTokenParts = $idToken.Split('.')
    $idPayloadJson = [System.Text.Encoding]::UTF8.GetString(
        [Convert]::FromBase64String(
            $idTokenParts[1].Replace('-', '+').Replace('_', '/').PadRight(
                $idTokenParts[1].Length + (4 - $idTokenParts[1].Length % 4) % 4, '='
            )
        )
    )
    $idClaims = $idPayloadJson | ConvertFrom-Json

    Write-Host "=== ID Token Claims ===" -ForegroundColor Cyan
    Write-Host "Name: $($idClaims.name)" -ForegroundColor White
    Write-Host "Email: $($idClaims.email ?? $idClaims.upn ?? $idClaims.preferred_username)" -ForegroundColor White
    Write-Host "Object ID (oid): $($idClaims.oid)" -ForegroundColor White
    Write-Host "Tenant ID (tid): $($idClaims.tid)" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "No ID token returned (access token only flow)" -ForegroundColor Yellow
    Write-Host ""
}
#endregion

#region Step 4: Call Protected API - Submit Test Result
Write-Host "=== Step 4: Call Protected API - Submit Test Result ===" -ForegroundColor Green
Write-Host "BEST PRACTICE: Include Bearer token in Authorization header" -ForegroundColor Yellow
Write-Host ""

$testResult = @{
    buildId = "msal-demo-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    testName = "MSAL PowerShell Demo Test"
    outcome = "Passed"
    durationSeconds = 42
    completedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

Write-Host "Test result payload:" -ForegroundColor Yellow
Write-Host $testResult -ForegroundColor White
Write-Host ""

try {
    $submitResponse = Invoke-RestMethod `
        -Uri "$apiBaseUrl/api/tests/result" `
        -Method Post `
        -Headers @{
            "Authorization" = "Bearer $accessToken"
            "Content-Type" = "application/json"
        } `
        -Body $testResult `
        -StatusCodeVariable statusCode

    Write-Host "HTTP Status: $statusCode" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Yellow
    $submitResponse | ConvertTo-Json -Depth 10
    Write-Host ""
} catch {
    Write-Host "API call failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host ""
}
#endregion

#region Step 5: Call Protected API - Retrieve Summary
Write-Host "=== Step 5: Call Protected API - Retrieve Summary ===" -ForegroundColor Green

try {
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
} catch {
    Write-Host "API call failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host ""
}
#endregion

#region Step 6: Token Refresh (Optional)
Write-Host "=== Step 6: Token Refresh (Optional) ===" -ForegroundColor Green
Write-Host "BEST PRACTICE: Use refresh tokens to get new access tokens without re-authenticating" -ForegroundColor Yellow
Write-Host ""

if ($authResult.RefreshToken) {
    Write-Host "Refresh token available. To refresh, use:" -ForegroundColor White
    Write-Host '  $newToken = Get-MsalToken -ClientId $swaggerClientId -TenantId $tenantId -Scopes $scopes -ForceRefresh' -ForegroundColor Gray
} else {
    Write-Host "No refresh token returned (may require offline_access scope or different client config)" -ForegroundColor Yellow
}
Write-Host ""
#endregion

#region Summary
Write-Host "=== OIDC Demo Complete ===" -ForegroundColor Cyan
Write-Host "You demonstrated:" -ForegroundColor Green
Write-Host "  ✓ OAuth 2.0 Device Code Flow (RFC 8628)" -ForegroundColor White
Write-Host "  ✓ Explicit scope-based authorization" -ForegroundColor White
Write-Host "  ✓ Access token and ID token inspection" -ForegroundColor White
Write-Host "  ✓ Bearer token authentication for API calls" -ForegroundColor White
Write-Host "  ✓ OIDC claims (aud, iss, sub, scp, etc.)" -ForegroundColor White
Write-Host ""
Write-Host "OIDC Best Practices Shown:" -ForegroundColor Cyan
Write-Host "  • Use specific client app (not Azure CLI)" -ForegroundColor White
Write-Host "  • Request minimal scopes (principle of least privilege)" -ForegroundColor White
Write-Host "  • Inspect token claims for debugging" -ForegroundColor White
Write-Host "  • Handle errors gracefully" -ForegroundColor White
Write-Host "  • Consider token refresh for long-running scenarios" -ForegroundColor White
Write-Host ""
#endregion