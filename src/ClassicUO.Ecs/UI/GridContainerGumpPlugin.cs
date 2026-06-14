using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Grid-container window (Profile.UseGridContainers). A resizable dark panel
// (WorldMapGump chrome) tagged ContainerWindow + ContainerGumpTag + UiMovable so
// the existing pickup / drop / double-click / drag / right-click-close systems
// drive it like a normal container. Items flow into a scrollable column of rows
// (Clay clips flowing children — absolute ones escape), each cell a fixed slot
// whose art fits-and-centres. Rebuilt only when the item set / search / sort /
// column-count changes (and never mid-drag).
internal struct GridContainerWindow { public uint Serial; }
internal struct GridContainerContent { public uint Serial; }
internal struct GridContainerRow { public ulong Window; }
internal struct GridContainerResizeHandle { public ulong Window; }
internal struct GridContainerSearchFrame { public uint Serial; }
internal struct GridContainerSortButton { public uint Serial; }
internal struct GridContainerCell { public uint Serial; public ushort Graphic; }

internal enum GridSort : byte { None, Name, Graphic }

// In-memory (this-session) per-container view: the item set, eager UI entity
// ids (valid the frame the window opens — mirrors ContainerUiMap), search
// field, sort, and a signature so the visual tree rebuilds only on change.
internal sealed class GridContainerState
{
    public sealed class View
    {
        public ulong Window;
        public ulong Content;
        public ulong SearchField;
        public GridSort Sort;
        public int LastSig = int.MinValue;
    }

    private readonly Dictionary<uint, View> _views = new();
    public View Get(uint serial) => _views.TryGetValue(serial, out var v) ? v : null;
    public View Create(uint serial) => _views[serial] = new View();
    public void Remove(uint serial) => _views.Remove(serial);
}

internal readonly struct GridContainerGumpPlugin : IPlugin
{
    internal const ushort CorpseGump = 0x0009;
    private const int Inset = 8;
    private const int HeaderH = 28;
    private const int Cell = 50;
    private const int Grip = 14;
    private const int SortW = 48;
    private const int HeaderGap = 4;
    private const int MinWin = 160, MaxWin = 900;

    private static int LastW = 320, LastH = 300;

    private static readonly ClayColor s_panelBg = new(26, 28, 34, 245);
    private static readonly ClayColor s_fieldBg = new(216, 218, 224, 255);
    private static readonly ClayColor s_btnBg = new(90, 96, 110, 255);
    private static readonly ClayColor s_grip = new(120, 124, 138, 255);
    private static readonly ClayColor s_text = new(235, 236, 240, 255);

    public void Build(App app)
    {
        var resizeFn = ResizeDrag;
        var openFn = OpenWindows;
        var rebuildFn = RebuildGrids;
        var closeFn = CloseWindows;

        app
            .AddResource(new GridContainerState())

            .AddSystem(resizeFn).InStage(Stage.PreUpdate).SingleThreaded()
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .Build()

            .AddSystem(openFn)
            .InStage(Stage.Update)
            .Label("cuo:gridcontainer:open")
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .RunIf((Res<Profile> p) => p.Value.UseGridContainers)
            .Build()

            .AddSystem(rebuildFn)
            .InStage(Stage.Update)
            .After("cuo:gridcontainer:open")
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .Build()

            .AddSystem(closeFn)
            .InStage(Stage.Update)
            .Build();
    }

    private static void OpenWindows(
        Commands commands,
        ResMut<GridContainerState> state,
        ResMut<UiZCounter> zCounter,
        EventReader<ContainerOpenedEvent> reader)
    {
        foreach (var ev in reader.Read())
        {
            if (ev.Graphic == 0xFFFF || ev.Graphic == 0x0030 || ev.Graphic == CorpseGump)
                continue;
            if (state.Value.Get(ev.Serial) != null)
                continue;

            int w = LastW, h = LastH;

            var root = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(120), Top = Val.Px(120),
                    Width = Val.Px(w), Height = Val.Px(h),
                })
                .Insert(new BackgroundColor(s_panelBg))
                .Insert(BorderRadius.All(8))
                .Insert(Interaction.None)
                .Insert<UOGump>()
                .Insert<UiMovable>()
                .Insert(new GlobalZIndex(zCounter.Value.Bump()))
                .Insert(new ContainerWindow { Serial = ev.Serial })
                // ContainerGumpTag makes PickupPlugin.DropItem recognise a drop
                // onto this window (Case 1). Bounds = content rect so the drop
                // coords clamp inside it; the grid re-lays out by row anyway.
                .Insert(new ContainerGumpTag
                {
                    Graphic = ev.Graphic,
                    OriginalGraphic = ev.Graphic,
                    Scale = 1f,
                    Bounds = new Rectangle(Inset, HeaderH, w - Inset * 2, h - HeaderH - Inset),
                })
                .Insert(new GridContainerWindow { Serial = ev.Serial });

            // Scrollable content column (rows of cells live here). Overflow.Scroll
            // clips the flowing rows to the panel and wheel-scrolls overflow.
            var content = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    FlexDirection = FlexDirection.Column,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(Inset), Top = Val.Px(HeaderH),
                    Width = Val.Px(w - Inset * 2),
                    Height = Val.Px(h - HeaderH - Inset),
                    Overflow = Overflow.Scroll,
                })
                .Insert(Interaction.None)
                .Insert(new GridContainerContent { Serial = ev.Serial });
            commands.AddChild(root.Id, content.Id);

            // Search field (filters by item name).
            var searchFrame = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(Inset), Top = Val.Px(5),
                    Width = Val.Px(w - Inset * 2 - SortW - HeaderGap), Height = Val.Px(18),
                })
                .Insert(new BackgroundColor(s_fieldBg))
                .Insert(BorderRadius.All(4))
                .Insert(new GridContainerSearchFrame { Serial = ev.Serial });
            var searchField = GuiPlugin.SpawnTextField(
                commands, searchFrame, new Vector2(5, 3),
                new TextFont { FontId = (ushort)(5 | UoFontRuntime.AsciiFlag), Size = 14 },
                UoFontRuntime.AsciiHue(1), string.Empty, masked: false);
            commands.AddChild(root.Id, searchFrame.Id);

            // Sort button (cycles None/Name/Graphic). Captured serial is an
            // immutable constant — safe in the observer.
            var serial = ev.Serial;
            var sortBtn = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(w - Inset - SortW), Top = Val.Px(5),
                    Width = Val.Px(SortW), Height = Val.Px(18),
                    JustifyContent = JustifyContent.Center,
                    AlignItems = AlignItems.Center,
                })
                .Insert(new BackgroundColor(s_btnBg))
                .Insert(BorderRadius.All(4))
                .Insert(new Text("Sort"))
                .Insert(new TextFont { FontId = 1, Size = 11 })
                .Insert(new TextColor(s_text))
                .Insert(Interaction.None)
                .Insert(new Button())
                .Insert(new GridContainerSortButton { Serial = ev.Serial })
                .Insert<UiNoWindowDrag>();
            sortBtn.Observe((On<UiClick> _, ResMut<GridContainerState> st) =>
            {
                var v = st.Value.Get(serial);
                if (v == null) return;
                v.Sort = (GridSort)(((int)v.Sort + 1) % 3);
                v.LastSig = int.MinValue; // force rebuild
            });
            commands.AddChild(root.Id, sortBtn.Id);

            // Resize grip (bottom-right) — ResizeDrag, scoped to this window.
            var grip = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(w - Grip - 2), Top = Val.Px(h - Grip - 2),
                    Width = Val.Px(Grip), Height = Val.Px(Grip),
                })
                .Insert(new BackgroundColor(s_grip))
                .Insert(BorderRadius.All(4))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>()
                .Insert(new GridContainerResizeHandle { Window = root.Id });
            commands.AddChild(root.Id, grip.Id);

            var view = state.Value.Create(ev.Serial);
            view.Window = root.Id;
            view.Content = content.Id;
            view.SearchField = searchField;
        }
    }

    // Maintain the per-view item set from the slot stream (the visual tree is
    // built by RebuildGrids). Reading the stream here keeps the rebuild off the
    // hot per-frame path.
    // Rebuild a window's row/cell tree when its signature (container children +
    // search + sort + columns) changes. Reads the container's CHILD ENTITIES
    // directly — NOT the transient ContainerSlotEvent stream, which the normal
    // container updater also drains — so population is robust to event timing.
    // Never rebuilds while an item is on the cursor so a drag isn't interrupted.
    private static void RebuildGrids(
        Commands commands,
        Res<GridContainerState> state,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<GrabbedItem> grabbed,
        EventReader<ContainerSlotEvent> slotEvents,
        Query<Data<Text>> textQ,
        Query<Data<GridContainerWindow, Node>> windowsQ,
        Query<Data<GridContainerRow>> rowsQ,
        Query<Data<TinyEcs.Parent, Graphic, Hue, Amount, NetworkSerial, EntityName>,
            Filter<With<ContainedInto>, Optional<Amount>, Optional<EntityName>>> itemsQ)
    {
        foreach (var ev in slotEvents.Read())
        {
            var touched = ev.ContainerSerial != 0 ? state.Value.Get(ev.ContainerSerial) : null;
            if (touched != null)
                touched.LastSig = int.MinValue;
        }

        if (grabbed.Value.Serial != 0)
            return;

        foreach (var (_, win, winNode) in windowsQ)
        {
            var view = state.Value.Get(win.Ref.Serial);
            if (view == null) continue;
            if (!entitiesMap.Value.TryGet(win.Ref.Serial, out var containerEnt))
                continue;

            int w = winNode.Ref.Width.Type == ValType.Px ? (int)winNode.Ref.Width.Value : LastW;
            int cols = Math.Max(1, (w - Inset * 2) / Cell);

            string query = "";
            if (textQ.TryGet(view.SearchField, out var tRow))
            {
                var (_, txt) = tRow;
                query = (txt.Ref.Value ?? "").Trim();
            }

            // Build the filtered + sorted display list from the container's
            // direct child items.
            var list = new List<(uint serial, ushort graphic, ushort hue, ushort amount, string name)>();
            foreach (var (parent, graphic, hue, amount, serial, name) in itemsQ)
            {
                if ((ulong)parent.Ref.Id != containerEnt)
                    continue;
                string nm = name.IsValid() ? name.Ref.Value : null;
                if (query.Length > 0 && (nm == null || !nm.Contains(query, StringComparison.OrdinalIgnoreCase)))
                    continue;
                var amt = amount.IsValid() ? amount.Ref.Value : 1;
                list.Add((serial.Ref.Value, graphic.Ref.Value, hue.Ref.Value, (ushort)Math.Clamp(amt, 1, ushort.MaxValue), nm));
            }

            if (view.Sort == GridSort.Name)
                list.Sort((a, b) => string.Compare(a.name ?? "", b.name ?? "", StringComparison.OrdinalIgnoreCase));
            else if (view.Sort == GridSort.Graphic)
                list.Sort((a, b) => a.graphic.CompareTo(b.graphic));
            else
                list.Sort((a, b) => a.serial.CompareTo(b.serial)); // stable order

            int sig = Sig(list, cols, query, view.Sort);
            if (sig == view.LastSig)
                continue;
            view.LastSig = sig;

            // Despawn this window's existing rows (cells cascade with the row).
            foreach (var (rowEnt, row) in rowsQ)
                if (row.Ref.Window == view.Window)
                    commands.Entity(rowEnt.Ref).Despawn();

            // Build fresh rows of cells.
            ulong rowId = 0;
            int colInRow = cols;
            foreach (var it in list)
            {
                if (colInRow >= cols)
                {
                    var row = commands.Spawn()
                        .Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Row, Width = Val.Auto, Height = Val.Px(Cell) })
                        .Insert(Interaction.None)
                        .Insert(new GridContainerRow { Window = view.Window });
                    commands.Entity(view.Content).AddChild(row.Id);
                    rowId = row.Id;
                    colInRow = 0;
                }

                var hueVec = ShaderHueTranslator.GetHueVector(it.hue, partial: false, alpha: 1f);
                var hoverHue = ShaderHueTranslator.GetHueVector(0x0035, partial: false, alpha: 1f);
                var cell = commands.Spawn()
                    .Insert(new Node { Display = Display.Flex, Width = Val.Px(Cell), Height = Val.Px(Cell) })
                    .Insert(new UiCustom
                    {
                        Data = new UOCustomRender
                        {
                            Kind = UOCustomKind.Art,
                            AssetId = ItemGraphics.Displayed(it.graphic, it.amount),
                            Hue = hueVec,
                        }
                    })
                    .Insert(Interaction.None)
                    // Whole-slot hit (scaled art can't be pixel-hit reliably):
                    // press/hover/tooltip resolve anywhere in the cell box.
                    .Insert<UiContainsByBounds>()
                    .Insert(new ContainerItemUI
                    {
                        Container = view.Window,
                        Serial = it.serial,
                        OriginalHue = hueVec,
                        HoverHue = hoverHue,
                    })
                    .Insert(new GridContainerCell { Serial = it.serial, Graphic = it.graphic });
                commands.Entity(rowId).AddChild(cell.Id);
                colInRow++;
            }
        }
    }

    private static int Sig(List<(uint serial, ushort graphic, ushort hue, ushort amount, string name)> list, int cols, string query, GridSort sort)
    {
        int h = 17;
        h = h * 31 + cols;
        h = h * 31 + (int)sort;
        h = h * 31 + (query?.GetHashCode() ?? 0);
        foreach (var it in list)
            h = h * 31 + (int)it.serial;
        return h;
    }

    private struct ResizeAnchor
    {
        public bool Active, Claimed;
        public ulong Window;
        public uint Serial;
        public Vector2 Mouse0;
        public float OriginW, OriginH;
    }

    private static void ResizeDrag(
        Res<MouseContext> mouse,
        ResMut<DragGate> gate,
        Local<ResizeAnchor> anchor,
        Query<Data<Node, ComputedNode, GridContainerResizeHandle>> gripQ,
        Query<Data<Node, GridContainerWindow>> windowsQ,
        Query<Data<Node, GridContainerContent>> contentQ,
        Query<Data<Node, GridContainerSearchFrame>> searchQ,
        Query<Data<Node, GridContainerSortButton>> sortQ)
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
            foreach (var (_, _, bb, grip) in gripQ)
            {
                if (pos.X < bb.Ref.Position.X || pos.Y < bb.Ref.Position.Y) continue;
                if (pos.X >= bb.Ref.Position.X + bb.Ref.Size.X || pos.Y >= bb.Ref.Position.Y + bb.Ref.Size.Y) continue;
                if (!windowsQ.TryGet(grip.Ref.Window, out var winRow)) continue;
                var (_, winNode, winTag) = winRow;
                gate.Value.Mode = ActiveDrag.UIWindow;
                anchor.Value = new ResizeAnchor
                {
                    Active = true,
                    Claimed = true,
                    Window = grip.Ref.Window,
                    Serial = winTag.Ref.Serial,
                    Mouse0 = pos,
                    OriginW = winNode.Ref.Width.Type == ValType.Px ? winNode.Ref.Width.Value : LastW,
                    OriginH = winNode.Ref.Height.Type == ValType.Px ? winNode.Ref.Height.Value : LastH,
                };
                break;
            }
        }

        if (!anchor.Value.Active || !windowsQ.TryGet(anchor.Value.Window, out var anchorRow))
            return;

        var delta = mouse.Value.Position - anchor.Value.Mouse0;
        int w = (int)Math.Clamp(anchor.Value.OriginW + delta.X, MinWin, MaxWin);
        int h = (int)Math.Clamp(anchor.Value.OriginH + delta.Y, MinWin, MaxWin);
        LastW = w; LastH = h;

        var (_, root, _) = anchorRow;
        root.Ref.Width = Val.Px(w);
        root.Ref.Height = Val.Px(h);

        // Only THIS window's content + grip (matched by serial / window id) —
        // never iterate-and-resize every grid window.
        foreach (var (_, node, c) in contentQ)
            if (c.Ref.Serial == anchor.Value.Serial)
            {
                node.Ref.Width = Val.Px(w - Inset * 2);
                node.Ref.Height = Val.Px(h - HeaderH - Inset);
            }
        foreach (var (_, node, _, grip) in gripQ)
            if (grip.Ref.Window == anchor.Value.Window)
            {
                node.Ref.Left = Val.Px(w - Grip - 2);
                node.Ref.Top = Val.Px(h - Grip - 2);
            }
        foreach (var (_, node, s) in searchQ)
            if (s.Ref.Serial == anchor.Value.Serial)
                node.Ref.Width = Val.Px(w - Inset * 2 - SortW - HeaderGap);
        foreach (var (_, node, s) in sortQ)
            if (s.Ref.Serial == anchor.Value.Serial)
                node.Ref.Left = Val.Px(w - Inset - SortW);
    }

    private static void CloseWindows(
        Commands commands,
        ResMut<GridContainerState> state,
        Query<Data<GridContainerWindow>> windowsQ,
        EventReader<ContainerClosedEvent> reader)
    {
        foreach (var ev in reader.Read())
        {
            foreach (var (winEnt, win) in windowsQ)
            {
                if (win.Ref.Serial != ev.Serial) continue;
                commands.Entity(winEnt.Ref).Despawn();
            }
            state.Value.Remove(ev.Serial);
        }
    }
}
