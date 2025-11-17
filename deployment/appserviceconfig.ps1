$tenantId = "1f39fa86-9689-41da-b81e-d60e10615bf0"
$apiClientId = "f3f36cfe-c1c7-4803-b9b8-0d704fc0354d"
$appAudience = "api://$apiClientId"
$swaggerClientId = "1b86dcb0-6c9a-4c7d-9dc1-ea0fe799b443"

$swaggerScopes = @(
    "api://$apiClientId/Metrics.Retrieve",
    "api://$apiClientId/Metrics.Submit",
    "api://$apiClientId/Metrics.Clear"
) -join " "

az webapp config appsettings set `
    --name $webAppName `
    --resource-group $resourceGroup `
    --settings `
        "AzureAd:Instance=https://login.microsoftonline.com/" `
        "AzureAd:TenantId=$tenantId" `
        "AzureAd:ClientId=$apiClientId" `
        "AzureAd:Audience=$appAudience" `
        "Swagger:ClientId=$swaggerClientId" `
        "Swagger:Scope=$swaggerScopes" `
        "Swagger:ScopeDescription=Access Metrics API - from App Service" `
        "WEBSITE_RUN_FROM_PACKAGE=1" `
        "ASPNETCORE_ENVIRONMENT=Development"