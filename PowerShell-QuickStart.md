# PowerShell Quick Start Guide

## 🚀 Running Production Tracker Locally

### Step 1: Navigate to the published folder
```powershell
cd c:\ProductionTracker\publish
```

### Step 2: Run the application (PowerShell syntax)
```powershell
.\ProductionTracker.exe
```

### Step 3: Access the application
Open your browser and go to: **http://localhost:5000**

## ⚠️ Important PowerShell Notes:
- Always use `.\` before executable names in PowerShell
- The application will start and show hosting information
- Press `Ctrl+C` to stop the application

## 🌐 Testing Features:
- ✅ Desktop navigation (sidebar)
- ✅ Mobile navigation (hamburger menu)
- ✅ Day/Night shift dashboard charts
- ✅ Responsive design
- ✅ All pages accessible

## 🔧 Troubleshooting:
- If port 5000 is in use, the app will automatically use the next available port
- Check the console output for the actual URL if different from localhost:5000
- Ensure Windows Firewall allows the application if testing from other devices

## 📦 Production Deployment:
Once local testing is successful, copy the entire `publish` folder to your production server and follow the deployment guides for your chosen platform.
