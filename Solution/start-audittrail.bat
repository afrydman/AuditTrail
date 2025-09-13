@echo off
echo ===================================
echo    AuditTrail System Startup
echo ===================================
echo Starting API and Web projects...
echo.

:: Start API project in background
echo Starting AuditTrail API on https://localhost:7001...
start "AuditTrail API" cmd /k "cd /d %~dp0AuditTrail.API && dotnet run --launch-profile https"

:: Wait a moment for API to start
timeout /t 5 /nobreak > nul

:: Start Web project
echo Starting AuditTrail Web on https://localhost:7002...
start "AuditTrail Web" cmd /k "cd /d %~dp0AuditTrail.Web && dotnet run --launch-profile https"

echo.
echo Both projects are starting...
echo.
echo API:  https://localhost:7001/swagger
echo Web:  https://localhost:7002
echo.
echo Press any key to exit this window (projects will continue running)
pause > nul