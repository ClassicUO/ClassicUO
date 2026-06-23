# CLAUDE.md

Guidance for Claude Code when working in this repo.

## Project Overview

ClassicUO — open-source Ultima Online Classic Client. C# / .NET 9.0 / FNA-XNA. Architecture is ECS-first (TinyEcs + Bevy-style plugin layering). Game logic lives in `src/ClassicUO.Ecs/`. Mods are out-of-process WASM components (Component Model / WASI, `tinyecs:modding` + `cuo:modding` WIT); the host C# code does NOT use the modding API.

## Build & Run

```bash
dotnet build                                      # dev build
dotnet build -c Release
./scripts/build-naot.sh                           # AOT release
dotnet test
dotnet run --project src/ClassicUO.Bootstrap     # run
dotnet run --project src/ClassicUO.Bootstrap -- --renderer OpenGL
```

**You MUST build and run `ClassicUO.Ecs` (the ECS exe, `cuo-ecs`) after non-trivial changes** to confirm boot + golden-path behaviour. Type-check alone is not sufficient. `ClassicUO.Client` is the legacy OOP build target and is no longer the runtime path.

For UI / gump work the deterministic harness is preferred — see Agent Harness below.

## Project Layout

```
src/
├── ClassicUO.Assets/         UO file-format loaders (ART, MAP, GUMP, SOUND…)
├── ClassicUO.Bootstrap/       entry point + WASM host init
├── ClassicUO.Ecs/             ECS exe (cuo-ecs) — current runtime path.
│   ├── Assets/Engine/Gameplay/Modding/Network/Rendering/Scenes/UI/
│   │                          all ECS plugins + systems live here
│   ├── Agent/                 AGENT_BUILD JSON-RPC harness
│   └── Game/Configuration/Input/Network/Resources/...
│                              copied stub support trees (only types
│                              transitively reached from ECS code)
├── ClassicUO.Client/          legacy OOP exe (cuo) — full Game/Network
│                              source kept as parity reference
├── ClassicUO.IO/              low-level file I/O
├── ClassicUO.Renderer/        rendering primitives + effects + batcher
├── ClassicUO.Utility/         common helpers
└── Mods/                      WASM component EXAMPLES — for mods only, not host code
tools/agent-desktop/           JSON-RPC harness for driving the AGENT_BUILD client
```

---

## ECS Rules (HARD)

These are the rules. Apply them every time you write or review host ECS code in `src/ClassicUO.Ecs/`.

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

If you need component access by entity id, ADD A QUERY that selects that component; let the query do the lookup. The only `World` references that survive are in the modding bridge (`Modding/CuoModdingRegistry.cs` `IModComponent` impls) because the WASM guest bindings need raw entity access — host gameplay code does not.

### 2. Always go through `Commands` for mutation

Structural changes (spawn, despawn, insert, remove) MUST go through `Commands`. They are applied at the next sync point, which keeps systems running in parallel safe and observers firing in deterministic order. Never mutate component bags directly through a `World` reference.

For mutation of an existing component's fields, use the mutable ref from the query: `var (_, node) = q.Get(id); node.Ref.Left = Val.Px(x);` — this is in-place and does NOT need `Commands`.

### 3. `Local<T>` vs `Res<T>` / `ResMut<T>`

- **`Local<T>`** — per-system scratch (drag anchor, frame counters, cached lookup). Constructed per system instance; not shared. Use when the state's lifetime is "as long as the system runs."
- **`Res<T>`** — read-only singleton (e.g. `Res<MouseContext>`, `Res<AssetsServer>`, `Res<Time>`).
- **`ResMut<T>`** — singleton write access. Prefer `Res<T>` if you only read; ask for `ResMut<T>` only when you actually write.

Register singletons with `app.AddResource(new T())` at plugin build time. Do NOT use a static class to share state between systems — that hides ordering and breaks parallel scheduling. If two systems share state, it is a `Res` / `ResMut`.

**Time uses `Res<Time>`, never `System.Environment.TickCount64` / `DateTime.Now` / `Stopwatch`.** `Time.Total` is the engine clock in milliseconds (float, monotonic from boot). `Time.Frame` is the per-frame delta in seconds. Wall-clock APIs jump on system clock changes, miss frame-paused state, and bypass the deterministic harness — agent screenshots and replays drift.

**No closure-captured mutable state in system / observer lambdas.** A captured local survives one invocation only by accident — the lambda is stored, but the entity it observes (or the system instance) may be despawned and rebuilt between frames, resetting the closure to its captured initial value. Use the proper ECS slot for the lifetime you need:

- **System scratch** (counters, last-X timestamps, cached lookups): `Local<T>`.
- **Cross-system / cross-observer state**: register a `Res<T>` / `ResMut<T>` (`app.AddResource(new T())`).
- **Per-entity state**: a component on the entity itself (queried by the observer).
- **Constants needed inside the lambda** (a captured ushort / serial / lookup table): only capture immutable values whose lifetime exceeds the lambda's targets, and *never* capture an entity id whose entity might be rebuilt.

Counter-example: paperdoll backpack dclick originally stored `lastClick` as a closure local on the sprite's `Observe<UiClick>` — equip changes despawn+respawn the sprite mid-gesture, so the second click's closure was a fresh instance and the gap never tripped. Fix: `Res<PaperdollDClickState>` + a marker component on the sprite (`PaperdollBackpackUI`) read inside the observer.

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

Plugins are composed in `src/ClassicUO.Ecs/Boot.cs` (`CuoPlugin.Build`).

---

## UO Gump Behaviour Contract

Every UO gump window (server-pushed paperdoll, container, status bar, custom mod gump rendering through `UOCustomRender`) MUST share these behaviours. They are not reimplemented per gump — they fall out of the shared infra in `WindowDragPlugin.cs` + `UOGumpBundle` + `UiPick` (which wraps `UiHitTest`).

**`UiPick` (`src/ClassicUO.Ecs/UI/UiPick.cs`) is THE hit-test. Do NOT hand-roll another loop.** Every gesture — drag, right-click-close, top-bar yield, container pickup, hover selection — asks the same two questions and must use the same answers:
- `UiPick.Topmost(pos, assets, rendered)` — the topmost rendered, pixel-hit, *visible* element. It scans ALL rendered elements (`ComputedNode + UiCustom + Node`), not just movable roots, so an opaque child sprite (a container item, a paperdoll body/equipment overlay) over a window's transparent interior is the real hit. Ranks by `ComputedNode.PaintOrder` (Clay's z-then-tree order — z is folded in; **never tiebreak on `ClayId`**, an entity-id hash that flips on despawn/respawn).
- `UiPick.MovableRoot(entity, movables, parents)` — walks the `Parent` chain to the owning `UIMovable` window root. The hit is usually a child; the gesture targets its window.

Every recurring gump bug (overlapping windows picking the wrong one, right-click closing the window behind, the top bar swallowing clicks under a gump, the paperdoll only dragging from its frame) was a hand-rolled loop diverging from this. Tests: `tests/ClassicUO.Ecs.Tests/UiPickTests.cs`.

| Behaviour | How |
|-----------|-----|
| **Right-click closes** | Tag the window root with `UIMovable`. `WindowDragPlugin.CloseOnRightClick` (Stage.PreUpdate) resolves the window via `UiPick` (topmost element → `MovableRoot`), consumes the right-click and despawns the subtree (or routes container windows through `ContainerClosedEvent`). Closes the topmost window, not one behind it. |
| **Drag to move** | Same `UIMovable` tag. `WindowDragPlugin.Drag` (Stage.Update) latches on press-once via `UiPick` (so a press on any opaque child — paperdoll body/arch, container bg — drags the window), yields to `ContainerItemUI`/`PaperdollEquipUI` (pickup owns those), writes `Node.Left/Top`. Continuous-held pattern; one-frame Interaction lag avoided. |
| **Pixel-perfect hit-test** | `UiHitTest.PixelHit(assets, custom, bb, pos)` — bounding box reject + per-kind alpha check against the source gump/art mask; a click on a transparent pixel passes through. `UiPick.Topmost` calls it per candidate. Reach for `UiPick`, not `PixelHit` directly, unless you only need a single-element alpha test. |
| **Stack on interact (topmost on top)** | Only the window ROOT carries `GlobalZIndex`; `LayoutSystem` threads that z down to every descendant float automatically. On click latch, bump the root via `UiZCounter.Bump()` — the whole window lifts in one assignment. **Do NOT add per-child GlobalZIndex.** |
| **Click-capture to game world** | `WindowDragPlugin.ClaimSelectedFromMovable` (Stage.Last) claims `SelectedEntity` at `float.MaxValue` so world/pickup/use systems bail when the cursor is over a window. |

**To spawn a gump**: use `UOGumpBundle` (`src/ClassicUO.Ecs/UI/UOGump.cs`). It inserts `Node` + `UiCustom` + `UOCustomRender` + `Interaction.None` + `UOGump` + `UIMovable` + `GlobalZIndex(ZOrder)` in one go. Children are normal Bevy.UI nodes — no tags.

**Closing**: do not despawn from inside the gump's own systems. Right-click + `CloseOnRightClick` is the canonical close path. For server-driven closes (e.g. server cancels a container), send `ContainerClosedEvent` / equivalent and let `ContainerGumpPlugin.TearDownClosedUi` despawn.

Container windows have an extra item-aware selection path in `ContainerGumpPlugin.UpdateSelectedFromContainerUI`; the generic `ClaimSelectedFromMovable` filters them out via `Without<ContainerWindow>`. Mirror this filter if you add another item-aware claim.

**Buttons fire on release.** Click handlers use `On<UiClick>` (press+release inside same element; drag-off cancels). `On<UiPointerUp>` only when off-target release matters. (Window drag/close do NOT use pointer events — Clay only fires them on `Interaction`-bearing elements, and gump children are deliberately non-interactive so pickup's own scan owns them; the gestures use `UiPick` over the raw mouse press instead.)

---

## Custom Rendering (ClayUO commands)

UO-specific rendering primitives are dispatched through `ClayUOCommandType` inside Clay's `CLAY_RENDER_COMMAND_TYPE_CUSTOM`. To add a new primitive:

1. Add an enum value in `ClayUOCommandType` (`src/ClassicUO.Ecs/UI/GuiPlugin.cs`).
2. Add a `case` in `GuiRenderingPlugin.cs` custom command switch.
3. Pull asset via `Res<AssetsServer>` (`assets.Value.Gumps`, `.Arts`, `.Lands`, …).
4. Draw with `UltimaBatcher2D`. Respect `cmd.zIndex`.
5. If pixel-perfect hit-test is needed, extend `UiHitTest.PixelHit` (`src/ClassicUO.Ecs/UI/UiHitTest.cs`) with a matching case — bounding-box-only is not enough for transparent-area passthrough.

`ClayUOCommandData` lives at `src/ClassicUO.Ecs/UI/GuiPlugin.cs`; commands are buffered via `ClayUOCommandBuffer` and reset per frame.

---

## Networking

Packet handlers are individual `IncomingPacket` structs registered in `NetworkPlugin.cs`. Each is its own file under `src/ClassicUO.Ecs/Network/IncomingPackets/On…Packet_0xXX.cs`. To handle a new packet: write the struct, register it in `NetworkPlugin.Build`, react via Commands / Observers — same ECS rules.

---

## Modding (FOR MODS ONLY)

Mods are out-of-process WASM components, loaded through the WebAssembly Component Model (WASI) by the wasmtime-dotnet fork. Two WIT interfaces define the boundary:

- **`tinyecs:modding`** (generic) — lives in the reusable `TinyEcs.Bevy.Modding` library. Gives a guest the engine-level commands: spawn/despawn entities, component get/set, add-observer, resource get/set.
- **`cuo:modding`** (game-specific) — `src/ClassicUO.Ecs/Modding/wit/cuo/cuo-modding.wit`. Adds UO imports: `cuo:modding/net` (send / block packets, via `CuoNetBridge`), `cuo:modding/ui` (gump size, measure-text, cliloc lookup), input-consume.

Host composition lives in `src/ClassicUO.Ecs/Modding/`:
- `ModdingPlugin` (`Modding/ModdingPlugin.cs`) composes the generic `TinyEcs.Bevy.Modding.ModdingPlugin` (loader + per-stage dispatch) and supplies the cuo registry + per-mod bridge hooks.
- `CuoModdingRegistry.Build()` (`Modding/CuoModdingRegistry.cs`) is the whitelist: which host ECS components + resources a mod may read/write, keyed by WIT type-path (`cuo:ui/node` → `Node`, `cuo:player/hits` → `Hits`, `cuo:game/context`, …). A mod builds UI by spawning entities and setting the registered `cuo:ui/*` components — the host lays out + renders them like native gumps (tag with the movable-window markers for shared drag / right-click-close).
- Incoming packets surface as the `cuo:net/incoming` event (gated by `ModNetTap`, written from `NetworkPlugin.PacketReader`).

Built components deploy one folder per mod to `ecs-mods/<mod>/{mod.json, mod.wasm}` (copied next to the exe; the host scans `<exe>/ecs-mods/*/mod.json`). The `mod.json` manifest names the mod (`name`, `version`), the `wasm` file to load, and a reserved `ruleset` object (`ModManifest` in `TinyEcs.Bevy.Modding`). Examples in `src/Mods/`, each with its own `wit/`: `ecs-topbar` / `ecs-status` / `ecs-netlog` (Rust), `ecs-ui` (TypeScript, jco/componentize-js React). Design notes: `Modding/DESIGN.md`.

**Rule**: do not reach into the modding layer (`Modding/`) from host gameplay code. Host code uses Commands, Queries, Observers, Resources, Events — not the modding API. The registry + bridges exist to expose host capabilities to WASM guests, full stop.

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
