// Item / mobile tooltip — ECS port of Game/UI/Tooltip.cs + ObjectPropertiesList
// Manager.cs. The server pushes an Object Property List (cliloc name + property
// lines) per entity via 0xD6 (MegaCliloc); 0xDC (OPLInfo) just announces a
// revision so the client knows when its cached OPL is stale. We request the OPL
// lazily: when the cursor hovers a serial-bearing entity (world item/mobile,
// container slot, paperdoll equipment) we queue a 0xD6 request; the reply fills
// ObjectPropertyLists and, after the hover delay, a small HTML text box renders
// at the cursor.
//
// The box is offset down-right of the cursor point so it never sits under the
// hot pixel — that keeps it out of UiPick (which tests the exact mouse point),
// so it can't hijack drag / right-click-close on the window behind it. It is NOT
// a UiMovable gump: it is an ephemeral overlay this plugin fully owns and
// despawns directly (the "don't despawn gumps yourself" rule is about
// server/right-click-closable windows, not overlays like this or PopupMenu).

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Cached OPL data + the lazy request queue. Mirrors legacy
// ObjectPropertiesListManager: store keyed by serial, plus a dedup set of
// serials awaiting a 0xD6 request to be sent.
internal sealed class ObjectPropertyLists
{
    internal struct Entry
    {
        public uint Revision;
        public string Name;   // first cliloc line (the object name)
        public string Data;   // remaining property lines, '\n'-joined
        public int MaxWidth;  // wrap width (legacy SetTooltip maxWidth); 0 = default
    }

    private readonly Dictionary<uint, Entry> _map = new();
    private readonly HashSet<uint> _requested = new();

    public void Add(uint serial, uint revision, string name, string data, int maxWidth = 0)
    {
        _map[serial] = new Entry { Revision = revision, Name = name, Data = data, MaxWidth = maxWidth };
        _requested.Remove(serial);
    }

    public bool TryGet(uint serial, out Entry entry) => _map.TryGetValue(serial, out entry);

    // Legacy IsRevisionEquals: the server's revision can carry a 0x40000000
    // mask; compare both masked and raw.
    public bool IsRevisionEquals(uint serial, uint revision)
        => _map.TryGetValue(serial, out var p)
        && ((revision & ~0x40000000) == p.Revision || revision == p.Revision);

    // Queue a request only if we have no data yet (hover path).
    public void Request(uint serial)
    {
        if (!_map.ContainsKey(serial))
            _requested.Add(serial);
    }

    // Queue unconditionally — the 0xDC revision-mismatch path refreshes an OPL
    // we already hold but that changed server-side.
    public void ForceRequest(uint serial) => _requested.Add(serial);

    public bool HasPending => _requested.Count > 0;

    public void DrainInto(List<uint> dst)
    {
        dst.AddRange(_requested);
        _requested.Clear();
    }

    public void Clear()
    {
        _map.Clear();
        _requested.Clear();
    }
}

// Display singleton: the hovered serial + its hover-start clock, and the
// currently rendered box (so we rebuild only on serial/revision change).
internal sealed class TooltipState
{
    public uint Serial;          // serial under the cursor this frame (0 = none)
    public float HoverStart;     // Time.Total when the current hover began
    public uint ShownSerial;     // serial the live box was built for
    public uint ShownRevision;   // revision the live box was built for
    public ulong Root;           // tooltip box root entity (0 = none)
    public int Width, Height;
}

// Marker on the tooltip box root.
internal struct TooltipRoot { }

internal readonly struct TooltipPlugin : IPlugin
{
    private const float Delay = 250f;     // legacy TooltipDelayBeforeDisplay
    private const int MaxWidth = 600;      // legacy clamps tooltip text to 600px
    private const int Pad = 4;             // legacy box = text + 4px each side
    private const int OffsetX = 8;         // box origin down-right of the cursor
    private const int OffsetY = 22;        //   point so it stays out of UiPick
    private static readonly ClayColor Background = new(0, 0, 0, 178); // black @ 0.7

    public void Build(App app)
    {
        app.AddResource(new TooltipState());

        var requestFn = SendRequests;
        app.AddSystem(requestFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var trackFn = TrackAndRender;
        app.AddSystem(trackFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var despawnFn = DespawnOnExit;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
    }

    // Drain the request queue into 0xD6 batches (15 serials max per packet,
    // CV_5090+) or per-serial 0xBF 0x10 on older clients. Mirrors legacy
    // PacketHandlers.SendMegaClilocRequests.
    private static void SendRequests(
        Res<NetClient> net,
        Res<GameContext> gameCtx,
        ResMut<ObjectPropertyLists> opl,
        Local<List<uint>> buf)
    {
        if (!opl.Value.HasPending) return;

        buf.Value ??= new List<uint>();
        buf.Value.Clear();
        opl.Value.DrainInto(buf.Value);
        var list = buf.Value;

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_5090)
        {
            for (int i = 0; i < list.Count; i += 15)
            {
                int count = Math.Min(15, list.Count - i);
                net.Value.Send_MegaClilocRequest(CollectionsMarshal.AsSpan(list).Slice(i, count));
            }
        }
        else
        {
            foreach (var s in list)
                net.Value.Send_MegaClilocRequest_Old(s);
        }
    }

    private static void TrackAndRender(
        Commands commands,
        Res<Time> time,
        Res<MouseContext> mouse,
        Res<SelectedEntity> selected,
        Res<GrabbedItem> grabbed,
        Res<DragGate> gate,
        Res<AssetsServer> assets,
        Res<UiSurface> surface,
        ResMut<ObjectPropertyLists> opl,
        ResMut<TooltipState> state,
        Query<Data<NetworkSerial, Notoriety>, Filter<Optional<Notoriety>>> worldQ,
        Query<Data<ContainerItemUI>> contQ,
        Query<Data<PaperdollSlot>> slotQ,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<Node>, With<TooltipRoot>> rootQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        var pos = mouse.Value.Position;

        // A UI item under the cursor (container slot, paperdoll equipment) owns
        // the tooltip — resolve it via the shared hit-test, same as pickup does.
        // Otherwise fall back to the world pick in SelectedEntity (ground items,
        // mobiles). The tooltip box is offset off the cursor point so it is never
        // the UiPick hit.
        uint serial = 0;
        var noto = NotorietyFlag.Unknown;
        var hit = UiPick.Topmost(pos, assets.Value, rendered);
        if (hit.Found)
        {
            if (contQ.Contains(hit.Entity)) { var (_, c) = contQ.Get(hit.Entity); serial = c.Ref.Serial; }
            // Slot bg / frame / icon all carry the equipped item's serial, so the
            // tooltip fires anywhere over the slot square, not only the icon art.
            else if (slotQ.Contains(hit.Entity)) { var (_, sl) = slotQ.Get(hit.Entity); serial = sl.Ref.ItemSerial; }
            // Non-item elements (e.g. a dragged spell cast button) carry a synthetic
            // tooltip serial on their render payload, pre-seeded in the OPL store.
            else if (rendered.Contains(hit.Entity))
            {
                var (_, _, _, uc, _, _) = rendered.Get(hit.Entity);
                if (uc.IsValid() && uc.Ref.Render() is { TooltipSerial: not 0 } r)
                    serial = r.TooltipSerial;
            }
        }
        // entity 0 is the null/sentinel id — Contains(0) can resolve to a stale
        // archetype entry, so the tooltip would show for "nothing" hovered.
        if (serial == 0 && selected.Value.Entity != 0 && worldQ.Contains(selected.Value.Entity))
        {
            var (_, s, n) = worldQ.Get(selected.Value.Entity);
            serial = s.Ref.Value;
            // Mobile names are coloured by notoriety (legacy ReadProperties).
            if (n.IsValid())
                noto = n.Ref.Value;
        }

        // Dragging suppresses the tooltip (legacy hides it while held): an item
        // pickup in flight, or any active window/forced drag (e.g. a spell cast
        // button just torn off the spellbook riding the cursor).
        if (grabbed.Value.IsActive || grabbed.Value.Serial != 0 || gate.Value.Mode != ActiveDrag.None)
            serial = 0;

        if (serial == 0)
        {
            HideBox(commands, state, childrenQ);
            state.Value.Serial = 0;
            return;
        }

        if (serial != state.Value.Serial)
        {
            state.Value.Serial = serial;
            state.Value.HoverStart = time.Value.Total;
            opl.Value.Request(serial);
            HideBox(commands, state, childrenQ);   // drop the old box until the new one is ready
        }

        if (time.Value.Total - state.Value.HoverStart < Delay)
            return;

        if (!opl.Value.TryGet(serial, out var entry))
        {
            opl.Value.Request(serial);   // delay elapsed but reply not in yet
            return;
        }

        string html = BuildHtml(serial, entry, noto);
        if (string.IsNullOrEmpty(html))
            return;

        bool needsBuild = state.Value.Root == 0
            || state.Value.ShownSerial != serial
            || state.Value.ShownRevision != entry.Revision;

        if (needsBuild)
        {
            HideBox(commands, state, childrenQ);

            int wrapWidth = entry.MaxWidth > 0 ? entry.MaxWidth : MaxWidth;
            var (w, h) = UoFontRenderer.Measure(html, font: 1, wrapWidth, isHtml: true, htmlStartColor: 0xFFFFFFFF, htmlBg: false, align: TEXT_ALIGN_TYPE.TS_CENTER);
            if (w <= 0 || h <= 0)
                return;

            int boxW = w + Pad * 2;
            int boxH = h + Pad * 2;
            var (bx, by) = ClampToScreen(pos, boxW, boxH, surface.Value);

            var root = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(bx),
                    Top = Val.Px(by),
                    Width = Val.Px(boxW),
                    Height = Val.Px(boxH),
                })
                .Insert(new BackgroundColor(Background))
                .Insert(new GlobalZIndex(short.MaxValue))   // always above every gump
                .Insert(new TooltipRoot());

            var label = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(Pad),
                    Top = Val.Px(Pad - 1),
                    Width = Val.Px(w),
                    Height = Val.Px(h),
                })
                .Insert(new UiCustom
                {
                    Data = new UOCustomRender
                    {
                        Kind = UOCustomKind.WrappedText,
                        Hue = Vector3.UnitZ,
                        Text = html,
                        TextFont = 1,
                        TextHue = 0,
                        WrapWidth = wrapWidth,
                        IsHtml = true,
                        HtmlStartColor = 0xFFFFFFFF,
                        HtmlBg = false,
                        TextCenter = true,
                    }
                });
            commands.AddChild(root.Id, label.Id);

            state.Value.Root = root.Id;
            state.Value.ShownSerial = serial;
            state.Value.ShownRevision = entry.Revision;
            state.Value.Width = boxW;
            state.Value.Height = boxH;
            return;   // box spawns deferred; reposition starts next frame
        }

        // Follow the cursor (in-place Node mutation; no structural change),
        // clamped so the box never spills past the window edges.
        var (cx, cy) = ClampToScreen(pos, state.Value.Width, state.Value.Height, surface.Value);
        foreach (var (_, node) in rootQ)
        {
            node.Ref.Left = Val.Px(cx);
            node.Ref.Top = Val.Px(cy);
        }
    }

    // Cursor + offset, clamped to the window (legacy Tooltip.Draw clamps to
    // ClientBounds). UiSurface.LogicalSize IS the Clay layout space — the same
    // logical pixels as the mouse / Node coords — so the box never spills past
    // the real window edges.
    private static (float X, float Y) ClampToScreen(Vector2 pos, int boxW, int boxH, UiSurface surface)
    {
        float w = surface.LogicalSize.X;
        float h = surface.LogicalSize.Y;
        float x = pos.X + OffsetX;
        float y = pos.Y + OffsetY;
        if (x < 0) x = 0; else if (x > w - boxW) x = w - boxW;
        if (y < 0) y = 0; else if (y > h - boxH) y = h - boxH;
        return (x, y);
    }

    // Name is coloured like legacy ReadProperties: items always yellow, mobiles
    // by notoriety. Property lines follow in white.
    private static string BuildHtml(uint serial, in ObjectPropertyLists.Entry e, NotorietyFlag noto)
    {
        var sb = new StringBuilder();
        bool item = ClassicUO.Game.SerialHelper.IsItem(serial);
        string startTag = item ? "<basefont color=\"yellow\">" : NotorietyHtmlTag(noto);

        if (!string.IsNullOrEmpty(e.Name))
        {
            sb.Append(startTag);
            sb.Append(e.Name);
            if (startTag.Length != 0) sb.Append("<basefont color=\"#FFFFFFFF\">");
        }

        if (!string.IsNullOrEmpty(e.Data))
        {
            if (sb.Length != 0) sb.Append('\n');
            sb.Append(e.Data);
        }

        return sb.ToString();
    }

    // Mirrors legacy Notoriety.GetHTMLHue.
    private static string NotorietyHtmlTag(NotorietyFlag flag) => flag switch
    {
        NotorietyFlag.Innocent => "<basefont color=\"cyan\">",
        NotorietyFlag.Ally => "<basefont color=\"lime\">",
        NotorietyFlag.Criminal or NotorietyFlag.Gray => "<basefont color=\"gray\">",
        NotorietyFlag.Enemy => "<basefont color=\"orange\">",
        NotorietyFlag.Murderer => "<basefont color=\"red\">",
        NotorietyFlag.Invulnerable => "<basefont color=\"yellow\">",
        _ => string.Empty,
    };

    private static void HideBox(Commands commands, ResMut<TooltipState> state, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (state.Value.Root == 0) return;
        DespawnSubtree(commands, state.Value.Root, childrenQ);
        state.Value.Root = 0;
        state.Value.ShownSerial = 0;
        state.Value.ShownRevision = 0;
    }

    private static void DespawnOnExit(
        Commands commands,
        ResMut<TooltipState> state,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        HideBox(commands, state, childrenQ);
        state.Value.Serial = 0;
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
}
