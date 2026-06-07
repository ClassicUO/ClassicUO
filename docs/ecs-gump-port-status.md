# ECS Gump Port Status

Tracks which legacy `ClassicUO.Client` gumps are ported to the ECS branch
(`src/ClassicUO.Ecs/`). Source of truth = `src/ClassicUO.Client/Game/UI/Gumps/`.

Last updated: 2026-06-07.

---

## Ported

| Legacy gump | ECS plugin |
|-------------|-----------|
| BuffGump | `UI/BuffGumpPlugin.cs` |
| ColorPickerGump | `UI/ColorPickerPlugin.cs` |
| ContainerGump | `Gameplay/Containers/ContainerGumpPlugin.cs` |
| HealthBarGump | `UI/HealthBarPlugin.cs` |
| JournalGump / ResizableJournal | `UI/JournalPlugin.cs` |
| MenuGump / GrayMenuGump (0x7C) | `UI/MenuGumpPlugin.cs` |
| MiniMapGump | `UI/MiniMapPlugin.cs` |
| OptionsGump | `UI/OptionsGumpPlugin.cs` (partial) |
| PaperdollGump | `UI/PaperdollPlugin.cs` |
| PartyGump / PartyInviteGump | `UI/PartyGumpPlugin.cs` + `UI/PartyPlugin.cs` |
| PopupMenuGump | `UI/PopupMenuPlugin.cs` |
| ProfileGump | `UI/ProfileGumpPlugin.cs` |
| ShopGump | `UI/Vendor/VendorGumpPlugin.cs` |
| SkillGumpAdvanced / StandardSkillsGump | `UI/SkillsGumpPlugin.cs` |
| SpellbookGump | `UI/SpellbookGumpPlugin.cs` |
| SplitMenuGump | `UI/SplitMenuPlugin.cs` |
| StatusGump | `UI/StatusBarPlugin.cs` |
| TextEntryDialogGump | `UI/TextEntryDialogPlugin.cs` |
| TopBarGump | `UI/TopBarPlugin.cs` |
| TradingGump | `UI/TradingGumpPlugin.cs` |
| (logout/quit) | `UI/LogoutGumpPlugin.cs` |
| server gumps (0xB0) | `UI/ServerGumpPlugin.cs` |

---

## Missing

### Gameplay
- **Book reading** (ModernBookGump) — 0x66 / 0x93 parsed, no window.
- **MapGump** — paper / treasure map (0x90); only the minimap exists.
- **WorldMapGump** — full world map.
- **GridLootGump** — grid loot container view.
- **NameOverheadGump** + NameOverHeadHandlerGump — names floating over mobiles.
- **QuestArrowGump** — quest pointer arrow (0xBA parsed, no arrow).
- **Macros** — MacroGump, MacroButtonGump.
- **Action buttons** — UseAbilityButtonGump, UseSpellButtonGump, SkillButtonGump (draggable hotbar buttons).
- **CounterBarGump** — item counters.
- **InfoBarGump** — custom stat bar.
- **CombatBookGump**.
- **RacialAbilitiesBookGump** — paperdoll has a stub ("no ECS RacialAbilitiesBook").
- **BulletinBoardGump** — 0x71 parsed, no board UI.
- **HouseCustomizationGump** — house design mode (large).

### Dialogs / misc
- MessageBoxGump.
- TipNoticeGump — 0xA6 parsed.
- QuestionGump — only the logout variant is done.
- CreditsGump.
- LocationGoGump.
- RaceChangeGump.
- IgnoreManagerGump.
- MarkersManagerGump / UserMarkerGump — worldmap markers.
- Chat channel UI (ChatGump / ChatGumpChooseName) — `Gameplay/Chat/ChatPlugin.cs`
  exists; coverage may be partial.

### Debug-only (low priority)
- InspectorGump, DebugGump, NetworkStatsGump.

---

## Not gumps (base classes / infra — ignore)
Gump, GumpType, TextContainerGump, ResizableGump, AnchorableGump,
WorldViewportGump (→ GameScreenPlugin), SystemChatControl.

---

## Priority for golden-path play
Book, Macros / action-buttons, NameOverhead, paper Map.
