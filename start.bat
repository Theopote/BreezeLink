@echo off
REM BreezeLink Startup Script
REM This script starts both the core controller and UI

echo Starting BreezeLink...
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: .NET 8.0 is not installed. Please install .NET 8.0 SDK or Runtime.
    pause
    exit /b 1
)

REM Start core controller
echo Starting core controller...
start "BreezeLink Core Controller" cmd /k "cd core-controller && dotnet run"

REM Wait a moment
timeout /t 3 /nobreak >nul

REM Start UI
echo Starting UI...
start "BreezeLink UI" cmd /k "cd ui && dotnet run"

echo.
echo BreezeLink is starting...
echo - Core controller: http://127.0.0.1:8800
echo - UI should open automatically
echo.
echo Press any key to close this window...
pause >nul
