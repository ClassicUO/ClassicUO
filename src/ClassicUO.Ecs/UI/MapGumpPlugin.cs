// Port of main's Game/UI/Gumps/MapGump.cs (treasure / cartography maps).
//
// Server pushes 0x90 (or 0xF5 with a facet, post-308Z variants carry the facet
// field) — the observer bakes the multimap region into a texture via
// AssetsServer.MultiMaps and opens a framed window: ResizePic 0x1432 frame, the
// map texture at (24,31), plot-course buttons (0x1398/0x1399/0x139A), and the
// pin decoration 0x139D bottom-right. 0x56 MapData drives pins: Add appends a
// pin (0x139B) + extends the course polyline, Clear empties it, EditResponse
// flips the plot state (the Plot/Stop buttons send action 6 and wait for it).
// Clicking the map while plotting sends action 1 (add pin) like legacy's
// TextureControlOnMouseUp; pin dragging (legacy local-only) is not ported.
//
// Movable / right-click-close / stacking come from the shared WindowDrag infra
// (UiMovable root). The baked Texture2D is plugin-owned: a registry keyed by
// window root + a per-frame reap disposes it when the window despawns by any
// path (right-click, server close, state exit).

using System;
using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Map window root, keyed by the map item serial.
internal struct MapWindow
{
    public uint Serial;
    public int PlotState;            // 0 = idle, 1 = plotting
    public int MapW, MapH;
    public ulong MapNode;
    public ulong PlotBtn, StopBtn, ClearBtn;
    // The map node's custom render — Points is the course polyline the
    // renderer draws over the texture; pin sprites are tracked alongside.
    public UOCustomRender Render;
    public List<ulong> PinEnts;
}

// Baked multimap textures keyed by window root, disposed by the reap when the
// root despawns (the component is gone by then, so the texture rides here).
internal sealed class MapTextureRegistry : Dictionary<ulong, Texture2D> { }

internal readonly struct MapGumpPlugin : IPlugin
{
    private const ushort FrameGraphic = 0x1432;    // ResizePic parchment frame
    private const ushort PlotBtnGraphic = 0x1398;  // "Plot Course"
    private const ushort StopBtnGraphic = 0x1399;  // "Stop Plotting"
    private const ushort ClearBtnGraphic = 0x139A; // "Clear Course"
    private const ushort PinGraphic = 0x139B;
    private const ushort DecorGraphic = 0x139D;    // bottom-right pin art
    private const int MapX = 24, MapY = 31;        // map texture origin in the frame

    public void Build(App app)
    {
        app.AddResource(new MapTextureRegistry());

        app.AddObserver<On<PacketReceived<OnShowMapPacket_0x90_Pre308Z>>, Commands, MapGumpParams>(
            static (t, commands, p) =>
            {
                var k = t.Event.Packet;
                BuildMap(commands, p, k.Serial, k.Width, k.Height, k.StartX, k.StartY, k.EndX, k.EndY, facet: null);
            });
        app.AddObserver<On<PacketReceived<OnShowMapPacket_0x90_Post308Z>>, Commands, MapGumpParams>(
            static (t, commands, p) =>
            {
                var k = t.Event.Packet;
                // Post-308Z clients read the multimap even from the facetless
                // packet (legacy DisplayMap: version >= 308Z -> facet 0).
                BuildMap(commands, p, k.Serial, k.Width, k.Height, k.StartX, k.StartY, k.EndX, k.EndY, facet: 0);
            });
        app.AddObserver<On<PacketReceived<OnShowMapFacetPacket_0xF5_Pre308Z>>, Commands, MapGumpParams>(
            static (t, commands, p) =>
            {
                var k = t.Event.Packet;
                // Pre-308Z 0xF5 carries no facet field on the wire.
                BuildMap(commands, p, k.Serial, k.Width, k.Height, k.StartX, k.StartY, k.EndX, k.EndY, facet: 0);
            });
        app.AddObserver<On<PacketReceived<OnShowMapFacetPacket_0xF5_Post308Z>>, Commands, MapGumpParams>(
            static (t, commands, p) =>
            {
                var k = t.Event.Packet;
                BuildMap(commands, p, k.Serial, k.Width, k.Height, k.StartX, k.StartY, k.EndX, k.EndY, k.Facet);
            });

        // 0x56 — server-driven pin add / clear / plot-state flip.
        app.AddObserver((
            On<PacketReceived<OnMapDataPacket_0x56>> trig,
            Commands commands,
            Res<GumpBuilder> builder,
            Query<Data<MapWindow>> windowsQ,
            Query<Data<Node>> nodeQ,
            Query<Data<TinyEcs.Children>> childrenQ) =>
        {
            var k = trig.Event.Packet;
            foreach (var (ent, win) in windowsQ)
            {
                if (win.Ref.Serial != k.Serial) continue;
                switch (k.MessageType)
                {
                    case MapMessageType.Add:
                        AddPin(commands, builder.Value, ref win.Ref, k.X, k.Y);
                        break;
                    case MapMessageType.Clear:
                        ClearPins(commands, ref win.Ref, childrenQ);
                        break;
                    case MapMessageType.EditResponse:
                        win.Ref.PlotState = k.PlotEnabled ? 1 : 0;
                        ApplyPlotState(win.Ref, nodeQ);
                        break;
                }
            }
        });

        // Map-surface click while plotting: press latched on the map node, pin
        // added on a near-stationary release (legacy guards |drag| < 5 so a
        // window drag over the map doesn't plant pins).
        var clickFn = MapPlotClick;
        app.AddSystem(clickFn).InStage(Stage.PreUpdate).SingleThreaded().Build();

        var reapFn = ReapTextures;
        app.AddSystem(reapFn).InStage(Stage.PostUpdate).Build();

        var despawnFn = DespawnAll;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugMapQueue());
        Action<Commands, ResMut<DebugMapQueue>> drainFn = DrainDebugMap;
        app.AddSystem(Stage.First, drainFn);
#endif
    }

    private static void BuildMap(
        Commands commands, MapGumpParams p,
        uint serial, int width, int height, int startX, int startY, int endX, int endY, int? facet)
    {
        // Re-push for the same serial replaces the window (legacy adds a new
        // gump; one live window per map item keeps the pin state unambiguous).
        foreach (var (ent, win) in p.WindowsQ)
            if (win.Ref.Serial == serial)
                DespawnSubtree(commands, ent.Ref, p.ChildrenQ);

        var assets = p.Assets.Value;
        var builder = p.Builder.Value;

        var mapInfo = assets.MultiMaps.GetMap(facet, width, height, startX, startY, endX, endY);

        int totalW = width + 44, totalH = height + 61;

        // Root: invisible full-bounds drag/close surface (the frame's interior
        // pixels are mostly the map texture, which has no alpha mask to hit).
        var root = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(150), Top = Val.Px(80),
                Width = Val.Px(totalW), Height = Val.Px(totalH),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None)
            .Insert<UOGump>()
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(p.ZCounter.Value.Bump()));
        ulong rootId = root.Id;

        var frame = builder.AddGumpNinePatch(commands, FrameGraphic, Vector3.UnitZ, new Vector2(0, 0), new Vector2(totalW, totalH));
        commands.AddChild(rootId, frame.Id);

        var render = new UOCustomRender
        {
            Kind = UOCustomKind.DynTexture,
            Hue = Vector3.UnitZ,
            Dynamic = mapInfo.Texture,
            Points = new List<Vector2>(),
        };
        var mapNode = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(MapX), Top = Val.Px(MapY),
                Width = Val.Px(width), Height = Val.Px(height),
            })
            .Insert(new UiCustom { Data = render });
        commands.AddChild(rootId, mapNode.Id);

        var decor = builder.AddGump(commands, DecorGraphic, Vector3.UnitZ, new Vector2(width - 20, height - 20));
        commands.AddChild(rootId, decor.Id);

        // Plot-state buttons (legacy coords; Plot visible idle, Stop+Clear while
        // plotting). Plot/Stop send action 6 and flip on the EditResponse echo —
        // legacy flips immediately, but the echo also fires, so the echo is the
        // single source of truth here.
        var plot = builder.AddButton(commands, (PlotBtnGraphic, PlotBtnGraphic, PlotBtnGraphic), Vector3.UnitZ, new Vector2((width - 100) >> 1, 5))
            .Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>();
        var stop = builder.AddButton(commands, (StopBtnGraphic, StopBtnGraphic, StopBtnGraphic), Vector3.UnitZ, new Vector2((width - 70) >> 1, 5))
            .Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>();
        var clear = builder.AddButton(commands, (ClearBtnGraphic, ClearBtnGraphic, ClearBtnGraphic), Vector3.UnitZ, new Vector2((width - 66) >> 1, height + 37))
            .Insert<UiNoWindowDrag>().Insert<UiContainsByBounds>();
        commands.AddChild(rootId, plot.Id);
        commands.AddChild(rootId, stop.Id);
        commands.AddChild(rootId, clear.Id);

        var capSerial = serial;
        var capRoot = rootId;
        plot.Observe((On<UiClick> _, Res<NetClient> net, Query<Data<MapWindow>> wq, Query<Data<Node>> nq) =>
            TogglePlot(capRoot, capSerial, net.Value, wq, nq));
        stop.Observe((On<UiClick> _, Res<NetClient> net, Query<Data<MapWindow>> wq, Query<Data<Node>> nq) =>
            TogglePlot(capRoot, capSerial, net.Value, wq, nq));
        clear.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd, Query<Data<MapWindow>> wq, Query<Data<TinyEcs.Children>> kidsQ) =>
        {
            net.Value.Send_MapMessage(capSerial, 5, 0, unchecked((ushort)-24), unchecked((ushort)-31));
            if (!wq.Contains(capRoot)) return;
            var (_, w) = wq.Get(capRoot);
            ClearPins(cmd, ref w.Ref, kidsQ);
        });

        commands.Entity(rootId).Insert(new MapWindow
        {
            Serial = serial,
            PlotState = 0,
            MapW = width,
            MapH = height,
            MapNode = mapNode.Id,
            PlotBtn = plot.Id,
            StopBtn = stop.Id,
            ClearBtn = clear.Id,
            Render = render,
            PinEnts = new List<ulong>(),
        });
        // Stop/Clear start hidden; the node query can't see the spawn-frame
        // entities yet, so re-insert the builder's node shape with Display.None.
        commands.Entity(stop.Id).Insert(HiddenButtonNode(assets, StopBtnGraphic, (width - 70) >> 1, 5));
        commands.Entity(clear.Id).Insert(HiddenButtonNode(assets, ClearBtnGraphic, (width - 66) >> 1, height + 37));

        if (p.MapTextures.Value.Remove(rootId, out var stale))
            stale?.Dispose();
        if (mapInfo.Texture != null)
            p.MapTextures.Value[rootId] = mapInfo.Texture;
    }

    private static void TogglePlot(ulong rootId, uint serial, NetClient net, Query<Data<MapWindow>> wq, Query<Data<Node>> nq)
    {
        if (!wq.Contains(rootId)) return;
        var (_, w) = wq.Get(rootId);
        net.Send_MapMessage(serial, 6, (byte)w.Ref.PlotState, unchecked((ushort)-24), unchecked((ushort)-31));
        // Legacy flips locally without waiting for the EditResponse echo; some
        // servers don't echo, so mirror that.
        w.Ref.PlotState = w.Ref.PlotState == 0 ? 1 : 0;
        ApplyPlotState(w.Ref, nq);
    }

    private static void ApplyPlotState(in MapWindow w, Query<Data<Node>> nodeQ)
    {
        SetDisplay(nodeQ, w.PlotBtn, w.PlotState == 0);
        SetDisplay(nodeQ, w.StopBtn, w.PlotState == 1);
        SetDisplay(nodeQ, w.ClearBtn, w.PlotState == 1);
    }

    private static void SetDisplay(Query<Data<Node>> q, ulong e, bool visible)
    {
        if (e == 0 || !q.Contains(e)) return;
        var (_, n) = q.Get(e);
        n.Ref.Display = visible ? Display.Flex : Display.None;
    }

    // GumpBuilder's MakeFloatingNode shape (Node.Default + abs position + sprite
    // size) with Display.None — the hidden plot-state buttons start like this.
    private static Node HiddenButtonNode(AssetsServer assets, ushort id, float x, float y)
    {
        var n = Node.Default;
        n.Display = Display.None;
        n.PositionType = PositionType.Absolute;
        n.Left = Val.Px(x);
        n.Top = Val.Px(y);
        n.Width = Val.Px(GumpW(assets, id));
        n.Height = Val.Px(GumpH(assets, id));
        return n;
    }

    // Pin sprite child of the map node at the packet's map-relative coords
    // (legacy PinControl adds an odd self-size offset that drifts pins ~13px
    // from the click; the wire coords are the truth, so they're used directly).
    // The polyline point sits at the sprite origin like legacy's line pass.
    private static void AddPin(Commands commands, GumpBuilder builder, ref MapWindow win, int x, int y)
    {
        var pin = builder.AddGump(commands, PinGraphic, Vector3.UnitZ, new Vector2(x, y));
        commands.AddChild(win.MapNode, pin.Id);
        win.PinEnts.Add(pin.Id);
        win.Render.Points.Add(new Vector2(x, y));
    }

    private static void ClearPins(Commands commands, ref MapWindow win, Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var e in win.PinEnts)
            DespawnSubtree(commands, e, childrenQ);
        win.PinEnts.Clear();
        win.Render.Points.Clear();
    }

    private struct PlotPress { public bool Active; public ulong Window; public Vector2 Pos; }

    // While a window is in plot state, a press latched on its map node plants a
    // pin on release if the cursor barely moved (legacy TextureControlOnMouseUp
    // |LDragOffset| < 5). Runs before WindowDragPlugin so the gesture coexists
    // with window dragging: big moves drag the window and plant nothing.
    private static void MapPlotClick(
        Res<MouseContext> mouse,
        Res<NetClient> net,
        Commands commands,
        Res<GumpBuilder> builder,
        Local<PlotPress> press,
        Query<Data<ComputedNode>> bbQ,
        Query<Data<MapWindow>> windowsQ)
    {
        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            var pos = mouse.Value.Position;
            foreach (var (ent, win) in windowsQ)
            {
                if (win.Ref.PlotState != 1 || !bbQ.Contains(win.Ref.MapNode)) continue;
                var (_, bb) = bbQ.Get(win.Ref.MapNode);
                if (pos.X < bb.Ref.Position.X || pos.Y < bb.Ref.Position.Y) continue;
                if (pos.X >= bb.Ref.Position.X + bb.Ref.Size.X || pos.Y >= bb.Ref.Position.Y + bb.Ref.Size.Y) continue;
                press.Value = new PlotPress { Active = true, Window = ent.Ref, Pos = pos };
                return;
            }
            return;
        }

        if (!mouse.Value.IsReleased(MouseButtonType.Left) || !press.Value.Active)
            return;

        var latched = press.Value;
        press.Value = default;
        var rel = mouse.Value.Position;
        if (Math.Abs(rel.X - latched.Pos.X) >= 5 || Math.Abs(rel.Y - latched.Pos.Y) >= 5)
            return; // it was a drag
        if (!windowsQ.Contains(latched.Window) || !bbQ.Contains(latched.Window)) return;
        var (_, w) = windowsQ.Get(latched.Window);
        if (w.Ref.PlotState != 1 || !bbQ.Contains(w.Ref.MapNode)) return;
        var (_, mapBb) = bbQ.Get(w.Ref.MapNode);

        ushort x = (ushort)(rel.X - mapBb.Ref.Position.X + 5); // legacy e.X + 5
        ushort y = (ushort)(rel.Y - mapBb.Ref.Position.Y);
        net.Value.Send_MapMessage(w.Ref.Serial, 1, 0, x, y);
        AddPin(commands, builder.Value, ref w.Ref, x, y);
    }

    // Dispose the baked texture when its window root is gone (right-click close
    // despawns the subtree without notifying the plugin; OnRemove<T> is
    // unreliable on despawn, so poll the registry against live roots).
    private static void ReapTextures(
        ResMut<MapTextureRegistry> textures,
        Query<Data<MapWindow>> windowsQ,
        Local<List<ulong>> dead)
    {
        if (textures.Value.Count == 0) return;
        dead.Value.Clear();
        foreach (var kv in textures.Value)
            if (!windowsQ.Contains(kv.Key))
                dead.Value.Add(kv.Key);
        foreach (var id in dead.Value)
        {
            if (textures.Value.Remove(id, out var tex))
                tex?.Dispose();
        }
    }

    private static void DespawnAll(
        Commands commands,
        Query<Data<MapWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ)
            DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong e, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.Contains(e))
        {
            var (_, kids) = childrenQ.Get(e);
            foreach (var cid in kids.Ref)
                DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(e).Despawn();
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    private static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }

#if AGENT_BUILD
    // Drains debug.openMap by emitting the registered 0x90 Post308Z trigger so
    // the gump opens through the real parse + observer path (needs the UO data
    // dir's Multimap.rle for the bake).
    private static void DrainDebugMap(Commands commands, ResMut<DebugMapQueue> q)
    {
        if (!q.Value.Pending) return;
        q.Value.Pending = false;

        var buf = new List<byte>();
        void U32(uint v) { buf.Add((byte)(v >> 24)); buf.Add((byte)(v >> 16)); buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        void U16(ushort v) { buf.Add((byte)(v >> 8)); buf.Add((byte)v); }

        U32(q.Value.Serial);
        U16(0x139D);
        U16((ushort)q.Value.StartX); U16((ushort)q.Value.StartY);
        U16((ushort)q.Value.EndX); U16((ushort)q.Value.EndY);
        U16((ushort)q.Value.Width); U16((ushort)q.Value.Height);

        var pkt = new OnShowMapPacket_0x90_Post308Z();
        pkt.Fill(new IO.StackDataReader(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buf)));
        commands.EmitTrigger(new PacketReceived<OnShowMapPacket_0x90_Post308Z> { Packet = pkt });
    }
#endif
}

internal sealed class MapGumpParams : CompositeSystemParam
{
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<AssetsServer> Assets;
    public readonly Res<UiZCounter> ZCounter;
    public readonly ResMut<MapTextureRegistry> MapTextures;
    public readonly Query<Data<MapWindow>> WindowsQ;
    public readonly Query<Data<TinyEcs.Children>> ChildrenQ;

    public MapGumpParams()
    {
        Builder = Add(new Res<GumpBuilder>());
        Assets = Add(new Res<AssetsServer>());
        ZCounter = Add(new Res<UiZCounter>());
        MapTextures = Add(new ResMut<MapTextureRegistry>());
        WindowsQ = Add(new Query<Data<MapWindow>>());
        ChildrenQ = Add(new Query<Data<TinyEcs.Children>>());
    }
}

#if AGENT_BUILD
internal sealed class DebugMapQueue
{
    public bool Pending;
    public uint Serial = 0x4000D00D;
    public int StartX = 1000, StartY = 1000, EndX = 1400, EndY = 1400;
    public int Width = 200, Height = 200;
}
#endif
