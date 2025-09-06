# ClassicUO Cross-Platform Migration

This document describes the changes made to migrate ClassicUO to .NET 8.0 in a cross-platform manner.

## Changes Made

### 1. .NET 8.0 Update
- Removed `net8.0-windows` and kept only `net8.0`
- Removed Windows Forms dependencies
- Updated `Directory.Build.props` for pure .NET 8.0

### 2. Windows Forms Replacement
- Created `CrossPlatformFileDialog` to replace `SaveFileDialog` and `OpenFileDialog`
- Implemented support for:
  - Windows: PowerShell with Windows Forms
  - Linux: zenity or kdialog
  - macOS: osascript
  - Fallback: console input

### 3. Conditional P/Invoke
- All Windows-specific P/Invoke calls are now conditional
- Added stub implementations for non-Windows platforms
- Affected files:
  - `Main.cs`
  - `UoAssist.cs`
  - `Plugin.cs`
  - `DllMap.cs`
  - `Native.cs`

### 4. Resources (.resx)
- Updated .resx files to use `System.Resources.Reader` instead of `System.Windows.Forms`
- Fixed special characters that caused encoding errors

### 5. Serialization
- Replaced obsolete `BinaryFormatter` with `JsonSerializer`
- Updated `TileMarkerManager` to use JSON

### 6. Dependencies
- Added `System.Drawing.Common` for cross-platform compatibility
- Kept `System.CodeDom` for script compilation

## How to Build

### Windows
```cmd
dotnet build src/ClassicUO.Client/ClassicUO.Client.csproj
```

### Linux/macOS
```bash
dotnet build src/ClassicUO.Client/ClassicUO.Client.csproj
```

### Cross-Platform Build
```bash
# Linux/macOS
./scripts/build-crossplatform.sh

# Windows
scripts\build-crossplatform.cmd
```

## How to Run

### Windows
```cmd
dotnet run --project src/ClassicUO.Client/ClassicUO.Client.csproj
```

### Linux
```bash
dotnet run --project src/ClassicUO.Client/ClassicUO.Client.csproj
```

### macOS
```bash
dotnet run --project src/ClassicUO.Client/ClassicUO.Client.csproj
```

## Requirements

- .NET 8.0 SDK
- FNA (already included in project)
- SDL2 (already included in project)

## Cross-Platform Features

### File Dialogs
- **Windows**: Uses PowerShell with Windows Forms
- **Linux**: Uses zenity or kdialog (if available)
- **macOS**: Uses osascript
- **Fallback**: Console input

### Graphics
- Uses FNA (cross-platform)
- Support for OpenGL, Vulkan and DirectX (Windows)

### Input
- Uses SDL2 (cross-platform)
- Support for mouse, keyboard and gamepad

## Known Limitations

1. **UoAssist**: Windows-specific features may not work on other platforms
2. **Memory Mapped Files**: Used only on Windows for UltimaLive
3. **Plugins**: May have Windows-specific dependencies

## Testing

To test compilation on different platforms:

```bash
# Test compilation
dotnet build

# Test execution
dotnet run --project src/ClassicUO.Client/ClassicUO.Client.csproj
```

## Troubleshooting

### Build Error
- Check if .NET 8.0 SDK is installed
- Run `dotnet restore` before building

### Runtime Error
- Check if native libraries (SDL2, FNA) are available
- On Linux, install SDL2 dependencies: `sudo apt-get install libsdl2-dev`

### File Dialogs
- If native dialogs don't work, the system will use console input
- On Linux, install zenity: `sudo apt-get install zenity`
