#!/bin/bash

set -e

# Check if zig is already installed
if ! command -v zig &> /dev/null; then
    echo "Zig not found, installing..."
    snap install zig --classic --beta
else
    echo "Zig is already installed"
fi

dotnet tool install --global ClangSharpPInvokeGenerator

cd ../external/Clay-cs/Clay-builder
zig build

cd ../../TinyEcs
dotnet build

cd ../../src/ClassicUO.Client
dotnet build

