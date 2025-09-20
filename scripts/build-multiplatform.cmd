@echo off
setlocal enabledelayedexpansion

REM ClassicUO Multiplatform Build Script for Windows
REM Supports Windows x64, ARM64, and AnyCPU

REM Configuration
set PROJECT_PATH=..\src\ClassicUO.Client\ClassicUO.Client.csproj
set OUTPUT_BASE=..\bin
set CONFIG=%1

REM Validate configuration
if "%CONFIG%"=="" set CONFIG=Release
if not "%CONFIG%"=="Debug" if not "%CONFIG%"=="Release" (
    echo Usage: %0 [Debug^|Release]
    echo Default: Release
    exit /b 1
)

if "%CONFIG%"=="Debug" (
    echo *** WARNING: USING DEBUG CONFIGURATION. IT WILL AFFECT PERFORMANCE OF THE GAME!! ***
)

echo Building ClassicUO for .NET 8...
echo Configuration: %CONFIG%
echo Project: %PROJECT_PATH%

REM Detect architecture
if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    set RUNTIME=win-x64
    set PLATFORM=x64
) else if "%PROCESSOR_ARCHITECTURE%"=="ARM64" (
    set RUNTIME=win-arm64
    set PLATFORM=ARM64
) else (
    set RUNTIME=win-x64
    set PLATFORM=AnyCPU
)

echo Platform: Windows %PLATFORM%
echo Runtime: %RUNTIME%

REM Build for current platform
set OUTPUT_DIR=%OUTPUT_BASE%\%CONFIG%-windows-%PLATFORM%
echo Output directory: %OUTPUT_DIR%

echo Building...
dotnet build "%PROJECT_PATH%" -c "%CONFIG%" -p:Platform=%PLATFORM% -o "%OUTPUT_DIR%"

echo Publishing self-contained...
set PUBLISH_DIR=%OUTPUT_BASE%\publish-windows-%PLATFORM%
dotnet publish "%PROJECT_PATH%" -c "%CONFIG%" -p:Platform=%PLATFORM% -r "%RUNTIME%" --self-contained true -o "%PUBLISH_DIR%"

echo Build completed successfully!
echo Executable location: %PUBLISH_DIR%\ClassicUO.exe

pause
