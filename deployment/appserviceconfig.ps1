$tenantId = "1f39fa86-9689-41da-b81e-d60e10615bf0"
$apiClientId = "6634c5b8-bb6e-4840-a8eb-f21df489d4b1"
$appAudience = "api://$apiClientId/.default"
$swaggerClientId = "1b86dcb0-6c9a-4c7d-9dc1-ea0fe799b443"

az webapp config appsettings set `
    --name $webAppName `
    --resource-group $resourceGroup `
    --settings `
        "AzureAd:Instance=https://login.microsoftonline.com/" `
        "AzureAd:TenantId=$tenantId" `
        "AzureAd:ClientId=$apiClientId" `
        "AzureAd:Audience=$appAudience" `
        "Swagger:ClientId=$swaggerClientId" `
        "Swagger:Scope=$appAudience" `
        "WEBSITE_RUN_FROM_PACKAGE=1" `
        "ASPNETCORE_ENVIRONMENT=Development" 