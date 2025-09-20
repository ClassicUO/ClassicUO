# ARM64 Native Libraries for ClassicUO

This directory contains the ARM64 native libraries required for ClassicUO to run on ARM64 platforms (Windows ARM64, macOS Apple Silicon, Linux ARM64).

## Required Libraries

The following native libraries are required for ARM64 support:

### Windows ARM64 (win-arm64)
- `FAudio.dll` - Audio processing library
- `FNA3D.dll` - 3D graphics library  
- `libtheorafile.dll` - Video codec library
- `SDL2.dll` - Cross-platform multimedia library
- `vcruntime140.dll` - Visual C++ runtime
- `zlib.dll` - Compression library

### macOS ARM64 (osx-arm64)
- `libFAudio.0.dylib` - Audio processing library
- `libFNA3D.0.dylib` - 3D graphics library
- `libMoltenVK.dylib` - Vulkan implementation for macOS
- `libSDL2-2.0.0.dylib` - Cross-platform multimedia library
- `libtheorafile.dylib` - Video codec library
- `libvulkan.1.dylib` - Vulkan loader

### Linux ARM64 (linux-arm64)
- `libFAudio.so.0` - Audio processing library
- `libFNA3D.so.0` - 3D graphics library
- `libSDL2-2.0.so.0` - Cross-platform multimedia library
- `libtheorafile.so` - Video codec library

## Obtaining ARM64 Libraries

### Option 1: Build from FNA Source
1. Clone the FNA repository: `git clone https://github.com/FNA-XNA/FNA.git`
2. Follow the FNA build instructions for ARM64
3. Copy the generated ARM64 libraries to this directory

### Option 2: Use Pre-built Libraries
1. Download ARM64 libraries from FNA releases
2. Extract and copy to appropriate subdirectories:
   - `win-arm64/` for Windows ARM64 libraries
   - `osx-arm64/` for macOS ARM64 libraries  
   - `linux-arm64/` for Linux ARM64 libraries

### Option 3: Cross-compile
1. Use cross-compilation tools for your target platform
2. Build FNA and its dependencies for ARM64
3. Copy the resulting libraries here

## Directory Structure

```
external/arm64/
├── win-arm64/          # Windows ARM64 libraries
│   ├── FAudio.dll
│   ├── FNA3D.dll
│   ├── libtheorafile.dll
│   ├── SDL2.dll
│   ├── vcruntime140.dll
│   └── zlib.dll
├── osx-arm64/          # macOS ARM64 libraries
│   ├── libFAudio.0.dylib
│   ├── libFNA3D.0.dylib
│   ├── libMoltenVK.dylib
│   ├── libSDL2-2.0.0.dylib
│   ├── libtheorafile.dylib
│   └── libvulkan.1.dylib
├── linux-arm64/        # Linux ARM64 libraries
│   ├── libFAudio.so.0
│   ├── libFNA3D.so.0
│   ├── libSDL2-2.0.so.0
│   └── libtheorafile.so
└── README.md           # This file
```

## Notes

- These libraries are platform-specific and cannot be interchanged
- Make sure to use the correct architecture (ARM64) libraries
- The libraries should be compatible with the FNA version used in ClassicUO
- For development, you may need to build these libraries yourself

## Building FNA for ARM64

Refer to the FNA documentation for building ARM64 libraries:
- [FNA GitHub Repository](https://github.com/FNA-XNA/FNA)
- [FNA Building Documentation](https://github.com/FNA-XNA/FNA/wiki/1:-Download-and-Update-FNA)

## Troubleshooting

If you encounter issues with ARM64 libraries:

1. Verify the library architecture: `file library_name`
2. Check library dependencies: `ldd library_name` (Linux) or `otool -L library_name` (macOS)
3. Ensure all required libraries are present
4. Check that the libraries are compatible with your .NET runtime
