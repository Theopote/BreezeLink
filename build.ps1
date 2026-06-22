# BreezeLink Build Script
# This script builds both the core controller and UI components

Write-Host "🚀 Building BreezeLink..." -ForegroundColor Green

# Build core controller
Write-Host "📦 Building core controller..." -ForegroundColor Cyan
Set-Location "core-controller"
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to build core controller" -ForegroundColor Red
    exit 1
}
Set-Location ".."

# Build UI
Write-Host "🎨 Building UI..." -ForegroundColor Cyan
Set-Location "ui"
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to build UI" -ForegroundColor Red
    exit 1
}
Set-Location ".."

Write-Host "✅ Build completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To run the application:" -ForegroundColor Yellow
Write-Host "1. Start the core controller: cd core-controller && dotnet run"
Write-Host "2. Start the UI: cd ui && dotnet run"
Write-Host ""
Write-Host "For production builds, use:" -ForegroundColor Yellow
Write-Host "dotnet publish --configuration Release --runtime win-x64"
