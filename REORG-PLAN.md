# Code reorganization plan

Goal: every cohesive subsystem lives in its own `Game/<Feature>/` folder.
Each folder = one namespace = one feature. Manager facade + collaborator
interfaces + concrete implementations colocated. Cross-cutting concerns
(events) live in `Game/Events/`.

## Status today

- 10 manager facades already decomposed with `IEventListener` + cohesive
  collaborator interfaces (Audio / Chat / Corpses / Houses / Messaging /
  Opl / Party / Targeting / WorldText + the IEventListener contract
  itself).
- Namespaces aligned to feature folders via `Game/_GlobalUsings.cs` and
  `tests/.../_GlobalUsings.cs`.
- 613/613 tests pass.

## Target structure

```
Game/
├── _GlobalUsings.cs
├── Audio/                ✓ DONE
├── Boats/                ← BoatMovingManager
├── Chat/                 ✓ DONE + ChatChannel + ChatStatus
├── Combat/               ← combat-swing subscribers extracted from World partials
├── Commands/             ← CommandManager
├── Constants.cs          (root)
├── Containers/           ← ContainerManager + ContainerItems/OpenContainer subs
├── Corpses/              ✓ DONE
├── Data/                 (keep types)
├── Effects/              ← EffectManager + AuraManager + GraphicEffect subs + AnimatedStaticsManager
├── Entities/             (renamed from GameObjects/)
│   ├── Items/            ← Item, ItemHold, UseItemQueue, equipment subs
│   ├── Mobiles/          ← Mobile, MobileAnimation, WalkerManager, mobile-update subs
│   ├── Players/          ← PlayerMobile, enter-world subs, character-status subs
│   └── Views/            (keep)
├── Events/               ✓
├── GameActions.cs        (root — touched by hundreds of files, keep)
├── Houses/               ✓ DONE
│   └── Customization/    ← HouseCustomizationManager (defer split)
├── Input/                ← GameCursor, SelectedObject, Scan*, DelayedObjectClick, DenyMove subs
│   └── Hotkeys/          ← HotkeysManager
├── Login/                ← LoginScene + LastCharacterManager
├── Macros/               ← MacroManager (defer split — 2606 LOC)
├── Map/                  ← Map + UltimaLive + Stitchin + MapData/MapPatches subs
├── Maps/WorldMap/        ← WorldMapEntityManager
├── Messaging/            ✓ DONE + MessageEventArgs
│   └── Journal/          ← JournalManager
├── Movement/             ← Pathfinder + WalkDenied/WalkConfirmed/PlayerMoved subs
├── Opl/                  ✓ DONE
├── Party/                ✓ DONE
├── Players/Social/       ← IgnoreManager
├── Scenes/               (keep)
├── Seasons/              ← Season + SeasonManager + SeasonChanged subs
├── Skills/               ← SkillsGroupManager + SkillList/SkillsUpdated subs
├── Spells/               ← ActiveIconsManager + spell-icon subs
├── Targeting/            ✓ DONE
├── UI/                   (UIManager.cs stays at UI/ root)
│   ├── Anchoring/        ← AnchorManager
│   ├── Controls/         (keep — defer splits)
│   ├── Gumps/            (keep — defer splits per "no file splits" rule)
│   ├── HealthBars/       ← HealthLinesManager + HealthBar subs
│   ├── InfoBar/          ← InfoBarManager
│   └── Names/            ← NameOverHeadManager
├── Weather/              ← Weather
├── World.cs / World.Helpers.cs / World.Subscribers.cs   (root core)
├── WorldText/            ✓ DONE + TextRenderer base
└── (root utility) LinkedObject, SerialHelper, UoAssist
```

## Phases

### R1 — Move remaining `Game/Managers/*.cs` into feature folders

29 file moves (mechanical):

| Manager                       | Destination               |
|-------------------------------|---------------------------|
| ActiveIconsManager            | `Spells/`                 |
| AnchorManager                 | `UI/Anchoring/`           |
| AnimatedStaticsManager        | `Effects/`                |
| AuraManager                   | `Effects/`                |
| BoatMovingManager             | `Boats/`                  |
| ChatChannel                   | `Chat/`                   |
| ChatStatus                    | `Chat/`                   |
| CommandManager                | `Commands/`               |
| ContainerManager              | `Containers/`             |
| DelayedObjectClickManager     | `Input/`                  |
| EffectManager                 | `Effects/`                |
| HealthLinesManager            | `UI/HealthBars/`          |
| HotkeysManager                | `Input/Hotkeys/`          |
| HouseCustomizationManager     | `Houses/Customization/`   |
| IgnoreManager                 | `Players/Social/`         |
| InfoBarManager                | `UI/InfoBar/`             |
| JournalManager                | `Messaging/Journal/`      |
| LastCharacterManager          | `Login/`                  |
| MacroManager                  | `Macros/`                 |
| MessageEventArgs              | `Messaging/`              |
| NameOverHeadManager           | `UI/Names/`               |
| Season                        | `Seasons/`                |
| SeasonManager                 | `Seasons/`                |
| SkillsGroupManager            | `Skills/`                 |
| Stitchin                      | `Map/`                    |
| TextRenderer                  | `WorldText/`              |
| UIManager                     | `UI/`                     |
| UseItemQueue                  | `Entities/Items/`         |
| WalkerManager                 | `Entities/Mobiles/`       |
| WorldMapEntityManager         | `UI/WorldMap/`            |

Namespace per folder. `_GlobalUsings.cs` extended. No behavior change.

### R2 — Move `Game/` root files to homes

| File                | Destination       |
|---------------------|-------------------|
| GameCursor          | `Input/`          |
| ItemHold            | `Input/`          |
| SelectedObject      | `Input/`          |
| ScanModeObject      | `Input/`          |
| ScanTypeObject      | `Input/`          |
| Pathfinder          | `Movement/`       |
| Weather             | `Weather/`        |
| UoAssist            | own folder or `Network/Assist/` |
| UltimaLive          | `Map/UltimaLive/` |
| LinkedObject        | root keep         |
| SerialHelper        | root keep         |
| Constants           | root keep         |
| GameActions         | root keep         |

### R3 — IEventListener for any remaining manager with EventSink subs

After R1, audit remaining managers for `EventSink.X += ...`. Apply
IEventListener pattern. (Most have no subs — likely small.)

### R4 — Extract `World.Subscribers.*.cs` partials into dedicated `IEventListener` classes

17 partials → ~17 dedicated listener classes in their feature folder:

| Partial                                | Target listener                                  |
|----------------------------------------|--------------------------------------------------|
| World.Subscribers.Boat.cs              | `Boats/BoatMovementHandler.cs`                   |
| World.Subscribers.CharacterStatus.cs   | `Entities/Players/CharacterStatusHandler.cs`     |
| World.Subscribers.ContainerItems.cs    | `Containers/ContainerItemsHandler.cs`            |
| World.Subscribers.DeathDisplay.cs      | `Combat/DeathDisplayHandler.cs`                  |
| World.Subscribers.DenyMove.cs          | `Input/DenyMoveHandler.cs`                       |
| World.Subscribers.EnterWorld.cs        | `Entities/Players/EnterWorldExtrasHandler.cs`    |
| World.Subscribers.Equipment.cs         | `Entities/Items/EquipmentHandler.cs`             |
| World.Subscribers.ExtendedGumps.cs     | `UI/Gumps/ExtendedGumpsHandler.cs`               |
| World.Subscribers.ExtendedMisc.cs      | split per concern (Map, Player, Spells)          |
| World.Subscribers.ExtendedStats.cs     | `Entities/Mobiles/ExtendedStatsHandler.cs`       |
| World.Subscribers.ExtendedWalk.cs      | `Movement/ExtendedWalkHandler.cs`                |
| World.Subscribers.GumpsUI.cs           | `UI/Gumps/GumpsUIHandler.cs`                     |
| World.Subscribers.MapData.cs           | `Map/MapDataHandler.cs`                          |
| World.Subscribers.MobileUpdates.cs     | `Entities/Mobiles/MobileUpdatesHandler.cs`       |
| World.Subscribers.OpenContainer.cs     | `Containers/OpenContainerHandler.cs`             |
| World.Subscribers.Skills.cs            | `Skills/SkillsHandler.cs`                        |
| World.Subscribers.Vendor.cs            | `Containers/VendorHandler.cs` (or own `Vendor/`) |

Plus `World.Subscribers.cs` main partial: extract per-event handlers
into appropriate feature folders.

Each listener gets World reference via ctor, uses plain `+= / -=` in
Subscribe / Unsubscribe (per memory).

### R5 — Rename `Game/GameObjects/` → `Game/Entities/`

File moves + namespace updates. ~20 files.

Subfolders: `Items/`, `Mobiles/`, `Players/`, `Views/`.

### R6 — Decompose smaller managers with mixed responsibilities

Candidates (each ~200–700 LOC, multiple responsibilities):

- ContainerManager 530 → store positions + grid-loot pending serial
- HotkeysManager 662 → binding store + dispatcher
- HealthLinesManager 375 → state + render
- EffectManager 242 → spawn + lifecycle
- WorldMapEntityManager 246 → marker store + range filter
- SeasonManager 689 → calendar + asset swap + tile remap

Parallel agents, same recipe as Audio / Chat / etc.

### R7 — God-manager splits (defer)

MacroManager (2606) and HouseCustomizationManager (2099) are big god
classes. Splits are multi-day. Ask before tackling.

### R8 — God-gump splits (skip per memory: no file splits)

OptionsGump 4941, WorldMapGump 3297, HealthBarGump 1906, etc. Skipped.

## Risks

1. **Namespace churn** — already mitigated by `_GlobalUsings.cs`.
2. **World partials → dedicated listeners (R4)** — `World._subs` is
   private. Each extracted listener gets its own subscription style
   (plain `+= / -=`). World shrinks; some test fixtures may need
   updating to call new listener's `Subscribe()` explicitly.
3. **UIManager** — static-ish registry, touched by hundreds of files.
   Keep as is.
4. **GameActions** — static, touched by hundreds of files. Keep as is.
5. **TextRenderer base** — `WorldTextManager` extends it. After R1 move
   to `WorldText/`, namespace is `ClassicUO.Game.WorldText`. Global
   using catches it.

## Execution order

1. **R1** (mechanical moves, this session).
2. **R2** (mechanical moves, this session).
3. **R3** (IEventListener for stragglers, this session).
4. **R4** (World partial extraction, parallel agents, next session).
5. **R5** (GameObjects → Entities rename, parallel agents).
6. **R6** (small manager decomp, parallel agents).
7. **R7** (god-class splits) — only if user explicitly asks.
8. **R8** — skipped.
