@echo off
echo ========================================
echo   Production Tracker - Final Deployment
echo ========================================
echo.

echo Checking build status...
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed! Please fix errors before deploying.
    pause
    exit /b 1
)

echo.
echo ✅ Build successful!
echo.
echo Creating production package...
dotnet publish -c Release -o .\publish --self-contained false
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Publish failed!
    pause
    exit /b 1
)

echo.
echo ✅ Production package created in .\publish\
echo.
echo 🎉 Production Tracker is ready for deployment!
echo.
echo Available deployment options:
echo   1. Local Test: cd publish ^&^& .\ProductionTracker.exe
echo   2. IIS: Follow deploy-iis.md guide
echo   3. Docker: docker build -t production-tracker .
echo   4. Azure: Follow deploy-azure.md guide
echo.
echo 📱 Features Ready:
echo   ✅ Mobile hamburger navigation
echo   ✅ Desktop sidebar navigation
echo   ✅ Day/Night shift charts
echo   ✅ Responsive design
echo   ✅ Bootstrap JavaScript loaded
echo   ✅ MongoDB cloud connection
echo.
echo For detailed instructions, see DEPLOYMENT_READY.md
echo.
pause
