# Azure Deployment Guide for Production Tracker

## Azure App Service Deployment

### Method 1: Visual Studio Code
1. Install Azure App Service extension
2. Right-click on project folder → "Deploy to Web App"
3. Follow the prompts to create/select App Service

### Method 2: Azure CLI
```bash
# Login to Azure
az login

# Create resource group
az group create --name ProductionTrackerRG --location "East US"

# Create App Service plan
az appservice plan create --name ProductionTrackerPlan --resource-group ProductionTrackerRG --sku B1

# Create web app
az webapp create --name production-tracker-app --resource-group ProductionTrackerRG --plan ProductionTrackerPlan --runtime "DOTNETCORE|8.0"

# Deploy from local folder
az webapp deployment source config-zip --name production-tracker-app --resource-group ProductionTrackerRG --src ./publish.zip
```

### Method 3: GitHub Actions (CI/CD)
Add this to `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 8.0.x
    - name: Publish
      run: dotnet publish -c Release -o ./publish
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'production-tracker-app'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

## Access
Your app will be available at: `https://production-tracker-app.azurewebsites.net`
