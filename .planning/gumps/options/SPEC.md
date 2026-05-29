# Options Gump Spec

## Overview

The Options gump is the client's master settings window — a large (700×500), multi-tab,
client-side-only configuration panel. It is **not** a UO server gump: nothing on the wire
opens or updates it. It is opened locally by the client (paperdoll Options button, or a
hotkey/macro in OOP) and reads/writes the active `Profile` + `GlobalSettings`. Tabs along a
left rail switch the visible page; each page is a vertically scrolling list of settings
controls (checkboxes, sliders, comboboxes, color pickers, input fields, font selectors).
Bottom-right has four action buttons: Cancel, Apply, Default, OK.

Because it is purely client-side, the ECS port's value is parity of layout + behaviour, and a
durable settings store the controls read/write. There is currently **no ECS Profile/Settings
mutation layer** wired to gameplay systems, which is the main open question (see below).

## Source of truth

- `src/ClassicUO.Client/Game/UI/Gumps/OptionsGump.cs` (~4942 lines, the whole file).

Key landmarks:
- Constants: `OptionsGump.cs:26-31` — `FONT = 0xFF`, `HUE_FONT = 0xFFFF`, `WIDTH = 700`,
  `HEIGHT = 500`, `TEXTBOX_HEIGHT = 25`, `SCREEN_ZOOM_STEPS = 20`.
- Ctor / chrome (background, tab rail, separator lines, bottom buttons): `OptionsGump.cs:160-426`.
- Tab page builders:
  - `BuildGeneral` (page 1) — `442-1439`
  - `BuildSounds` (page 2) — `1441-1559`
  - `BuildVideo` (page 3) — `1561-1971`
  - `BuildCommands`/Macros (page 4) — `1974-2259`
  - `BuildTooltip` (page 5) — `2261-2367`
  - `BuildFonts` (page 6) — `2369-2436`
  - `BuildSpeech` (page 7) — `2438-2769`
  - `BuildCombat` (page 8) — `2771-3021`
  - `BuildCounters` (page 9) — `3023-3146`
  - `BuildInfoBar` (page 10) — `3224-3350`
  - `BuildContainers` (page 11) — `3352-3534`
  - `BuildExperimental` (page 12) — `3148-3221`
- `OnButtonClick` (Cancel/Apply/Default/OK/IgnoreList) — `3537-3580`.
- `SetDefault` (per-page reset values, authoritative defaults) — `3582-3814`.
- `Apply` (profile write-back, side effects) — `3816-` onward (extends past 3930).
- Control factory helpers: `AddInputField` `4444`, `AddLabel` `4495`, `AddCheckBox` `4508`,
  `AddCombobox` `4529`, `AddHSlider` `4549`, `AddColorBox` `4579`, `AddSettingsSection` `4605`.
- `Buttons` enum — `4635-4658`.
- `SettingsSection` nested control (indent/right-flow layout helper) — `4661`+.
- `GetScreenZoom` — `4925`.

## Visual structure

All coordinates below are gump-local (window origin top-left). The window itself is positioned
at (0,0) in OOP and dragged from there.

### Window chrome (every page)

| Element | Type | Asset | x | y | w | h | Notes |
|---------|------|-------|---|---|---|---|-------|
| Background | `AlphaBlendControl(0.95)` | none (solid fill) | 1 | 1 | 698 (`WIDTH-2`) | 498 (`HEIGHT-2`) | Hue 999, alpha 0.95. NOT a gump sprite — a translucent dark rect. |
| Tab buttons ×13 | `NiceButton` | none (text + highlight) | 10 | `10 + 30*i` | 140 | 25 | i=0..12. Captions below. First (General) `IsSelected`. `ButtonParameter` = page number 1..12; last is IgnoreList (Activate). |
| Vertical rail separator | `Line` | color `Color.Gray` | 160 | 5 | 1 | 490 (`HEIGHT-10`) | Divides tab rail from content. |
| Bottom separator | `Line` | color `Color.Gray` | 160 | 441 (`405+35+1`) | 540 (`WIDTH-160`) | 1 | Above action buttons. |
| Cancel button | `Button` | gump up `0x00F3`, over `0x00F1`, down `0x00F2` | 214 (`154+60`) | 465 (`405+60`) | — | — | `Buttons.Cancel`, Activate. |
| Apply button | `Button` | up `0x00EF`, over `0x00F0`, down `0x00EE` | 308 (`248+60`) | 465 | — | — | `Buttons.Apply`. |
| Default button | `Button` | up `0x00F6`, over `0x00F4`, down `0x00F5` | 406 (`346+60`) | 465 | — | — | `Buttons.Default`. |
| OK button | `Button` | up `0x00F9`, over `0x00F8`, down `0x00F7` | 503 (`443+60`) | 465 | — | — | `Buttons.Ok`. |

Tab captions (top to bottom, from `ResGumps`): General, Sound, Video, Macros, Tooltip, Fonts,
Speech, Combat / Spells, Counters, Info Bar, Containers, Experimental, Ignore List Manager.
(`offsetX=60, offsetY=60` are added to the raw button x/y in ctor.)

### Content area (per page)

Each page is built into a `ScrollArea` at **(190, 20), width `WIDTH-210` = 490, height 420**,
with vertical scroll enabled (`BuildCommands` uses a narrower 150-wide list area — see below).
Inside the scroll area, complex pages use a `DataBox` + `SettingsSection` layout: a section has
a bold title label, then a vertical stack of rows; `section.Add` places a control on a new row,
`section.AddRight` places it to the right of the previous, `PushIndent/PopIndent` shift x.

Per-page control inventory (summarized — exact strings are `ResGumps.*` keys, hues are
`HUE_FONT = 0xFFFF` for labels/checkboxes unless a color box):

**Page 1 — General** (`442-1439`), 5 sections:
- *General*: checkboxes HighlightObjects, EnablePathfinding (+ ShiftPathfinding right),
  AlwaysRun (+ AlwaysRunHidden right), FastRotation, AutoOpenDoors (+ SmoothDoors right),
  AutoOpenCorpses (+ indented CorpseOpenRange InputField 50×25, SkipEmptyCorpses, CorpseOpenOptions
  Combobox w=150), OutOfRangeColor, SallosEasyGrab, ShowHousesContent (CV≥70796), SmoothBoat (CV≥7090).
- *Mobiles*: ShowHP + HP-type Combobox (w=100) + HP-mode Combobox (w=100); Highlight Poisoned/
  Paralyzed/Invul each with a ClickableColorBox (13×14) + label; ShowIncMobiles, ShowIncCorpses;
  AuraUnderFeet Combobox (w=100); PartyAura checkbox + PartyAuraColor box.
- *Gumps & Context*: DisableMenu, AltCloseGumps, AltMoveGumps, ClickCloseAllGumps,
  StandardSkillGump, UseOldStatusGump, StatusGumpBarMutuallyExclusive, ShowGumpPartyInv,
  UseCustomHPBars (+ UseBlackBackgr right), SaveHPBarsOnLogout, CloseHPGumpWhen Combobox (w=150),
  GridLoot Combobox (w=120), ShiftContext, ShiftStack.
- *Miscellaneous*: EnableCircleTrans + radius HSlider (w=200), CircleTransType Combobox (w=150),
  HideScreenshotStoredInMessage, ObjAlphaFading, TextAlphaFading, ShowTarRangeIndicator,
  name-overhead checkboxes ×2, EnableDragSelect (+ DragKey Combobox w=100, DragHumanoidsOnly,
  DragHostileOnly, DragSelectStartX/Y HSliders w=200, DragSelectAnchoredHB), ShowStatsChanged,
  ShowSkillsChanged + delta HSlider (w=150), `SetAsNewDefault` NiceButton (w=section-18, h=25).
- *Terrain & Statics*: HideRoofTiles, TreesStumps, HideVegetation, MarkCaveTiles, HPFields
  Combobox (w=150).

**Page 2 — Sound** (`1441-1559`): checkboxes Sounds, Music, LoginMusic; three HSliders
(SoundVolume / MusicVolume / LoginMusicVolume) each w=200 at x=120; FootSteps, CombatMusic,
ReproduceSoundsAndMusic (background) checkboxes. Layout is manual y-stacking (no DataBox).

**Page 3 — Video** (`1561-1971`): FPS label + HSlider (min/max FPS, w=250); ReduceFPSWhenInactive;
then DataBox sections: *Game window* (Fullsize, Borderless, Lock, position X/Y InputFields 50×25,
size W/H InputFields), *Zoom* (ScreenZoom HSlider w=250 range ±20, DefaultZoom HSlider w=100,
EnableMouseWheelForZoom, ReleasingCtrlRestoresScale), *Lights* (AlternativeLights, LightLevel +
HSlider w=250 range 0..0x1E, LightLevelType Combobox w=150, DarkNights, UseColoredLights),
*Misc* (DeathScreen, BlackWhiteForDead, RunMouseSeparateThread, AuraOnMouse, AnimatedWater),
*Shadows* (Shadows, indented ShadowStatics, TerrainShadowsLevel HSlider w=200).

**Page 4 — Macros** (`1974-2259`): a narrow `ScrollArea` (190, 81, 150, 360) list of macro
NiceButtons (w=130, h=25) on the left, plus *New Macro* (`190,20,130×20`) / *Delete Macro*
(`190,52,130×20`) NiceButtons, two `Line` separators. Selecting a macro shows a `MacroControl`
editor at (400, 20). Drag a macro button → spawns a `MacroButtonGump`. This page is the most
complex and stateful (depends on `World.Macros`).

**Page 5 — Tooltip** (`2261-2367`): UseTooltip checkbox; DelayBeforeDisplay HSlider (0..1000,
w=200), TooltipZoom HSlider (100..200, w=200), TooltipBackgroundOpacity HSlider (0..100, w=200),
TooltipFontHue ClickableColorBox, `FontSelector(7, ...)` for tooltip font.

**Page 6 — Fonts** (`2369-2436`): OverrideGameFont checkbox + ASCII/Unicode Combobox (w=100);
ForceUnicodeInJournal checkbox; SpeechFont label + `FontSelector(20, ...)` (chat font picker).

**Page 7 — Speech** (`2438-2769`): ScaleSpeechDelay + delay HSlider (0..1000, w=180);
SaveJournalToFile; MaxJournalFiles checkbox + InputField (50×25); JournalFileWithSerial;
chat checkboxes (ActivateChatAfterEnter, AdditionalButtons, ShiftEnter, HideChatGradient),
IgnoreGuild, IgnoreAlliance, UseAlternateJournal, OverheadPartyMessages; `RandomizeSpeechHues`
NiceButton (140×25); then a 2-column grid of ClickableColorBoxes: Speech/Emote, Yell/Whisper,
PartyMessage/GuildMessage, AllyMessage/ChatMessage (each box 13×14, second column +200x).

**Page 8 — Combat / Spells** (`2771-3021`): checkboxes NewTargetSystem, TabCombat, QueryAttack,
QueryBeneficialActs, EnableOverheadSpellFormat, EnableOverheadSpellHue, UIButtonsSingleClick,
ShowBuffDuration, EnableFastSpellsAssign, ShowDPSWithDamage; then color-box columns: left column
Innocent/Friend/Criminal/CanAttack/Murderer/Enemy, right column (+200x) Benefic/Harmful/Neutral;
SpellOverheadFormat InputField (200×25).

**Page 9 — Counters** (`3023-3146`): EnableCounters; HighlightOnChange; EnableAbbreviatedAmount +
abbreviated-amount InputField (50×25); HighlightRedWhenBelow + amount InputField (50×25);
CounterLayout / CellSize HSlider (30..80, w=80).

**Page 10 — Info Bar** (`3224-3350`): ShowInfoBar checkbox; DataHighlightType Combobox (w=150);
`Add Item` NiceButton (90×20); column header labels Label/Color/Data + a `Line`; a `DataBox` of
`InfoBarBuilderControl` rows (each = label InputField + color box + data Combobox), built from
`World.InfoBars.GetInfoBars()`.

**Page 11 — Containers** (`3352-3534`): BackpackStyle Combobox (w=200, CV≥705301); ContainerScale
HSlider (w=200); checkboxes ScaleItemsInsideContainers, UseLargeContainersGump (CV≥706000),
DoubleClickLootContainers, RelativeDragAndDrop, HighlightContainerWhenSelected, HueContainerGumps,
OverrideContainerGumpLocation + location Combobox (w=200); `RebuildContainers` NiceButton (130×30).

**Page 12 — Experimental** (`3148-3221`): checkboxes DisableDefaultUOHotkeys, DisableArrowsMovement,
DisableTab, DisableMessageHistory, DisableClickAutomove.

## Assets

| Asset / value | Kind | Used by |
|---------------|------|---------|
| (none — `AlphaBlendControl`) | solid translucent rect, hue 999, alpha 0.95 | window background |
| `0x00F3 / 0x00F1 / 0x00F2` | gump (up/over/down) | Cancel button |
| `0x00EF / 0x00F0 / 0x00EE` | gump | Apply button |
| `0x00F6 / 0x00F4 / 0x00F5` | gump | Default button |
| `0x00F9 / 0x00F8 / 0x00F7` | gump | OK button |
| `0x00D2 / 0x00D3` | gump (unchecked / checked) | every `Checkbox` (`AddCheckBox`) |
| `0x0BB8` | gump (resizepic bg) | `InputField` background, `Combobox` background |
| `0x00FC` | gump | Combobox dropdown arrow (Combobox.cs:70) |
| `Color.Gray.PackedValue` | line color | the three `Line` separators |
| Hue `999` | hue | background tint |
| Hue `0xFFFF` (`HUE_FONT`) | font hue | all labels / checkbox text / sliders / input text |
| Hue `0x0453` | font hue | Combobox label text (OOP Combobox.cs) |
| Hue `0x0386` | font hue | (referenced elsewhere; paperdoll title — not options) |
| Font `0xFF` (`FONT`) | font | all options text controls |
| ColorBox swatch | 13×14 rect filled with the picked hue | every `ClickableColorBox` |

There is **no single window-background gump sprite** — the panel is a translucent rect, not a
resizepic. That is a notable departure from paperdoll/container windows.

Default hue values worth porting (from `SetDefault`): poison `0x0044`, paralyzed `0x014C`,
invul `0x0030`, partyAura `0x0044`; speech `0x02B2`, emote/yell `0x0021`, whisper `0x0033`,
party/guild `0x0044`, ally `0x0057`, chat `0x0256`; innocent `0x005A`, friend `0x0044`,
criminal/canattack/neutral `0x03B2`, murderer `0x0023`, enemy `0x0031`, benefic `0x0059`,
harmful `0x0020`.

## Behaviors

| Behavior | OOP source | ECS mechanism |
|----------|-----------|---------------|
| Drag to move | `CanMove = true` (ctor 409) | Tag root `UIMovable`; `WindowDragPlugin.Drag` handles it. Background must carry a `UiCustom`/`ComputedNode` hit surface (use `UOCustomKind.None` solid fill since there's no bg sprite). |
| Right-click closes | `CanCloseWithRightClick = true` (410) | `UIMovable` + `WindowDragPlugin.CloseOnRightClick` despawns the subtree. NOT a container, so the generic despawn path applies (no `ContainerClosedEvent`). |
| Topmost on click / z-stack | OOP UIManager focus | Root carries the only `GlobalZIndex`; `WindowDragPlugin` bumps via `UiZCounter` on drag-latch. Children inherit z at layout. |
| Click-capture vs world | OOP modal-ish gump | `WindowDragPlugin.ClaimSelectedFromMovable` (Stage.Last) at `float.MaxValue` — falls out of `UIMovable`. With `UOCustomKind.None` the whole 700×500 rect captures (matches a solid panel). |
| Tab switch (pages) | `NiceButton(ButtonAction.SwitchPage)` → `ChangePage(n)` (425, 185) | Page-visibility component on each content child + a `CurrentPage` on the root, flipped by tab-button `On<UiClick>` observers — mirror `ServerGumpPlugin`'s `ServerGumpChild.Page` + `SyncPageVisibility` (PostUpdate) pattern. Tab buttons fire on release. |
| Buttons fire on release | OOP `MouseUp` / `ButtonAction.Activate` (OnButtonClick) | `On<UiClick>` observers on each action button (Cancel/Apply/Default/OK) and tab buttons. Never `UiPointerDown` (that is drag/focus only). |
| Vertical scroll per page | `ScrollArea` | A clipping outer Node with `Overflow.Scroll` + `ScrollPosition` component and an inner content Node, exactly as `ServerGumpPlugin.SpawnWrappedText`. Clay handles wheel scroll. |
| Checkbox toggle | `Checkbox.IsChecked` | `On<UiClick>` observer flips a `CheckboxState` component + swaps the `UOCustomRender.AssetId` between `0x00D2`/`0x00D3` in place. |
| Slider drag | `HSliderBar` | New draggable-thumb widget (no ECS slider exists yet) — gap. |
| Combobox dropdown | `Combobox` | New popup-list widget (no ECS combobox exists yet) — gap. |
| Color picker | `ClickableColorBox` → `ColorPickerGump` | New swatch + color-picker popup — gap. |
| Input field | `InputField` (text/number) | Needs a Bevy.UI text-input widget — gap (ServerGump only renders text, no editing). |
| Apply / Default / Cancel / OK | `Apply()` / `SetDefault()` / `Dispose()` (3537+) | `On<UiClick>` observers that read every control's state component and write a `Profile`/`Settings` resource; Default resets state components to the page's defaults; Cancel/OK despawn. **Requires an ECS settings store + write-back path that does not yet exist.** |

## Server packets

**None.** OptionsGump is entirely client-side. It is not opened by `0xB0`/`0xDD` (those are
generic server gumps handled by `ServerGumpPlugin`), and there is no open/update opcode. Apply
may trigger outgoing requests indirectly (e.g. refresh-rate, skill-gump swap), but the gump
itself is never server-driven.

## ECS implementation plan

**Plugin**: `internal readonly struct OptionsGumpPlugin : IPlugin` at
`src/ClassicUO.Ecs/UI/OptionsGumpPlugin.cs`. Compose in `Boot.cs` (`CuoPlugin.Build`).

**Open trigger (wire the existing stub)**: `PaperdollPlugin.cs:299-301` already spawns the
paperdoll Options button (`0x07D6/0x07D7/0x07D8`) with a no-op `On<UiClick>` that logs
`"[Paperdoll] Options clicked — no ECS OptionsGump"`. Replace that no-op with an
`EventWriter<OpenOptionsEvent>` send (or a direct spawn observer). Add an `OpenOptionsEvent`
and an observer in `OptionsGumpPlugin` that spawns the window (dedup: only one Options window —
focus the existing root if present, mirroring `PaperdollPlugin.SpawnOnOpenPaperdoll`).

**Resources / components**:
- `Res<OptionsSettings>` (or reuse/introduce an ECS `Profile` resource) — the read/write store
  the controls bind to. **This is the load-bearing gap**: there is no ECS profile/settings
  mutation layer today. Minimum viable: a resource snapshotting the values, with Apply writing
  it back to `Configuration/Settings.cs` + emitting any side-effect events.
- `OptionsWindow` marker on the root (dedup + dispose-on-logout, like `PaperdollWindow`).
- `OptionsPage { int Current }` on the root + `OptionsPageChild { ulong Root; int Page }` on each
  content child — drives page visibility (copy `ServerGumpChild`/`ServerGump`+`SyncPageVisibility`).
- `OptionsTab { int Page }` on each tab NiceButton (click sets `OptionsPage.Current`).
- Per-control state components for the new widgets: `CheckboxState { bool Checked; ushort Off; ushort On; }`,
  plus slider/combobox/colorbox/input states once those widgets exist.

**Bundle usage**: the window background is a translucent panel, not a gump sprite, so do **not**
use `UOGumpBundle` (which always inserts a `UOCustomRender` gump/nine-patch). Instead spawn the
root with: `Node` (700×500 absolute) + `UiCustom { UOCustomKind.None }` (solid hit surface, like
`ServerGumpPlugin`'s no-resizepic root at lines 629-645) + `Interaction.None` + `UIMovable` +
`GlobalZIndex`, then add a `BackgroundColor` child (translucent dark, alpha ~0.95, mirroring
`AlphaBlendControl`). This gives drag / right-click-close / z-stack / click-capture for free via
`WindowDragPlugin` while matching the non-sprite background. Add a marker so the click-capture
path treats it like any movable (it is `Without<ContainerWindow>`, so `ClaimSelectedFromMovable`
already handles it).

**Tab rail + content**: build 13 `GumpBuilder.AddLabel`-style NiceButton equivalents on the left
(text + selection highlight; a small new control or reuse a labelled `BackgroundColor` box).
Build each content page's children tagged `OptionsPageChild { Root, Page = n }`, parented to a
scroll container (`Overflow.Scroll` + `ScrollPosition` as in `ServerGumpPlugin.SpawnWrappedText`).

**Systems / observers**:
- Observer `On<OpenOptionsEvent>` → spawn-or-focus.
- Observer per tab button `On<UiClick>` → set `OptionsPage.Current` (or tag a page-request like
  `ServerGumpPageRequest`).
- System `SyncOptionsPageVisibility` (Stage.PostUpdate) → flip each `OptionsPageChild`'s
  `Node.Display` to match `OptionsPage.Current` (page 0 always shown for shared chrome).
- Observers `On<UiClick>` on Cancel (despawn), Apply (write-back), Default (reset state comps for
  current page), OK (write-back + despawn). Use the canonical right-click close for the X/close;
  Cancel/OK despawn via `Commands` + subtree walk (copy `WindowDragPlugin.DespawnSubtree`).
- `DisposeOnLogout` system `OnExit(GameState.GameScreen)` despawning the window (like
  `PaperdollPlugin.DisposeOnLogout`).

**New ClayUO custom render command / UiHitTest**: the background panel needs **no** new ClayUO
command (`BackgroundColor` + `UOCustomKind.None` already render and hit-test). New *widgets*
(slider thumb, combobox dropdown, color swatch, text input) will each need rendering — sliders
and color swatches can likely be drawn with existing `BackgroundColor`/gump sprites; a text-input
caret/selection and the combobox popup are the most likely candidates for a new ClayUO primitive,
but only if a Bevy.UI widget can't cover them. Checkbox/button reuse existing `UOCustomKind.Gump`
sprite swapping (no new case). `UiHitTest` needs no new case for the panel (`None` is bbox-solid).

**Conformance to CLAUDE.md**: all mutation through `Commands`; existence/read via queries
(`q.Contains`/`q.Get`), never `World`; page state as components + a `Res` settings store, never
static; tab/action wiring via `On<UiClick>` observers; drag/close/z from `UIMovable`. No
closure-captured mutable state — bind control values to components, not lambda locals.

## How to trigger for capture

1. Boot ModernUO (`127.0.0.1:2593`, `admin/admin`) and log a character into the world (the gump
   is a GameScreen UI, unavailable at the login screen).
2. Open the paperdoll (top-bar Paperdoll button, or double-click the player → server pushes
   `0x88` → `PaperdollPlugin` spawns it).
3. On the **player's own** paperdoll, click the **Options** button (gump `0x07D6`, the second
   button in the right-hand column at paperdoll-local `(185, 71)`). In OOP this opens
   `OptionsGump`. In the current ECS branch this logs a no-op (`PaperdollPlugin.cs:300`) — so for
   a **reference screenshot of the real gump**, capture against the **legacy OOP client**
   (`ClassicUO.Client`), where the button is wired, not the ECS exe.
4. Page through the 12 tabs (left rail buttons) for per-tab reference shots; the gump opens on
   page 1 (General) by default (`ChangePage(1)` at ctor `425`).

Required game state: in-world, a valid active `Profile` loaded (the gump reads
`ProfileManager.CurrentProfile`). Some controls are version-gated (ShowHousesContent CV≥70796,
SmoothBoat CV≥7090, BackpackStyle CV≥705301, UseLargeContainersGump CV≥706000) — use a modern
client version so all controls are visible.

## Open questions

- **Settings store**: there is no ECS `Profile`/`GlobalSettings` write-back layer wired to
  gameplay systems. Porting Apply/Default meaningfully requires deciding where options live in
  ECS (a `Res<Profile>` snapshot? direct `Configuration/Settings.cs` writes? per-setting events
  consumed by the relevant plugins?). Most of the gump's value is the side effects of Apply, which
  touch dozens of subsystems (FPS, topbar, skill gump, cave/tree textures, draw-Z, etc.).
- **Missing widgets**: ECS has no slider, combobox, color-picker, or editable text-input widget
  yet (ServerGump renders text but cannot edit it). These are prerequisites; v1 could ship a
  read-only/partial gump (checkboxes + tab nav) and stub the rest.
- **Macros / Info Bar pages** are deeply stateful (`World.Macros`, `World.InfoBars`,
  `MacroControl`, drag-to-create `MacroButtonGump`). Likely a separate later phase; v1 may render
  the tab as a placeholder.
- **Background fidelity**: confirm the translucent-panel approach (`UOCustomKind.None` +
  `BackgroundColor`) visually matches `AlphaBlendControl(0.95f)` hue 999 — the exact RGBA of hue
  999 at 0.95 alpha needs to be resolved from the hue table.
- **NiceButton selection styling**: the tab rail uses `NiceButton` with a selected-highlight bar
  (no sprite). Need an ECS equivalent (highlighted `BackgroundColor` on the selected tab).
- **Window position**: OOP opens at (0,0) and is non-centered; confirm desired ECS spawn position
  (centered vs top-left vs mouse-anchored like paperdoll's staggered fallback).
