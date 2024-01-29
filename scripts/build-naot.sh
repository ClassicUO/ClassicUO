#!/bin/bash

set -e

# Define paths and project details
bootstrap_project="../src/ClassicUO.Bootstrap/src/ClassicUO.Bootstrap.csproj"
client_project="../src/ClassicUO.Client"
output_directory="../bin/dist"

# Determine the platform
platform=$(uname -s)

# Build for the appropriate platform
case $platform in
  Linux)
    # Add Linux-specific build commands here
    dotnet publish "$bootstrap_project" -c Release -o "$output_directory"
    dotnet publish "$client_project" -c Release -f net8.0 -p:PublishAot=true -p:TargetFrameworks=net8.0 -p:NativeLib=Shared -p:OutputType=Library -r linux-x64 -o "$output_directory" 
    ;;
  Darwin)
    # Add macOS-specific build commands here
    dotnet publish "$bootstrap_project" -c Release -o "$output_directory"
    dotnet publish "$client_project" -c Release -f net8.0 -p:PublishAot=true -p:TargetFrameworks=net8.0 -p:NativeLib=Shared -p:OutputType=Library -r osx-x64 -o "$output_directory" 
    ;;
  MINGW* | CYGWIN*)
    # Add Windows-specific build commands here
    dotnet publish "$bootstrap_project" -c Release -o "$output_directory"
    dotnet publish "$client_project" -c Release -f net8.0 -p:PublishAot=true -p:TargetFrameworks=net8.0 -p:NativeLib=Shared -p:OutputType=Library -r win-x64 -o "$output_directory" 
    ;;
  *)
    echo "Unsupported platform: $platform"
    exit 1
    ;;
esac