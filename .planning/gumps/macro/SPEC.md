# Macro Gump Spec

## Overview

There are TWO distinct, related client-side UI surfaces in the macro system, both built on the same `MacroControl` editor control:

1. **Macro editor window (`MacroGump`)** — a small floating "fast-assign" window titled `Edit macro: {name}`. 260x200, semi-transparent dark background. It hosts a single `MacroControl` (constructed with `isFastAssign = true`) which lets the user assign a hotkey, create the macro button, open full macro settings, and edit the macro's action list. Opened by `GameActions.OpenMacroGump(world, name)` (only one instance allowed; reopening disposes the previous). NOT server-driven — purely client-side.

2. **Macro button (`MacroButtonGump`)** — a tiny 88x44 draggable, anchorable button that fires/executes a saved macro. Carries a centered label with the macro name. Hover-highlights. Single-click (when `CastSpellsByOneClick`) or double-click executes the macro. Created from inside `MacroControl` ("Create new macro button" action) and persisted/restored via `macros.xml` / gump save XML.

The full macro editor also appears embedded (not as its own window) inside `OptionsGump` page 4 (the "Macros" options tab), using the SAME `MacroControl` with `isFastAssign = false` at fixed position `(400, 20)`. That embedded usage is out of scope for a standalone gump port but the control tree is identical except for the extra Add/Remove buttons and a larger scroll area (see Visual structure).

This spec covers `MacroGump` (the fast-assign editor window) and `MacroButtonGump` (the executable button) as standalone ECS gumps, plus the shared `MacroControl` subtree.

## Source of truth

- `src/ClassicUO.Client/Game/UI/Gumps/MacroGump.cs` — the fast-assign editor window.
  - ctor `MacroGump(World, string name)` lines 9-43: `CanMove=true`, `CanCloseWithRightClick=true`; `AlphaBlendControl` bg 260x200 at `(camera.W/2 - 125, 150)` alpha 0.8; `Label "Edit macro: {name}"` font 1 (unicode) at `(camera.W/2 - 105, bg.Y+2)`; `MacroControl(this, name, true)` at `(bg.X+20, bg.Y+20)`; `SetInScreen()`.
- `src/ClassicUO.Client/Game/UI/Gumps/MacroButtonGump.cs` — the macro button.
  - ctor lines 22-41: `CanMove`, `AcceptMouseInput`, `CanCloseWithRightClick`, `WantUpdateSize=false`, `WidthMultiplier=2`, `HeightMultiplier=1`, `GroupMatrixWidth/Height=44`, `AnchorType=SPELL`.
  - `BuildGump()` lines 46-70: `Width=88 Height=44`; centered `Label(_macro.Name, unicode, hue 0x03b2, width=Width, maxWidth 255, BlackBorder, TS_CENTER)`, `label.Width=Width-10`, `label.Y = (Height>>1) - (label.Height>>1)`; `backgroundTexture = SolidColorTextureCache.GetTexture(Color(30,30,30))`.
  - `OnMouseEnter` lines 72-77: `label.Hue = 53`, bg = `Color.DimGray`.
  - `OnMouseExit` lines 79-84: `label.Hue = 0x03b2`, bg = `Color(30,30,30)`.
  - `OnMouseUp` lines 87-97: single-click run when `CastSpellsByOneClick && Left && !Alt && |LDragOffset| < 5px`.
  - `OnMouseDoubleClick` lines 99-109: double-click run when `!CastSpellsByOneClick && Left`.
  - `RunMacro` lines 111-119: `World.Macros.SetMacroToExecute(_macro.Items as MacroObject)` + `WaitForTargetTimer=0` + `World.Macros.Update()`.
  - `AddToRenderLists` lines 121-158: draws `backgroundTexture` filled rect `(x,y,88,44)` then `DrawRectangle(Color.Gray)` 1px border.
- `src/ClassicUO.Client/Game/UI/Controls/MacroControl.cs` — the shared editor control (the bulk of the visual contract).
  - ctor lines 32-130 (layout metrics below).
  - `buttonsOption` enum lines 24-30: `AddBtn, RemoveBtn, CreateNewMacro, OpenMacroOptions`.
  - `OnButtonClick` lines 336-358 (button actions).
  - `MacroEntry` inner class lines 361-527: the per-action row (combobox + optional sub-combobox or text-entry resizepic).
- `src/ClassicUO.Client/Game/Managers/MacroManager.cs` — the data model + execution engine.
  - `MacroManager` (a `LinkedObject`) lines 23-1843: `Load/Save` (macros.xml), `CreateDefaultMacros` 128-255, `GetAllMacros` 258-284, `FindMacro` overloads 287-353, `SetMacroToExecute` 355-358, `Update`/`Process` 360-1807 (the giant action dispatch — what a macro DOES).
  - `Macro` class lines 1846-2184: per-macro key/mods/mouse/wheel + `Items` linked list of `MacroObject`. `GetBoundByCode` lines 2108-2183 (sub-menu ranges).
  - `MacroObject` lines 2187-2253: `Code` (MacroType), `SubCode` (MacroSubType), `SubMenuType` (0=none, 1=combobox sub, 2=text-entry). `MacroObjectString` 2255-2268 adds `Text`.
  - `MacroType` enum lines 2270-2352; `MacroSubType` enum lines 2354-2605.
- `src/ClassicUO.Client/Game/UI/Controls/HotkeyBox.cs` — the hotkey capture box (210x25; inner resizepic 0x0BB8 150x25; centered hovered label font 1 hue 0x0021; OK btn 0x0481/3/2 at x=152; Cancel btn 0x047E/0480/047F at x=182).
- `src/ClassicUO.Client/Game/GameActions.cs` line 47-53: `OpenMacroGump`.
- `src/ClassicUO.Client/Game/UI/Gumps/OptionsGump.cs` lines 2090-2249: embedded `MacroControl` on Options page 4 (reference for the non-fast-assign variant).

## Visual structure

### A. MacroButtonGump (88 x 44)

No UO gump sprite. The background is a solid-color quad drawn directly + a 1px border. This is a CUSTOM render primitive (not a standard gump/art).

| Control | Type | Asset / Color | x | y | w | h | font / hue | text |
|---|---|---|---|---|---|---|---|---|
| Background fill | solid quad | `Color(30,30,30)` default / `Color.DimGray` on hover | 0 | 0 | 88 | 44 | — | — |
| Border | 1px rectangle | `Color.Gray` | 0 | 0 | 88 | 44 | — | — |
| Name label | unicode label, centered | — | 0 | `(44>>1)-(labelH>>1)` (~vertically centered) | `Width-10` = 78 | auto | font 1; hue `0x03b2` default / `53` on hover; `FontStyle.BlackBorder`; `TS_CENTER` | `macro.Name` |

### B. MacroGump (fast-assign editor window, 260 x 200)

Window origin `X = camera.Bounds.Width/2 - 125, Y = 150` (centered horizontally, `SetInScreen()` clamps to viewport).

- **Window root / background** — `AlphaBlendControl` 260x200 at window origin, `Alpha = 0.8`, color black. This is a translucent black box, NOT a UO gump sprite. (CUSTOM render: translucent fill, like `checkertrans` / `BackgroundColor` in `ServerGumpPlugin`.)
- **Title label** — unicode `Label("Edit macro: {name}", isunicode=true, font=15... )` Actually constructed as `new Label(text, true, 15)` → isunicode=true, hue 15. At `(bg.X + 20, bg.Y + 2)` (the source uses `camera.W/2 - 105` for X which equals `bg.X + 20`). Text `Edit macro: {name}`.
- **MacroControl** (isFastAssign=true) — at `(bg.X + 20, bg.Y + 20)`. See subtree C below. Child coordinates below are relative to the MacroControl origin.

### C. MacroControl subtree (relative coordinates)

`HotkeyBox.Height = 25`. Let `H = 25`.

| # | Control | Type | Asset / hue | x | y | w | h | text |
|---|---|---|---|---|---|---|---|---|
| 1 | Hotkey box | composite (resizepic + label + 2 buttons) | bg resizepic `0x0BB8` (150x25); label font 1 hue `0x0021` centered; OK button `0x0481/0x0483/0x0482` at x=152; Cancel `0x047E/0x0480/0x047F` at x=182 | 0 | 0 | 210 | 25 | shows key combo or empty |
| 2 | "Create macro button" | `NiceButton` (text button, ButtonAction.Activate) | param `CreateNewMacro` | 0 | `H+3` = 28 | 170 | 25 | `ResGumps.CreateMacroButton` |
| 3a (fast-assign only) | "Open macro settings" | `NiceButton` | param `OpenMacroOptions` | 0 | `H+30` = 55 | 170 | 25 | `ResGumps.OpenMacroSettings` |
| 3b (NON-fast-assign only) | "Add" | `NiceButton` | param `AddBtn` | 0 | `H+30` = 55 | 50 | 25 | `ResGumps.Add` |
| 3c (NON-fast-assign only) | "Remove" | `NiceButton` | param `RemoveBtn`, `TS_LEFT` | 52 | `H+30` = 55 | 50 | 25 | `ResGumps.Remove` |
| 4 | Scroll area | `ScrollArea` (vertical, with scrollbar) | — | 10 | `HotkeyBox.Bounds.Bottom + 80` = `25+80` = 105 | fast: 230 / full: 280 | fast: 80 / full: 280 | — |
| 5 | Data box | `DataBox` (vertical stack container, auto-size) | — | 0 | 0 | 280 | 280 | holds N `MacroEntry` rows |

NiceButton default font is the small bitmap font (font 0xFF / `9`), text hue 0x99 (gold), highlight-on-hover. They are text buttons, not gump-sprite buttons.

#### C.1 MacroEntry row (one per MacroObject in the macro's action list)

| Control | Type | Asset | x | y | w | h | notes |
|---|---|---|---|---|---|---|---|
| Main combobox | `Combobox` | drop-down chrome | 0 | 0 | 200 | combobox H (~25) | options = all `MacroType` names (`Enum.GetNames(typeof(MacroType))`); selected index = `(int)obj.Code`; `Tag = obj` |
| Sub combobox (SubMenuType==1) | `Combobox` | drop-down chrome | 20 | rowH | 180 | ~25 | options = `MacroSubType` names sliced by `GetBoundByCode(obj.Code, count, offset)`; selected = `(int)obj.SubCode - offset`; max-height 300 |
| Text-entry bg (SubMenuType==2) | `ResizePic` | `0x0BB8` | 16 | rowH | 240 | 60 | wood frame |
| Text-entry field (SubMenuType==2) | `StbTextBox` | — | bg.X+4 = 20 | bg.Y+4 | bg.W-4 = 236 | bg.H-4 = 56 | multiline, `FontStyle.BlackBorder`, font `0xFF`/80 height; text = `((MacroObjectString)obj).Text` |

A row has SubMenuType 0 (no sub control — e.g. WarPeace, LastSpell), 1 (a sub-combobox — e.g. Open/Close/CastSpell/UseSkill/Walk), or 2 (a free-text box — e.g. Say/Emote/Delay/SetUpdateRange). Selecting index 0 ("None") in the main combobox removes the action.

## Assets

| Asset id | Type | Where | Notes |
|---|---|---|---|
| `0x0BB8` | gump (resizepic, nine-patch) | HotkeyBox bg, MacroEntry text-entry bg | wood input frame |
| `0x0481 / 0x0483 / 0x0482` | gump (button n/p/o) | HotkeyBox OK button | |
| `0x047E / 0x0480 / 0x047F` | gump (button n/p/o) | HotkeyBox Cancel button | |
| solid `Color(30,30,30)` | color quad | MacroButtonGump bg (default) | custom solid fill |
| solid `Color.DimGray` | color quad | MacroButtonGump bg (hover) | custom solid fill |
| solid `Color.Gray` | 1px rect | MacroButtonGump border | custom rectangle |
| black @ alpha 0.8 | color quad | MacroGump window bg | `AlphaBlendControl` |
| hue `0x03b2` (946) | text hue | MacroButton label (normal) | unicode font 1 |
| hue `53` | text hue | MacroButton label (hover) | |
| hue `0x0021` (33) | text hue | HotkeyBox label | unicode font 1 |
| hue `15` | text hue | MacroGump title label | unicode |
| font `1` | unicode font | titles, hotkey label, macro-button label | |
| font `0xFF`/`9` | bitmap font | NiceButton text, StbTextBox | small UI font |

No `artid` / `landid` used. No standard window-frame gump sprite — both windows are color quads.

## Behaviors

| Behavior | OOP source | ECS mechanism |
|---|---|---|
| **Drag to move** (both windows) | `CanMove = true` | `UIMovable` marker on the window root (carried by the bundle). `WindowDragPlugin.Drag` handles it. NOTE: MacroButtonGump has no per-pixel sprite mask — its hit-test must be solid (`UOCustomKind.None` → `UiHitTest` default solid-fill case). |
| **Right-click closes** (both) | `CanCloseWithRightClick = true` | `UIMovable` + `WindowDragPlugin.CloseOnRightClick`. No container routing (not a container) → plain `DespawnSubtree`. |
| **Topmost on click / z-stack** | `BringOnTop` | root carries `GlobalZIndex`; `UiZCounter.Bump()` on focus latch (already in `WindowDragPlugin.Drag`). |
| **Run macro on single-click** (button) | `OnMouseUp` + `CastSpellsByOneClick && !Alt && drag<5px` | `On<UiClick>` observer on the button root (UiClick = press+release same element, drag-off cancels — matches the `<5px` no-drag requirement). Guard on a `Res<Profile>`/settings `CastSpellsByOneClick` flag and keyboard Alt state. |
| **Run macro on double-click** (button) | `OnMouseDoubleClick` + `!CastSpellsByOneClick` | `On<UiDoubleClick>` observer on the button root (Bevy.UI synthesizes `UiDoubleClick`; see PaperdollPlugin backpack). Guard `!CastSpellsByOneClick`. |
| **Hover highlight** (button) | `OnMouseEnter/Exit` → label hue + bg color swap | A system reading `Interaction` (Hovered) on the button root, mutating the label's `TextColor`/hue and the root's custom-render fill color in place. Or two observers `OnInsert/OnRemove` of a Hovered marker if Bevy.UI exposes one; otherwise a per-frame read of `Interaction` (cheap, one entity). |
| **Hotkey capture** | `HotkeyBox` keyboard/mouse/wheel capture + `HotkeyChanged`/`HotkeyCancelled` | Requires a focusable keyboard-capture widget. ECS has none yet — this is a NEW widget (see Open questions). On change: validate against existing macros (`FindMacro`), reject duplicates with a message box, else write key/mods to the `Macro`. |
| **Create macro button** (button #2) | `OnButtonClick CreateNewMacro` → dispose existing button for this macro, spawn `MacroButtonGump` at mouse pos | `On<UiClick>` observer → despawn any existing `MacroButton` entity for this `Macro`, then spawn a `MacroButtonBundle` at the current mouse position. |
| **Open macro settings** (button #3a) | `OpenMacroOptions` → dispose MacroGump, `GameActions.OpenSettings(world, 4)` | `On<UiClick>` → despawn this MacroGump + open Options page 4. (No ECS OptionsGump yet → log + close, mirroring Paperdoll's "no ECS OptionsGump" stub.) |
| **Add empty action** (button #3b, non-fast-assign only) | `AddBtn` → `AddEmptyMacro()` appends a `MacroObject(None)` row | `On<UiClick>` → mutate the `Macro` linked list + rebuild the DataBox subtree (observer-driven rebuild, see below). |
| **Remove last action** (button #3c) | `RemoveBtn` → `RemoveLastCommand()` | `On<UiClick>` → remove last `MacroObject` + rebuild. |
| **Select action type** (row combobox) | `BoxOnOnOptionSelected` → 0 removes; else replace `MacroObject` + rebuild that row's sub-control | combobox `On<…SelectionChanged>` → mutate the `Macro` + rebuild the row (despawn old sub-control, add the new sub-combobox / text-entry per `SubMenuType`). |
| **Select sub-action** (sub combobox) | `sub.OnOptionSelected` → `obj.SubCode = offset + index` | selection observer → mutate `MacroObject.SubCode`. |
| **Edit action text** (text-entry) | `textbox.TextChanged` → `((MacroObjectString)obj).Text = …` | text-input `On<TextChanged>` → mutate `MacroObjectString.Text`. |
| **Vertical scroll** (action list) | `ScrollArea` | `Overflow.Scroll` + `ScrollPosition` component (same pattern as `ServerGumpPlugin.SpawnWrappedText`). |
| **Persistence** | `macros.xml` load/save; `MacroButtonGump.Save/Restore` | A `MacroStore` resource (port of `MacroManager` data model) loads/saves `macros.xml`. Button gumps persist via the existing gump-save mechanism if/when ECS has one. |
| **Execute macro** | `World.Macros.SetMacroToExecute` + `Update()`/`Process()` (the 1400-line dispatch) | A `MacroExecutionPlugin` / `Res<MacroStore>` running the action queue. This is a LARGE separate concern (touches network, targeting, gump open/close, profile). The button's job is only to enqueue (`SetMacroToExecute`) + kick the runner. |

## Server packets

NONE. The macro system is entirely client-side. `MacroGump`, `MacroButtonGump`, and `MacroControl` are opened by client actions (`GameActions.OpenMacroGump`, the "Create new macro button" click, Options page 4). There is no server opcode that opens or updates these gumps.

Macro EXECUTION sends many outgoing packets (e.g. `Send_OpenSpellBook`, `Send_InvokeVirtueRequest`, `Send_EquipLastWeapon`, `Send_TargetSelectedObject`, `Send_ToggleGargoyleFlying`) and triggers many `GameActions`, but those are downstream of execution, not gump-driver packets.

## ECS implementation plan

Proposed split into two plugins + a shared data resource:

### Files
- `src/ClassicUO.Ecs/Gameplay/Macros/MacroStore.cs` — `Res<MacroStore>` data model. Port of `MacroManager`'s data side (the `Macro` / `MacroObject` / `MacroObjectString` linked structures, `MacroType` / `MacroSubType` enums, `FindMacro`, `GetAllMacros`, `GetBoundByCode`, `CreateEmptyMacro`, `CreateDefaultMacros`, `Load`/`Save` of `macros.xml`). Pure data — no UI, no `World`.
- `src/ClassicUO.Ecs/Gameplay/Macros/MacroExecutionPlugin.cs` — runs the queued macro (`SetMacroToExecute` + per-frame `Process`). Big, separate, can be stubbed v1 (enqueue + log the action) and filled in incrementally. Uses `Res<Time>` for `_nextTimer` (NOT `Time.Ticks` wall-clock; use `Time.Total`).
- `src/ClassicUO.Ecs/Gameplay/Macros/MacroButtonPlugin.cs` — the `MacroButtonGump` button window.
- `src/ClassicUO.Ecs/Gameplay/Macros/MacroEditorPlugin.cs` — the `MacroGump` fast-assign editor window + `MacroControl` subtree.

### Components / resources
- `Res<MacroStore>` — the macro data (see above).
- `Res<MacroExecQueue>` (or fields on MacroStore) — `LastMacro` + `NextTimer` for the runner.
- `struct MacroButton { uint MacroId; }` — marker on a macro-button window root (MacroId = index into store / stable handle); used to dedup "create button" and to resolve which macro to run.
- `struct MacroButtonLabel { ulong WindowEntity; }` — on the button's label child so the hover system can recolor it.
- `struct MacroEditorWindow { … name/macro handle … }` — marker on the MacroGump root.
- `struct MacroEntryRow { ulong WindowEntity; int Index; }` — on each action row, so type-change can rebuild precisely (mirrors `PaperdollBodyChild`).
- `struct MacroHotkeyBox { … }` — on the hotkey-capture widget.

### Bundle usage
- The MacroButton and MacroGump windows do NOT use a UO gump sprite, so `UOGumpBundle` with a real `BackgroundId` does not apply directly. Two options, both conforming to the contract:
  1. Insert the same component set the bundle does but with `UOCustomKind.None` (invisible solid hit surface) PLUS a `BackgroundColor` child for the tinted/translucent fill — exactly how `ServerGumpPlugin` handles its no-resizepic root (lines 629-645) and `checkertrans` (lines 575-596). The root still gets `UIMovable` + `GlobalZIndex` + `Interaction.None` so drag / right-click-close / z-stack fall out for free.
  2. Add a new `UOCustomKind.SolidFill` (see below) so the root itself renders the colored quad + border with a solid hit-test.
- Recommendation: option 2 for MacroButton (it needs the fill + 1px border + hover recolor on the root, exactly what a dedicated kind gives), option 1 for the MacroGump translucent bg (reuse `BackgroundColor`).

### Observers
- `On<UiClick>` per editor button (Create-button, Open-settings, Add, Remove) — same pattern as PaperdollPlugin button observers.
- `On<UiClick>` / `On<UiDoubleClick>` on the MacroButton root for run-on-click / run-on-double-click (guarded by the `CastSpellsByOneClick` profile flag + Alt state).
- Combobox / sub-combobox selection-changed + text-entry text-changed observers (depend on new widgets — see below).
- A rebuild observer (keyed on a `MacroDirty` marker the click handlers insert, mirroring `ServerGumpPageRequest`) that despawns the DataBox row subtree and rebuilds it from the `Macro` linked list — analogous to `PaperdollPlugin.RebuildOnEquip` despawn-and-rebuild of `PaperdollBodyChild`.

### Systems
- Hover recolor system (MacroButton): reads `Interaction` on the button root, mutates the label `TextColor` + the root fill color in place. One entity per button — trivial.
- `MacroExecutionPlugin` runner system in `Stage.Update`, gated on a non-empty queue.
- `DisposeOnLogout` (`OnExit(GameState.GameScreen)`) to despawn open macro windows, mirroring `PaperdollPlugin.DisposeOnLogout`.

### New ClayUO custom render + UiHitTest
- **New `UOCustomKind.SolidFill`** in `GuiPlugin.cs` (`UOCustomKind` enum, line 318) carrying a fill color (and optional border color/width) — used by MacroButton. Then:
  - Add a `case UOCustomKind.SolidFill` in `GuiRenderingPlugin.cs`'s custom-command switch: `batcher.Draw` a 1x1 white texture stretched to bounds in the fill color, then `DrawRectangle` for the 1px border. (`UOCustomRender` currently only has `AssetId`/`Hue`; add a `Color`/`BorderColor` field, or reuse `Hue` packing — see Open questions.)
  - Add a `case UOCustomKind.SolidFill` in `UiHitTest.PixelHit` (`UiHitTest.cs`) → solid within bounds (return true; same as the `default` branch). This is required so the button captures drag / right-click-close / run-click everywhere inside it.
- The translucent MacroGump bg uses existing `BackgroundColor` (Clay native, no new kind needed), with the root using `UOCustomKind.None` for the hit surface (already handled by `UiHitTest` default).

### New widgets (NOT yet in ECS — biggest gap)
The macro editor depends on three interactive widgets the ECS branch does NOT have:
1. **Combobox / dropdown** — used twice per row. No ECS equivalent exists. Must be built (closed state = selected-text label + arrow; open state = a floating list of options with click-to-select). This is substantial.
2. **Text input field** (`StbTextBox` analogue) — for SubMenuType==2 rows and conceptually the hotkey label. ECS has no editable text widget (server-gump `textentry` is rendered as a static baked label only — see `ServerGumpPlugin` lines 503-515).
3. **HotkeyBox** — keyboard/mouse/wheel chord capture with OK/Cancel. Entirely new; needs keyboard focus + raw key/mod capture wired to `Res<Keyboard>`-equivalent input.

These widgets are shared infrastructure; recommend building them as reusable Bevy.UI controls before (or alongside) this gump. Until they exist, a v1 can ship MacroButtonGump (run/drag/close/hover — all achievable today) and a read-only / stubbed MacroGump.

### ECS rules conformance
- No `World` access — all reads via `Query<…>`, all spawn/despawn via `Commands`, singletons via `Res`/`ResMut`.
- Macro data is a `Res<MacroStore>` (a singleton shared across systems) — NOT a static. The runner's timer uses `Res<Time>.Total`, not `Time.Ticks`.
- Buttons fire on release (`On<UiClick>`); double-click via synthesized `On<UiDoubleClick>`; drag-latch never uses these.
- Subtree rebuild on action edit goes through a dirty marker + observer (no per-frame `Changed` scan), mirroring server-gump page requests / paperdoll equip rebuild.

## How to trigger for capture

`MacroGump` and `MacroButtonGump` are client-only — no server packet, no top-bar button by default. In the live OOP client:

1. Boot ModernUO (`127.0.0.1:2593`, `admin/admin`) and log a character fully into the world.
2. **Macro editor window (`MacroGump`)**: open the Options window (default: `Alt+O` macro, or paperdoll Options button) → go to the **Macros** tab (Options page 4). That shows the embedded `MacroControl` editor (the full, non-fast-assign variant) — this is the closest live view of the editor subtree. The standalone fast-assign `MacroGump` window itself is only summoned via `GameActions.OpenMacroGump(world, name)`, which is not bound to a default UI affordance; capture it by triggering that code path (e.g. via the agent harness RPC invoking the action, or temporarily binding it).
3. **Macro button (`MacroButtonGump`)**: inside the Macros editor (or the fast-assign window), click **"Create a macro button for this macro"** — a small 88x44 labelled button appears at the mouse position. Drag it anywhere; right-click closes it; double-click (or single-click with "Cast spells by one click" enabled) runs the macro.

For the ECS port, prefer the agent harness (`tools/agent-desktop/`): `up --persist`, drive an RPC that opens the macro editor (once wired), `rpc-shot`, `down`. Required game state: logged into the world (these are in-game gumps).

## Open questions

1. **Interactive widgets don't exist in ECS.** Combobox, editable text field, and the HotkeyBox keyboard-chord capture are all missing. They are the long pole. Should this gump block on building those reusable widgets first, or ship MacroButton-only + a stubbed editor?
2. **MacroExecution scope.** `MacroManager.Process` is ~1400 lines touching network, targeting, gump open/close, profile toggles, camera. Port incrementally (which actions in v1)? Many target actions (`Open Paperdoll`, `Open Backpack`) depend on ECS gumps that may not exist yet.
3. **`UOCustomRender` has no color field.** Adding `UOCustomKind.SolidFill` needs a fill/border color on `UOCustomRender` (currently only `AssetId` + `Hue` Vector3). Add explicit `Clay.Color Fill`/`Border` fields, or pack into the existing fields? Confirm the preferred extension.
4. **`CastSpellsByOneClick` + Alt state.** Need the ECS profile/settings resource exposing `CastSpellsByOneClick` and a keyboard-modifier resource exposing Alt, for the run-on-click vs run-on-double-click branch. Confirm those resources exist (`Res<Profile>` / input modifier state) or must be added.
5. **Macro button persistence / anchoring.** OOP `MacroButtonGump` is an `AnchorableGump` (snaps into spell/anchor groups) and persists in the saved gump XML. ECS has no anchor system or gump-save yet — is anchoring + persistence in scope, or is a free-floating, non-persistent button acceptable for v1?
6. **Title label font/hue fidelity.** The MacroGump title uses unicode font 1 hue 15, and the macro-button label uses font 1 hue 0x03b2 with BlackBorder. Bevy.UI text currently renders TTF/baked fonts; confirm font 1 + hue mapping is reproducible (the ServerGumpPlugin notes Clay.NET text-color caveats).
7. **Coordinates are camera-relative.** MacroGump centers on `camera.Bounds.Width/2`; the ECS port should center on the current viewport width via the appropriate `Res` (window/camera size), then `SetInScreen`-equivalent clamp.
