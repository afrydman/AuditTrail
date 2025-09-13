# AuditTrail System Startup Script
Write-Host "===================================" -ForegroundColor Cyan
Write-Host "    AuditTrail System Startup" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host "Starting API and Web projects..." -ForegroundColor Green
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

try {
    # Start API project in background
    Write-Host "Starting AuditTrail API on https://localhost:7001..." -ForegroundColor Yellow
    $apiPath = Join-Path $scriptPath "AuditTrail.API"
    Start-Process -FilePath "dotnet" -ArgumentList "run", "--launch-profile", "https" -WorkingDirectory $apiPath -WindowStyle Normal

    # Wait for API to initialize
    Write-Host "Waiting for API to initialize..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5

    # Start Web project
    Write-Host "Starting AuditTrail Web on https://localhost:7002..." -ForegroundColor Yellow
    $webPath = Join-Path $scriptPath "AuditTrail.Web"
    Start-Process -FilePath "dotnet" -ArgumentList "run", "--launch-profile", "https" -WorkingDirectory $webPath -WindowStyle Normal

    Write-Host ""
    Write-Host "Both projects are starting..." -ForegroundColor Green
    Write-Host ""
    Write-Host "Available URLs:" -ForegroundColor Cyan
    Write-Host "  API:  https://localhost:7001/swagger" -ForegroundColor White
    Write-Host "  Web:  https://localhost:7002" -ForegroundColor White
    Write-Host ""
    
    # Wait a bit more and try to open browser
    Write-Host "Opening web browser..." -ForegroundColor Yellow
    Start-Sleep -Seconds 3
    Start-Process "https://localhost:7002"
    
    Write-Host "Projects started successfully!" -ForegroundColor Green
    Write-Host "To stop the projects, close their respective console windows." -ForegroundColor Yellow
    
} catch {
    Write-Host "Error starting projects: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure you have .NET 8 SDK installed and projects are built." -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit this script..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")