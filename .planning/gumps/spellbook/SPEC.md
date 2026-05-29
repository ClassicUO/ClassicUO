# Spellbook Gump Spec

## Overview

The Spellbook gump is a two-page "open book" window that lists the spells contained
in a spellbook item the player owns. It exists in eight flavours selected by the
spellbook item graphic: Magery, Necromancy, Chivalry (Paladin), Bushido, Ninjitsu,
Spellweaving, Mysticism, and Mastery. Each flavour uses a different book background
sprite, a different spell-icon graphic base, and a different spell-name/reagent/skill
data set.

Structure (per book):
- A **dictionary section** (front pages): an index of spell names, two book-pages
  worth at a time, with each spell name acting as a clickable jump-link to that
  spell's detail page. Magery additionally shows eight circle-selector buttons.
- A **detail section** (back pages): one spell per half-page showing the spell icon,
  name, power-words/abbreviation, reagents (if any), and mana/skill requirements.

The book can be **minimized** to a small closed-book icon (click the hitbox at the
spine) and restored by double-clicking the minimized icon. Left/right page-corner
arrows turn pages; double-clicking a corner jumps to first/last page.

It appears when the player double-clicks a spellbook item in their backpack/world.
The server responds with `0x24` OpenContainer (graphic `0xFFFF`) to open the window,
then `0xBF` subcommand `0x1B` to deliver the spell bitfield contents.

## Source of truth

- `src/ClassicUO.Client/Game/UI/Gumps/SpellbookGump.cs` — the entire gump.
  Key references (line numbers):
  - `BuildGump()` line 95 — root sprite, datbox, hitbox, page corners.
  - `GetBookInfo()` line 930 — per-type book/minimized/icon-base graphics + counts.
  - `AssignGraphic()` line 1317 — item.Graphic → SpellBookType mapping.
  - `CreateBook()` line 172 — full control tree build (index pages + detail pages).
  - Magery circle buttons: lines 219-300.
  - Index page labels + spell jump-links: lines 304-576.
  - Detail page (icon + name + abbrev + reagents + requires): lines 578-793.
  - `SetActivePage()` line 1240 — page clamp + corner Page visibility + sound.
  - `IsMinimized` setter line 46; `_hitBox_MouseUp` line 157; `_picBase_MouseDoubleClick` line 149.
  - `OnIconDoubleClick` line 814 (cast); `OnIconDragBegin` line 827 (drag out a UseSpellButton).
  - `OnLabelMouseUp` line 1263 / `OnLabelMouseDoubleClick` line 1275 — jump-link vs cast.
  - `PageCornerOnMouseClick` line 1373 / `...DoubleClick` line 1387.
  - `HueGumpPic` inner class line 1429 — active-spell highlight (hue 38) + ctrl+alt edit overlay.
- `src/ClassicUO.Client/Network/PacketHandlers.cs:1305` `OpenContainer` — spawn path
  (graphic `0xFFFF` → `new SpellbookGump(world, spellBookItem)` + sound `0x0055`).
- `src/ClassicUO.Client/Network/PacketHandlers.cs:4542` — `0xBF 0x1B` content →
  `GetGump<SpellbookGump>(spellbook)?.RequestUpdateContents()`.
- Spell data classes: `src/ClassicUO.Client/Game/Data/Spells{Magery,Necromancy,Chivalry,Bushido,Ninjitsu,Spellweaving,Mysticism,Mastery}.cs`
  (NOT yet present in `src/ClassicUO.Ecs/`).

## Visual structure

Coordinates are relative to the window root (the book background sprite at 0,0).
The book is 2 facing pages; left-page controls use the smaller X, right-page the
larger X. `dataX` 62 = left page text column, 225 = right page text column.
`indexX` 106 = left page index column, 269 = right page index column.

### Root window
- **picBase** — `GumpPic` at (0,0), gump id = book graphic (per type, see Assets).
  This is the window background and hit/drag surface. Double-click restores from
  minimized. When minimized, its graphic swaps to the minimized book id and all
  other children hide.
- **dataBox** — invisible `DataBox` at (0,0); a paged container. Holds every page's
  children; only children whose `Page` == active page render (page 0 = always shown).
- **hitBox** — invisible `HitBox` at (0, 98) size 27x23 — the book-spine minimize
  trigger (left mouse up → minimize).
- **pageCornerLeft** — `GumpPic` at (50, 8), gump `0x08BB`. Turn page back / jump first.
  Visible only when not on the first page.
- **pageCornerRight** — `GumpPic` at (321, 8), gump `0x08BC`. Turn page forward / jump
  last. Visible only when not on the last page.

### Magery circle-selector buttons (only SpellBookType.Magery; page 0, always shown)
Eight `Button`s at Y=175, each `Activate` to the named page. (X, gumpUp/gumpDown, ToPage):

| X | gump | ToPage | circles |
|---|------|--------|---------|
| 58  | 0x08B1 | 1 | 1/2 |
| 93  | 0x08B2 | 1 | 1/2 |
| 130 | 0x08B3 | 2 | 3/4 |
| 164 | 0x08B4 | 2 | 3/4 |
| 227 | 0x08B5 | 3 | 5/6 |
| 260 | 0x08B6 | 3 | 5/6 |
| 297 | 0x08B7 | 4 | 7/8 |
| 332 | 0x08B8 | 4 | 7/8 |

`OnButtonClick` maps these button IDs to `SetActivePage(1..4)`.

### Index (dictionary) pages — left/right half per `page`
For each of the two halves (j = 0 left, j = 1 right):
- **"Index" header** `Label` at (indexX, 10): indexX = 106 (left) or 269 (right),
  font 6, hue `0x0288`.
- **Circle/section name** `Label` at (dataX, 30), font 6, hue `0x0288`:
  - Magery: `SpellsMagery.CircleNames[(page-1)*2 + j%2]` (e.g. "First Circle").
  - Mastery: "Activated" or, on the last fill page, "Passive".
- Chivalry page 1 only: **"Tithing Points: N"** `Label` at (62, 162), font 6, hue `0x0288`
  (`ResGumps.TithingPointsAvailable + Player.TithingPoints`).
- **Spell jump-links** — one `HoveredLabel` per owned spell on the page, stacked at
  (dataX, 52 + 15*n). Normal hue `0x0288`, hover hue `0x33`, font 9, maxwidth 130,
  Cropped style. Clicking jumps to that spell's detail page; double-click casts.
- Mastery extra ("Abilities") column has its own icon list (out of scope detail; see
  Open Questions — Mastery is a rare edge case).

### Detail pages — one spell per half-page
For each owned spell i (in spell order), half-pages alternate left (spellsDone even)
and right (odd). Left column origin x=62, right column x=225 (`iconX`).
- **Section/circle name** `Label` at (topTextX, topTextY): left (87, 6), right (224, 6).
  - Magery: `SpellsMagery.CircleNames[i >> 3]`.
  - Mastery: `SpellsMastery.GetMasteryGroupByID(i+1)`.
  - Other: the spell name (font 6, hue `0x0288`).
- **Spell icon** — `HueGumpPic` at (iconX, 40), gump = iconStartGraphic + i
  (Mastery: `SpellsMastery.GetSpell(i+1).GumpIconID`). Hue 0 normally, **38** when
  the spell is currently active (`World.ActiveSpellIcons.IsActive`). Double-click =
  cast; drag = spawn a floating UseSpellButton. iconStartGraphic per type (see Assets).
- **Spell name** `Label` at (iconTextX, 34): left x=112, right x=275, maxwidth 80, font 6.
- **Power words / abbreviation** `Label` at (iconTextX, 26..34+nameHeight), font 8
  (Magery) or font 9 / font 6 (others).
- **Reagents** (Magery/Necro/Mysticism/Mastery): a `GumpPicTiled` separator
  `0x0835` at (iconX, 88) size 120x5 (non-Mastery only), then a **"Reagents:"**
  `Label` at (iconX, 92) font 6, then the reagent list `Label` at (iconX, 114) font 9.
  All hue `0x0288`.
- **Requires** (non-Magery): `Label` "Mana Cost N, Min. Skill M%" at (iconX, requiriesY)
  font 6 hue `0x0288`. requiriesY = 162 normally, 148 for Mastery with tithing cost.

### Minimized state
- picBase graphic → minimized book id; every other child `IsVisible = false`;
  picBase stays visible. Window shrinks to the icon size. Double-click restores.

## Assets

Per-type background / minimized / icon-base graphics (from `GetBookInfo`, line 930):

| SpellBookType | item.Graphic | book bg | minimized | icon base | MaxSpells |
|---------------|--------------|---------|-----------|-----------|-----------|
| Magery        | 0x0EFA       | 0x08AC  | 0x08BA    | 0x08C0    | SpellsMagery.MaxSpellCount (64) |
| Necromancy    | 0x2253       | 0x2B00  | 0x2B03    | 0x5000    | SpellsNecromancy.MaxSpellCount  |
| Chivalry      | 0x2252       | 0x2B01  | 0x2B04    | 0x5100    | SpellsChivalry.MaxSpellCount    |
| Bushido       | 0x238C       | 0x2B07  | 0x2B09    | 0x5400    | SpellsBushido.MaxSpellCount     |
| Ninjitsu      | 0x23A0       | 0x2B06  | 0x2B08    | 0x5300    | SpellsNinjitsu.MaxSpellCount    |
| Spellweaving  | 0x2D50       | 0x2B2F  | 0x2B2D    | 0x59D8    | SpellsSpellweaving.MaxSpellCount|
| Mysticism     | 0x2D9D       | 0x2B32  | 0x2B30    | 0x5DC0    | SpellsMysticism.MaxSpellCount   |
| Mastery       | 0x225A/0x225B| 0x08AC  | 0x08BA    | 0x0945    | SpellsMastery.MaxSpellCount     |

Shared / fixed assets:

| Asset | id | Use |
|-------|-----|-----|
| Page corner left  | gump 0x08BB | back-page arrow |
| Page corner right | gump 0x08BC | forward-page arrow |
| Magery circle btns| gump 0x08B1..0x08B8 | circle selectors |
| Reagent separator | gump 0x0835 (tiled 120x5) | reagent divider line |
| Macro-edit "+" overlay | gump 0x09CF | ctrl+alt fast-spell-assign hint (HueGumpPic) |
| Open/close/page sound | sound 0x0055 | on open, page turn, minimize, dispose |

Fonts / hues:
- Most labels: font **6**, hue **0x0288** (off-white book text).
- Spell name (detail): font **6**, maxwidth 80.
- Power-words: font **8** (Magery) / font **9** (others).
- Reagent list & jump-link: font **9**.
- Jump-link `HoveredLabel`: normal hue **0x0288**, hover hue **0x33**.
- Active-spell icon highlight: hue **38** (0x26).

## Behaviors

| Behavior | Legacy | ECS mechanism |
|----------|--------|---------------|
| **Drag to move** | `CanMove = true` | `UIMovable` on root (UOGumpBundle) → `WindowDragPlugin.Drag`. No reimplementation. |
| **Right-click close** | `CanCloseWithRightClick = true`; `Dispose` plays sound + saves pos | `UIMovable` → `WindowDragPlugin.CloseOnRightClick`. (Sound + position-save are not yet ported; see Open Questions.) |
| **Topmost on click** | gump z via UIManager | Root `GlobalZIndex` bumped by `WindowDragPlugin.Drag` latch. Do not add per-child z. |
| **Pixel-perfect hit** | per-control PixelCheck | `UiHitTest.PixelHit` (Gump kind) used by drag/close/select. The book bg is a `Gump` kind so transparent arch areas pass through. |
| **Page corner click** | `PageCornerOnMouseClick` (LDragOffset==0) → ±1 page | `On<UiClick>` observer on each corner sprite → `SetActivePage(±1)`. Fire-on-release. |
| **Page corner double-click** | jump first/last | `On<UiDoubleClick>` → first / last page. |
| **Magery circle buttons** | `Button` `Activate`, `OnButtonClick` → SetActivePage | `builder.AddButton` + `On<UiClick>` → set active page. Buttons fire on release. |
| **Spell jump-link click** | `OnLabelMouseUp` enqueues page; applied after dbl-click window | `On<UiClick>` on the link label → set active page (the dbl-click-vs-single disambiguation is handled by Bevy.UI's UiClick/UiDoubleClick split). |
| **Spell jump-link / icon double-click** | cast spell (`GameActions.CastSpell`) | `On<UiDoubleClick>` on link / icon → `Send_UseSpell` / cast request. |
| **Icon drag-out** | spawn floating `UseSpellButtonGump` | Out of scope v1 (no ECS UseSpellButton gump yet). Log only. |
| **Minimize** | hitbox left-up → IsMinimized=true | `On<UiClick>` on a hitbox child → toggle `SpellbookWindow.IsMinimized`; an observer/system swaps picBase asset + hides children. |
| **Restore** | picBase double-click when minimized | `On<UiDoubleClick>` on root → clear IsMinimized. |
| **Active-spell highlight** | `HueGumpPic.Update` sets hue 38 when active | A system queries spell-icon entities + active-spell state and writes `UOCustomRender.Hue` in place. |
| **Server content update** | `RequestUpdateContents` rebuilds on 0xBF 0x1B | Observer on the 0xBF-0x1B-derived event rebuilds the page subtree (despawn old children, rebuild) like PaperdollPlugin's RebuildOnEquip. |
| **Page-turn / open / minimize sound** | `Audio.PlaySound(0x0055)` | Deferred — no ECS AudioManager resource yet (matches container plugin's TODO(audio)). |

## Server packets

- **`0x24` OpenContainer** (`OnOpenContainerPacket_0x24`, already registered) with
  `Graphic == 0xFFFF` → open the spellbook for `Serial`. This is the open trigger
  (legacy `OpenContainer`, PacketHandlers.cs:1315). Container plugin already skips
  `0xFFFF`/`0x0030` graphics, so spellbook open must be handled here separately.
- **`0xBF` subcommand `0x1B`** (`OnExtendedCommandPacket_0xBF`, field
  `SpellbookContent` of type `SpellbookContentData { Serial, Graphic, Type, uint[2]
  SpellBitfields }`, already parsed). Delivers which spells are present (64-bit
  bitfield). Triggers content (re)build. Legacy: PacketHandlers.cs:4542.
- The spell list itself comes from the bitfields; the spellbook item's child items
  (`item.Items`, each `Amount` = spell index) are the legacy fallback source in
  `CreateBook`. ECS should prefer the 0xBF bitfields.

## ECS implementation plan

**Plugin**: `SpellbookGumpPlugin` (`internal readonly struct ... : IPlugin`).
**File**: `src/ClassicUO.Ecs/Gameplay/SpellbookGumpPlugin.cs`.

### Resources
- None new strictly required for window state if state lives on components.
- A `SpellbookContentEvent` (EventWriter/Reader) bridged from the 0xBF observer is
  the cleanest decoupling: the 0xBF handler observer sends it; a spawn/update system
  reads it. (Alternatively observe `On<PacketReceived<OnExtendedCommandPacket_0xBF>>`
  and branch on `SpellbookContent.HasValue`, mirroring ServerGumpPlugin's observer
  shape.)

### Components
- `SpellbookWindow { uint Serial; SpellBookType Type; ushort BookGraphic;
  ushort MinimizedGraphic; ushort IconBase; int MaxPage; int ActivePage;
  bool IsMinimized; }` — on the window root (dedup by Serial like PaperdollWindow).
- `SpellbookPageChild { ulong WindowEntity; int Page; }` — on every dynamic page
  child so a rebuild/page-switch can despawn precisely and a visibility-sync system
  can flip `Node.Display` (mirrors `ServerGumpChild` + `PaperdollBodyChild`).
- `SpellbookSpellIcon { ushort SpellId; uint BookSerial; }` — on each detail icon
  for active-spell highlight + cast.
- `SpellbookJumpLink { int TargetPage; int SpellId; }` — on each index link label.
- `SpellbookPageCorner { bool IsRight; }` — on the two corner sprites.
- `SpellbookMinimizeHitbox { ulong WindowEntity; }` — on the spine hitbox.

### Bundle usage
- Root via **`UOGumpBundle`** (or `builder.SpawnUOGump`) with `BackgroundId` = the
  per-type book graphic, `Kind = UOCustomKind.Gump` (single sprite, pixel-perfect),
  `ZOrder` from `UiZCounter.Bump()`. This yields `UIMovable` (drag + right-click
  close), `GlobalZIndex`, `Interaction.None`, and the `Gump`-kind custom render —
  matching the UO Gump Behaviour Contract with zero per-gump reimplementation.
- Children via `GumpBuilder`: `AddGump` (corners, icons), `AddButton` (Magery
  circles), `AddGumpTiled` (reagent separator `0x0835`), `AddLabel` (all text).
  Add invisible hitbox as a plain `Node` + `Interaction.None` + `SpellbookMinimizeHitbox`.

### Observers
- `On<PacketReceived<OnOpenContainerPacket_0x24>>` → if `Graphic == 0xFFFF`, spawn
  (or focus existing by Serial, bump z) the window. Resolve `SpellBookType` from the
  item's `Graphic` via `AssignGraphic`'s mapping (needs the item entity's `Graphic`
  component via `NetworkEntitiesMap` + a `Query<Data<Graphic>>`).
- `On<PacketReceived<OnExtendedCommandPacket_0xBF>>` (guard `SpellbookContent != null`)
  → rebuild the matching window's page subtree from the bitfield (despawn
  `SpellbookPageChild` with `WindowEntity == root`, then rebuild). Same shape as
  `PaperdollPlugin.RebuildOnEquip`.
- Per-child `On<UiClick>` / `On<UiDoubleClick>` observers wired at spawn time (like
  PaperdollPlugin's button `.Observe(...)`): page corners, circle buttons, jump
  links, spell icons, minimize hitbox.

### Systems
- **PageVisibilitySync** (Stage.PostUpdate): for each `SpellbookPageChild`, flip
  `Node.Display` = (Page == 0 || Page == window.ActivePage) ? Flex : None. Copy
  `ServerGumpPlugin.SyncPageVisibility` verbatim in shape.
- **CornerVisibilitySync**: hide left corner on first page, right corner on last.
- **ActiveSpellHighlight** (Stage.Update): query `SpellbookSpellIcon` + the active-
  spell-icons state; write `UOCustomRender.Hue` = hue 38 when active else `UnitZ`.
  Mirrors `HueGumpPic.Update`. (Depends on an ECS equivalent of `ActiveSpellIcons`,
  fed by 0xBF 0x25 `SpellIconSpell`/`SpellIconActive` — see Open Questions.)
- **DisposeOnLogout** (`OnExit(GameState.GameScreen)`): despawn all spellbook
  windows + subtrees, mirroring PaperdollPlugin.DisposeOnLogout.

### Minimize handling
A `On<UiClick>` observer on the minimize hitbox toggles `SpellbookWindow.IsMinimized`;
a tiny system reacts to that flag: swap root `UOCustomRender.AssetId` between
BookGraphic/MinimizedGraphic, resize the root `Node` to the new sprite size, and set
every child `Node.Display` to None/Flex (matching `ContainerGumpPlugin.HandleMinimizeClick`
which is the closest existing pattern). Restore via `On<UiDoubleClick>` on the root.

### New ClayUO custom render command / UiHitTest case
- **None required.** The book bg, corners, circle buttons, reagent separator, and
  icons all use existing kinds (`Gump`, `GumpTiled`, `Art` not needed — icons are
  gump graphics, so `Gump`). `UiHitTest.PixelHit` already covers `Gump` and
  `GumpTiled`. No new enum value, no new switch case, no new hit-test branch.

### Prerequisite port (blocking)
The eight `Spells*.cs` data classes live only in `src/ClassicUO.Client/Game/Data/`.
They must be copied into the ECS stub tree (e.g. `src/ClassicUO.Ecs/Game/Data/`) —
they hold spell names, power-words, reagents, mana/skill, icon ids, circle names.
`SpellBookType` enum and `SpellDefinition` likewise. This is the largest single
piece of work and gates everything but the window chrome.

### Scope cut for v1
Mastery's special "Abilities" icon column (CreateBook lines 342-442) and the icon
drag-out → floating UseSpellButton (lines 827-851) can be deferred. Implement
Magery first (most common), then the other linear books (Necro/Chivalry/Bushido/
Ninjitsu/Spellweaving/Mysticism share the same layout), then Mastery.

## How to trigger for capture

1. Boot ModernUO (127.0.0.1:2593, admin/admin) and the ECS client (`cuo-ecs`),
   log a character into the world.
2. Ensure the character has a spellbook. As admin: `[add spellbook` (Magery) at your
   feet, then pick it up into your backpack — or `[add spellbook` then double-click
   it on the ground. For a fully-populated book use an admin command to fill it
   (e.g. `[fillspellbook` on a recent ModernUO, or add scrolls and "write" them).
   A *full* Magery book is best for a reference screenshot (all 64 spells, all pages).
3. Double-click the spellbook item. Server sends `0x24` (graphic `0xFFFF`) → window
   opens at (64,64) with the open-book sound, followed by `0xBF 0x1B` content.
4. For the index→detail capture: click the right page corner (`0x08BC`) or a Magery
   circle button to page through. For the minimized state: click the spine hitbox
   (~x 0-27, y 98-121 of the window).
5. Harness loop: `up --persist` → `rpc-click` the spellbook serial's double-click →
   `rpc-shot`. Pin window size + server in `settings.json` for deterministic coords
   (see `tools/agent-desktop/AGENTS.md`).

Required game state: a logged-in character holding a non-empty spellbook of the
desired school. An empty book opens but shows only chrome (no spell links/icons).

## Open questions

- **Spell data port**: the `Spells*.cs` + `SpellDefinition` + `SpellBookType` types
  are not in the ECS tree. Where should they land — `src/ClassicUO.Ecs/Game/Data/`
  stub tree, or a shared assembly? This blocks the data-driven content build.
- **ActiveSpellIcons equivalent**: legacy `World.ActiveSpellIcons` (fed by 0xBF 0x25)
  drives the hue-38 highlight. Is there an ECS resource for it yet, or must one be
  added (a `Res<ActiveSpellIcons>` updated by the 0xBF 0x25 observer)?
- **Open trigger ownership**: `0x24` with graphic `0xFFFF` currently has no ECS
  handler (the container plugin skips it). Confirm this packet reaches the spellbook
  observer and isn't swallowed earlier. Also `0x24` graphic `0x0030` = "buy/sell"
  (vendor) — out of scope here.
- **Sound + saved position**: legacy plays `0x0055` on open/page/minimize/close and
  caches the window position per serial. No ECS AudioManager resource and no per-
  serial gump position cache exist for non-server gumps yet — defer or add?
- **Item-child fallback vs bitfield**: legacy `CreateBook` reads spells from the
  spellbook item's child items (`item.Items`, Amount = spell index). The ECS open
  path may have the bitfield (0xBF 0x1B) but not the child-item list. Confirm the
  bitfield always arrives so the child-item fallback isn't needed.
- **Mastery layout**: the Mastery "Abilities" icon column and `SpellsMastery.SpellbookIndices`
  paging differ from the linear books. Confirm v1 can defer Mastery entirely.
- **Fonts**: GumpBuilder.AddLabel uses FontId 0 size 12; legacy uses UO font 6/8/9
  with hue 0x0288. Bevy.UI label fonts may not map 1:1 (same gap PaperdollPlugin
  notes for its title). Confirm acceptable visual fidelity or pre-bake text like
  ServerGumpPlugin.SpawnWrappedText.
