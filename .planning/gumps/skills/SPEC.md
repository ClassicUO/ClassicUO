# Standard Skills Gump Spec

## Overview

The **Standard Skills Gump** (`StandardSkillsGump`) is the client-side skill list window. It is a single floating UO scroll gump (the parchment "expandable scroll" body, gump `0x1F40`) that lists every player skill, organized into collapsible user-defined **groups** (e.g. "Miscellaneous", "Combat Ratings", …). Each skill row shows an optional *use* button (only for "clickable"/active skills), the skill name, its numeric value, and a per-skill *lock-state* button (Up / Down / Locked). The window has a minimize/restore tab, a "show real / show caps" pair of checkboxes that toggle which value is displayed, a "new group" button, a "reset groups" button, and a running total-skill-points label.

It appears when the player opens the skills list — in the legacy client via the paperdoll **Skills** button (`PaperDollGump`), via a top-bar / macro action, or via the `OpenSkills` command. The client itself owns the window: the server does not push it. The only server interaction is the **0x3A skill update** packet that feeds skill values/locks, and the outgoing **0x34 skills-request** (re-fetch), **0x3A lock-change request**, and **0x12 use-skill** packets.

There is a *second*, alternate skills UI — `SkillGumpAdvanced` — a flat, sortable, semi-transparent table (no groups). It is not the default ("standard") gump; it is summarized here for completeness but the ECS port targets `StandardSkillsGump`.

> ECS state-of-the-world: the ECS branch currently registers `OnUpdateSkillsPacket_0x3A` as a **Stub** (`InGamePacketsPlugin.cs:258`) — skill values are parsed off the wire but **not stored** in any component/resource yet. There is **no `SkillsGroupManager`** and **no `Skill` value model** in `src/ClassicUO.Ecs/` (only `SkillEntry` name/hasAction metadata via `ClassicUO.Assets.SkillsLoader`, and the `Lock` enum at `Game/Data/Skill.cs`). The Paperdoll "Skills" button (`PaperdollPlugin.cs:320`) only re-sends `Send_SkillsRequest`. Implementing this gump therefore requires a **skill-data backing store first** (see ECS implementation plan, Phase 0).

## Source of truth

| File | Role | Key refs |
|------|------|----------|
| `src/ClassicUO.Client/Game/UI/Gumps/StandardSkillsGump.cs` | The standard grouped skills window. | ctor `:38-152`; `IsMinimized` `:156-188`; `OnButtonClick` (new group / reset) `:209-250`; `LoadSkills` `:252-277`; `Update` layout reflow `:279-300`; `Update(int skillIndex)` `:303-314`; `UpdateSkillsValues` (real/caps toggle) `:316-338`; total sum `:353-356`. Nested `SkillsGroupControl` `:359-766` (group header textbox/button/tiled-divider, drag-to-regroup `OnMouseOver` `:563-617`, delete-group `OnKeyUp` `:649-688`, edit-state highlight render `:712-765`). Nested `SkillItemControl` `:768-1002` (use button `:796-803`, lock button `:809-816`, name/value labels `:818-824`, lock cycle `OnButtonClick` `:844-874`, value text `:886-911`, lock graphic picker `:913-924`, drag-out spawns `SkillButtonGump` `:926-947`, hover highlight render `:970-1001`). |
| `src/ClassicUO.Client/Game/UI/Gumps/SkillButtonGump.cs` | Floating single-skill "macro" button dragged out of the list. | ctor `:18-38`; `BuildGump` (ResizePic `0x24B8` + centered name) `:45-85`; one-click vs double-click cast `:88-108`. |
| `src/ClassicUO.Client/Game/UI/Gumps/SkillGumpAdvanced.cs` | Alternate flat sortable table (NOT the standard gump). | ctor `:43-119`; sort buttons + indicator `OnButtonClick` `:123-148`; `BuildGump` row build `:150-200`; `SkillListEntry` `:256-395` (lock-pic cycle `:319-344`, drag-out `:347-365`). |
| `src/ClassicUO.Ecs/Network/IncomingPackets/OnUpdateSkillsPacket_0x3A.cs` | Wire parse of skill update (full list `0xFE` defs, value rows, single-update `0xFF`/`0xDF`, cap presence). | whole file. |
| `src/ClassicUO.Assets/SkillsLoader.cs` | Skill name/hasAction metadata (`SkillEntry`). | `Skills` list `:21`; `SkillEntry` `:72-151`. |
| `src/ClassicUO.Ecs/Game/Data/Skill.cs` | `Lock` enum (Up=0, Down=1, Locked=2). | `:6-11`. |
| `src/ClassicUO.Client/Network/OutgoingPackets.cs` | Wire format of outgoing skill packets. | `Send_UseSkill` (0x12, subcmd `0x24`, ASCII `"{idx} 0"`) `:1134`; `Send_SkillStatusChangeRequest` (0x3A, u16 index + u8 lock) `:3159`. |

## Visual structure

`_diffY = 22`. The window root has NO background sprite of its own; the `ExpandableScroll` (`0x1F40`) is the visible body. Coordinates below are control-local (relative to the gump origin) as authored in the ctor.

- **Window root** (`StandardSkillsGump : Gump`), `AcceptMouseInput=false`, `CanMove=true`, `CanCloseWithRightClick=true`, initial `Height = 200 + 22 = 222`.
  - **Minimize/restore tab pic** — `GumpPic` at `(160, 0)`, gump `0x82D` (restored) / `0x839` (minimized). Double-click restores when minimized. `:46-47`.
  - **ExpandableScroll body** — `ExpandableScroll(x=0, y=22, height=222, graphic=0x1F40)`, `TitleGumpID=0x0834`. This is the parchment scroll with a draggable bottom resize handle; it owns most of the visible chrome. `:49-55`.
  - **Top divider** — `GumpPic` at `(50, 35+22=57)`, gump `0x082B` (horizontal rule). `:57`.
  - **Bottom divider** (`_bottomLine`) — `GumpPic` at `(50, Height-98)`, gump `0x082B`. Y re-derived each frame. `:58`.
  - **Bottom comment plate** (`_bottomComment`) — `GumpPic` at `(25, Height-85)`, gump `0x0836`. `:59`.
  - **Skill list scroll area** (`_area : ScrollArea`) at `(22, 45+22+_bottomLine.Height-10, _scrollArea.Width-14, _scrollArea.Height-(83+22))`, no visible scrollbar background (arg `false`). Holds the `DataBox` group container. `:61-70`.
    - **DataBox container** (`_container`) at `(0,0)`, auto-sized; vertically stacks `SkillsGroupControl`s. `:72-77`.
  - **Total-skills label** (`_skillsLabelSum`) — `Label`, ASCII (`isunicode=false`), maxwidth 600, hue `0`, **font `3`**, text = `Player.Skills.Sum(Value).ToString("F1")`. Positioned `X=_bottomComment.X+_bottomComment.Width+5`, `Y=_bottomComment.Y-5` (re-set to `+2` in Update). `:79-89, 289, 355`.
  - **New-group button** (`_newGroupButton`) — `Button(id=0, 0x083A/0x083A/0x083A)`, `X=60`, `Y=Height-52`, `ContainsByBounds=true`, `ButtonAction.Activate`. `:94-103, 288`.
  - **Show-Real checkbox** (`_checkReal`) — `Checkbox(0x938 unchecked, 0x939 checked, text=ResGumps.ShowReal, maxwidth=1, hue=0x0386, isunicode=false)`, `X=_newGroupButton.X+width+30`, `Y=_newGroupButton.Y-6`. `:105-116, 290`.
  - **Show-Caps checkbox** (`_checkCaps`) — same graphics, `text=ResGumps.ShowCaps`, hue `0x0386`, `X=` same as ShowReal, `Y=_newGroupButton.Y+7`. The two are mutually exclusive (toggling one unchecks the other). `:118-129, 291, 320-330`.
  - **Reset-groups button** (`_resetGroups : NiceButton`) at `(_scrollArea.X+25, _scrollArea.Y+7, w=100, h=18)`, `ButtonAction.Activate`, text `ResGumps.ResetGroups`, `unicode=false`, **font 6**, `ButtonParameter=1`, `IsSelectable=false`. `:137-145`.
  - **Minimize hitbox** (`_hitBox : HitBox`) at `(160, 0, 23, 24)` — left-click-up minimizes. `:147-149, 201-207`.

### SkillsGroupControl (one per group, child of DataBox) — `:359-490`

Local size `200×20`, `X=3, Y=3` at first; restacked at 17px steps.
- **Collapse/expand button** (`_button`) — `Button(id=1000, 0x0827/.../0x0827)` minimized, `0x826` expanded; `ContainsByBounds=true`; hidden until the group has ≥1 skill. Toggles `IsMinimized` (`OnButtonClick 1000`). `:387-394, 495-514, 690-696`.
- **Group-name textbox** (`_textbox : StbTextBox`) — font **6**, `FontStyle.Fixed`, `X=16, Y=-3, W=200, H=17`, not editable until clicked twice (3-state click cycle: 0 idle → 1 selected → 2 editable). `:398-417, 435-489`.
- **Name divider** (`_gumpPic : GumpPicTiled`) — gump `0x0835`, `X=width+11+16`, `Y=5`, `Width=215-X` (fills the line right of the name). `:421-429`.
- **Skills box** (`_box : DataBox`) — holds the `SkillItemControl`s, hidden when minimized. `:431`.
- Edit-state highlight (custom draw): `_status==2` → `Color.Beige` rect over `(0,0,Width,17)`; `_status==1` → `Color.Bisque` rect over `(16,0,200,17)`. `:712-765`.

### SkillItemControl (one per skill, child of group's box) — `:768-1002`

Local size `255×17`, stacked at 17px increments (`AddSkill(index, 0, 17 + i*17)`).
- **Use button** (only if `skill.IsClickable`) — `Button(id=0, 0x0837/0x0838/0x0838)`, `X=8`, `ButtonAction.Activate` → `GameActions.UseSkill(Index)`. `:794-803, 846-849`.
- **Lock-state button** (`_buttonStatus`) — `Button(id=1, graphic×3)`, `X=251`, `ContainsByBounds=true`. Graphic by lock: **Up → `0x0984`**, **Down → `0x0986`**, **Locked → `0x082C`** (`GetStatusButtonGraphic` `:913-924`). Click cycles Up→Down→Locked→Up and sends lock-change request. `:809-816, 850-873`.
- **Name label** — `Label(skill.Name, isunicode=false, hue=0x0288, font=9)`, `X=22`. `:818-819`.
- **Value label** (`_value`) — `Label("", false, hue=0x0288, font=9)`, right-aligned: `X = 250 - _value.Width`; text = `Value` (or `Base` when ShowReal, `Cap` when ShowCaps), `"F1"`. `:822, 886-911`.
- Hover highlight (custom draw): when this is the last left-mouse-down control, fill `(0,0,Width,Height)` with `Color.Wheat`. `:970-1001`.

### SkillButtonGump (dragged-out single skill) — `SkillButtonGump.cs`

`88×44`, `AnchorType.SPELL`, `GroupMatrix 44×44`.
- **Background** — `ResizePic(0x24B8)` sized `88×44`. `:52-59`.
- **Name label** — `Label(skill.Name, isunicode=true, hue=0, maxwidth=Width-8=80, font=1, TS_CENTER)`, `X=4`, vertically centered. `:63-84`.
- Left-click (one-click-cast profile) or double-click (default) → `GameActions.UseSkill(skill.Index)`. `:88-108`.

### SkillGumpAdvanced (alternate, NOT the port target) — control inventory

Fixed `500×360`. `AlphaBlendControl(0.95)` fill `(1,1,498,358)`; gray 1px border drawn in `AddToRenderLists`. Header sort buttons (`NiceButton`, all `10×... ButtonAction.Activate`): **Name** `(40,25)` w180 param1, **Real** `(220,25)` w80 param2, **Base** `(300,25)` w80 param3, **Cap** `(380,25)` w80 param4. Two white `Line`s at `y=60` and `y=310` (width 435, color `0xFFFFFFFF`). Sort-order indicator `GumpPic` (`0x985` asc / `0x983` desc) positioned at `btn.X+btn.W-15, btn.Y+5`. `ScrollArea(20,60,460,250, normal-scrollbar)` holds rows. Each `SkillListEntry` (`h=20`): optional use button `0x837/0x838` at `(0,4)`; name label x20; base x200; value x280; cap x360 (all unicode, hue `1153`, **font 3**); lock `GumpPic` at `(425,4)` with `0x983`(Up)/`0x985`(Down)/`0x82C`(Locked) cycling on click; drag-out spawns a `SkillButtonGump`. Totals labels at `(40,320)/(220,320)/(300,320)`. Page IDs: single page (no tabs).

## Assets

| Asset | ID | Kind | Use |
|-------|-----|------|-----|
| Expandable scroll body | `0x1F40` | gump | Main window parchment (ExpandableScroll). |
| Scroll title cap | `0x0834` | gump | `ExpandableScroll.TitleGumpID`. |
| Minimize tab (restored) | `0x82D` | gump | Tab pic at (160,0). |
| Minimize tab (minimized) | `0x839` | gump | Tab pic when collapsed. |
| Horizontal divider rule | `0x082B` | gump | Top + bottom divider lines. |
| Bottom comment plate | `0x0836` | gump | Plate behind total label. |
| New-group button | `0x083A` | gump | Normal/pressed/over all same. |
| Checkbox unchecked | `0x938` | gump | Show-Real / Show-Caps. |
| Checkbox checked | `0x939` | gump | Show-Real / Show-Caps. |
| Group collapse (minimized) | `0x0827` | gump | Group header expand button. |
| Group collapse (expanded) | `0x826` | gump | Group header expand button. |
| Group name divider (tiled) | `0x0835` | gumppictiled | Fills line right of group name. |
| Skill use button (normal) | `0x0837` | gump | Per-skill use, IsClickable only. |
| Skill use button (over/pressed) | `0x0838` | gump | Per-skill use. |
| Lock state — Up | `0x0984` | gump | Lock button (standard gump). |
| Lock state — Down | `0x0986` | gump | Lock button (standard gump). |
| Lock state — Locked | `0x082C` | gump | Lock button (standard gump). |
| SkillButtonGump bg | `0x24B8` | resizepic | Dragged-out single skill macro button. |
| (Advanced) lock Up / Down / Locked | `0x983` / `0x985` / `0x82C` | gump | SkillGumpAdvanced lock pic + sort indicator. |
| Hue — group-name text | `0x0288` | hue | Skill name + value labels (font 9). |
| Hue — checkbox text | `0x0386` | hue | Show-Real / Show-Caps labels. |
| Hue — advanced rows | `1153` | hue | SkillGumpAdvanced labels. |
| Highlight colors | `Beige` / `Bisque` / `Wheat` | solid | Group edit / select / skill hover highlights. |

Fonts: **3** (total label, advanced rows, ASCII), **6** (group name textbox, reset-groups NiceButton, fixed-width), **9** (skill name + value, ASCII), **1** (SkillButtonGump name, unicode).

String resources (`ResGumps`): `ShowReal`, `ShowCaps`, `NewGroup`, `ResetGroups`, `NoName`, `Name`, `Real`, `Base`, `Cap`, `Total` — all present in `src/ClassicUO.Ecs/Resources/ResGumps.Designer.cs`.

## Behaviors

| Behavior | Legacy mechanism | ECS mechanism |
|----------|------------------|---------------|
| **Drag to move** | `Gump.CanMove=true`. | `UIMovable` on the `UOGumpBundle` root — `WindowDragPlugin.Drag`. |
| **Right-click closes** | `CanCloseWithRightClick=true`. | `UIMovable` → `WindowDragPlugin.CloseOnRightClick` (despawn subtree). No item-aware close needed (not a container). |
| **Topmost-on-click / z-stack** | UIManager bring-to-front. | `GlobalZIndex` on root + `UiZCounter.Bump()` on drag-latch (already in `WindowDragPlugin`). |
| **Click-capture to world** | UIManager hit-test. | `WindowDragPlugin.ClaimSelectedFromMovable` (root carries no NetworkSerial). |
| **Pixel-perfect hit-test** | `PixelCheck` per control. | `UiHitTest.PixelHit` (Gump kind) — already wired for all `UiCustom` sprites via `GuiPlugin` clay hit-test. |
| **Skill use button (fire on release)** | `Button ButtonAction.Activate` (mouse-up) → `UseSkill`. | Per-button `On<UiClick>` observer → `net.Send_UseSkill(index)`. |
| **Lock-state cycle** | Lock button `id=1` cycles Up→Down→Locked→Up; `Send_SkillStatusChangeRequest`; swaps button graphic. | `On<UiClick>` observer on the lock button reads/advances the skill's `Lock` (stored in the skill resource), sends 0x3A change, mutates the button's `UOButton` triplet in place (graphic per lock). |
| **Show-Real / Show-Caps toggle** | Checkbox `ValueChanged` → recompute every value label + total; mutually exclusive. | `On<UiClick>` observer on each checkbox flips a `SkillsDisplayMode` resource (Value/Real/Cap), swaps the checkbox sprite (`0x938`/`0x939`), and triggers a value-label refresh (observer on the mode resource change or a tagged refresh). |
| **New group** | `OnButtonClick(0)` adds a `SkillsGroup`, rebuilds container. | New-group button `On<UiClick>` → `Commands` to append a group to `SkillsGroupsState` + rebuild the list subtree (despawn old `SkillRowChild`s, re-spawn). |
| **Reset groups** | `OnButtonClick(1)` → confirm dialog → `MakeDefault()` + reload. | Reset button `On<UiClick>` → reset `SkillsGroupsState` to default + rebuild. (Confirm dialog deferred — see open questions.) |
| **Group collapse/expand** | Group button `id=1000` toggles `IsMinimized`, hides skills box, reflows. | Per-group-header `On<UiClick>` flips a `SkillGroupState.Minimized` field, a sync system sets child `Node.Display` (mirrors `ServerGumpPlugin` page-visibility sync) and re-stacks `Node.Top`. |
| **Minimize whole window** | HitBox mouse-up sets `IsMinimized`, hides all children, swaps tab graphic. | Hitbox sprite `On<UiClick>` flips a `SkillsWindowState.Minimized`; sync system toggles child `Display` + swaps the tab `UOCustomRender.AssetId` between `0x82D`/`0x839`. |
| **Restore from minimized** | Tab `MouseDoubleClick`. | Tab `On<UiDoubleClick>` (Bevy.UI synthesizes it; see `PaperdollPlugin` backpack dclick) → clear minimized. |
| **Drag skill out → SkillButtonGump** | `SkillItemControl.OnMouseUp` spawns `SkillButtonGump` when released off-list. | **Deferred (v2)** — needs a SkillButtonGump plugin + drag-off detection. Not in v1; log only. |
| **Drag skill between groups** | `SkillsGroupControl.OnMouseOver` re-parents on hover-while-dragging. | **Deferred (v2)** — complex drag-regroup; v1 ships fixed default groups. |
| **Group rename / delete** | textbox 3-state edit; Delete key. | **Deferred (v2)** — needs editable Bevy.UI text widget. |
| **Server-driven value update** | `StandardSkillsGump.Update(int skillIndex)` called from skill packet handler refreshes one row + total. | `0x3A` observer writes `PlayerSkills` resource; an `OnInsert`/changed observer (or a "skills dirty" marker) rebuilds value labels + total on any open skills window. |
| **Resize (vertical)** | `ExpandableScroll` bottom handle changes `Height`; list reflows. | **Deferred (v2)** — `ExpandableScroll` is a custom resizable nine-region scroll; v1 uses a fixed-height window. See open questions. |
| **Mouse-wheel scroll** | `ScrollArea`. | Wrap the list in an `Overflow.Scroll` + `ScrollPosition` container; `GuiPlugin.RouteWheelToScrollable` already handles the wheel for movable gumps. |

## Server packets

This gump is **client-opened** (no server open packet). Related packets:

- **Incoming `0x3A` — Update Skills** (`OnUpdateSkillsPacket_0x3A`). `UpdateType`: `0xFE` = full skill *definition* list (count + per-skill hasButton + name); `0x00` = full value list; `0xFF` = single update (no cap); `0xDF` = single update with cap; `0x01-0x03`/`0xDF` carry caps. Each value row: `Id`(i16), `RealValue`(i16), `BaseValue`(i16), `Status`(u8 Lock), optional `Cap`(i16). Real/Base are fixed-point ×10 (display `value/10` as `F1`). **Currently a Stub in ECS — must be wired to a `PlayerSkills` store.**
- **Outgoing `0x34` — Skills request** (`Send_SkillsRequest`, exists in ECS `OutgoingPackets.cs:540`): subcommand `0x05` + player serial; asks the server to (re)send 0x3A.
- **Outgoing `0x3A` — Skill lock-change request** (`Send_SkillStatusChangeRequest`): `u16 skillIndex` + `u8 lockState`. **Not yet in ECS `OutgoingPackets.cs` — must be added.**
- **Outgoing `0x12` — Use skill** (`Send_UseSkill`): subcommand `0x24` + ASCII `"{idx} 0"`. **Not yet in ECS `OutgoingPackets.cs` — must be added.**

## ECS implementation plan

**Plugin:** `internal readonly struct SkillsGumpPlugin : IPlugin` → `src/ClassicUO.Ecs/Gameplay/SkillsGumpPlugin.cs`. Compose in `Boot.cs` (`CuoPlugin.Build`). Model on `PaperdollPlugin` (bundle + observers + child subtree) and `ServerGumpPlugin` (page/visibility sync, registry).

### Phase 0 — skill data store (prerequisite)

1. **`PlayerSkills` resource** (`Res`/`ResMut`): an array indexed by skill id of `{ short Real; short Base; short Cap; Lock Lock; bool HasButton; string Name }`. Populated by a real **`0x3A` observer** replacing the stub at `InGamePacketsPlugin.cs:258`:
   - `0xFE` → fill definition names/hasButton (count from packet; cross-ref `SkillsLoader.Skills` for fallback names).
   - `0x00`/value lists → set Real/Base/Lock/(Cap).
   - `0xFF`/`0xDF` → update a single id; then re-trigger a "skills changed" signal.
2. **`SkillsLoader`** is reached via `Res<UOFileManager>().Skills` for names + `HasAction` (clickability).
3. Add outgoing **`Send_UseSkill`** (0x12) and **`Send_SkillStatusChangeRequest`** (0x3A) to `src/ClassicUO.Ecs/Network/OutgoingPackets.cs`, copying the wire format from the legacy client (`Send_UseSkill` subcmd `0x24` + `"{idx} 0"`; lock-change `u16 index`+`u8 lock`).

### Phase 1 — window + rows

- **Resources/components:**
  - `SkillsGumpState` resource — `Minimized` flag, plus the open window entity id (dedupe to one window; bump z + un-minimize on re-open, mirroring `PaperdollPlugin.SpawnOnOpenPaperdoll`).
  - `SkillsDisplayMode` resource — enum `{ Value, Real, Cap }` (mutually exclusive checkboxes).
  - `SkillsGroupsState` resource — list of groups (`Name`, `byte[] SkillIds`, `bool Minimized`). v1 seeds the default UO groups (port `SkillsGroupManager.MakeDefault`; that manager lives only in the legacy tree — replicate the default grouping table in ECS).
  - Component `SkillsWindow` on the root (like `PaperdollWindow`) for dedupe/teardown.
  - Component `SkillRowChild { ulong WindowEntity; int SkillId; }` on every dynamic list child (rows, group headers, dividers) — lets a rebuild despawn precisely (mirrors `PaperdollBodyChild`).
  - Component `SkillLockButton { int SkillId; }` on each lock button; `SkillUseButton { int SkillId; }` on each use button.
- **Spawn trigger:** add a `SkillsGumpOpenEvent` (or reuse the paperdoll Skills button). The Paperdoll **Skills** button (`PaperdollPlugin.cs:320`) currently only `Send_SkillsRequest`s — extend its `On<UiClick>` to also raise the open event. Open command also from a top-bar entry if present.
- **Bundle:** root via `GumpBuilder.SpawnUOGump(commands, 0x1F40, Vector3.UnitZ, pos, zCounter)` (kind `Gump`). This gives `Node`+`UiCustom`+`UOGump`+`UIMovable`+`GlobalZIndex` in one go and inherits drag / right-click-close / z-stack / click-capture from `WindowDragPlugin`. Insert `SkillsWindow` on it.
- **Children (all via `GumpBuilder` + `commands.AddChild(root, …)`):**
  - Static chrome: dividers (`AddGump 0x082B`), comment plate (`AddGump 0x0836`), tab pic (`AddGump 0x82D`), checkboxes (`AddGump 0x938`), new-group button (`AddButton (0x083A,0x083A,0x083A)`), reset button (text label — `NiceButton` has no ECS equivalent; render as label + bbox or a plain `AddButton`). Coordinates from Visual Structure.
  - Group headers: expand button `AddButton (0x0827/0x826)` + name `AddLabel` + tiled divider `AddGumpTiled 0x0835`.
  - Skill rows (per skill in group order): optional use button `AddButton (0x0837,0x0838,0x0838)` (only if `HasAction`); name `AddLabel(font 9, hue 0x0288)` at x=22; value `AddLabel` right-aligned to x=250; lock button `AddButton` with the lock-state graphic triplet (Up `0x0984` / Down `0x0986` / Locked `0x082C`) at x=251. Stack rows at 17px (`Node.Top`).
  - Wrap the rows region in an `Overflow.Scroll` + `ScrollPosition` container (reuse the `ServerGumpPlugin.SpawnWrappedText` two-node pattern) so `RouteWheelToScrollable` scrolls it.
- **Observers / systems (all static, ECS-rule-compliant — no `World`):**
  - Use button: `btn.Observe((On<UiClick> _, Res<NetClient> net) => net.Value.Send_UseSkill(id))` (capture immutable `id`).
  - Lock button: `On<UiClick>` reads `ResMut<PlayerSkills>` for the skill, advances `Lock`, `net.Send_SkillStatusChangeRequest((ushort)id, (byte)newLock)`, and rewrites this button's `UOButton.Normal/Pressed/Over` in place (query `Data<UOButton, SkillLockButton>`), exactly like `PaperdollPlugin`'s war-mode button swap.
  - Checkboxes: `On<UiClick>` flips `ResMut<SkillsDisplayMode>`, swaps the checkbox `UOCustomRender.AssetId`, and tags the window for value refresh.
  - New-group / reset: `On<UiClick>` mutate `ResMut<SkillsGroupsState>` then despawn+rebuild `SkillRowChild`s (rebuild helper like `PaperdollPlugin.RebuildOnEquip`).
  - Group collapse + window minimize: `On<UiClick>` flip a state flag; a `Stage.PostUpdate` **visibility sync** system sets each `SkillRowChild`'s `Node.Display` and re-stacks `Node.Top` (port `ServerGumpPlugin`'s page-visibility sync). Window minimize also swaps the tab sprite asset id and toggles every child's `Display`.
  - Tab restore: `On<UiDoubleClick>` clears minimized.
  - **Skill value refresh:** an observer keyed on a "skills changed" trigger (e.g. an `OnInsert<SkillsDirty>` marker the 0x3A observer adds to the window, or a dedicated event) rebuilds the value labels + recomputes the total label. Prefer the observer-on-marker pattern over a per-frame scan (CLAUDE.md rule 4).
  - Teardown on logout: `OnExit(GameState.GameScreen)` despawns open windows (port `PaperdollPlugin.DisposeOnLogout`).

### New ClayUO render command / UiHitTest

**None required.** Every element is a standard `Gump` / `GumpTiled` / text label / `Art`-free sprite already covered by `GuiRenderingPlugin`'s switch (`UOCustomKind.Gump` `:253`, `GumpTiled` `:289`) and by `UiHitTest.PixelHit`'s `Gump`/`GumpTiled` cases. The legacy hover/edit highlight rectangles (`Color.Wheat/Beige/Bisque`) and the SkillGumpAdvanced gray border / `AlphaBlendControl` are **cosmetic** and can be reproduced with Bevy.UI `BackgroundColor` nodes (no new custom command). If a highlight-on-hover is desired in v1, add a `BackgroundColor` child toggled by Interaction — still no new ClayUO type.

### Notes on conformance

- No `World` access anywhere; all reads via `Query`/`Res`, all structural change via `Commands` (rules 1-2).
- Per-skill state lives in the `PlayerSkills` resource (cross-system) and per-button marker components (per-entity); no static shared state, no closure-captured mutables beyond immutable `id`s (rule 3).
- System→system interop is observer-first (rule 4); the only per-frame system is the cheap visibility/restack sync (justified, mirrors `ServerGumpPlugin`).
- Buttons fire on `On<UiClick>` (release), not `UiPointerDown` (gump contract).

## How to trigger for capture

The ECS gump does not exist yet, so a live screenshot requires the implementation. Once built, in the agent harness against ModernUO (`127.0.0.1:2593`, `admin/admin`):

1. Build AGENT_BUILD ECS exe; `up --persist` with `settings.json` pinned window size + UO data dir (see `tools/agent-desktop/AGENTS.md`).
2. Log a character fully into the world (GameScreen state) so `Player` + skills exist. The server sends `0x3A` on login; if values look empty, the open path also fires `Send_SkillsRequest(playerSerial)` to refresh.
3. Open the skills window: the canonical path is the **paperdoll Skills button** — open the paperdoll (it auto-opens on login, or via the top-bar/`0x88`), then `rpc-click` the Skills button at paperdoll-local `(185, 44 + 27*4 = 152)`. Alternatively wire/trigger the `SkillsGumpOpenEvent` directly.
4. `rpc-shot` for the reference image; `down`.

**Required game state:** a logged-in player with a populated skill list (any starter template has all skills at their base values). To capture lock-state button variants, cycle a skill's lock first. To capture group-collapse, click a group header. The legacy reference can also be captured by running the OOP `cuo` client and opening Skills from the paperdoll for a pixel comparison.

## Open questions

- **ExpandableScroll resize**: the legacy body (`0x1F40`) is a vertically resizable nine-region scroll with a drag handle; the ECS `UOCustomKind.Gump` renders a single fixed sprite. v1 ships a fixed-height window — confirm whether the resizable scroll is needed for parity or can be deferred. If needed, it requires a new render path (nine-region vertical scroll) + a resize-handle drag system.
- **SkillsGroupManager port**: the default-group table and group persistence live only in the legacy client (`Game/Managers/SkillsGroupManager`, not present in `src/ClassicUO.Ecs/`). The ECS port must replicate the default grouping (and decide whether group customization/persistence to profile XML is in scope — legacy saves to gump XML via `Save`/`Restore`).
- **NiceButton equivalent**: the reset button is a `NiceButton` (text button with selectable highlight). ECS has no NiceButton; confirm acceptable substitute (plain label + bbox click, or a `BackgroundColor` button).
- **Editable group name + drag-regroup + drag-out-to-SkillButtonGump**: these need an editable text widget, hover-while-dragging reparenting, and a SkillButtonGump plugin respectively. Confirm all three are v2 (deferred) as proposed.
- **Confirm dialog on reset**: legacy pops a `MessageBoxGump` OK/Cancel before resetting. ECS has no MessageBox gump — confirm reset can be immediate in v1 or whether a confirm UI must land first.
- **Fixed-point scale**: 0x3A Real/Base/Cap are ×10 fixed-point (legacy displays `value/10` as `F1`). Confirm the ECS `PlayerSkills` store keeps raw wire values and divides at display time.
- **Which open trigger**: should opening be driven solely by the paperdoll Skills button, or also a top-bar entry / keybind? (Affects whether a `SkillsGumpOpenEvent` is needed.)
