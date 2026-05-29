# Shop / Vendor Gump Spec

> ECS implementation spec for the ClassicUO Shop / Vendor (buy & sell) window.
> The legacy OOP `ShopGump` is the source of truth.

## Overview

The Shop gump is the vendor buy/sell window. It opens when the player double-clicks
(or single-clicks "Buy"/"Sell" on) a vendor NPC. It is a **two-panel** window:

- **Left panel** — the vendor's stock list (items the player can buy) OR the player's
  sellable inventory (items the player can sell). One row per item: an art/animation
  icon, a divider line, the item name + price, and an amount counter. Scrollable.
- **Right panel** — the *transaction* list (the player's running cart). Double-clicking
  a left-panel row moves 1 (or the full stack with Shift held) into the cart. Each cart
  row has a name, amount, and +/- spinner buttons. Below the cart: a **Total** label,
  the player's **Gold** label (buy mode only), and **Accept** / **Clear** buttons.

There are two visual flavours selected by `isBuyGump`:

- **Buy** (`isBuyGump == true`): vendor's stock. Icons of *mobile* items render as
  animation frames; everything else renders as art. Player gold shown.
- **Sell** (`isBuyGump == false`): player's items. Icons always render as art. No gold
  label; the Total label is repositioned.

The window is resizable vertically via a drag "expander" button at the bottom-center
of the left panel; the chosen height persists to `Profile.VendorGumpHeight`.

The gump auto-closes (disposes) when its stock list becomes empty (e.g. after Accept,
or when the server clears it).

## Source of truth

- `src/ClassicUO.Client/Game/UI/Gumps/ShopGump.cs` — entire file (single class).

Key references (line numbers from the Client source):

| Concern | Lines |
|---------|-------|
| Ctor + panel construction | `ShopGump(...)` ctor, L66–312 |
| Left/right background graphic constants | L84–90 |
| Background slicing (top/middle/bottom GumpPicTexture) | L92–150 |
| Shop scroll area | L152–161 |
| Transaction scroll area + DataBox | L163–175 |
| Total label | L177–183 |
| Player gold label (buy only) | L185–198 |
| Expander (resize) button | L200–207 |
| Accept / Clear hitboxes | L209–229 |
| Scroll up/down hitboxes (left + right panels) | L231–288 |
| Resize logic (`Update`, expander drag) | L371–457 |
| Scroll repeat (`ProcessListScroll`) | L464–485 |
| AddItem (populate stock list) | L334–361 |
| Double-click row → add to cart | L487–528 |
| Cart +/- handlers | L530–576 |
| Row select highlight | L578–588 |
| Accept / Clear → send buy/sell | L590–621 |
| `ShopItem` inner control (left row) | L629–898 |
| `ShopItem.AddToRenderLists` (icon draw: anim vs art) | L778–897 |
| `TransactionItem` inner control (cart row) | L900–1098 |
| `ResizePicLine` divider (graphic 0x39 triplet) | L1100–1157 |
| `GumpPicTexture` (sliced/tiled bg sprite) | L1159–1223 |

Supporting Client refs:
- Buy/Sell wire send: `NetClient.Socket.Send_BuyRequest` / `Send_SellRequest` (L601, L605).
- `ResGumps.Item0Price1` — the "`{0}: {1}`" name/price format string (L664, L769).

## Visual structure

The window is built from **two** background gumps (left + right), each sliced into a
fixed top, a vertically-tiled stretchable middle, and a fixed bottom. The right gump is
offset down-and-right relative to the left so the two panels overlap into one window.

Geometry constants (from the source):

```
LEFT_TOP_HEIGHT     = 64
LEFT_BOTTOM_HEIGHT  = 116
RIGHT_OFFSET        = 32
RIGHT_BOTTOM_HEIGHT = 93
default middle height = Profile.VendorGumpHeight  (resizable; clamp lo=minHeight, hi=640)
```

Let `LW = artInfoLeft.UV.Width`, `LH = artInfoLeft.UV.Height` (native size of the left bg
gump), `RW`/`RH` likewise for the right bg gump. `H` = the live left-middle height
(= `Profile.VendorGumpHeight`). `diff = H - nativeMiddleHeight`.

Right panel origin:
```
rightX = LW - RIGHT_OFFSET          (=LW-32)
rightY = LH/2 - RIGHT_OFFSET        (=LH/2-32)
```

### Control tree

- **Window root** (UOGump, UIMovable) — no own sprite; the sliced bg pieces are children.
  - **Left-top** `GumpPicTexture` (graphic = left bg) at (0,0), source rect (0,0,LW,64),
    not tiled.
  - **Left-middle** `GumpPicTexture` (left bg) at (0,64), source rect (0,64, LW, LH-180),
    **tiled vertically**, height overridden to `H`.
  - **Left-bottom** `GumpPicTexture` (left bg) at (0, 64+H), source rect (0, 64+(LH-180),
    LW, 116), not tiled.
  - **Right-top** `GumpPicTexture` (right bg) at (rightX, rightY), source (0,0,RW,64).
  - **Right-middle** `GumpPicTexture` (right bg) at (rightX, rightY+64), source
    (0,64,RW,RH-157), tiled, height = nativeMiddle + diff.
  - **Right-bottom** `GumpPicTexture` (right bg) at (rightX, rightY+64+rightMiddleH),
    source (0,*,RW,93), not tiled.
  - **Shop scroll area** (left list viewport) at (32, 64), size
    (LW - 64 + 5, H + 50), scroll-max-height `H`. Contains the **ShopItem** rows.
  - **Transaction scroll area** (right cart viewport) at (rightX+16, 64+rightY), size
    (RW - 64 + 16 + 5, rightMiddleH). Contains a **DataBox** → **TransactionItem** rows.
  - **Total label** (`Label`, isunicode, hue 0x0386, font 0, align 1):
    - Buy: at (rightX + 32 + 4 + RIGHT_OFFSET, rightBottomY + 93 - 96 + 15).
    - Sell: X = (rightX + RW) - 96; same Y.
    - Text = running total cost ("0" initially).
  - **Player gold label** (buy only) (`Label`, hue 0x0386) at (TotalLabel.X+120,
    TotalLabel.Y). Text = `World.Player.Gold`.
  - **Expander** `Button(2, 0x082E, 0x082F)` at (LW/2 - 10, leftBottomY + 116 - 5).
    Resize handle.
  - **Accept** `HitBox` (invisible, alpha 0) at (rightX+32, rightBottomY+93-50), 34×30.
  - **Clear** `HitBox` (invisible) at (Accept.X+175, Accept.Y), 20×20.
  - **Left scroll-up** `HitBox` at (leftTop.right-50, leftTop.bottom-18), 18×16.
  - **Left scroll-down** `HitBox` at (sameX, leftBottomY), 18×16.
  - **Right scroll-up** `HitBox` at (rightTop.right-50, rightTop.bottom-18), 18×16.
  - **Right scroll-down** `HitBox` at (sameX, rightBottomY), 18×16.

### ShopItem row (left list) — `ShopItem`, L629

Row size: Width 220, Height = `max(50, max(nameH,35)+10 [+staticHeight for items]) + lineH`.
- **Divider** `ResizePicLine(0x39)` at (10, 0), width 190. Draws gump triplet
  `0x39 | 0x3A | 0x3B` (left cap | tiled middle | right cap).
- **Icon** (no child control — drawn in `AddToRenderLists`):
  - Buy + serial is a *mobile*: animation frame 0 of stand group, clamped to 45×45, drawn
    at (x-3, y+20). Hued via the animation's own hue + partial-hue flag.
  - Otherwise: art `Graphic`, real art bounds, clamped to a 50×Height box, centered,
    drawn at (x+pt.X-5, y+pt.Y+10). Hued via item `Hue` + partial-hue flag.
- **Name label** (`Label`, isunicode, hue 0x0219 normal / 0x0021 selected, maxwidth 110,
  font 1, align left, wrap) at (55,15). Text = `"{CapitalizedName}: {Price}"`
  (`ResGumps.Item0Price1`).
- **Amount label** (`Label`, isunicode, hue 0x0219, maxwidth 35, font 1, align right) at
  (168, 15 + height/4). Text = remaining count.

### TransactionItem row (right cart) — `TransactionItem`, L900

Row size: Width 245, Height = name label height.
- **Name label** (`Label`, isunicode, hue 0x021F, maxwidth 140, font 1, align left, wrap)
  at (50, 0).
- **Amount label** (`Label`, isunicode, hue 0x021F, maxwidth 35, font 1, align right) at
  (10, 0).
- **Plus button** `Button(0, 0x37, 0x37)` at (190, 5), ContainsByBounds.
- **Minus button** `Button(1, 0x38, 0x38)` at (210, 5), ContainsByBounds.
  - Both buttons auto-repeat while held (500ms initial delay, then accelerating ~45ms
    steps — see L965–1060). One step = ±1 (±full stack with Shift).

## Assets

| Asset | ID | Kind | Where | Notes |
|-------|----|------|-------|-------|
| Buy bg left | 0x0870 | Gump (sliced) | left panel, buy | top64 / tiled mid / bottom116 |
| Buy bg right | 0x0871 | Gump (sliced) | right panel, buy | top64 / tiled mid / bottom93 |
| Sell bg left | 0x0872 | Gump (sliced) | left panel, sell | "" |
| Sell bg right | 0x0873 | Gump (sliced) | right panel, sell | "" |
| Row divider line | 0x39, 0x3A, 0x3B | Gump triplet | each ShopItem | cap/tiled/cap, width 190 |
| Expander (resize) btn | 0x082E (normal), 0x082F (pressed) | Gump | left bottom | drag to resize |
| Cart "+" button | 0x37 | Gump | TransactionItem | increase amount |
| Cart "-" button | 0x38 | Gump | TransactionItem | decrease amount |
| Item icon (sell / non-mobile buy) | item `Graphic` | Art | ShopItem | clamp 50×H, centered |
| Mobile icon (buy) | mobile `Graphic` | Animation frame 0 | ShopItem | clamp 45×45 |
| Accept / Clear / scroll | none | invisible HitBox | — | hit-region only, alpha 0 |

| Hue | Usage |
|-----|-------|
| 0x0386 | Total label, player gold label |
| 0x0219 | ShopItem name + amount (normal) |
| 0x0021 | ShopItem labels when row selected (highlight) |
| 0x021F | TransactionItem name + amount |

Fonts: ShopItem/TransactionItem labels use **font 1** (unicode). Total/gold labels use
**font 0** (unicode). All labels are `isunicode = true`.

## Behaviors

| Behavior | Client mechanism | ECS mechanism |
|----------|------------------|---------------|
| **Drag to move** | `CanMove = true` | `UIMovable` on root → `WindowDragPlugin.Drag` |
| **Right-click closes** | `CanCloseWithRightClick` | `UIMovable` → `WindowDragPlugin.CloseOnRightClick`. NOT a container window, so it despawns in-place (no `ContainerClosedEvent`). |
| **Topmost on click** | UIManager z | `GlobalZIndex` on root only; `UiZCounter.Bump()` on drag latch |
| **Click-capture over window** | AcceptMouseInput | `ClaimSelectedFromMovable` (root has no NetworkSerial/Items, so world/pickup bail) |
| **Pixel-perfect hit-test** | per-control bounds | sliced bg pieces are `UOCustomKind.Gump` → `UiHitTest.PixelHit` masks transparent areas |
| **Accept button** | `_accept.MouseUp` → `OnButtonClick(Accept)` → `Send_BuyRequest`/`Send_SellRequest`, then `Dispose()` | `On<UiClick>` observer on Accept entity → send buy/sell, despawn window |
| **Clear button** | `_clear.MouseUp` → remove all cart items | `On<UiClick>` observer → despawn all cart rows, restore amounts, reset total |
| **Add to cart** | row `MouseDoubleClick` → create/increment `TransactionItem`; Shift = full stack | `On<UiDoubleClick>` observer on ShopItem entity (Bevy.UI synthesizes UiDoubleClick); mutate `ShopCart` resource + spawn/inc TransactionItem child |
| **Cart +/-** | spinner buttons w/ auto-repeat | `On<UiClick>` on +/- entities for the single-step case. Auto-repeat-on-hold is a per-frame system gated on `Interaction.Hovered + IsPressed` reading `Res<Time>` for the delay (NO `Time.Ticks`). |
| **Row select highlight** | `MouseUp` recolors clicked row's labels | `On<UiClick>` observer on ShopItem → set a `ShopRowSelected` marker; a system recolors label `TextColor` / hue (or store selected serial in `ShopCart` and recolor in a system). |
| **Vertical resize** | expander drag in `Update`, `Mouse.LDragOffset.Y` | A `ShopResizePlugin` system: while expander `Interaction == Pressed`, snapshot anchor (`Local<>`), recompute middle heights + reposition the moving children, clamp `[minHeight,640]`. Persist to a `ShopGumpHeight` resource (Profile is not in ECS scope yet — see Open questions). |
| **Scroll buttons (auto-repeat)** | `ProcessListScroll`, 60ms delay | scroll-area is a `ScrollPosition` node (mirror ServerGumpPlugin htmlgump scroll); scroll buttons nudge `ScrollPosition` on a `Res<Time>`-gated repeat while held. |
| **Mouse-wheel scroll** | ScrollArea | `Overflow.Scroll` + `ScrollPosition` on the two viewport nodes (Clay handles wheel). |
| **Auto-close when empty** | `Update`: stock count 0 → `Dispose()` | when `ShopCart`/stock for this window hits 0 items, despawn (only relevant after Accept; v1 may just despawn on Accept). |
| **Live gold update** | `Update` re-reads `World.Player.Gold` | a system reads `Player`'s gold component each frame and writes the gold label `Text` (only while a buy window is open). |
| **Live total update** | `_updateTotal` flag | recompute on cart change inside the add/remove observers; write Total label `Text`. |

Buttons fire on **release** (`On<UiClick>`) per the UO Gump contract — matching OOP's
`MouseUp` / `ButtonAction.Activate`.

## Server packets

This gump is **server-driven open + populate**. The two list packets are already parsed
in the ECS branch (`OnBuyListPacket_0x74`, `OnSellListPacket_0x9E`) but currently
**stubbed** (`InGamePacketsPlugin.cs:268,280`) — they parse and discard. This plugin
must add observers that actually build/populate the gump.

| Opcode | Direction | Role | ECS struct |
|--------|-----------|------|-----------|
| **0x3B SecureTrading? no** — actually **0x3C** container-contents (vendor's buy box) | S→C | Carries the buy items' graphic/hue/amount/serial; arrives *before/with* 0x74. The buy list 0x74 only carries price + name keyed by index/serial into that container. | (existing container content packet) |
| **0x74** Buy item list (prices + names) | S→C | Opens/populates the **buy** window. Contains `ContainerSerial`, `Count`, then per entry `{Price, Name}`. Names/prices pair positionally with the container contents already received. | `OnBuyListPacket_0x74` |
| **0x9E** Sell item list | S→C | Opens/populates the **sell** window. Contains vendor `Serial`, `Count`, then per entry `{ItemSerial, Graphic, Hue, Amount, Price, Name}` (fully self-contained). | `OnSellListPacket_0x9E` |
| **0x3B** Buy request | C→S | Sent on Accept (buy). `Send_BuyRequest(vendorSerial, (serial,amount)[])`. | (send) |
| **0x9F** Sell request | C→S | Sent on Accept (sell). `Send_SellRequest(vendorSerial, (serial,amount)[])`. | (send) |

> Note: the **buy** path needs the vendor's buy-container contents (graphic/hue/amount,
> via the container 0x3C path + the `ContainerSerial`) to render icons, since 0x74 itself
> carries only price + name. Confirm how the ECS container-content packet exposes those
> items keyed by `ContainerSerial` (see Open questions). The **sell** path (0x9E) is
> self-contained and is the simpler first target.

## ECS implementation plan

**Plugin:** `ShopGumpPlugin` → `src/ClassicUO.Ecs/Gameplay/ShopGumpPlugin.cs`
(sibling of `PaperdollPlugin.cs`). Compose it in `Boot.cs` `CuoPlugin.Build`.

### Resources

```csharp
internal sealed class ShopState   // app.AddResource(new ShopState())
{
    // Per open window: cart contents keyed by item serial, selected row, etc.
    // Keyed by window root entity id.
    public readonly Dictionary<ulong, ShopWindowData> Windows = new();
}
internal sealed class ShopGumpHeight { public int Value = /*default*/; } // stands in for Profile.VendorGumpHeight (see Open Q)
```

Prefer holding mutable per-window cart state in **components on entities** where it maps
cleanly (cart rows are entities; their amount is a component), and only the cross-system
"current total / selected serial / resize anchor owner" in `Res<ShopState>`. Resize
anchor is a `Local<>` on the resize system (per CLAUDE.md rule 3).

### Components

```csharp
internal struct ShopWindow      { public uint VendorSerial; public bool IsBuy; }            // root marker (alongside UOGump)
internal struct ShopListRoot    { public ulong WindowEntity; }                              // left scroll viewport
internal struct ShopCartRoot    { public ulong WindowEntity; }                              // right scroll viewport (DataBox analogue)
internal struct ShopItemRow     { public ulong WindowEntity; public uint ItemSerial; public uint Price; public ushort Graphic; public ushort Hue; public bool IsMobile; public int Remaining; }
internal struct ShopCartRow     { public ulong WindowEntity; public uint ItemSerial; public int Amount; public uint Price; }
internal struct ShopTotalLabel  { public ulong WindowEntity; }
internal struct ShopGoldLabel   { public ulong WindowEntity; }
internal struct ShopExpander    { public ulong WindowEntity; }                              // resize handle
internal struct ShopScrollButton{ public ulong WindowEntity; public bool Cart; public bool Up; }
internal struct ShopCartSpinner { public ulong CartRowEntity; public bool Increase; }
```

### Bundle usage

The window background is **two sliced gumps**, not a single sprite — `UOGumpBundle`
assumes one bg sprite. Two options:

1. **Preferred:** spawn the root as an invisible drag/close surface
   (`UOCustomKind.None`, sized to the full window bbox) exactly like
   `ServerGumpPlugin`'s no-resizepic fallback (insert `Node` + `UiCustom{None}` +
   `Interaction.None` + `UIMovable` + `GlobalZIndex`). Then add the six sliced bg pieces
   as child gump sprites. This keeps the bg slices independently positioned/tiled and
   resizable, and the `None` surface gives the whole window a solid hit-test + drag area.
2. Alternatively `UOGumpBundle` with one panel as the "primary" bg — rejected: the second
   panel + slicing/tiling don't fit the single-sprite bundle.

Sliced bg pieces need a **sub-rectangle gump draw** (draw only `[srcX,srcY,srcW,srcH]` of
the gump, optionally vertically tiled). `GumpBuilder.AddGump` / `AddGumpTiled` draw the
*whole* sprite. **A new ClayUO custom command is required** (see below).

Item icons: sell + non-mobile-buy → `GumpBuilder.AddArtSized` (already clamps + centers,
matching `ShopItem`'s 50×H art box). Mobile-buy icons → animation frame, needs the
`Animation` custom kind (already an enum value) wired for shop use, or fall back to art
in v1 (Open question).

### New ClayUO custom render command

The sliced background requires drawing a **sub-rectangle** of a gump sprite, with
optional vertical tiling and an overridden height — `GumpPicTexture` in OOP. Add:

1. New `UOCustomKind.GumpSlice` (or extend `UOCustomRender` with a `SourceRect` +
   `Tiled` field) in `GuiPlugin.cs`.
2. Render case in `GuiRenderingPlugin.cs`: pull `assets.Value.Gumps`, then
   `batcher.Draw` / `batcher.DrawTiled` with src rect `gumpInfo.UV + sliceRect`, dest
   `(x,y,Width,Height)`, respecting `cmd.zIndex`. Mirror `GumpPicTexture.AddToRenderLists`
   (ShopGump.cs L1180–1222).
3. `UiHitTest.PixelHit` case for `GumpSlice`: map cursor into the slice's source mask
   (like the `Gump` / `GumpTiled` cases) so transparent panel corners pass through.

The divider line (`ResizePicLine`, triplet `0x39/0x3A/0x3B`) is a left-cap + tiled-mid +
right-cap composite — either a second small custom command or three sliced/tiled child
sprites. Reuse `GumpNinePatch` semantics where possible.

### Observers

- `app.AddObserver<On<PacketReceived<OnSellListPacket_0x9E>>, Commands, ShopSpawnParams>(SpawnSellGump)`
  — build the sell window from the self-contained 0x9E entries.
- `app.AddObserver<On<PacketReceived<OnBuyListPacket_0x74>>, Commands, ShopSpawnParams>(SpawnBuyGump)`
  — build the buy window; resolve icon graphic/hue/amount from the vendor's container
  contents (`ContainerSerial`).
- `On<UiDoubleClick>` (global, filtered by `ShopItemRow`) → add to cart (mirror
  `PaperdollPlugin`'s backpack dclick observer pattern).
- `On<UiClick>` observers wired per-entity (`.Observe(...)`) on Accept / Clear / +/- /
  scroll buttons / ShopItem rows (selection). Entity-scoped, mirroring
  `ServerGumpPlugin` buttons and `PaperdollPlugin` buttons.
- Dedup-on-reopen: query existing `ShopWindow` for the same vendor serial + mode; focus
  (z-bump) instead of duplicating (mirror `SpawnOnOpenPaperdoll`).
- `OnExit(GameState.GameScreen)` dispose system (mirror `PaperdollPlugin.DisposeOnLogout`).

### Systems

- `ShopResize` (Stage.Update) — while the expander entity's `Interaction == Pressed`,
  resize middle slices + reposition the dependent children, clamp, store new height in
  `ShopGumpHeight`. Drag anchor in `Local<ShopResizeAnchor>`. Uses `Res<Time>` not
  wall-clock.
- `ShopScrollRepeat` (Stage.Update) — while a `ShopScrollButton` is held, nudge the
  target viewport's `ScrollPosition` on a `Res<Time>`-gated 60ms repeat.
- `ShopCartSpinnerRepeat` (Stage.Update) — auto-repeat +/- while held (500ms then
  accelerating), reading `Res<Time>`.
- `ShopRefreshLabels` (Stage.Update) — recompute Total from cart rows; write Total +
  (buy) Gold label `Text`. Could be folded into the cart-mutation observers to avoid a
  per-frame scan; gold must poll the player component while a buy window is open.

### Sending requests

On Accept: gather cart rows for the window → `(serial, amount)[]`, call
`net.Value.Send_BuyRequest(vendorSerial, items)` or `Send_SellRequest(...)`, then despawn
the window subtree. Confirm both send helpers exist on the ECS `NetClient` (Open Q).

## How to trigger for capture

The harness ModernUO boot (`127.0.0.1:2593`, `admin/admin`) — see
`tools/agent-desktop/AGENTS.md`. Steps:

1. `dotnet build -p:AGENT_BUILD=true`, then `agent-desktop up --persist`.
2. Log in to the game world.
3. Walk the player next to a **vendor NPC** (any town shopkeeper — e.g. a Provisioner /
   Blacksmith in a guarded town). On a fresh ModernUO server you may need to spawn one:
   as admin, use `[add <vendortype>` (e.g. `[add Provisioner`) and place it.
4. **Buy window:** single-click the vendor and pick **"Buy"** from the context menu, OR
   double-click the vendor (servers vary). Server sends container contents (0x3C) + 0x74.
5. **Sell window:** click the vendor and pick **"Sell"** (only shows items the vendor
   buys that you carry — ensure inventory has at least one sellable item). Server sends
   0x9E.
6. `agent-desktop rpc-shot` for the reference screenshot; `down` when finished.

Required game state: in-world (not login/char-select), a vendor within range, and (for
sell) at least one sellable item in the player's backpack.

## Open questions

- **Buy-icon source.** 0x74 carries only price + name; the icon graphic/hue/amount come
  from the vendor's buy *container contents* (`ContainerSerial`, packet 0x3C). How does
  the ECS container path expose those items keyed by container serial, and is the
  contents packet guaranteed to arrive before 0x74? (Sell 0x9E is self-contained — start
  there.)
- **Mobile icons in buy mode.** ShopItem renders *mobile* buy entries (e.g. pets/animals
  for sale) as animation frame 0. Is there an existing ECS animation-frame UI render path
  (`UOCustomKind.Animation`) usable for static gump icons, or do we fall back to art in
  v1?
- **VendorGumpHeight persistence.** OOP persists the resize height to
  `Profile.VendorGumpHeight`. Is there an ECS profile/settings store to persist to, or
  should v1 keep it in a session-only `Res<ShopGumpHeight>`?
- **Send helpers.** Confirm `Send_BuyRequest(uint, (serial,amount)[])` and
  `Send_SellRequest(...)` exist on the ECS `NetClient` (they exist on the OOP socket).
- **Sliced/tiled gump rendering.** Confirm `UltimaBatcher2D.DrawTiled` + sub-rect draw
  are available in `GuiRenderingPlugin`'s batcher (they are used elsewhere) before
  committing to the `GumpSlice` custom command vs. composing from existing kinds.
- **Row label fonts/hues.** Bevy.UI Text currently renders TTF; OOP uses bitmap font 1
  with specific hues (0x0219 / 0x0021 / 0x021F). Exact color mapping needs the same
  `HueToClayColor`-style translation `ServerGumpPlugin` uses; verify those hues read
  correctly.
- **`ResizePicLine` divider.** Whether to add a dedicated custom command for the
  cap/tiled/cap triplet (0x39/0x3A/0x3B) or compose from three child sprites.
