# ARM64 Support for ClassicUO

ClassicUO now supports ARM64 architecture across multiple platforms, bringing the future of computing to Ultima Online!

## Supported Platforms

### Windows ARM64
- **Target**: Windows 10/11 on ARM64 devices
- **Runtime**: `win-arm64`
- **Examples**: Surface Pro X, Lenovo ThinkPad X13s, HP Elite Folio

### macOS ARM64 (Apple Silicon)
- **Target**: macOS on Apple Silicon (M1, M2, M3, etc.)
- **Runtime**: `osx-arm64`
- **Examples**: MacBook Air M1/M2, MacBook Pro M1/M2/M3, Mac Studio, Mac Pro

### Linux ARM64
- **Target**: Linux distributions on ARM64
- **Runtime**: `linux-arm64`
- **Examples**: Raspberry Pi 4/5, ARM-based servers, ARM laptops

## Building for ARM64

### Quick Start

#### Windows
```cmd
# Build for current platform (auto-detects ARM64)
scripts\build-multiplatform.cmd Release

# Build specifically for ARM64
scripts\build-arm64.cmd Release
```

#### Linux/macOS
```bash
# Build for current platform (auto-detects ARM64)
scripts/build-multiplatform.sh Release

# Build specifically for ARM64
scripts/build-arm64.sh Release
```

### Manual Build

#### Windows ARM64
```cmd
dotnet publish src\ClassicUO.Client\ClassicUO.Client.csproj -c Release -p:Platform=ARM64 -r win-arm64 --self-contained true
```

#### macOS ARM64
```bash
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -p:Platform=ARM64 -r osx-arm64 --self-contained true
```

#### Linux ARM64
```bash
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -p:Platform=ARM64 -r linux-arm64 --self-contained true
```

## Native Libraries

ARM64 support requires platform-specific native libraries. These are located in:

```
external/arm64/
├── win-arm64/          # Windows ARM64 libraries
├── osx-arm64/          # macOS ARM64 libraries
└── linux-arm64/        # Linux ARM64 libraries
```

### Required Libraries

#### Windows ARM64
- `FAudio.dll` - Audio processing
- `FNA3D.dll` - 3D graphics
- `libtheorafile.dll` - Video codec
- `SDL2.dll` - Multimedia
- `vcruntime140.dll` - Visual C++ runtime
- `zlib.dll` - Compression

#### macOS ARM64
- `libFAudio.0.dylib` - Audio processing
- `libFNA3D.0.dylib` - 3D graphics
- `libMoltenVK.dylib` - Vulkan for macOS
- `libSDL2-2.0.0.dylib` - Multimedia
- `libtheorafile.dylib` - Video codec
- `libvulkan.1.dylib` - Vulkan loader

#### Linux ARM64
- `libFAudio.so.0` - Audio processing
- `libFNA3D.so.0` - 3D graphics
- `libSDL2-2.0.so.0` - Multimedia
- `libtheorafile.so` - Video codec

## Obtaining ARM64 Libraries

### Option 1: Build from FNA Source
1. Clone FNA: `git clone https://github.com/FNA-XNA/FNA.git`
2. Follow FNA ARM64 build instructions
3. Copy libraries to appropriate directories

### Option 2: Use Pre-built Libraries
1. Download from FNA releases
2. Extract to `external/arm64/[platform]/`

### Option 3: Cross-compile
1. Use cross-compilation tools
2. Build FNA for ARM64
3. Copy resulting libraries

## Performance Benefits

ARM64 architecture offers several advantages:

### Apple Silicon (M1/M2/M3)
- **Power Efficiency**: Up to 2x better battery life
- **Performance**: Native ARM64 performance vs x64 emulation
- **Thermal**: Cooler operation, less fan noise
- **Memory**: Unified memory architecture benefits

### Windows ARM64
- **Battery Life**: Extended battery life on ARM devices
- **Performance**: Native ARM64 execution
- **Compatibility**: Better compatibility with ARM-native apps

### Linux ARM64
- **Server Efficiency**: Lower power consumption for servers
- **Embedded**: Better suited for embedded applications
- **Cost**: Often more cost-effective for cloud deployments

## Troubleshooting

### Common Issues

#### Missing Native Libraries
```
Error: Unable to load DLL 'FNA3D.dll'
```
**Solution**: Ensure ARM64 libraries are in `external/arm64/[platform]/`

#### Wrong Architecture
```
Error: BadImageFormatException
```
**Solution**: Verify you're using ARM64 libraries, not x64

#### Plugin Compatibility
```
Warning: Plugin requires Windows-specific assemblies
```
**Solution**: Some plugins may not support ARM64. Check plugin documentation.

### Verification Commands

#### Check Library Architecture (Linux/macOS)
```bash
file external/arm64/linux-arm64/libFNA3D.so.0
# Should show: ELF 64-bit LSB shared object, ARM aarch64
```

#### Check Library Architecture (Windows)
```cmd
dumpbin /headers external\arm64\win-arm64\FNA3D.dll
# Look for "machine (ARM64)"
```

## Development Notes

### Cross-Platform Development
- Use `RuntimeInformation.ProcessArchitecture` to detect ARM64
- Test on actual ARM64 hardware when possible
- Consider ARM64-specific optimizations

### Plugin Development
- Test plugins on ARM64 platforms
- Avoid platform-specific code when possible
- Use .NET Standard for maximum compatibility

## Future Enhancements

Planned improvements for ARM64 support:

1. **Optimized Rendering**: ARM64-specific graphics optimizations
2. **Plugin Framework**: Enhanced ARM64 plugin compatibility
3. **Performance Profiling**: ARM64-specific performance tools
4. **CI/CD**: Automated ARM64 builds and testing

## Contributing

To contribute to ARM64 support:

1. Test on ARM64 hardware
2. Report issues with ARM64-specific details
3. Contribute ARM64 native library builds
4. Improve cross-platform compatibility

## Resources

- [FNA GitHub Repository](https://github.com/FNA-XNA/FNA)
- [.NET ARM64 Documentation](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog)
- [Apple Silicon Development](https://developer.apple.com/documentation/apple-silicon)
- [Windows ARM64 Development](https://docs.microsoft.com/en-us/windows/arm/)

---

**The future is ARM64, and ClassicUO is ready!** 🚀
