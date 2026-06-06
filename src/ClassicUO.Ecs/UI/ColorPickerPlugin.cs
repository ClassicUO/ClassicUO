// Server-pushed dye/hue picker (packet 0x95, "DisplayDyeWindow"). Renders the
// dye-tub window (bg 0x0906) with a hue swatch grid, a graduation slider, a
// live-tinted preview of the dye tub, and an OK button that replies with the
// chosen hue via Send_DyeDataResponse (0x95).
//
// The swatch grid is a single HueGrid custom-render element (see UOCustomKind):
// 10 rows x 20 cols of 8px cells. The hue of cell i is graduation+2+5*i — the
// exact legacy ColorPickerBox formula — so the SENT hue is precise; the per-cell
// display RGB is baked from the hue palette (the UO hue shader can't tint a plain
// white pixel, so we pre-resolve to RGBA). The graduation slider (0..4) shifts
// the whole band by 1 and rebakes.
//
// The eyedropper button (legacy "pick from target") is rendered for fidelity but
// inert — entity targeting isn't ported. The window is movable; the grid and
// slider carry UINoWindowDrag so pressing them edits instead of dragging.

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;

namespace ClassicUO.Ecs;

internal readonly struct ColorPickerPlugin : IPlugin
{
    private const int Rows = 10;
    private const int Cols = 20;
    private const int Cell = 8;
    private const ushort BgGump = 0x0906;
    private const ushort PreviewArt = 0x0FAB;

    public void Build(App app)
    {
        app.AddObserver<On<PacketReceived<OnColorPickerPacket_0x95>>, Commands, ColorPickerParams>(Spawn);

        Action<Commands, Query<Data<ColorPickerRoot>>> teardownFn =
            (cmd, q) =>
            {
                foreach (var (ent, _) in q)
                    cmd.Entity(ent.Ref).Despawn();
            };
        app.AddSystem(teardownFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugColorPickerQueue());
        Action<Commands, Res<DebugColorPickerQueue>> drainFn = DrainDebug;
        app.AddSystem(Stage.First, drainFn);
#endif
    }

    private static void Spawn(
        On<PacketReceived<OnColorPickerPacket_0x95>> trig,
        Commands commands,
        ColorPickerParams p)
    {
        var pkt = trig.Event.Packet;
        Open(commands, p.Assets.Value, p.Builder.Value, p.Files.Value.Hues, p.ZCounter.Value, p.Surface.Value, pkt.Serial, pkt.Graphic);
    }

    // Spawn the dye picker for (serial, graphic). serial 0 = a local/no-target
    // picker (OK still sends, but the server ignores serial 0) — used by the
    // Options test gump. Exposed so non-packet callers can open it too.
    internal static void Open(
        Commands commands, AssetsServer assets, GumpBuilder builder, HuesLoader hues,
        UiZCounter zc, UiSurface surface, uint serial, ushort graphic)
    {
        const int grad = 1; // legacy ColorPickerBox default Graduation

        var z = zc.Bump();
        var rootId = commands.Spawn().Insert(new ColorPickerRoot()).Id;

        ref readonly var bg = ref assets.Gumps.GetGump(BgGump);
        int bgW = bg.UV.Width, bgH = bg.UV.Height;
        commands.AddChild(rootId, builder.AddGump(commands, BgGump, Vector3.UnitZ, new Vector2(0, 0)).Id);

        // Swatch grid.
        var colors = new uint[Rows * Cols];
        Bake(colors, grad, hues);
        var gridRender = new UOCustomRender
        {
            Kind = UOCustomKind.HueGrid,
            GridRows = Rows, GridCols = Cols, CellW = Cell, CellH = Cell,
            SelectedIndex = 0, GridColors = colors,
        };
        var gridCmd = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(34), Top = Val.Px(34),
                Width = Val.Px(Cols * Cell), Height = Val.Px(Rows * Cell),
            })
            .Insert(new UiCustom { Data = gridRender })
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UINoWindowDrag>();
        var gridId = gridCmd.Id;
        commands.AddChild(rootId, gridId);

        // Live preview of the dye tub, tinted to the selected hue.
        var previewId = builder.AddArt(commands, PreviewArt,
            ShaderHueTranslator.GetHueVector((ushort)(grad + 2), false, 1f), new Vector2(200, 78)).Id;
        commands.AddChild(rootId, previewId);

        commands.Entity(rootId).Insert(new ColorPickerState
        {
            Serial = serial, Graphic = graphic, Graduation = grad,
            GridEntity = gridId, PreviewEntity = previewId,
        });

        var capRoot = rootId; var capGrid = gridId; var capPrev = previewId;

        // Graduation slider (0..4). On change: rebake the grid band + retint preview.
        var slider = builder.AddHSlider(commands, 0, 4, grad, new Vector2(39, 142), 145, thumbGump: 0x0845);
        commands.AddChild(rootId, slider.Id);
        slider.Observe((On<SliderChanged> t,
            Query<Data<UiCustom>> ucq, Query<Data<ColorPickerState>> stq, Res<UOFileManager> files) =>
        {
            int g = Math.Clamp((int)MathF.Round(t.Event.Value), 0, 4);
            if (stq.Contains(capRoot)) { var (_, st) = stq.Get(capRoot); st.Ref.Graduation = g; }
            if (ucq.Contains(capGrid))
            {
                var (_, uc) = ucq.Get(capGrid);
                var r = uc.Ref.Render();
                if (r?.GridColors != null)
                {
                    Bake(r.GridColors, g, files.Value.Hues);
                    SetPreviewHue(ucq, capPrev, (ushort)(g + 2 + 5 * r.SelectedIndex));
                }
            }
        });

        // Grid click → cell → SelectedIndex + preview retint. Fraction-based so it
        // is independent of DPI scaling (ComputedNode + pointer are both screen space).
        gridCmd.Observe((On<UiPointerDown> t,
            Query<Data<ComputedNode, UiCustom>> geomQ, Query<Data<UiCustom>> ucq, Query<Data<ColorPickerState>> stq) =>
        {
            if (!geomQ.Contains(capGrid)) return;
            var (_, cn, uc) = geomQ.Get(capGrid);
            var r = uc.Ref.Render();
            if (r == null) return;
            var bb = cn.Ref;
            if (bb.Size.X <= 0 || bb.Size.Y <= 0) return;
            int col = Math.Clamp((int)((t.Event.Position.X - bb.Position.X) / bb.Size.X * Cols), 0, Cols - 1);
            int row = Math.Clamp((int)((t.Event.Position.Y - bb.Position.Y) / bb.Size.Y * Rows), 0, Rows - 1);
            int idx = row * Cols + col;
            r.SelectedIndex = idx;
            int g = stq.Contains(capRoot) ? GetGrad(stq, capRoot) : grad;
            SetPreviewHue(ucq, capPrev, (ushort)(g + 2 + 5 * idx));
        });

        // OK (legacy button id 0): reply with the selected hue, then close.
        var ok = builder.AddButton(commands, (0x0907, 0x0908, 0x0909), Vector3.UnitZ, new Vector2(208, 138));
        ok.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd,
            Query<Data<ColorPickerState>> stq, Query<Data<UiCustom>> ucq) =>
        {
            if (!stq.Contains(capRoot)) return;
            var (_, st) = stq.Get(capRoot);
            int sel = 0;
            if (ucq.Contains(capGrid)) { var (_, uc) = ucq.Get(capGrid); sel = uc.Ref.Render()?.SelectedIndex ?? 0; }
            ushort hue = (ushort)(st.Ref.Graduation + 2 + 5 * sel);
            net.Value.Send_DyeDataResponse(st.Ref.Serial, st.Ref.Graphic, hue);
            cmd.Entity(capRoot).Despawn();
        });
        commands.AddChild(rootId, ok.Id);

        // Eyedropper (legacy button id 1) — rendered only; target-pick not ported.
        var eye = builder.AddButton(commands, (0x5669, 0x566B, 0x566A), Vector3.UnitZ, new Vector2(212, 33));
        commands.AddChild(rootId, eye.Id);

        var cx = MathF.Max(0, (surface.LogicalSize.X - bgW) * 0.5f);
        var cy = MathF.Max(0, (surface.LogicalSize.Y - bgH) * 0.5f);
        commands.Entity(rootId)
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(cx), Top = Val.Px(cy),
                Width = Val.Px(bgW), Height = Val.Px(bgH),
            })
            // Movable (legacy CanMove=true). The grid + slider carry UINoWindowDrag
            // so a press on them edits instead of dragging; a press anywhere else
            // on the window (bg/preview/buttons) drags it.
            .Insert<UIMovable>()
            .Insert(new GlobalZIndex(z));
    }

    private static int GetGrad(Query<Data<ColorPickerState>> stq, ulong rootId)
    {
        var (_, st) = stq.Get(rootId);
        return st.Ref.Graduation;
    }

    private static void SetPreviewHue(Query<Data<UiCustom>> ucq, ulong previewId, ushort hue)
    {
        if (!ucq.Contains(previewId)) return;
        var (_, uc) = ucq.Get(previewId);
        var r = uc.Ref.Render();
        if (r != null) r.Hue = ShaderHueTranslator.GetHueVector(hue, false, 1f);
    }

    // Bake the per-cell display RGBA for a graduation band. Cell i shows hue
    // graduation+2+5*i; GetPolygoneColor(30, hue) is the brightest gradient cell
    // (it does the hue-1 shift internally, matching GetHueVector), so the swatch
    // and the preview tint of the same hue agree.
    private static void Bake(uint[] colors, int graduation, HuesLoader hues)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            ushort hue = (ushort)(graduation + 2 + 5 * i);
            uint c = hues.GetPolygoneColor(30, hue);
            if ((c >> 24) == 0) c |= 0xFF000000u; // force opaque if the palette cell has no alpha
            colors[i] = c;
        }
    }

#if AGENT_BUILD
    private static void DrainDebug(Commands commands, Res<DebugColorPickerQueue> q)
    {
        if (q.Value.Pending.Count == 0) return;
        foreach (var r in q.Value.Pending)
            commands.EmitTrigger(new PacketReceived<OnColorPickerPacket_0x95>
            {
                Packet = BuildDebugPacket(r.Serial, r.Graphic)
            });
        q.Value.Pending.Clear();
    }

    private static OnColorPickerPacket_0x95 BuildDebugPacket(uint serial, ushort graphic)
    {
        // Real 0x95 wire body (serial onward; id+len stripped by the dispatcher).
        var buf = new List<byte>();
        void U16(int v) { buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        void U32(uint v) { buf.Add((byte)(v >> 24)); buf.Add((byte)(v >> 16)); buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        U32(serial);
        U16(0);          // reserved
        U16(graphic);
        var pkt = new OnColorPickerPacket_0x95();
        pkt.Fill(new StackDataReader(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }
#endif
}

// Marker on the dialog root (logout teardown + state lookup scope).
internal struct ColorPickerRoot;

internal struct ColorPickerState
{
    public uint Serial;
    public ushort Graphic;
    public int Graduation;
    public ulong GridEntity;
    public ulong PreviewEntity;
}

internal sealed class ColorPickerParams : CompositeSystemParam
{
    public readonly Res<AssetsServer> Assets;
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<UOFileManager> Files;
    public readonly Res<UiZCounter> ZCounter;
    public readonly Res<UiSurface> Surface;

    public ColorPickerParams()
    {
        Assets   = Add(new Res<AssetsServer>());
        Builder  = Add(new Res<GumpBuilder>());
        Files    = Add(new Res<UOFileManager>());
        ZCounter = Add(new Res<UiZCounter>());
        Surface  = Add(new Res<UiSurface>());
    }
}

#if AGENT_BUILD
internal sealed class DebugColorPickerQueue
{
    public readonly List<(uint Serial, ushort Graphic)> Pending = new();
}
#endif
