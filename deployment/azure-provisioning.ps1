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

