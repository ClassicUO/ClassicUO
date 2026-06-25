# ClassicUO ECS — Modding Surface

What an out-of-process WASM mod (`tinyecs:modding` + `cuo:modding` WIT) can reach,
what it still can't, and the checklist to make the whole game moddable.

## Principle

Mods operate at the **ECS data boundary**: registered components, resources, events
+ a few host capabilities. They do **not** patch host **system logic** (packet
parsers, layout engine, movement/combat resolution, render pipeline) — that is
compiled host code, invisible to mods. Moddability of any subsystem = exactly what
the registry exposes as data. Widen `src/ClassicUO.Ecs/Modding/CuoModdingRegistry.cs`
(+ `ModJsonContext`) and you widen what mods can do.

**Observers are not registered separately** — a mod observes any registered
component's `insert`/`remove` or any registered event by name (`add_observer`).
So "what observers exist" falls out of the registered components + events.

Registration kinds:
- **Component** — `reg.Register("cuo:…", new ModComponent<T>(ctx))` + `[JsonSerializable(typeof(T))]`. Markers (zero-size tags) and data both. Private nested types must be promoted to namespace-level `internal`.
- **Resource** — `reg.RegisterResource("cuo:…", …)`. Non-serializable fields (Socket, Dictionary, HashSet, GPU buffers) need a hand-mapped DTO + `IModResource`.
- **Event** — `reg.RegisterEvent("cuo:…", new ModEvent<T>(ctx))`, `T : struct`. Host↔mod: mod `emit-event` → real `EmitTrigger<T>`; mod `add_observer(Custom("cuo:…"))` ← host `On<T>`.
- **Capability** — host import in `cuo-modding.wit` (`net`, `ui`, input-consume) or the generic `commands` surface.

## Baseline (already exposed)

Components: `Node Text TextFont TextColor BackgroundColor UiCustom Interaction UiName`,
`OptionsWindow ScrollPosition`, topbar (`TopBarFull IsTopBar TopBarDragHandle TopBarButton`),
`ModClicked ModHovered`, player (`Player Hits Mana Stamina PlayerData StatLocks`),
`StatusBarWindow UiTooltip UOButton StatLockButton`,
window infra (`UiMovable UiMovableNoDrag UiNoWindowDrag UiContainsByBounds GlobalZIndex`).
Resources: `Time GameContext(DTO) Mouse Keyboard`.
Capabilities: `net.send`, `ui.gump-size/measure-text/resolve-cliloc`, `input-consume-mouse`,
`emit-event`, `resource get/set`, scene-tree walk (`parent/children/get`).

## Incoming-packet filter (replaces `cuo:net/incoming` + block-by-id)

**Removed:** the `cuo:net/incoming` event, `ModIncomingPacket`, and the
`block-packet` / `unblock-packet` net imports.

**New:** an **opt-in guest export** a mod declares in its own `world.wit`:

```wit
/// Host calls this synchronously per incoming packet, BEFORE host dispatch.
/// return true  -> BLOCK (host handlers skip this packet)
/// return false -> CONTINUE (host handlers run normally)
export on-incoming-packet: func(id: u8, data: list<u8>) -> bool;
```

- `data` is the full framed packet (id + length + body), as the wire delivered it.
- Pure decision — **no `commands` param**, so it is safe to run mid-packet-read. A mod
  that wants to act stores state guest-side and reacts in its own `Update` system
  (the netlog pattern).
- Host wiring: `NetworkPlugin.PacketReader` (`SingleThreaded`) calls
  `ModNetTap.Filter`, a neutral delegate the modding layer installs. The lib
  (`ModRuntimes.AnyReturnsTrue`) probes each enabled mod once for the export (miss
  cached — no per-packet exceptions), builds `(id, list<u8>)`, calls, ORs the bools.
- Zero cost when no loaded mod exports it.

## Implemented status

Compile-verified (host builds 0 errors); **runtime not yet exercised** by a mod.

- **Tier 0** — `cuo:game/state` (read + `QueueState` drive); text infra `cuo:ui/{text-input,editable-text,masked-text,focused-input}`; WIT round-trip `entity.id()` + `commands.entity-by-id`; `cuo:net.resolve-serial`; `commands.input-consume-keyboard` (+ `KeyboardContext.Consume` added). A mod focuses its own field by reading its glyph id (`entity.id()`) and setting `focused-input` on `ModClicked`.
- **Tier 1** — 6 scene markers `cuo:scene/{login,server-selection,character-selection,character-creation,login-error,game}` + events `cuo:scene/login-request`, `cuo:scene/server-list`, `cuo:scene/character-list` (DTO), `cuo:scene/login-error-info`.
- **Tier 2** — 27 gump markers `cuo:gump/*` (map/vendor/bulletin/book presence-only via `ModPresence`) + `cuo:gump/container-{opened,closed}` events.
- **Tier 3** — 18 components `cuo:ent/*` (serial/graphic/hue/world-position/facing/name/notoriety/amount/slot-position/contained-into/is-container/is-mobile/is-item/is-multi/server-flags/animation; + `equipment`/`mob-steps` via InlineArray→DTO).
- **Tier 4** — `cuo:target/state`, `cuo:player/grabbed-item`, `cuo:player/steps` (InlineArray→DTO).

**DTOs done:** `cuo:ent/equipment` (EquipmentSlots → item serials by layer), `cuo:ent/mob-steps` (MobileSteps), `cuo:player/steps` (PlayerStepsContext) — hand-mapped read-only DTOs (mods change these by sending packets). Map/Vendor/Bulletin/Book gumps registered presence-only via `ModPresence`.

**Scene info events (done):** `cuo:scene/server-list`, `cuo:scene/character-list` (DTO — flattens TownInfo's ValueTuple), `cuo:scene/login-error-info`. The host fires these via EventWriter; the ModdingPlugin re-emits them as triggers (3 forwarder systems, separate EventReader cursor) so a mod's On<T> observer receives them — a mod can now both **populate and drive** a replacement server/character-select scene.

**Only remaining (optional):** `OnSelectCharacter`/`OnCreateCharacter` triggers — a host refactor of inline `net.Send_*` in the char-select/create plugins. Mods can already select/create via `net.send`, so this is ergonomics, not a capability gap.

A reusable `ModPresence<T>` (lib) exposes a component for query/despawn without serializing its struct (`get()` → `"{}"`) — for markers holding collections/refs, or zero-size tags (`World.Get` on a tag would panic).

## Tiers — checklist

`[ ]` = todo, `[x]` = done. Type names/`file:line` for gumps/world/resources are
agent-surveyed — **confirm each at registration time**.

### Tier 0 — cross-cutting plumbing (unlocks the rest)
- [ ] `GameState` + `NextState<GameState>` (read + drive scene transitions) — `Boot.cs:131`
- [ ] text-field input infra: `TextInput`, `MaskedText`, `EditableText` + `FocusedInput` resource (GuiPlugin)
- [ ] serial→entity lookup capability (`NetworkEntitiesMap`, `InGamePacketsPlugin.cs:46`) — likely a new `cuo:` import (entity-handle return)
- [ ] `input-consume-keyboard` capability (mirror `input-consume-mouse`)
- blocked/limit: typed component **read** (fork variant-return crash) — JSON `get` only

### Tier 1 — scenes (6 markers + drive events)
Promote private scene tags → namespace-level `internal`, then register:
- [ ] `LoginScene` `LoginScreenPlugin.cs:433`
- [ ] `ServerSelectionScene` `ServerSelectionPlugin.cs:286`
- [ ] `CharacterSelectionScene` `CharacterSelectionPlugin.cs:370`
- [ ] `CharCreationScene` `CharacterCreationPlugin.cs`
- [ ] `LoginErrorScene` `LoginErrorScreenPlugin.cs:148`
- [ ] `GameScene` `GameScreenPlugin.cs:663`
- [ ] events: `OnLoginRequest` `NetworkPlugin.cs:16`, `ServerSelectionInfoEvent`, `CharacterSelectionInfoEvent`, `LoginErrorsInfoEvent`
- [ ] add triggers: `OnSelectCharacter`, `OnCreateCharacter` (today direct `net.Send_*`)

Note: mod scene UI must spawn from an **Update** system (mods have no `OnEnter` hook);
host scenes spawn in `OnEnter`, so the mod despawns the marker subtree + builds its own.

### Tier 2 — gump window markers (~28) + lifecycle events
Register each window root marker + sub-part markers already cover most (`UOButton` etc):
- [ ] `PaperdollWindow ContainerWindow BuffGumpUI JournalWindow SkillsWindow SpellbookWindow`
- [ ] `BookWindow CombatBookWindow RacialBookWindow HealthBarWindow MiniMapWindow WorldMapWindow`
- [ ] `MenuGumpWindow MessageBoxWindow TipNoticeWindow LogoutGumpWindow`
- [ ] `PartyManifestWindow PartyInviteWindow ProfileWindow TradeWindow BulletinBoardWindow`
- [ ] `VendorWindow MapWindow GridContainerWindow GridLootWindow SplitMenuWindow HouseDesignWindow`
- [ ] events: `ContainerOpenedEvent`, `ContainerClosedEvent` (ContainerGumpPlugin)

### Tier 3 — world entity components (~14)
- [ ] `NetworkSerial Graphic Hue WorldPosition Facing` (`Components.cs`)
- [ ] `EntityName Notoriety Amount ContainerSlotPosition ContainedInto IsContainer`
- [ ] `EquipmentSlots` (`Components.cs:144`)
- [ ] `MobileSteps MobAnimation ServerFlags` (`MobAnimationsPlugin.cs`)
- [ ] tags `Mobiles Items IsMulti`

### Tier 4 — action state resources (read)
- [ ] `TargetingState` `TargetingPlugin.cs:522`
- [ ] `GrabbedItem` `PickupPlugin.cs`
- [ ] `ServerFlags` (war mode)
- [ ] `EquipmentSlots`, `PlayerStepsContext`
- most actions already reachable via `net.send`; this is for **reading** action state.

## Excluded (per request)
- DTOs for `NetClient`, `SelectedEntity`, `ObjectPropertyLists` — deferred.
- Tier 5 typed packet events (Damage/Death/Buff/Weather…) — **not** doing; the
  bool incoming filter covers observe + block in one mechanism.

## Known limits
- Typed component **read** blocked (fork variant-return crash) — read via JSON `get`.
- Reload ceiling: TinyEcs has no global-observer removal; observers a mod registers
  only on reload aren't wired (fork fn-table cap bounds loaded mods, not reloads).
- Each non-serializable resource needs a DTO (the `GameContextDto` pattern).

## Harness note
Do **not** copy all mods into `ecs-mods/` during agent-harness sessions (noise) —
copy only the mod under test.
