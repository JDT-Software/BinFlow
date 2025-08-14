# Bin Flow (ProductionTracker)

Blazor Server app for shift production tracking.

## Local Development
```
dotnet run
# or specific profile
dotnet run --launch-profile https
```
Browse: http://localhost:5103 or https://localhost:7285

## Configuration
Provide Mongo settings via environment variables (preferred for deployment):
- MongoDbSettings__ConnectionString
- MongoDbSettings__DatabaseName (defaults to production_tracker if omitted)

## Publish
```
dotnet publish -c Release -o publish
```

## Deploy to Render (Option A - Native Build)
1. Push this repo to GitHub.
2. Create a Render Web Service:
   - Build Command: `dotnet publish -c Release -o out`
   - Start Command: `dotnet out/ProductionTracker.dll --urls=http://0.0.0.0:$PORT`
   - Enable WebSockets.
3. Add environment variables:
```
ASPNETCORE_ENVIRONMENT=Production
MongoDbSettings__ConnectionString=YOUR_MONGO_CONNECTION
MongoDbSettings__DatabaseName=production_tracker
```
4. Health check: GET /health returns `OK`.

## Deploy to Azure App Service (Quick)
```
dotnet publish -c Release -o publish
Compress-Archive -Path publish/* -DestinationPath binflow.zip -Force
```
Upload via Kudu or use `az webapp deploy`.

## Security Notes
- Connection string removed from `appsettings.json`. Supply via env vars.
- Do not commit secrets.

## Next Enhancements
- Add Serilog structured logging.
- Add authentication/authorization.
- Add integration tests.
