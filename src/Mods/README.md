## The idea

Make ClassicUO as moddable as possible across platforms. Mods are out-of-process
WebAssembly components (the WebAssembly Component Model / WASI), loaded by the host
through the `tinyecs:modding` + `cuo:modding` WIT interfaces. ClassicUO is the host;
mods read and write host ECS components/resources, observe packets, and build UI —
without forking the client.

## Examples

`src/Mods/` holds the WIT mod examples (each carries its own `wit/` directory):

- `ecs-topbar/` — adds a button to the host top bar (Rust).
- `ecs-status/` — replaces the host status gump with a mod-built one (Rust).
- `ecs-netlog/` — live-lists incoming packets via the `cuo:net/incoming` event (Rust).
- `ecs-ui/` — the full React/jco storybook UI mod (TypeScript, componentize-js).
- `ecs-csharp/` — a movable window + click counter (C#, wit-bindgen-dotnet +
  NativeAOT-LLVM). Guest bindings come from the `WitBindgen.SourceGenerator` /
  `WitBindgen.Runtime` NuGet packages; `make build-mods` `dotnet publish`es it
  (needs the NativeAOT-LLVM toolchain, like the Rust mods need cargo).

## Build & load

Each mod compiles to a `.wasm` component and ships in its own folder with a
`mod.json` manifest:

```
ecs-mods/<mod>/
  mod.json   { "name": "...", "version": "...", "wasm": "mod.wasm", "ruleset": {} }
  mod.wasm
```

The built folders live under `ecs-mods/` at the repo root and are copied next to
the exe on build; the host scans `<exe>/ecs-mods/*/mod.json` at startup, reads the
manifest, and loads the `wasm` it names. (`ruleset` is a reserved, empty object for
now.) See each mod's own build steps and the host registry in
`src/ClassicUO.Ecs/Modding/`.
