# CLAUDE.md

Guidance for Claude Code when working in this repo.

## Project Overview

ClassicUO — open-source Ultima Online Classic Client. C# / .NET 9.0 / FNA-XNA. Architecture is ECS-first (TinyEcs + Bevy-style plugin layering). Game logic lives in `src/ClassicUO.Client/Ecs/`. Mods are out-of-process WASM (Extism); the host C# code does NOT use the modding API.

## Build & Run

```bash
dotnet build                                      # dev build
dotnet build -c Release
./scripts/build-naot.sh                           # AOT release
dotnet test
dotnet run --project src/ClassicUO.Bootstrap     # run
dotnet run --project src/ClassicUO.Bootstrap -- --renderer OpenGL
```

**You MUST build and run `ClassicUO.Client` after non-trivial changes** to confirm boot + golden-path behaviour. Type-check alone is not sufficient.

For UI / gump work the deterministic harness is preferred — see Agent Harness below.

## Project Layout

```
src/
├── ClassicUO.Assets/         UO file-format loaders (ART, MAP, GUMP, SOUND…)
├── ClassicUO.Bootstrap/       entry point + WASM host init
├── ClassicUO.Client/          game logic, ECS, networking, rendering glue
│   └── Ecs/                   all gameplay/UI/rendering systems live here
├── ClassicUO.IO/              low-level file I/O
├── ClassicUO.Renderer/        rendering primitives + effects + batcher
├── ClassicUO.Utility/         common helpers
└── Mods/                      WASM plugin EXAMPLES — for mods only, not host code
tools/agent-desktop/           JSON-RPC harness for driving the AGENT_BUILD client
```

---

## ECS Rules (HARD)

These are the rules. Apply them every time you write or review host ECS code in `src/ClassicUO.Client/Ecs/`.

### 1. Never touch `TinyEcs.World` directly

In systems / observers / bundles, do NOT call `world.Has<T>(id)`, `world.Get<T>(id)`, `world.Add<T>`, `world.Set<T>`, `world.Remove<T>`, `world.Spawn`, `world.Despawn`. Those bypass scheduling and break determinism.

Use:

| Need | Use |
|------|-----|
| Read / mutate a single entity's components | `Query<Data<T,…>>` then `q.Contains(id)` + `q.Get(id)` |
| Existence check | `q.Contains(id)` (NOT `world.Has<T>`) |
| Spawn / despawn / add / remove components | `Commands` (`commands.Spawn(...)`, `commands.Entity(id).Insert(...)`, `.Despawn()`, `.Remove<T>()`) |
| Singleton state | `Res<T>` (read) / `ResMut<T>` (write) — see rule 3 |
| Per-system scratch state | `Local<T>` — see rule 3 |

If you need component access by entity id, ADD A QUERY that selects that component; let the query do the lookup. The only `World` reference that survives is in `Modding/Host/Api.cs` because Extism guest bindings need it — host gameplay code does not.

### 2. Always go through `Commands` for mutation

Structural changes (spawn, despawn, insert, remove) MUST go through `Commands`. They are applied at the next sync point, which keeps systems running in parallel safe and observers firing in deterministic order. Never mutate component bags directly through a `World` reference.

For mutation of an existing component's fields, use the mutable ref from the query: `var (_, node) = q.Get(id); node.Ref.Left = Val.Px(x);` — this is in-place and does NOT need `Commands`.

### 3. `Local<T>` vs `Res<T>` / `ResMut<T>`

- **`Local<T>`** — per-system scratch (drag anchor, frame counters, cached lookup). Constructed per system instance; not shared. Use when the state's lifetime is "as long as the system runs."
- **`Res<T>`** — read-only singleton (e.g. `Res<MouseContext>`, `Res<AssetsServer>`, `Res<Time>`).
- **`ResMut<T>`** — singleton write access. Prefer `Res<T>` if you only read; ask for `ResMut<T>` only when you actually write.

Register singletons with `app.AddResource(new T())` at plugin build time. Do NOT use a static class to share state between systems — that hides ordering and breaks parallel scheduling. If two systems share state, it is a `Res` / `ResMut`.

### 4. Prefer Observers for system→system interop

When system A produces an event that system B reacts to, the default is an **observer** keyed on a component change (`OnInsert<T>`, `OnRemove<T>`, `OnAdd<T>`) or a custom trigger. Reasons:

- Observer runs synchronously after the structural change is applied — no one-frame lag, no `Changed<T>` scan.
- Wiring is local (`app.AddObserver(...)`) and discoverable from the producer side.
- No polling per frame.

Examples in tree:
- `PaperdollPlugin.cs:117` — `OnInsert<EquipmentSlots>` rebuilds the paperdoll body subtree.
- `NetworkEntitiesMapPlugin.cs:27` — `OnRemove<EquipmentSlots>` / `OnRemove<NetworkSerial>` keep the serial→entity map in sync.
- `LoginScreenPlugin.cs:187` — `.Observe((On<UiClick> trigger) => …)` for entity-scoped UI events.
- `NetworkPlugin.cs:197` — `AddObserver` on `OnLoginRequest` triggers connect + send.

Fall back to `EventWriter` / `EventReader` only when the producer and consumer are decoupled in time (cross-frame) or fan out 1→N.

### 5. Plugin shape

Each subsystem is `internal readonly struct XPlugin : IPlugin` with `Build(App app)`. Inside `Build`:

```csharp
app.AddResource(new MyResource());
var systemFn = MySystem;
app.AddSystem(systemFn).InStage(Stage.Update).Build();
app.AddObserver<OnInsert<MyComponent>, Commands, MyParams>(MyHandler);
app.AddPlugin<ChildPlugin>();
```

Systems are static methods taking `Res<...> / ResMut<...> / Local<...> / Query<...> / Commands / EventWriter<...> / EventReader<...>` parameters — never `World`.

Plugins are composed in `src/ClassicUO.Client/Ecs/Boot.cs` (`CuoPlugin.Build`).

---

## UO Gump Behaviour Contract

Every UO gump window (server-pushed paperdoll, container, status bar, custom mod gump rendering through `UOCustomRender`) MUST share these behaviours. They are not reimplemented per gump — they fall out of the shared infra in `WindowDragPlugin.cs` + `UOGumpBundle` + `UiHitTest`.

| Behaviour | How |
|-----------|-----|
| **Right-click closes** | Tag the window root with `UIMovable`. `WindowDragPlugin.CloseOnRightClick` (Stage.PreUpdate) consumes the right-click and despawns the subtree (or routes container windows through `ContainerClosedEvent`). |
| **Drag to move** | Same `UIMovable` tag. `WindowDragPlugin.Drag` (Stage.Update) latches on press-once, writes `Node.Left/Top`. Continuous-held pattern; one-frame Interaction lag avoided. |
| **Pixel-perfect hit-test** | `UiHitTest.PixelHit(assets, custom, bb, pos)` — bounding box reject + per-kind alpha check against the source gump/art mask. A click on a transparent pixel passes through. This is used by every hit path: drag latch, right-click close, click-capture. |
| **Stack on interact (topmost on top)** | Only the window ROOT carries `GlobalZIndex`; `LayoutSystem` threads that z down to every descendant float automatically. On click latch, bump the root via `UiZCounter.Bump()` — the whole window lifts in one assignment. **Do NOT add per-child GlobalZIndex.** |
| **Click-capture to game world** | `WindowDragPlugin.ClaimSelectedFromMovable` (Stage.Last) claims `SelectedEntity` at `float.MaxValue` so world/pickup/use systems bail when the cursor is over a window. |

**To spawn a gump**: use `UOGumpBundle` (`src/ClassicUO.Client/Ecs/UI/UOGump.cs`). It inserts `Node` + `UiCustom` + `UOCustomRender` + `Interaction.None` + `UOGump` + `UIMovable` + `GlobalZIndex(ZOrder)` in one go. Children are normal Bevy.UI nodes — no tags.

**Closing**: do not despawn from inside the gump's own systems. Right-click + `CloseOnRightClick` is the canonical close path. For server-driven closes (e.g. server cancels a container), send `ContainerClosedEvent` / equivalent and let `ContainerGumpPlugin.TearDownClosedUi` despawn.

Container windows have an extra item-aware selection path in `ContainerGumpPlugin.UpdateSelectedFromContainerUI`; the generic `ClaimSelectedFromMovable` filters them out via `Without<ContainerWindow>`. Mirror this filter if you add another item-aware claim.

---

## Custom Rendering (ClayUO commands)

UO-specific rendering primitives are dispatched through `ClayUOCommandType` inside Clay's `CLAY_RENDER_COMMAND_TYPE_CUSTOM`. To add a new primitive:

1. Add an enum value in `ClayUOCommandType` (`src/ClassicUO.Client/Ecs/UI/GuiPlugin.cs`).
2. Add a `case` in `GuiRenderingPlugin.cs` custom command switch.
3. Pull asset via `Res<AssetsServer>` (`assets.Value.Gumps`, `.Arts`, `.Lands`, …).
4. Draw with `UltimaBatcher2D`. Respect `cmd.zIndex`.
5. If pixel-perfect hit-test is needed, extend `UiHitTest.PixelHit` (`src/ClassicUO.Client/Ecs/UI/UiHitTest.cs`) with a matching case — bounding-box-only is not enough for transparent-area passthrough.

`ClayUOCommandData` lives at `src/ClassicUO.Client/Ecs/UI/GuiPlugin.cs`; commands are buffered via `ClayUOCommandBuffer` and reset per frame.

---

## Networking

Packet handlers are individual `IncomingPacket` structs registered in `NetworkPlugin.cs`. Each is its own file under `src/ClassicUO.Client/Ecs/Network/IncomingPackets/On…Packet_0xXX.cs`. To handle a new packet: write the struct, register it in `NetworkPlugin.Build`, react via Commands / Observers — same ECS rules.

---

## Modding (FOR MODS ONLY)

Mods are out-of-process WASM, loaded by Extism. The host (`src/ClassicUO.Client/`) talks to mods through:

- **Host → guest**: `HostMessage` (`src/ClassicUO.Client/Ecs/Modding/Host/HostMessages.cs`).
- **Guest → host**: `PluginMessage` (`src/ClassicUO.Client/Ecs/Modding/Guest/PluginMessages.cs`) + `Api.Functions` bindings in `Modding/Host/Api.cs`.
- **UI**: mods construct UI through `cuo_ui_node` (JSON UINode tree) — host deserializes into ECS entities and routes through `GuiPlugin` / `GuiRenderingPlugin`.

The React reconciler (`src/Mods/user-interface/src/react/reconciler.ts`) is one mod example built on top of those bindings. It is NOT used by host C# code.

**Rule**: do not import or reach into `Modding/Guest/` or `Modding/Host/Api.cs` from host gameplay code. Host code uses Commands, Queries, Observers, Resources, Events — not the modding API. The modding API exists to expose host capabilities to WASM guests, full stop.

To write or modify a mod: `src/Mods/user-interface/` (TypeScript + React reconciler), `src/Mods/sandbox/` (Rust), `src/Mods/my-plugin/` (C#).

---

## Agent Harness

The `AGENT_BUILD` flavour of `ClassicUO.Client` exposes a JSON-RPC server (TCP loopback) for scripted UI / parity / screenshot scenarios driven by `tools/agent-desktop/`.

Read `tools/agent-desktop/AGENTS.md` BEFORE invoking any `agent-desktop` verb — it covers:

- Build commands (`-p:AGENT_BUILD=true`).
- The verb catalog (`up`, `down`, `rpc-shot`, `rpc-click`, `rpc-type`, `script`, …).
- `settings.json` pins (window size, server ip/port, UO data dir) required for deterministic input coordinates.
- ModernUO boot (`127.0.0.1:2593`, `admin/admin`).
- Pitfalls (SDL2 vs SDL3 input handler, stale `port.json`).

All agent code is gated on `AGENT_BUILD`; the production build strips it out. Do not move automation logic outside that gate.

When verifying a UI change, prefer the harness loop (`up --persist` → `rpc-click` / `rpc-shot` → `down`) over manual clicking — it's deterministic and screenshots are diffable.

---

## Conventions

- Don't add error handling, fallbacks, or validation for impossible cases. Trust internal callers; validate only at system boundaries (network, file I/O, user input).
- Don't write comments that restate what the code does. Comments earn their place only by explaining a non-obvious WHY: a hidden constraint, a workaround, a subtle invariant.
- Don't add backward-compat shims unless something on the network or asset boundary actually requires them.
- For UI/frontend changes you cannot test, say so explicitly — type-check is not feature-correctness.
- Test database / mocking: real ECS in tests, do not stub `Commands` / queries.
