#!/bin/bash

# ClassicUO Cross-Platform Build Script
# This script builds ClassicUO for multiple platforms

set -e

echo "Building ClassicUO for cross-platform deployment..."

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build for different platforms
echo "Building for Windows x64..."
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r win-x64 --self-contained true -o bin/Release/win-x64

echo "Building for Linux x64..."
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r linux-x64 --self-contained true -o bin/Release/linux-x64

echo "Building for macOS x64..."
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r osx-x64 --self-contained true -o bin/Release/osx-x64

echo "Building for macOS ARM64..."
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -r osx-arm64 --self-contained true -o bin/Release/osx-arm64

echo "Build completed successfully!"
echo "Executables are available in:"
echo "  - Windows: bin/Release/win-x64/ClassicUO.exe"
echo "  - Linux: bin/Release/linux-x64/ClassicUO"
echo "  - macOS x64: bin/Release/osx-x64/ClassicUO"
echo "  - macOS ARM64: bin/Release/osx-arm64/ClassicUO"
