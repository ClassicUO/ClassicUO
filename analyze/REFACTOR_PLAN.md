# ClassicUO.Client — OOP → ECS Refactor Plan

**Goal:** Detangle spaghetti in `ClassicUO.Client`, prepare for an ECS architecture, keep the game runnable and the plugin API stable throughout.

**Non-goals (for now):** Changing the plugin ABI (`PluginHost`, `Network/Plugin.cs`). Touching `ClassicUO.IO`, `ClassicUO.Assets`, `ClassicUO.Renderer` beyond what's needed. Big-bang rewrites.

---

## Guiding principles

1. **Strangler fig, not rewrite.** Every step must leave the game playable. Commits are small, reversible.
2. **Seams before semantics.** Introduce interfaces/dispatch points *before* changing behavior. Behavior changes ride on top of seams.
3. **Characterization tests are the safety net.** We can't unit-test most of the code today. Before refactoring anything risky, capture current behavior with tests — even crude golden-value ones.
4. **GameObject stays as a facade during migration.** Components/systems grow next to it. OOP code keeps working by delegating to component storage under the hood. Remove the facade only when every caller is migrated.
5. **Plugin API is a hard boundary.** All refactoring happens behind it. The plugin surface doesn't move until a future milestone.

---

## Current pain (from scan)

### In scope — spaghetti to clean up

| File / area | Lines | Smell | Targeted in |
|---|---:|---|---|
| `Network/PacketHandlers.cs` | 7147 | Mega-switch; handlers reach into statics everywhere | Phase 1 |
| `Network/OutgoingPackets.cs` | 4670 | Static facade for every outbound packet | Phase 1 |
| `Network/Plugin.cs` | 1416 | Plugin glue tangled with packet flow | Phase 1 (seam only) |
| `Game/Managers/*` | 37 classes (~14k LOC) | Mostly static singletons; biggest: `MacroManager` 2606, `HouseCustomizationManager` 2099, `UIManager` 714 | Phase 2 |
| `Game/GameActions.cs` | 854 | Static, calls into everything — same shape as a Manager | Phase 2 |
| `Game/UltimaLive.cs` | 1078 | Cross-cutting static; subscribes to packets and mutates world | Phase 2 |
| `Game/UoAssist.cs` | — | Same pattern as UltimaLive | Phase 2 |
| `Game/Scenes/GameScene.cs` | 1280 | Mixed update/lifecycle | Phase 3 |
| `Game/Scenes/GameSceneInputHandler.cs` | 1530 | Modal input tangled with state | Phase 3 |
| `Game/Scenes/GameSceneDrawingSorting.cs` | 1302 | Render+sort+state | Phase 3 |
| `Game/Scenes/LoginScene.cs` | 1107 | State machine + UI + network mixed | Phase 3 |
| `GameController.cs` | 985 | God object — window/scene/input/network/audio | Phase 3 |
| `Game/Pathfinder.cs` | 1117 | Static, mutates `World` directly | Phase 3.5 |
| `Game/GameCursor.cs` | 778 | Holds drag/target/cursor state globally | Phase 3.5 |
| `Game/World.cs` | 878 | Static globals; queries+mutations mixed; **future ECS substrate** | Phase 3.5 → reshaped in Phase 4 |
| `Configuration/Profile.cs` | 776 | Settings god object accessed from everywhere | Phase 3.5 |
| `Game/UI/Gumps/OptionsGump.cs` | 4941 | One class per tab, no separation | Phase 3.7 |
| `Game/UI/Gumps/WorldMapGump.cs` | 3297 | Render+input+data fetch in one | Phase 3.7 |
| `Game/UI/Gumps/HouseCustomizationGump.cs` | 2136 | Tightly coupled to `HouseCustomizationManager` | Phase 3.7 |
| `Game/UI/Gumps/HealthBarGump.cs` | 1906 | Two variants merged via flags | Phase 3.7 |
| `Game/UI/Gumps/SpellbookGump.cs` | 1565 | Pagination + state + input mixed | Phase 3.7 |
| `Game/GameObjects/MobileAnimation.cs` | 2071 | Big switch on body type — **deferred**, dissolves into components in Phase 5 |
| `Game/GameObjects/Views/MobileView.cs` | 1342 | Same — **deferred** to Phase 5 |
| `Game/GameObjects/Mobile.cs`, `PlayerMobile.cs` | 1088 + 691 | Domain entities — **deferred**, become facades in Phase 4–5 |

### Out of scope (not spaghetti, just large)

- `Resources/Res*.Designer.cs` (~6k LOC) — auto-generated; do not touch.
- `Game/Data/ChairTable.cs`, `SpellsMastery.cs`, `SpellsMagery.cs` (~4k LOC) — pure data tables. Maybe extract to JSON later, but not part of refactor.
- `Network/Encryption/Twofish*.cs`, `Blowfish*.cs` (~1.6k LOC) — algorithmic; large but isolated and correct.

**Good news:** `GameObjects/Views/` is already a data/rendering split. That's a proto-ECS shape we can build on.

---

## Target architecture (north star, not step 1)

```
Entities         = integer IDs (reuse existing Serial where possible)
Components       = POD structs (Position, Graphic, Hue, Health, Container, Name, Animation, ...)
Systems          = one responsibility each (Movement, Animation, Rendering, NetworkInbound, ...)
World            = component store + entity registry (replaces static World.cs globals)
Dispatcher       = wires packets → systems, input → systems, tick → systems
Facades          = temporary OOP shells (Item/Mobile/...) that read/write component store, kept until callers migrate
Plugin API       = unchanged; sits in front of facades
```

No ECS framework dependency required yet — a hand-rolled sparse-set store is ~300 lines and keeps us free to adopt a library later (Arch, Friflo, DefaultEcs) once shape is proven.

---

## Phased plan

### Phase 0 — Safety net (prereq for everything)
- Add characterization tests for: packet round-trip (pick 10 common packets), `Pathfinder` result on fixed map fixture, `WalkerManager` step sequence, `World.Get(serial)` lookup, `GameActions` parameter encoding.
- Wire a **headless run mode**: `GameController` must be instantiable without SDL window for tests. Introduce `IPlatformHost` seam; current SDL path stays default.
- **Exit criteria:** CI runs tests on every PR; headless mode launches, processes a recorded packet log, and shuts down cleanly.

### Phase 1 — Packet dispatcher split
- Keep `PacketHandlers.cs` class, but move each handler group (login, movement, items, UI, ...) to a `PacketHandlers.<Group>.cs` partial.
- Extract dispatch table: `Dictionary<byte, Action<...>>` built once, replacing the mega-switch.
- Each handler takes explicit dependencies (`World world, UIManager ui, ...`) instead of reaching into statics. Internally those still resolve to the singletons — we're just *naming* the graph.
- **Exit criteria:** `PacketHandlers.cs` under 500 lines (pure dispatch); every handler's dependencies visible in its signature; tests can drive a handler without booting the client.

### Phase 2 — De-staticify all global facades
- Covers: every `Game/Managers/*` class, **plus** `GameActions`, `UltimaLive`, `UoAssist`, and any other static type that holds mutable state or cross-cuts the codebase.
- For each, extract an `I<Name>` interface containing *only the methods actually called externally*. Leave the static facade in place, pointing to a singleton instance.
- `GameController` owns construction; passes instances to scenes/handlers via constructor or a context object (`GameContext`).
- No call-site rewrites yet — keep the static shim so plugins and existing code compile. The point is to make the wiring *explicit* somewhere.
- Largest offenders (`MacroManager` 2606, `HouseCustomizationManager` 2099) get an *internal* split too — extract sub-responsibilities into separate classes/files behind the same interface.
- **Exit criteria:** Every static facade has an interface + instance form. New code must use the interface. `grep` shows a finite, shrinking set of remaining static call sites. No file in `Managers/` or comparable static facade exceeds 1000 lines.

### Phase 2.5 — Network outbound + plugin seam
- Apply the same dispatcher treatment to `OutgoingPackets.cs` (4670 lines): split per packet family into partials, each method takes explicit dependencies.
- `Network/Plugin.cs` (1416 lines): isolate the plugin-facing API behind `IPluginHost`. Internal packet flow stops calling `Plugin.*` directly; goes through the host interface. **Plugin ABI itself does not change.**
- **Exit criteria:** `OutgoingPackets.cs` under 500 lines (pure dispatch); `Plugin.cs` only contains the public ABI surface; everything else moves to `PluginRuntime` (internal).

### Phase 3 — Scene/controller slimming
- Split `GameSceneInputHandler.cs` → `IInputRouter` + per-mode handlers (target, walk, drag, macro).
- Split `GameSceneDrawingSorting.cs` → `ISceneRenderer` + sort strategy. Rendering is a natural first "system."
- Split `LoginScene.cs` → state machine extracted (`LoginStateMachine`), per-state UI handlers separated.
- `GameController` becomes a composition root: builds the DI graph, owns the main loop, delegates. Target <400 lines.
- **Exit criteria:** Every scene file under 500 lines; input/render/update are separate pipelines you can instantiate alone; `GameController` does no domain logic.

### Phase 3.5 — World, Pathfinder, Cursor, Profile
- `World.cs`: separate **queries** (`IWorldReader`) from **mutations** (`IWorldWriter`); identify the entity-registry portion (will become the ECS substrate in Phase 4) vs. transient game state (weather, season, lighting). No data layout change yet — just interface extraction.
- `Pathfinder.cs`: convert from static to instance class taking `IWorldReader`. Pure function where possible.
- `GameCursor.cs`: split cursor visual state from drag/target intent. Drag/target intent moves into Phase 2-style interfaces.
- `Configuration/Profile.cs`: split into typed sections (`InputProfile`, `RenderProfile`, etc.); single load/save entry point. No migration of existing profile files — same on-disk format.
- **Exit criteria:** No more `World.X = ...` from outside `World`. Pathfinder takes its world via parameter. Profile sections are independently testable.

### Phase 3.7 — UI Gump giants
- One PR per gump, in this order (smallest to largest, to refine the pattern):
  - `SpellbookGump` (1565) → split per page kind.
  - `HealthBarGump` (1906) → split modern vs. classic variants into separate classes sharing a base.
  - `HouseCustomizationGump` (2136) → already coupled to a Manager; split per design panel.
  - `WorldMapGump` (3297) → separate render, input, data fetching, marker store.
  - `OptionsGump` (4941) → one class per tab + a thin host gump.
- Pattern: **state class** + **input handler** + **render method**, each in its own file. Exactly the same shape we're applying to scenes.
- **Exit criteria:** No gump file over 800 lines. Each tab/panel can be opened in isolation in a test fixture.

### Phase 4 — Component store alongside GameObject (the ECS seed)
- Pick **one** facet to migrate first: recommend **Position/Tile occupancy** (hot, well-bounded, already semi-isolated in `World`/`Map`).
- Introduce `ComponentStore<T>` (sparse-set). Add `PositionComponent { ushort X, Y; sbyte Z; }`.
- `GameObject.X/Y/Z` becomes a facade that reads/writes the component store. Every setter funnels through one place.
- Add a `MovementSystem` that updates positions from `WalkerManager` input and publishes tile-change events.
- **Exit criteria:** Position data lives in the store; tests can assert position without instantiating a `Mobile`. Profiler shows no regression (expect slight win from cache-friendlier iteration).

### Phase 4.5 — Framework evaluation checkpoint
- After Phase 4 lands, we have one component (Position) iterating through a hand-rolled `ComponentStore<T>` in a real scene. Now — and only now — decide whether to adopt an ECS framework.
- **Bench harness:** record a 60s gameplay session (busy town, ~2–5k visible objects). Replay it headless, measuring per-frame cost of Position iteration + Movement system.
- **Candidates to bench against the hand-rolled store:**
  - **Arch** — archetype-based, modern, fastest in synthetic benchmarks. Best fit if entity shapes are stable.
  - **Leopotam EcsLite** — sparse-set, minimal API, closest to our hand-rolled shape (lowest-cost swap).
  - **DefaultEcs** — source-gen queries, mature. Pick if we want compile-time query checking.
  - **Friflo.Engine.ECS** — heavier; only consider if we want built-in serialization (save states, replays).
- **Decision criteria (in order):**
  1. Facade compatibility — can `GameObject.X` still read/write without per-access allocation?
  2. Perf — must be ≥ hand-rolled on our workload, not synthetic.
  3. API ergonomics for the team — readability matters more than 5% benchmark wins.
  4. Dependency cost — binary size, license, maintenance activity.
- **Possible outcomes:**
  - **Stay hand-rolled.** Likely if the store stays under ~500 lines and perf is fine. Zero dependency, full control.
  - **Adopt Leopotam.** Lowest-friction swap; sparse-set semantics match what we built.
  - **Adopt Arch.** Worth the bigger refactor only if archetype iteration shows clear win on tile/static rendering.
- **Exit criteria:** Decision documented with bench numbers in `analyze/ECS_FRAMEWORK_DECISION.md`. If adopting a framework, Phase 5 is replanned around its API before continuing.

### Phase 5 — Peel more components
- Next candidates, in order: `GraphicComponent` (graphic+hue), `HealthComponent`, `ContainerComponent`, `AnimationComponent`. Each migration is its own PR: add component, move storage, convert one system to iterate components instead of polymorphic GameObject.
- Managers convert to systems one-for-one where the mapping is clean (`AnimatedStaticsManager` → `AnimatedStaticsSystem`, `EffectManager` → `EffectSystem`, etc.).
- `Views/*View.cs` convert to stateless render systems iterating component tuples. This is where the current `Views` split pays off.

### Phase 6 — Retire facades (future milestone, not now)
- Once all component data is migrated and all systems iterate components, `GameObject`/`Item`/`Mobile` shrink to thin wrappers, then delete. Plugin API is re-pointed at component accessors.

---

## Sequencing rationale

- **Phase 0 is non-negotiable.** Without tests we'll regress silently.
- **Phases 1–3 don't require ECS.** They pay for themselves in readability/testability even if ECS gets deferred. They also expose the true dependency graph, which is what ECS needs anyway.
- **Phase 4 is the first "real" ECS commitment.** Pick one slice, prove the pattern end-to-end (storage + system + facade + tests + perf), *then* scale.
- **Phase 5 is repetitive and parallelizable.** Once the pattern is proven, multiple components can migrate in parallel PRs.

---

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| Hidden state reads/writes break silently | Characterization tests + headless replay of recorded packet logs |
| Performance regression from component indirection | Bench a hot loop (draw sort, animation) before & after Phase 4; abort slice if >5% regression |
| Plugin API leaks internals | Keep a single `PluginHost` seam; any new types stay `internal` |
| Refactor stalls mid-migration, leaving worse mess | Every phase exits with the code shippable. Never merge a half-migrated component. |
| Scope creep into IO/Renderer | Hard rule: those projects are touched only for seams, never for behavior change, until Client is stable |

---

## What I'd want to confirm before Phase 0

- **Testing framework:** stick with whatever `tests/ClassicUO.UnitTests/` already uses, or want a switch?
- **Packet log fixtures:** do you have recordings we can use for replay tests, or do we need to capture fresh ones?
- **Perf baseline:** is there an existing benchmark harness, or do we add one in Phase 0?
- **Platforms to keep working during migration:** just desktop SDL, or must WASM/mobile builds stay green each phase?

---

## Minimum first PR (if you want to start tomorrow)

1. Add `IPlatformHost` abstraction; move SDL bits behind it.
2. Add a headless test runner that boots `GameController` with a stub host and a recorded packet stream.
3. Add characterization tests for 5 packet handlers (the most-touched ones).

That single PR unlocks Phases 1+ without committing to any architectural direction. If ECS gets deprioritized later, none of this work is wasted.

---

## Appendix — Worked examples

Concrete before/after for the three biggest phases, grounded in real files.

### Example: Phase 1 — Packet dispatcher split

**Today.** One 7147-line `Network/PacketHandlers.cs`: a static `_handlers[0x100]` array, ~150 `Handler.Add(0xNN, Method)` registrations in a single `Initialize()`, and ~150 `private static` handler methods all reaching into `world.X`, `world.WorldTextManager`, `UIManager`, and other statics. Real handler:

```csharp
private static void Damage(World world, ref StackDataReader p) {
    if (world.Player == null) return;
    Entity entity = world.Get(p.ReadUInt32BE());
    if (entity != null) {
        ushort damage = p.ReadUInt16BE();
        if (damage > 0)
            world.WorldTextManager.AddDamage(entity, damage);
    }
}
```

**After.** Split by packet family into partial classes; introduce a `PacketContext` so dependencies appear in handler signatures.

```
Network/
├── PacketHandlers.cs            (~300 lines: parse loop + dispatch)
├── PacketHandlers.Login.cs
├── PacketHandlers.Combat.cs     ← Damage, CharacterStatus, NewHealthbarUpdate, Swing
├── PacketHandlers.Movement.cs
├── PacketHandlers.Items.cs
├── PacketHandlers.Vendor.cs
├── PacketHandlers.Party.cs
├── PacketHandlers.Chat.cs
├── PacketHandlers.Gump.cs
├── PacketHandlers.Housing.cs
├── PacketHandlers.World.cs
└── PacketHandlers.Misc.cs
```

```csharp
// Network/PacketContext.cs — explicit dependency graph
internal readonly struct PacketContext {
    public readonly World World;
    public readonly IWorldTextManager WorldText;
    public readonly IUIManager UI;
    public readonly INetClient Net;
}

// Network/PacketHandlers.Combat.cs
internal sealed partial class PacketHandlers
{
    private static void Damage(in PacketContext ctx, ref StackDataReader p) {
        if (ctx.World.Player is null) return;
        Entity entity = ctx.World.Get(p.ReadUInt32BE());
        if (entity is null) return;
        ushort damage = p.ReadUInt16BE();
        if (damage > 0)
            ctx.WorldText.AddDamage(entity, damage);   // explicit dep
    }

    private static void RegisterCombat(PacketHandlers h) {
        h.Add(0x0B, Damage);
        h.Add(0x11, CharacterStatus);
        h.Add(0x16, NewHealthbarUpdate);
        h.Add(0x17, NewHealthbarUpdate);
        h.Add(0x2C, DeathScreen);
        h.Add(0x2F, Swing);
    }
}

// Network/PacketHandlers.cs — slim core
private void Initialize() {
    RegisterLogin(this);   RegisterCombat(this);   RegisterMovement(this);
    RegisterItems(this);   RegisterVendor(this);   RegisterParty(this);
    RegisterChat(this);    RegisterGump(this);     RegisterHousing(this);
    RegisterWorld(this);   RegisterMisc(this);
}
```

**Unlocks:** handlers become unit-testable without booting the client (substitute `IWorldTextManager` with a recording fake, hand the handler a stub `PacketContext`).

**PR shape:** ~14 small PRs (one per packet family + tests), each shippable on its own.

---

### Example: Phase 2 — De-staticify global facades

Reality check from the codebase: most "Managers" are already instance fields on `World` (`world.TargetManager`, `world.Journal`); the real smell is that `World` is a service locator with concrete types, plus `UIManager` (48 statics) and `GameActions` (63 statics) which are genuinely static.

#### 2A — Instance manager → interface

```csharp
// Game/Managers/IWorldTextManager.cs (NEW)
internal interface IWorldTextManager {
    void AddDamage(uint serial, int damage);
    void Update();
    void Draw(UltimaBatcher2D b, int sx, int sy, float depth, bool isGump = false);
    void Clear();
}

// Game/Managers/WorldTextManager.cs — one line changed
internal class WorldTextManager : TextRenderer, IWorldTextManager { ... }

// Game/World.cs — return type narrowed
public IWorldTextManager WorldTextManager { get; }
```

Tests can now substitute the manager:

```csharp
private sealed class RecordingWorldText : IWorldTextManager {
    public List<(uint serial, int dmg)> Damages = new();
    public void AddDamage(uint serial, int damage) => Damages.Add((serial, damage));
    public void Update() {} public void Clear() => Damages.Clear();
    public void Draw(UltimaBatcher2D _, int __, int ___, float ____, bool _____) {}
}

[Fact]
public void Damage_PositiveValue_RecordsOverhead() {
    var text = new RecordingWorldText();
    var ctx  = TestContext.With(worldText: text);
    PacketHandlers.Damage(ctx, ref PacketBuilder.Damage(0x1234, 25).Reader);
    Assert.Equal(25, text.Damages[0].dmg);
}
```

**Per-manager PR:** add interface, implement (one line), narrow `World.X` return type, fix any compile errors that surface (those are leaks worth interfacing). Repeat ×37, parallelizable.

#### 2B — True static → instance behind a static shim

```csharp
// Game/IGameActions.cs (NEW)
internal interface IGameActions {
    void Attack(uint serial);
    void DoubleClick(uint serial);
    void OpenPaperdoll(uint serial);
    // ...
}

// Game/GameActionsImpl.cs (NEW — real instance)
internal sealed class GameActionsImpl : IGameActions {
    private readonly World _world;
    public GameActionsImpl(World world) => _world = world;
    public void Attack(uint serial) => GameActions.AttackCore(_world, serial);
    public void DoubleClick(uint serial) => GameActions.DoubleClickCore(_world, serial);
}

// Game/GameActions.cs — old static API kept as a shim, delegates to instance
internal static class GameActions {
    internal static IGameActions Instance { get; set; }   // wired by GameController

    public static void Attack(World w, uint serial) => Instance.Attack(serial);
    public static void DoubleClick(World w, uint serial) => Instance.DoubleClick(serial);

    internal static void AttackCore(World w, uint serial) { /* original body */ }
    internal static void DoubleClickCore(World w, uint serial) { /* original body */ }
}
```

```csharp
// GameController.cs — composition root wires it once
GameActions.Instance = new GameActionsImpl(_world);

// New code uses the interface
public sealed class CombatHandlers {
    private readonly IGameActions _actions;
    public CombatHandlers(IGameActions a) => _actions = a;
    public void OnAttackHotkey(uint target) => _actions.Attack(target);
}

// Old code keeps working unchanged
GameActions.Attack(world, mob.Serial);   // still compiles, still runs the same
```

**Why the shim:** zero call-site changes required to ship; plugin ABI unchanged; old code migrates opportunistically when files are touched. The shim is deleted in the *last* PR of Phase 2, not the first.

Same treatment applies to `UIManager`, `UltimaLive`, `UoAssist`.

---

### Example: Phase 3 — Scene/controller slimming

The current `GameController.Update()` does **eleven things** in 80 lines (time, mouse, network parse, plugin tick, scene update, UI update, FPS, frame pacing, cursor, audio, queued actions). The current `OnLeftMouseDown` branches on whether you're targeting/dragging/walking/clicking — each branch reaches into different managers.

#### 3A — `GameController` becomes a composition root

```csharp
// GameController.cs — slimmed to ~9 lines per loop method
protected override void Update(GameTime gameTime) {
    _frameClock.Tick(gameTime);
    foreach (var system in _updateSystems) system.Update(_frameClock);
    if (_framePacer.ShouldSuppressDraw(gameTime, IsActive)) SuppressDraw();
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime) {
    _renderTargets.EnsureSizes(...);
    foreach (var renderer in _drawSystems) renderer.Draw(_frameClock);
    base.Draw(gameTime);
}

// LoadContent — one place where the dependency graph lives
protected override void LoadContent() {
    _updateSystems = new IUpdateSystem[] {
        new InputPollSystem(),
        new NetworkPollSystem(NetClient.Socket, _packets),
        new PluginTickSystem(_pluginHost),
        new SceneUpdateSystem(this),
        new UIUpdateSystem(_ui),
        new CursorUpdateSystem(_cursor),
        new AudioUpdateSystem(_audio),
        new QueuedActionSystem(_queuedActions),
        new FpsCounterSystem(),
    };
    _drawSystems = new IDrawSystem[] {
        new SceneDrawSystem(this),
        new UIDrawSystem(_ui),
        new CursorDrawSystem(_cursor),
    };
}
```

```csharp
internal interface IUpdateSystem { void Update(in FrameClock clock); }
internal interface IDrawSystem   { void Draw(in FrameClock clock); }

internal sealed class NetworkPollSystem : IUpdateSystem {
    private readonly NetClient _socket;
    private readonly PacketHandlers _handlers;
    public NetworkPollSystem(NetClient s, PacketHandlers h) { _socket = s; _handlers = h; }

    public void Update(in FrameClock _) {
        var data = _socket.CollectAvailableData();
        var count = _handlers.ParsePackets(_socket, UO.World, data);
        _socket.Statistics.TotalPacketsReceived += (uint)count;
        _socket.Flush();
    }
}
```

**Critical insight:** this system list **is the same shape Phase 4 ECS systems take**. We're building the ECS runtime in Phase 3; storage layout comes later. Phase 4 doesn't replace Phase 3, it plugs in.

#### 3B — Modal input → per-mode handlers

```csharp
internal interface IInputMode { bool TryHandle(in PointerEvent e); }

internal sealed class TargetingInputMode : IInputMode {
    private readonly ITargetManager _targets;
    public TargetingInputMode(ITargetManager t) => _targets = t;
    public bool TryHandle(in PointerEvent e) {
        if (!_targets.IsTargeting || e.Kind != PointerKind.LeftDown) return false;
        _targets.Target(SelectedObject.Object);
        return true;
    }
}

internal sealed class DragHoldInputMode : IInputMode { /* drop held item */ }
internal sealed class DragToWalkInputMode : IInputMode { /* start walking */ }
internal sealed class DefaultClickInputMode : IInputMode { /* normal click */ }

internal sealed class InputRouter {
    private readonly IInputMode[] _modes;   // ordered: most specific first
    public InputRouter(ITargetManager t, IItemHold h, IWalkerManager w, /*…*/) {
        _modes = new IInputMode[] {
            new TargetingInputMode(t),
            new DragHoldInputMode(h),
            new DragToWalkInputMode(w),
            new DefaultClickInputMode(/*…*/),
        };
    }
    public bool Dispatch(in PointerEvent e) {
        foreach (var m in _modes) if (m.TryHandle(e)) return true;
        return false;
    }
}

// GameScene becomes a thin shell
internal override bool OnMouseDown(MouseButtonType b)
    => _input.Dispatch(new PointerEvent(PointerKind.From(b, down: true), Mouse.Position));
```

Precedence is now declared in **one** place (the array order); today it's encoded by `if`-statement order across hundreds of lines.

#### 3C — `GameSceneDrawingSorting` → renderer + sort strategy

```csharp
internal interface ISceneRenderer { void Render(in FrameClock clock, ICamera cam); }
internal interface ITileVisibilityCollector { void Collect(IWorldReader world, RenderList list); }
internal interface IIsoSorter { void Sort(RenderList list); }

internal sealed class GameSceneRenderer : ISceneRenderer {
    public void Render(in FrameClock _, ICamera cam) {
        _list.Clear();
        _collector.Collect(_world, _list);
        _sorter.Sort(_list);
        _batcher.Submit(_list);
    }
}
```

The sorter becomes a swap-in strategy — useful when the Phase 5 ECS render system iterates archetype storage instead of polymorphic `View.Draw()`.

#### 3D — `LoginScene` state machine extraction

```csharp
internal enum LoginStage { Main, ConnectionScreen, ServerSelection, CharacterSelection }

internal interface ILoginStage {
    void Enter(LoginContext ctx);
    void Update(LoginContext ctx);
    void Exit(LoginContext ctx);
}

internal sealed class LoginStateMachine {
    private readonly Dictionary<LoginStage, ILoginStage> _stages;
    private LoginStage _current;
    public void Transition(LoginStage next) {
        _stages[_current].Exit(_ctx);
        _current = next;
        _stages[next].Enter(_ctx);
    }
    public void Update() => _stages[_current].Update(_ctx);
}
```

Transitions become explicit in one place — today they're scattered across packet handlers, button callbacks, and timeout checks.

**Exit state for Phase 3:** every scene file <500 lines, `GameController` <400 lines, every per-frame concern is a named system instantiable alone, every input mode testable without SDL.
