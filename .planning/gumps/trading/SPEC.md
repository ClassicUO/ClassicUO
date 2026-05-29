# Secure Trade Gump Spec

## Overview

The Secure Trade gump is a two-pane drag-to-trade window that appears when two
players initiate a secure trade. It shows:

- A "my offer" item box (left/top pane) and a "his offer" item box (right/bottom
  pane), each a fixed 110x80 drop area for items the two sides put up.
- Both player names (mine + partner's).
- An accept checkbox for me + an accept indicator (gump pic) for the partner.
- On modern clients (CV >= 704565) a gold/platinum offer area: my gold/plat
  labels, the partner's gold/plat labels, and two numeric text-entry boxes I can
  type my gold/platinum offer into.

It opens **only when a trade partner is present** — the server pushes packet
`0x6F` subcommand `0x00` (begin trade) carrying the two trade-container serials.
Dragging an item into "my box" drops it into the trade container; ticking my
checkbox accepts; closing the window cancels the trade.

There are two layout variants gated on client version:

- **Legacy** (`CV < 704565`): single-coin-less layout, background gump `0x0866`.
- **Modern** (`CV >= 704565`): gold/platinum layout, background gump `0x088A`.

The ECS branch targets the modern layout (`0x088A`) as the primary contract and
notes the legacy values for completeness. There is no ECS trade code today
(packet `0x6F` is unregistered, no `Send_TradeResponse` / `Send_TradeUpdateGold`
in `src/ClassicUO.Ecs/Network/OutgoingPackets.cs`) — this is a from-scratch port.

## Source of truth

- **Gump control tree**: `src/ClassicUO.Client/Game/UI/Gumps/TradingGump.cs`
  - `BuildGump()` — `TradingGump.cs:387-581` (full control tree, both variants).
  - `SetCheckboxes()` — `TradingGump.cs:339-385` (my checkbox + his accept pic,
    per-variant coords).
  - `UpdateContents()` — `TradingGump.cs:147-260` (populates `_myBox` / `_hisBox`
    item icons from the two trade-container serials ID1/ID2).
  - `OnMouseUp()` — `TradingGump.cs:262-331` (drop held item into my box ->
    `GameActions.DropItem(..., ID1)`; otherwise delayed click / target).
  - `Dispose()` — `TradingGump.cs:333-337` (`GameActions.CancelTrade(ID1)` on
    close).
  - `MyCheckboxOnValueChanged()` — `TradingGump.cs:583-587`
    (`GameActions.AcceptTrade(ID1, ImAccepting)`).
  - Gold/Platinum setters — `TradingGump.cs:53-119` (update coin labels when
    `CV >= 704565`).
  - `OnTextChanged` (gold/plat entry handler) — `TradingGump.cs:451-528`
    (clamps to current gold/plat, sends `Send_TradeUpdateGold(ID1, gold, plat)`).
- **Packet open/update handler** (`0x6F` SecureTrading):
  `src/ClassicUO.Client/Network/PacketHandlers.cs:375-443`.
- **Outgoing packets**:
  `src/ClassicUO.Client/Network/OutgoingPackets.cs`
  - `Send_TradeResponse(serial, code, state)` — lines 1435-1480
    (code 1 = cancel, code 2 = accept w/ state).
  - `Send_TradeUpdateGold(serial, gold, platinum)` — lines 1482-1514
    (subcommand `0x03`).
- **GameActions wrappers**: `src/ClassicUO.Client/Game/GameActions.cs:702-710`
  (`AcceptTrade` -> `Send_TradeResponse(serial, 2, accepted)`;
  `CancelTrade` -> `Send_TradeResponse(serial, 1, false)`).

## Visual structure

Coordinates are window-local (relative to the gump root at 0,0). Fonts: UO ASCII
font indices. The ECS Bevy.UI text node does not expose UO ASCII font 1/3/9
directly today (PaperdollPlugin uses the default font for labels) — see Open
questions.

### Modern layout — `CV >= 704565` (PRIMARY contract)

Root background: `GumpPic(0,0, 0x088A)` — natural sprite size defines window w/h.

| Control | Type | Asset / text | X | Y | W | H | Font | Hue |
|---|---|---|---|---|---|---|---|---|
| Window bg | GumpPic | gump `0x088A` | 0 | 0 | native | native | — | 0 |
| My name | Label | `World.Player.Name` | 73 | 32 | auto | auto | 3 | 0x0481 |
| His name | Label | partner `name` | `250 - GetWidthASCII(3,name)` | 244 | auto | auto | 3 | 0x0481 |
| My gold | Label | `_gold` (N0) | 43 | 67 | auto | auto | 9 | 0x0481 |
| My platinum | Label | `_platinum` (N0) | 180 | 67 | auto | auto | 9 | 0x0481 |
| His gold | Label | `_hisGold` (N0) | 180 | 190 | auto | auto | 9 | 0x0481 |
| His platinum | Label | `_hisPlatinum` (N0) | 180 | 210 | auto | auto | 9 | 0x0481 |
| My gold entry | StbTextBox (numbers only) | initial "0", Tag=0 | 43 | 190 | 100 | 20 | 9 | — |
| My plat entry | StbTextBox (numbers only) | initial "0", Tag=1 | 43 | 210 | 100 | 20 | 9 | — |
| My box | DataBox (item drop area) | — | 30 | 110 | 110 | 80 | — | — |
| His box | DataBox (item area) | — | 192 | 110 | 110 | 80 | — | — |
| My checkbox (unchecked) | Checkbox | inactive `0x0867` / pressed `0x0868` | 37 | 29 | — | — | — | — |
| My checkbox (checked) | Checkbox | active `0x0869` / pressed `0x086A` | 37 | 29 | — | — | — | — |
| His accept pic (not accepting) | GumpPic | `0x0867` | 258 | 240 | — | — | — | 0 |
| His accept pic (accepting) | GumpPic | `0x0869` | 258 | 240 | — | — | — | 0 |

### Legacy layout — `CV < 704565`

Root background: `GumpPic(0,0, 0x0866)`.

| Control | Type | Asset / text | X | Y | W | H | Font | Hue |
|---|---|---|---|---|---|---|---|---|
| Window bg | GumpPic | gump `0x0866` | 0 | 0 | native | native | — | 0 |
| My name | Label | `World.Player.Name` | 84 | 40 | auto | auto | 1 | 0x0386 |
| His name | Label | partner `name` | `260 - GetWidthASCII(1,name)` | 170 | auto | auto | 1 | 0x0386 |
| My box | DataBox | — | 45 | 70 | 110 | 80 | — | — |
| His box | DataBox | — | 192 | 70 | 110 | 80 | — | — |
| My checkbox | Checkbox | `0x0867/0x0868` or `0x0869/0x086A` | 52 | 29 | — | — | — | — |
| His accept pic | GumpPic | `0x0867` or `0x0869` | 266 | 160 | — | — | — | 0 |

Extra (only `CV < CV_500A`, both variants): two `ColorBox(110,60, hue 0)` filler
boxes at `(45,90)` and `(192,70)` — `TradingGump.cs:551-556`. Out of scope for v1
(modern target is well past 500A).

### Item boxes (both variants)

Each box (`_myBox` = ID1 container, `_hisBox` = ID2 container) is 110x80,
`ContainsByBounds = true`, mouse-input enabled, movable. Items are `ItemGump`
children placed at the item's stored (X, Y), clamped so the art stays inside the
box (`x = min(x, W - artW)`, `y = min(y, H - artH)`, then floored at 0) —
`TradingGump.cs:172-203` (my box) and `:227-258` (his box). Each item icon has
`HighlightOnMouseOver = true` (hover hue, same as container slots).

## Assets

| Asset | ID | Kind | Usage |
|---|---|---|---|
| Modern window bg | `0x088A` | gump | root background (CV >= 704565) |
| Legacy window bg | `0x0866` | gump | root background (CV < 704565) |
| Checkbox inactive (normal) | `0x0867` | gump | my unchecked box / his not-accepting pic |
| Checkbox inactive (pressed) | `0x0868` | gump | my unchecked box pressed art |
| Checkbox active (normal) | `0x0869` | gump | my checked box / his accepting pic |
| Checkbox active (pressed) | `0x086A` | gump | my checked box pressed art |
| Item icons | item graphic | art | trade-box contents (per item) |
| Item hover hue | `0x0035` | hue | highlight on mouse-over (mirrors ItemGump) |
| Name labels (modern) | — | font 3 | hue `0x0481` |
| Coin labels (modern) | — | font 9 | hue `0x0481` |
| Name labels (legacy) | — | font 1 | hue `0x0386` |

Notes:
- Hues are passed to UO label rendering as `parts[3]`; the +1 wire convention
  applies (see `ServerGumpPlugin.HueToClayColor`). For ECS the label hue path is
  currently approximate (see Open questions).
- `0x0867`/`0x0869` double as my-checkbox normal frames AND the partner's
  accept-indicator pics.

## Behaviors

| Behavior | Legacy/OOP source | ECS mechanism |
|---|---|---|
| **Drag to move** | `CanMove = true` (`TradingGump.cs:38`) | `UIMovable` tag on root via `UOGumpBundle`; `WindowDragPlugin.Drag` handles it. |
| **Right-click close** | `CanCloseWithRightClick = true` (`:39`) + `Dispose()` -> `CancelTrade(ID1)` (`:333-337`) | `UIMovable` -> `WindowDragPlugin.CloseOnRightClick` despawns the subtree. The cancel send must NOT live in close-on-right-click generically; add a dedicated observer/system that sends `Send_TradeResponse(ID1, 1, false)` when a `TradeWindow` entity despawns (see ECS plan). |
| **Topmost-on-click / z-stack** | UIManager z-order | Single `GlobalZIndex` on root via `UOGumpBundle`; `WindowDragPlugin.Drag` bumps `UiZCounter` on latch. |
| **Pixel-perfect hit-test** | OOP PixelCheck | `UiHitTest.PixelHit` with `UOCustomKind.Gump` for the `0x088A`/`0x0866` bg (native-size mask) — no new case needed. |
| **My accept checkbox (fire on release)** | `Checkbox.ValueChanged` -> `MyCheckboxOnValueChanged` -> `AcceptTrade(ID1, ImAccepting)` (`:583-587`) | A 2-state gump-pic acting as a button. `On<UiClick>` observer toggles `TradeWindow.ImAccepting`, swaps the asset id (`0x0867`<->`0x0869`), and sends `Send_TradeResponse(ID1, 2, accepting)`. |
| **Partner accept indicator** | server `0x6F` type 2 sets `HeIsAccepting` -> `SetCheckboxes()` swaps `_hisPic` (`:380-384`) | Observer on the trade-update packet flips the his-accept gump-pic's `UOCustomRender.AssetId` (`0x0867`<->`0x0869`) in place. |
| **Drop item into my box** | `OnMouseUp` -> `GameActions.DropItem(serial, x, y, 0, ID1)` (`:262-305`) | The "my box" is a container-like drop target keyed on the ID1 trade-container serial. Reuse the existing drop path (the pickup/drop system that drops onto a container serial), targeting ID1. |
| **Item hover highlight** | `HighlightOnMouseOver` (`:169`,`:224`) | Item icons carry hover/original hue like `ContainerItemUI`; selection hue toggle applies `0x0035` to the hovered icon. |
| **Trade-box contents update** | `UpdateContents()` rebuilds `_myBox`/`_hisBox` children from ID1/ID2 container items (`:147-260`) | The two trade containers are normal UO containers in `NetworkEntitiesMap`. Items arrive via container-update events; spawn item icons under the matching box, despawn+rebuild on change (observer, mirroring container item spawn). |
| **Gold/platinum text entry** | `_myCoinsEntries[0/1]` numbers-only, clamp to current gold/plat, `Send_TradeUpdateGold(ID1, gold, plat)` (`:451-528`) | Bevy.UI text-input nodes (numbers-only) with a change observer that clamps + sends `Send_TradeUpdateGold`. v1 may render labels only and defer editable entries (no Bevy.UI numeric widget yet — see Open questions). |
| **My/his coin labels** | server `0x6F` type 3/4 -> set `Gold`/`Platinum`/`HisGold`/`HisPlatinum` -> label text (`:53-119`, handler `:425-442`) | Update-packet observer rewrites the four coin labels' `Text` from `TradeWindow` state. |
| **Pages/tabs / scroll / resize** | none | n/a — single fixed-size page, no scroll, no resize. |

## Server packets

**Incoming — `0x6F` (SecureTrading)**, variable length. Handler:
`PacketHandlers.cs:375-443`. Wire shape:

```
byte   type
uint32 serial          // trade id; matches ID1 in the begin packet (the my-container)
switch (type):
  0x00 (begin):
    uint32 id1         // my trade container serial
    uint32 id2         // partner's trade container serial
    bool   hasName
    ASCII  name        // partner name (null-terminated, only if hasName)
    -> open TradingGump(serial, name, id1, id2)
       (skipped if world.Get(id1)==null || world.Get(id2)==null)
  0x01 (close): -> close/dispose the gump for `serial`
  0x02 (update accept state):
    uint32 id1         // my accept flag (!=0 means I accept)
    uint32 id2         // his accept flag
    -> ImAccepting = id1!=0; HeIsAccepting = id2!=0; refresh
  0x03 (update HIS gold):
    uint32 hisGold
    uint32 hisPlatinum
  0x04 (update MY gold):
    uint32 myGold
    uint32 myPlatinum
```

Note: types 3/4 read gold then platinum BE; the handler labels them His for 3 and
mine for 4 (`PacketHandlers.cs:425-442`).

**Outgoing — `0x6F`** (`OutgoingPackets.cs`):
- `Send_TradeResponse(serial, code, state)`:
  - `code 1` -> byte `0x01` + uint32 serial (CANCEL trade).
  - `code 2` -> byte `0x02` + uint32 serial + uint32 (state?1:0) (ACCEPT toggle).
- `Send_TradeUpdateGold(serial, gold, platinum)` -> byte `0x03` + uint32 serial +
  uint32 gold + uint32 platinum (offer gold/plat).

Item drops into the trade window use the standard item move/drop packet onto the
ID1 container serial (no trade-specific opcode).

## ECS implementation plan

### Files

- **Plugin**: `src/ClassicUO.Ecs/Gameplay/TradingGumpPlugin.cs`
  (`internal readonly struct TradingGumpPlugin : IPlugin`). Register in
  `Boot.cs` `CuoPlugin.Build`.
- **Incoming packet**: `src/ClassicUO.Ecs/Network/IncomingPackets/OnSecureTradingPacket_0x6F.cs`
  — an `IPacket` struct mirroring `OnOpenPaperdollPacket_0x88.cs` shape. Because
  `0x6F` is multi-shape, parse `Type`, `Serial`, and the type-dependent fields
  (`Id1`, `Id2`, `HasName`, `Name`, `Gold`, `Platinum`) in `Fill`, exposing them
  as properties. Register it in `NetworkPlugin.Build` via
  `packetsMap.Value.Register<OnSecureTradingPacket_0x6F>()`.
- **Outgoing helpers**: add `Send_TradeResponse` and `Send_TradeUpdateGold` to
  `src/ClassicUO.Ecs/Network/OutgoingPackets.cs` (port the two methods verbatim
  from the Client `OutgoingPackets.cs`).

### Components / resources

```csharp
// Root marker on the trade window. Carries the trade-container serials + state.
internal struct TradeWindow
{
    public uint TradeSerial;   // == ID1 (my container). The 0x6F `serial` field.
    public uint MyContainer;   // ID1
    public uint HisContainer;  // ID2
    public bool ImAccepting;
    public bool HeIsAccepting;
    public uint Gold, Platinum, HisGold, HisPlatinum;
}

// Marker on the my-accept toggle sprite (2-state gump pic acting as a button).
internal struct TradeAcceptCheckbox { public ulong WindowEntity; public uint TradeSerial; }

// Marker on the his-accept indicator sprite.
internal struct TradeHisAcceptPic { public ulong WindowEntity; }

// Marker on each coin label so the update observer can rewrite text.
internal struct TradeCoinLabel { public ulong WindowEntity; public TradeCoinKind Kind; }
enum TradeCoinKind { MyGold, MyPlatinum, HisGold, HisPlatinum }

// Marker on each item box (drop target) keyed to its container serial.
internal struct TradeItemBox { public ulong WindowEntity; public uint ContainerSerial; public bool IsMine; }

// Serial -> window entity map (mirrors ContainerUiMap) so update/close packets
// find the right window synchronously. Register: app.AddResource(new TradeUiMap()).
internal sealed class TradeUiMap { /* Dictionary<uint, ulong> by TradeSerial */ }
```

`ClientVersion` for the layout gate comes from `Res<GameContext>.ClientVersion`
(as in `PaperdollPlugin` / `TopBarPlugin`); compare against
`ClientVersion.CV_704565`.

### Bundle usage

Spawn the window root with `GumpBuilder.SpawnUOGump(commands, bgId, Vector3.UnitZ,
spawnPos, zCounter)` where `bgId = CV >= 704565 ? 0x088A : 0x0866`. That yields the
`UOGumpBundle` contract (Node + UiCustom Gump + Interaction.None + UOGump +
UIMovable + GlobalZIndex). Insert `TradeWindow` on the returned root, exactly like
`PaperdollPlugin.BuildWindow` inserts `PaperdollWindow`.

Children (names, coin labels, checkbox, his-pic, item boxes) are plain Bevy.UI
nodes added via `GumpBuilder.AddLabel` / `AddGump` and `commands.AddChild(root,
child)` — no per-child tags except the markers above. Coordinates from the Visual
structure table.

### Observers

1. `On<PacketReceived<OnSecureTradingPacket_0x6F>>` (composite param like
   `PaperdollSpawnParams`) — switch on `packet.Type`:
   - **0x00**: dedup by `TradeUiMap`/serial (focus-bump if open), else
     `BuildWindow(...)`; register in `TradeUiMap`. Guard: only build if both ID1
     and ID2 resolve in `NetworkEntitiesMap` (mirrors OOP's invisible-trader
     check).
   - **0x01**: look up window by serial, despawn subtree, drop the `TradeUiMap`
     entry. Do NOT re-send a cancel here (server initiated the close).
   - **0x02**: set `ImAccepting`/`HeIsAccepting` on `TradeWindow`; flip the
     accept-checkbox + his-pic asset ids in place.
   - **0x03 / 0x04**: set the four coin fields; rewrite coin label `Text`.
2. `OnRemove<TradeWindow>` (or a dedicated despawn-detect) — when a window the
   user right-click-closed is removed, send `Send_TradeResponse(TradeSerial, 1,
   false)`. This is the `Dispose() -> CancelTrade(ID1)` port. Because
   `WindowDragPlugin.CloseOnRightClick` despawns generic `UIMovable` subtrees
   directly (not via an event), the cleanest port is an `OnRemove<TradeWindow>`
   observer that fires the cancel — but verify it does NOT fire on server-driven
   (type 0x01) closes (set a flag on `TradeWindow`, e.g. `ServerClosed`, before
   despawn in the 0x01 branch, and skip the cancel when set). See Open questions.
3. Item-box population: reuse the container item pipeline. The two trade
   containers are real UO containers; subscribe an observer/system to the same
   container update event used by `ContainerGumpPlugin.SpawnContainerItemUI`, but
   spawn icons under the `TradeItemBox` whose `ContainerSerial` matches. On
   change, despawn-and-rebuild the box's item children (mirrors
   `PaperdollPlugin.RebuildOnEquip`).
4. Accept-checkbox click: `acceptSprite.Observe((On<UiClick> _, ...))` toggles
   `TradeWindow.ImAccepting`, swaps the sprite asset, sends
   `Send_TradeResponse(TradeSerial, 2, accepting)`. Fire-on-release (`UiClick`)
   per the button contract.
5. Gold/plat entry change (modern, if editable widget available): clamp to
   current gold/plat and `Send_TradeUpdateGold(TradeSerial, gold, plat)`.

### Systems

- Logout teardown: `OnExit(GameState.GameScreen)` despawns all `TradeWindow`
  subtrees + clears `TradeUiMap` (mirrors `PaperdollPlugin.DisposeOnLogout`).
- Item-box hover hue toggle: if not folded into the existing container selection
  system, a small system applies `0x0035` to the hovered item icon (mirrors
  `ContainerGumpPlugin.UpdateSelectedFromContainerUI`).

### Drop / pickup integration

The "my box" must accept dropped items onto the ID1 container serial. Prefer
reusing the existing drop path (the system that drops a grabbed item onto a
container UI entity, computing clamped X/Y). Tag `TradeItemBox` so that system
resolves the drop to `ContainerSerial == ID1` and only allows drops on the mine
box (OOP only drops into `_myBox` — `TradingGump.cs:271`). Mirror the item-aware
selection filter pattern (`ClaimSelectedFromMovable` skips `ContainerWindow`); add
a matching `Without<TradeWindow>` filter if the trade box needs its own
item-aware claim.

### ClayUO custom render + UiHitTest

**No new `ClayUOCommandType` / `UOCustomKind` value is required.** Everything maps
to existing kinds:
- Window bg, checkbox, his-pic, coin/name labels -> `UOCustomKind.Gump` + Bevy.UI
  Text.
- Item icons -> `UOCustomKind.Art` (same as container slots).

`UiHitTest.PixelHit` already covers `Gump` (native-mask) and `Art` (slot-art
scale) — no new case. The `0x088A`/`0x0866` bg uses the plain `Gump` path so
clicks on transparent corners pass through, matching the shared contract.

### Conformance to ECS rules (CLAUDE.md)

- No `World` access: all reads/mutations via `Query` / `Commands` / `Res`.
- Structural changes (spawn/despawn) via `Commands`; in-place field edits
  (asset-id swap, label text, accept flags) via mutable query refs.
- Cross-system state in `Res` (`TradeUiMap`), per-entity state in components
  (`TradeWindow`, `TradeCoinLabel`, ...). No closure-captured mutable state —
  capture only immutable serials in observer lambdas (mirrors PaperdollPlugin's
  `capturedSerial`).
- Time via `Res<Time>` if any timing is needed (none expected for v1).
- Buttons (accept checkbox) fire on `On<UiClick>` (release), per the button
  contract.

## How to trigger for capture

The gump requires a live trade between two characters on the server.

1. Boot ModernUO at `127.0.0.1:2593`, log in two clients (`admin/admin` and a
   second character) into the same area, standing adjacent.
2. From client A, drag-target / double-click the "secure trade" context-menu
   entry on player B, or use the server command to initiate a trade (ModernUO:
   target another player with the trade gesture / `[trade` style command if
   enabled). The server then pushes `0x6F` type `0x00` to both clients.
3. On receipt the window opens automatically (no top-bar button, no keybind, no
   item double-click opens it — it is strictly server-driven).
4. To exercise states for the screenshot:
   - Drag an item into "my box" (drops into ID1) -> server echoes container
     contents (`0x6F` is not involved; standard container update populates the
     box).
   - Click my accept checkbox -> sends `Send_TradeResponse(serial, 2, true)`;
     server replies `0x6F` type 2 -> his/my accept indicators update.
   - Type gold/plat (modern) -> `Send_TradeUpdateGold`; server replies type 3/4.

Required game state: two in-range, mutually visible player mobiles; client
version >= 704565 to capture the modern (`0x088A`) layout (the design target).
Using the harness, the deterministic path is to inject a synthetic `0x6F` type
`0x00` after two mobiles exist in `NetworkEntitiesMap`.

## Open questions

1. **Cancel-on-close detection**: `WindowDragPlugin.CloseOnRightClick` despawns
   generic `UIMovable` subtrees directly without an event. Porting `Dispose() ->
   CancelTrade(ID1)` cleanly needs either (a) an `OnRemove<TradeWindow>` observer
   that sends the cancel (with a `ServerClosed` guard so the 0x01 server-close
   path does not echo a cancel), or (b) routing trade windows through a
   close-event like containers do (`ContainerClosedEvent`). Which pattern does
   the team prefer? Containers already special-case in `CloseOnRightClick`
   (`ContainerWindow` branch) — a `TradeWindow` branch there mirroring it is the
   most consistent option.
2. **UO ASCII fonts (1/3/9) + label hues**: Bevy.UI's text node does not expose
   UO ASCII fonts; `PaperdollPlugin` falls back to the default font for its
   title. Coin labels (font 9) and name labels (font 3/1) with hues `0x0481` /
   `0x0386` will be approximate until a UO-font text path exists. Acceptable for
   v1? `ServerGumpPlugin.SpawnWrappedText` bakes text via `UoFontRenderer.Bake`
   (font 1) — reuse that for closer fidelity?
3. **Numeric gold/plat text entry**: is there a numbers-only Bevy.UI text-input
   widget available? If not, v1 should render the coin labels read-only and defer
   the editable entries + `Send_TradeUpdateGold` (label-only display still tracks
   server-pushed amounts via type 3/4).
4. **Trade item boxes vs ContainerGump**: the two trade containers (ID1/ID2) are
   real UO containers. Do they also trigger a normal `ContainerGump` to open
   (double window), or does the server only mark them as trade boxes? Need to
   confirm the trade containers are NOT separately opened as standalone container
   gumps, and that item-update events for them can be routed into the trade box
   instead.
5. **Legacy vs modern scope**: confirm the ECS target is modern-only (`0x088A`).
   The `CV < CV_500A` ColorBox fillers and the legacy `0x0866` layout are
   included here for completeness but proposed out of scope for v1.
6. **Item drop coordinate clamp**: OOP clamps drop X/Y to the box using
   `_myBox.Width/Height` (note: it uses `_myBox` dimensions even when checking the
   his box in `UpdateContents`, lines 234-242 — a latent OOP quirk). Confirm the
   ECS port clamps each box against its own 110x80 bounds.
