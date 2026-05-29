# Journal Gump Spec

## Overview

The Journal is a **client-side** scrolling log of all game text the player has
seen: speech, system messages, object labels, guild/party chat, spell words,
etc. It is NOT opened by a server packet — the client maintains a rolling
buffer of entries (`JournalManager.Entries`, max `MAX_JOURNAL_HISTORY_COUNT`)
and the gump renders the subset that passes the active filter. New entries are
appended live as messages arrive (the legacy client fires `World.Journal.EntryAdded`).

Two legacy forms exist:

- **`JournalGump`** (legacy) — a fixed `ExpandableScroll` parchment (gump
  `0x1F40`) with 4 fixed filter checkboxes (System / Objects / Client / Guild),
  a dark-mode toggle, a `ScrollFlag`, and a minimize-to-tab `HitBox`.
- **`ResizableJournal`** (modern, the form to spec as PRIMARY) — a translucent
  `AlphaBlendControl` rectangle, resizable, with **user-defined tabs** (each
  tab is a named set of `MessageType` filters stored in
  `ProfileManager.CurrentProfile.JournalTabs`), a `+` add-tab button, per-tab
  right-click context menu (toggle message types / delete tab), and a
  `ScrollBar`.

The gump appears when the user clicks the **Journal** button on the TopBar
(`TopBarPlugin`), or via a keybind, or when restored from a saved gump layout.

## Source of truth

Primary (resizable form to implement):
- `src/ClassicUO.Client/Game/UI/Gumps/ResizableJournal.cs`
  - ctor + layout: lines 52-118
  - constants (`BORDER_WIDTH=4`, `MIN_WIDTH`, `MIN_HEIGHT=100`,
    `SCROLL_BAR_WIDTH=14`, `TAB_WIDTH=80`, `TAB_HEIGHT=30`): lines 22-29
  - `_lastWidth=MIN_WIDTH`, `_lastHeight=350`, `_lastX=100`, `_lastY=100`: lines 48-49
  - `_background` AlphaBlendControl (alpha 0.7, hue 0): lines 64-74
  - `_scrollBarBase` ScrollBar: lines 79-82
  - `_journalArea` JournalEntriesContainer: lines 84-92
  - `_newTabButton` (`+`, w=20, h=TAB_HEIGHT): lines 100-111
  - `BuildTabs` / `AddTab` (NiceButton per tab, width 80, font 1): lines 127-149, 231-251
  - `OnButtonClick` (tab selection + filter swap): lines 219-229
  - `Reposition` (resize → reflow): lines 151-171
  - `JournalEntriesContainer.AddToRenderLists` (clipped scroll render of entries): lines 327-361
  - `JournalEntriesContainer.AddEntry` (timestamp Label + body Label, hue/font from entry): lines 418-440
  - `CanBeDrawn` (filter test against `_currentFilter` MessageType[]): lines 442-463
  - `TabContextEntry` (right-click context menu: toggle MessageType, delete tab): lines 503-624
  - `OnMouseWheel` → scrollbar: lines 213-217

Legacy form (reference only, NOT primary):
- `src/ClassicUO.Client/Game/UI/Gumps/JournalGump.cs`
  - parchment background `ExpandableScroll(0, 22, h, 0x1F40)`, `TitleGumpID=0x82A`: lines 34-40
  - title gump-pic `0x82D` (minimized `0x830`) at (160,0): lines 32, 218
  - dark-mode checkbox (`0x00D2`/`0x00D3`, hue `0x0288`, dark hue 903): lines 42-73
  - 4 filter checkboxes (System/Objects/Client/Guild, font 6, hue `0x0386`): lines 99-195
  - `RenderedTextList` scroll render (timestamp hue 1150 + entry): lines 334-621
  - `ScrollFlag(-25, 58, h, true)`: line 75
  - minimize hitbox `(160,0,23,24)`: lines 91-92

Data + enums:
- `src/ClassicUO.Client/Game/Managers/JournalManager.cs` — `JournalEntry`
  (Text, Font, Hue, Name, IsUnicode, Time, TextType, MessageType), `Add(...)`,
  `EntryAdded` event, `Entries` deque.
- `src/ClassicUO.Ecs/Game/Data/MessageType.cs` — Regular=0, System=1, Emote=2,
  Limit3Spell=3, Label=6, Focus=7, Whisper=8, Yell=9, Spell=10, Guild=13,
  Alliance=14, Command=15, Encoded=0xC0, Party=0xFF.
- `src/ClassicUO.Client/Game/Data/TextType.cs` — CLIENT, SYSTEM, OBJECT, GUILD_ALLY.
- `src/ClassicUO.Ecs/Game/Constants.cs:91` — `MAX_JOURNAL_HISTORY_COUNT = 100`.

## Visual structure (ResizableJournal — primary)

Window root is a resizable frame at `(_lastX=100, _lastY=100)`, default size
`(_lastWidth=MIN_WIDTH, _lastHeight=350)`. `BORDER_WIDTH=4` insets the content.
The frame itself in the legacy form is the `ResizableGump` chrome (border
sprites + resize-drag corner); the visible fill is the translucent background.

Coordinates below are **relative to the window root** unless noted.

| # | Control | Type | Asset / fill | X | Y | W | H | Font / hue / text |
|---|---------|------|--------------|---|---|---|---|-------------------|
| 1 | Window root | ResizableGump frame | (border chrome + resize corner) | 0 | 0 | W | H | — |
| 2 | Background | AlphaBlendControl | translucent black, alpha **0.7**, hue **0x0000** | `BORDER_WIDTH`(4) | 4 | `W - 8` | `H - 8` | — |
| 3 | Tab N (0..n) | NiceButton (selectable) | text button | `i*80 + 4` | 0 | **80** | **30** | font **1**, caption = tab name; selected tab highlighted |
| 4 | New-tab `+` | NiceButton (not selectable) | text button | `tabCount*80 + 4` | 0 | **20** | **30** | `+`, tooltip "Add a new tab" |
| 5 | Scroll bar | ScrollBar | scrollbar sprite | `W - 14 - 4` | `4 + 30` (`=34`) | **14** | `H - 30 - 8` | vertical, value tracks bottom |
| 6 | Journal area | JournalEntriesContainer | clipped scrolling list | `BORDER_WIDTH`(4) | `4 + 30` (`=34`) | `W - 14 - 8` | `H - 8 - 30` | per-entry timestamp + body |

Journal area entry layout (per visible entry, top→bottom, drawn clipped to the
area rect, offset by `-scrollBar.Value`):

- **Timestamp label**: `Label("{Time:t}", IsUnicode, Hue, font=Font)` drawn at
  area-left. (`{:t}` = short time, e.g. "3:45 PM".)
- **Body label**: `Label("{Name}: {Text}", IsUnicode, Hue, maxWidth = areaW - BORDER_WIDTH - timestampWidth, font=Font)`
  drawn at `area-left + timestampWidth + 5`.
- Both use the entry's own `Hue` and `Font` (font 0 unicode / font 9 ascii by
  default, see JournalManager.Add). Entries whose `Name`/`Text` is empty or
  whose `Name` is in the ignore list are skipped.
- Entries are filtered by the active tab's `MessageType[]` (`CanBeDrawn`):
  if `_currentFilter != null`, an entry shows only when its `MessageType` is in
  the filter array; SYSTEM `TextType` shows only when the filter contains
  `MessageType.System`. Null filter = show all.

`MIN_WIDTH = (BORDER_WIDTH*2) + (TAB_WIDTH * tabCount) + 20 = 8 + 80*n + 20`.
With the default profile (typically a single "All"/"Regular" tab) `n=1` →
`MIN_WIDTH = 108`. `MIN_HEIGHT = 100`.

### Legacy JournalGump structure (for reference / parity screenshot)

| Control | Type | Asset | X | Y | W | H | Notes |
|---------|------|-------|---|---|---|---|-------|
| Title bar pic | GumpPic | **0x82D** (min: 0x830) | 160 | 0 | — | — | double-click restores when minimized |
| Parchment bg | ExpandableScroll | **0x1F40**, TitleGumpID **0x82A** | 0 | 22 (`DIFF_Y`) | ~ | `H-22` | expandable height |
| Dark-mode checkbox | Checkbox | **0x00D2**/**0x00D3** | `bg.W - textW - 2` | 29 | — | — | font 6, hue **0x0288**, label "Dark Mode"; dark hue **903** |
| Filter: System | Checkbox | 0x00D2/0x00D3 | 43 | bottom | — | — | font 6, hue **0x0386** |
| Filter: Objects | Checkbox | 0x00D2/0x00D3 | 118 | bottom | — | — | font 6, hue 0x0386 |
| Filter: Client | Checkbox | 0x00D2/0x00D3 | 193 | bottom | — | — | font 6, hue 0x0386 |
| Filter: Guild | Checkbox | 0x00D2/0x00D3 | 268 | bottom | — | — | font 6, hue 0x0386 |
| Entry list | RenderedTextList | — | 25 | 58 | `bg.W - sb.W/2 - 5` | 200 | timestamp hue **1150**, FontStyle BlackBorder+Indention |
| Scroll flag | ScrollFlag | — | -25 | 58 | — | `H-22` | |
| Minimize hitbox | HitBox | — | 160 | 0 | 23 | 24 | mouse-up minimizes |

## Assets

| Asset | ID | Used in | Purpose |
|-------|-----|---------|---------|
| Parchment scroll bg | `0x1F40` | JournalGump | ExpandableScroll background (legacy only) |
| Scroll title gump | `0x82A` | JournalGump | TitleGumpID of ExpandableScroll |
| Title bar pic | `0x82D` | JournalGump | header pic (open) |
| Title bar pic (min) | `0x830` | JournalGump | header pic (minimized) |
| Checkbox unchecked | `0x00D2` | JournalGump | filter / dark-mode checkbox normal |
| Checkbox checked | `0x00D3` | JournalGump | filter / dark-mode checkbox checked |
| (resizable bg) | — none — | ResizableJournal | translucent fill via AlphaBlendControl, not a gump sprite |

| Hue | Value | Use |
|-----|-------|-----|
| Background hue | `0x0000` | AlphaBlendControl tint (resizable) |
| Dark-mode label | `0x0288` | dark-mode checkbox text (legacy) |
| Filter label | `0x0386` | filter checkbox text (legacy) |
| Dark journal | `903` | scroll bg hue when dark mode on (legacy) |
| Timestamp | `1150` | RenderedTextList hour stamp (legacy) |
| Entry hue | per-entry | each `JournalEntry.Hue` (server/client supplied) |

| Font | Value | Use |
|------|-------|-----|
| Tab caption | `1` | NiceButton tab text (resizable) |
| Filter / dark-mode | `6` | checkbox labels (legacy) |
| Entry body | per-entry `Font` | 0 (unicode) or 9 (ascii) by default |
| Timestamp (legacy) | `1` unicode | RenderedTextList hour |

Layout metrics (resizable): `BORDER_WIDTH=4`, `SCROLL_BAR_WIDTH=14`,
`TAB_WIDTH=80`, `TAB_HEIGHT=30`, `MIN_HEIGHT=100`, default H `350`,
`MAX_JOURNAL_HISTORY_COUNT=100`.

## Behaviors

| Behavior | Legacy mechanism | ECS mechanism |
|----------|------------------|---------------|
| **Drag to move** | `CanMove=true`; bg/area `DragBegin → InvokeDragBegin` | `UIMovable` on root + `WindowDragPlugin.Drag` (latches on press, writes `Node.Left/Top`). No per-child drag wiring. |
| **Right-click close** | `CanCloseWithRightClick=true` | `UIMovable` on root + `WindowDragPlugin.CloseOnRightClick`. Journal is NOT a container, so it despawns in-place (no `ContainerClosedEvent`). Spec note: tabs in legacy set `CanCloseWithRightClick=false` so a right-click on a tab opens its context menu, not closes the window — see Open Questions. |
| **Stack on top (z)** | UIManager z order | only the root carries `GlobalZIndex`; `WindowDragPlugin.Drag` bumps via `UiZCounter.Bump()` on latch. Children inherit z at layout. |
| **Click-capture to world** | gump consumes clicks | `WindowDragPlugin.ClaimSelectedFromMovable` (root carries no NetworkSerial → world/pickup bail). |
| **Scroll (wheel)** | `OnMouseWheel → _scrollBarBase.InvokeMouseWheel` | journal area is a clip+scroll container (Overflow.Scroll + `ScrollPosition`); `GuiPlugin.RouteWheelToScrollable` already drives wheel on such containers under the cursor and consumes the notch. |
| **Scrollbar thumb drag** | ScrollBar control | `TinyEcs.Bevy.UI.Widgets.ScrollbarPlugin` (already installed in GuiPlugin) on a `Scrollbar` widget, OR rely on wheel + auto-stick-to-bottom for v1. |
| **Auto-scroll to newest** | `AddEntry`: if at max, keep `Value=MaxValue` | on new entry, if already scrolled to bottom, clamp `ScrollPosition.OffsetY = max`. |
| **Tab select** | `NiceButton` Activate → `OnButtonClick(id)` sets `_currentFilter`, recalcs scrollbar | each tab is a UI node with `On<UiClick>` observer that writes the active filter into a `ResMut<JournalState>` (or marks the root); a refresh re-bakes the visible entry list. Buttons fire on release. |
| **Add tab (`+`)** | MouseUp → `EntryDialog` → add to `JournalTabs`, `ReloadTabs=true` | `+` node `On<UiClick>` → open a name-entry prompt (deferred; see Open Questions) → mutate profile tabs → rebuild tab row. |
| **Tab context menu (right-click on tab)** | `TabContextEntry` (toggle each MessageType, "X Delete Tab" → QuestionGump) | deferred (no ECS context-menu / question-gump infra yet) — see Open Questions. |
| **Dark mode (legacy)** | checkbox toggles `JournalDarkMode`, swaps bg hue 903 | not in resizable form; skip for v1. |
| **Minimize (legacy)** | HitBox mouse-up → swap `0x82D`↔`0x830`, hide children | resizable form has no minimize; skip. |
| **Resize** | `ResizableGump` drag corner → `Reposition()` reflows bg/area/scrollbar | deferred — ECS has no generic resizable-window infra yet. v1 ships a fixed-size window at default `350` height; see Open Questions. |
| **Live entry append** | `World.Journal.EntryAdded += AddJournalEntry` | observe the message stream (see ECS plan): append to a `Res<JournalLog>` ring buffer and dirty the open window so it re-bakes. |
| **Ignore filter** | skip entries whose Name ∈ IgnoredCharsList | mirror against the ECS ignore store if present; else skip (no-op) for v1. |

## Server packets

**None open or update this gump directly.** The Journal is client-side. Its
content is fed indirectly by the text-message packets that the client already
parses:

- `0x1C` ASCII speech (`OnAsciiSpeechPacket_0x1C`)
- `0xAE` Unicode speech (`OnUnicodeSpeechPacket_0xAE`)
- `0xC1` localized message (`OnClilocMessagePacket_0xC1`)
- `0xCC` localized message + affix (`OnClilocMessageAffixPacket_0xCC`)

In legacy these route through `JournalManager.Add(...)`. In ECS they currently
surface as `TextOverheadEvent` / `HostMessage.MessageReceived`
(`Gameplay/Chat/TextOverheadPlugin.cs`). The journal feed must tap the same
data — opcodes are listed here only as the upstream source, not as gump-open
triggers.

## ECS implementation plan

Proposed file: `src/ClassicUO.Ecs/Gameplay/JournalPlugin.cs`
Plugin: `internal readonly struct JournalPlugin : IPlugin`, composed in
`Boot.cs` `CuoPlugin.Build` next to `PaperdollPlugin`.

### Resources

- `JournalLog` (`Res`/`ResMut`) — singleton ring buffer of journal entries
  (max `Constants.MAX_JOURNAL_HISTORY_COUNT = 100`). Each entry mirrors
  `JournalEntry`: `string Name, Text; ushort Hue; byte Font; bool IsUnicode;
  float Time; MessageType MessageType; TextType TextType`. Use `Time.Total`
  for the timestamp source (NOT `DateTime.Now`) per ECS rules; format for
  display from a stored absolute total, or store a wall-clock string at append
  time only if `{:t}` display is required (flag in Open Questions).
  Registered via `app.AddResource(new JournalLog())`.
- Reuse `UiZCounter` (already a resource from `WindowDragPlugin`).

### Components

- `JournalWindow { }` — marker on the gump root (dedup open, despawn-on-logout,
  find-for-refresh). Mirrors `PaperdollWindow`.
- `JournalEntryArea { ulong RootEntity; }` — marker on the scroll container so
  the refresh system can find + rebuild the entry list.
- `JournalTabUI { int Index; }` — marker on each tab node; click observer reads
  this to set the active filter.
- `JournalChild { ulong WindowEntity; }` — tag on dynamic children (tab row +
  baked entry image) so refresh can despawn precisely, mirroring
  `PaperdollBodyChild`.

### Spawn (bundle usage)

Open path (TopBar Journal button — see TopBarPlugin gap):
1. Dedup: if a `JournalWindow` already exists, bump its `GlobalZIndex` and return.
2. Otherwise build the root. Because the resizable journal has **no background
   gump sprite** (it uses a translucent fill), spawn the root as a movable
   surface the same way `ServerGumpPlugin` does for sprite-less gumps:
   `Node` (absolute, default 108×350 or wider) + `UiCustom { UOCustomRender {
   Kind = UOCustomKind.None } }` + `Interaction.None` + `UIMovable` +
   `GlobalZIndex(zCounter.Bump())` + `JournalWindow`.
   - Optionally add a child `BackgroundColor(new Clay.Color(0,0,0,178))` node
     (alpha ≈ 0.7×255) inset by `BORDER_WIDTH` to reproduce the AlphaBlendControl
     fill. (`ServerGumpPlugin` uses this exact `BackgroundColor` pattern for
     `checkertrans`.)
   - `UOGumpBundle` itself is for gump-sprite-backed windows; the journal has
     no sprite, so follow the `UOCustomKind.None` root recipe instead. (If a
     future variant uses a real bg gump, switch to `builder.SpawnUOGump`.)
3. Build the tab row: one node per `JournalTabs` entry via `builder.AddLabel`
   (or a custom button node) at `(i*80+4, 0)`, size `80×30`, tagged
   `JournalTabUI{Index=i}` + `Interaction.None` + `JournalChild`. The `+` node
   at `(tabCount*80+4, 0)` size `20×30`.
4. Build the scroll area: an outer node at `(4, 34)`, size
   `(W-22, H-38)`, `Overflow = Overflow.Scroll`, `ScrollPosition` component,
   tagged `JournalEntryArea{RootEntity}` + `JournalChild`. Inner content is a
   baked-text image (see below).

### Entry rendering (custom render — reuse, don't add a new ClayUO command)

The journal entry list is **per-entry hued + per-entry font** wrapped text.
Clay.NET does not wrap, so reuse the existing baked-texture path that
`ServerGumpPlugin.SpawnWrappedText` uses: `UoFontRenderer.Bake(text, font, hue,
maxWidth, isHtml=false)` → `Texture2D` → a `UiImage` node inside the scroll
container.

- v1 simple path: bake **one** image per visible entry as
  `"{time} {Name}: {Text}"` (a single combined string per entry, matching the
  legacy timestamp+body composition closely enough) at the entry's `Font`/`Hue`,
  width = `areaW - BORDER_WIDTH`. Stack the per-entry images vertically inside
  the inner content node; the outer container scrolls.
- This means **no new `ClayUOCommandType` and no new `UiHitTest` case are
  required** — entries are `UiImage`, the root is `UOCustomKind.None` (already
  bbox-opaque in `UiHitTest`), and the wheel/scroll path already supports
  `ScrollPosition` containers. This is the key reuse: the journal rides
  entirely on existing infra.
- (Alternative considered: a new `ClayUOCommandType.JournalText` that draws the
  entry deque directly with `UltimaBatcher2D` + clip, mirroring
  `JournalEntriesContainer.AddToRenderLists`. Rejected for v1 — more code, and
  the baked-image path already exists and is proven by ServerGump html text.)

### Observers / systems

- **Open observer/system** — wire the TopBar **Journal** button (currently a
  no-op in `TopBarPlugin.cs`): its `On<UiClick>` spawns/focuses the window.
  (Paperdoll uses `On<UiPointerDown>`; journal is a window-open toggle so
  `On<UiClick>` fire-on-release is correct.)
- **Append observer/system** — read the message stream (`EventReader<TextOverheadEvent>`
  or a dedicated journal event mirrored from the cliloc/speech packet handlers)
  and push into `ResMut<JournalLog>`, trimming to 100. Then mark every open
  `JournalWindow` dirty (e.g. insert a `JournalDirty` tag via `Commands`) so a
  refresh system re-bakes. Prefer an observer keyed on the message event over
  per-frame polling (ECS rule 4).
- **Refresh system** — on `JournalDirty` windows (or `Changed`), despawn the
  `JournalChild` entry images under the area and rebuild from `JournalLog`
  filtered by the window's active tab filter; re-clamp scroll to bottom if it
  was at bottom. Mirrors `PaperdollPlugin.RebuildOnEquip` despawn-then-rebuild.
- **Tab-click observer** — `On<UiClick>` on `JournalTabUI` nodes: set the
  window's active filter (store filter index on a `JournalWindow` field or a
  per-window `JournalActiveFilter` component) and mark dirty.
- **Despawn-on-logout system** — `OnExit(GameState.GameScreen)` despawns all
  `JournalWindow` subtrees (mirror `PaperdollPlugin.DisposeOnLogout`).

### ECS-rule conformance checklist

- No `World.*` access — all reads via `Query`, all structural change via
  `Commands`, singletons via `Res`/`ResMut`, scratch via `Local`. ✔
- Time via `Res<Time>.Total`, never `DateTime.Now`/`TickCount`. ✔ (see Open Q
  on `{:t}` display).
- Mutation of existing component fields (scroll offset, filter) via query
  `.Get(id).Ref`, not `Commands`. ✔
- Window behaviours (drag / right-click close / z / click-capture / wheel)
  inherited from `UIMovable` + `WindowDragPlugin` + `GuiPlugin`, NOT
  reimplemented. ✔
- Buttons (tabs, `+`) fire on release via `On<UiClick>`. ✔
- Composite param object (`JournalSpawnParams : CompositeSystemParam`) to keep
  observer arity small, mirroring `PaperdollSpawnParams`. ✔

## How to trigger for capture

Game state required: connected to ModernUO (`127.0.0.1:2593`, `admin/admin`),
character in-world (GameScreen). The Journal needs at least a few text entries
to be visually meaningful, so generate some chat/system text first.

Steps (agent harness, `tools/agent-desktop`):
1. `up --persist` (build with `-p:AGENT_BUILD=true`, boot to GameScreen with
   pinned `settings.json` window size + server).
2. Produce journal content: `rpc-type` a line of chat (Enter sends speech →
   server echoes → message stream), or wait for ambient system messages.
3. Open the gump: `rpc-click` the **Journal** button on the TopBar (4th action
   button, caption "Journal", large 0x098D graphic; located at the computed X
   in `TopBarPlugin.Spawn` — Map(small)+Paperdoll(large)+Inventory(large)
   precede it, start X=30). NOTE: the TopBar Journal button is currently a
   no-op — wiring it is part of this implementation. Until wired, open by
   directly invoking the spawn system from the harness, or add a temporary
   keybind.
4. `rpc-shot` to capture.
5. `down`.

Legacy parity reference: run the OOP `ClassicUO.Client` build and open Journal
from its TopBar to screenshot `ResizableJournal` for pixel comparison.

## Open questions

1. **Resize**: ECS has no generic resizable-window infra (`ResizableGump`
   equivalent). v1 should ship a fixed default-size window (108×350 or wider).
   Is resize required for parity sign-off, or deferred to a later phase?
2. **Tabs persistence**: tabs live in `ProfileManager.CurrentProfile.JournalTabs`
   (a `Dictionary<string, MessageType[]>`). Does the ECS profile/config layer
   expose `JournalTabs`? If not, v1 may hardcode a single "All" tab (null
   filter = show everything) and defer user tabs.
3. **Add-tab / context-menu / delete-tab**: these need an `EntryDialog`
   (name prompt), `ContextMenuControl`, and `QuestionGump` — none exist in ECS
   yet. Defer the `+` button, per-tab right-click menu, and delete to a later
   phase; v1 ships read-only tabs (or single tab)?
4. **Right-click on a tab vs. close window**: legacy tabs set
   `CanCloseWithRightClick=false` and use right-click for the context menu, but
   the window root closes on right-click. In ECS, `CloseOnRightClick` hits the
   topmost `UIMovable` under the cursor pixel-perfectly — a tab node is a child
   (no `UIMovable`), so right-clicking a tab would still close the window unless
   the tab consumes the right-click first. Decide: (a) defer context menu so
   right-click-anywhere closes (simplest v1), or (b) add a right-click consume
   on tab nodes.
5. **Timestamp display**: legacy shows wall-clock `{Time:t}` ("3:45 PM").
   ECS rules forbid `DateTime.Now` in systems. Options: store the wall-clock
   string once at append time inside the packet-handler boundary (acceptable —
   it's at the network boundary, not a system clock read in a per-frame
   system), or display an engine-relative timestamp. Confirm desired format.
6. **Data feed**: should the journal tap `TextOverheadEvent` (overhead-only
   message types) or a broader stream? Legacy `JournalManager.Add` is called
   for MANY message types beyond overhead (system, client, object labels,
   guild, party). `TextOverheadPlugin` only forwards a subset
   (Regular/Spell/Whisper/Yell/Label/Limit3Spell). The journal likely needs a
   dedicated event emitted by ALL the cliloc/speech packet handlers, or a
   `JournalLog.Add` call inside each handler. Confirm the canonical feed.
7. **Ignore list**: is there an ECS `IgnoreManager` equivalent? Legacy skips
   entries whose `Name` is ignored. If absent, v1 skips the ignore filter.
8. **Scrollbar widget vs. wheel-only**: ship a visible draggable scrollbar
   (needs a `Scrollbar` widget + sprite) or rely on mouse-wheel + auto-bottom
   for v1?
