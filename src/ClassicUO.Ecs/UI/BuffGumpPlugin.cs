// Port of main's Game/UI/Gumps/BuffGump.cs to the ECS branch.
//
// The bar is NOT auto-opened. The server's 0xDF (BuffDebuff) only maintains the
// PlayerBuffs map (one entry per BuffIconType — a Count==0 packet removes that
// type) and marks it Dirty. The window is opened by the status bar's buff-icon
// button (StatusBarPlugin), which calls OpenOrFocus — exactly like legacy
// StatusGump.OnButtonClick(BuffIcon).
//
// Buff ORDER matches legacy: PlayerBuffs.Buffs is a Dictionary keyed by
// BuffIconType, mutated the same way as legacy PlayerMobile (`dict[type] = …`
// to add/refresh, `Remove(type)` to drop) and iterated in the same .NET
// dictionary enumeration order, so icons sit in identical slots (incl. the
// free-slot reuse after a removal).
//
// The graphic each icon draws is BuffTable.Table keyed by the icon id, like
// legacy (the packet's per-entry icon field is ignored — legacy keys off the
// header IconType). The bar supports all four legacy directions; the toggle
// button cycles 0x757F→0x7580→0x7581→0x7582→wrap, swapping the background and
// re-laying the icon row. Movability / right-click close / topmost-on-click /
// whole-bbox hit (UiContainsByBounds) come from the shared WindowDrag infra.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Singleton buff state, fed by the 0xDF observer, read by the rebuild system.
// A Dictionary (not a List) so enumeration order mirrors legacy PlayerMobile's
// _buffIcons exactly — same type, same add/remove ops, same iteration.
internal sealed class PlayerBuffs
{
    public readonly Dictionary<BuffIconType, ushort> Buffs = new();
    // Set when the buff set OR the bar direction changes; the open bar relays
    // its icons and clears it. Stays set while closed so a later open is current.
    public bool Dirty;

    public void Clear()
    {
        Buffs.Clear();
        Dirty = false;
    }
}

// Window root marker + the bar's current direction graphic (0x757F..0x7582).
internal struct BuffGumpUI;
internal struct BuffBarLayout { public ushort Graphic; }

// Marks a buff icon sprite and the direction-toggle button so a rebuild can
// find/replace just those. One bar, so flat markers (no window key) are enough.
internal struct BuffIconUI;
internal struct BuffBarToggle;

internal readonly struct BuffGumpPlugin : IPlugin
{
    // Legacy direction backgrounds. Default LEFT_HORIZONTAL (0x7580).
    private const ushort GraphicLeftVertical = 0x757F;
    private const ushort GraphicLeftHorizontal = 0x7580;
    private const ushort GraphicRightVertical = 0x7581;
    private const ushort GraphicRightHorizontal = 0x7582;
    private const ushort ToggleNormal = 0x7585, TogglePressed = 0x7589;
    private const int IconStep = 31;

    public void Build(App app)
    {
        app.AddResource(new PlayerBuffs());

        app.AddObserver<On<PacketReceived<OnBuffDebuffPacket_0xDF>>, ResMut<PlayerBuffs>>(OnBuffDebuff);

        var syncFn = Sync;
        var despawnFn = Despawn;
        app.AddSystem(syncFn).InStage(Stage.Update).Build();
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugBuffQueue());
        Action<Commands, Res<DebugBuffQueue>> drainFn = DrainDebugBuffs;
        app.AddSystem(Stage.First, drainFn);
        var openFn = DrainDebugOpen;
        app.AddSystem(openFn).InStage(Stage.First).Build();
#endif
    }

    // Open the buff bar, or focus it (bump z) if already open. One instance.
    // Called by the status bar's buff-icon button. Lays out via Sync (Dirty).
    public static void OpenOrFocus(
        Commands commands,
        GumpBuilder builder,
        UiZCounter zCounter,
        PlayerBuffs buffs,
        Query<Data<Node>, Filter<With<BuffGumpUI>>> rootQ)
    {
        foreach (var (ent, _) in rootQ)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
            return;
        }

        // UiContainsByBounds: the bar's interior is mostly transparent; bbox hit
        // makes the whole panel a drag / right-click-close target like the
        // healthbar / status bar (legacy ContainsByBounds).
        var root = builder.SpawnUOGump(commands, GraphicLeftHorizontal, Vector3.UnitZ, new Vector2(100, 100), zCounter)
            .Insert<BuffGumpUI>()
            .Insert<UiContainsByBounds>()
            .Insert(new BuffBarLayout { Graphic = GraphicLeftHorizontal });
        var rootId = root.Id;

        var toggle = builder.AddButton(commands, (ToggleNormal, TogglePressed, TogglePressed), Vector3.UnitZ, new Vector2(-2, 36))
            .Insert<BuffBarToggle>();
        toggle.Observe((On<UiClick> _,
                        ResMut<PlayerBuffs> b,
                        Query<Data<BuffBarLayout>, Filter<With<BuffGumpUI>>> layoutQ) =>
        {
            foreach (var (_, lay) in layoutQ)
            {
                lay.Ref.Graphic = NextGraphic(lay.Ref.Graphic);
                break;
            }
            b.Value.Dirty = true;
        });
        commands.AddChild(rootId, toggle.Id);

        buffs.Dirty = true;
    }

    // 0x3E9 / 0x466 are the two contiguous BuffIconType blocks; legacy folds the
    // new block down to continue the table index right after the old block.
    private const ushort BuffIconStart = 0x03E9;
    private const ushort BuffIconStartNew = 0x0466;

    private static void OnBuffDebuff(
        On<PacketReceived<OnBuffDebuffPacket_0xDF>> trig,
        ResMut<PlayerBuffs> buffs)
    {
        var packet = trig.Event.Packet;
        ushort ic = (ushort)packet.IconType;
        ushort iconID = ic >= BuffIconStartNew
            ? (ushort)(ic - (BuffIconStartNew - 125))
            : (ushort)(ic - BuffIconStart);

        if (iconID >= BuffTable.Table.Length)
            return;

        // Mirror legacy PlayerMobile: dict[type]=graphic to add/refresh (keeps
        // the existing slot on refresh), Remove(type) on a Count==0 packet.
        if (packet.Count != 0)
            buffs.Value.Buffs[packet.IconType] = BuffTable.Table[iconID];
        else
            buffs.Value.Buffs.Remove(packet.IconType);

        buffs.Value.Dirty = true;
    }

    // Rebuild the open bar's icon row (+ background + toggle pos) when the buff
    // set or direction changed. No-ops while the bar is closed (no root) — the
    // flag stays set so the next open shows the current buffs.
    private static void Sync(
        Commands commands,
        ResMut<PlayerBuffs> buffs,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Query<Data<Node, UiCustom, BuffBarLayout>, Filter<With<BuffGumpUI>>> rootQ,
        Query<Data<Node>, Filter<With<BuffIconUI>>> iconQ,
        Query<Data<Node>, Filter<With<BuffBarToggle>>> toggleQ)
    {
        if (!buffs.Value.Dirty)
            return;

        ulong root = 0;
        ushort graphic = GraphicLeftHorizontal;
        foreach (var (ent, node, custom, layout) in rootQ)
        {
            root = ent.Ref;
            graphic = layout.Ref.Graphic;

            ref readonly var bg = ref assets.Value.Gumps.GetGump(graphic);
            node.Ref.Width = Val.Px(bg.UV.Width);
            node.Ref.Height = Val.Px(bg.UV.Height);
            custom.Ref.Render().AssetId = graphic;
            break;
        }
        if (root == 0)
            return;

        buffs.Value.Dirty = false;

        var dir = DirectionOf(graphic);
        ref readonly var bgInfo = ref assets.Value.Gumps.GetGump(graphic);
        int bgW = bgInfo.UV.Width, bgH = bgInfo.UV.Height;

        var (btnX, btnY) = ButtonPosition(dir);
        foreach (var (_, tnode) in toggleQ)
        {
            tnode.Ref.Left = Val.Px(btnX);
            tnode.Ref.Top = Val.Px(btnY);
            break;
        }

        DespawnIcons(commands, iconQ);

        int i = 0;
        foreach (var kv in buffs.Value.Buffs)
        {
            var (x, y) = IconPosition(dir, i, bgW, bgH);
            var icon = builder.Value.AddGump(commands, kv.Value, Vector3.UnitZ, new Vector2(x, y));
            icon.Insert<BuffIconUI>();
            commands.AddChild(root, icon.Id);
            i++;
        }
    }

    private enum Direction { LeftVertical, LeftHorizontal, RightVertical, RightHorizontal }

    private static Direction DirectionOf(ushort graphic) => graphic switch
    {
        GraphicLeftHorizontal => Direction.LeftHorizontal,
        GraphicRightVertical => Direction.RightVertical,
        GraphicRightHorizontal => Direction.RightHorizontal,
        _ => Direction.LeftVertical,
    };

    private static ushort NextGraphic(ushort graphic)
    {
        graphic++;
        if (graphic > GraphicRightHorizontal)
            graphic = GraphicLeftVertical;
        return graphic;
    }

    // Icon slot i (offset = 31*i), mirroring legacy BuffGump.UpdateElements.
    // ECS gump roots are overflow-visible, so the RIGHT variants' negative
    // offsets just render left/above the bar — no origin-shift dance needed.
    private static (int x, int y) IconPosition(Direction dir, int i, int bgW, int bgH)
    {
        int off = IconStep * i;
        return dir switch
        {
            Direction.LeftVertical => (25, 26 + off),
            Direction.LeftHorizontal => (26 + off, 5),
            Direction.RightVertical => (5, bgH - 48 - off),
            Direction.RightHorizontal => (bgW - 48 - off, 5),
            _ => (26 + off, 5),
        };
    }

    private static (int x, int y) ButtonPosition(Direction dir) => dir switch
    {
        Direction.LeftHorizontal => (-2, 36),
        Direction.RightVertical => (34, 78),
        Direction.RightHorizontal => (76, 36),
        _ => (0, 0), // LeftVertical
    };

    private static void DespawnIcons(Commands commands, Query<Data<Node>, Filter<With<BuffIconUI>>> iconQ)
    {
        foreach (var (ent, _) in iconQ)
            commands.Entity(ent.Ref).Despawn();
    }

    private static void Despawn(
        Commands commands,
        ResMut<PlayerBuffs> buffs,
        Query<Data<Node>, Filter<With<BuffGumpUI>>> rootQ,
        Query<Data<Node>, Filter<With<BuffIconUI>>> iconQ)
    {
        DespawnIcons(commands, iconQ);
        foreach (var (ent, _) in rootQ)
            commands.Entity(ent.Ref).Despawn();
        buffs.Value.Clear();
    }

#if AGENT_BUILD
    // Drains debug.addBuff requests by emitting the same PacketReceived trigger
    // the network path fires, so the buff map updates through the real 0xDF
    // parse + observer path (deterministic harness repro — no server spell).
    private static void DrainDebugBuffs(Commands commands, Res<DebugBuffQueue> q)
    {
        if (q.Value.Pending.Count == 0) return;
        foreach (var r in q.Value.Pending)
            commands.EmitTrigger(new PacketReceived<OnBuffDebuffPacket_0xDF>
            {
                Packet = BuildDebugBuffPacket(r.Serial, r.IconType, r.Count)
            });
        q.Value.Pending.Clear();
    }

    // Drains debug.openBuffBar by invoking the same OpenOrFocus the status
    // button calls — lets the harness open the bar without driving the status UI.
    private static void DrainDebugOpen(
        Commands commands,
        ResMut<DebugBuffQueue> q,
        Res<GumpBuilder> builder,
        Res<UiZCounter> zCounter,
        Res<PlayerBuffs> buffs,
        Query<Data<Node>, Filter<With<BuffGumpUI>>> rootQ)
    {
        if (!q.Value.OpenRequested) return;
        q.Value.OpenRequested = false;
        OpenOrFocus(commands, builder.Value, zCounter.Value, buffs.Value, rootQ);
    }

    private static OnBuffDebuffPacket_0xDF BuildDebugBuffPacket(uint serial, ushort iconType, ushort count)
    {
        // Serialize into the real 0xDF wire body (sender onward; id+len are
        // stripped by the dispatcher) and Fill() — exact network parse path.
        var buf = new List<byte>();
        void U16(int v) { buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        void U32(uint v) { buf.Add((byte)(v >> 24)); buf.Add((byte)(v >> 16)); buf.Add((byte)(v >> 8)); buf.Add((byte)v); }

        U32(serial);
        U16(iconType);
        U16(count);
        for (int i = 0; i < count; i++)
        {
            U16(0);              // SourceType
            U16(0);              // skip 2
            U16(0);              // Icon (parsed, ignored — graphic comes from BuffTable)
            U16(0);              // QueueIndex
            U32(0);              // skip 4
            U16(0xFFFF);         // Timer (0xFFFF = no countdown)
            buf.Add(0); buf.Add(0); buf.Add(0); // skip 3
            U32(0); U32(0); U32(0);             // title / desc / additional cliloc
            U16(0);              // argsLen (Arguments) = 0
            U16(0);              // null terminator for the always-read ReadUnicodeLE()
            U16(0);              // argsLen (Arguments2) = 0
            U16(0);              // argsLen (Arguments3) = 0
        }

        var pkt = new OnBuffDebuffPacket_0xDF();
        pkt.Fill(new StackDataReader(CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }
#endif
}

#if AGENT_BUILD
internal sealed class DebugBuffQueue
{
    public readonly List<(uint Serial, ushort IconType, ushort Count)> Pending = new();
    public bool OpenRequested;
}
#endif
