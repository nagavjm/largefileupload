@echo off
title Launch Full Stack: Backend + Frontend
echo ==========================================================
echo   Starting Enterprise Large File Upload Platform...
echo ==========================================================

echo [1/2] Starting ASP.NET Core Web API...
start "Backend Web API (.NET 8)" cmd /k "cd /d "%~dp0src\LargeFileUpload.API" && dotnet run --launch-profile http"

echo [2/2] Starting Angular 19 Client...
start "Frontend UI (Angular 19)" cmd /k "cd /d "%~dp0client\large-file-upload-client" && npm start"

echo.
echo Both services launched in separate windows!
echo   - Backend Swagger: http://localhost:5179/swagger
echo   - Angular App:     http://localhost:4200
