// Bevy.UI port of the legacy Game/UI/Gumps/TradingGump.cs.
//
// Secure trade (packet 0x6F): two item containers (Id1 = mine, Id2 = his), an
// accept toggle for each side and — on >= CV_704565 — gold/platinum display
// labels plus my-side gold/platinum entry fields. The window is opened by the
// 0x6F type-0 observer, torn down by type-1 (or right-click), and its accept /
// gold state is driven by types 2/3/4.
//
// Items inside the trade containers arrive through the SAME ContainerSlotEvent
// stream the normal container UI consumes (the trade boxes are real server
// containers). This plugin filters that stream to Id1/Id2 and renders the item
// sprites itself into the two box regions — it does NOT register the boxes as
// ContainerWindows, so pickup/drop reuse is intentionally out of scope here.
//
// Layout structure mirrors ContainerGumpPlugin: an Absolute UOGump root + a
// single Relative content child filling it; every label / button / item is an
// Absolute child of that content layer (Floating-attached to a non-floating
// parent), which is the layering pattern that renders correctly over the gump
// background.

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Window root marker. Carries the trade serial + container ids and the live
// accept flags. On despawn (right-click close OR server type-1) the OnRemove
// observer sends Send_TradeResponse(Id1, 1, false) — legacy Dispose() →
// CancelTrade.
// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct TradeWindow
{
    public uint Serial;
    public uint Id1;
    public uint Id2;
    public bool ImAccepting;
    public bool HeIsAccepting;
    public bool IsNew;
}

// My accept toggle sprite. Click flips ImAccepting + sends AcceptTrade.
internal struct TradeAcceptButton { public uint Serial; }
// His (read-only) accept indicator sprite.
internal struct TradeHisAcceptPic { public uint Serial; }

// An item sprite inside a trade box.
internal struct TradeItemUI
{
    public uint TradeSerial;
    public uint ItemSerial;
    public Vector3 OriginalHue;
    public Vector3 HoverHue;
}

// My-side gold/platinum entry glyph (>= CV_704565). The send system reads its
// Text, parses, clamps to the server-known max and sends Send_TradeUpdateGold.
internal struct TradeGoldEntry
{
    public uint TradeSerial;
    public uint Id1;
    public bool IsPlatinum;
}

internal sealed class TradeUiMap
{
    public sealed class Entry
    {
        public ulong UiEntity;
        public ulong Content;
        public uint Id1, Id2;
        public bool IsNew;
        public Vector2 MyBox;   // top-left of my item box, content-local
        public Vector2 HisBox;
        public ulong MyAccept, HisAccept;
        // Coin display labels (null on the old layout).
        public UOCustomRender MyGoldLabel, MyPlatLabel, HisGoldLabel, HisPlatLabel;
        // Server-known max I can offer (from type-4) — entry fields clamp to it.
        public uint Gold, Platinum;
        // Last values actually sent so the poll loop only sends on change.
        public uint LastGoldSent, LastPlatSent;
    }

    private readonly Dictionary<uint, Entry> _byTrade = new();

    public IReadOnlyCollection<Entry> Entries => _byTrade.Values;
    public void Set(uint serial, Entry e) => _byTrade[serial] = e;
    public bool TryGet(uint serial, out Entry e) => _byTrade.TryGetValue(serial, out e);
    public bool Has(uint serial) => _byTrade.ContainsKey(serial);
    public void Remove(uint serial) => _byTrade.Remove(serial);
    public void Clear() => _byTrade.Clear();

    // Find the trade whose Id1 (mine) or Id2 (his) matches a container serial.
    public bool TryGetByContainer(uint container, out Entry e, out bool mine)
    {
        foreach (var entry in _byTrade.Values)
        {
            if (entry.Id1 == container) { e = entry; mine = true; return true; }
            if (entry.Id2 == container) { e = entry; mine = false; return true; }
        }
        e = null; mine = false; return false;
    }
}

internal readonly struct TradingGumpPlugin : IPlugin
{
    // Trade box size (legacy DataBox 110x80).
    private const int BoxW = 110, BoxH = 80;

    private const ushort BgNew = 0x088A, BgOld = 0x0866;
    private const ushort AcceptOff = 0x0867, AcceptOn = 0x0869;
    private const ushort NameHueNew = 0x0481, NameHueOld = 0x0386;
    private const ushort CoinHue = 0x0481;

    public void Build(App app)
    {
        app.AddResource(new TradeUiMap());

        // Open (type 0).
        app.AddObserver((
            On<PacketReceived<OnSecureTradePacket_0x6F>> trig,
            Commands commands,
            Res<NetworkEntitiesMap> entitiesMap,
            Res<GumpBuilder> builder,
            Res<AssetsServer> assets,
            Res<UiZCounter> zCounter,
            Res<GameContext> gameCtx,
            ResMut<TradeUiMap> uiMap) =>
        {
            var p = trig.Event.Packet;
            if (p.Type != 0)
                return;

            // Standard client refuses a trade where either trader is invisible
            // (not sent by the server). Mirror that guard.
            if (!entitiesMap.Value.TryGet(p.Id1, out _) || !entitiesMap.Value.TryGet(p.Id2, out _))
                return;

            if (uiMap.Value.Has(p.Serial))
                return;

            SpawnTradeGump(commands, builder.Value, assets.Value, zCounter.Value, gameCtx.Value, uiMap.Value,
                p.Serial, p.Id1, p.Id2, p.Name ?? string.Empty);
        });

        // Close (type 1), accept (type 2), his gold (type 3), my gold (type 4).
        app.AddObserver((
            On<PacketReceived<OnSecureTradePacket_0x6F>> trig,
            Commands commands,
            ResMut<TradeUiMap> uiMap,
            Query<Data<TradeWindow>> windowQ,
            Query<Data<UiCustom>> customQ) =>
        {
            var p = trig.Event.Packet;
            if (!uiMap.Value.TryGet(p.Serial, out var entry))
                return;

            switch (p.Type)
            {
                case 1:
                    commands.Entity(entry.UiEntity).Despawn();
                    uiMap.Value.Remove(p.Serial);
                    break;

                case 2:
                    ApplyAcceptState(commands, entry, windowQ, customQ, p.Id1 != 0, p.Id2 != 0);
                    break;

                case 3:
                    if (entry.HisGoldLabel != null) entry.HisGoldLabel.Text = p.Gold.ToString("N0");
                    if (entry.HisPlatLabel != null) entry.HisPlatLabel.Text = p.Platinum.ToString("N0");
                    break;

                case 4:
                    entry.Gold = p.Gold;
                    entry.Platinum = p.Platinum;
                    if (entry.MyGoldLabel != null) entry.MyGoldLabel.Text = p.Gold.ToString("N0");
                    if (entry.MyPlatLabel != null) entry.MyPlatLabel.Text = p.Platinum.ToString("N0");
                    break;
            }
        });

        // Item add/remove inside the trade boxes (filtered ContainerSlotEvent).
        var itemsFn = UpdateTradeItems;
        app.AddSystem(itemsFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<ContainerSlotEvent> r) => r.HasEvents)
            .Build();

        // Hover hue on the topmost trade item under the cursor.
        var hoverFn = HoverItems;
        app.AddSystem(hoverFn).InStage(Stage.Last).Build();

        // My-side gold/platinum entry → Send_TradeUpdateGold on change.
        var goldFn = SendGoldChanges;
        app.AddSystem(goldFn).InStage(Stage.Update).Build();

        // Closing the window (right-click via WindowDragPlugin, or server type-1)
        // sends a cancel — legacy TradingGump.Dispose → GameActions.CancelTrade.
        app.AddObserver((
            OnRemove<TradeWindow> trig,
            Res<NetClient> net,
            ResMut<TradeUiMap> uiMap) =>
        {
            net.Value.Send_TradeResponse(trig.Component.Id1, 1, false);
            uiMap.Value.Remove(trig.Component.Serial);
        });

        var despawnFn = DisposeOnLogout;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugTradeQueue());
        var drainFn = DrainDebugTrade;
        app.AddSystem(drainFn).InStage(Stage.First).Build();
#endif
    }

    internal static void SpawnTradeGump(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        UiZCounter zCounter,
        GameContext gameCtx,
        TradeUiMap uiMap,
        uint serial, uint id1, uint id2, string name)
    {
        bool isNew = gameCtx.ClientVersion >= ClientVersion.CV_704565;
        ushort bg = isNew ? BgNew : BgOld;

        var root = builder.SpawnUOGump(commands, bg, Vector3.UnitZ, new Vector2(100, 100), zCounter)
            .Insert(new TradeWindow { Serial = serial, Id1 = id1, Id2 = id2, IsNew = isNew });
        var rootId = root.Id;

        // Children parent directly to the Absolute UOGump root at gump-local
        // (0,0) — identical to PaperdollPlugin. An intermediate Relative content
        // layer (the ContainerGumpPlugin pattern) does NOT fill the root cleanly
        // here: Clay lays it out inset (~8,19) and short, which shifts every
        // label / field off the legacy coordinates. Absolute art/label children
        // of an Absolute root render over the background fine (paperdoll proves
        // it), so no content layer is needed.
        var contentId = rootId;

        var entry = new TradeUiMap.Entry
        {
            UiEntity = rootId,
            Content = contentId,
            Id1 = id1,
            Id2 = id2,
            IsNew = isNew,
        };

        byte nameFont = isNew ? (byte)3 : (byte)1;
        ushort nameHue = isNew ? NameHueNew : NameHueOld;

        if (isNew)
        {
            // My name (73,32); his name right-aligned to x=250 (244).
            AddLabel(commands, contentId, gameCtx.PlayerName ?? string.Empty, new Vector2(73, 32), 180, nameFont, nameHue);
            var hisW = UoFontRenderer.Measure(name, nameFont, 250, false, 0, false, ascii: true).Width;
            AddLabel(commands, contentId, name, new Vector2(250 - hisW, 244), 250, nameFont, nameHue);

            // Coin display labels.
            entry.MyGoldLabel = AddLabel(commands, contentId, "0", new Vector2(43, 67), 120, 9, CoinHue);
            entry.MyPlatLabel = AddLabel(commands, contentId, "0", new Vector2(180, 67), 120, 9, CoinHue);
            entry.HisGoldLabel = AddLabel(commands, contentId, "0", new Vector2(180, 190), 120, 9, CoinHue);
            entry.HisPlatLabel = AddLabel(commands, contentId, "0", new Vector2(180, 210), 120, 9, CoinHue);

            // My gold / platinum entry fields (43,190) / (43,210).
            AddGoldEntry(commands, contentId, new Vector2(43, 190), serial, id1, isPlatinum: false);
            AddGoldEntry(commands, contentId, new Vector2(43, 210), serial, id1, isPlatinum: true);

            entry.MyBox = new Vector2(30, 110);
            entry.HisBox = new Vector2(192, 110);
        }
        else
        {
            AddLabel(commands, contentId, gameCtx.PlayerName ?? string.Empty, new Vector2(84, 40), 250, nameFont, nameHue);
            var hisW = UoFontRenderer.Measure(name, nameFont, 260, false, 0, false, ascii: true).Width;
            AddLabel(commands, contentId, name, new Vector2(260 - hisW, 170), 260, nameFont, nameHue);

            entry.MyBox = new Vector2(45, 70);
            entry.HisBox = new Vector2(192, 70);
        }

        SpawnAcceptControls(commands, builder, contentId, entry, serial, isNew, imAccepting: false, heIsAccepting: false);

        uiMap.Set(serial, entry);
    }

    // (Re)build the my-accept toggle + his-accept indicator for the current
    // accept flags. Mirrors legacy SetCheckboxes (positions differ by version).
    private static void SpawnAcceptControls(
        Commands commands,
        GumpBuilder builder,
        ulong contentId,
        TradeUiMap.Entry entry,
        uint serial,
        bool isNew,
        bool imAccepting,
        bool heIsAccepting)
    {
        if (entry.MyAccept != 0) commands.Entity(entry.MyAccept).Despawn();
        if (entry.HisAccept != 0) commands.Entity(entry.HisAccept).Despawn();

        var (myX, myY, hisX, hisY) = isNew ? (37, 29, 258, 240) : (52, 29, 266, 160);

        var myG = imAccepting ? AcceptOn : AcceptOff;
        var my = builder.AddGump(commands, myG, Vector3.UnitZ, new Vector2(myX, myY))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>()
            .Insert(new TradeAcceptButton { Serial = serial });
        my.Observe((On<UiClick> _,
                    Res<NetClient> net,
                    Commands cmd,
                    ResMut<TradeUiMap> map,
                    Query<Data<TradeWindow>> windowQ,
                    Query<Data<UiCustom>> customQ,
                    Res<GumpBuilder> b) =>
        {
            if (!map.Value.TryGet(serial, out var en) || !windowQ.TryGet(en.UiEntity, out var winRow)) return;
            var (_, win) = winRow;
            bool next = !win.Ref.ImAccepting;
            net.Value.Send_TradeResponse(en.Id1, 2, next);
            // Optimistic local flip (legacy flips ImAccepting before sending);
            // server type-2 reaffirms.
            ApplyAcceptStateInline(cmd, en, win.Ref.IsNew, customQ, next, win.Ref.HeIsAccepting, windowQ);
        });
        commands.AddChild(contentId, my.Id);
        entry.MyAccept = my.Id;

        var hisG = heIsAccepting ? AcceptOn : AcceptOff;
        var his = builder.AddGump(commands, hisG, Vector3.UnitZ, new Vector2(hisX, hisY))
            .Insert(new TradeHisAcceptPic { Serial = serial });
        commands.AddChild(contentId, his.Id);
        entry.HisAccept = his.Id;
    }

    // Apply accept flags from a server type-2: update the window flags + swap the
    // two indicator sprites in place (no despawn — just retint/regraphic).
    private static void ApplyAcceptState(
        Commands commands,
        TradeUiMap.Entry entry,
        Query<Data<TradeWindow>> windowQ,
        Query<Data<UiCustom>> customQ,
        bool im, bool he)
    {
        if (!windowQ.TryGet(entry.UiEntity, out var winRow)) return;
        var (_, win) = winRow;
        ApplyAcceptStateInline(commands, entry, win.Ref.IsNew, customQ, im, he, windowQ);
    }

    private static void ApplyAcceptStateInline(
        Commands commands,
        TradeUiMap.Entry entry,
        bool isNew,
        Query<Data<UiCustom>> customQ,
        bool im, bool he,
        Query<Data<TradeWindow>> windowQ)
    {
        if (windowQ.TryGet(entry.UiEntity, out var winRow))
        {
            var (_, win) = winRow;
            win.Ref.ImAccepting = im;
            win.Ref.HeIsAccepting = he;
        }

        if (entry.MyAccept != 0 && customQ.TryGet(entry.MyAccept, out var myRow))
        {
            var (_, c) = myRow;
            c.Ref.Render().AssetId = im ? AcceptOn : AcceptOff;
        }
        if (entry.HisAccept != 0 && customQ.TryGet(entry.HisAccept, out var hisRow))
        {
            var (_, c) = hisRow;
            c.Ref.Render().AssetId = he ? AcceptOn : AcceptOff;
        }
    }

    // Single-line UO ASCII label as a WrappedText custom. Returns the render ref
    // so callers can mutate .Text later (coin counters).
    private static UOCustomRender AddLabel(
        Commands commands, ulong parent, string text, Vector2 pos, int width, byte font, ushort hue)
    {
        var render = new UOCustomRender
        {
            Kind = UOCustomKind.WrappedText,
            Hue = Vector3.UnitZ,
            Text = text ?? string.Empty,
            TextFont = font,
            TextHue = hue,
            WrapWidth = width,
            IsHtml = false,
            TextAscii = true,
        };
        var ent = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                Width = Val.Px(width), Height = Val.Px(20),
            })
            .Insert(new UiCustom { Data = render });
        commands.AddChild(parent, ent.Id);
        return render;
    }

    private static void AddGoldEntry(
        Commands commands, ulong parent, Vector2 pos, uint serial, uint id1, bool isPlatinum)
    {
        var frame = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                Width = Val.Px(100), Height = Val.Px(20),
            });
        var capSerial = serial; var capId1 = id1; var capPlat = isPlatinum;
        GuiPlugin.SpawnTextField(
            commands, frame, new Vector2(2, 2),
            new TextFont { FontId = UoFontRuntime.DefaultFont, Size = 16 },
            new ClayColor(255, 255, 255, 255), "0", masked: false,
            decorate: e => e.Insert(new TradeGoldEntry { TradeSerial = capSerial, Id1 = capId1, IsPlatinum = capPlat }));
        commands.AddChild(parent, frame.Id);
    }

    // Add/remove item sprites in the trade boxes from the shared slot stream.
    private static void UpdateTradeItems(
        Commands commands,
        Res<AssetsServer> assets,
        ResMut<TradeUiMap> uiMap,
        EventReader<ContainerSlotEvent> reader,
        Query<Data<TradeItemUI>> existingQ)
    {
        foreach (var ev in reader.Read())
        {
            // Despawn any existing sprite for this serial (dedup on Add, the
            // whole job on Remove).
            foreach (var (oldEnt, link) in existingQ)
                if (link.Ref.ItemSerial == ev.ItemSerial)
                    commands.Entity(oldEnt.Ref).Despawn();

            if (ev.Action == ContainerSlotAction.Remove)
                continue;

            if (!uiMap.Value.TryGetByContainer(ev.ContainerSerial, out var entry, out var mine))
                continue;

            ushort drawGraphic = ItemGraphics.Displayed(ev.Graphic, ev.Amount);
            ref readonly var art = ref assets.Value.Arts.GetArt(drawGraphic);
            int w = art.UV.Width, h = art.UV.Height;

            // Clamp into the 110x80 box (legacy UpdateContents).
            int x = ev.X, y = (short)ev.Y;
            if (art.Texture != null)
            {
                if (x + w > BoxW) x = BoxW - w;
                if (y + h > BoxH) y = BoxH - h;
            }
            if (x < 0) x = 0;
            if (y < 0) y = 0;

            var box = mine ? entry.MyBox : entry.HisBox;
            var origHue = ShaderHueTranslator.GetHueVector(ev.Hue, false, 1f, false);
            var hoverHue = ShaderHueTranslator.GetHueVector(0x0035, false, 1f, false);

            var item = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(box.X + x), Top = Val.Px(box.Y + y),
                    Width = Val.Px(w), Height = Val.Px(h),
                })
                .Insert(new UiCustom
                {
                    Data = new UOCustomRender { Kind = UOCustomKind.Art, AssetId = drawGraphic, Hue = origHue }
                })
                .Insert(Interaction.None)
                .Insert(new TradeItemUI
                {
                    TradeSerial = mine ? entry.Id1 : entry.Id2,
                    ItemSerial = ev.ItemSerial,
                    OriginalHue = origHue,
                    HoverHue = hoverHue,
                });
            commands.AddChild(entry.Content, item.Id);
        }
    }

    // Highlight the topmost trade item under the cursor (legacy HighlightOnMouseOver).
    private static void HoverItems(
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<TradeItemUI, UiCustom>> itemQ)
    {
        if (itemQ.Count() == 0) return;

        var hit = UiPick.Topmost(mouse.Value.Position, assets.Value, rendered, parents);
        ulong top = hit.Found && itemQ.Contains(hit.Entity) ? hit.Entity : 0;

        foreach (var (ent, link, custom) in itemQ)
            custom.Ref.Render().Hue = ent.Ref == top ? link.Ref.HoverHue : link.Ref.OriginalHue;
    }

    // Poll my-side gold/platinum entry fields and Send_TradeUpdateGold when the
    // parsed (clamped) values change. Mirrors legacy OnTextChanged.
    private static void SendGoldChanges(
        Res<NetClient> net,
        ResMut<TradeUiMap> uiMap,
        Query<Data<TradeGoldEntry, Text>> entriesQ)
    {
        foreach (var entry in uiMap.Value.Entries)
        {
            if (!entry.IsNew) continue;

            uint gold = entry.LastGoldSent, plat = entry.LastPlatSent;
            uint rawGold = 0, rawPlat = 0;
            bool found = false;
            foreach (var (_, tag, text) in entriesQ)
            {
                if (tag.Ref.Id1 != entry.Id1) continue;
                found = true;
                uint raw = ParseUInt(text.Ref.Value);
                if (tag.Ref.IsPlatinum) rawPlat = raw; else rawGold = raw;
            }
            if (!found) continue;

            // The server converts between currencies (1 platinum == 1e9 gold)
            // and validates the combined offer against combined wealth, so cap
            // each field by total wealth minus what the other field already
            // offers — not by its own currency balance alone.
            const ulong PlatinumValue = 1_000_000_000;
            ulong totalWealth = entry.Gold + (ulong)entry.Platinum * PlatinumValue;
            ulong platOffered = (ulong)rawPlat * PlatinumValue;
            uint maxGold = (uint)Math.Min(uint.MaxValue, totalWealth > platOffered ? totalWealth - platOffered : 0);
            uint maxPlat = (uint)Math.Min(uint.MaxValue, (totalWealth > rawGold ? totalWealth - rawGold : 0) / PlatinumValue);
            gold = Math.Min(rawGold, maxGold);
            plat = Math.Min(rawPlat, maxPlat);

            if (gold != entry.LastGoldSent || plat != entry.LastPlatSent)
            {
                entry.LastGoldSent = gold;
                entry.LastPlatSent = plat;
                net.Value.Send_TradeUpdateGold(entry.Id1, gold, plat);
            }
        }
    }

    private static uint ParseUInt(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return uint.TryParse(text.Replace(",", ""), out var v) ? v : 0;
    }

    private static void DisposeOnLogout(
        Commands commands,
        ResMut<TradeUiMap> uiMap)
    {
        foreach (var entry in uiMap.Value.Entries)
            commands.Entity(entry.UiEntity).Despawn();
        uiMap.Value.Clear();
    }

#if AGENT_BUILD
    // Drains debug.openTrade / debug.tradeUpdate by replaying the same
    // PacketReceived trigger the network path fires. Type-0 bypasses the entity-
    // existence guard by calling SpawnTradeGump directly (the harness has no real
    // trade partners), later types go through the observer.
    private static void DrainDebugTrade(
        Commands commands,
        ResMut<DebugTradeQueue> q,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> zCounter,
        Res<GameContext> gameCtx,
        ResMut<TradeUiMap> uiMap)
    {
        foreach (var r in q.Value.Pending)
        {
            if (r.Type == 0)
            {
                if (!uiMap.Value.Has(r.Serial))
                    SpawnTradeGump(commands, builder.Value, assets.Value, zCounter.Value, gameCtx.Value, uiMap.Value,
                        r.Serial, r.Id1, r.Id2, r.Name);
            }
            else
            {
                commands.EmitTrigger(new PacketReceived<OnSecureTradePacket_0x6F>
                {
                    Packet = BuildDebugPacket(r)
                });
            }
        }
        q.Value.Pending.Clear();
    }

    private static OnSecureTradePacket_0x6F BuildDebugPacket(DebugTradeQueue.Req r)
    {
        var buf = new List<byte>();
        void U8(int v) => buf.Add((byte)v);
        void U32(uint v) { buf.Add((byte)(v >> 24)); buf.Add((byte)(v >> 16)); buf.Add((byte)(v >> 8)); buf.Add((byte)v); }

        U8(r.Type);
        U32(r.Serial);
        if (r.Type == 2) { U32(r.Id1); U32(r.Id2); }
        else if (r.Type == 3 || r.Type == 4) { U32(r.Gold); U32(r.Platinum); }

        var pkt = new OnSecureTradePacket_0x6F();
        pkt.Fill(new ClassicUO.IO.StackDataReader(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }
#endif
}

#if AGENT_BUILD
internal sealed class DebugTradeQueue
{
    public struct Req
    {
        public byte Type;
        public uint Serial;
        public uint Id1, Id2;
        public uint Gold, Platinum;
        public string Name;
    }

    public readonly List<Req> Pending = new();
}
#endif
