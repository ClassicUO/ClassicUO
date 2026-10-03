# ClassicUO.PluginApi

Public contract for **ClassicUO Plugin v2**. Plugin authors reference this
package; the v2 apphost (`ClassicUO.BootstrapHost`) provides the implementation.

License: BSD-2-Clause. Target: `net10.0`.
Source: <https://github.com/ClassicUO/ClassicUO>

## What v2 plugins look like

A plugin is a `net10.0` class library that implements `IPlugin` and registers
itself via a `[ModuleInitializer]` static method. The host discovers it under
`Data/Plugins/<folder>/<folder>.dll`, runs the module constructor, and reads
the resulting `PluginRegistry` entry — no reflection, no attribute scan.

```csharp
using System.Runtime.CompilerServices;
using ClassicUO.PluginApi;

internal static class MyPluginRegistration
{
#pragma warning disable CA2255 // ModuleInitializer is the documented v2 plugin contract
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Register() =>
        PluginRegistry.Register(new PluginRegistration(
            id: "MyPlugin",
            factory: static () => new MyPlugin(),
            name: "My Plugin",
            version: "1.0.0"));
}

public sealed class MyPlugin : IPlugin
{
    public void OnInitialize(IPluginContext ctx)
    {
        ctx.Connected += () => ctx.Client.SetWindowTitle("connected!");

        ctx.Input.Hotkey += (key, mod, pressed) =>
        {
            if (key == 27 /* ESC */ && pressed)
                ctx.Actions.CastSpell(1);
            return true;        // false = block default behavior
        };

        ctx.Packets.Incoming += (ReadOnlySpan<byte> p, ref bool block) =>
        {
            // p is valid only for the duration of this call.
            // Do not store it; copy with p.ToArray() if you need the bytes.
            if (p.Length > 0 && p[0] == 0x1B) // login confirm
                ctx.Client.SetWindowTitle("logged in");
        };
    }

    public void OnShutdown() { /* save state */ }
}
```

The factory passed to `PluginRegistration` is a `Func<IPlugin>`, so a single
DLL can register multiple plugins (rarely useful) and the host never needs
`Activator.CreateInstance` — the discovery path is fully reflection-free and
trim-friendly.

## Project setup

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <OutputType>Library</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ClassicUO.PluginApi" Version="1.0.0" />
  </ItemGroup>
</Project>
```

Build, then drop the resulting `MyPlugin.dll` (plus any private dependencies)
into:

```
<ClassicUO install>/Data/Plugins/MyPlugin/MyPlugin.dll
```

The folder name and the DLL name **must** match. Run `ClassicUO.BootstrapHost`
(not the legacy `ClassicUO.exe`) to load v2 plugins.

## Surface at a glance

| Service                    | What it does                                                  |
| -------------------------- | ------------------------------------------------------------- |
| `ctx.Packets`              | Subscribe to / inject server↔client packets (ReadOnlySpan)    |
| `ctx.Input`                | Hotkey + mouse routing; hotkey handlers return false to block |
| `ctx.Actions`              | `CastSpell`, `RequestMove`, `TryGetPlayerPosition`            |
| `ctx.Client`               | `SetWindowTitle`, `GetCliloc`                                 |
| `ctx.Game`                 | `IsGameThread`, `Post`, `PostAsync` (game-thread marshaling)  |
| `ctx.Connected/Disconnected/FocusGained/FocusLost/Tick/Closing` | Lifecycle events                            |
| `ctx.PlayerPositionChanged`| `(x, y, z)` whenever the player tile changes                  |
| `ctx.UODataPath`           | Path to the user's UO data directory                          |
| `ctx.PluginDataPath`       | Per-plugin folder, owned by your plugin                       |

## Threading

- All lifecycle events fire on the game thread.
- Timers and async work end up on whatever thread you started them on. Use
  `ctx.Game.Post(...)` to marshal back before touching world state, calling
  `Actions.RequestMove`, etc. (`Actions.RequestMove` will throw if called off
  the game thread; `CastSpell` auto-marshals.)

## Packet handler contract

```csharp
public delegate void PacketHandler(ReadOnlySpan<byte> packet, ref bool block);
```

- The span aliases the live network buffer. **Do not store it** or any
  pointer derived from it. Copy with `packet.ToArray()` if you need the bytes
  past the handler return.
- Set `block = true` to prevent the client from seeing the packet.
- If multiple plugins subscribe, the packet is blocked if **any** handler
  sets `block = true`.

## Hotkey blocking

```csharp
public delegate bool HotkeyHandler(int key, int modifiers, bool pressed);
```

- Return `false` to suppress the client's default handling for that key.
- If multiple plugins subscribe, the hotkey is suppressed if **any** handler
  returns `false`.

## Sample

A worked example lives at
[`samples/HelloPlugin`](https://github.com/ClassicUO/ClassicUO/tree/main/samples/HelloPlugin)
in the ClassicUO repository.

## Status

The current contract covers packet observe-and-block, input, actions, client
services, and lifecycle. Out of scope today: packet **mutation**,
in-world plugin UI overlays (`IGumpHost`), player/item/mobile state queries
(`IWorld`), and SDL event forwarding.
