# tinyecs:modding — Generic Modding Interface (design)

Status: **design only**. No host code changed by this doc. Builds on the working
vertical slice in `src/ClassicUO.Ecs/Modding/` (tinyecs:modding WIT + wasmtime-dotnet
fork).

Confirmed claims carry `file:line`. Anything marked *(inferred)* needs a
compile/live check before it's load-bearing.

---

## 1. Goal + principle

Anyone mods the game without touching host code. A mod is a WASM **component**
that registers ECS systems (wasvy/Bevy model). Those systems operate on the
*shared world* through four primitives — query, commands, resources, free
capability calls. The host stays **mod-unaware**: no per-gump, per-packet, or
per-feature hooks added for mods. The interface = the tinyecs:modding WIT surface + a
curated **type-path registry** (`ModComponentRegistry.cs`).

Driving acceptance test: **replace the status gump, interactive, mod-only** —
read player stats, render a custom bar, toggle stat-locks (sends a packet),
suppress the host bar. See §7.

---

## 2. What exists today (working, test- + live-verified)

| Capability | How | State |
|---|---|---|
| Register systems into a cuo Stage | `app.add-systems(schedule, systems)` | done |
| Query world (ref/mut/with/without, dynamic) | `system.add-query`, `query.iter`, `query-result.component` | done |
| Spawn/despawn/insert/remove | `commands.*`, `entity-commands.*` | done |
| Read/write any **registered component** as JSON | `component.get/set`, `entity.get` | done |
| Navigate the tree | `entity.parent/children` | done |
| Add a child (attach mod UI into a host gump) | `entity-commands.add-child` | done |
| Click feedback (host `On<UiClick>` → poll) | host tags `ModClicked`, mod queries `cuo:ui/clicked` | done |

Registry today (`ModComponentRegistry.cs:157`): `cuo:test/*`, `cuo:ui/*`
(node/text/fonts/colors/custom/interaction/name + topbar & options markers).

This already covers **"edit UI: add nodes"** and **"read/write components"**.
The gaps the user named — *change resource*, *packets in/out*, *input
behaviour*, and *remove/replace* host UI — are below.

---

## 3. The unifying model: four access patterns

Every modding need collapses to one of four patterns. Only two need new WIT.

| Pattern | Mechanism | New WIT? |
|---|---|---|
| **Read** state (stats, settings, input pos) | register component/resource → mod queries / `resource-get` | resource path: tiny. components: none |
| **Act** (spawn UI, despawn host gump, mutate, send packet) | `commands` (exists) + `net-send` free fn | `net-send`: small |
| **React** to events (incoming packet, input edge, click) | host turns each event into a **one-frame poll-entity** with registered components; mod queries it, cleared Stage.Last (same idiom as `ModClicked`) | **none** |
| **Intercept / override** (drop a packet, consume a keypress, replace host behaviour) | synchronous: a guest export the host calls *at the choke point* (packets) or a free `consume` fn + early stage (input) | packets: 1 export+verdict. input: 1 free fn |

The poll-entity idiom is the laziest win: "react to anything" needs **zero WIT
change** — only host emitter systems + registry entries. It reuses the query +
component path already proven. Cost: one-frame lag (fine for observe; useless
for intercept — see §5/§6).

---

## 4. Capability designs

### A. Edit UI — add / remove / replace

- **Add**: done. `commands.spawn(bundle)` of `cuo:ui/node` + text/custom/etc,
  `add-child` on a host container found by marker/`UiName`. Robust.
- **Remove host nodes**: `commands.entity(id).despawn()`. **Robust only on
  static gumps** (host doesn't rebuild them). The status bar qualifies — it's
  spawned once and only *mutated in place* by its `Refresh` system
  (`StatusBarPlugin.cs:261`), so a despawned bar **stays gone** *(inferred from
  Refresh-only-mutates; confirm live)*. Destructive-rebuild gumps (options rows)
  fight removal — documented limitation, same as browser-extension fragility.
- **Replace**: = remove host + add mod's own. No new primitive.
- **Make a mod window behave like a gump** (drag / right-click-close / z-stack):
  register `cuo:ui/movable` → `UIMovable`. `WindowDragPlugin` is generic over
  that tag (CLAUDE.md "UO Gump Behaviour Contract") — the mod window gets
  drag+close+stacking free, no host edit.

New registry entries (no WIT change): `cuo:ui/movable` → `UIMovable`.

### B. Change resource (singletons)

Per-entity state = components (done). Singletons (`Profile`, settings,
`MouseContext`, game context) are `Res<T>`. Expose generically:

```wit
// new free functions on `interface app`
resource-get: func(resource: type-path) -> serialized-component;   // "null" if absent
resource-set: func(resource: type-path, value: serialized-component);
```

Host side: an `IModResource` registry mirroring `IModComponent` — a closed
generic `ModResource<T>` over `Res<T>`/`ResMut<T>` + STJ source-gen (AOT-safe,
no reflection). Free functions (not a handle) = laziest; the `GuestBridge`
import impl already holds `ctx` (World + registries), so it can resolve+(de)serialize.

Register what mods should reach: e.g. `cuo:profile/profile` → `Profile`,
`cuo:input/mouse` → `MouseContext` (read), settings as needed. Whitelist only —
never blanket-expose.

### C. Packets — OUT (send)

Generic primitive, mod owns the bytes:

```wit
// free function on `interface app`
net-send: func(packet: list<u8>);   // fully-framed packet incl. length field
```

Host side: `GuestBridge` → `Res<NetClient>.Send(bytes)`. The mod writes a
complete packet (id + variable-length length field at offset 1–2 if applicable),
exactly as `OutgoingPackets.cs` does. The host does **not** reframe — it's a raw
send, which is what "packet output, generic" means.

> *(inferred)* a top-level `list<u8>` param lowers cleanly through the fork
> source-gen. The known fork bug is a `list` nested in a `tuple` in a `list`
> (the `bundle` shape) — a flat `list<u8>` should take the working path (the
> smoke test handled `list<borrow>`/`list<variant>`). **Confirm by compiling.**
> Fallback if it mislowers: `net-send: func(packet: string)` with base64, matching
> the existing JSON-string workaround.

Convenience helpers (`send-status-request`, `send-stat-lock`, …) are optional
sugar over `net-send`; skip them — a mod can build any packet. Add only if a
packet's framing is error-prone enough to be worth a host-validated helper.

### D. Packets — IN (observe)  +  intercept

**Observe (poll, no WIT change).** At the single dispatch choke point
`NetworkPlugin.PacketReader` (`NetworkPlugin.cs:348`; id at :366, raw span at
:403–407, dispatch `TryDispatch`+`EmitTrigger(PacketReceived<T>)` at :434/:63),
add **one** host system/site that spawns a one-frame entity:

```
ModIncomingPacket { ushort Id; string Payload; }   // Payload = base64 of the span
```

tagged `cuo:net/incoming`. Mods query `[ref cuo:net/incoming]`, match `Id`,
decode. Cleared Stage.Last (like `ModClicked`). Register both in the registry.
Mods react to **any** packet with zero new WIT.

**Intercept / drop / rewrite (synchronous — needs a guest export).** Poll is
one frame late: the packet is already dispatched. To veto, the host must call
the mod *during* `PacketReader`, before `TryDispatch` — the same choke point the
observe tap already writes from. WIT:

```wit
variant packet-verdict { pass, drop, replace(list<u8>) }
// guest world `guest` gains an OPTIONAL export:
export on-packet-in: func(id: u8, packet: list<u8>) -> packet-verdict;
```

Host calls `on-packet-in` at the choke point if the mod exports it; acts on the
verdict before dispatch. Heaviest piece. **Recommend observe-first; ship
intercept only when a mod needs it.**

### E. Input behaviour

**Read state (no WIT change):** register `cuo:input/mouse` → `MouseContext`,
`cuo:input/keyboard` → `KeyboardContext` as resources (§B). Mod `resource-get`s
position / button / pressed-keys. Updated Stage.First (`FnaPlugin.cs:75`).

**React to input edges (no WIT change):** poll-entity idiom — host spawns
one-frame `ModInputEvent { byte Kind; int Key; float X; float Y; }`
(`cuo:input/event`) for key/mouse edges; mod queries, cleared Stage.Last.

**Override / consume (light — fits the system model):** input is *polled*, not
synchronously dispatched, so a mod system running **before** host gameplay reads
(Stage.PreUpdate) can consume within the same frame:

```wit
// free function on `interface app`
input-consume-mouse: func(button: u8);
input-consume-key: func(key: u32);
```

Host side: mouse already has `MouseContext.Consume(button)` / `IsConsumed`
(precedent) — wire the free fn to it. Keyboard needs a matching consume set
added to `KeyboardContext` (small host addition) + host gameplay checks it.
No new guest export needed (unlike packet intercept) because the frame ordering
gives the mod its veto window.

---

## 5. New WIT delta (the whole addition)

```wit
interface app {
    // ... everything that exists today ...

    // C: packet out
    net-send: func(packet: list<u8>);

    // B: singleton resources
    resource-get: func(resource: type-path) -> serialized-component;
    resource-set: func(resource: type-path, value: serialized-component);

    // E: input override (consume within-frame)
    input-consume-mouse: func(button: u8);
    input-consume-key: func(key: u32);

    // D: packet intercept (only if/when needed)
    variant packet-verdict { pass, drop, replace(list<u8>) }
}

world guest {
    // ... existing setup export ...
    // D: optional — host calls it synchronously at the packet choke point
    export on-packet-in: func(id: u8, packet: list<u8>) -> packet-verdict;
}
```

Everything else (observe packets, observe input, read stats/settings, edit UI,
replace gumps) is **registry + host emitter systems** — no WIT change.

Host work behind it: `IModResource` registry (+ STJ entries); `net-send`/
`input-consume*` bridge impls in `GuestBridge`; emitter systems at the packet
and input choke points; keyboard-consume field; new registry component/resource
entries. All AOT-safe (closed generics, source-gen JSON) — consistent with the
existing bridge.

---

## 6. Risks / constraints

- **Fork source-gen bug** still open (tuple-with-list-element). New `list<u8>`
  params are *probably* fine but unproven — §C fallback is base64 string.
- **AOT publish** (`build-naot.sh`) of the modding path still unverified
  (per memory). New free fns + resource registry don't change that risk profile.
- **Intercept is invasive.** `on-packet-in` runs the guest synchronously inside
  the network read — a slow/looping mod stalls packet processing. Needs a
  time/error guard. Observe path has no such risk.
- **Remove/replace fragility**: robust on static gumps (status bar), fragile on
  destructive-rebuild gumps (options rows). Inherent to mod-unaware host.
- **Teardown**: no mod-unload path yet; mod entities are tagged `ModEntity` but
  not reaped. Pre-existing gap, unchanged here.
- **One-frame lag** on every poll-based react path. Fine for display/observe,
  unusable for veto (hence the synchronous export for packet intercept).

---

## 7. Worked example: interactive status-gump replacement

Proves *replace a host gump, mod-only, interactive*. Uses **read + act +
react**, plus **net-send** for stat-locks. No host gump file edited.

**Registry additions (host, no gump edits — these components already exist):**
```
cuo:player/player      -> Player           (marker; Components.cs:78)
cuo:player/hits        -> Hits             (Components.cs:63)
cuo:player/mana        -> Mana             (Components.cs:68)
cuo:player/stamina     -> Stamina          (Components.cs:73)
cuo:player/data        -> PlayerData       (str/dex/int…; Components.cs:103)
cuo:player/stat-locks  -> StatLocks        (Components.cs:98)
cuo:ui/statusbar-window-> StatusBarWindow  (find+despawn host bar; StatusBarPlugin.cs:23)
cuo:ui/movable         -> UIMovable        (drag/close/z for the mod window)
```
(STJ: add each struct to `ModJsonContext`. `Hits/Mana/Stamina/PlayerData/StatLocks`
are field/prop structs — same `IncludeFields` path already used.)

**Mod systems (Rust/JS guest):**
1. `setup` — register the systems below.
2. `mod-startup` — spawn the mod status window: a `cuo:ui/movable` root +
   `cuo:ui/custom` background + label `cuo:ui/text` children, named via
   `cuo:ui/name` for stable lookup. Three stat-lock buttons (`cuo:ui/node` +
   `cuo:ui/custom` + `cuo:ui/interaction`), named `mod.status.lock.str` etc.
3. `mod-suppress-host` (Update) — query `[with cuo:ui/statusbar-window]` →
   `commands.entity(id).despawn()`. Host bar dies same frame, stays gone (§4A).
4. `mod-refresh` (Update) — query `[with cuo:player/player, ref cuo:player/hits,
   ref cuo:player/mana, ref cuo:player/stamina, ref cuo:player/data,
   ref cuo:player/stat-locks]`; format values; `component.set` each label's
   `cuo:ui/text`. Live values for free — packet handlers write the components in
   place (`OnUpdateHits` etc., `InGamePacketsPlugin.cs:1462`).
5. `mod-locks` (Update) — query `[with cuo:ui/clicked, ref cuo:ui/name]`; if a
   lock button was clicked, compute next lock state, **`net-send`** a 0xBF /
   subcmd 0x1A stat-lock packet (bytes per `OutgoingPackets.cs:1273`), update the
   lock icon. This is the one piece needing the new `net-send` WIT.

**Coverage:** read (stats via components) ✓, act (despawn host + spawn mod UI +
net-send) ✓, react (click poll) ✓. Resource/input patterns demoed separately.
Stat values flow without any packet-IN WIT because the host already lands them on
components — the mod just re-reads. Only `net-send` is genuinely new for this
example.

---

## 8. Recommended phasing

1. **Status-bar slice** (this doc's example): registry additions + `net-send`
   WIT + the mod. Smallest path to "replace a gump, interactive, mod-only".
   Verifies `list<u8>` param + the despawn-stays-gone claim live.
2. **Resources** (`resource-get/set` + `IModResource`): unlocks settings/Profile
   read-write and input-state read.
3. **Observe packets + input** (poll-entities, no WIT): mods react to any
   packet / input edge.
4. **Override**: `input-consume-*` (cheap) then `on-packet-in` intercept
   (invasive — only on demand).

Each phase is independently shippable and testable (round-trip test + harness),
matching how the existing slice was built.

---

## 9. Implementation status (ALL FOUR PHASES SHIPPED)

Built + verified: **378/378 tests** (was 372; +6 new), cuo-ecs builds 0 errors.
Phase 1 is real-wasm round-trip (`ecs_status.wasm`); phases 2–4 use real
App/World/registry/bridge (the wasm string/scalar marshalling they rely on is the
same path phase 1 + the prior slice prove). Tests in
`tests/ClassicUO.Ecs.Tests/EcsModdingRoundTripTests.cs`.

| Capability | WIT (on `commands`) | Host | Test |
|---|---|---|---|
| Packet out | `net-send(list<u8>)` | `GuestBridge.NetSend`→`NetClient.Send` | status mod lock click |
| Resource get/set | `resource-get/set(path[,value])` | `IModResource`/`ModResource<T>`/`ModGameContext`; `cuo:engine/time`, `cuo:game/context` | `Resource_get_and_set…` |
| Read input | `resource-get` | `ModMouseInput`/`ModKeyboardInput` DTOs; `cuo:input/mouse`, `cuo:input/keyboard` | `Mouse_input_resource…` |
| Observe packets | none (poll-entity) | `ModNetTap` (neutral) ← `PacketReader`; drained → `cuo:net/incoming` one-frame entities | `Mod_observes_incoming_packets…` |
| Intercept (suppress) | `block-packet/unblock-packet(id)` | `ModNetTap.Blocked` honored in `PacketReader` | `Packet_block_toggles…` |
| Input override | `input-consume-mouse(button)` | `MouseContext.Consume` (host-honored) | `Mouse_consume_overrides…` |
| Replace a gump | (composed) | despawn `cuo:ui/statusbar-window` + build own (`cuo:ui/movable`…) | `Mod_replaces_status_bar…` |

### Deviations from §5 (deliberate, lazier/safer)

- **Methods on the `commands` resource, NOT free interface functions.** Free
  interface functions are unproven in the fork source-gen (only resources are);
  resource methods are proven. Same ergonomics, zero codegen risk.
- **Packet intercept = block-by-id (suppress)**, not the synchronous
  `on-packet-in -> packet-verdict` export. Suppress covers the common case (the
  doc itself recommended observe-first / heavy export only on demand). Body
  rewrite/replace via a synchronous guest export remains the documented follow-up.
- **Input read via resource DTOs**, input *edge* events NOT surfaced as
  poll-entities — a mod diffs the state itself (YAGNI). Packet observe DID get the
  poll-entity treatment because raw bytes have no other channel.
- **Keyboard consume deferred.** `MouseContext` has a host-honored consume API;
  `KeyboardContext` does not — wiring it needs a `TinyEcs.Bevy.Input` library
  change + honor points across gameplay. Mouse consume shipped; keyboard noted.
- **`resource` is a WIT keyword** → the param is named `path`.

### Live-verified (cuo-ecs AGENT_BUILD, real ModernUO)

- Status-gump replacement runs in the full game: mod panel renders the live
  player stats, and **clicking a stat-lock button cycles the lock + sends
  0xBF/0x1A** (3 clicks → 3 toggles, no errors). Two fixes were needed for the
  live click (unit tests passed without them because they inject `UiClick` in
  Stage.First, dodging both):
  1. Interactive children of a movable mod window need **`cuo:ui/no-window-drag`**
     (host `WindowDragPlugin` opt-out, scanned by bounds) — not
     `cuo:ui/movable-no-drag` (that's whole-window nomove). Else a press latches
     the panel drag instead of firing `UiClick`.
  2. The click bridge cleared `ModClicked` in Stage.Last, but `UiClick` fires in
     `UiPostLayoutStage` (after Update) — so Last stripped the tag the same frame,
     before the mod's next Update poll. Moved the clear to **Stage.Update, after
     the mod runner** (read-then-clear). This also fixes the topbar mod's live
     click.

### Not verified (follow-ups)

- AOT publish (`build-naot.sh`) of the modding path.
- The new WIT methods aren't mirrored into every guest dep `.wit` yet (only the
  host canonical + `ecs-status`'s `net-send`); a guest binds only what it imports,
  so unused additions don't break existing mods.
