// TipNoticeGump — ECS port of Game/UI/Gumps/TipNoticeGump.cs.
//
// Server pushes 0xA6 (WindowTip): flag + serial + ASCII text. flag 1 means
// "close / nothing to show" (ignored). flag 0 is a Tip-of-the-day scroll with
// a title banner (0x9CA) and prev/next arrows (0x9CC / 0x9CD) that send 0xA7
// Send_TipRequest and close; any other flag is a Notice scroll (title 0x9D2,
// no arrows).
//
// The window is the same resizable parchment scroll as the profile gump
// (ExpandableScroll 0x0820: top cap, tiled body, bottom cap, bottom-center
// resize knob), so this mirrors ProfileGumpPlugin's bg + LayoutWindow + resize
// infra. The tip text is read-only, wrapped, and scrolls in the middle.
//
// Movable / right-click-close / topmost-on-click come from the shared WindowDrag
// infra via the pixel-perfect parchment sprites walking up to the UiMovable root.

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal struct TipNoticeWindow
{
    public ushort TipSerial;
    public int SpecialHeight;
    public float Width;
    public ulong MidEntity, BottomEntity, ExpanderEntity;
    public ulong PrevEntity, NextEntity; // 0 for a notice (no arrows)
}

internal struct TipNoticeExpander { public ulong Window; }

internal readonly struct TipNoticeGumpPlugin : IPlugin
{
    private const ushort ScrollGraphic = 0x0820; // +0 top cap, +2 body (tiled), +3 bottom cap
    private const ushort ExpanderNormal = 0x082E, ExpanderPressed = 0x082F;
    private const ushort TipTitle = 0x9CA, NoticeTitle = 0x9D2;
    private const ushort PrevArrow = 0x9CC, NextArrow = 0x9CD;
    private const int Height = 322;     // legacy default 300 + DiffY(22)
    private const int MinHeight = 274;  // c_ExpandableScrollHeight_Min
    private const int MaxHeight = 800;  // c_ExpandableScrollHeight_Max
    private const int CapOverlap = 14;
    private const int ContentX = 35;    // legacy textbox X
    private const int ContentY = 32;    // legacy ScrollArea Y
    private const int BodyWidth = 220;
    private const int ButtonsFromBottom = 53; // legacy commented OnPageChanged: Y = SpecialHeight - 53
    private const byte Font = 1;        // unicode font 1 (legacy uses ASCII 6; 1 renders cleanly on parchment)
    private const uint TextColorBlack = 0x010101FF;

    public void Build(App app)
    {
        app.AddObserver<On<PacketReceived<OnWindowTipPacket_0xA6>>, Commands, TipNoticeParams>(OnTip);

        var layoutFn = LayoutWindow;
        app.AddSystem(layoutFn).InStage(Stage.Update).Build();

        var resizeFn = ResizeDrag;
        app.AddSystem(resizeFn).InStage(Stage.PreUpdate).Build();

        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugTipQueue());
        Action<Commands, ResMut<DebugTipQueue>> drainFn = DrainDebugTip;
        app.AddSystem(Stage.First, drainFn);
#endif
    }

    private static void OnTip(
        On<PacketReceived<OnWindowTipPacket_0xA6>> trig,
        Commands commands,
        TipNoticeParams p)
    {
        var packet = trig.Event.Packet;
        byte flag = packet.Flags;
        if (flag == 1) return; // "close / nothing" — legacy returns immediately

        bool isTip = flag == 0;
        ushort tipSerial = (ushort)packet.Serial;
        string text = packet.Text?.Replace('\r', '\n') ?? string.Empty;

        var assets = p.Assets.Value;
        var builder = p.Builder.Value;

        int topW = GumpW(assets, ScrollGraphic), topH = GumpH(assets, ScrollGraphic);
        int midW = GumpW(assets, (ushort)(ScrollGraphic + 2));
        int botW = GumpW(assets, (ushort)(ScrollGraphic + 3)), botH = GumpH(assets, (ushort)(ScrollGraphic + 3));
        int width = Math.Max(topW, Math.Max(midW, botW));

        // Bare container root (no UiCustom) so UiPick hits the parchment sprites
        // pixel-perfectly and walks up to this UiMovable root for drag/close.
        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(isTip ? 200 : 20), Top = Val.Px(isTip ? 100 : 20),
                Width = Val.Px(width), Height = Val.Px(Height),
            })
            .Insert(Interaction.None)
            .Insert<UOGump>()
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(p.ZCounter.Value.Bump()));
        var rootId = root.Id;

        var mid = builder.AddGumpTiled(commands, (ushort)(ScrollGraphic + 2), Vector3.UnitZ,
            new Vector2((width - midW) / 2f, topH), new Vector2(midW, Height - topH));
        commands.AddChild(rootId, mid.Id);
        var top = builder.AddGump(commands, ScrollGraphic, Vector3.UnitZ, new Vector2((width - topW) / 2f, 0));
        commands.AddChild(rootId, top.Id);
        var bottom = builder.AddGump(commands, (ushort)(ScrollGraphic + 3), Vector3.UnitZ, new Vector2((width - botW) / 2f, Height - botH));
        commands.AddChild(rootId, bottom.Id);

        var expander = builder.AddButton(commands, (ExpanderNormal, ExpanderPressed, ExpanderNormal), Vector3.UnitZ, new Vector2(0, 0))
            .Insert(new TipNoticeExpander { Window = rootId });
        commands.AddChild(rootId, expander.Id);

        // Title banner, centered in the top cap (legacy ExpandableScroll title).
        ushort titleId = isTip ? TipTitle : NoticeTitle;
        int titleW = GumpW(assets, titleId), titleH = GumpH(assets, titleId);
        var title = builder.AddGump(commands, titleId, Vector3.UnitZ, new Vector2((topW - titleW) / 2f, (topH - titleH) / 2f));
        commands.AddChild(rootId, title.Id);

        // Read-only wrapped text in a scrollable viewport (mouse wheel scrolls
        // long tips). Frame clips to the body area; the text node grows past it.
        int bodyH = Height - ContentY - 70;
        var frame = commands.Spawn().Insert(new Node
        {
            Display = Display.Flex,
            FlexDirection = FlexDirection.Column,
            PositionType = PositionType.Absolute,
            Left = Val.Px(ContentX), Top = Val.Px(ContentY),
            Width = Val.Px(BodyWidth), Height = Val.Px(bodyH),
            Overflow = Overflow.Scroll,
        }).Insert(new ScrollPosition());
        commands.AddChild(rootId, frame.Id);

        var bodyNode = SpawnText(commands, BodyWidth, text);
        if (bodyNode != 0) commands.AddChild(frame.Id, bodyNode);

        ulong prevId = 0, nextId = 0;
        if (isTip)
        {
            // Prev / next arrows (legacy buttons 1 / 2). Both send 0xA7 and close
            // this scroll; the server replies with the adjacent tip.
            var prev = builder.AddButton(commands, (PrevArrow, PrevArrow, PrevArrow), Vector3.UnitZ, new Vector2(35, Height - ButtonsFromBottom));
            prev.Observe((On<UiClick> _, Commands cmd, Res<NetClient> net,
                Query<Data<TinyEcs.Children>, Filter<With<TipNoticeWindow>>> winQ,
                Query<Data<TinyEcs.Children>> kidsQ) =>
            {
                net.Value.Send_TipRequest(tipSerial, 0);
                DespawnAll(cmd, winQ, kidsQ);
            });
            commands.AddChild(rootId, prev.Id);
            prevId = prev.Id;

            var next = builder.AddButton(commands, (NextArrow, NextArrow, NextArrow), Vector3.UnitZ, new Vector2(240, Height - ButtonsFromBottom));
            next.Observe((On<UiClick> _, Commands cmd, Res<NetClient> net,
                Query<Data<TinyEcs.Children>, Filter<With<TipNoticeWindow>>> winQ,
                Query<Data<TinyEcs.Children>> kidsQ) =>
            {
                net.Value.Send_TipRequest(tipSerial, 1);
                DespawnAll(cmd, winQ, kidsQ);
            });
            commands.AddChild(rootId, next.Id);
            nextId = next.Id;
        }

        commands.Entity(rootId).Insert(new TipNoticeWindow
        {
            TipSerial = tipSerial,
            SpecialHeight = Height,
            Width = width,
            MidEntity = mid.Id,
            BottomEntity = bottom.Id,
            ExpanderEntity = expander.Id,
            PrevEntity = prevId,
            NextEntity = nextId,
        });
    }

    private static ulong SpawnText(Commands commands, int width, string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var (w, h) = UoFontRenderer.Measure(text, Font, width, isHtml: true, TextColorBlack, htmlBg: false);
        if (w <= 0 || h <= 0) return 0;

        return commands.Spawn()
            .Insert(new Node { Display = Display.Flex, Width = Val.Px(w), Height = Val.Px(h) })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.WrappedText,
                    Hue = Vector3.UnitZ,
                    Text = text,
                    TextFont = Font,
                    WrapWidth = width,
                    IsHtml = true,
                    HtmlStartColor = TextColorBlack,
                    HtmlBg = false,
                },
            }).Id;
    }

    private static void LayoutWindow(
        Res<AssetsServer> assets,
        Query<Data<TipNoticeWindow>> windowsQ,
        Query<Data<Node>> nodeQ)
    {
        foreach (var (rootEnt, win) in windowsQ)
        {
            int topH = GumpH(assets.Value, ScrollGraphic);
            int botH = GumpH(assets.Value, (ushort)(ScrollGraphic + 3));
            int expW = GumpW(assets.Value, ExpanderNormal);
            int h = win.Ref.SpecialHeight;
            int botY = h - botH;
            int midY = topH - CapOverlap;
            int midH = Math.Max(1, botY - midY);

            SetNodeHeight(nodeQ, rootEnt.Ref, h);
            SetNodeY(nodeQ, win.Ref.MidEntity, midY);
            SetNodeHeight(nodeQ, win.Ref.MidEntity, midH);
            SetNodeY(nodeQ, win.Ref.BottomEntity, botY);
            SetNodeXY(nodeQ, win.Ref.ExpanderEntity, (win.Ref.Width - expW) / 2f, h - 2);
            // Arrows track the resizable bottom.
            SetNodeY(nodeQ, win.Ref.PrevEntity, h - ButtonsFromBottom);
            SetNodeY(nodeQ, win.Ref.NextEntity, h - ButtonsFromBottom);
        }
    }

    private struct ResizeAnchor { public bool Active, Claimed; public ulong Window; public float MouseY0; public int Origin; }

    private static void ResizeDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<ResizeAnchor> anchor,
        Query<Data<ComputedNode>, Filter<With<TipNoticeExpander>>> handleBbQ,
        Query<Data<TipNoticeExpander>> handleQ,
        Query<Data<TipNoticeWindow>> windowsQ)
    {
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        if (!held)
        {
            if (anchor.Value.Claimed && gate.Value.Mode == ActiveDrag.UIWindow)
                gate.Value.Mode = ActiveDrag.None;
            anchor.Value = default;
            return;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            if (gate.Value.Mode != ActiveDrag.None) return;
            var pos = mouse.Value.Position;
            foreach (var (ent, bb) in handleBbQ)
            {
                if (pos.X < bb.Ref.Position.X || pos.Y < bb.Ref.Position.Y) continue;
                if (pos.X >= bb.Ref.Position.X + bb.Ref.Size.X || pos.Y >= bb.Ref.Position.Y + bb.Ref.Size.Y) continue;
                if (!handleQ.TryGet(ent.Ref, out var expRow)) continue;
                var (_, exp) = expRow;
                if (!windowsQ.TryGet(exp.Ref.Window, out var winRow)) continue;
                var (_, win) = winRow;
                gate.Value.Mode = ActiveDrag.UIWindow;
                anchor.Value.Claimed = true;
                anchor.Value.Active = true;
                anchor.Value.Window = exp.Ref.Window;
                anchor.Value.MouseY0 = pos.Y;
                anchor.Value.Origin = win.Ref.SpecialHeight;
                break;
            }
        }

        if (!anchor.Value.Active || !windowsQ.TryGet(anchor.Value.Window, out var anchorWinRow)) return;
        var (_, w) = anchorWinRow;
        float newH = anchor.Value.Origin + (mouse.Value.Position.Y - anchor.Value.MouseY0);
        w.Ref.SpecialHeight = (int)Math.Clamp(newH, MinHeight, MaxHeight);
    }

    private static void SetNodeHeight(Query<Data<Node>> q, ulong e, float h)
    {
        if (e != 0 && q.TryGet(e, out var nodeRow)) { var (_, n) = nodeRow; n.Ref.Height = Val.Px(h); }
    }
    private static void SetNodeY(Query<Data<Node>> q, ulong e, float y)
    {
        if (e != 0 && q.TryGet(e, out var nodeRow)) { var (_, n) = nodeRow; n.Ref.Top = Val.Px(y); }
    }
    private static void SetNodeXY(Query<Data<Node>> q, ulong e, float x, float y)
    {
        if (e != 0 && q.TryGet(e, out var nodeRow)) { var (_, n) = nodeRow; n.Ref.Left = Val.Px(x); n.Ref.Top = Val.Px(y); }
    }

    private static void Despawn(
        Commands commands,
        Query<Data<TinyEcs.Children>, Filter<With<TipNoticeWindow>>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
        => DespawnAll(commands, windowsQ, childrenQ);

    private static void DespawnAll(
        Commands commands,
        Query<Data<TinyEcs.Children>, Filter<With<TipNoticeWindow>>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ)
            DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong entity, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.TryGet(entity, out var kidsRow))
        {
            var (_, kids) = kidsRow;
            foreach (var cid in kids.Ref)
                DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(entity).Despawn();
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

#if AGENT_BUILD
    private static void DrainDebugTip(Commands commands, ResMut<DebugTipQueue> q)
    {
        if (!q.Value.Pending) return;
        q.Value.Pending = false;
        commands.EmitTrigger(new PacketReceived<OnWindowTipPacket_0xA6>
        {
            Packet = BuildDebugTipPacket(q.Value.Flag, q.Value.Serial, q.Value.Text)
        });
    }

    private static OnWindowTipPacket_0xA6 BuildDebugTipPacket(byte flag, uint serial, string text)
    {
        text ??= string.Empty;
        var buf = new List<byte>();
        buf.Add(flag);
        buf.Add((byte)(serial >> 24)); buf.Add((byte)(serial >> 16)); buf.Add((byte)(serial >> 8)); buf.Add((byte)serial);
        buf.Add((byte)(text.Length >> 8)); buf.Add((byte)text.Length);
        foreach (var c in text) buf.Add((byte)c);

        var pkt = new OnWindowTipPacket_0xA6();
        pkt.Fill(new StackDataReader(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }
#endif
}

internal sealed class TipNoticeParams : CompositeSystemParam
{
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<AssetsServer> Assets;
    public readonly Res<UiZCounter> ZCounter;

    public TipNoticeParams()
    {
        Builder = Add(new Res<GumpBuilder>());
        Assets = Add(new Res<AssetsServer>());
        ZCounter = Add(new Res<UiZCounter>());
    }
}

#if AGENT_BUILD
internal sealed class DebugTipQueue
{
    public bool Pending;
    public byte Flag;
    public uint Serial;
    public string Text = string.Empty;
}
#endif
