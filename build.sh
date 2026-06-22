#!/bin/bash

# BreezeLink Build Script
# This script builds both the core controller and UI components

echo "🚀 Building BreezeLink..."

# Build core controller
echo "📦 Building core controller..."
cd core-controller
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo "❌ Failed to build core controller"
    exit 1
fi
cd ..

# Build UI
echo "🎨 Building UI..."
cd ui
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo "❌ Failed to build UI"
    exit 1
fi
cd ..

echo "✅ Build completed successfully!"
echo ""
echo "To run the application:"
echo "1. Start the core controller: cd core-controller && dotnet run"
echo "2. Start the UI: cd ui && dotnet run"
echo ""
echo "For production builds, use:"
echo "dotnet publish --configuration Release --runtime win-x64"
