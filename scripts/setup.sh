#!/bin/bash

set -e

# Check if zig is already installed
if ! command -v zig &> /dev/null; then
    echo "Zig not found, installing..."
    snap install zig --classic --beta
else
    echo "Zig is already installed"
fi

pushd ..
dotnet tool install --global ClangSharpPInvokeGenerator
popd

pushd ../external/Clay-cs/Clay-builder
zig build
popd

pushd ../external/TinyEcs
dotnet build
popd

pushd ../src/ClassicUO.Client
dotnet build
popd

