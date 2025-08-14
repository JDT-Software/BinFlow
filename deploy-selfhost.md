# Production Tracker - Windows Service Setup

## Simple Self-Hosting

### Option A: Run as Console Application
1. Navigate to the published folder
2. Run: `ProductionTracker.exe`
3. Access at: `http://localhost:5000` or `https://localhost:5001`

### Option B: Configure for Production URLs
1. Edit `appsettings.json` and add:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      },
      "Https": {
        "Url": "https://0.0.0.0:443"
      }
    }
  }
}
```

### Option C: Windows Service
1. Install as Windows Service:
```powershell
sc create "Production Tracker" binPath="C:\ProductionTracker\publish\ProductionTracker.exe" start=auto
sc description "Production Tracker" "Production monitoring and shift management system"
sc start "Production Tracker"
```

2. To remove service:
```powershell
sc stop "Production Tracker"
sc delete "Production Tracker"
```

## Important Notes
- Ensure port 80/443 are available
- Configure Windows Firewall if needed
- For HTTPS, you'll need SSL certificates
- Consider using reverse proxy (IIS/nginx) for production
