# ECS Gump Porting — Coverage Index (Core Batch)

This index covers the core batch of gump specs for porting ClassicUO's UI windows from the
legacy OOP client (`src/ClassicUO.Client/`) to the ECS runtime (`src/ClassicUO.Ecs/`). The
legacy **Client is the source of truth** for layout, behaviour, packet wiring, and pixel
fidelity — each spec captures how the OOP gump works and how it maps onto the ECS infra
(`UOGumpBundle`, `WindowDragPlugin`, `UiHitTest`, Commands/Queries/Observers/Resources, the
shared UO Gump Behaviour Contract). Use the per-gump `SPEC.md` files for implementation
detail; this file is the map and the capture/verification plan.

## Gumps

| Gump | slug | complexity | spec path | server packets | trigger summary |
|------|------|-----------|-----------|----------------|-----------------|
| Status Bar / Health Bar | `statusbar-healthbar` | medium | `C:/dev/cuo/cuo-agents/.planning/gumps/statusbar-healthbar/SPEC.md` | 0x11, 0x2D (handled); 0x16, 0x17 (stubbed) | Client-side; open via paperdoll Status button / double-click self bar / single-click NPC |
| Journal | `journal` | medium | `C:/dev/cuo/cuo-agents/.planning/gumps/journal/SPEC.md` | 0x1C, 0xAE, 0xC1, 0xCC (feed only) | Top-bar Journal button (currently no-op stub) |
| Skills | `skills` | high | `C:/dev/cuo/cuo-agents/.planning/gumps/skills/SPEC.md` | 0x3A in (stub), 0x34/0x3A/0x12 out | Paperdoll Skills button (185,152) |
| Spellbook | `spellbook` | high | `C:/dev/cuo/cuo-agents/.planning/gumps/spellbook/SPEC.md` | 0x24 (graphic 0xFFFF), 0xBF 0x1B, 0xBF 0x25 | Double-click a spellbook item in backpack |
| MiniMap | `minimap` | medium | `C:/dev/cuo/cuo-agents/.planning/gumps/minimap/SPEC.md` | (none) | Top-bar Map button (~x=30,y=1) |
| Buff | `buff` | medium | `C:/dev/cuo/cuo-agents/.planning/gumps/buff/SPEC.md` | 0xDF (data only) | Client-side; macro/status menu / auto-open on first buff |
| Counter Bar | `counterbar` | high | `C:/dev/cuo/cuo-agents/.planning/gumps/counterbar/SPEC.md` | (none) | Options → Counters → Show Counters (no ECS Options yet → debug spawn) |
| Options | `options` | high | `C:/dev/cuo/cuo-agents/.planning/gumps/options/SPEC.md` | (none) | Paperdoll Options button (185,71) — ECS button is no-op |
| Macro | `macro` | high | `C:/dev/cuo/cuo-agents/.planning/gumps/macro/SPEC.md` | (none) | Options → Macros tab; standalone via `GameActions.OpenMacroGump` |
| Party | `party` | high | `C:/dev/cuo/cuo-agents/.planning/gumps/party/SPEC.md` | 0xBF sub 0x06 (codes 1/2/7), out actions 1/2/6/8/9 | Double-click paperdoll party-manifest pic (~39,196) |
| Shop / Vendor | `shop` | high | `C:/dev/cuo/cuo-agents/.planning/gumps/shop/SPEC.md` | 0x74, 0x9E, 0x3C in (stubbed); 0x3B, 0x9F out | Single/double-click vendor NPC → Buy/Sell |
| Trading | `trading` | high | `C:/dev/cuo/cuo-agents/.planning/gumps/trading/SPEC.md` | 0x6F (types 0x00–0x04); out TradeResponse/UpdateGold | Server-driven; two adjacent players initiate secure trade |

## Capture order

Ordered so the easiest-to-trigger windows come first (toggle from top-bar/paperdoll/keybind,
no special game state), then item-dependent windows, then world-state-dependent windows that
need a vendor, a party partner, or a second player.

### Tier 1 — UI-toggleable (solo, no special state)
1. **MiniMap** — top-bar Map button (~x=30,y=1); left double-click to toggle small/large.
2. **Journal** — top-bar Journal button (4th action button, 0x098D); generate chat text first so entries show.
3. **Status Bar / Health Bar** — open paperdoll, click Status button (0x07EB) or double-click self bar.
4. **Buff** — auto-open on first buff (or macro/status menu); cast a self-buff so 0xDF fires.
5. **Skills** — open paperdoll, rpc-click Skills button at paperdoll-local (185,152).
6. **Options** — open paperdoll, click Options button (0x07D6) at paperdoll-local (185,71); capture against OOP (ECS button is no-op).
7. **Macro** — Options → Macros tab for the editor; standalone fast-assign via harness `GameActions.OpenMacroGump(world, name)`.
8. **Counter Bar** — Options → Counters → "Show Counters" (no ECS Options yet → temporary debug spawn hook); drag a stackable item to populate.
9. **Party** — double-click paperdoll party-manifest pic (~root 39,196); works solo for an empty manifest.

### Tier 2 — Item-dependent
10. **Spellbook** — give the character a filled spellbook (`[add spellbook`), then double-click it; server sends 0x24 (0xFFFF) + 0xBF 0x1B.

### Tier 3 — World-state-dependent (last)
11. **Shop / Vendor** — walk next to a vendor NPC (`[add Provisioner`), single/double-click → Buy (0x74/0x3C) or Sell (0x9E, needs a sellable item in backpack).
12. **Trading** — log in two adjacent, mutually visible players and initiate a secure trade; server pushes 0x6F type 0x00 to both (or inject a synthetic 0x6F type 0x00 via the harness).

## Open questions (consolidated)

### Cross-cutting infra (affects multiple gumps)
- **Profile / settings store**: ECS has no Profile / GlobalSettings read-write layer. Many gumps depend on profile flags — `CustomBarsToggled`, `UseOldStatusGump`, `UseUOPGumps`, `StatusGumpBarMutuallyExclusive`, `CloseHealthBarType` (status/health); `JournalTabs` (journal); `BuffBarTime` (buff); `CounterBar*` + `CastSpellsByOneClick` (counterbar/macro); `PartyInviteGump` gate (party); `VendorGumpHeight` (shop). Decide: surface a settings Res, or hard-code OOP defaults for v1.
- **Gump persistence / save-restore**: ECS has no gump-save layer. Affects self-health-bar auto-restore, buff direction+position, counter bar cells/size, macro button anchoring, per-serial spellbook window position, minimap isminimized. v1 likely spawns fresh each session.
- **Missing widgets**: no slider, combobox, color-picker, editable text input, hotkey-chord capture, NiceButton, resizable-window frame, or context-menu infra in ECS. Blocks Options content controls, Macro editor (combobox/text/hotkey), Skills reset NiceButton, Counter Bar / Journal resize, Party Accept/Decline NiceButton. Decide build-vs-defer per widget.
- **Text fidelity**: `GumpBuilder.AddLabel` hard-codes FontId 0 / size 12; legacy uses UO bitmap fonts (1/2/6/8/9) with specific hues (0x0219/0x0021/0x021F/0x0288/0x0386/0x0481/0x03b2, etc.). Confirm `UoFontRenderer.Bake` pre-bake path (as in ServerGumpPlugin) vs TTF default per gump; verify Bevy.UI text-color caveats.
- **Tooltip / cliloc**: no ECS cliloc-tooltip text path. Affects modern status gump (~40 tooltips), buff per-icon title/remaining-time tooltips. Confirm deferral; carry raw cliloc ids+args on records for later translation.
- **Right-click-close semantics**: shared contract closes any topmost `UIMovable` on right-click. Conflicts with OOP windows that set `CanCloseWithRightClick=false` (counterbar uses Right+Alt on a cell; journal/skills tabs use right-click context menus). Decide adopt-ECS-close vs special-case / child consume.
- **Sound + audio**: no ECS AudioManager Res. Legacy spellbook plays sound 0x0055 on open. Defer or add.

### statusbar-healthbar
- Self health bar auto-spawn trigger in ECS (OOP restores from saved gumps on login).
- Which mobile state is already an ECS component (notoriety hue, IsRenamable, IsPoisoned/IsYellowHits, IsDead)? Poison/yellow-hits need 0x16/0x17 un-stubbed.
- Classic bar fill (GumpPicWithWidth cropped-percent) — set child `Node.Width` (renderer clips) vs a new cropped-gump kind? Custom bars covered by proposed UORect kind.
- Confirm BuffGump, PartyGump, rename text-entry, SkillsGump out of scope (log-only) for v1.

### journal
- Resize: no generic resizable-window infra — fixed-size v1 or defer resize.
- Add-tab / per-tab context menu / delete-tab need EntryDialog/ContextMenuControl/QuestionGump (none in ECS) — read-only or single-tab v1.
- Timestamp: legacy uses wall-clock `{Time:t}`; ECS forbids `DateTime.Now` in systems — capture string at packet boundary, or display engine-relative time.
- Data feed: `TextOverheadPlugin` forwards only a subset; journal needs broader feed (system/client/object/guild/party) — add `JournalLog.Add` in every cliloc/speech handler. Confirm canonical feed.
- Ignore list: is there an ECS `IgnoreManager` equivalent? If absent, v1 skips the filter.
- Scrollbar: ship draggable widget+sprite, or mouse-wheel + auto-stick-to-bottom for v1.

### skills
- ExpandableScroll vertical resize — fixed-height v1 or required for parity.
- `SkillsGroupManager` default-group table + XML persistence live only in legacy — in scope?
- Editable group name, drag-regroup, drag-out-to-SkillButtonGump — confirm v2/deferred.
- Reset legacy pops MessageBoxGump OK/Cancel; no ECS MessageBox — immediate reset in v1.
- 0x3A values are x10 fixed-point — confirm PlayerSkills keeps raw, divides at display.
- Open trigger: paperdoll Skills button only, or also top-bar/keybind (affects need for SkillsGumpOpenEvent).

### spellbook
- Spell data classes (`Spells*.cs`, `SpellDefinition`, `SpellBookType`) live only in Client — must port into ECS first (largest blocker).
- No ECS `World.ActiveSpellIcons` equivalent for hue-38 active highlight — needs a Res fed by the 0xBF 0x25 observer.
- 0x24 graphic 0xFFFF has no ECS handler today (container plugin skips it) — confirm spellbook observer receives it.
- Per-serial window position cache — defer or add.
- Confirm 0xBF 0x1B bitfield always arrives so the item-child-list fallback isn't needed.
- Mastery's Abilities icon column + SpellbookIndices paging differ — confirm v1 defers Mastery.

### minimap
- Renderer data plumbing: extend `UOCustomRender` with minimap fields (player pos, blink, dot list, cached radar texture) vs a dedicated sibling payload.
- Per-window radar `Texture2D` ownership + disposal on window despawn.
- Confirm `Res<UOFileManager>.Maps` exposes the block-read API (reuse `TerrainPlugin`'s map-block accessor instead of `World.Map`).
- Live multi/static overlay: ECS has no Chunk tile grid — v1 may render terrain+statics from files only, skip live multis.
- Mobile enumeration + NotorietyFlag resolution in ECS — need the notoriety-carrying component to hue dots.
- Top-bar Map button wiring: EventWriter/typed trigger vs direct observer.

### buff
- Open trigger: macro/hotkey BuffGumpOpenEvent vs auto-open on first buff (proposed v1: auto-open).
- Is `Clilocs.Translate` reachable from the packet observer for title/description?
- Replicate legacy RIGHT_* negative-shift origin compensation, or allow negative child Left/Top in Clay (verify identical render/hit-test).

### counterbar
- Confirm queryable shape of player's carried + nested container items so `GetTotalAmountOfItem` is reproducible without `World` access.
- No ECS context-menu infra for Add/Read-only/Use/CompareTo/IgnoreHue; CompareTo entry-dialog + macro UseSlot unported — likely out of v1.
- Count label needs UO font 1 + BlackBorder; `AddLabel` hard-codes FontId 0.
- No generic resizable-window widget — bottom-right resize handle + SnapToGrid is net-new; consider fixed-grid v1.
- No sanctioned open trigger (no packet, no Options gump) — debug keybind/top-bar/Options port needed.

### options
- No Profile/GlobalSettings write-back — Apply/Default/OK have no store; Apply side-effects touch dozens of subsystems.
- No slider/combobox/color-picker/editable-text widgets — v1 may ship checkboxes + tab nav only.
- Macros (page 4) and Info Bar (page 10) are deeply stateful — likely a later phase.
- Background fidelity: confirm `UOCustomKind.None` + BackgroundColor matches `AlphaBlendControl(0.95)` hue 999 (need exact RGBA).
- NiceButton tab-rail selection highlight — need an ECS equivalent (highlighted BackgroundColor on selected tab).
- Spawn position: OOP opens at (0,0); decide ECS placement (centered / top-left / mouse-anchored).

### macro
- Combobox + editable text + HotkeyBox chord-capture don't exist — the long pole; block on reusable widgets or ship MacroButton-only + stubbed editor.
- `MacroManager.Process` is ~1400 lines touching network/targeting/gump-open/profile — which actions ship in v1.
- `UOCustomRender` has no color field; `UOCustomKind.SolidFill` needs fill/border color — explicit Clay.Color fields vs pack into Hue.
- Need Res exposing CastSpellsByOneClick + Alt modifier state for run-on-click vs double-click branch.
- OOP MacroButtonGump is AnchorableGump + persists; ECS has no anchor/gump-save — free-floating non-persistent button OK for v1?
- `MacroGump` centers on `camera.Bounds.Width/2` then SetInScreen-clamps — confirm the ECS viewport/camera-size Res.

### party
- System-chat injection for Tell/Send-message buttons — confirm a programmatic chat-input path or v1 no-ops.
- No NiceButton for Accept/Decline — BackgroundColor box + label + On<UiClick>, or add a GumpBuilder text-button helper.
- PartyState change signal: `OnInsert<PartyState>` on a singleton vs a dedicated `PartyChangedEvent` rebuild trigger.
- Member-name source/fallback: confirm `NetworkEntitiesMap` → mobile `Name` component path with not-seen fallback.

### shop
- Buy-icon graphic/hue/amount come from the vendor's buy-container (0x3C via ContainerSerial), not 0x74 — how does the ECS container path expose those keyed by container serial, and is 0x3C guaranteed before 0x74? Start with self-contained sell (0x9E).
- Mobile buy entries render as animation frame 0 in OOP — is there a usable `UOCustomKind.Animation` render path, or fall back to art in v1.
- Confirm `Send_BuyRequest` / `Send_SellRequest` exist on the ECS NetClient.
- Sliced/tiled gump bg needs a new ClayUO `GumpSlice` command (sub-rect + vertical tiling + height override) + matching `UiHitTest.PixelHit` case; confirm `UltimaBatcher2D.DrawTiled` + sub-rect Draw in the GUI batcher.
- ResizePicLine divider (0x39/0x3A/0x3B triplet) — dedicated command vs three composed child sprites.

### trading
- Cancel-on-close: `CloseOnRightClick` despawns subtrees with no event — port `Dispose()`→CancelTrade via `OnRemove<TradeWindow>` (with ServerClosed guard) or add a TradeWindow branch to `CloseOnRightClick` like ContainerWindow.
- Numbers-only text-input for gold/plat entries, or render coin labels read-only and defer editable entries + `Send_TradeUpdateGold`.
- Are the two trade containers (ID1/ID2) ever opened as standalone ContainerGumps too, and can their update events route into the trade boxes without a duplicate window?
- Confirm ECS target is modern-only (0x088A); legacy 0x0866 + CV<500A ColorBox fillers out of scope.
- Confirm each trade box clamps dropped item X/Y against its own 110x80 bounds (OOP has a latent quirk using `_myBox` dims when clamping the his box).
