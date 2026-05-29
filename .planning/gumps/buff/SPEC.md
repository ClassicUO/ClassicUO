# Buff Gump Gump Spec

## Overview

The Buff Gump is a small, client-side, persistent window that shows a strip of
buff/debuff icons for the player, each with an optional countdown timer text.
It is NOT server-pushed: the server only feeds buff *data* via packet `0xDF`
(Buff/Debuff). The gump itself is opened by the player (a macro action
`MacroType.OpenBuffsGump`, the Status gump menu, or profile restore at startup)
and then re-renders itself whenever the player's buff set changes.

Each icon is a UO gump sprite (graphic looked up from `BuffTable`). Near
expiry (under 10s) the icon pulses its alpha between 60 and 255. When the
profile option `BuffBarTime` is on, a small fixed-font countdown ("1:23",
"45s", "2h") is drawn over each icon. Hovering an icon shows a tooltip with the
buff title/description and remaining time.

A single toggle button cycles the gump through four layout *directions*
(left-vertical, left-horizontal, right-vertical, right-horizontal), each backed
by a different background sprite. The default on first open is
`LEFT_HORIZONTAL` with background `0x7580`.

## Source of truth

- `src/ClassicUO.Client/Game/UI/Gumps/BuffGump.cs` — the gump itself.
  - `BuffGump(World, int x, int y)` ctor (lines 32-43): default `_direction =
    LEFT_HORIZONTAL`, `_graphic = 0x7580`, `SetInScreen()`, `BuildGump()`.
  - `BuildGump()` (lines 47-114): builds `_background` GumpPic, the toggle
    `_button`, the `_box` DataBox, and a `BuffControlEntry` per active buff.
  - `UpdateElements()` (lines 142-235): per-direction icon placement (31px
    stride), negative-coordinate shift compensation, box sizing.
  - `OnButtonClick(int)` (lines 237-274): button 0 cycles `_graphic`
    0x757F..0x7582 and maps each to a `GumpDirection`.
  - `GumpDirection` enum (lines 276-282).
  - `BuffControlEntry` nested class (lines 284-439): per-icon sprite, alpha
    pulse, timer text, tooltip; `Update()` (321-394) drives pulse + tooltip +
    auto-refresh on expiry; `AddToRenderLists()` (396-432) draws icon + text.
- `src/ClassicUO.Client/Network/PacketHandlers.cs` lines 5470-5570 — `0xDF`
  handler: reads serial + `BuffIconType` + count; count==0 -> `RemoveBuff`,
  else `AddBuff(ic, BuffTable.Table[iconID], timer, text)`; pokes the open
  `BuffGump` via `RequestUpdateContents()`.
- `src/ClassicUO.Client/Game/Data/BuffIcon.cs` — `BuffIcon { Type, Graphic,
  Timer(=Time.Ticks+timer*1000, or 0xFFFFFFFF if timer<=0), Text }`.
- `src/ClassicUO.Client/Game/Data/BuffTable.cs` — `BuffIconType` enum (0x3E9..)
  and `BuffTable.Table[]` graphic lookup. iconID derivation:
  `ic >= 0x466 ? ic - (0x466-125) : ic - 0x3E9`.
- `src/ClassicUO.Client/Game/Managers/MacroManager.cs:1442` —
  `UIManager.Add(new BuffGump(_world, 100, 100))` (macro open at 100,100).
- `src/ClassicUO.Client/Game/UI/Gumps/StatusGump.cs:52` — status-menu open.
- `src/ClassicUO.Client/Configuration/Profile.cs:539-540` — restored from
  saved layout (GumpType.Buff).

### Existing ECS pieces (already present)

- `src/ClassicUO.Ecs/Network/IncomingPackets/OnBuffDebuffPacket_0xDF.cs` —
  packet is fully parsed into `OnBuffDebuffPacket_0xDF { Serial, IconType,
  Count, List<BuffEntry> Entries }` (BuffEntry carries SourceType, Icon,
  QueueIndex, Timer, the three clilocs, Arguments/Arguments2/Arguments3).
- `src/ClassicUO.Ecs/Network/InGamePacketsPlugin.cs:303` — currently
  `Stub<OnBuffDebuffPacket_0xDF>(app)` (parsed, then discarded). This spec
  replaces the stub with a real observer.
- `BuffIconType` lives in `ClassicUO.Game.Data` (shared) and is reachable.

## Visual structure

Window root = the background GumpPic (`_background`) at the gump origin (0,0).
All three direction-dependent background sprites are 0x757F..0x7582 (the toggle
button cycles them). The default `LEFT_HORIZONTAL` uses `0x7580`.

Control tree (initial `LEFT_HORIZONTAL`, `_graphic=0x7580`):

| # | Control | Type | Asset | Kind | X | Y | W/H | Notes |
|---|---------|------|-------|------|---|---|-----|-------|
| 1 | `_background` | GumpPic | `0x7580` (one of 0x757F-0x7582) | Gump | 0 | 0 | native sprite | window root; LocalSerial 1 |
| 2 | `_button` | Button | up `0x7585` / down+over `0x7589` | Gump | -2 | 36 | native | toggle direction; buttonID 0 |
| 3 | `_box` | DataBox | none (layout container) | - | 0 | 0 | computed | holds the icon strip |
| 3.n | `BuffControlEntry` | GumpPic (subclass) | `icon.Graphic` (from BuffTable) | Gump | see below | see below | native icon (~30x30) | one per active buff; +text overlay |

Button position per direction (`BuildGump` switch, lines 71-97):

| Direction | Button X | Button Y |
|-----------|----------|----------|
| LEFT_VERTICAL (default fallthrough) | 0 | 0 |
| LEFT_HORIZONTAL | -2 | 36 |
| RIGHT_VERTICAL | 34 | 78 |
| RIGHT_HORIZONTAL | 76 | 36 |

Icon placement per direction (`UpdateElements`, lines 147-177), `offset = i*31`:

| Direction | Icon X | Icon Y |
|-----------|--------|--------|
| LEFT_VERTICAL | 25 | 26 + offset |
| LEFT_HORIZONTAL | 26 + offset | 5 |
| RIGHT_VERTICAL | 5 | `bgHeight - 48 - offset` |
| RIGHT_HORIZONTAL | `bgWidth - 48 - offset` | 5 |

Negative-coordinate compensation (lines 179-215): for the RIGHT_* variants,
when many icons push an icon X/Y below 0, every child + the background + the
button are shifted by `-min`, and the gump origin is moved by the same amount
(`_shiftX/_shiftY`) so the background stays put on screen. `Save()` un-shifts
before persisting the anchor. The box is then explicitly sized to the bounding
box of all icons (lines 217-234).

Per-icon timer text (`BuffControlEntry`, only when `Profile.BuffBarTime`):
- RenderedText created with hue `0xFFFF`, font **2**, `isunicode=true`,
  style `Fixed | BlackBorder`, align `TS_CENTER`, max width = icon width.
- Text content: `"{h}h"` if hours>0, else `"{m}:{ss}"` if minutes>0, else
  `"{ss}s"` (lines 345-355).
- Drawn at `(x - 3, y + sourceRect.Height/2 - 3)` (line 424), i.e. centered
  vertically over the icon, nudged left 3px.

Alpha pulse (lines 358-391): only when `Timer != 0xFFFFFFFF && delta < 10000`.
`addVal = (10000 - delta) / 600`; alpha ramps down to 60 then back up to 255,
ping-pong. When `delta <= 0` the gump requests a content rebuild (icon expired).

## Assets

| Asset | ID | Kind | Used for |
|-------|----|----|----------|
| BG left-vertical | `0x757F` | Gump | background, LEFT_VERTICAL |
| BG left-horizontal | `0x7580` | Gump | background, LEFT_HORIZONTAL (default) |
| BG right-vertical | `0x7581` | Gump | background, RIGHT_VERTICAL |
| BG right-horizontal | `0x7582` | Gump | background, RIGHT_HORIZONTAL |
| Toggle button up | `0x7585` | Gump | direction-cycle button normal |
| Toggle button down/over | `0x7589` | Gump | direction-cycle button pressed + over |
| Buff icon | `BuffTable.Table[iconID]` | Gump | per-buff icon (derived from `BuffIconType`) |
| Timer font | font id **2** | text | countdown text (RenderedText, unicode) |
| Timer text hue | `0xFFFF` | hue | white timer text (Fixed+BlackBorder) |
| Icon hue | `0` (none) | hue | icons drawn unhued; alpha varies via pulse |

iconID -> graphic mapping (PacketHandlers 5475-5486 / BuffTable):
`BUFF_ICON_START = 0x03E9`, `BUFF_ICON_START_NEW = 0x466`.
`iconID = (ushort)ic >= 0x466 ? ic - (0x466 - 125) : ic - 0x3E9`, guard
`iconID < BuffTable.Table.Length`, then `graphic = BuffTable.Table[iconID]`.

## Behaviors

| Behavior | Legacy | ECS mechanism |
|----------|--------|---------------|
| Drag to move | `CanMove = true` | `UIMovable` on root (from `UOGumpBundle`); `WindowDragPlugin.Drag`. No custom code. |
| Right-click close | `CanCloseWithRightClick = true` | `UIMovable` -> `WindowDragPlugin.CloseOnRightClick` despawns subtree. No custom code. Note: closing only hides the window — buff data lives on the player, so a re-open repopulates. |
| Topmost on click | base Gump | root-only `GlobalZIndex`, bumped by drag latch. No custom code. |
| Click-capture vs world | base Gump | `ClaimSelectedFromMovable` (root has no NetworkSerial/ContainerWindow). No custom code. |
| Pixel-perfect hit | Gump PixelCheck | `UiHitTest.PixelHit` Gump case on the bg + icon sprites (already covers `UOCustomKind.Gump`). The four bg sprites have transparent corners, so transparent-pixel passthrough already works. |
| Direction toggle button | `OnButtonClick(0)`: cycle 0x757F..0x7582, set direction, `RequestUpdateContents` | `UOButton {Normal=0x7585, Pressed/Over=0x7589}` + `On<UiClick>` observer that advances `BuffGumpState.Graphic`/`Direction` and triggers a rebuild. Fire-on-release per contract. |
| Buff added/removed (server) | `0xDF` handler -> `AddBuff/RemoveBuff` + `RequestUpdateContents` | Replace `Stub<OnBuffDebuffPacket_0xDF>` with `AddObserver<On<PacketReceived<OnBuffDebuffPacket_0xDF>>,...>` that updates a `PlayerBuffs` resource and triggers a rebuild of any open buff window. |
| Alpha pulse near expiry | `BuffControlEntry.Update` | Per-icon component `BuffIconView { Timer, Alpha, DecreaseAlpha }` mutated by a `Stage.Update` system using `Res<Time>` (NOT `Time.Ticks`). System writes the pulsing alpha into the icon's `UOCustomRender.Hue.Z` (alpha) each frame. |
| Auto-rebuild on expiry | `delta<=0 -> RequestUpdateContents` | Same update system: when remaining time <=0, drop the buff from `PlayerBuffs` and trigger window rebuild. |
| Timer text | RenderedText drawn in `AddToRenderLists` when `BuffBarTime` | New `BuffTimerText` ClayUO custom command (see below) OR a Bevy.UI `Text` child re-synced each frame. Gated on a `BuffBarTime` flag (read from a profile/settings resource; default off until settings are wired — see open questions). |
| Tooltip (title + time left) | `SetTooltip(...)` updated ~1/s | Out of scope for v1 unless an ECS tooltip system exists. Carry the title text on the icon component for a later tooltip pass. (open question) |
| Save/restore layout + direction | `Save/Restore` write graphic+direction | Persist `Direction`/`Graphic` to the ECS gump-persistence layer if/when one exists; v1 may default to LEFT_HORIZONTAL each session (open question). |

## Server packets

- **`0xDF` Buff/Debuff** (only data source). Already parsed in
  `OnBuffDebuffPacket_0xDF`. Layout:
  `Serial(u32) IconType(u16) Count(u16)`, then per entry: `SourceType(u16),
  skip2, Icon(u16), QueueIndex(u16), skip4, Timer(u16, seconds), skip3,
  TitleCliloc(u32), DescriptionCliloc(u32), AdditionalCliloc(u32),
  Args(unicode-LE, 2-char prefix), Args2(u16-len then u16-LE),
  Args3(u16-len then u16-LE)`. `Count==0` removes the buff of that
  `IconType`; otherwise add/refresh. Doc: `doc/network/incoming/0xDF_buff-debuff.md`.

There is **no** packet that opens the gump. The window is opened locally
(macro / status menu / profile restore). This is the key behavioral
difference from paperdoll/container gumps.

## ECS implementation plan

**Plugin**: `src/ClassicUO.Ecs/Gameplay/BuffGumpPlugin.cs`
(`internal readonly struct BuffGumpPlugin : IPlugin`), composed into
`CuoPlugin.Build` in `src/ClassicUO.Ecs/Boot.cs`.

### Resources

- `Res<PlayerBuffs>` (`internal sealed class PlayerBuffs`): the authoritative
  buff set, keyed by `BuffIconType`. Each value: `{ ushort Graphic, float
  ExpiryTotalMs (Time.Total + timer*1000, or float.PositiveInfinity for
  permanent), string Text }`. Replaces legacy `PlayerMobile.BuffIcons`.
  Register `app.AddResource(new PlayerBuffs())`.
- `Res<UiZCounter>` — already registered by `WindowDragPlugin`.
- `Res<BuffGumpSettings>` (or reuse an existing profile/settings resource) for
  the `BuffBarTime` flag. If no settings resource exists yet, default `false`
  and gate the timer text behind it (open question).

### Components

- `BuffGumpWindow { ushort Graphic; BuffDirection Direction; }` — tag on the
  window root (alongside the `UOGumpBundle` markers). `BuffDirection` enum
  mirrors legacy `GumpDirection` (LeftVertical, LeftHorizontal, RightVertical,
  RightHorizontal).
- `BuffGumpToggle` — tag on the cycle button (so the click observer finds it
  and the rebuild can reach its window root).
- `BuffGumpChild { ulong WindowEntity; }` — tag on every icon (+ text) sprite,
  so a rebuild despawns precisely the dynamic strip (mirrors
  `PaperdollBodyChild`).
- `BuffIconView { BuffIconType Type; float ExpiryTotalMs; byte Alpha; bool
  DecreaseAlpha; }` — per-icon state for the pulse + expiry system.

### Bundle usage

Root spawned via `GumpBuilder.SpawnUOGump(commands, bgId, Vector3.UnitZ,
position, zCounter)` (the `UOGumpBundle` path) with `bgId` = current direction
background (`0x7580` default). `.Insert(new BuffGumpWindow{...})`. This gives
`UIMovable` + `GlobalZIndex` + right-click-close + drag + click-capture for
free — no per-gump reimplementation (rule 4 / UO Gump contract).

The toggle button: `GumpBuilder.AddButton(commands, (0x7585, 0x7589, 0x7589),
Vector3.UnitZ, buttonPos)` `.Insert(new BuffGumpToggle())`, `AddChild` to root.

Icons: `GumpBuilder.AddGump(commands, iconGraphic, iconHue, iconPos)`
`.Insert(new BuffGumpChild{WindowEntity=root})` `.Insert(new BuffIconView{...})`,
`AddChild` to root. (Icons are children of the root directly; the legacy
`_box` DataBox is just a layout helper — ECS positions absolutely so no
intermediate container is needed. Compute positions with the per-direction
formula + negative-shift compensation in C#.)

### Observers

1. `On<PacketReceived<OnBuffDebuffPacket_0xDF>>` (replaces the stub in
   `InGamePacketsPlugin.cs:303`): for each entry, derive `iconID` ->
   `BuffTable.Table[iconID]` graphic, store/refresh in `PlayerBuffs`
   (`Count==0` removes the `IconType`). Then trigger a rebuild of every open
   `BuffGumpWindow` (despawn `BuffGumpChild`s, rebuild strip) — same
   despawn-then-rebuild pattern as `PaperdollPlugin.RebuildOnEquip`. Commands
   passed top-level for auto-apply.
2. Toggle-button click: `button.Observe((On<UiClick> _, Commands, <params>) =>
   ...)` advances `Graphic` (0x757F..0x7582 wrap to 0x757F) -> maps to
   `Direction` (0x7580=LeftHorizontal, 0x7581=RightVertical,
   0x7582=RightHorizontal, else LeftVertical), updates the root's
   `UOCustomRender.AssetId` (the new bg) in place, updates `BuffGumpWindow`,
   moves the button to the new direction position, and rebuilds the icon strip.
3. (Optional) open trigger: there is no server open packet. Provide an open
   path equivalent to the macro — a `BuffGumpOpenEvent` (sent by a future macro
   system / hotkey) handled by a spawn observer that builds the window if none
   exists, else focuses (bump z). v1 may instead auto-open the window once the
   player has >=1 buff (open question — see below).

### Systems

- `UpdateBuffIcons` (`Stage.Update`): query `Data<BuffIconView, UiCustom>`.
  Using `Res<Time>` (`Time.Total`), compute `delta = ExpiryTotalMs - Time.Total`.
  When `ExpiryTotalMs` is finite and `delta < 10000`: run the ping-pong alpha
  (`addVal = (10000 - delta) / 600`, clamp 60..255) and write `Alpha/255f` into
  the icon's `UOCustomRender.Hue.Z`. When `delta <= 0`: mark the buff for
  removal (remove from `PlayerBuffs` + trigger window rebuild). Permanent buffs
  (`ExpiryTotalMs == +inf`) render at full alpha and are skipped.
  MUST use `Res<Time>`, never `Time.Ticks` / `DateTime` (CLAUDE.md rule 3).
- `DisposeOnLogout` (`OnExit(GameState.GameScreen)`): despawn all
  `BuffGumpWindow` subtrees + clear `PlayerBuffs` (mirror
  `PaperdollPlugin.DisposeOnLogout`).

### New ClayUO custom render command (timer text)

The icons reuse the existing `UOCustomKind.Gump` render + `UiHitTest.Gump`
case — no new code needed there.

The countdown text needs the UO **RenderedText** look (unicode font 2, Fixed +
BlackBorder, centered, hue 0xFFFF). Two options:

- **Preferred**: a Bevy.UI `Text` child node per icon, re-synced each frame
  from `BuffIconView` (font 2 via `TextFont`, white `TextColor`), positioned at
  `(iconX - 3, iconY + iconH/2 - 3)`. No new ClayUO command — reuses the
  existing `ClayUOCommandType.Text` path. This is the lowest-risk route.
- **Alternative**: add `ClayUOCommandType.BuffTimerText` (enum in
  `GuiPlugin.cs`) + a `case` in `GuiRenderingPlugin.cs` custom switch that draws
  via FontStash with the BlackBorder effect, only when `BuffBarTime` is on.
  Choose this only if the plain `Text` node can't reproduce the black border /
  fixed font; document the decision when implementing.

No new `UiHitTest` case is required (timer text is non-interactive; icons use
the existing Gump case).

### CLAUDE.md conformance checklist

- No `World` access; all mutation via `Commands`, reads via `Query`/`Res`.
- Spawn via `UOGumpBundle` (`GumpBuilder.SpawnUOGump`); drag/close/z/capture
  inherited, not reimplemented.
- Buttons fire on release (`On<UiClick>`).
- Server reaction via observer on the typed packet trigger (no per-frame
  `EventReader<IPacket>` scan).
- Time via `Res<Time>` (`Time.Total`), never wall-clock.
- Cross-system state (`PlayerBuffs`) is a `Res`/`ResMut`, not a static.
- Rebuild = despawn `BuffGumpChild` subtree + rebuild (PaperdollPlugin pattern).

## How to trigger for capture

The buff gump is client-opened, not server-pushed. Steps for a live reference
screenshot:

1. Boot ModernUO (`127.0.0.1:2593`, `admin/admin`) and the AGENT_BUILD client
   per `tools/agent-desktop/AGENTS.md` (`up --persist`).
2. Log a character into the world (golden path).
3. Acquire at least one buff so `0xDF` fires and `BuffTable` has an icon. On
   ModernUO the simplest is to cast a self-buff (e.g. Night Sight / Bless /
   Protection) or have GM commands add a buff; debuffs (Poison) also work.
4. Open the buff gump. In legacy this is the `OpenBuffsGump` macro or the
   Status-gump menu entry; in ECS use whatever open path the plugin exposes
   (`BuffGumpOpenEvent` / hotkey / auto-open-on-first-buff per the chosen
   design). At least one active buff must be present, or the strip is empty.
5. `rpc-shot` to capture. To verify the direction toggle, `rpc-click` the
   toggle button (top-left, near (-2,36) offset from the window origin) and
   re-shot for each of the four backgrounds.

Required game state: in-world, player has >=1 active buff/debuff. For the timer
text, `BuffBarTime` profile option must be enabled (and an ECS settings hook
must exist — see open questions).

## Open questions

1. **Open trigger.** Legacy opens via macro / status menu / profile restore;
   there is no server open packet. What is the ECS open path — a macro/hotkey
   that sends `BuffGumpOpenEvent`, or auto-open the window when the player gains
   their first buff? Need a product decision; v1 default proposed:
   auto-open-on-first-buff + close-with-right-click, since macros/hotkeys may
   not be wired yet.
2. **`BuffBarTime` settings source.** Is there an ECS profile/settings resource
   exposing the `BuffBarTime` flag? If not, timer text is gated off in v1
   (icons-only) until settings are wired.
3. **Tooltips.** Does the ECS branch have a tooltip system? Legacy updates the
   per-icon tooltip ~1/s with title + remaining time. If no tooltip infra
   exists, defer; the `BuffIconView`/buff record should still carry the title
   text for later.
4. **Cliloc translation.** Legacy builds `text` by translating the three
   clilocs with args. Does the ECS branch have a `Clilocs.Translate`
   equivalent reachable from the packet observer? If not, store raw cliloc ids
   + args on the buff record and translate later (only matters for tooltip).
5. **Layout persistence.** Should `Direction`/`Graphic` + window position
   survive a session (legacy `Save/Restore`)? Depends on whether ECS has a
   gump-persistence layer yet. v1 may reset to LEFT_HORIZONTAL each session.
6. **Negative-shift compensation.** The legacy RIGHT_* shift juggles the gump
   origin so the background stays put as icons grow leftward/upward. In ECS,
   icons are absolute children of the root; confirm whether to replicate the
   origin-shift math or simply allow negative child offsets (Clay supports
   negative `Left/Top`). Simpler: allow negative child offsets, skip the
   origin shift — verify it renders/hit-tests identically.
