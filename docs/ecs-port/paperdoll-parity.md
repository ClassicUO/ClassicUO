# ECS Paperdoll Parity — Job Spec

**Status:** in progress.
**Branch:** `impl/ecs`.
**Last commit on this work:** see `git log --oneline -5`. The most
recent paperdoll commits are listed under "Already shipped" below.

## Goal

Bring `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs` (and the
adjacent UI it relies on) to feature parity with the legacy
`src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs` so the player
paperdoll on the ECS branch looks and behaves like the legacy
client's paperdoll on `main`.

This is one tile in a larger ECS UI port; the same plumbing (Bevy.UI
+ Clay + ECS systems) is being used for all gumps. Paperdoll is the
first one we're pushing past the bare-mechanism MVP into full
parity.

## Where things stand

### Already shipped on `impl/ecs`

- **Packet:** `OnOpenPaperdollPacket_0x88` is parsed and handled by
  `PaperdollPlugin.ProcessPaperdollPackets`. Window entity is
  spawned with `IsPaperdoll`, `PaperdollTarget { Serial }`,
  `FloatingWindowState`, `UIMovable`, `ZIndex(100)`, `UiCustom`,
  and `UOCustomRender { Gump, 0x07D0 or 0x07D1, Vector3.UnitZ }`.
- **Equipment overlays:** layered children rendered in main's
  `_layerOrder`. `MALE_GUMP_OFFSET / FEMALE_GUMP_OFFSET + AnimID`
  with cross-sex fallback when one variant is missing. Each child
  is `PaperdollEquipChild { WindowEntity, ItemSerial }` and has
  `Interaction.None` + a `UiPointerDown` observer that calls
  `Send_PickUpRequest(itemSerial, 1)`.
- **Live equip refresh:** `RefreshEquipmentOverlays` runs on
  `Changed<EquipmentSlots>` for any open paperdoll. Despawns the
  old `PaperdollEquipChild` set, respawns from current slots.
- **Title text** at `(39, 262)` inside the panel.
- **Stat text dump** at `(40, 220)` inside the panel — single
  `PaperdollStatText { WindowEntity }` entity whose `Text.Value`
  is rewritten every frame from `PlayerData + Hits + Mana + Stamina`
  by `RefreshStatPanel`.
- **Buttons (gump pics):** virtue (`0x0071`, 80,4 — player only),
  profile (`0x07D2`, 25,196), party-manifest (`0x07D2`, 39,196 —
  player only, stub click handler). Each spawned via
  `SpawnButton` helper with `ZIndex(102) + UiCustom +
  Interaction.None`.
- **Right-click close:** `ClosePaperdollOnRightClick` picks the
  topmost `IsPaperdoll` under the cursor by `ComputedNode.ClayId`
  on `IsPressedOnce(Right)` and despawns the window plus its
  `PaperdollEquipChild` and `PaperdollStatText` children.
- **Drop-to-equip:** the window itself observes `UiPointerDown`;
  when `GrabbedItem.Serial != 0`, fires
  `Send_EquipRequest(serial, ItemData.Layer, targetSerial)` and
  clears `GrabbedItem`.
- **`PlayerData.IsFemale`:** field added to `Components.cs`,
  populated from packet 0x11 in `HandleCharacterStatus`.
- **6 jewelry-slot frames** on the left edge of the panel (gump
  `0x2344`, 19x20, at X=2 Y=75+21*i for Helmet/Earrings/Necklace/
  Ring/Bracelet/Tunic). When a slot is filled, an 18x18
  `UOCustomKind.Art` child renders the equipped item's art at the
  slot origin. Each slot is `PaperdollJewelrySlot { WindowEntity,
  Layer, ItemSerial }`; left-click fires
  `Send_PickUpRequest(serial, 1)`. `RefreshEquipmentOverlays`
  despawns + respawns the slots alongside the body overlays so the
  slot icons stay in sync with `EquipmentSlots`.

### Visual baseline captures

`.runtime/compare/cmp__main__pd.png` (main, full parity) vs
`.runtime/compare/cmp__ecs__pd.png` (impl/ecs current). At a
glance: main wraps the paperdoll in a much richer UI shell
(top toolbar, right-side action button column, attached world
view); the ECS version shows just the bare gump + robe overlay +
profile pic + stat text.

## What's still missing vs main

Audited against `src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs`
on `main`. Grouped by effort.

### Small (1-2h each)

- **Help icon (?)** at top-right of panel. Just a `GumpPic`
  with a `UiPointerDown` observer that opens help. Help can
  stub-log on impl/ecs.

- **Combat book button** (`PaperDollGump.cs` searches for
  `_picVirtueMenu` block). One more `SpawnButton` call with
  the combat-book asset id; click sends the same gump-response
  pattern as virtue.

- **Logout button** (right-side button stack on main). Sends
  `Send_LogoutNotification` and returns to login screen.

- **Stat block layout per-field** instead of one text dump.
  Six `Text` entities (STR / DEX / INT / Followers / Karma /
  Fame) at fixed positions matching the panel art. Keep the
  same `PaperdollStatText` marker pattern — just spawn one
  per field with a `Field` enum and have `RefreshStatPanel`
  switch on it.

### Medium (half-day each)

- **HP / Mana / Stam visual bars.** Three `UOCustomRender` of
  `UOCustomKind.GumpNinePatch` with width scaled to
  `value / max`. Stat IDs and positions come from the legacy
  gump (search PaperdollGump for `0x0806 / 0x0807 / 0x0808`
  or similar). Same per-frame refresh pattern as stat text.

- **Resistances / Damage / Defense block.** All values already
  on `PlayerData`. Render as 4-row text column on the right
  side of the panel.

- **Right-click context menu on equipped items.** ECS has no
  context-menu system yet, so this includes building one.
  Minimum viable: spawn a small floating Node with two text
  children ("Use", "Drop") on right-click of an equipped item;
  the buttons fire `Send_DoubleClick(serial)` and
  `Send_PickUpRequest(serial, 1)` respectively. Despawn on
  click outside.

- **Tooltip on hover.** Listen for `OnInsert<Interaction>`
  with `Interaction.Hovered` on equipped items. Send
  `Send_MegaClilocRequest(serial)` to get item data; render
  a small text bubble near the cursor. Requires a tooltip
  system that other gumps will reuse — worth scoping as its
  own component.

- **Real `PartyGump`.** Stub handler currently just logs.
  Port `PartyGump.cs` from main — separate sub-task; could
  defer.

### Large (1-2 days each)

- **Top toolbar / menu bar.** Main's screenshot shows
  `File / Character / Mobility / Journal / Chat / World Map /
  Info / Tools / Counterbar / RM Skills / Detect Stat` at
  the top of the window. This is `TopBarGump.cs` on main —
  ~400 lines plus a clickable label sub-control. Probably a
  separate plugin (`TopBarPlugin.cs`) and a separate parity
  job from paperdoll proper.

- **EquipConversions dictionary lookup.** Main reads
  `Animations.EquipConversions[mobileGraphic][animID]` to
  remap paperdoll graphics for unusual races / variants. On
  ECS, no equiv data structure yet. Port the dictionary load
  from `AnimationsLoader` (already present) and hook
  `SpawnEquipmentOverlays` to consult it before the
  `MALE/FEMALE_GUMP_OFFSET + AnimID` fallback.

- **TileArt appearance lookup.** `TileArt.uop` is parsed by
  `TileArtLoader.cs` on main. ECS doesn't load it yet (or
  loads but doesn't expose `TryGetTileArtInfo`). Plug into
  the equip-graphic resolution after EquipConversions.

- **Quiver-fix `_layerOrder` variant.** Main switches to
  `_layerOrder_quiver_fix` when a quiver layer is present.
  Reorder logic + condition. Small once EquipConversions is
  in.

- **Mount layer (Layer.Mount = 0x19) rendering.** Player
  on horseback shows the mount overlay. Mount has its own
  gump derived from the mount's body graphic. Touches
  `MobAnimationsPlugin.Mounts.FixMountGraphic`.

- **Dead / ghost / corpse paperdoll.** Mobile.IsDead branch
  in main shows `Layer.Robe = corpse robe gump`. Track
  alive state on the target mobile (no component yet).

- **Drag visual feedback during pickup.** Currently the
  equip is despawned on `RefreshEquipmentOverlays` when the
  server sends the unequip packet. Main shows the item
  *floating with the cursor* between left-mouse-down and the
  next click. Needs a `GrabbedItem` overlay system (also
  missing on the world view — same overlay reused).

- **Buff/debuff icon strip.** Below the stats panel on
  main. Reads from buff packets (0xDF) that already exist
  in ECS but go unhandled — see `Unhandled packet 0xDF`
  lines in `/tmp/*-cuo*.log`.

### Code-quality TODOs picked up along the way

- `RefreshStatPanel` runs every frame regardless of whether
  PlayerData / Hits / Mana / Stam changed. Cheap but
  wasteful — add `Changed<>` filters once `RefreshStatPanel`
  is split into per-field systems.
- Per-frame `StringBuilder` allocation in `RefreshStatPanel`.
  Pre-format into a `Span<char>` buffer once per-field
  systems exist.
- `PaperdollEquipChild` only tracks `WindowEntity` for
  cleanup. When right-click context menu lands, also store
  `Layer` so the menu can show layer-specific actions.

## How to resume tomorrow

```bash
cd /c/dev/cuo/cuo-agents
git checkout impl/ecs
git log --oneline -5     # confirm 0fb049d12 is tip or close to it
```

1. Pick the next item from the "Small" list (jewelry-slot
   frames is the most visually impactful single change).
2. Read `src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs`
   lines 260-280 + `Game/UI/Controls/EquipmentSlot.cs` to
   confirm the geometry and click behaviour.
3. Add the new spawn logic inside `ProcessPaperdollPackets`
   in `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs`
   right after the existing equipment overlay loop. Use the
   `SpawnButton` helper pattern — but the slot is a
   `UOCustomKind.Art` (not `Gump`) since main's
   `EquipmentSlot` renders the item's art graphic.
4. The agent harness is already wired: build with
   `dotnet build src/ClassicUO.Client/ClassicUO.Client.csproj
   -p:AGENT_BUILD=true`, run `./bin/agent/net10.0/cuo.agent.exe`,
   drive via `tools/agent-desktop/bin/Debug/net9.0/agent-desktop.dll`
   (login + walk-into-world + double-click character is a
   single shell loop already proven against ModernUO at
   `127.0.0.1:2593` with `admin / admin`).
5. Capture diffs against
   `.runtime/compare/cmp__main__pd.png` (legacy reference)
   to validate each step.

### Shard

ModernUO at `C:/dev/ModernUO/Distribution/ModernUO.exe`. Run it
first; verify with `netstat -ano | grep :2593`.

Account: `admin` / `admin`. Character: `KARASHO` (Grandmaster
Alchemist, robe). Start position roughly (5443, 1151, 0)
Felucca / Green Acres.

### Build flavor

`-p:AGENT_BUILD=true` produces `bin/agent/net10.0/cuo.agent.exe`
with the JSON-RPC harness baked in. Without the flag, the
client builds clean but the harness is stripped (no RPC
control, no synth input).

### Scenario for verification

`tools/agent-desktop/scenarios/login-paperdoll-walk.json` is
the JSON script that drives login → server-select →
character-select → double-click → walk-west. Coordinates inside
are calibrated for 640x480 backbuffer and the ModernUO shard
above.

## Files touched on this work

- `src/ClassicUO.Client/Ecs/Components.cs` — `PlayerData.IsFemale`.
- `src/ClassicUO.Client/Ecs/Network/InGamePacketsPlugin.cs` —
  `HandleCharacterStatus` populates `PlayerData`.
- `src/ClassicUO.Client/Ecs/Gameplay/GameplayPlugin.cs` —
  `AddPlugin<PaperdollPlugin>()`.
- `src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs` — all
  paperdoll logic (the whole file is from this work).
- `tools/agent-desktop/scenarios/login-paperdoll-walk.json` —
  scenario script.

## Notes for the AI session that picks this up

- ECS uses Bevy.UI + Clay. Layout uses `Node` with
  `PositionType.Absolute` for floating elements. Custom UO
  rendering goes through `UOCustomRender` with one of
  `UOCustomKind.{Gump, GumpNinePatch, Art, Land, Text}`.
  Layout passes only emit a Clay Custom render command when
  the entity has a `UiCustom` marker — easy to forget when
  adding new visual elements.
- Z-index does NOT inherit from parent. New children that
  need to render above the paperdoll body must carry their
  own `ZIndex(101)` (or higher) component.
- TinyEcs entity-tied observers run on the entity that
  emits the trigger (e.g. `UiPointerDown` is propagated up
  through parents). Use `EntityCommands.Observe<On<T>>(...)`
  at spawn time.
- `Commands.AddChild(parent, child)` parents UI but does NOT
  cascade `Despawn` — children must be explicitly despawned
  before/after the parent (see `ClosePaperdollOnRightClick`).
- `PaperdollTarget.Serial` is the mobile this paperdoll
  represents. For local-player-specific UI (virtue button,
  party manifest), check `pd.Serial == gameCtx.Value.PlayerSerial`.
- SDL3 is the active backend on impl/ecs (`bin/agent/net10.0/`
  ships `SDL3.dll`). SDL2-flavor `SDL_PushEvent` calls don't
  reach FNA's event pipeline — see why `input.type` goes
  through `AgentServerState.PendingTypedChars +
  DrainTypedCharsSystem` instead of pushing SDL events.

---

## Prompt to bootstrap tomorrow's session

Copy-paste this into a fresh Claude Code session in
`C:\dev\cuo\cuo-agents` on the `impl/ecs` branch:

````
Read docs/ecs-port/paperdoll-parity.md end-to-end before
doing anything else. It describes a multi-day porting job
from the legacy paperdoll (src/ClassicUO.Client/Game/UI/Gumps/
PaperdollGump.cs on `main`) to the ECS paperdoll
(src/ClassicUO.Client/Ecs/Gameplay/PaperdollPlugin.cs on
`impl/ecs`), the items already done, the items left grouped by
effort, and the exact entrypoint to resume work.

Current state:
- Branch: impl/ecs, tip near commit 0fb049d12 (run `git log
  --oneline -5` to confirm).
- A separate worktree at C:\dev\cuo\cuo-main (branch
  agent-port/main, tip 39cfd8a5f) holds the legacy paperdoll
  for reference + the agent harness ported to the legacy
  GameController loop.
- The agent harness CLI lives at tools/agent-desktop/. It
  drives the running cuo.agent.exe via JSON-RPC on a
  loopback port discovered at
  %LOCALAPPDATA%/ClassicUO/agent/port.json.
- ModernUO shard runs at 127.0.0.1:2593 from
  C:/dev/ModernUO/Distribution/ModernUO.exe. Account
  admin/admin, character KARASHO (Grandmaster Alchemist,
  starts in Green Acres at ~5443,1151).
- A reference paperdoll capture from `main` is at
  .runtime/compare/cmp__main__pd.png and the current ECS
  state is at .runtime/compare/cmp__ecs__pd.png. Diff these
  for visual gap.

Today's goal: pick the next item from the doc's "Small"
section (jewelry-slot frames is the recommended first
step), implement it, build, run it against ModernUO via
the agent harness, capture a fresh ECS paperdoll
screenshot, and commit. Repeat for as many Small items as
fit in the session.

Build invocation:
  dotnet build src/ClassicUO.Client/ClassicUO.Client.csproj
    "-p:AGENT_BUILD=true"

Run invocation (after killing any prior cuo.agent.exe):
  ./bin/agent/net10.0/cuo.agent.exe > /tmp/ecs.log 2>&1 &

Drive invocation (CLI at tools/agent-desktop/bin/Debug/net9.0/
agent-desktop.dll):
  dotnet $CLI rpc-click ...
  dotnet $CLI rpc-type ...
  dotnet $CLI rpc-double-click ...
  dotnet $CLI rpc-shot --out <path>

Don't push to remote. All commits stay local. Read AGENTS.md
+ CLAUDE.md if anything in the harness or the ECS layout is
unclear.
````
