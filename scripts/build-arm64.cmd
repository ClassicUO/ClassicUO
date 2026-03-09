@echo off
setlocal enabledelayedexpansion

REM ClassicUO ARM64 Build Script for Windows
REM Builds ClassicUO for ARM64 architecture

REM Configuration
set PROJECT_PATH=..\src\ClassicUO.Client\ClassicUO.Client.csproj
set OUTPUT_BASE=..\bin
set CONFIG=%1
set PLATFORM=ARM64

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

echo Building ClassicUO for ARM64...
echo Configuration: %CONFIG%
echo Platform: %PLATFORM%
echo Project: %PROJECT_PATH%

REM Set runtime identifier for ARM64
set RUNTIME=win-arm64
echo Runtime: %RUNTIME%

REM Build for ARM64
set OUTPUT_DIR=%OUTPUT_BASE%\%CONFIG%-arm64
echo Output directory: %OUTPUT_DIR%

echo Building for ARM64...
dotnet build "%PROJECT_PATH%" -c "%CONFIG%" -p:Platform=%PLATFORM% -o "%OUTPUT_DIR%"

if %ERRORLEVEL% neq 0 (
    echo Build failed!
    exit /b 1
)

echo Publishing self-contained for ARM64...
set PUBLISH_DIR=%OUTPUT_BASE%\publish-arm64
dotnet publish "%PROJECT_PATH%" -c "%CONFIG%" -p:Platform=%PLATFORM% -r "%RUNTIME%" --self-contained true -o "%PUBLISH_DIR%"

if %ERRORLEVEL% neq 0 (
    echo Publish failed!
    exit /b 1
)

echo.
echo ========================================
echo ARM64 Build completed successfully!
echo ========================================
echo Executable location: %PUBLISH_DIR%\ClassicUO.exe
echo.
echo Note: Make sure you have ARM64 native libraries in:
echo   - external\arm64\win-arm64\
echo   - external\arm64\osx-arm64\ (for macOS)
echo   - external\arm64\linux-arm64\ (for Linux)
echo.
echo For more information, see: external\arm64\README.md
echo.

pause
