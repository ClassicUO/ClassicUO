// Vendor buy/sell gump — ECS port of Game/UI/Gumps/ShopGump.cs.
//
// Buy: server sends 0x24 graphic 0x0030 (vendor serial). The shop items already
// live in the vendor's shop-layer containers (filled by 0x3C) and are priced by
// 0x74 (stamped as VendorListing on each item entity). Sell: 0x9E carries the
// full item list inline (cached in VendorStore.SellByVendor).
//
// Layout mirrors legacy: two resizable panels built from the buy/sell gump art
// (0x0870-0x0873) sliced top/middle(tiled)/bottom. Left = scrollable shop list;
// right = transaction list with +/- per line, a running total, the player's gold
// (buy only) and Accept/Clear. Double-click a shop row to add it (Shift = all);
// Accept sends 0x3B (buy) / 0x9F (sell).

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct VendorGumpPlugin : IPlugin
{
    private const ushort BuyLeft = 0x0870, BuyRight = 0x0871;
    private const ushort SellLeft = 0x0872, SellRight = 0x0873;
    private const int TopH = 64, LeftBotH = 116, RightBotH = 93, RightOffset = 32;
    private const ushort SortGraphic = 0x2AF8; // legacy: this container sorts by X, others read reversed
    private const ushort TextHue = 0x0219;

    public void Build(App app)
    {
        app.AddResource(new VendorStore());

        // Buy: 0x24 graphic 0x0030.
        app.AddObserver((
            On<PacketReceived<OnOpenContainerPacket_0x24>> trig,
            ResMut<VendorStore> store,
            EventWriter<VendorOpenedEvent> opened) =>
        {
            if (trig.Event.Packet.Graphic != 0x0030) return;
            store.Value.Revision++;
            opened.Send(new VendorOpenedEvent(trig.Event.Packet.Serial, true));
        });

        // Sell: 0x9E carries the whole list. Names resolve live at rebuild.
        app.AddObserver((
            On<PacketReceived<OnSellListPacket_0x9E>> trig,
            ResMut<VendorStore> store,
            EventWriter<VendorOpenedEvent> opened) =>
        {
            var p = trig.Event.Packet;
            var list = new List<VendorEntry>(p.Entries.Count);
            foreach (var e in p.Entries)
                list.Add(new VendorEntry { Serial = e.ItemSerial, Graphic = e.Graphic, Hue = e.Hue, Amount = e.Amount, Price = e.Price, RawName = e.Name });
            store.Value.SellByVendor[p.Serial] = list;
            store.Value.Revision++;
            opened.Send(new VendorOpenedEvent(p.Serial, false));
        });

        // OPL (0xD6 mega-cliloc) carries the real item names; they typically
        // arrive after the gump opens. Bump the revision so open windows rebuild
        // and re-resolve names from the now-populated OPL (legacy SetNameTo).
        app.AddObserver((
            On<PacketReceived<OnMegaClilocPacket_0xD6>> trig,
            ResMut<VendorStore> store) =>
        {
            store.Value.Revision++;
        });

        // 0x74 BuyList — stamp price + raw name onto the container's item
        // entities. Display names resolve live at rebuild.
        app.AddObserver((
            On<PacketReceived<OnBuyListPacket_0x74>> trig,
            Commands commands,
            ResMut<VendorStore> store,
            Res<NetworkEntitiesMap> map,
            Query<Data<TinyEcs.Children>> childrenQ,
            Query<Data<Graphic>> graphicQ,
            Query<Data<ContainerSlotPosition>> slotQ) =>
        {
            var p = trig.Event.Packet;
            if (!map.Value.TryGet(p.ContainerSerial, out var containerEnt)) return;
            var ordered = OrderShopChildren(containerEnt, ReadGraphic(graphicQ, containerEnt), childrenQ, slotQ);
            int n = Math.Min(ordered.Count, p.Entries.Count);
            for (int i = 0; i < n; i++)
                commands.Entity(ordered[i]).Insert(new VendorListing { Price = p.Entries[i].Price, RawName = p.Entries[i].Name });
            store.Value.Revision++;
        });

        // Server-driven close (0x3B handled elsewhere as ContainerClosed) and
        // distance close route through ContainerClosedEvent.
        app.AddObserver((
            On<ContainerClosedEvent> trig,
            Commands commands,
            Query<Data<VendorWindow>> windowsQ,
            Query<Data<TinyEcs.Children>> childrenQ) =>
        {
            foreach (var (ent, w) in windowsQ)
                if (w.Ref.Vendor == trig.Event.Serial)
                    DespawnSubtree(commands, ent.Ref, childrenQ);
        });

        // Clicks on the vendor surfaces go through a bbox poll (PreUpdate +
        // DragGate claim), like the spellbook corners/icons: Clay doesn't fire
        // UiClick reliably on these None/Art surfaces inside a scroll, and the
        // poll also keeps the click from latching a window drag.
        // SingleThreaded: these systems are the only ones that touch the vendor
        // component types, and several first-register those types via their query
        // builds on the same entering-GameScreen frame — running them in the
        // parallel pool races TinyEcs's lazy component registration (GrowSlots).
        var clickFn = VendorClicks;
        app.AddSystem(clickFn).InStage(Stage.PreUpdate).SingleThreaded().Build();

        var scrollFn = VendorScroll;
        app.AddSystem(scrollFn).InStage(Stage.PreUpdate).SingleThreaded().Build();

        var openFn = OpenWindow;
        app.AddSystem(openFn).InStage(Stage.Update).SingleThreaded()
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var rebuildFn = RebuildContent;
        app.AddSystem(rebuildFn).InStage(Stage.Update).SingleThreaded()
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var resizeFn = ResizeDrag;
        app.AddSystem(resizeFn).InStage(Stage.PreUpdate).SingleThreaded().Build();

        var despawnFn = DespawnAll;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

        // Register the vendor component types up front (Stage.Startup is
        // single-threaded). TinyEcs's lazy GrowSlots isn't thread-safe; without
        // this, several vendor systems first-register their types via parallel
        // query builds on the entering-GameScreen frame and crash in GrowSlots.
        var warmFn = WarmComponentTypes;
        app.AddSystem(warmFn).InStage(Stage.Startup).Build();
    }

    private static void WarmComponentTypes(Commands commands)
    {
        var e = commands.Spawn()
            .Insert(default(VendorWindow))
            .Insert(default(VendorShopRow))
            .Insert(default(VendorTxnButton))
            .Insert(default(VendorActionButton))
            .Insert(default(VendorResizeHandle))
            .Insert(default(VendorScrollButton))
            .Insert(default(VendorListing));
        commands.Entity(e.Id).Despawn();
    }

    // Buy name (legacy PacketHandlers.BuyList): OPL name first, else a numeric
    // raw is a cliloc translated with "\t{itemdata}: \t{amount}" args, else the
    // tiledata name when blank, else the raw string.
    private static string ResolveBuyName(uint serial, ushort graphic, ushort amount, string raw, UOFileManager files, ObjectPropertyLists opl)
    {
        if (serial != 0 && opl.TryGet(serial, out var e) && !string.IsNullOrEmpty(e.Name)) return e.Name;
        if (int.TryParse(raw, out int cliloc))
            return files.Clilocs.Translate(cliloc, $"\t{TileName(files, graphic)}: \t{amount}", true);
        if (string.IsNullOrEmpty(raw)) return TileName(files, graphic);
        return raw;
    }

    // Sell name (legacy PacketHandlers.SellList): numeric raw is a cliloc; blank
    // falls back to OPL then tiledata; otherwise the raw string.
    private static string ResolveSellName(uint serial, ushort graphic, string raw, UOFileManager files, ObjectPropertyLists opl)
    {
        if (int.TryParse(raw, out int cliloc)) return files.Clilocs.GetString(cliloc);
        if (string.IsNullOrEmpty(raw))
        {
            if (serial != 0 && opl.TryGet(serial, out var e) && !string.IsNullOrEmpty(e.Name)) return e.Name;
            return TileName(files, graphic);
        }
        return raw;
    }

    private static string TileName(UOFileManager files, ushort graphic)
    {
        var name = files.TileData.StaticData[graphic].Name;
        return string.IsNullOrEmpty(name) ? "Item" : name;
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

    // Order a shop container's item entities the way legacy BuyList/OpenContainer
    // do: graphic 0x2AF8 sorts by slot X ascending; every other container is read
    // in reverse insertion order. This must match the 0x74 entry order so prices
    // land on the right items.
    private static List<ulong> OrderShopChildren(
        ulong containerEnt, ushort containerGraphic,
        Query<Data<TinyEcs.Children>> childrenQ,
        Query<Data<ContainerSlotPosition>> slotQ)
    {
        var result = new List<ulong>();
        if (!childrenQ.Contains(containerEnt)) return result;
        var (_, kids) = childrenQ.Get(containerEnt);
        foreach (var c in kids.Ref) result.Add(c);

        if (containerGraphic == SortGraphic)
        {
            result.Sort((a, b) => ReadSlotX(slotQ, a) - ReadSlotX(slotQ, b));
        }
        else
        {
            result.Reverse();
        }
        return result;
    }

    private static void OpenWindow(
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> z,
        EventReader<VendorOpenedEvent> reader,
        Query<Data<VendorWindow>> existingQ)
    {
        foreach (var ev in reader.Read())
        {
            bool focused = false;
            foreach (var (ent, w) in existingQ)
            {
                if (w.Ref.Vendor != ev.Vendor) continue;
                // Re-open of the same kind just focuses + rebuilds; a different
                // kind (buy vs sell) replaces it.
                if (w.Ref.IsBuy == ev.IsBuy)
                {
                    commands.Entity(ent.Ref).Insert(new GlobalZIndex(z.Value.Bump()));
                    w.Ref.Dirty = true;
                    focused = true;
                }
            }
            if (focused) continue;
            BuildWindow(commands, builder.Value, assets.Value, z.Value, ev.Vendor, ev.IsBuy);
        }
    }

    private static void BuildWindow(Commands commands, GumpBuilder builder, AssetsServer assets, UiZCounter z, uint vendor, bool isBuy)
    {
        ushort gLeft = isBuy ? BuyLeft : SellLeft;
        ushort gRight = isBuy ? BuyRight : SellRight;
        int leftW = GumpW(assets, gLeft), leftH = GumpH(assets, gLeft);
        int rightW = GumpW(assets, gRight), rightH = GumpH(assets, gRight);
        int naturalMid = leftH - (TopH + LeftBotH);
        int naturalMidR = rightH - (TopH + RightBotH);
        float bodyH = naturalMid;
        float rightMidExtra = naturalMidR - naturalMid; // keep right panel diff like legacy
        int rightX = leftW - RightOffset;
        int rightY = leftH / 2 - RightOffset;

        var pos = new Vector2(150, 60);
        int totalW = rightX + rightW;
        int totalH = (int)(rightY + TopH + naturalMidR + RightBotH) + 8;

        // Root: invisible full-bounds drag/close surface (UIMovable window).
        var root = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                Width = Val.Px(totalW), Height = Val.Px(totalH),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None)
            .Insert<UIMovable>()
            .Insert(new GlobalZIndex(z.Bump()));
        ulong rootId = root.Id;

        // LEFT panel slices.
        var leftTop = builder.AddGumpSlice(commands, gLeft, Vector3.UnitZ, new Vector2(0, 0), new Vector2(leftW, TopH), new Rectangle(0, 0, leftW, TopH), false);
        commands.AddChild(rootId, leftTop.Id);
        var leftMid = builder.AddGumpSlice(commands, gLeft, Vector3.UnitZ, new Vector2(0, TopH), new Vector2(leftW, bodyH), new Rectangle(0, TopH, leftW, naturalMid), true);
        commands.AddChild(rootId, leftMid.Id);
        var leftBottom = builder.AddGumpSlice(commands, gLeft, Vector3.UnitZ, new Vector2(0, TopH + bodyH), new Vector2(leftW, LeftBotH), new Rectangle(0, TopH + naturalMid, leftW, LeftBotH), false);
        commands.AddChild(rootId, leftBottom.Id);

        // RIGHT panel slices.
        float rightMidH = naturalMidR;
        var rightTop = builder.AddGumpSlice(commands, gRight, Vector3.UnitZ, new Vector2(rightX, rightY), new Vector2(rightW, TopH), new Rectangle(0, 0, rightW, TopH), false);
        commands.AddChild(rootId, rightTop.Id);
        var rightMid = builder.AddGumpSlice(commands, gRight, Vector3.UnitZ, new Vector2(rightX, rightY + TopH), new Vector2(rightW, rightMidH), new Rectangle(0, TopH, rightW, naturalMidR), true);
        commands.AddChild(rootId, rightMid.Id);
        var rightBottom = builder.AddGumpSlice(commands, gRight, Vector3.UnitZ, new Vector2(rightX, rightY + TopH + rightMidH), new Vector2(rightW, RightBotH), new Rectangle(0, TopH + naturalMidR, rightW, RightBotH), false);
        commands.AddChild(rootId, rightBottom.Id);

        float rightBottomY = rightY + TopH + rightMidH; // rightBottom slice Top

        // Shop list (left, scrollable flex column). Coords mirror legacy
        // _shopScrollArea (RIGHT_OFFSET, leftMiddle.Y, artW-2*OFF+5).
        int shopX = RightOffset, shopY = TopH;
        int shopW = leftW - RightOffset * 2 + 5;
        var shopList = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex, FlexDirection = FlexDirection.Column,
                PositionType = PositionType.Absolute, Left = Val.Px(shopX), Top = Val.Px(shopY),
                Width = Val.Px(shopW), Height = Val.Px(bodyH), Overflow = Overflow.Scroll,
            })
            .Insert(new ScrollPosition());
        commands.AddChild(rootId, shopList.Id);

        // Transaction list (right) — legacy _transactionScrollArea.
        int txnX = rightX + RightOffset / 2, txnY = rightY + TopH;
        int txnW = rightW - RightOffset * 2 + RightOffset / 2 + 5;
        var txnList = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex, FlexDirection = FlexDirection.Column,
                PositionType = PositionType.Absolute, Left = Val.Px(txnX), Top = Val.Px(txnY),
                Width = Val.Px(txnW), Height = Val.Px(rightMidH), Overflow = Overflow.Scroll,
            })
            .Insert(new ScrollPosition());
        commands.AddChild(rootId, txnList.Id);

        // Scroll arrows over the baked up/down arrow art (legacy leftUp/leftDown
        // /rightUp/rightDown hitboxes). 18x16 like legacy.
        AddScrollArrow(commands, rootId, shopList.Id, -1, leftW - 50, TopH - 18);
        var leftDown = AddScrollArrow(commands, rootId, shopList.Id, +1, leftW - 50, TopH + (int)bodyH);
        AddScrollArrow(commands, rootId, txnList.Id, -1, rightX + rightW - 50, rightY + TopH - 18);
        var rightDown = AddScrollArrow(commands, rootId, txnList.Id, +1, rightX + rightW - 50, (int)rightBottomY);

        // Total + gold labels (legacy positions). Buy: total at rightX+68; sell:
        // total right-aligned at (rightX+rightW)-96. Gold (buy only) +120 right.
        int totalX = isBuy ? rightX + 68 : (rightX + rightW) - RightOffset * 3;
        int totalY = (int)rightBottomY + RightBotH - RightOffset * 3 + 15;
        var total = AddText(commands, rootId, "0", 1, totalX, totalY, 0x0386);
        ulong goldId = 0;
        if (isBuy)
        {
            var gold = AddText(commands, rootId, "0", 1, totalX + 120, totalY, 0x0386);
            goldId = gold.Id;
        }

        // Accept / Clear hit surfaces over the baked buttons (legacy coords).
        int btnY = (int)rightBottomY + RightBotH - 50;
        var accept = SpawnHit(commands, rightX + RightOffset, btnY, 34, 30);
        accept.Insert(new VendorActionButton { Window = rootId, Action = 0 });
        commands.AddChild(rootId, accept.Id);

        var clear = SpawnHit(commands, rightX + RightOffset + 175, btnY, 20, 20);
        clear.Insert(new VendorActionButton { Window = rootId, Action = 1 });
        commands.AddChild(rootId, clear.Id);

        // Resize expander button (0x082E), bottom-center of the left panel
        // (legacy _expander at artW/2-10, leftBottom.Y+leftBottom.Height-5).
        int handleY = TopH + (int)bodyH + LeftBotH - 5;
        var handle = builder.AddGump(commands, 0x082E, Vector3.UnitZ, new Vector2(leftW / 2 - 10, handleY))
            .Insert(Interaction.None).Insert<UINoWindowDrag>().Insert<UiContainsByBounds>()
            .Insert(new VendorResizeHandle { Window = rootId });
        commands.AddChild(rootId, handle.Id);

        commands.Entity(rootId).Insert(new VendorWindow
        {
            Vendor = vendor,
            IsBuy = isBuy,
            BuiltRevision = -1,
            Dirty = true,
            BodyHeight = bodyH,
            MinBodyHeight = naturalMid,
            ShopList = shopList.Id,
            TxnList = txnList.Id,
            TotalLabel = total.Id,
            GoldLabel = goldId,
            LeftMid = leftMid.Id,
            LeftBottom = leftBottom.Id,
            RightMid = rightMid.Id,
            RightBottom = rightBottom.Id,
            Handle = handle.Id,
            Accept = accept.Id,
            Clear = clear.Id,
            LeftDownArrow = leftDown.Id,
            RightDownArrow = rightDown.Id,
            IsBuyGold = isBuy,
            RightMidExtra = rightMidExtra,
            RightYBase = rightY,
            Items = new Dictionary<uint, VendorEntry>(),
            Txn = new Dictionary<uint, int>(),
        });
    }

    private static void RebuildContent(
        Commands commands,
        Res<VendorStore> store,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UOFileManager> files,
        ResMut<ObjectPropertyLists> opl,
        Query<Data<VendorWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<Graphic>> graphicQ,
        Query<Data<Graphic, Hue, Amount, NetworkSerial>> itemQ,
        Query<Data<ContainerSlotPosition>> slotQ,
        Query<Data<VendorListing>> listingQ,
        Query<Data<PlayerData>> playerQ,
        Query<Data<Text>> textQ,
        Res<NetworkEntitiesMap> map)
    {
        foreach (var (ent, win) in windowsQ)
        {
            bool dirty = win.Ref.Dirty || win.Ref.BuiltRevision != store.Value.Revision;
            if (!dirty) continue;
            win.Ref.Dirty = false;
            win.Ref.BuiltRevision = store.Value.Revision;

            // Gather shop items.
            win.Ref.Items.Clear();
            var items = new List<VendorEntry>();
            if (win.Ref.IsBuy)
                GatherBuyItems(win.Ref.Vendor, items, map, equipQ, graphicQ, itemQ, slotQ, listingQ, childrenQ);
            else if (store.Value.SellByVendor.TryGetValue(win.Ref.Vendor, out var sell))
                items.AddRange(sell);

            // Resolve display names live from the now-current OPL (the real names
            // usually arrive via 0xD6 after the gump opened) with cliloc/tiledata
            // /raw fallbacks, and request OPL for any item we don't have yet so
            // the names fill in (legacy requests mega-clilocs for shop contents).
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                it.Name = win.Ref.IsBuy
                    ? ResolveBuyName(it.Serial, it.Graphic, it.Amount, it.RawName, files.Value, opl.Value)
                    : ResolveSellName(it.Serial, it.Graphic, it.RawName, files.Value, opl.Value);
                items[i] = it;
                if (it.Serial != 0) opl.Value.Request(it.Serial);
            }

            foreach (var it in items) win.Ref.Items[it.Serial] = it;

            // Prune txn entries no longer offered.
            if (win.Ref.Txn.Count > 0)
            {
                var stale = new List<uint>();
                foreach (var kv in win.Ref.Txn) if (!win.Ref.Items.ContainsKey(kv.Key)) stale.Add(kv.Key);
                foreach (var s in stale) win.Ref.Txn.Remove(s);
            }

            ClearChildren(commands, win.Ref.ShopList, childrenQ);
            ClearChildren(commands, win.Ref.TxnList, childrenQ);

            var rootId = ent.Ref;
            int shopW = ShopListWidth(win.Ref);
            foreach (var it in items)
            {
                int chosen = win.Ref.Txn.TryGetValue(it.Serial, out var c) ? c : 0;
                int avail = it.Amount - chosen;
                BuildShopRow(commands, builder.Value, assets.Value, win.Ref.ShopList, rootId, it, avail, shopW);
            }

            foreach (var kv in win.Ref.Txn)
            {
                if (!win.Ref.Items.TryGetValue(kv.Key, out var it)) continue;
                BuildTxnRow(commands, builder.Value, win.Ref.TxnList, rootId, it, kv.Value);
            }

            // Total + gold.
            long sum = 0;
            foreach (var kv in win.Ref.Txn)
                if (win.Ref.Items.TryGetValue(kv.Key, out var it)) sum += (long)it.Price * kv.Value;
            SetText(commands, win.Ref.TotalLabel, sum.ToString(), textQ);
            if (win.Ref.GoldLabel != 0)
            {
                uint gold = 0;
                foreach (var (_, pd) in playerQ) { gold = pd.Ref.Gold; break; }
                SetText(commands, win.Ref.GoldLabel, gold.ToString(), textQ);
            }
        }
    }

    private static void GatherBuyItems(
        uint vendor, List<VendorEntry> outItems,
        Res<NetworkEntitiesMap> map,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<Graphic>> graphicQ,
        Query<Data<Graphic, Hue, Amount, NetworkSerial>> itemQ,
        Query<Data<ContainerSlotPosition>> slotQ,
        Query<Data<VendorListing>> listingQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (!map.Value.TryGet(vendor, out var vendorEnt) || !equipQ.Contains(vendorEnt)) return;
        var (_, slots) = equipQ.Get(vendorEnt);
        Span<Layer> shopLayers = stackalloc Layer[] { Layer.ShopBuyRestock, Layer.ShopBuy };
        foreach (var layer in shopLayers)
        {
            var containerEnt = slots.Ref[layer];
            if (containerEnt == 0) continue;
            var ordered = OrderShopChildren(containerEnt, ReadGraphic(graphicQ, containerEnt), childrenQ, slotQ);
            foreach (var child in ordered)
            {
                if (!itemQ.Contains(child)) continue;
                var (_, g, h, a, sn) = itemQ.Get(child);
                var entry = new VendorEntry
                {
                    Serial = sn.Ref.Value,
                    Graphic = g.Ref.Value,
                    Hue = h.Ref.Value,
                    Amount = (ushort)a.Ref.Value,
                };
                if (listingQ.Contains(child))
                {
                    var (_, l) = listingQ.Get(child);
                    entry.Price = l.Ref.Price;
                    entry.RawName = l.Ref.RawName;
                }
                outItems.Add(entry);
            }
        }
    }

    private static ushort ReadGraphic(Query<Data<Graphic>> q, ulong e)
    { if (e == 0 || !q.Contains(e)) return 0; var (_, g) = q.Get(e); return g.Ref.Value; }
    private static int ReadSlotX(Query<Data<ContainerSlotPosition>> q, ulong e)
    { if (e == 0 || !q.Contains(e)) return 0; var (_, s) = q.Get(e); return s.Ref.X; }

    private static int ShopListWidth(in VendorWindow w) => 180;

    private const ushort DividerGraphic = 0x0039; // ResizePicLine: 0x39 left, 0x3A mid (tiled), 0x3B right

    // One shop row, matching legacy ShopItem: a full-width divider line, then a
    // flowing [icon | wrapped name | Avail] block. The row auto-sizes to the
    // wrapped name height so the scroll content height is correct.
    private static void BuildShopRow(Commands commands, GumpBuilder builder, AssetsServer assets, ulong list, ulong rootId, VendorEntry it, int avail, int width)
    {
        uint serial = it.Serial;
        var row = commands.Spawn()
            .Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Val.Px(width), Height = Val.Auto, Gap = Val.Px(2) })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None).Insert<UINoWindowDrag>().Insert<UiContainsByBounds>()
            .Insert(new VendorShopRow { Window = rootId, Serial = serial });
        commands.AddChild(list, row.Id);

        // Divider: left cap (0x39) + tiled middle (0x3A) + right cap (0x3B).
        int capL = GumpW(assets, DividerGraphic), capR = GumpW(assets, (ushort)(DividerGraphic + 2));
        int lineH = GumpH(assets, DividerGraphic);
        var div = commands.Spawn()
            .Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Row, Width = Val.Px(width), Height = Val.Px(lineH) });
        commands.AddChild(row.Id, div.Id);
        commands.AddChild(div.Id, builder.AddGump(commands, DividerGraphic, Vector3.UnitZ).Id);
        var mid = commands.Spawn()
            .Insert(new Node { Width = Val.Px(Math.Max(0, width - capL - capR)), Height = Val.Px(lineH) })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.GumpTiled, AssetId = (ushort)(DividerGraphic + 1), Hue = Vector3.UnitZ } });
        commands.AddChild(div.Id, mid.Id);
        commands.AddChild(div.Id, builder.AddGump(commands, (ushort)(DividerGraphic + 2), Vector3.UnitZ).Id);

        // Content row: icon | wrapped name | avail.
        var inner = commands.Spawn()
            .Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Start, Gap = Val.Px(4), Width = Val.Px(width), Height = Val.Auto });
        commands.AddChild(row.Id, inner.Id);

        var icon = commands.Spawn()
            .Insert(new Node { Width = Val.Px(44), Height = Val.Px(44) })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Art, AssetId = it.Graphic, Hue = ShaderHueTranslator.GetHueVector(it.Hue, partial: false, alpha: 1f) } })
            .Insert(Interaction.None).Insert<UINoWindowDrag>().Insert<UiContainsByBounds>()
            .Insert(new VendorShopRow { Window = rootId, Serial = serial });
        commands.AddChild(inner.Id, icon.Id);

        // Wrapped name + price in the unicode font (legacy ShopItem uses
        // isunicode font 1, maxwidth 110), sized to its measured height.
        int nameW = Math.Min(110, Math.Max(40, width - 44 - 4 - 30));
        string text = $"{it.Name} at {it.Price}gp";
        var (_, th) = UoFontRenderer.Measure(text, font: 1, nameW, isHtml: false, 0, htmlBg: false);
        var name = commands.Spawn()
            .Insert(new Node { Width = Val.Px(nameW), Height = Val.Px(Math.Max(th, 14)) })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.WrappedText, Hue = Vector3.UnitZ, Text = text, TextFont = 1, TextHue = 0x0219, WrapWidth = nameW, IsHtml = false } });
        commands.AddChild(inner.Id, name.Id);

        var amt = commands.Spawn()
            .Insert(new Node { Width = Val.Px(28), Height = Val.Auto })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.WrappedText, Hue = Vector3.UnitZ, Text = avail.ToString(), TextFont = 1, TextHue = 0x0219, WrapWidth = 28, IsHtml = false } });
        commands.AddChild(inner.Id, amt.Id);
    }

    private static void BuildTxnRow(Commands commands, GumpBuilder builder, ulong list, ulong rootId, VendorEntry it, int amount)
    {
        const int RowH = 22;
        // Legacy TransactionItem: 220-wide row, children at fixed X (amount 10,
        // name 50, +button 190, -button 210). Flex flowed them all hard-left and
        // buried the +/- under the name — absolute positions restore parity.
        var row = commands.Spawn()
            .Insert(new Node { Width = Val.Px(220), Height = Val.Px(RowH) })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } });
        commands.AddChild(list, row.Id);

        AddText(commands, row.Id, amount.ToString(), 9, 10, 4, TextHue);
        AddText(commands, row.Id, it.Name, 9, 50, 4, TextHue);

        uint serial = it.Serial;
        var plus = builder.AddGump(commands, 0x37, Vector3.UnitZ, new Vector2(190, 5))
            .Insert(Interaction.None).Insert<UINoWindowDrag>().Insert<UiContainsByBounds>()
            .Insert(new VendorTxnButton { Window = rootId, Serial = serial, Dir = +1 });
        commands.AddChild(row.Id, plus.Id);

        var minus = builder.AddGump(commands, 0x38, Vector3.UnitZ, new Vector2(210, 5))
            .Insert(Interaction.None).Insert<UINoWindowDrag>().Insert<UiContainsByBounds>()
            .Insert(new VendorTxnButton { Window = rootId, Serial = serial, Dir = -1 });
        commands.AddChild(row.Id, minus.Id);
    }

    // Single bbox-poll for every clickable vendor surface (PreUpdate, claims the
    // DragGate so a press doesn't latch a window drag). Mirrors SpellbookNav.
    private static void VendorClicks(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<KeyboardContext> kb,
        Res<NetClient> net,
        Res<GameContext> ctx,
        Res<Time> time,
        Commands commands,
        Local<bool> claimed,
        Local<TxnRepeat> repeat,
        Query<Data<ComputedNode, VendorActionButton>> actQ,
        Query<Data<ComputedNode, VendorTxnButton>> txnQ,
        Query<Data<ComputedNode, VendorShopRow>> rowQ,
        Query<Data<VendorWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (claimed.Value && gate.Value.Mode == ActiveDrag.UIWindow) gate.Value.Mode = ActiveDrag.None;
            claimed.Value = false;
            repeat.Value = default;
            return;
        }

        var p = mouse.Value.Position;

        // Auto-repeat: while a +/- is held and the cursor stays over it, fire on an
        // accelerating cadence after a 500ms initial delay (legacy TransactionItem
        // MouseDown→MouseOver: t0 = +500, then every (45 - stepChanger) ms, with
        // stepChanger += 2 each 3 fires, capped at 45). Shift re-read each fire.
        if (repeat.Value.Active && !mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            bool over = false;
            foreach (var (_, bb, b) in txnQ)
            {
                if (b.Ref.Window != repeat.Value.Window || b.Ref.Serial != repeat.Value.Serial || b.Ref.Dir != repeat.Value.Dir) continue;
                if (Contains(bb.Ref, p)) over = true;
                break;
            }
            if (over && time.Value.Total > repeat.Value.NextTime)
            {
                AdjustTxn(repeat.Value.Window, repeat.Value.Serial, repeat.Value.Dir, ShiftDown(kb.Value), windowsQ);
                repeat.Value.NextTime = time.Value.Total + (45 - repeat.Value.StepChanger);
                repeat.Value.StepsDone++;
                if (repeat.Value.StepChanger < 45 && repeat.Value.StepsDone % 3 == 0) repeat.Value.StepChanger += 2;
            }
            return;
        }

        if (!mouse.Value.IsPressedOnce(MouseButtonType.Left)) return;
        // Allow our own prior-frame claim through (a double-click's first press
        // claims the gate; the second press must still register on the row).
        if (gate.Value.Mode != ActiveDrag.None && !claimed.Value) return;

        bool dbl = mouse.Value.IsPressedDouble(MouseButtonType.Left);
        bool shift = ShiftDown(kb.Value);

        foreach (var (_, bb, a) in actQ)
        {
            if (!Contains(bb.Ref, p)) continue;
            if (a.Ref.Action == 0) DoAccept(a.Ref.Window, windowsQ, net.Value, ctx.Value, commands, childrenQ);
            else ClearTxn(a.Ref.Window, windowsQ);
            gate.Value.Mode = ActiveDrag.UIWindow; claimed.Value = true; return;
        }
        foreach (var (_, bb, b) in txnQ)
        {
            if (!Contains(bb.Ref, p)) continue;
            AdjustTxn(b.Ref.Window, b.Ref.Serial, b.Ref.Dir, shift, windowsQ);
            repeat.Value = new TxnRepeat
            {
                Active = true, Window = b.Ref.Window, Serial = b.Ref.Serial, Dir = b.Ref.Dir,
                NextTime = time.Value.Total + 500, StepChanger = 0, StepsDone = 0,
            };
            gate.Value.Mode = ActiveDrag.UIWindow; claimed.Value = true; return;
        }
        foreach (var (_, bb, r) in rowQ)
        {
            if (!Contains(bb.Ref, p)) continue;
            if (dbl) AdjustTxn(r.Ref.Window, r.Ref.Serial, +1, shift, windowsQ);
            gate.Value.Mode = ActiveDrag.UIWindow; claimed.Value = true; return;
        }
    }

    private struct TxnRepeat
    {
        public bool Active;
        public ulong Window;
        public uint Serial;
        public int Dir;
        public float NextTime, StepChanger, StepsDone;
    }

    // Hold an up/down arrow to scroll its list (legacy ScrollArea hitboxes).
    private static void VendorScroll(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<bool> claimed,
        Query<Data<ComputedNode, VendorScrollButton>> arrowsQ,
        Query<Data<ScrollPosition>> scrollQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (claimed.Value && gate.Value.Mode == ActiveDrag.UIWindow) gate.Value.Mode = ActiveDrag.None;
            claimed.Value = false;
            return;
        }
        // Latch only on press-once (then keep scrolling while held).
        if (mouse.Value.IsPressedOnce(MouseButtonType.Left) && gate.Value.Mode != ActiveDrag.None && !claimed.Value)
            return;

        var p = mouse.Value.Position;
        foreach (var (_, bb, a) in arrowsQ)
        {
            if (!Contains(bb.Ref, p)) continue;
            if (scrollQ.Contains(a.Ref.List))
            {
                var (_, sp) = scrollQ.Get(a.Ref.List);
                sp.Ref.OffsetY = Math.Max(0f, sp.Ref.OffsetY + a.Ref.Dir * 6f);
            }
            gate.Value.Mode = ActiveDrag.UIWindow;
            claimed.Value = true;
            return;
        }
    }

    private static void ClearTxn(ulong rootId, Query<Data<VendorWindow>> wq)
    {
        if (!wq.Contains(rootId)) return;
        var (_, w) = wq.Get(rootId);
        if (w.Ref.Txn != null && w.Ref.Txn.Count > 0) { w.Ref.Txn.Clear(); w.Ref.Dirty = true; }
    }

    private static void AdjustTxn(ulong rootId, uint serial, int dir, bool shift, Query<Data<VendorWindow>> wq)
    {
        if (!wq.Contains(rootId)) return;
        var (_, w) = wq.Get(rootId);
        if (!w.Ref.Items.TryGetValue(serial, out var entry)) return;
        int chosen = w.Ref.Txn.TryGetValue(serial, out var cc) ? cc : 0;
        if (dir > 0)
        {
            int remaining = entry.Amount - chosen;
            if (remaining <= 0) return;
            int add = shift ? remaining : 1;
            w.Ref.Txn[serial] = chosen + add;
        }
        else
        {
            int sub = shift ? chosen : 1;
            int next = chosen - sub;
            if (next <= 0) w.Ref.Txn.Remove(serial);
            else w.Ref.Txn[serial] = next;
        }
        w.Ref.Dirty = true;
    }

    private static void DoAccept(ulong rootId, Query<Data<VendorWindow>> wq, NetClient net, GameContext ctx, Commands commands, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (!wq.Contains(rootId)) return;
        var (ent, w) = wq.Get(rootId);
        if (w.Ref.Txn.Count > 0)
        {
            var items = new (uint serial, ushort amount)[w.Ref.Txn.Count];
            int i = 0;
            foreach (var kv in w.Ref.Txn) items[i++] = (kv.Key, (ushort)kv.Value);
            if (w.Ref.IsBuy) net.Send_BuyRequest(w.Ref.Vendor, items);
            else net.Send_SellRequest(w.Ref.Vendor, items);
        }
        DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    // Bottom-center handle drag adjusts BodyHeight; rebuild repositions panels.
    private static void ResizeDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<ResizeAnchor> anchor,
        Query<Data<ComputedNode, VendorResizeHandle>> handlesQ,
        Query<Data<VendorWindow>> windowsQ,
        Query<Data<Node>> nodeQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (anchor.Value.Claimed && gate.Value.Mode == ActiveDrag.UIWindow) gate.Value.Mode = ActiveDrag.None;
            anchor.Value = default;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            var p = mouse.Value.Position;
            foreach (var (_, bb, h) in handlesQ)
            {
                if (!Contains(bb.Ref, p)) continue;
                if (!windowsQ.Contains(h.Ref.Window)) continue;
                var (_, w) = windowsQ.Get(h.Ref.Window);
                anchor.Value.Active = true; anchor.Value.Claimed = true;
                anchor.Value.Window = h.Ref.Window;
                anchor.Value.StartY = p.Y;
                anchor.Value.InitialBody = w.Ref.BodyHeight;
                gate.Value.Mode = ActiveDrag.UIWindow;
                break;
            }
        }

        if (anchor.Value.Active && windowsQ.Contains(anchor.Value.Window))
        {
            var (_, w) = windowsQ.Get(anchor.Value.Window);
            float steps = mouse.Value.Position.Y - anchor.Value.StartY;
            float nb = Math.Clamp(anchor.Value.InitialBody + steps, w.Ref.MinBodyHeight, 640f);
            if (Math.Abs(nb - w.Ref.BodyHeight) >= 1f)
            {
                w.Ref.BodyHeight = nb;
                RepositionPanels(w.Ref, nodeQ);
                w.Ref.Dirty = true;
            }
        }
    }

    // Move the resizable panels + scroll lists to match BodyHeight (legacy Update).
    private static void RepositionPanels(in VendorWindow w, Query<Data<Node>> nodeQ)
    {
        float body = w.BodyHeight;
        SetNodeHeight(nodeQ, w.LeftMid, body);
        SetNodeTop(nodeQ, w.LeftBottom, TopH + body);
        SetNodeHeight(nodeQ, w.ShopList, body);
        float rightMid = body + w.RightMidExtra;
        SetNodeHeight(nodeQ, w.RightMid, rightMid);
        SetNodeHeight(nodeQ, w.TxnList, rightMid);

        // Bottom-anchored controls follow the panel bottoms (legacy Update()).
        float rightBottomY = w.RightYBase + TopH + rightMid;
        SetNodeTop(nodeQ, w.RightBottom, rightBottomY);
        SetNodeTop(nodeQ, w.LeftDownArrow, TopH + body);
        SetNodeTop(nodeQ, w.RightDownArrow, rightBottomY);
        SetNodeTop(nodeQ, w.Accept, rightBottomY + RightBotH - 50);
        SetNodeTop(nodeQ, w.Clear, rightBottomY + RightBotH - 50);
        float totalY = rightBottomY + RightBotH - RightOffset * 3 + 15;
        SetNodeTop(nodeQ, w.TotalLabel, totalY);
        SetNodeTop(nodeQ, w.GoldLabel, totalY);
        SetNodeTop(nodeQ, w.Handle, TopH + body + LeftBotH - 5);
    }

    private static void SetNodeHeight(Query<Data<Node>> q, ulong e, float h)
    {
        if (e == 0 || !q.Contains(e)) return;
        var (_, n) = q.Get(e); n.Ref.Height = Val.Px(h);
    }
    private static void SetNodeTop(Query<Data<Node>> q, ulong e, float t)
    {
        if (e == 0 || !q.Contains(e)) return;
        var (_, n) = q.Get(e); n.Ref.Top = Val.Px(t);
    }

    private struct ResizeAnchor { public bool Active, Claimed; public ulong Window; public float StartY, InitialBody; }

    private static bool Contains(in ComputedNode bb, Vector2 p)
        => p.X >= bb.Position.X && p.Y >= bb.Position.Y && p.X < bb.Position.X + bb.Size.X && p.Y < bb.Position.Y + bb.Size.Y;

    private static bool ShiftDown(KeyboardContext kb)
        => kb.IsPressed(Keys.LeftShift) || kb.IsPressed(Keys.RightShift)
        || kb.IsPressedOnce(Keys.LeftShift) || kb.IsPressedOnce(Keys.RightShift);

    private static EntityCommands AddScrollArrow(Commands commands, ulong rootId, ulong list, int dir, int x, int y)
    {
        var a = SpawnHit(commands, x, y, 18, 16).Insert(new VendorScrollButton { List = list, Dir = dir });
        commands.AddChild(rootId, a.Id);
        return a;
    }

    private static EntityCommands SpawnHit(Commands commands, int x, int y, int w, int h)
        => commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(x), Top = Val.Px(y), Width = Val.Px(w), Height = Val.Px(h) })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None).Insert<UINoWindowDrag>().Insert<UiContainsByBounds>();

    private static EntityCommands AddText(Commands commands, ulong parent, string s, int font, int x, int y, ushort hue)
    {
        var t = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(x), Top = Val.Px(y), Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(s))
            .Insert(new TextFont { FontId = (ushort)(font | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(hue)));
        commands.AddChild(parent, t.Id);
        return t;
    }

    private static EntityCommands AddFlowText(Commands commands, string s, int font, ushort hue)
        => commands.Spawn()
            .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(s))
            .Insert(new TextFont { FontId = (ushort)(font | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(hue)));

    private static void SetText(Commands commands, ulong e, string s, Query<Data<Text>> textQ)
    {
        if (e == 0 || !textQ.Contains(e)) return;
        var (_, t) = textQ.Get(e);
        t.Ref.Value = s;
    }

    private static void ClearChildren(Commands commands, ulong parent, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (parent == 0 || !childrenQ.Contains(parent)) return;
        var (_, kids) = childrenQ.Get(parent);
        foreach (var cid in kids.Ref) DespawnSubtree(commands, cid, childrenQ);
    }

    private static void DespawnAll(
        Commands commands,
        Query<Data<VendorWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ) DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong e, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.Contains(e))
        {
            var (_, kids) = childrenQ.Get(e);
            foreach (var cid in kids.Ref) DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(e).Despawn();
    }
}
