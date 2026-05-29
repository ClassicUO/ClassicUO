# MiniMap Gump Spec

## Overview

The MiniMap (a.k.a. radar map) is a small, draggable, square overlay window that
shows a top-down isometric "radar" of the terrain/statics around the player plus
colored dots for nearby mobiles (and a white dot for the player). It is a
client-side gump (no server packet opens or updates it) toggled from the top
bar's **Map** button (legacy `GameActions.OpenMiniMap`). It exists in two sizes:
a **small** map (background gump `5010`) and a **large** map (background gump
`5011`), toggled by **left double-click** on the window. Mobile dots blink
(toggle on/off every 500 ms). It is closable by right-click and movable by drag,
exactly like every other UO gump window.

Legacy: single client-side gump rebuilt every frame; the radar texture is
regenerated when the player moves or the map changes.

## Source of truth

- `C:/dev/cuo/cuo-agents/src/ClassicUO.Client/Game/UI/Gumps/MiniMapGump.cs`
  - `SMALL_MAP_GRAPHIC = 5010`, `BIG_MAP_GRAPHIC = 5011` — lines 34-35.
  - ctor: `CanMove = true; AcceptMouseInput = true; CanCloseWithRightClick = true;` — lines 37-42.
  - `GumpType => GumpType.MiniMap` — line 44.
  - `Save/Restore`: persists `isminimized` = `_useLargeMap` bool — lines 46-57.
  - `CreateMap()`: picks bg gump by `_useLargeMap`, sets `Width/Height` = gump UV size, caches blank bg pixels per-size in `_blankGumpsPixels`, builds radar texture — lines 59-80.
  - `Update()`: `CreateMap()` on `World.MapIndex` change; blink toggle `_draw` every 500 ms — lines 82-100.
  - `ToggleSize(bool?)`: flips `_useLargeMap`, rebuilds — lines 102-116.
  - `AddToRenderLists`: draws bg gump, regenerates + draws radar texture, then draws mobile dots (2x2 px, hue = `Notoriety.GetHue(mob.NotorietyFlag)`) and a white player dot at center — lines 118-194.
  - `OnMouseDoubleClick` (Left) → `ToggleSize()` — lines 196-206.
  - `CreateMiniMapTexture`: the radar pixel generator. Walks map blocks around the player, picks topmost land/static/multi color, writes isometric-rotated pixels into the bg-gump-shaped buffer — lines 213-405.
  - `CreatePixels`: writes a 2-pixel vertical stamp per cell into the buffer, only over the "blank" bg color `0xFF080808` — lines 407-447.
  - `Contains`: pixel-perfect hit-test against the cached blank bg pixel mask — lines 449-466.
- Top bar trigger: `C:/dev/cuo/cuo-agents/src/ClassicUO.Client/Game/UI/Gumps/TopBarGump.cs:211-214` — `case Buttons.Map: GameActions.OpenMiniMap(World);`.

## Visual structure

The minimap is a single square window: a background gump that defines its size
and its hit-mask, with the radar pixels baked INTO a copy of that gump's texture
(legacy mutates the gump texture in place each frame), then mobile/player dots
drawn on top. There are no child controls, buttons, labels, or text.

| # | Control | Type | Asset (gumpid) | x | y | w | h | Notes |
|---|---------|------|----------------|---|---|---|---|-------|
| 1 | Window background (small) | Gump sprite | `5010` (0x1392) | 0 | 0 | gump UV w | gump UV h | Used when `_useLargeMap == false`. Square radar frame. Native UV size determines window size. |
| 1b | Window background (large) | Gump sprite | `5011` (0x1393) | 0 | 0 | gump UV w | gump UV h | Used when `_useLargeMap == true`. |
| 2 | Radar pixels | Procedural texture | — | 0 | 0 | = bg w | = bg h | Per-cell 2px stamps baked over the bg's blank `0xFF080808` pixels. Color from land/static/multi radar color. Centered on player at `(w>>1, h>>1)`. |
| 3 | Mobile dots | Solid 2x2 rect | — (solid color tex) | `w/2 + (dx-dy)` | `h/2 + (dx+dy)` | 2 | 2 | One per nearby `Mobile` except player. `dx = mob.X - player.X`, `dy = mob.Y - player.Y`. Hue = `Notoriety.GetHue(mob.NotorietyFlag)`. Only drawn while `_draw == true` (blink). |
| 4 | Player dot | Solid 2x2 rect | — (white) | `w/2` | `h/2` | 2 | 2 | Always at center; hue 0 (white). Only while `_draw == true`. |

Notes on coordinates:
- The radar is isometric-rotated: a world delta `(dx, dy)` maps to screen
  `(gx, gy) = (dx - dy, dx + dy)`, then offset by the gump center `(w/2, h/2)`.
- Pixel buffer is `Width * Height` (the bg gump's UV size). The blank bg color
  sentinel that radar pixels are allowed to overwrite is `0xFF080808`.

## Assets

| Asset | ID (dec / hex) | Kind | Role |
|-------|----------------|------|------|
| Small map bg | `5010` / `0x1392` | Gump | Window background + size + hit mask (default). |
| Large map bg | `5011` / `0x1393` | Gump | Window background + size + hit mask (toggled). |
| Mobile dot | — | SolidColorTexture | 2x2 px, hued by notoriety. |
| Player dot | — | SolidColorTexture (White) | 2x2 px, hue 0. |
| Notoriety hue | via `Notoriety.GetHue(NotorietyFlag)` | hue index | Per-mobile dot color. |
| Radar land color | `Hues.GetColor16(16384, color-0x4000)` | 16-bit color | Hued land cell. |
| Radar default color | `Hues.GetRadarColorData(color)` | 16-bit color | Land/static/multi radar color. |

Fonts: none. Hues used: notoriety hues (dots), radar color tables (terrain). No
text strings.

## Behaviors

| Behavior | Legacy source | ECS mechanism |
|----------|---------------|---------------|
| **Movable (drag)** | `CanMove = true` | `UIMovable` marker on root (via `UOGumpBundle`); `WindowDragPlugin.Drag` drives it. No reimplementation. |
| **Right-click closes** | `CanCloseWithRightClick = true` | `UIMovable` + `WindowDragPlugin.CloseOnRightClick` (despawn subtree). It is NOT a container, so the generic in-place despawn path applies. |
| **Topmost on click / z-stack** | UIManager bring-to-front | `GlobalZIndex` on root only (via `UOGumpBundle`); `WindowDragPlugin.Drag` bumps `UiZCounter` on focus. |
| **Click-capture to world** | `Contains` pixel-perfect | `WindowDragPlugin.ClaimSelectedFromMovable` + `UiHitTest.PixelHit`. Requires a pixel-mask hit-test case (see ECS plan). |
| **Pixel-perfect hit-test** | `Contains` reads cached blank-bg pixel mask | New `UiHitTest.PixelHit` case for the minimap kind: bbox reject + `Gumps.PixelCheck(bgId, lx, ly)` against the bg gump mask (the radar pixels only fill where the bg sprite is opaque, so the bg gump's own alpha mask is the correct hit mask — matches `Contains` semantics). |
| **Left double-click toggles size** | `OnMouseDoubleClick(Left) → ToggleSize()` | `On<UiDoubleClick>` observer on the root (Bevy.UI synthesizes `UiDoubleClick`); swap `MiniMapState.UseLargeMap`, swap bg `AssetId` + resize `Node.Width/Height`, rebuild radar. |
| **Blink (dots on/off every 500 ms)** | `Update()` toggles `_draw` on `Time.Ticks` 500 ms cadence | A `Res<MiniMapState>` (or `Local`) blink flag driven off `Res<Time>` (`Time.Total`), toggled every 500 ms in an Update system. The render command reads it to skip dots. **Use `Res<Time>`, never wall-clock.** |
| **Radar regenerate on player move / map change** | `CreateMiniMapTexture` re-runs when `_x/_y` changed or forced; `CreateMap` on `MapIndex` change | A per-frame render reads live player `WorldPosition` + `GameContext.Map` + nearby mobiles directly in the ClayUO custom render command (analogous to legacy `AddToRenderLists` regenerating every frame). No baked texture state needed in ECS if rendered live. |
| **Buttons / pages / scroll / hover / resize** | none | N/A — the minimap has none. Size toggle is the only "resize." |
| **Server-driven updates** | none | N/A — no packet opens or updates it. |
| **Persistence (`isminimized`)** | `Save/Restore` XML attr | Out of scope for v1 (ECS has no gump-save layer yet); document as open question. Default to small map. |

Buttons fire-on-release: N/A (no buttons). The double-click handler uses
`On<UiDoubleClick>` per the gump contract.

## Server packets

**None.** The minimap is purely client-side. It is opened by the top-bar Map
button (`GameActions.OpenMiniMap`), not by any incoming opcode. It reads, but is
not driven by, world state populated by movement/mobile packets (`0x20`, `0x77`,
`0x78`, etc.). No opcode opens, updates, or closes it.

## ECS implementation plan

### Plugin

- **File**: `src/ClassicUO.Ecs/UI/MiniMapPlugin.cs`
- **Shape**: `internal readonly struct MiniMapPlugin : IPlugin` with `Build(App)`.
- **Composed in**: `src/ClassicUO.Ecs/Boot.cs` (`CuoPlugin.Build`, alongside
  `TopBarPlugin` / `PaperdollPlugin`).

### Resources / Components

- `internal sealed class MiniMapState` resource (`app.AddResource(new MiniMapState())`):
  - `bool UseLargeMap` — current size (false = small `5010`, true = large `5011`).
  - `bool BlinkOn` — dot blink phase.
  - `float NextBlinkMs` — next toggle time against `Time.Total`.
- `internal struct MiniMapWindow` — marker on the window root for dedup
  (open-once / focus instead of duplicate) and double-click routing, mirroring
  `PaperdollWindow`.

No per-entity radar pixel buffer is required: render the radar live in the custom
command each frame (legacy already regenerates every frame in `AddToRenderLists`).

### Spawning

- Open path: the top-bar **Map** button. In `TopBarPlugin.Spawn`, wire
  `Buttons.Map`'s observer (currently unwired) to send a trigger / set a flag
  that `MiniMapPlugin` observes — OR have `MiniMapPlugin` add its own observer.
  Cleanest within ECS rules: add an `EventWriter<OpenMiniMapEvent>` (or a typed
  trigger) on the Map button's `On<UiClick>`, and an
  `EventReader<OpenMiniMapEvent>` / observer in `MiniMapPlugin` that spawns or
  focuses the window. (Map button in legacy opens on click; keep `On<UiClick>`
  per the buttons-fire-on-release rule.)
- Spawn via `GumpBuilder.SpawnUOGump(commands, bgId, Vector3.UnitZ, spawnPos, zCounter)`
  with `bgId = 0x1392` (small default), then `.Insert(new MiniMapWindow())`.
  `UOGumpBundle` gives `Node` + `UiCustom`(Gump) + `Interaction.None` + `UOGump`
  + `UIMovable` + `GlobalZIndex` for free. **No child entities** — the radar +
  dots are all drawn by one custom render command on the root.
- Dedup: before spawn, query existing `MiniMapWindow`; if found, bump
  `UiZCounter` + reinsert `GlobalZIndex` to focus (mirror
  `PaperdollPlugin.SpawnOnOpenPaperdoll`).
- Despawn on logout: `OnExit(GameState.GameScreen)` system despawns all
  `MiniMapWindow` roots (mirror `PaperdollPlugin.DisposeOnLogout`).

### Rendering — NEW ClayUO custom render command

The plain `UOCustomKind.Gump` only draws the static bg sprite. The minimap needs
to (a) draw the bg, (b) bake/draw the radar terrain pixels, (c) draw mobile +
player dots. This is a UO-specific primitive, so add a dedicated custom kind:

1. Add `MiniMap` to `UOCustomKind` enum (`src/ClassicUO.Ecs/UI/GuiPlugin.cs`).
   The root's `UOCustomRender.AssetId` carries the current bg gump id
   (`0x1392`/`0x1393`); `Kind = UOCustomKind.MiniMap`.
2. Add a `case UOCustomKind.MiniMap` in `GuiRenderingPlugin.DrawCustom`
   (`src/ClassicUO.Ecs/Rendering/GuiRenderingPlugin.cs`). It must:
   - Draw the bg gump (`assets.Gumps.GetGump(custom.AssetId)`).
   - Generate the radar pixels into a per-window cached `Texture2D` (mirror
     `CreateMiniMapTexture`): walk map blocks around the player using
     `UOFileManager.Maps` + `Hues.GetRadarColorData` / `GetColor16`, write the
     2-px isometric stamps, then draw it over the bg.
   - Draw mobile dots (2x2 solid, `Notoriety.GetHue`) and the white player dot,
     gated on `MiniMapState.BlinkOn`.
   - Respect `cmd.ZIndex`.
   - **Data plumbing problem**: the renderer's `DrawCustom` only receives
     `AssetsServer` + the command. It needs `UOFileManager` (map blocks), live
     player position, nearby mobiles, and `MiniMapState.BlinkOn`. Two options:
     - (A) Carry the needed snapshot on `UOCustomRender` (extend it with minimap
       fields: player X/Y, map index, blink flag, and a precomputed list of dot
       positions+hues). A dedicated Update system in `MiniMapPlugin` fills these
       each frame from `Query<WorldPosition>` (player + mobiles), `GameContext`,
       and `MiniMapState`. This keeps the renderer dependency-free and is the
       house pattern (the render payload is a mutable reference object updated
       post-layout, like `UpdateUOButtonsState`).
     - (B) Add `Res<UOFileManager>` to the render system signature and resolve
       player/mobiles via a side channel. More coupling; prefer (A).
   - **Recommended: option (A).** Extend `UOCustomRender` (or add a sibling
     reference payload referenced from it) with: `bool MiniMapLarge`,
     `bool MiniMapBlink`, `ushort PlayerX`, `ushort PlayerY`, `int MapIndex`,
     and an array/list of `(int gx, int gy, ushort hue)` dots. The radar terrain
     baking still needs `UOFileManager.Maps`; bake it in the same per-frame
     update system into a cached `Texture2D` carried on the payload, and have the
     renderer just `Draw` it. This keeps the heavy map walk in an ECS system
     (with proper `Res<UOFileManager>` access) rather than the render path.
3. Add a `case UOCustomKind.MiniMap` in `UiHitTest.PixelHit`
   (`src/ClassicUO.Ecs/UI/UiHitTest.cs`): bbox reject, then
   `assets.Gumps.PixelCheck(custom.AssetId, lx, ly)` against the bg gump mask
   (identical mapping to the existing `UOCustomKind.Gump` case, since the
   minimap draws at native size). This reproduces legacy `Contains`, which keyed
   off the blank bg pixel mask.

### Systems / observers (in `MiniMapPlugin.Build`)

- `app.AddResource(new MiniMapState());`
- Open observer/reader: spawn-or-focus `MiniMapWindow` on the Map-button event.
- Per-frame update system (Stage.Update): drives blink off `Res<Time>`
  (`Time.Total` >= `NextBlinkMs` → flip `BlinkOn`, `NextBlinkMs += 500`), and
  rebuilds the dot list + radar texture into the root's `UOCustomRender` payload
  (queries player `WorldPosition`, all mobiles' `WorldPosition` + `Graphic`/
  notoriety, `Res<GameContext>` for map index, `Res<UOFileManager>` for blocks,
  `Res<MiniMapState>`). Uses `Query`/`Res`, never `World`.
- Double-click observer: `app.AddObserver` on `On<UiDoubleClick>` filtered to
  `MiniMapWindow` — flips `MiniMapState.UseLargeMap`, then updates the root's
  `UOCustomRender.AssetId` to `0x1393`/`0x1392` and resizes `Node.Width/Height`
  to the new bg UV size (mutate the queried `Node.Ref` in place — no Commands
  needed for field mutation).
- Despawn-on-logout system: `OnExit(GameState.GameScreen)`.

All mutation of structural state (spawn/despawn/focus z reinsert) goes through
`Commands`. Component-field edits (Node size, payload fields) use the mutable ref
from a query. No `World` access anywhere. Blink uses `Res<Time>`.

## How to trigger for capture

1. Boot ModernUO (`127.0.0.1:2593`, `admin/admin`) and the ECS client
   (`cuo-ecs`), log a character fully into the world (GameScreen state — the
   top bar only spawns `OnEnter(GameState.GameScreen)`).
2. The top bar appears at the top-left. Click the **Map** button (leftmost
   labeled "Map", a small `0x098B` button at x≈30, y≈1). Legacy maps this to
   `GameActions.OpenMiniMap`. In ECS this requires the Map-button wiring from the
   implementation plan to be in place.
3. The small minimap window appears (default spawn near the cursor / staggered
   position). It shows the radar terrain around the player with blinking dots.
4. For the large variant: **left double-click** anywhere on the minimap window.
5. Reference screenshot tip: stand somewhere with varied terrain + a few nearby
   NPCs so the radar pixels and notoriety-hued dots are visible. Capture a frame
   while `BlinkOn` (dots visible); the harness is deterministic if `Time` is
   stepped.

Harness: prefer `tools/agent-desktop` loop — `up --persist`, drive to GameScreen,
`rpc-click` the Map button, `rpc-shot`. See `tools/agent-desktop/AGENTS.md`.

## Open questions

- **Renderer data plumbing**: confirm whether extending `UOCustomRender` with
  minimap fields (option A) is acceptable, or whether a dedicated sibling payload
  type referenced from the command is preferred. Option A matches the existing
  "mutable reference payload" pattern but bloats the shared struct for one gump.
- **Radar texture caching**: legacy mutates the bg gump texture in place and
  caches blank-pixel arrays in a static. In ECS, where should the per-window
  radar `Texture2D` live and who disposes it on window despawn? Proposal: own it
  on the payload object and dispose in the despawn path. Needs a disposal hook.
- **`UOFileManager.Maps` access in ECS**: confirm `Res<UOFileManager>` exposes
  the same `Maps.MapBlocksSize` + block read API the legacy `CreateMiniMapTexture`
  uses (it reads `World.Map.GetIndex` / `GetChunk` / `StaticFile` / `MapFile`).
  The ECS `TerrainPlugin` already reads map blocks — reuse its accessor pattern
  rather than `World.Map`.
- **Multi/static "obj" layer**: legacy walks `block.Tiles[x,y]` live game objects
  (multis) for radar color. ECS world objects live as entities, not a `Chunk`
  tile grid. v1 may render terrain + statics only (from map/statics files) and
  skip the live-multi overlay; confirm acceptable for parity v1.
- **Mobile enumeration + notoriety**: confirm how to enumerate nearby mobiles and
  resolve `NotorietyFlag` in ECS (legacy iterates `World.Mobiles.Values`). Need
  the component carrying notoriety to hue the dots.
- **Persistence (`isminimized`)**: ECS has no gump-save layer; small map is the
  v1 default. Confirm deferring save/restore.
- **Top-bar Map button wiring**: the Map button observer is currently unwired in
  `TopBarPlugin`. Confirm the open mechanism (event vs. direct observer in
  `MiniMapPlugin`).
