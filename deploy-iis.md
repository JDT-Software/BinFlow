# IIS Deployment Guide for Production Tracker

## Prerequisites
1. Windows Server with IIS installed
2. ASP.NET Core Hosting Bundle installed
3. .NET 8.0 Runtime installed

## Steps
1. **Publish the application:**
   ```powershell
   dotnet publish -c Release -o C:\inetpub\wwwroot\ProductionTracker
   ```

2. **Create IIS Application:**
   - Open IIS Manager
   - Right-click "Default Web Site" → "Add Application"
   - Alias: `ProductionTracker`
   - Physical Path: `C:\inetpub\wwwroot\ProductionTracker`

3. **Configure Application Pool:**
   - Create new Application Pool: `ProductionTrackerPool`
   - Set .NET CLR Version to "No Managed Code"
   - Set Process Model Identity to appropriate account

4. **Set Environment Variables:**
   - In IIS Manager, go to Application → Configuration Editor
   - Set `ASPNETCORE_ENVIRONMENT` to `Production`

## Access
Your app will be available at: `http://yourserver/ProductionTracker`
