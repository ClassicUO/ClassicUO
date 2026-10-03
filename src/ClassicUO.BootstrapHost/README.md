# ClassicUO.BootstrapHost

The .NET 10 native apphost that loads `cuo` and runs Plugin v2 plugins.

License: BSD-2-Clause.

## What it is

`ClassicUO.BootstrapHost` is a sibling of the legacy `ClassicUO.Bootstrap`
(net472, ships as `ClassicUO.exe`). Both apphosts load the same `cuo`
NativeAOT binary via `LoadLibrary`/`dlopen` and fill the same
`HostBindings` function-pointer table — but `BootstrapHost`:

- runs on `net10.0` natively on Windows, Linux, and macOS (no WINE, no Mono);
- loads **v2 plugins only** via `AssemblyLoadContext` per plugin;
- uses `[UnmanagedCallersOnly]` + `delegate* unmanaged[Cdecl]<...>` for the
  cuo boundary (no `Marshal.GetFunctionPointerForDelegate` thunks);
- exposes packet handlers as `ReadOnlySpan<byte>` over the live network
  buffer (no copy in/out, no pin lifetime hazard).

`cuo` itself is unchanged. Users pick their apphost at launch.

| Apphost                       | Runtime    | Plugins it loads             | Multi-platform |
| ----------------------------- | ---------- | ---------------------------- | -------------- |
| `ClassicUO.exe` (legacy)      | net472     | v1 (e.g., classic Razor)     | Windows / Mono / WINE |
| `ClassicUO.BootstrapHost`     | net10.0    | v2 (this contract)           | Native everywhere     |

## Build

```bash
dotnet build src/ClassicUO.BootstrapHost -c Release
```

Output: `bin/Release/net10.0/ClassicUO.BootstrapHost.dll` (framework-dependent).

For a self-contained per-RID single binary:

```bash
dotnet publish src/ClassicUO.BootstrapHost -c Release -r linux-x64   /p:PublishingForRelease=true
dotnet publish src/ClassicUO.BootstrapHost -c Release -r win-x64     /p:PublishingForRelease=true
dotnet publish src/ClassicUO.BootstrapHost -c Release -r osx-x64     /p:PublishingForRelease=true
dotnet publish src/ClassicUO.BootstrapHost -c Release -r osx-arm64   /p:PublishingForRelease=true
```

`PublishingForRelease=true` enables `SelfContained=true`,
`PublishSingleFile=true`, embedded debug symbols, ReadyToRun + composite
pre-compilation, and tiered PGO.

### Why ReadyToRun and not full NativeAOT

The release publish profile uses `PublishReadyToRun` + `PublishReadyToRunComposite`
+ `TieredPGO` rather than `PublishAot=true`. NativeAOT eliminates the JIT
entirely, which is incompatible with the v2 plugin model: the host loads
managed plugin DLLs at runtime via `LoadFromAssemblyPath`, and without a JIT
the plugin's IL has no way to execute. R2R + composite pre-compiles the host
and framework code to native (fast cold start, no warmup tax on the host's
own code), while keeping the JIT available so plugin assemblies still run.

## Run

The apphost expects to find `cuo.dll` (or `cuo.so`/`cuo.dylib` per OS) next
to itself, exactly like the legacy Bootstrap does. It then discovers
plugins under:

```
<install>/Data/Plugins/<name>/<name>.dll
```

One folder per plugin; folder name and DLL name must match.

### Building cuo as a shared library

Default `dotnet publish` of `ClassicUO.Client` produces `cuo.exe` (NativeAOT
exe with a `Main` entry — useful for direct launch, but `[UnmanagedCallersOnly]`
methods like `Initialize` aren't exposed as DLL exports). To get the apphost-
loadable artifact, set `BootstrapHostMode=true`:

```bash
dotnet publish src/ClassicUO.Client -c Release -r win-x64 -p:BootstrapHostMode=true
dotnet publish src/ClassicUO.Client -c Release -r linux-x64 -p:BootstrapHostMode=true
dotnet publish src/ClassicUO.Client -c Release -r osx-x64 -p:BootstrapHostMode=true
```

This switches `OutputType` to `Library` and `NativeLib` to `Shared`, producing
`cuo.dll` / `cuo.so` / `cuo.dylib` with `Initialize` exposed as a native export.
The published binary lives under `bin/dist/`.

### Staging a runnable layout

A convenience PowerShell script bundles the cuo publish output, the apphost
binary, and the sample plugin into `bin/test-bootstraphost/`:

```pwsh
pwsh tools/stage-bootstraphost.ps1
```

Drop a `settings.json` (with a valid `UltimaOnlineDirectory`) into that folder
and run `ClassicUO.BootstrapHost.exe`. Set `CUO_PLUGIN_TEST_LOG` to a writable
path to capture the HelloPlugin event stream.

## How it loads plugins

1. `Program.Main` constructs a `HostBridge` and calls `Run(args)`.
2. `Run` resolves the cuo native library, loads it via `NativeLibrary.Load`,
   and locates the `Initialize` export.
3. The bridge runs plugin discovery: every `Data/Plugins/<name>/<name>.dll`
   is loaded into its own non-collectible `AssemblyLoadContext`. The
   contract DLL `ClassicUO.PluginApi` is forced into the default ALC so
   interface identity (and `PluginRegistry`'s static state) holds across
   plugins.
4. For each plugin assembly, the loader calls
   `RuntimeHelpers.RunModuleConstructor(asm.ManifestModule.ModuleHandle)`
   to force the plugin's `[ModuleInitializer]` to run, then drains
   `PluginRegistry.DrainPending()` to read what the plugin registered. This
   path uses no reflection — no `GetTypes()`, no `Activator.CreateInstance`.
5. The bridge fills a `HostBindings` struct of native function pointers and
   calls cuo's `Initialize`.
6. cuo calls back into the bridge's `[UnmanagedCallersOnly]` static methods
   for every event; each call fans out to every loaded plugin's
   `IPluginContext` event subscribers and aggregates block-flags / return
   values.

## Smoke testing

`tests/ClassicUO.BootstrapHost.Tests/` validates the discovery + dispatch
plumbing without needing `cuo.dll`:

```bash
dotnet test tests/ClassicUO.BootstrapHost.Tests
```

The tests stage a temp `Plugins/HelloPlugin/` folder containing the sample
plugin's DLL and drive the lifecycle through the bridge's test-only entry
points (`LoadPluginsForTest`, `TestRaise*`). The sample plugin's behavior
is verified by reading the per-test event log it writes.

## Not yet supported

The current contract covers packet observe-and-block, input, actions,
client services, and lifecycle. Out of scope today:

- **Packet mutation.** Handlers can block packets but cannot rewrite them.
- **In-world plugin UI overlays** (no `IGumpHost`).
- **Player / item / mobile state queries** (no `IWorld`).
- **SDL event forwarding** (`SdlEventFn` returns 0).
- **Legacy `CmdListFn`** for plugin draw commands.

Mutation and UI surfaces require a copy + writeback contract and a clear
ordering rule across multiple subscribers, which is intentionally deferred
until the rest of the surface settles.
