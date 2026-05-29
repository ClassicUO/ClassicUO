# Status / Health Bar Gump Spec

## Overview

Two related, server-data-driven floating windows that report a mobile's vital
stats:

1. **Health Bar Gump** — a small bar showing a name + HP (and, for self / party,
   Mana + Stamina) for one mobile. Three visual forms:
   - **Self bar** (`LocalSerial == World.Player`): name + HP/Mana/Stam, 3 bars.
   - **Other-mobile bar**: name + single HP bar (single-line, compact).
   - **Party bar**: name + HP/Mana/Stam + two heal buttons (Greater Heal /
     Heal). Wider, multi-line.
   Each form has TWO render styles, switched by a profile flag:
   - **Classic** (`HealthBarGump`): uses UO gump sprites for bg + bars.
   - **Custom** (`HealthBarGumpCustom`): draws solid-color rectangles (lines),
     by "Syrupz(Alan)" — modern look, no art files. Selected by
     `ProfileManager.CurrentProfile.CustomBarsToggled`.
   The self health bar appears at login (mini player bar) and other-mobile bars
   appear when you single-click / target / attack another mobile. Bars
   continuously poll the backing entity each frame and recolor / resize bars by
   HP%, notoriety hue, poison/yellow-hits state, war mode, target highlight, and
   out-of-range / dead state.

2. **Status Gump** — the large player-only "paperdoll stats" panel (Str/Dex/Int,
   HP/Mana/Stam, weight, gold, resistances, damage, followers, luck, stat cap,
   and AOS bonus stats). Two forms:
   - **Old** (`StatusGumpOld`): single fixed gump 0x0802, pre-AOS layout.
   - **Modern** (`StatusGumpModern`): gump 0x2A6C, two sub-layouts depending on
     `UseUOPGumps` (UOP gump set = AOS extended stats, classic = compact). Form
     chosen by `UseOldStatusGump` profile flag + client version.
   Opened by double-clicking the player's health bar (when no status gump open),
   or by clicking the "open bar / minimize" hitbox region. Stat-lock arrows
   (up/down/locked) cycle on click and send `ChangeStatLock`. Refreshes its
   labels from `World.Player` every 250 ms.

Self health bar and status gump are **mutually exclusive** when
`StatusGumpBarMutuallyExclusive` is set — opening one disposes the other.

## Source of truth

- `src/ClassicUO.Client/Game/UI/Gumps/HealthBarGump.cs`
  - `BaseHealthBarGump` (abstract) — lines 21-300. Shared input handling:
    - ctor + `GameActions.RequestMobileStatus` line 34; `CanCloseWithRightClick`
      line 36.
    - `CalculatePercents(max, current, maxValue)` lines 152-170 — bar fill math.
    - `OnMouseDoubleClick` lines 211-255 — attack / open corpse / dclick; for
      self opens StatusGump (lines 246-251).
    - `OnMouseDown` lines 184-209, `OnMouseOver` lines 275-286, `OnKeyDown`
      (rename) lines 257-273, `TextBoxOnMouseUp` (target / rename) lines 119-150.
  - `HealthBarGumpCustom` (line graphics) — lines 302-1270.
    - Constants lines 304-325 (sizes + colors).
    - `BuildGump` lines 690-1194 (party / self / other branches).
    - `Update` lines 369-688 (per-frame state machine).
    - `LineCHB` inner control lines 1201-1248 (solid-color rect draw).
  - `HealthBarGump` (classic gump graphics) — lines 1272-1906.
    - Background/line gump constants lines 1274-1282.
    - `BuildGump` lines 1335-1571 (party / self / other branches).
    - `Update` lines 1574-1875.
    - `OnButtonClick` (party heal spells) lines 1877-1898; `ButtonParty` enum
      lines 1901-1905.
- `src/ClassicUO.Client/Game/UI/Gumps/StatusGump.cs`
  - `StatusGumpBase` (abstract) — lines 17-180. Lock graphics 0x0984 / 0x0986 /
    0x082C lines 19-21; `OnMouseUp` (open/minimize health bar) lines 65-92;
    `GetStatLockGraphic` lines 141-153; `UpdateContents` (refresh lock arrows)
    lines 155-166; `AddStatusGump` factory lines 122-139.
  - `StatusGumpOld` — lines 182-539. bg 0x0802 line 189; layout + labels +
    hitboxes lines 184-484; `Update` (250 ms refresh) lines 486-521;
    `MobileStats` enum lines 524-538.
  - `StatusGumpModern` — lines 541-1492. bg 0x2A6C line 549; UOP vs classic
    layout lines 551-1242; minimize hitbox lines 1303-1315; `AddStatTextLabel`
    helper lines 1321-1348; `Update` lines 1350-1452; `MobileStats` enum
    lines 1455-1490.

## Visual structure

### A. Health Bar — CLASSIC (`HealthBarGump`)

Bars are `GumpPicWithWidth` (a gump sprite cropped to a percent width). Red
"empty" pic sits under a colored "full" pic whose width = HP%.

#### A1. Self, NOT in party (lines 1450-1499)
Root bg gump = `0x0803` (peace) / `0x0807` (war) — `BACKGROUND_NORMAL` /
`BACKGROUND_WAR`. Width/Height = bg sprite size.

| Control | Type | Asset | X | Y | W | H | Notes |
|---|---|---|---|---|---|---|---|
| Background | GumpPic | 0x0803 / 0x0807 | 0 | 0 | sprite | sprite | swaps on war mode |
| HP empty | GumpPic | 0x0805 (LINE_RED) | 34 | 12 | — | — | |
| Mana empty | GumpPic | 0x0805 | 34 | 25 | — | — | |
| Stam empty | GumpPic | 0x0805 | 34 | 38 | — | — | |
| HP full | GumpPicWithWidth | 0x0806 (LINE_BLUE) | 34 | 12 | %·? | — | width from HP% |
| Mana full | GumpPicWithWidth | 0x0806 | 34 | 25 | %·? | — | |
| Stam full | GumpPicWithWidth | 0x0806 | 34 | 38 | %·? | — | |

No name textbox in this branch (self bar shows the bg art's printed labels).

#### A2. Other mobile, NOT party (lines 1501-1562)
Root bg gump = `0x0804` (hued by notoriety `barColor`). Single HP bar.

| Control | Type | Asset | X | Y | W | H | Font/Hue |
|---|---|---|---|---|---|---|---|
| Background | GumpPic | 0x0804 (hue=barColor) | 0 | 0 | sprite | sprite | |
| HP empty | GumpPic | 0x0805 (hue=hitsColor) | 34 | 38 | — | — | |
| HP full | GumpPicWithWidth | 0x0806 | 34 | 38 | %·109 | — | barW=109 |
| Name | StbTextBox | — | 16 | 14 | 120 | 15 | font Fixed, hue textColor (0x0386, or 0x000E if renamable) |

#### A3. Party (lines 1341-1447)
Root bg gump = `0x0803` (alpha 0). Width=115, Height=55. Two heal buttons.

| Control | Type | Asset | X | Y | W | H | Notes |
|---|---|---|---|---|---|---|---|
| Background | GumpPic | 0x0803 (Alpha 0) | 0 | 0 | 115 | 55 | |
| Name | StbTextBox | — | 0 | -2 | 120 (self) / 109 | 50 | font Fixed[+BlackBorder], notoriety hue. Self name = "Self" |
| Heal btn 1 | Button | 0x0938/0x093A/0x0938 | 0 | 20 | — | — | Greater Heal (spell 29) |
| Heal btn 2 | Button | 0x0939/0x093A/0x0939 | 0 | 33 | — | — | Heal (spell 11) |
| HP empty | GumpPic | 0x0028 (LINE_RED_PARTY) | 18 | 20 | — | — | |
| Mana empty | GumpPic | 0x0028 | 18 | 33 | — | — | |
| Stam empty | GumpPic | 0x0028 | 18 | 45 | — | — | |
| HP full | GumpPicWithWidth | 0x0029 (LINE_BLUE_PARTY) | 18 | 20 | %·96 | — | barW=96 |
| Mana full | GumpPicWithWidth | 0x0029 | 18 | 33 | %·96 | — | |
| Stam full | GumpPicWithWidth | 0x0029 | 18 | 45 | %·96 | — | |

Poison bar = hue 63 (party) / graphic `0x0808` (LINE_POISONED, non-party).
Yellow-hits bar = hue 353 (party) / graphic `0x0809` (LINE_YELLOWHITS).

### B. Health Bar — CUSTOM (`HealthBarGumpCustom`)

All elements are `LineCHB` (solid-color filled rectangle) + an
`AlphaBlendControl` background (alpha 0.7). Constants (lines 304-325):
`HPB_WIDTH=120`, `HPB_HEIGHT_MULTILINE=60`, `HPB_HEIGHT_SINGLELINE=36`,
`HPB_BAR_WIDTH=100`, `HPB_BAR_HEIGHT=8`, `HPB_BAR_SPACELEFT=(120-100)/2=10`,
border=1, outline=1.

Colors: HP/normal = DodgerBlue, empty/back = Red, gray (out-of-range) = Gray,
poison = LimeGreen, yellow-hits = Orange, border = Black.

#### B1. Self / party (multiline, H=60) (lines 697-883, 886-1048)

| Control | Type | Color | X | Y | W | H | Notes |
|---|---|---|---|---|---|---|---|
| Background | AlphaBlendControl(0.7) | hue=notoriety | 0 | 0 | 120 | 60 | hue 912 when dead/out-of-range |
| Name | StbTextBox | notoriety hue | 0 | 3 | 100 | — | Cropped+BlackBorder, centered, unicode |
| Outline | LineCHB | Black | 9 | 26 | 102 | 28 | bar group outline (SPACELEFT-1, 27-1, BARW+2, BARH·3+2+2) |
| HP empty | LineCHB | Red | 10 | 27 | 100 | 8 | |
| Mana empty | LineCHB | Red | 10 | 36 | 100 | 8 | |
| Stam empty | LineCHB | Red | 10 | 45 | 100 | 8 | |
| HP full | LineCHB | Blue | 10 | 27 | %·100 | 8 | LineWidth=HP% |
| Mana full | LineCHB | Blue | 10 | 36 | %·100 | 8 | |
| Stam full | LineCHB | Blue | 10 | 45 | %·100 | 8 | |
| Border top | LineCHB | Black | 0 | 0 | 120 | 1 | |
| Border bottom | LineCHB | Black | 0 | 59 | 120 | 1 | |
| Border left | LineCHB | Black | 0 | 0 | 1 | 60 | |
| Border right | LineCHB | Black | 119 | 0 | 1 | 60 | |

(Self branch lines 886-1048 is identical to the party multiline branch except
the name textbox config; mana/stam empty bars are anonymous in the self branch.)

#### B2. Other mobile (single-line, H=36) (lines 1050-1172)

| Control | Type | Color | X | Y | W | H |
|---|---|---|---|---|---|---|
| Background | AlphaBlendControl(0.7) | notoriety | 0 | 0 | 120 | 36 |
| Outline | LineCHB | Black | 9 | 20 | 102 | 10 |
| HP empty | LineCHB | Red | 10 | 21 | 100 | 8 |
| HP full | LineCHB | Blue | 10 | 21 | %·100 | 8 |
| Border top/bottom/left/right | LineCHB | Black | 0/0/0/119 | 0/35/0/0 | 120/120/1/1 | 1/1/36/36 |
| Name | StbTextBox | notoriety | 0 | 0 | 120 | 15 | centered, Cropped+BlackBorder |

### C. Status Gump — OLD (`StatusGumpOld`) (lines 182-539)

Root bg gump = `0x0802`. font 1, hue `0x0386` for all labels. `_point=(244,112)`.
`xOffset` for lock arrows = 28 (UOP) / 40 (classic).

| Control | Type | Asset / Text | X | Y | Notes |
|---|---|---|---|---|---|
| Background | GumpPic | 0x0802 | 0 | 0 | |
| Name | Label | Player.Name | 86 | 42 | |
| Buff icon btn | Button | 0x7538/0x7539/0x7539 | 20 | 42 | CV >= 5020; opens BuffGump |
| Str lock | GumpPic | 0x0984/0x0986/0x082C | 28/40 | 62 | click cycles StrLock |
| Dex lock | GumpPic | lock graphic | 28/40 | 74 | |
| Int lock | GumpPic | lock graphic | 28/40 | 86 | |
| Strength | Label | Player.Strength | 86 | 62 | |
| Dexterity | Label | Player.Dexterity | 86 | 74 | |
| Intelligence | Label | Player.Intelligence | 86 | 86 | |
| Sex | Label | Male/Female | 86 | 98 | |
| AR | Label | PhysicalResistance | 86 | 110 | |
| Hits | Label | Hits/HitsMax | 171 | 62 | |
| Mana | Label | Mana/ManaMax | 171 | 74 | |
| Stamina | Label | Stamina/StaminaMax | 171 | 86 | |
| Gold | Label | Gold | 171 | 98 | |
| Weight | Label | Weight/WeightMax | 171 | 110 | |
| 10 HitBox tooltips | HitBox | cliloc strings | see lines 353-481 | | for Str/Dex/Int/Sex/Armor/Hits/Mana/Stam/Gold/Weight |

### D. Status Gump — MODERN (`StatusGumpModern`) (lines 541-1492)

Root bg gump = `0x2A6C`. font 1, default label hue `0x0386`. Two sub-layouts:

- **UOP gump set** (AOS, `UseUOPGumps`): full extended-stat layout, ~38 labels
  including HitChanceInc, DefenseChanceInc, LowerManaCost, DamageChanceInc,
  SwingSpeedInc, LowerReagentCost, SpellDamageInc, FasterCasting,
  FasterCastRecovery, max-capped resists (`{cur}/{max}`), damage range, followers.
- **Classic gump set** (non-UOP): compact layout with single-value resists, gold,
  no AOS combat bonuses.

Layout metrics (all from the source, hue `0x0386`, font 1):
- Name: x=90 (UOP) / 58, y=50, maxWidth=320, centered.
- Buff icon btn `0x7538/0x7539/0x7539` at (40, 50), CV >= 5020.
- Lock arrows column at x=28 (UOP) / 40, y = 76 (Str), 102 (Dex), 132 (Int).
- Str/Dex/Int values at x=80/88, y=77/105/133.
- HP cur/max stacked at y=70/83; Stam at 98/111; Mana at 126/139 (textWidth=40,
  centered) — center column x≈146-150.
- Separator `Line` 0xFF383838 at y=82/110/138 between cur/max pairs.
- Right columns (StatCap, Luck, Weight, Damage, Followers, Gold, resists) per
  lines 879-1242; resist block x=475 (UOP) / 354, y=74-134 (5 rows ~14-18px apart).
- Minimize / open-bar hitbox: x/y from `_point` = (389,152), or (540,180) for UOP;
  16x16; tooltip "Minimize" or "Open bar".
- Each stat has a `HitBox` tooltip control overlapping the label/icon (cliloc
  ids in source, e.g. 1061146 Str, 1061149 HitPoints, 1061151 Mana, 1061154
  Weight, 1061156 Gold, 1061158-1061162 resists, 1075616-1075629 AOS bonuses).

Refresh: `Update` rewrites every label from `World.Player` every 250 ms
(lines 1350-1452).

## Assets

| Asset ID | Kind | Use |
|---|---|---|
| 0x0802 | gump | StatusGumpOld bg |
| 0x2A6C | gump | StatusGumpModern bg |
| 0x0803 | gump | HealthBar bg normal (self/party classic) |
| 0x0807 | gump | HealthBar bg war (self classic) |
| 0x0804 | gump | HealthBar bg other-mobile (classic) |
| 0x0805 | gump | HP/Mana/Stam empty line (LINE_RED) |
| 0x0806 | gump | HP/Mana/Stam full line (LINE_BLUE) |
| 0x0808 | gump | poison line (LINE_POISONED) |
| 0x0809 | gump | yellow-hits line (LINE_YELLOWHITS) |
| 0x0028 | gump | party empty line (LINE_RED_PARTY) |
| 0x0029 | gump | party full line (LINE_BLUE_PARTY) |
| 0x0938/0x093A | gump | party Heal1 button (Greater Heal) normal/pressed |
| 0x0939/0x093A | gump | party Heal2 button (Heal) normal/pressed |
| 0x0984 | gump | stat lock "up" arrow (LOCK_UP_GRAPHIC) |
| 0x0986 | gump | stat lock "down" arrow (LOCK_DOWN_GRAPHIC) |
| 0x082C | gump | stat lock "locked" (LOCK_LOCKED_GRAPHIC) |
| 0x7538/0x7539 | gump | buff-icon button (status gump) |
| Color.DodgerBlue | rect | custom HP/Mana/Stam full |
| Color.Red | rect | custom bars empty + war/target border |
| Color.Gray | rect | custom bars out-of-range |
| Color.LimeGreen | rect | custom poison fill |
| Color.Orange | rect | custom yellow-hits fill |
| Color.Black | rect | custom border/outline |
| 0xFF383838 | line | modern status separator lines |

Hues: name default `0x0386`; renamable name `0x000E`; dead / out-of-range
`912` (0x0390); notoriety hue from `Notoriety.GetHue(NotorietyFlag)` for
name + bg of other-mobile bars. Font: status labels `font 1`; health bar name
boxes `FontStyle.Fixed` (classic) / `Cropped|BlackBorder` (custom), unicode for
custom.

## Behaviors

| Behavior | OOP source | ECS mechanism |
|---|---|---|
| **Drag to move** | `CanMove = true` (BaseHealthBarGump line 49, StatusGumpBase line 33) | `UIMovable` on root (UOGumpBundle) → WindowDragPlugin.Drag |
| **Right-click closes** | `CanCloseWithRightClick = true` | `UIMovable` → WindowDragPlugin.CloseOnRightClick (despawn subtree) |
| **Stack topmost on click** | UIManager z-order | only-root `GlobalZIndex` + `UiZCounter.Bump()` on drag latch |
| **Pixel-perfect hit** | `Contains` override / sprite hit | `UiHitTest.PixelHit` (Gump kind); custom bars use bbox-solid (None / a new rect kind) |
| **HP/Mana/Stam bar fill** | `CalculatePercents` + GumpPicWithWidth.Percent / LineCHB.LineWidth (Update) | per-frame system mutates child `Node.Width` (classic via cropped sprite width) or a new `UORect` custom-render width |
| **Notoriety / state recolor** | Update reads NotorietyFlag, IsPoisoned, IsYellowHits, IsDead, war mode, target | per-frame system mutates child `UOCustomRender.Hue` / `.AssetId` / rect color in place (rule 2 in-place ref) |
| **Out-of-range / dead → gray** | Update sets hue 912 + gray bars + close logic | per-frame system; `CloseHealthBarType` profile decides dispose |
| **Double-click bar** | `OnMouseDoubleClick` → attack / open corpse / dclick; self → open StatusGump | `On<UiDoubleClick>` observer on root; self → spawn StatusGump entity; other → `Send_DoubleClick` / `Send_AttackRequest` |
| **Target the mobile** | `TextBoxOnMouseUp` / `OnMouseDown` when `IsTargeting` | `On<UiClick>` observer → if targeting cursor active, send target reply |
| **Rename (own pet)** | `OnKeyDown` RETURN → `GameActions.Rename` | text-input focus + Enter key handler when `CanBeRenamed` (deferred v1) |
| **Party heal buttons** | `OnButtonClick` Heal1/Heal2 → CastSpell 29 / 11 | `On<UiClick>` per button → `Send_CastSpell` (party path deferred until party UI lands) |
| **Stat-lock arrows cycle** | `_lockers[i].MouseUp` → `ChangeStatLock`, swap graphic | `On<UiClick>` per lock pic → cycle lock state, send `ChangeStatLock`, mutate `UOCustomRender.AssetId` |
| **Buff-icon button** | `OnButtonClick` BuffIcon → open BuffGump | `On<UiClick>` → spawn BuffGump (deferred; no ECS BuffGump yet — log) |
| **Open / minimize health bar** | StatusGump `OnMouseUp` hitbox → spawn HealthBarGump, dispose self if mutually-exclusive | `On<UiClick>` on minimize hitbox → spawn self HealthBar, despawn status if exclusive |
| **Server-driven stat refresh** | `Update` re-reads `World.Player` / entity every frame (status: 250 ms) | per-frame system queries `Hits`/`Mana`/`Stamina`/`PlayerData` on the backing entity and updates labels/bars; no polling of the wire (components already updated by packet observers) |
| **War-mode bg swap (self classic)** | Update swaps 0x0803 ↔ 0x0807 | observer on `OnInsert<ServerFlags>` (mirrors PaperdollWarModeButton) flips root `UOCustomRender.AssetId` |
| **Status request on open** | `GameActions.RequestMobileStatus` in ctor | on spawn, `Send_StatusRequest(serial)` (already wired as `Send_StatusRequest`) |

## Server packets

The gumps are **not** opened by a dedicated server packet — they are client-side
windows opened by user action (double-click health bar, top-bar paperdoll/status
button, click the status-gump minimize hitbox). They are **driven** by stat
packets that update the backing entity's components:

- `0x11` `OnCharacterStatusPacket_0x11` — full character status (name, HP, optional
  Str/Dex/Int/Stam/Mana/Gold/Weight/resists/AOS bonuses). Already handled:
  inserts `Hits` / `Mana` / `Stamina` / `PlayerData` on the entity
  (`InGamePacketsPlugin.OnCharacterStatus`, lines 970-1025). The status gump
  reads these.
- `0x2D` `OnMobileAttributesPacket_0x2D` — HP/Mana/Stam (handled, lines 1027-1037).
- `0x16` `OnHealthBarStatusPacket_0x16` / `0x17`
  `OnHealthBarStatusDetailsPacket_0x17` — poison / yellow-hits flags. Currently
  **stubbed** (`InGamePacketsPlugin` lines 252-253) — must be implemented to
  drive the poison/yellow-hits bar color.
- Outgoing: `Send_StatusRequest(serial)` (0x34 request) on open; `ChangeStatLock`
  for lock arrows; `Send_CastSpell` for party heal.

## ECS implementation plan

### Plugin

- New: `src/ClassicUO.Ecs/Gameplay/HealthBarPlugin.cs`
  (`internal readonly struct HealthBarPlugin : IPlugin`).
- New: `src/ClassicUO.Ecs/Gameplay/StatusGumpPlugin.cs`
  (`internal readonly struct StatusGumpPlugin : IPlugin`).
  Compose both in `Boot.cs` `CuoPlugin.Build`.

### Components / markers

```csharp
internal struct HealthBarWindow { public uint Serial; public bool IsPlayer; public bool Custom; }
internal struct HealthBarFill   { public ulong WindowEntity; public BarKind Kind; } // Hp/Mana/Stam
internal struct HealthBarName   { public ulong WindowEntity; }
internal struct StatusGumpWindow { public uint Serial; public bool Modern; public bool Uop; }
internal struct StatusLabel     { public ulong WindowEntity; public StatField Field; }
internal struct StatLockArrow   { public int Index; } // 0=Str 1=Dex 2=Int
internal struct StatusRefreshTimer { public float NextMs; } // Local<> or component, 250ms cadence
```

`BarKind` / `StatField` enums mirror the OOP `MobileStats` enums.

### Resources

- Reuse `Res<UiZCounter>`, `Res<AssetsServer>`, `Res<GumpBuilder>`,
  `Res<GameContext>`, `Res<NetworkEntitiesMap>`, `Res<NetClient>`, `Res<Time>`.
- A profile/settings `Res` to read `CustomBarsToggled`, `UseOldStatusGump`,
  `UseUOPGumps`, `StatusGumpBarMutuallyExclusive`, `CloseHealthBarType` — check
  whether an ECS profile resource exists; if not, hardcode classic defaults for
  v1 and note in Open questions.

### Bundle usage

- Status gump + classic/other health bar roots: `UOGumpBundle` via
  `GumpBuilder.SpawnUOGump(commands, bgId, hue, pos, zCounter)` (Kind=Gump for
  the single-sprite bg).
- Custom (line-graphics) health bar: the bg is an alpha-blended rect, not a gump
  sprite. Either (a) spawn root with `UOGumpBundle` Kind=`None` (invisible
  drag/hit surface sized to the window) + a child alpha rect, or (b) add a new
  `UORect` custom-render kind (see below) and use it for the bg + every bar.
  Recommended: add `UORect` so bars and the bg both render through the existing
  Custom command path and hit-test through `UiHitTest`.

### Children

- Build via `GumpBuilder.AddGump` / `AddButton` / `AddLabel`, `commands.AddChild`
  onto the root (mirror PaperdollPlugin.BuildWindow). Each fill bar + name +
  label carries its `HealthBarFill` / `StatusLabel` tag for the refresh system.

### Observers

- `On<PacketReceived<OnOpenPaperdollPacket_0x88>>`-style is NOT the trigger.
  Spawn is user-initiated:
  - TopBar / paperdoll "Status" button → spawn StatusGump (currently the
    paperdoll status button just re-requests 0x11 — wire it to spawn here).
  - `On<UiDoubleClick>` observer on a health bar root → self opens StatusGump.
  - `On<UiClick>` on status minimize hitbox → spawn self HealthBar.
- `OnInsert<ServerFlags>` observer → war-mode bg swap on self classic bar
  (pattern copied from `PaperdollPlugin` RefreshWarModeButtons).
- `OnInsert<Hits>` / `OnInsert<Mana>` / `OnInsert<Stamina>` observers → could
  drive bar refresh event-driven (preferred per rule 4) instead of per-frame
  polling. For notoriety/poison/target/war recolor a small per-frame system is
  acceptable (mirrors OOP `Update`), but prefer observers where the trigger is a
  discrete component (re)insert.
- Per-button `On<UiClick>` observers for stat-lock cycle, buff icon, party heal,
  minimize — registered inline on the EntityCommands (see PaperdollPlugin
  buttons).
- `OnExit(GameState.GameScreen)` despawn system (copy
  `PaperdollPlugin.DisposeOnLogout`).

### Systems

- `RefreshHealthBars` (Stage.Update): for each `HealthBarFill`, look up backing
  entity via `NetworkEntitiesMap` + `Query<Data<Hits,Mana,Stamina>>`, compute
  `CalculatePercents`, set the fill child's `Node.Width` (classic cropped width)
  or `UORect` width; recolor by state. Keep this event-driven where possible.
- `RefreshStatusLabels` (Stage.Update, 250 ms gate via `Local<StatusRefreshTimer>`
  + `Res<Time>`): rewrite `Text` on each `StatusLabel` from the player's `Hits` /
  `Mana` / `Stamina` / `PlayerData` components. No wall-clock; use `Time.Total`.

### New ClayUO custom render command + UiHitTest case

- Add `UORect` to `UOCustomKind` (`GuiPlugin.cs`): a solid-color filled rectangle
  (color carried in a new `UOCustomRender.Color` field, since `Hue` is a shader
  hue not an RGBA). Used for the custom health bar bg (alpha 0.7) + every
  HP/Mana/Stam/border/outline rect.
  - `GuiRenderingPlugin.DrawCustom` (`Rendering/GuiRenderingPlugin.cs`, switch at
    line 251): add `case UOCustomKind.UORect` → `b.Draw(solidColorTexture, bb
    rect, color, zIndex)` mirroring `LineCHB.AddToRenderLists` (lines 1222-1247).
  - `UiHitTest.PixelHit` (`UI/UiHitTest.cs`, switch at line 25): add
    `case UOCustomKind.UORect → return true;` (solid bbox; the bg rect captures
    drag/close/click-capture over its whole area, like None).
- The classic / status sprite gumps need **no** new kind — `UOCustomKind.Gump`
  + existing `PixelHit` Gump case already cover them.

### ECS-rule conformance

- No `World` access — all reads via `Query`, all spawn/despawn/insert via
  `Commands` (rule 1, 2). Bar/label refresh mutates `Node.Width` / `Text` /
  `UOCustomRender` fields in place via the query's mutable ref (rule 2).
- `Res<Time>` for the 250 ms status refresh, never TickCount (rule 3).
- No closure-captured mutable state — captured serials only (immutable) on button
  observers, matching PaperdollPlugin; per-window state lives on the
  `HealthBarWindow` / `StatusGumpWindow` component (rule 3).
- Observers preferred for discrete triggers (war mode, equip-like inserts);
  per-frame system only for continuous recolor (rule 4).
- Window contract (drag / right-click close / z-stack / pixel hit / click
  capture) entirely from `UIMovable` + `UOGumpBundle` — not reimplemented.

## How to trigger for capture

Requires: ECS client (`cuo-ecs`) booted into the game world against ModernUO
(`127.0.0.1:2593`, `admin/admin`) via the agent harness (see
`tools/agent-desktop/AGENTS.md`; `up --persist`).

- **Self health bar**: appears on login if a self bar is configured. If not yet
  auto-spawned in ECS, open it via the status gump minimize hitbox, or (once
  wired) it spawns from the top-bar. For v1 capture, spawn it from the
  double-click-status path.
- **Status gump (modern)**: from the in-game paperdoll, click the **Status**
  button (paperdoll button column, gump `0x07EB`), or double-click the player's
  health bar (self) — `BaseHealthBarGump.OnMouseDoubleClick` opens
  `StatusGumpBase.AddStatusGump`. ECS: wire the paperdoll Status button / health
  bar dclick to spawn `StatusGumpWindow`.
  - Modern vs Old: requires `UseOldStatusGump=false` + client version
    >= CV_308Z (default). UOP-extended layout requires `UseUOPGumps` (the data
    set's gump pack) — confirm the pinned UO data dir uses UOP gumps.
- **Other-mobile health bar**: single-click another mobile (NPC) in the world;
  OOP `WorldScene` opens a `HealthBarGump` for the clicked serial. ECS: wire a
  world single-click → spawn `HealthBarWindow` for that serial.
- **Party bar / heal buttons**: requires being in a party (no ECS party UI yet)
  — defer capture.

To take the reference shot of the **legacy** client for pixel parity, run the
`ClassicUO.Client` AGENT_BUILD, log in, double-click the player health bar
(status modern) and single-click an NPC (other-mobile bar), then `rpc-shot`.

## Open questions

- Does the ECS branch have a profile/settings resource exposing
  `CustomBarsToggled`, `UseOldStatusGump`, `UseUOPGumps`,
  `StatusGumpBarMutuallyExclusive`, `CloseHealthBarType`? If not, which subset
  ships in v1 (recommend: classic status gump + classic/other health bar; defer
  custom bars + party)?
- Self health bar auto-spawn: OOP restores it from the profile / saved gumps on
  login. Does ECS persist/restore gumps? If not, what is the v1 spawn trigger
  for the self bar (top-bar button vs auto on enter GameScreen)?
- Notoriety hue + IsRenamable + IsPoisoned + IsYellowHits + IsDead + war mode:
  which of these are already present as ECS components on mobiles? `ServerFlags`
  (war) exists; `Hits/Mana/Stamina/PlayerData` exist. Poison / yellow-hits need
  `0x16`/`0x17` un-stubbed; notoriety + renamable flags need a component check.
- `GumpPicWithWidth` (cropped-percent sprite) has no ECS equivalent — confirm the
  classic bar fill should be done by setting child `Node.Width` (the renderer
  clips the sprite to the node) or whether a new cropped-gump custom kind is
  needed. The custom (rect) bars are straightforward via the proposed `UORect`.
- Stat-lock click semantics: OOP fires on `MouseUp` (release). Confirm
  `On<UiClick>` (press+release same element) is the right mapping (per the UO
  Gump contract "buttons fire on release" — yes).
- BuffGump, PartyGump, rename text-entry, and SkillsGump are referenced by these
  gumps but have no ECS port — confirm they are out of scope (log-only) for v1.
- Modern status gump is large (~38 stat fields, two layouts, ~40 tooltip
  hitboxes). Confirm tooltip hitboxes can be deferred (cliloc tooltip rendering
  not yet in the ECS UI text path).
