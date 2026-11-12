Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Starting Enterprise Large File Upload Platform...        " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Start Backend API in a separate terminal window
Write-Host "[1/2] Starting ASP.NET Core Web API on http://localhost:5179..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\src\LargeFileUpload.API'; Write-Host 'Starting .NET 8 Web API...' -ForegroundColor Green; dotnet run --launch-profile http"

# 2. Start Angular Frontend in a separate terminal window
Write-Host "[2/2] Starting Angular 19 Client on http://localhost:4200..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PSScriptRoot\client\large-file-upload-client'; Write-Host 'Starting Angular Client...' -ForegroundColor Green; npm start"

Write-Host "`nBoth services are launching in separate console windows." -ForegroundColor Yellow
Write-Host "  -> Backend Swagger: http://localhost:5179/swagger" -ForegroundColor White
Write-Host "  -> Angular App:     http://localhost:4200" -ForegroundColor White