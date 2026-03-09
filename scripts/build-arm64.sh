#!/bin/bash

# ClassicUO ARM64 Build Script
# Builds ClassicUO for ARM64 architecture on Linux/macOS

set -e

# Configuration
PROJECT_PATH="../src/ClassicUO.Client/ClassicUO.Client.csproj"
OUTPUT_BASE="../bin"
CONFIG="${1:-Release}"
PLATFORM="ARM64"

# Validate configuration
if [ "$CONFIG" != "Debug" ] && [ "$CONFIG" != "Release" ]; then
    echo "Usage: $0 [Debug|Release]"
    echo "Default: Release"
    exit 1
fi

if [ "$CONFIG" = "Debug" ]; then
    echo "*** WARNING: USING DEBUG CONFIGURATION. IT WILL AFFECT PERFORMANCE OF THE GAME!! ***"
fi

echo "Building ClassicUO for ARM64..."
echo "Configuration: $CONFIG"
echo "Platform: $PLATFORM"
echo "Project: $PROJECT_PATH"

# Detect platform and set runtime identifier
OS=$(uname -s)
ARCH=$(uname -m)

case $OS in
    Linux*)
        PLATFORM_NAME="linux"
        if [ "$ARCH" = "aarch64" ]; then
            RUNTIME="linux-arm64"
        else
            echo "Warning: Not running on ARM64 Linux. Building for linux-arm64 anyway."
            RUNTIME="linux-arm64"
        fi
        ;;
    Darwin*)
        PLATFORM_NAME="macos"
        if [ "$ARCH" = "arm64" ]; then
            RUNTIME="osx-arm64"
        else
            echo "Warning: Not running on Apple Silicon. Building for osx-arm64 anyway."
            RUNTIME="osx-arm64"
        fi
        ;;
    *)
        echo "Unsupported platform: $OS"
        echo "This script is designed for Linux and macOS ARM64 builds"
        exit 1
        ;;
esac

echo "Platform: $PLATFORM_NAME"
echo "Runtime: $RUNTIME"

# Build for ARM64
OUTPUT_DIR="$OUTPUT_BASE/$CONFIG-arm64"
echo "Output directory: $OUTPUT_DIR"

echo "Building for ARM64..."
dotnet build "$PROJECT_PATH" -c "$CONFIG" -p:Platform="$PLATFORM" -o "$OUTPUT_DIR"

echo "Publishing self-contained for ARM64..."
PUBLISH_DIR="$OUTPUT_BASE/publish-arm64"
dotnet publish "$PROJECT_PATH" -c "$CONFIG" -p:Platform="$PLATFORM" -r "$RUNTIME" --self-contained true -o "$PUBLISH_DIR"

echo ""
echo "========================================"
echo "ARM64 Build completed successfully!"
echo "========================================"
echo "Executable location: $PUBLISH_DIR/ClassicUO"
echo ""
echo "Note: Make sure you have ARM64 native libraries in:"
echo "  - external/arm64/win-arm64/ (for Windows)"
echo "  - external/arm64/osx-arm64/ (for macOS)"
echo "  - external/arm64/linux-arm64/ (for Linux)"
echo ""
echo "For more information, see: external/arm64/README.md"
echo ""
