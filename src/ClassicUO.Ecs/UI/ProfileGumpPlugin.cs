// Port of main's Game/UI/Gumps/ProfileGump.cs to the ECS branch.
//
// Server pushes 0xB8 (CharacterProfile: serial + ASCII header + unicode footer
// + unicode body) — fired in reply to the paperdoll Profile button's
// Send_ProfileRequest (PaperdollPlugin already sends it) or unsolicited. The
// observer opens a parchment scroll window (legacy ExpandableScroll 0x0820)
// keyed by serial: header label at the top, the free-form body wrapped + mouse-
// wheel-scrolled in the middle, footer label at the bottom.
//
// One window per serial; a fresh 0xB8 for the same serial rebuilds it (legacy
// GetGump(serial)?.Dispose() + Add). Read-only for now — legacy lets the player
// edit their own profile (canEdit = serial == player) and sends 0xB9
// Send_ProfileUpdate on close; that editable path is not ported yet.
//
// Movable / right-click-close / topmost-on-click come from the shared WindowDrag
// infra: the parchment sprites are pixel-perfect hit targets that walk up to the
// UiMovable root (no whole-bbox capture — legacy ProfileGump is pixel-perfect).

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClassicUO.Game;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Profile window root, keyed by the character serial it shows. Carries the
// resizable scroll height + the bg/expander entity ids LayoutWindow repositions.
internal struct ProfileWindow
{
    public uint Serial;
    public int SpecialHeight;
    public float Width;
    public ulong MidEntity, BottomEntity, ExpanderEntity;
    // Footer (divider + footer text) container, anchored a fixed distance above
    // the scroll bottom so it tracks the resizable window height instead of
    // sitting just below the body with empty parchment beneath it.
    public ulong FooterEntity;
    public int FooterOffsetFromBottom;
}

// Drag-to-resize handle marker (the bottom expander knob).
internal struct ProfileExpander { public ulong Window; }

// On the editable body glyph (own profile): the serial to update + the text as
// last sent, so commit-on-blur only fires the 0xB8 update when it actually changed.
internal struct ProfileBodyEdit { public uint Serial; public string Original; }

// One-shot flag letting the harness force the editable path regardless of serial
// (debug.openProfile { edit:true }); always false in production.
internal sealed class ProfileEditOverride { public bool ForceNext; }

internal readonly struct ProfileGumpPlugin : IPlugin
{
    private const ushort ScrollGraphic = 0x0820; // +0 top cap, +2 body (tiled), +3 bottom cap
    private const ushort MinimizeGump = 0x82D;   // top-center knob
    private const ushort ExpanderNormal = 0x082E, ExpanderPressed = 0x082F; // resize handle
    // Header divider (seal + gold line + leaf) and footer divider (curl line).
    private const ushort HdrLeft = 0x005C, HdrMid = 0x005D, HdrRight = 0x005E;
    private const ushort FtrLeft = 0x005F, FtrMid = 0x0060, FtrRight = 0x0061;
    private const int Height = 322;              // legacy default 300 + DiffY(22)
    private const int MinHeight = 274;           // legacy c_ExpandableScrollHeight_Min
    private const int MaxHeight = 800;           // legacy c_ExpandableScrollHeight_Max
    private const int CapOverlap = 14;           // body slides up under the top cap
    // Content origin inside the scroll, mirroring legacy ScrollArea(22, 32+DiffY)
    // measured from the scroll top (the cap sits at y=0 here).
    private const int ContentX = 22;
    private const int ContentY = 32;
    private const int BodyWidth = 220;
    private const byte Font = 1; // legacy unicode font 1
    // Near-black text on the parchment. RGBA packing (low byte = alpha), the same
    // value ServerGumpPlugin.HtmlStartColor uses for no-bg parchment text — NOT
    // 0xAARRGGBB (that put alpha in the wrong byte and rendered transparent).
    private const uint TextColorBlack = 0x010101FF;

    public void Build(App app)
    {
        app.AddResource(new ProfileEditOverride());

        app.AddObserver<On<PacketReceived<OnOpenCharacterProfilePacket_0xB8>>, Commands, ProfileParams>(OnProfile);

        // Commit the player's edited body when focus leaves the field (legacy sent
        // on Dispose; TinyEcs OnRemove-on-despawn is unreliable, so blur is the hook).
        var commitFn = CommitEdit;
        app.AddSystem(commitFn).InStage(Stage.Update).Build();

        var layoutFn = LayoutWindow;
        app.AddSystem(layoutFn).InStage(Stage.Update).Build();
        // PreUpdate (before window drag) so a press on the expander claims the
        // shared DragGate first — same ordering as the journal resize handle.
        var resizeFn = ResizeDrag;
        app.AddSystem(resizeFn).InStage(Stage.PreUpdate).Build();

        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugProfileQueue());
        Action<Commands, ResMut<DebugProfileQueue>, ResMut<ProfileEditOverride>> drainFn = DrainDebugProfile;
        app.AddSystem(Stage.First, drainFn);
#endif
    }

    private static void OnProfile(
        On<PacketReceived<OnOpenCharacterProfilePacket_0xB8>> trig,
        Commands commands,
        ProfileParams p)
    {
        var packet = trig.Event.Packet;

        // Rebuild on a repeat for the same serial (legacy disposes + re-adds).
        foreach (var (ent, win, kids) in p.ExistingQ)
        {
            if (win.Ref.Serial != packet.Serial) continue;
            commands.Entity(ent.Ref).Despawn();
        }

        var assets = p.Assets.Value;
        var builder = p.Builder.Value;

        int topW = GumpW(assets, ScrollGraphic), topH = GumpH(assets, ScrollGraphic);
        int midW = GumpW(assets, (ushort)(ScrollGraphic + 2));
        int botW = GumpW(assets, (ushort)(ScrollGraphic + 3)), botH = GumpH(assets, (ushort)(ScrollGraphic + 3));
        int width = Math.Max(topW, Math.Max(midW, botW));

        // Bare container root — no UiCustom, so UiPick skips it and the gump is
        // hit pixel-perfectly through its parchment sprites (legacy ProfileGump
        // Contains), not as a whole rectangle. Drag / right-click-close still
        // resolve via the parchment children walking up to this UiMovable root.
        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(300), Top = Val.Px(110),
                Width = Val.Px(width), Height = Val.Px(Height),
            })
            .Insert(Interaction.None)
            .Insert<UOGump>()
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(p.ZCounter.Value.Bump()));
        var rootId = root.Id;

        // Scroll bg: tiled body first so the caps paint over its seams.
        // LayoutWindow repositions mid/bottom/expander every frame from SpecialHeight.
        var mid = builder.AddGumpTiled(commands, (ushort)(ScrollGraphic + 2), Vector3.UnitZ,
            new Vector2((width - midW) / 2f, topH), new Vector2(midW, Height - topH));
        commands.AddChild(rootId, mid.Id);
        var top = builder.AddGump(commands, ScrollGraphic, Vector3.UnitZ, new Vector2((width - topW) / 2f, 0));
        commands.AddChild(rootId, top.Id);
        var bottom = builder.AddGump(commands, (ushort)(ScrollGraphic + 3), Vector3.UnitZ, new Vector2((width - botW) / 2f, Height - botH));
        commands.AddChild(rootId, bottom.Id);

        // Resize expander knob, bottom-center.
        var expander = builder.AddButton(commands, (ExpanderNormal, ExpanderPressed, ExpanderNormal), Vector3.UnitZ, new Vector2(0, 0))
            .Insert(new ProfileExpander { Window = rootId });
        commands.AddChild(rootId, expander.Id);

        // Minimize knob, centered above the top roll (decorative for now).
        int knobW = GumpW(assets, MinimizeGump), knobH = GumpH(assets, MinimizeGump);
        var knob = builder.AddGump(commands, MinimizeGump, Vector3.UnitZ, new Vector2((width - knobW) / 2f, -knobH + 8));
        commands.AddChild(rootId, knob.Id);

        // Content laid out top-down, mirroring legacy ProfileGump's ScrollArea:
        // header label, header divider (seal/line/leaf), body, footer divider,
        // footer label. All offsets are legacy's (same 0x0820 scroll gfx), so the
        // divider pieces land exactly as in the OOP client.
        var header = SpawnText(commands, new Vector2(ContentX + 53, ContentY + 6), 140, packet.Header, scroll: false, out int headerH);
        if (header != 0) commands.AddChild(rootId, header);

        int dividerY = ContentY + Math.Max(0, headerH - 15);
        AddDivider(commands, builder, assets, rootId, HdrLeft, HdrMid, HdrRight,
            ContentX + 4, ContentX + 56, 138, ContentX + 194, dividerY, midDrop: 0);

        bool canEdit = packet.Serial == p.GameCtx.Value.PlayerSerial || p.EditOverride.Value.ForceNext;
        p.EditOverride.Value.ForceNext = false;

        int bodyY = ContentY + Math.Max(0, headerH - 15) + 44;
        int bodyH;
        if (canEdit)
        {
            // Editable body for the player's own profile: a multiline wrapped field
            // (Enter inserts a newline; text wraps at BodyWidth). The typed value is
            // pushed via the 0xB8 update on blur (CommitEdit).
            bodyH = 110;
            // Scrollable: the wrapped editable text grows past bodyH; the frame
            // clips it to the box and the mouse wheel scrolls it (the flowing
            // SpawnTextField row + glyph live inside this Overflow.Scroll viewport).
            var frame = commands.Spawn().Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PositionType = PositionType.Absolute,
                Left = Val.Px(ContentX + 4), Top = Val.Px(bodyY),
                Width = Val.Px(BodyWidth), Height = Val.Px(bodyH),
                Overflow = Overflow.Scroll,
            }).Insert(new ScrollPosition());
            var capSerial = packet.Serial;
            var capBody = packet.Body ?? string.Empty;
            GuiPlugin.SpawnTextField(commands, frame, new Vector2(0, 0),
                new TextFont { FontId = Font, Size = 16 }, new ClayColor(0, 0, 0, 255), capBody, masked: false,
                decorate: e => e.Insert(new ProfileBodyEdit { Serial = capSerial, Original = capBody }),
                multiline: true, wrapWidth: BodyWidth);
            commands.AddChild(rootId, frame.Id);
        }
        else
        {
            var body = SpawnText(commands, new Vector2(ContentX + 4, bodyY), BodyWidth, packet.Body, scroll: false, out bodyH);
            if (body != 0) commands.AddChild(rootId, body);
            bodyH = Math.Max(bodyH, 20);
        }

        int footerY = bodyY + bodyH + 3;
        // Footer (divider + footer text) lives in its own container so LayoutWindow
        // can slide it as one unit to track the resizable scroll bottom. Children
        // are positioned relative to the container (footer divider at y=0, label
        // below), so moving the container's Top moves them together.
        var footerContainer = commands.Spawn().Insert(new Node
        {
            Display = Display.Flex,
            PositionType = PositionType.Absolute,
            Left = Val.Px(0), Top = Val.Px(footerY),
            Width = Val.Px(width), Height = Val.Auto,
        });
        commands.AddChild(rootId, footerContainer.Id);
        AddDivider(commands, builder, assets, footerContainer.Id, FtrLeft, FtrMid, FtrRight,
            ContentX + 4, ContentX + 13, 197, ContentX + 210, 0, midDrop: 9);
        var footer = SpawnText(commands, new Vector2(ContentX + 2, 26), BodyWidth, packet.Footer, scroll: false, out _);
        if (footer != 0) commands.AddChild(footerContainer.Id, footer);

        commands.Entity(rootId).Insert(new ProfileWindow
        {
            Serial = packet.Serial,
            SpecialHeight = Height,
            Width = width,
            MidEntity = mid.Id,
            BottomEntity = bottom.Id,
            ExpanderEntity = expander.Id,
            FooterEntity = footerContainer.Id,
            // Distance the footer keeps above the scroll bottom (constant as the
            // window resizes): its gap from the default bottom edge.
            FooterOffsetFromBottom = Height - footerY,
        });
    }

    // Three-piece divider: left ornament, horizontally tiled middle, right
    // ornament. midDrop nudges the tiled line down within the band (footer line
    // sits a few px lower than its end curls, like legacy).
    private static void AddDivider(Commands commands, GumpBuilder builder, AssetsServer assets, ulong rootId,
        ushort left, ushort mid, ushort right, int leftX, int midX, int midW, int rightX, int y, int midDrop)
    {
        var l = builder.AddGump(commands, left, Vector3.UnitZ, new Vector2(leftX, y));
        commands.AddChild(rootId, l.Id);
        var m = builder.AddGumpTiled(commands, mid, Vector3.UnitZ, new Vector2(midX, y + midDrop), new Vector2(midW, GumpH(assets, mid)));
        commands.AddChild(rootId, m.Id);
        var r = builder.AddGump(commands, right, Vector3.UnitZ, new Vector2(rightX, y));
        commands.AddChild(rootId, r.Id);
    }

    // Wrapped UO-font text run, black on the parchment, sized to its measured
    // content (the scroll itself is fixed-height like legacy's default). Returns
    // the node id (0 if empty) and outputs the measured height so the caller can
    // flow the next element below it. `scroll` reserved for a future tall-body
    // scroller; unused while bodies fit the parchment.
    private static ulong SpawnText(Commands commands, Vector2 position, int width, string text, bool scroll, out int height)
    {
        height = 0;
        if (string.IsNullOrEmpty(text))
            return 0;

        var (w, h) = UoFontRenderer.Measure(text, Font, width, isHtml: true, TextColorBlack, htmlBg: false);
        height = h;
        if (w <= 0 || h <= 0)
            return 0;

        var outer = commands.Spawn().Insert(new Node
        {
            Display = Display.Flex,
            PositionType = PositionType.Absolute,
            Left = Val.Px(position.X), Top = Val.Px(position.Y),
            Width = Val.Px(w), Height = Val.Px(h),
        });

        {
            var inner = commands.Spawn()
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
                });
            commands.AddChild(outer.Id, inner.Id);
        }

        return outer.Id;
    }

    // Reposition the tiled body, bottom cap, expander and footer each frame from
    // the window's SpecialHeight. Top cap + header + body stay top-anchored; the
    // footer is bottom-anchored (tracks the scroll bottom), so growing the scroll
    // adds empty parchment between the body and the footer.
    private static void LayoutWindow(
        Res<AssetsServer> assets,
        Query<Data<ProfileWindow>> windowsQ,
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

            // Height only — Top/Left are the drag-controlled window position.
            SetNodeHeight(nodeQ, rootEnt.Ref, h);
            SetNodeY(nodeQ, win.Ref.MidEntity, midY);
            SetNodeHeight(nodeQ, win.Ref.MidEntity, midH);
            SetNodeY(nodeQ, win.Ref.BottomEntity, botY);
            SetNodeXY(nodeQ, win.Ref.ExpanderEntity, (win.Ref.Width - expW) / 2f, h - 2);
            SetNodeY(nodeQ, win.Ref.FooterEntity, h - win.Ref.FooterOffsetFromBottom);
        }
    }

    // Push the edited body when focus leaves the field (commit-on-blur). Tracks
    // the previously-focused entity; when it was a profile body field and its
    // text changed, sends the 0xB8 update once and re-baselines Original.
    private static void CommitEdit(
        Res<FocusedInput> focused,
        Res<NetClient> net,
        Local<ulong> lastFocused,
        Query<Data<UiCustom, ProfileBodyEdit>> fieldsQ)
    {
        var cur = focused.Value.Entity;
        if (cur == lastFocused.Value) return;
        var prev = lastFocused.Value;
        lastFocused.Value = cur;

        if (prev != 0 && fieldsQ.TryGet(prev, out var fieldRow))
        {
            var (_, custom, edit) = fieldRow;
            var value = custom.Ref.Render().Text ?? string.Empty;
            if (value != edit.Ref.Original)
            {
                net.Value.Send_ProfileUpdate(edit.Ref.Serial, value);
                edit.Ref.Original = value;
            }
        }
    }

    private struct ResizeAnchor { public bool Active, Claimed; public ulong Window; public float MouseY0; public int Origin; }

    private static void ResizeDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Local<ResizeAnchor> anchor,
        Query<Data<ComputedNode>, Filter<With<ProfileExpander>>> handleBbQ,
        Query<Data<ProfileExpander>> handleQ,
        Query<Data<ProfileWindow>> windowsQ)
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

        if (!anchor.Value.Active || !windowsQ.TryGet(anchor.Value.Window, out var dragWinRow)) return;
        var (_, w) = dragWinRow;
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
        Query<Data<TinyEcs.Children>, Filter<With<ProfileWindow>>> windowsQ)
    {
        foreach (var (ent, _) in windowsQ)
            commands.Entity(ent.Ref).Despawn();
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

#if AGENT_BUILD
    // Drains debug.openProfile by emitting the same PacketReceived trigger the
    // network path fires — opens the gump through the real 0xB8 parse + observer
    // path without a server reply (deterministic harness repro).
    private static void DrainDebugProfile(Commands commands, ResMut<DebugProfileQueue> q, ResMut<ProfileEditOverride> editOverride)
    {
        if (!q.Value.Pending) return;
        q.Value.Pending = false;
        editOverride.Value.ForceNext = q.Value.Edit;
        commands.EmitTrigger(new PacketReceived<OnOpenCharacterProfilePacket_0xB8>
        {
            Packet = BuildDebugProfilePacket(q.Value.Serial, q.Value.Header, q.Value.Footer, q.Value.Body)
        });
    }

    private static OnOpenCharacterProfilePacket_0xB8 BuildDebugProfilePacket(uint serial, string header, string footer, string body)
    {
        // Real 0xB8 wire body (sender onward): serial, ASCII header (null-term),
        // unicode-BE footer (null-term), unicode-BE body (null-term).
        var buf = new List<byte>();
        void U32(uint v) { buf.Add((byte)(v >> 24)); buf.Add((byte)(v >> 16)); buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        void Ascii(string s) { foreach (var c in s) buf.Add((byte)c); buf.Add(0); }
        void UniBE(string s) { foreach (var c in s) { buf.Add((byte)(c >> 8)); buf.Add((byte)c); } buf.Add(0); buf.Add(0); }

        U32(serial);
        Ascii(header ?? string.Empty);
        UniBE(footer ?? string.Empty);
        UniBE(body ?? string.Empty);

        var pkt = new OnOpenCharacterProfilePacket_0xB8();
        pkt.Fill(new StackDataReader(CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }
#endif
}

internal sealed class ProfileParams : CompositeSystemParam
{
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<AssetsServer> Assets;
    public readonly Res<UiZCounter> ZCounter;
    public readonly Res<GameContext> GameCtx;
    public readonly ResMut<ProfileEditOverride> EditOverride;
    public readonly Query<Data<ProfileWindow, TinyEcs.Children>> ExistingQ;

    public ProfileParams()
    {
        Builder = Add(new Res<GumpBuilder>());
        Assets = Add(new Res<AssetsServer>());
        ZCounter = Add(new Res<UiZCounter>());
        GameCtx = Add(new Res<GameContext>());
        EditOverride = Add(new ResMut<ProfileEditOverride>());
        ExistingQ = Add(new Query<Data<ProfileWindow, TinyEcs.Children>>());
    }
}

#if AGENT_BUILD
internal sealed class DebugProfileQueue
{
    public bool Pending;
    public bool Edit;
    public uint Serial;
    public string Header = string.Empty, Footer = string.Empty, Body = string.Empty;
}
#endif
