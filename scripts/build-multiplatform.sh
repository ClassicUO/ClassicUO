#!/bin/bash

# ClassicUO Multiplatform Build Script
# Supports Windows, macOS, and Linux (x64 and ARM64)

set -e

# Configuration
PROJECT_PATH="../src/ClassicUO.Client/ClassicUO.Client.csproj"
OUTPUT_BASE="../bin"
CONFIG="${1:-Release}"

# Validate configuration
if [ "$CONFIG" != "Debug" ] && [ "$CONFIG" != "Release" ]; then
    echo "Usage: $0 [Debug|Release]"
    echo "Default: Release"
    exit 1
fi

if [ "$CONFIG" = "Debug" ]; then
    echo "*** WARNING: USING DEBUG CONFIGURATION. IT WILL AFFECT PERFORMANCE OF THE GAME!! ***"
fi

echo "Building ClassicUO for .NET 8..."
echo "Configuration: $CONFIG"
echo "Project: $PROJECT_PATH"

# Detect platform
OS=$(uname -s)
ARCH=$(uname -m)

case $OS in
    Linux*)
        PLATFORM="linux"
        if [ "$ARCH" = "x86_64" ]; then
            RUNTIME="linux-x64"
            PLATFORM_SUFFIX="x64"
        elif [ "$ARCH" = "aarch64" ]; then
            RUNTIME="linux-arm64"
            PLATFORM_SUFFIX="arm64"
        else
            RUNTIME="linux-$ARCH"
            PLATFORM_SUFFIX="$ARCH"
        fi
        ;;
    Darwin*)
        PLATFORM="macos"
        if [ "$ARCH" = "x86_64" ]; then
            RUNTIME="osx-x64"
            PLATFORM_SUFFIX="x64"
        elif [ "$ARCH" = "arm64" ]; then
            RUNTIME="osx-arm64"
            PLATFORM_SUFFIX="arm64"
        else
            RUNTIME="osx-$ARCH"
            PLATFORM_SUFFIX="$ARCH"
        fi
        ;;
    CYGWIN*|MINGW*|MSYS*)
        PLATFORM="windows"
        RUNTIME="win-x64"
        PLATFORM_SUFFIX="x64"
        ;;
    *)
        echo "Unsupported platform: $OS"
        exit 1
        ;;
esac

echo "Platform: $PLATFORM ($PLATFORM_SUFFIX)"
echo "Runtime: $RUNTIME"

# Build for current platform
OUTPUT_DIR="$OUTPUT_BASE/$CONFIG-$PLATFORM-$PLATFORM_SUFFIX"
echo "Output directory: $OUTPUT_DIR"

echo "Building..."
dotnet build "$PROJECT_PATH" -c "$CONFIG" -p:Platform="$PLATFORM_SUFFIX" -o "$OUTPUT_DIR"

echo "Publishing self-contained..."
PUBLISH_DIR="$OUTPUT_BASE/publish-$PLATFORM-$PLATFORM_SUFFIX"
dotnet publish "$PROJECT_PATH" -c "$CONFIG" -p:Platform="$PLATFORM_SUFFIX" -r "$RUNTIME" --self-contained true -o "$PUBLISH_DIR"

echo "Build completed successfully!"
echo "Executable location: $PUBLISH_DIR/ClassicUO"
