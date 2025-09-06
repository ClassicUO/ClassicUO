# Plugin Compatibility Guide

## Cross-Platform Plugin Support

ClassicUO maintains cross-platform compatibility (Windows, Linux, macOS) while supporting plugins that may have platform-specific dependencies.

### How It Works

When a plugin fails to load due to missing platform-specific assemblies (like `WindowsBase` for WPF components), ClassicUO will:

1. **Detect the issue**: Automatically identify if the error is related to missing Windows-specific assemblies
2. **Log a warning**: Display a clear message explaining the plugin is Windows-specific
3. **Continue gracefully**: Skip the incompatible plugin and continue running normally

### Example Behavior

```
[WARN] Plugin 'ClassicAssist.dll' requires Windows-specific assemblies and cannot run on this platform. 
This plugin is designed for .NET Framework or Windows-specific .NET versions. 
Plugin will be skipped. Error: Could not load type 'System.Windows.Threading.Dispatcher'...
```

### Benefits

- ✅ **Cross-platform compatibility**: Runs on Windows, Linux, and macOS
- ✅ **Graceful degradation**: Incompatible plugins don't crash the application
- ✅ **Clear feedback**: Users understand why certain plugins don't work
- ✅ **Future-proof**: Supports both legacy .NET Framework plugins and modern cross-platform plugins

### For Plugin Developers

To ensure your plugin works across all platforms:

1. **Avoid Windows-specific dependencies**: Don't use `System.Windows.Threading.Dispatcher`, WPF controls, or other Windows-only APIs
2. **Use cross-platform alternatives**: Use `System.Threading` for threading operations
3. **Test on multiple platforms**: Verify your plugin works on Windows, Linux, and macOS
4. **Provide fallbacks**: Implement graceful degradation when platform-specific features aren't available

### Known Windows-Specific Plugins

The following plugins are automatically detected as likely Windows-specific:
- ClassicAssist (Assistant)
- Razor Enhanced
- Steam integration plugins
- Any plugin with "windows" or "wpf" in the filename
