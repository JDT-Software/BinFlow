@echo off
echo =================================
echo Production Tracker Deployment
echo =================================
echo.

echo Building and publishing application...
dotnet publish -c Release -o .\publish
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful! Application published to .\publish folder
echo.
echo Deployment options:
echo 1. Run locally: cd publish && ProductionTracker.exe
echo 2. Deploy to IIS: Copy publish folder to IIS directory
echo 3. Deploy to Docker: docker build -t production-tracker .
echo 4. Deploy to Azure: Follow deploy-azure.md guide
echo.
echo For detailed instructions, see the deploy-*.md files.
echo.
pause
