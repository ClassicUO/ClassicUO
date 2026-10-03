# HelloPlugin — Plugin v2 sample

The smallest possible v2 plugin. Subscribes to every lifecycle, input, and
packet event the contract exposes and appends a one-line entry to the log
file named in the `CUO_PLUGIN_TEST_LOG` environment variable.

Used by the smoke tests in `tests/ClassicUO.BootstrapHost.Tests/` to verify
the host's discovery and event-dispatch paths.

For the full author guide, see the
[`ClassicUO.PluginApi`](https://www.nuget.org/packages/ClassicUO.PluginApi)
package README.

## Files

- `HelloPlugin.cs` — the plugin class plus a `[ModuleInitializer]`-decorated
  static `Register` that pushes a `PluginRegistration` onto `PluginRegistry`
  at module-load time. Implements `IPlugin`.
- `HelloPlugin.csproj` — `net10.0` library that references
  `ClassicUO.PluginApi` with `Private=false` so the contract DLL isn't
  copied next to the plugin output.

## Anatomy

```csharp
internal static class HelloPluginRegistration
{
#pragma warning disable CA2255 // ModuleInitializer is the v2 plugin contract
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Register() =>
        PluginRegistry.Register(new PluginRegistration(
            id: "HelloPlugin",
            factory: static () => new HelloPlugin(),
            name: "Hello Plugin",
            version: "1.0.0"));
}

public sealed class HelloPlugin : IPlugin
{
    public void OnInitialize(IPluginContext context) { /* hook events */ }
    public void OnShutdown() { /* cleanup */ }
}
```

The entire v2 contract:

1. Implement `IPlugin`.
2. From a `[ModuleInitializer]` static, call `PluginRegistry.Register(...)`
   with an id and a `Func<IPlugin>` factory. The host runs your module
   constructor at discovery and reads what you registered.
3. In `OnInitialize`, hook the events you care about on the supplied
   `IPluginContext`. The host hands you one context, valid for the
   lifetime of the plugin.

## Try it

Build, drop the output into `Data/Plugins/HelloPlugin/` next to your
`ClassicUO.BootstrapHost` install, and launch the host:

```bash
dotnet build samples/HelloPlugin -c Release

mkdir -p "<ClassicUO>/Data/Plugins/HelloPlugin"
cp samples/HelloPlugin/bin/Release/net10.0/HelloPlugin.dll \
   "<ClassicUO>/Data/Plugins/HelloPlugin/"

export CUO_PLUGIN_TEST_LOG=/tmp/helloplugin.log
./ClassicUO.BootstrapHost
```

Watch `/tmp/helloplugin.log` while you play; lines like
`PacketIn:len=23,id=0xA1` will accumulate.

## See also

- [`ClassicUO.PluginApi`](https://www.nuget.org/packages/ClassicUO.PluginApi)
  — author guide and full contract reference.
- [`src/ClassicUO.BootstrapHost/README.md`](../../src/ClassicUO.BootstrapHost/README.md)
  — apphost / operator docs.
- [`tests/ClassicUO.BootstrapHost.Tests/SmokeTests.cs`](../../tests/ClassicUO.BootstrapHost.Tests/SmokeTests.cs)
  — what the smoke tests verify.
