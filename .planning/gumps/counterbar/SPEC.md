# Counter Bar Gump Spec

## Overview

The Counter Bar is a **client-side, fully local** floating gump that shows a resizable grid of "counter" cells. Each cell tracks the total quantity of a particular item graphic (optionally hue-filtered) that the player currently carries in their backpack (recursively, including sub-containers), and renders that item's art with a live count overlay. It is purely a client convenience widget — the server is never told the bar exists, and it produces no network traffic on its own (it only triggers normal `DoubleClick` / `DropItem` actions when the player uses or fills a slot).

When it appears:
- Not server-pushed. It is created locally when the user enables "Show Counters" in Options (`OptionsGump.cs:4216` → `new CounterBarGump(World, 200, 200, CounterBarCellSize)`), or restored on login from the saved profile gump XML (`Profile.cs:550` `GumpType.CounterBar` → `new CounterBarGump(world)` → `Restore`).
- Starts empty showing a help-text prompt ("Drag items here…"), and the player fills slots by dragging items onto it.

The grid is a wrapping flow of square cells of `_rectSize` px (clamped 30–80). Cells are populated by the user (drag-drop or context-menu "Add" placeholder), persisted to the profile XML, and each cell polls the player's inventory every 100 ms to refresh its displayed count.

## Source of truth

Client (legacy OOP) files — the contract:

- `src/ClassicUO.Client/Game/UI/Gumps/CounterBarGump.cs`
  - `class CounterBarGump : ResizableGump` (`:16`)
  - Const sizing: `MIN_SIZE=30`, `MAX_SIZE=80` (`:18-19`); `BORDER_LEFT/RIGHT/TOP/BOTTOM = 2` (`:20-23`)
  - `HELP_TEXT_HUE = 0x32` light yellow (`:25`)
  - `ReadOnly` property = `!ShowBorder` (`:34-40`)
  - ctor `(World)` → `:42`; ctor `(World,x,y,rectSize=30)` → `:97`
  - `SnapToGrid()` (`:54`) — rounds W/H down to whole cells; empty bar forced ≥ `6*_rectSize + borders` wide for help text
  - `ConfigureContextMenu` / `ToggleReadOnly` (`:73`, `:88`) — "Add", "Read-only on/off"
  - `AddPlaceholder()` (`:112`) — adds an empty `CounterItem(this,0,0,0)`
  - `SetCellSize(int)` (`:118`) — clamp + set MinW/MinH + relayout
  - `BuildGump()` (`:142`) — background AlphaBlend(0.7) + scissor + DataBox
  - `OnResize` / `SetupLayout()` (`:184`, `:194`) — wrap-flow layout, help-text spawn/teardown
  - `OnMouseDoubleClick` (`:257`) — left-double-click toggles ReadOnly
  - `OnMouseUp` (`:268`) — drop held item onto bar → new CounterItem + `GameActions.DropItem`
  - `UseSlot(string)` (`:290`) — macro entrypoint, 1-based slot index → `item.Use()`
  - `Save` / `Restore` (`:312`, `:340`) — XML persistence (rectsize, width, height, readonly, per-control graphic/hue/compareto)
  - `GumpType.CounterBar` (`:140`)

- `src/ClassicUO.Client/Game/UI/Gumps/CounterBarGump.CounterItem.cs`
  - `class CounterItem : Control` (`:23`)
  - Const: `UPDATE_INTERVAL=100` ms, `DRAG_OFFSET=22`, `HIGHLIGHT_AMOUNT_CHANGED_DURATION=5000f`, up-hue `1165` (icelight), down-hue `1166` (firelight) (`:25-29`)
  - Fields: `_amount`, `_lastChangeTime`, `_image` (ImageWithText), `_background` (AlphaBlend 0.0), `CompareTo`, `Graphic`, `Hue?` (`:30-57`)
  - `SetGraphic` (`:59`), `ConfigureContextMenu` (`:69`) — "Use Object", "Compare To", "Ignore Hue on/off"
  - `ToggleIgnoreHue` (`:84`), `CompareToSelected`/dialog (`:98-121`)
  - `RemoveItem` (`:123`), `Use` (`:137`) — `FindItem(Graphic, Hue ?? 0xFFFF)` → `GameActions.DoubleClick`
  - `OnMouseOver`/`OnMouseExit` (`:152`,`:167`) — tooltip from backpack item
  - `OnDragBegin`/`OnDragEnd`/`FinalizeDragDrop` (`:173`,`:196`,`:212`) — drag a cell out into a `DraggableGump`, drop reorders/moves between bars
  - `OnMouseUp` (`:242`) — Left: drop held item / cast-by-one-click; Right+Alt: remove; Right: passthrough
  - `OnMouseDoubleClick` (`:289`) — left double-click → `Use()` (unless CastSpellsByOneClick)
  - `CalculateDisplayAmount` = `_amount - CompareTo` (`:302`)
  - `Update()` (`:307`) — 100 ms poll → `GetTotalAmountOfItem` → highlight anim → set label text
  - `UpdateOnChangeAnimation` (`:341`), `CalculateDisplayAmountText`/`CalculateAmountPrefix` (`:360`,`:385`)
  - `AddToRenderLists` (`:408`) — draws cell border rectangle (Yellow hover / Red low-amount / Gray default)
  - nested `class ImageWithText : Control` (`:435`) — draws the item art centered + a count `Label` at bottom-left, font index 1 hue `0x35` BlackBorder
  - `DraggableGump` (`CounterBarGump.DraggableGump.cs`) — transient drag carrier

- `src/ClassicUO.Client/Game/UI/Gumps/ResizableGump.cs`
  - Border thickness **4** (`BorderControl(0,0,W,H,4)` `:39-46`); `BoderSize => 4`
  - Resize handle is a `Button(0, 0x837, 0x838, 0x838)` bottom-right (`:52`)
  - `ShowBorder` toggles border + resize button visibility (`:79`)

- `src/ClassicUO.Client/Game/GameObjects/PlayerMobile.cs:235` — `GetTotalAmountOfItem(graphic, hue?)`: walks player's equipped layers OneHanded..Legs; recurses containers via `GetTotalAmount`; matches `graphic` and (if hue set) `hue`, summing `Amount`. **This is the count source.** Hue==null means "any hue".

## Visual structure

The window is a **resizable transparent panel** (no UO gump-art background — unlike paperdoll/container). It is a `ResizableGump`: a 4px border frame (`BorderControl`) + a bottom-right resize-handle button, wrapping a semi-transparent fill and a wrap-flow grid of square cells.

```
CounterBarGump  (root, Width×Height, default 50×50 or 200×200; X,Y default 200,200)
├─ BorderControl          frame, 4px thick, drawn around the whole window (ResizableGump base)
│                         visible only when NOT ReadOnly  (ShowBorder == !ReadOnly)
├─ Button (resize handle) gumps 0x837 (normal) / 0x838 (over+pressed), bottom-right corner
│                         visible only when NOT ReadOnly
├─ AlphaBlendControl _background   at (4,4), size (W-8, H-8), alpha 0.7, solid dark fill
├─ ScissorControl (on)    clip rect (4,4,W-8,H-8)  — clips the DataBox grid
├─ DataBox _dataBox       at (4,4), holds the CounterItem cells laid out wrap-flow
│   └─ CounterItem ×N     each cell:
│       x = col*_rectSize + 2 (BORDER_LEFT), y = row*_rectSize + 2 (BORDER_TOP)
│       w = _rectSize - 4, h = _rectSize - 4   (minus BORDER_LEFT+RIGHT / TOP+BOTTOM = 2+2)
│       wrap when x + _rectSize > (W-8): x→0, y += _rectSize
│       ├─ AlphaBlendControl _background  (W,H), alpha 0..1 highlight pulse on amount change
│       │      hue 1165 (icelight) when amount went UP, 1166 (firelight) when DOWN;
│       │      alpha = max(0, 1 - (now - lastChange)/5000ms)  — fades over 5s
│       ├─ ImageWithText _image  (fills cell)
│       │   ├─ item ART centered (graphic), partial-hue aware, scaled down if oversized
│       │   └─ Label (count text)  at x=2, y=H-15, font index 1, hue 0x35, BlackBorder style
│       └─ cell border RECTANGLE drawn last (AddToRenderLists):
│              Yellow  if mouse over cell
│              Red     else if CounterBarHighlightOnAmount && displayAmount < HighlightAmount && Graphic!=0
│              Gray    otherwise
├─ ScissorControl (off)
└─ Label _helpTextLabel   ONLY when bar is empty: ResGumps.CounterEmptyHelpText,
       at (8,8) = (BORDER_LEFT*4, BORDER_TOP*4), W = (W-8) - 0, hue 0x32 (light yellow), multiline
```

Cell count text rules (`CalculateDisplayAmountText`, `CalculateAmountPrefix`):
- `displayAmount = _amount - CompareTo`
- empty string when `CompareTo==0 && displayAmount==1` (don't show "1" for a single item)
- prefix: `""` if `CompareTo==0`; `"±"` (the `�` literal in source is a placeholder glyph) if `displayAmount==0`; `"+"` if `>0`; `""` if `<0` (already signed)
- if `CounterBarDisplayAbbreviatedAmount` and `abs(displayAmount) >= CounterBarAbbreviatedAmount`: use `StringHelper.IntToAbbreviatedString` (e.g. "1.2k")

## Assets

| Asset | ID | Kind | Where |
|-------|-----|------|-------|
| Resize handle button — normal | `0x837` | Gump | ResizableGump base resize button |
| Resize handle button — over/pressed | `0x838` | Gump | ResizableGump base resize button |
| Cell item icon | per-cell `Graphic` (ushort, static art id) | **Art (statics)** | `ImageWithText.AddToRenderLists` `Arts.GetArt` |
| Window background | *(none — solid AlphaBlend fill, not a gump sprite)* | — | `_background` AlphaBlendControl(0.7) |
| Cell border / fills | white solid texture (`SolidColorTextureCache`) | rectangle | cell border + AlphaBlend |

Hues / fonts:

| Use | Value |
|-----|-------|
| Help-text label hue | `0x32` (light yellow) |
| Count label hue | `0x35` |
| Count label font | UO bitmap **font index 1**, style `FontStyle.BlackBorder` |
| Amount-increased highlight hue | `1165` (icelight) |
| Amount-decreased highlight hue | `1166` (firelight) |
| Item art hue | per-cell `Hue` (nullable; null = ignore hue when counting), partial-hue from `StaticData[graphic].IsPartialHue` |
| Cell border (hover) | `Color.Yellow` |
| Cell border (low amount) | `Color.Red` |
| Cell border (default) | `Color.Gray` |

Sizing constants:

| Name | Value |
|------|-------|
| Cell size `_rectSize` | clamp(profile `CounterBarCellSize`, 30, 80) |
| Border thickness (window) | 4 px (`BoderSize`) |
| Inner cell padding | 2 px each side (`BORDER_LEFT/RIGHT/TOP/BOTTOM`) |
| Default window X,Y | 200,200 (from Options) |
| Default size | 50×50 (base ctor) / restored from XML |
| Empty-bar min width | `6 * _rectSize + 4 (left) + 4 (right)` |
| Poll interval | 100 ms |
| Highlight fade duration | 5000 ms |
| Drag offset | 22 px |

## Behaviors

| Behavior | OOP source | ECS mechanism |
|----------|-----------|---------------|
| **Drag window to move** | `ResizableGump`/`Gump` `CanMove=true` | `UIMovable` marker on the root → `WindowDragPlugin.Drag` (no per-gump code). **NOTE:** OOP disables right-click-close (`CanCloseWithRightClick=false`). See Open Questions — ECS `CloseOnRightClick` closes any `UIMovable`. To match OOP, the root must NOT carry `UIMovable`, OR we accept right-click-close as the ECS close path (recommended). |
| **Right-click close** | OOP: disabled on the bar (closed only via Options toggle). Cells: Right (no Alt) passthrough; Right+Alt removes the cell. | ECS canonical close = right-click on `UIMovable` (`WindowDragPlugin.CloseOnRightClick`). Recommend adopting that. Cell "Right+Alt = remove" handled by a per-cell `On<UiPointerUp>`/`On<UiClick>` observer reading `Res<KeyboardModifiers>` (Alt) — only when NOT read-only. |
| **Topmost-on-click** | UIManager bring-to-front | `GlobalZIndex` on root only; `WindowDragPlugin` bumps via `UiZCounter` on drag latch. Falls out of `UOGumpBundle`. |
| **Click-capture vs world** | gump consumes clicks | `WindowDragPlugin.ClaimSelectedFromMovable` (Stage.Last). The bar has no gump-sprite bg, so hit-test must use `UOCustomKind.None` (solid bbox) on the root surface — pixel-hit would always pass through. |
| **Drop held item onto bar → new cell** | `CounterBarGump.OnMouseUp` `:268` | A system on left mouse-up that, when `Res<GrabbedItem>` has a held item AND the cursor is over the bar's bbox AND not read-only: `Commands` spawn a new cell child with the held graphic/hue, then send the existing `DropItem` flow (re-drop into original container). |
| **Drop held item onto an existing cell** | `CounterItem.OnMouseUp` Left `:246` | Same, but `SetGraphic` on the targeted cell instead of spawning a new one. |
| **Double-click cell → Use item** | `CounterItem.OnMouseDoubleClick` `:289` (unless CastSpellsByOneClick) | Per-cell `On<UiDoubleClick>` observer → resolve `FindItem(Graphic, Hue ?? 0xFFFF)` in player's bag → `net.Send_DoubleClick(serial)`. (Mirror PaperdollBackpackUI/ContainerItemUI dclick observers.) |
| **Single-click cell → Use (cast-by-one-click)** | `CounterItem.OnMouseUp` Left `:265` | Per-cell `On<UiClick>` observer gated on `Res<Profile>.CastSpellsByOneClick`. |
| **Double-click window → toggle ReadOnly** | `CounterBarGump.OnMouseDoubleClick` `:257` | `On<UiDoubleClick>` observer on the root → flip a `CounterBarWindow.ReadOnly` flag; toggle border/handle child `Display` + suppress drag-out. |
| **Live count refresh (100 ms poll)** | `CounterItem.Update` `:307` | A system in `Stage.Update` gated on a 100 ms accumulator (`Res<Time>`, per-cell `NextTickMs` field on the cell component) that recomputes the total from the player's inventory queries and writes the label + highlight pulse. **No `World` access** — query the player's `Items`/container children via ECS queries. |
| **Amount-change highlight pulse** | `UpdateOnChangeAnimation` `:341` | In the same poll system: when new total ≠ old, set cell `_background` hue (1165 up / 1166 down) + reset `LastChangeMs`; each frame compute alpha = `max(0,1-(now-LastChangeMs)/5000)` and write it into the cell-bg `UOCustomRender.Hue.Z`/alpha. Gated on profile `CounterBarHighlightOnChange`. |
| **Low-amount red border** | `AddToRenderLists` `:416` | Cell border color rule needs a render primitive (see ECS plan). Computed each frame from `displayAmount < CounterBarHighlightAmount`. |
| **Hover yellow border + tooltip** | `OnMouseOver` `:152`, `AddToRenderLists` Yellow | Hover state from `Interaction.Hovered` on the cell; border color switches; tooltip via existing tooltip infra keyed on the resolved backpack item. |
| **Drag a cell out / reorder** | `CounterItem.OnDragBegin/End` `:173` | Lower priority (v2). Mirror `PickupPlugin` latch (bbox+hit). Reorder = move cell child within parent / between bars. Gated on NOT read-only. |
| **Resize (snap to cell grid)** | `ResizableGump` + `SnapToGrid` `:54` | The bottom-right resize button is its own widget; on drag it changes root `Node.Width/Height`, then `SnapToGrid` rounds down to whole `_rectSize` cells (and enforces empty-bar min width). Re-runs the wrap-flow layout. |
| **Empty-bar help text** | `SetupLayout` `:215` | When the cell list is empty, spawn the help `Text` child (hue 0x32); despawn it when the first cell is added. |
| **Context menu (Add / Read-only / per-cell Use/CompareTo/IgnoreHue)** | `ConfigureContextMenu` | No ECS context-menu infra yet — see Open Questions. |
| **Macro `UseSlot`** | `CounterBarGump.UseSlot` `:290` | Out of scope for v1 (macro system not yet ported). |
| **Persistence (Save/Restore XML)** | `Save`/`Restore` `:312` | Out of scope for v1 (ECS gump-persistence layer not present). Note as open question. |

## Server packets

**None.** The Counter Bar is entirely client-side. It opens no gump packet and updates from local inventory state. Indirect outgoing packets when the user interacts:
- Use a slot (double-click / one-click cast): `Send_DoubleClick` (0x06) on the resolved backpack item.
- Drop a held item onto the bar/cell: the normal pickup/drop flow → `Send_DropRequest` / `DropItem` (0x08 / 0x07), exactly as the existing `PickupPlugin`/`ContainerGumpPlugin` drop path does.

Inventory count is recomputed from local entity state, which is itself fed by container-content packets (0x3C `OnContainerContent`, 0x25 `OnUpdateItemInContainer`, etc.) — but the bar reads ECS components, not packets.

## ECS implementation plan

**Plugin:** `CounterBarPlugin` → `src/ClassicUO.Ecs/Gameplay/CounterBarPlugin.cs`

Conforms to CLAUDE.md ECS rules: no `World` access, all mutation via `Commands`, single-entity reads/checks via `Query.Contains`/`Get`, singletons via `Res`/`ResMut`, system→system interop via observers, time via `Res<Time>`.

### Components / markers

```csharp
// Root window. Only the root carries GlobalZIndex/UIMovable (UOGumpBundle).
internal struct CounterBarWindow
{
    public int CellSize;      // _rectSize, clamped 30..80
    public bool ReadOnly;     // toggled by window double-click; hides border + handle, blocks edits
    public float WidthPx;     // current window W (drives wrap-flow)
    public float HeightPx;
}

// One per cell. Lives as a child of the window root (or of an inner DataBox child).
internal struct CounterBarCell
{
    public ulong WindowEntity;
    public ushort Graphic;    // 0 = empty placeholder
    public ushort Hue;        // ignored when HueIsNull
    public bool HueIsNull;    // Hue==null in OOP -> "any hue" when counting
    public int CompareTo;
    public int LastAmount;    // previous poll total (for change detection)
    public float NextTickMs;  // Res<Time>.Total gate (100ms poll)
    public float LastChangeMs;// for the 5s highlight fade
    public int SlotIndex;     // ordering, for layout + macro slot lookup
}

// Marker on the cell's background fill child (highlight pulse target).
internal struct CounterBarCellBg { public ulong CellEntity; }
// Marker on the cell's count label child.
internal struct CounterBarCellLabel { public ulong CellEntity; }
```

### Resources

- Reuse `Res<UiZCounter>`, `Res<AssetsServer>`, `Res<GumpBuilder>`, `Res<Time>`, `Res<NetClient>`, `Res<GrabbedItem>`, `Res<MouseContext>`.
- Player inventory access: query the player's items via existing ECS queries (the same components `ContainerGumpPlugin` reads — `Graphic`, `Hue`, `Amount`, container `Children`, `NetworkSerial`). A small helper mirrors `GetTotalAmountOfItem` walking the player's equipped/contained items. **Do not** call `World.*`.
- Profile flags (`CounterBarHighlightOnChange`, `CounterBarHighlightOnAmount`, `CounterBarHighlightAmount`, `CounterBarDisplayAbbreviatedAmount`, `CounterBarAbbreviatedAmount`, `CastSpellsByOneClick`) — read from whatever `Res<Profile>`/settings resource the ECS branch exposes; if absent, hard-code OOP defaults and note in Open Questions.

### Bundle usage

The bar has **no UO gump-sprite background** — it is a translucent panel. Two viable approaches:

1. **Preferred:** spawn the root with `UOGumpBundle { Kind = UOCustomKind.None }` (invisible solid-bbox hit surface → drag/right-click-close/click-capture all work via `UIMovable`), then add a child solid-fill rectangle for the 0.7-alpha background. The `None` kind already exists and `UiHitTest.PixelHit` returns bbox-solid `true` for it, which is exactly what a transparent-but-clickable panel needs (OOP's `AlphaBlendControl` captures clicks across its whole rect).
2. Draw the translucent fill via a `RenderCommandType.Rectangle` child node with a low-alpha `BackgroundColor` (Clay-native rectangle, already supported by `GuiRenderingPlugin.DrawRectangle`).

Cells:
- Item icon child: `GumpBuilder.AddArtSized(commands, graphic, hue, pos, new Vector2(cellW, cellH))` (`UOCustomKind.Art`, clamps/centers like paperdoll slots).
- Count label child: `GumpBuilder.AddLabel(...)` — **needs font index 1** (current `AddLabel` hard-codes `FontId=0`); pass an explicit `TextFont { FontId = 1 }` + `TextColor` for hue 0x35. Position at `(2, cellH-15)`.
- Cell bg / highlight pulse + cell border: see new render primitive below.

### Observers (system→system interop, rule 4)

- `On<UiDoubleClick>` on each cell → `Use` (resolve player bag item by graphic/hue, `Send_DoubleClick`). Mirrors `ContainerGumpPlugin`'s dclick observer.
- `On<UiDoubleClick>` on the root → toggle `CounterBarWindow.ReadOnly`; flip border/handle child `Display`.
- `On<UiClick>` on each cell (gated on `CastSpellsByOneClick`) → `Use`.
- `On<UiPointerUp>` on each cell (Right + Alt held, not read-only) → remove the cell (`Commands.Despawn` + relayout request).
- Optional: an `OnInsert`/`OnRemove<CounterBarCell>` observer to trigger relayout (so add/remove reflows the grid without a polling system), mirroring Paperdoll's `OnInsert<EquipmentSlots>` rebuild pattern.

### Systems

| System | Stage | Purpose |
|--------|-------|---------|
| `PollCounts` | `Update` | For each cell whose `NextTickMs <= Time.Total`: recompute total from player inventory queries; if changed, set highlight hue + `LastChangeMs`; write label text; set `NextTickMs = Total + 100`. |
| `FadeHighlight` | `Update` | Each frame, set each cell-bg alpha = `max(0, 1-(Total-LastChangeMs)/5000)` (only if `CounterBarHighlightOnChange`). |
| `Layout` | `Update` (run on dirty) | Wrap-flow: place cells at `(col*size+2, row*size+2)`, size `size-4`, wrap when `x+size > W-8`. Spawn/despawn help-text. Mirror `SetupLayout`. Trigger via a `CounterBarDirty` tag or the add/remove observer. |
| `DropOntoBar` | `Update`, `RunIf` left mouse-up + grabbed item | Cursor over bar bbox + not read-only → spawn new cell (or `SetGraphic` an existing cell under cursor) + run the normal drop flow. |
| `Resize` (v2) | `Update` | Resize-handle widget drag → set root W/H → `SnapToGrid` → mark dirty. |

### New ClayUO custom render command + UiHitTest

The cell **border rectangle** (Yellow/Red/Gray, hollow 1px outline) and the per-cell **alpha-blend highlight fill** are not expressible with the existing primitives as cleanly as OOP draws them:

- The translucent fill and the highlight pulse can both use the existing `RenderCommandType.Rectangle` (`DrawRectangle`) with a per-frame-updated `BackgroundColor` (RGBA incl. alpha) — **no new primitive needed** for fills.
- The **hollow cell border** (1px outline, color by state) has no current primitive. Options:
  1. Add a `ClayUOCommandType.RectangleOutline` (or reuse Clay's `Border` render command — `GuiRenderingPlugin` currently no-ops `Border`; implementing it covers this). **Recommended:** implement the existing Clay `Border` render command in `GuiRenderingPlugin` (draw 4 thin `white`-texture rects from `cmd.Border` color/width). Then a cell node with a `Border` component renders the outline; a small post-layout/poll system sets the border color (Yellow/Red/Gray) by state.
  2. Or add a dedicated `UOCustomKind`/`ClayUOCommandType` that draws the bordered cell in one custom command. Heavier; not preferred.

`UiHitTest`: the cells are `UOCustomKind.Art` (item icon) — already covered by the existing `Art` case (centered/clamped). The window root uses `UOCustomKind.None` → already returns bbox-solid `true`, which is the desired "translucent panel captures clicks" behavior. **No new `UiHitTest` case required** if we go with approach (1)/(2) above for the panel + Clay `Border` for the outline.

If instead a new `ClayUOCommandType` is added for the cell, follow CLAUDE.md "Custom Rendering": add the enum value in `GuiPlugin.cs` `ClayUOCommandType`, a `case` in `GuiRenderingPlugin` custom switch, and a matching `UiHitTest.PixelHit` case (bbox-solid is fine for a filled cell).

### Composition

Register `CounterBarPlugin` in `src/ClassicUO.Ecs/Boot.cs` (`CuoPlugin.Build`). Open trigger (v1): since there is no server packet and no ECS Options gump yet, add a debug/keybind entry point (or an `app.AddSystem(OnEnter(GameState.GameScreen))` that spawns an empty bar when a `CounterBarEnabled` flag is set) so the harness can open it. Mirror Paperdoll's `DisposeOnLogout` for `OnExit(GameState.GameScreen)`.

## How to trigger for capture

The bar is **not** opened by a top-bar button, keybind, or server packet by default in the ECS build — there is no ECS Options gump yet. To get a reference screenshot:

OOP reference client (source of truth visuals):
1. Boot the client, log into ModernUO (`127.0.0.1:2593`, admin/admin), enter world.
2. Open Options (paperdoll → Options, or the system menu) → "Counters" tab → check **"Show Counters"** (sets `CounterBarEnabled`). An empty bar appears at (200,200) showing the help text.
3. Drag a stackable item from your backpack (e.g. gold, bandages, reagents) onto the bar → a cell appears showing the item art + live count. Add several to fill the grid.
4. Hover a cell (yellow border), let an item's count drop below the highlight threshold (red border), and watch the icelight/firelight pulse when a count changes.

ECS build / harness:
- Add a temporary spawn hook (debug keybind or `OnEnter(GameScreen)` with a forced-enabled flag) that calls the `CounterBarPlugin` spawn path, since the ECS Options gump that would normally toggle it is not yet ported. Then drive with `agent-desktop` `rpc-click` to drop items and `rpc-shot` for the screenshot.

Required game state for a populated capture: logged-in player with a backpack containing at least one stackable item (gold/bandages/reagents) so a cell shows a non-trivial count.

## Open questions

1. **Right-click close vs OOP behavior.** OOP sets `CanCloseWithRightClick=false` on the bar (it's toggled via Options, not closed by right-click), and uses right-click+Alt on a *cell* to remove that cell. The ECS shared contract closes any `UIMovable` on right-click. Decision needed: (a) adopt ECS right-click-close for the whole bar (simplest, matches house style) and put cell-remove on a different gesture, or (b) keep the bar non-closable and special-case it. Recommend (a) unless parity is strict.
2. **Profile / settings resource.** Which ECS resource exposes the `CounterBar*` profile flags + `CastSpellsByOneClick`? If none exists yet, hard-code OOP defaults (`HighlightOnChange=on`, `HighlightOnAmount`/`HighlightAmount`, `AbbreviatedAmount`) and wire later.
3. **Persistence.** OOP saves/restores the bar (cells, size, readonly) to profile gump XML. The ECS branch has no gump-persistence layer in evidence — v1 likely spawns a fresh empty bar each session. Confirm scope.
4. **Open trigger.** No server packet and no ECS Options gump. Need a sanctioned entry point (debug keybind / top-bar / Options port) to spawn the bar in the ECS build.
5. **Player inventory enumeration in ECS.** Confirm the exact queryable shape of the player's carried items + nested container contents (component set + parent/children relations) so `GetTotalAmountOfItem` can be reproduced without `World` access. `ContainerGumpPlugin`/`NetworkEntitiesMap` are the closest references.
6. **Context menus.** OOP uses `ContextMenuControl` for Add / Read-only / Use / Compare-To / Ignore-Hue. No ECS context-menu infra was found. Either defer those actions or build a minimal context-menu widget. The "Compare To" entry-dialog and macro `UseSlot` are also unported and likely out of v1 scope.
7. **Count label font.** `GumpBuilder.AddLabel` hard-codes `FontId=0`; the cell count uses UO font index **1** with `BlackBorder`. Confirm the ECS text path can select font 1 (and emulate the black border) — paperdoll's title label has the same FontId=0 limitation noted in its source.
8. **Resize handle widget.** No generic resizable-window widget exists in the ECS UI yet (paperdoll/container are fixed-size). The bottom-right resize handle + `SnapToGrid` is net-new infra; consider deferring resize to v2 and shipping a fixed-grid bar first.
