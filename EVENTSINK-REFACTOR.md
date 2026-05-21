# EventSink Refactor Status

Branch: `refactor/eventsink-phase0` (30 commits ahead of `main`)
Build: green (0 errors)
Tests: 190/190 pass

## Goal

Decouple `Network/PacketHandlers.cs` from game/UI mutation. Each packet handler
parses bytes into a typed event argument record, raises the event via a static
`EventSink`, and returns. Subscribers in managers and `World` partials own the
state mutation.

## Architecture

- `Game/Events/EventSink.cs` — static hub exposing one `event Action<TArgs>` per
  game event plus a `Raise*` helper that isolates handler exceptions and a
  `ClearAll()` for tests.
- `Game/Events/*Events.cs` — `internal readonly record struct` arg types,
  grouped by domain (Chat, Mobile, Item, Combat, World, Audio, Network, Login,
  UI).
- `Game/World.Subscribers*.cs` — partial files on `World`, one per packet group.
  Each defines `Subscribe<X>()` / `Unsubscribe<X>()` and the matching `On<E>`
  handler. The master partial `World.Subscribers.cs` calls every
  `Subscribe<X>()` from `SubscribeEvents()` (and likewise for unsubscribe).
- Manager-owned subscribers — `AudioManager`, `Weather`, `MessageManager`,
  `WorldTextManager`, `TargetManager`, `CorpseManager`,
  `ObjectPropertiesListManager`, `HouseManager`, `ChatManager`, `LoginScene`
  each subscribe to their relevant events in their constructor (or
  `Load`/`Unload`) and own the corresponding mutations.

All event args are typed structured fields. The `byte[] Data, int Offset`
pattern that was used temporarily during agent work has been fully eliminated.

## Done

### Phase 0 — Infrastructure
- `EventSink` static class with `Raise*` helpers, `ClearAll()`, exception isolation.
- Args record types for 9 domains.
- `EventSinkTests` unit test for subscribe/raise/exception isolation.

### Phase 1 — Wire all packet handlers
Every implemented packet handler in `PacketHandlers.cs` (~95 handlers) now
raises an `EventSink.Raise*` event with parsed fields.

### Phase 2 — Subscribers own mutation

Migrated to subscribers (handlers shrunk to parse+emit):

**Audio**
- 0x54 PlaySoundEffect → `AudioManager.OnSoundPlay`
- 0x6D PlayMusic → `AudioManager.OnMusicPlay`

**Weather / World atmosphere**
- 0x65 SetWeather → `Weather.OnWeatherChanged`
- 0x21 DenyWalk weather reset → `Weather.OnWalkDenied`

**Combat**
- 0x0B Damage → `WorldTextManager.OnDamageReceived`
- 0x2F Swing turn-to-target → `World.OnCombatSwing`
- 0x72 Warmode → `World.OnWarModeChanged`
- 0xAA AttackCharacter → `TargetManager.OnAttackTargetChanged`
- 0x2C DeathScreen player death → `World.OnPlayerDeath`
- 0xAF DisplayDeath corpse re-serial → `World.OnMobileDeath`

**Chat**
- 0x1C Talk → `MessageManager.OnChatMessage`
- 0xAE UnicodeTalk → `MessageManager.OnUnicodeChatMessage`
- 0xC1 / 0xCC DisplayCliloc → `MessageManager.OnClilocMessage`
- 0xB2 ChatMessage channel admin → 12 typed events on `ChatManager`
- 0x9A ASCIIPrompt → `MessageManager.OnAsciiPrompt`
- 0xC2 UnicodePrompt → `MessageManager.OnUnicodePrompt`

**Movement / mobile state**
- 0x20 UpdatePlayer → `World.OnPlayerUpdated`
- 0x77 / 0xD2 UpdateCharacter → `World.OnMobileUpdated`
- 0x78 / 0xD3 UpdateObject → `World.OnMobileUpdated` (with equipment list)
- 0x97 MovePlayer → `World.OnPlayerMoved`
- 0x21 DenyWalk → `World.OnWalkDenied`
- 0x22 ConfirmWalk → `World.OnWalkConfirmed`
- 0x98 UpdateName → `World.OnMobileNameChanged`
- 0x16 / 0x17 NewHealthbarUpdate → `World.OnHealthBarStateChanged`
- 0x2D MobileAttributes → `World.OnMobileAttributesUpdated`
- 0xA1 UpdateHitpoints → `World.OnHitpointsUpdated`
- 0xA2 UpdateMana → `World.OnManaUpdated`
- 0xA3 UpdateStamina → `World.OnStaminaUpdated`
- 0xDE UpdateMobileStatus → emit only (no state change in original)
- 0xDF BuffDebuff → `World.OnBuffApplied` / `OnBuffRemoved`
- 0x6E CharacterAnimation → `World.OnCharacterAnimation`
- 0xE2 NewCharacterAnimation → `World.OnNewCharacterAnimation`
- 0x1D DeleteObject → `World.OnObjectDeleted`

**Items / containers**
- 0x1A UpdateItem → `World.OnItemUpdated`
- 0xF3 UpdateItemSA → `World.OnItemUpdated` (SA flag + player-branch)
- 0x25 UpdateContainedItem → `World.OnContainerItemAdded`
- 0x3C UpdateContainedItems → `World.OnContainerItemsReceived` then
  `OnContainerItemAdded` per item (received emitted first so clear runs before
  adds)
- 0x2E EquipItem → `World.OnItemEquipped`
- 0x89 CorpseEquipment → `CorpseManager.OnCorpseEquipmentReceived`
- 0xDC OPLInfo → `ObjectPropertiesListManager.OnOplInfoReceived`
- 0xD6 MegaCliloc → `ObjectPropertiesListManager.OnMegaClilocReceived`
- 0x24 OpenContainer → `World.OnContainerOpened`
- 0x23 DragAnimation → `World.OnItemDragAnimation` (handles graphic remap)
- 0x27 DenyMoveItem → `World.OnItemMoveDenied`
- 0x28 EndDraggingItem → `World.OnItemDragEnded`
- 0x29 DropItemAccepted → `World.OnItemDropAccepted`
- 0x95 DyeData → `World.OnDyeDataReceived`

**UI gumps**
- 0x7C OpenMenu → `World.OnContextMenuOpened`
- 0x88 OpenPaperdoll → `World.OnPaperdollOpened`
- 0x90 / 0xF5 DisplayMap → `World.OnMapDisplayed`
- 0x93 / 0xD4 OpenBook → `World.OnBookOpened` (Title/Author parsed)
- 0x66 BookData → `World.OnBookDataReceived` (pages parsed into typed list)
- 0xAB TextEntryDialog → `World.OnTextEntryDialogOpened`
- 0xA6 TipWindow → `World.OnTipWindowDisplayed`
- 0x71 BulletinBoardData → three typed events (Opened / Summary / Message)
- 0xA5 OpenUrl → `World.OnOpenUrlRequested`
- 0xB8 CharacterProfile → `World.OnCharacterProfileOpened`
- 0xB0 OpenGump → `World.OnGumpOpened`
- 0xDD OpenCompressedGump → `World.OnCompressedGumpOpened`
- 0x3B CloseVendorInterface → `World.OnVendorWindowClosed`
- 0x56 MapData → `World.OnMapDataReceived` (Action + PinX/PinY/PlotState parsed)

**Vendor / trading**
- 0x74 BuyList → `World.OnShopBuyListReceived` (typed entry list)
- 0x9E SellList → `World.OnShopSellListReceived` (typed entry list)
- 0x6F SecureTrading → 4 typed events (Open/Closed/AcceptUpdated/CurrencyUpdated)

**Houses**
- 0xD8 CustomHouse → `HouseManager.OnCustomHouseReceived` (planes parsed into
  typed components, no raw bytes)

**World atmosphere**
- 0xBC Season → `World.OnSeasonChanged`
- 0x4E PersonalLightLevel → `World.OnLightLevelChanged` (IsPersonal=true)
- 0x4F LightLevel → `World.OnLightLevelChanged` (IsPersonal=false)
- 0xC8 ClientViewRange → `World.OnClientViewRangeChanged`
- 0x73 Ping → emit only
- 0x70 / 0xC0 / 0xC7 GraphicEffect → `World.OnGraphicEffectSpawned`
- 0xF6 BoatMoving → `World.OnBoatMovingReceived` (passengers parsed into typed list)
- 0x99 MultiPlacement → `TargetManager.OnMultiPlacementReceived`
- 0x38 Pathfinding → `World.OnPathfindingReceived`

**Targeting**
- 0x6C TargetCursor → `TargetManager.OnTargetCursorReceived`

**Skills / quests / waypoints**
- 0x3A UpdateSkills → emit only (skills file is read in handler still)
- 0xBA DisplayQuestArrow → emit (UIManager call in handler)
- 0xE5 DisplayWaypoint → emit only
- 0xE6 RemoveWaypoint → emit only

**Login flow**
- 0x1B EnterWorld split:
  - `World.OnPlayerEnteredWorld` sets position/graphic/direction/range/map
  - `World.OnPlayerEnteredWorldExtras` does network sends, audio volume,
    season change on death, plugin notify
- 0x55 LoginComplete → emit only (scene transition stays in handler)
- 0xA8 ServerListReceived → `LoginScene.OnServerListReceived` (typed entries)
- 0x8C ReceiveServerRelay → `LoginScene.OnServerRelayReceived` (Ip/Port/Seed)
- 0x86 UpdateCharacterList → `LoginScene.OnCharacterListUpdated`
- 0xA9 ReceiveCharacterList → `LoginScene.OnCharacterListReceived`
  (characters + cities)
- 0xFD LoginDelay → `LoginScene.OnLoginDelayReceived`
- 0x82 / 0x85 / 0x53 ReceiveLoginRejection → `LoginScene.OnLoginRejected`
  (PacketId + Reason)
- 0xB9 EnableLockedFeatures → `World.OnLockedFeaturesEnabled`
- 0xD1 Logout → emit only

**Shared helpers** (moved out of `PacketHandlers` to `World.Helpers.cs`)
- `World.UpdateGameObject`
- `World.UpdatePlayer` (11-arg overload)
- `World.AddItemToContainer`
- `World.ClearContainerAndRemoveItems`
- `PacketHandlers._requestedGridLoot` now `internal static` so the relocated
  `AddItemToContainer` can still see it.

## To Do

### Packet handlers still doing non-trivial work
- **0x1B EnterWorld** — extras subscriber owns most side effects but a few
  network sends still happen directly in the handler. Confirm vs. design intent.
- **0x6D PlayMusic** uses `0xFFFF` sentinel in `MusicPlayArgs.Index` to mean
  "stop". Cleaner: separate `MusicStopArgs` / `MusicPlayArgs`.
- **0xBF ExtendedCommand** — never migrated. Big switch with many sub-commands.
  Each sub-command should get its own typed event.
- **0xC2 UnicodePrompt** is wired but 0x9A vs 0xC2 should share semantics —
  audit.

### Subscribers / managers
- `_requestedGridLoot` should move out of `PacketHandlers` into
  `ContainerManager` or `World`. Currently `internal static` global mutable
  state on the packet handler class — a refactor smell.
- `LoginScene` lifecycle: `Load()`/`Unload()` subscribes/unsubscribes; if
  multiple scene instances overlap, double-subscription is possible. Audit
  edge cases.
- `LoginScene` still owns parsing-style code for some scene-driven flows.
  Consider extracting a `LoginCoordinator` so the scene is purely view.

### Architecture cleanup
- Many sibling `World.Subscribers.*.cs` partials. Could group them under a
  `World.Subscribers/` subfolder or split `World` further. Cosmetic.
- `EventSink` clears `event` fields in `ClearAll()` but doesn't enforce
  unsubscribe on production code. Audit for handler leaks across
  `LoginScene`/`World` lifecycles.
- Many `World.Subscribers.*.cs` files contain logic that touches
  `Client.Game.UO.*` directly (e.g. `Animations`, `FileManager`). Long term:
  inject those services via constructor rather than reaching for globals.

### Phase 3 work (per master plan, not yet started)
1. Move folder layout from current ad-hoc to the target structure
   (`Net/`, `Game/State/`, `Game/Systems/`, etc.).
2. Split god files:
   - `Network/PacketHandlers.cs` (still ~5800 lines) into per-domain partials.
   - `Network/OutgoingPackets.cs` (4670 lines).
   - `Game/UI/Gumps/OptionsGump.cs` (4941 lines) per-tab partial.
   - `Game/UI/Gumps/WorldMapGump.cs`, `HouseCustomizationGump.cs`, etc.
3. Drop XNA types from `Game/Data/*` so it can extract into `ClassicUO.Core`.
4. ViewModel split on the hot gumps (`HealthBarGump`, `StatusGump`, etc.).
5. Extract pure logic (`Pathfinder`, `MobileAnimation`, `ChairTable`,
   spell tables) to `ClassicUO.Core`.

### Outgoing packets
- `OutgoingPackets.cs` (the `Send_*` family) has not been touched. If the
  inverse event flow is desired (UI raises `RequestX`, network subscribes),
  introduce `OutgoingEventSink` or similar.

### Testing
- Only `EventSinkTests` added. Each typed event arg + each subscriber should
  have a unit test (golden-byte tests for the parsing side, behavioral tests
  for the subscriber side). Currently relying on existing 190 tests + manual
  validation that build is green.

## Notes / lessons

- Agent isolation via `worktree` mode is brittle when absolute repo paths leak
  into prompts. Two early agents wrote to the main worktree because their
  prompts referenced `C:/dev/cuo/porting/ClassicUO/...` instead of relative
  paths. Fixed by adding a `STEP 0` instruction that explicitly runs
  `git reset --hard refactor/eventsink-phase0` after agent start.
- Several agents started on `main` (no EventSink infrastructure) because the
  worktree wasn't seeded from the right branch. The mandatory reset step
  recovered them.
- Conflicts in `World.Subscribers.cs` `SubscribeEvents`/`UnsubscribeEvents`
  bodies happen on every parallel agent batch because each adds one line to
  the same block. Resolution is mechanical (keep both sides).
- `BoatMoving` subscriber initially picked up a trailing `EntityIntoHouse +
  ClearSteps` block that was NOT in the original handler — copy-paste from
  the `0x1D house revision state` handler. Removed during integration.
