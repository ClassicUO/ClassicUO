// World map gump — ECS port of Game/UI/Gumps/WorldMapGump.cs.
//
// One movable window (modern dark panel, same palette as OptionsGumpPlugin)
// whose canvas child renders the whole facet through a single custom command
// (UOCustomKind.WorldMap): a background Task bakes land+statics radar colors
// with the legacy altitude shading into a full-map Texture2D, the renderer
// blits the zoomed/rotated viewport and stamps the overlay (markers, mobile /
// party / player dots + names + hp bars, grid, coordinates) — a straight port
// of legacy DrawAll. All view/show settings live in Profile.WorldMap*.
//
// Legacy's right-click ContextMenu is replaced by an options-gump-styled menu:
// the window root carries UiNoRightClickClose (legacy CanCloseWithRightClick =
// false), a PreUpdate gesture resolves the right-click through UiPick (the
// shared hit-test) and opens the menu as a child of the window at the cursor.
// Toggle rows write Profile directly (same observer pattern as options rows).
//
// Free view: left-drag pans the map (the canvas gets UiNoWindowDrag so the
// window-drag system yields); middle-drag enables free view and pans, mirrors
// legacy OnMouseDown. Wheel zooms over the legacy zoom table. Marker files
// (.csv / .usr / .map / .xml in Data/Client) load without icon textures —
// icon-only markers draw as colored dots (CurLoader's SDL surface path is not
// ported). Zones, multis (no live-house grid in ECS yet), goto/markers-manager
// gumps and positional targeting are not ported.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace ClassicUO.Ecs;

// Window root marker.
internal struct WorldMapWindow;

// The map canvas child (carries the custom render + the pan/zoom gestures).
internal struct WorldMapCanvas;

// Context-menu root marker (child of the window root).
internal struct WorldMapMenu;

// X close button (repositioned on resize).
internal struct WorldMapCloseBtn;

// Bottom-right drag-to-resize grip (legacy ResizableGump corner).
internal struct WorldMapResizeHandle { public ulong Window; }

internal sealed class WMapMarker
{
    public string Name;
    public int X, Y, MapId;
    public XnaColor Color;
    public int ZoomIndex;
}

internal sealed class WMapMarkerFile
{
    public string Name;
    public string FullPath;
    public List<WMapMarker> Markers = new();
    public bool IsEditable;
}

// Per-frame snapshot handed to the renderer through UOCustomRender.Tag. The
// build system rewrites it every frame; the renderer only reads. Reference
// semantics keep it zero-lookup at draw time (same trick as UOCustomRender).
internal sealed class WorldMapRenderData
{
    public struct Dot
    {
        public int WX, WY;
        public XnaColor Color;
        public string Name;     // null = no label
        public int HpPercent;   // -1 = no bar
    }

    public Texture2D Map;
    public bool Loading;
    public int MapIndex;
    public int CenterX, CenterY;
    public float Zoom = 1f;
    public int ZoomIndex;
    public bool Flip;
    public bool ShowGrid;
    public bool ShowMarkers;
    public bool ShowMarkerNames;
    public Vector2 MousePos;
    public List<WMapMarkerFile> MarkerFiles;
    public readonly List<Dot> Dots = new();
    public string TopText;
    public string BottomText;
}

internal sealed class WorldMapState
{
    public static readonly float[] Zooms = { 0.125f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 4f, 6f, 8f };

    public Texture2D MapTexture;
    public int BakedMap = -1;
    public int BakingMap = -1;
    public Task<uint[]> Bake;
    public int BakeW, BakeH;

    // View center in world tiles. Follows the player unless free view / pan.
    public int CenterX, CenterY;
    // Facet override while free-viewing another map; -1 = the player's map.
    public int ViewMap = -1;

    public bool Scrolling;
    public bool ScrollMiddle;
    public Vector2 ScrollMouseAnchor;
    public int ScrollCenterX, ScrollCenterY;

    public List<WMapMarkerFile> MarkerFiles = new();
    public bool MarkersLoaded;

    // Context-menu rebuild request (a toggle row changed a Profile flag).
    public bool MenuDirty;
    public Vector2 MenuPos;      // window-relative spawn position
    public ulong MenuWindow;     // owning window root

    public readonly WorldMapRenderData Render = new();

    public void DisposeTexture()
    {
        MapTexture?.Dispose();
        MapTexture = null;
        Render.Map = null;
        BakedMap = -1;
        BakingMap = -1;
        Bake = null;
    }
}

internal readonly struct WorldMapGumpPlugin : IPlugin
{
    private const int DOT_SIZE = 4;
    private const int DOT_SIZE_HALF = DOT_SIZE >> 1;
    private const int BAR_MAX_WIDTH = 25;
    private const int BAR_MAX_HEIGHT = 3;
    private const int CANVAS_INSET = 4;
    private const ushort NAME_FONT = 0;
    private const int GripSize = 14;
    private const int MinWinSize = 160;
    private const int MaxWinSize = 1200;

    // Options-gump palette (OptionsGumpPlugin) so the menu reads as one family.
    private static readonly ClayColor s_panelBg = new(26, 28, 34, 245);
    private static readonly ClayColor s_rowBg = new(44, 46, 56, 255);
    private static readonly ClayColor s_toggleOn = new(70, 160, 90, 255);
    private static readonly ClayColor s_toggleOff = new(90, 92, 104, 255);
    private static readonly ClayColor s_textMain = new(235, 236, 240, 255);
    private static readonly ClayColor s_textDim = new(160, 163, 175, 255);

    private static readonly XnaColor s_gridColor = new(255, 255, 255, 56);

    private static readonly string[] s_facetNames =
        { "Felucca", "Trammel", "Ilshenar", "Malas", "Tokuno", "TerMur" };

    private static readonly string s_markersPath =
        Path.Combine(AppContext.BaseDirectory, "Data", "Client");
    private static readonly string s_userMarkersPath =
        Path.Combine(s_markersPath, "userMarkers.usr");

    public void Build(App app)
    {
        app.AddResource(new WorldMapState());

        var bakeFn = BakeMap;
        var inputFn = PanZoomInput;
        var wheelFn = WheelZoom;
        var resizeFn = ResizeDrag;
        var buildFn = BuildRenderData;
        var syncDragFn = SyncCanvasDragOptOut;
        var menuFn = MenuGesture;
        var refreshFn = RefreshMenu;
        var despawnFn = DespawnAll;

        bool InGame(Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen;

        // Wheel zoom must read the wheel before GuiPlugin's
        // RouteWheelToScrollable consumes it over any gump (Stage.First).
        app.AddSystem(wheelFn).InStage(Stage.First).SingleThreaded().Before("cuo:route_wheel")
            .RunIf((Res<State<GameState>> s) => InGame(s)).Build();
        app.AddSystem(menuFn).InStage(Stage.PreUpdate).SingleThreaded()
            .RunIf((Res<State<GameState>> s) => InGame(s)).Build();
        // PreUpdate + DragGate claim so a press on the grip never also latches
        // a window move (WindowDragPlugin runs in Update and respects the gate).
        app.AddSystem(resizeFn).InStage(Stage.PreUpdate).SingleThreaded()
            .RunIf((Res<State<GameState>> s) => InGame(s)).Build();
        app.AddSystem(inputFn).InStage(Stage.Update).SingleThreaded()
            .RunIf((Res<State<GameState>> s) => InGame(s)).Build();
        app.AddSystem(bakeFn).InStage(Stage.Update).SingleThreaded()
            .RunIf((Res<State<GameState>> s) => InGame(s)).Build();
        app.AddSystem(buildFn).InStage(Stage.Update).SingleThreaded()
            .RunIf((Res<State<GameState>> s) => InGame(s)).Build();
        app.AddSystem(syncDragFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => InGame(s)).Build();
        app.AddSystem(refreshFn).InStage(Stage.Update).SingleThreaded()
            .RunIf((Res<State<GameState>> s) => InGame(s)).Build();
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
    }

    // ── Window ────────────────────────────────────────────────────────────

    public static void OpenOrFocus(
        Commands commands,
        WorldMapState state,
        Profile profile,
        UiZCounter zCounter,
        Query<Data<WorldMapWindow>> existingQ)
    {
        foreach (var (ent, _) in existingQ)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
            return;
        }

        int w = Math.Max(160, profile.WorldMapWidth);
        int h = Math.Max(160, profile.WorldMapHeight);

        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(100),
                Top = Val.Px(60),
                Width = Val.Px(w),
                Height = Val.Px(h),
            })
            .Insert(new BackgroundColor(s_panelBg))
            .Insert(BorderRadius.All(8))
            .Insert(Interaction.None)
            .Insert<UOGump>()
            .Insert<UiMovable>()
            // Right-click opens the context menu instead of closing (legacy
            // CanCloseWithRightClick = false); close lives in the menu / X.
            .Insert<UiNoRightClickClose>()
            .Insert(new GlobalZIndex(zCounter.Bump()))
            .Insert<WorldMapWindow>();
        ulong rootId = root.Id;

        var canvas = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(CANVAS_INSET),
                Top = Val.Px(CANVAS_INSET),
                Width = Val.Px(w - CANVAS_INSET * 2),
                Height = Val.Px(h - CANVAS_INSET * 2),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.WorldMap,
                    Hue = Vector3.UnitZ,
                    Tag = state.Render,
                }
            })
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<WorldMapCanvas>();
        commands.AddChild(rootId, canvas.Id);

        // X close button (right-click is taken by the menu).
        var close = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(w - 22),
                Top = Val.Px(4),
                Width = Val.Px(18),
                Height = Val.Px(18),
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            })
            .Insert(new BackgroundColor(new ClayColor(140, 60, 60, 220)))
            .Insert(BorderRadius.All(4))
            .Insert(new Text("X"))
            .Insert(new TextFont { FontId = 1, Size = 11 })
            .Insert(new TextColor(s_textMain))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>()
            .Insert<WorldMapCloseBtn>()
            .Observe((On<UiClick> _, Commands cmd, Query<Data<WorldMapWindow>> winQ, Query<Data<TinyEcs.Children>> kidsQ) =>
            {
                foreach (var (ent, _) in winQ)
                    UiHierarchy.DespawnSubtree(cmd, ent.Ref, kidsQ);
            });
        commands.AddChild(rootId, close.Id);

        // Resize grip: bottom-right corner, dragged by ResizeDrag (PreUpdate,
        // DragGate claim — same pattern as JournalPlugin's expander).
        var grip = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(w - GripSize - 2),
                Top = Val.Px(h - GripSize - 2),
                Width = Val.Px(GripSize),
                Height = Val.Px(GripSize),
            })
            .Insert(new BackgroundColor(s_toggleOff))
            .Insert(BorderRadius.All(4))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>()
            .Insert(new WorldMapResizeHandle { Window = rootId });
        commands.AddChild(rootId, grip.Id);
    }

    private static void DespawnAll(
        Commands commands,
        ResMut<WorldMapState> state,
        Query<Data<WorldMapWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ)
            UiHierarchy.DespawnSubtree(commands, ent.Ref, childrenQ);
        state.Value.DisposeTexture();
        state.Value.Scrolling = false;
        state.Value.ViewMap = -1;
    }

    // ── Map bake ──────────────────────────────────────────────────────────

    // Kicks the background bake when the viewed facet has no texture yet, and
    // uploads the finished buffer. The bake reads the map/statics files through
    // its own UOFile handles (legacy LoadMap's "2 sources at once" workaround)
    // so the worker never races the main thread's chunk streaming.
    private static void BakeMap(
        Res<GameContext> gameCtx,
        Res<GraphicsDevice> device,
        Res<UOFileManager> files,
        ResMut<WorldMapState> stateRes,
        Query<Data<WorldMapWindow>> windowsQ)
    {
        var state = stateRes.Value;

        bool anyWindow = false;
        foreach (var _ in windowsQ) { anyWindow = true; break; }
        if (!anyWindow) return;

        int desired = state.ViewMap >= 0 ? state.ViewMap : gameCtx.Value.Map;
        if (desired < 0 || desired >= MapLoader.MAPS_COUNT) return;

        // Upload a finished bake.
        if (state.Bake != null && state.Bake.IsCompleted)
        {
            var buffer = state.Bake.IsCompletedSuccessfully ? state.Bake.Result : null;
            int baked = state.BakingMap;
            state.Bake = null;
            state.BakingMap = -1;
            if (buffer != null)
            {
                state.MapTexture?.Dispose();
                state.MapTexture = new Texture2D(device.Value, state.BakeW, state.BakeH);
                state.MapTexture.SetData(buffer);
                state.BakedMap = baked;
            }
        }

        if (state.BakedMap == desired || state.BakingMap >= 0)
            return;

        int realWidth = files.Value.Maps.MapsDefaultSize[desired, 0];
        int realHeight = files.Value.Maps.MapsDefaultSize[desired, 1];
        state.BakeW = realWidth + 2;
        state.BakeH = realHeight + 2;
        state.BakingMap = desired;
        var filesCapture = files.Value;
        state.Bake = Task.Run(() => BakeBuffer(filesCapture, desired));
    }

    // Legacy LoadMap pixel generation: land radar color per tile, topmost
    // static wins the cell, then the two-row altitude shading pass. Output is
    // a (w+2)x(h+2) ABGR buffer with a 1px transparent border (OFFSET_PIX).
    private static uint[] BakeBuffer(UOFileManager files, int mapIndex)
    {
        const int OFFSET_PIX = 2;
        const int OFFSET_PIX_HALF = 1;

        var maps = files.Maps;
        var hues = files.Hues;
        var tileData = files.TileData;

        int realWidth = maps.MapsDefaultSize[mapIndex, 0];
        int realHeight = maps.MapsDefaultSize[mapIndex, 1];
        int fixedWidth = maps.MapBlocksSize[mapIndex, 0];
        int fixedHeight = maps.MapBlocksSize[mapIndex, 1];

        int stride = realWidth + OFFSET_PIX;
        int size = stride * (realHeight + OFFSET_PIX);
        var buffer = new uint[size];
        var allZ = new sbyte[size];
        var staticBlocks = new StaticsBlock[64];

        ClassicUO.IO.UOFile fileMap = null;
        ClassicUO.IO.UOFile fileStatics = null;
        try
        {
            for (int bx = 0; bx < fixedWidth; ++bx)
            {
                int mapX = bx << 3;

                for (int by = 0; by < fixedHeight; ++by)
                {
                    ref var indexMap = ref maps.GetIndex(mapIndex, bx, by);
                    if (!indexMap.IsValid())
                        continue;

                    fileMap ??= new ClassicUO.IO.UOFile(indexMap.MapFile.FilePath);
                    fileMap.Seek((long)indexMap.MapAddress, SeekOrigin.Begin);
                    var cells = fileMap.Read<MapBlock>().Cells;

                    int mapY = by << 3;

                    for (int y = 0; y < 8; ++y)
                    {
                        int block = (mapY + y + OFFSET_PIX_HALF) * stride + mapX + OFFSET_PIX_HALF;
                        int pos = y << 3;

                        for (int x = 0; x < 8; ++x, ++pos, ++block)
                        {
                            ushort color = (ushort)(0x8000 | hues.GetRadarColorData(cells[pos].TileID & 0x3FFF));
                            buffer[block] = HuesHelper.Color16To32(color) | 0xFF_00_00_00;
                            allZ[block] = cells[pos].Z;
                        }
                    }

                    if (indexMap.StaticCount == 0)
                        continue;

                    fileStatics ??= new ClassicUO.IO.UOFile(indexMap.StaticFile.FilePath);
                    fileStatics.Seek((long)indexMap.StaticAddress, SeekOrigin.Begin);

                    if (staticBlocks.Length < indexMap.StaticCount)
                        staticBlocks = new StaticsBlock[indexMap.StaticCount];

                    var span = staticBlocks.AsSpan(0, (int)indexMap.StaticCount);
                    fileStatics.Read(MemoryMarshal.AsBytes(span));

                    foreach (ref var sb in span)
                    {
                        if (sb.Color == 0 || sb.Color == 0xFFFF || !CanBeDrawn(tileData, sb.Color))
                            continue;

                        int block = (mapY + sb.Y + OFFSET_PIX_HALF) * stride + mapX + sb.X + OFFSET_PIX_HALF;
                        if (sb.Z >= allZ[block])
                        {
                            ushort color = (ushort)(0x8000 | (sb.Hue != 0
                                ? hues.GetColor16(16384, sb.Hue)
                                : hues.GetRadarColorData(sb.Color + 0x4000)));
                            buffer[block] = HuesHelper.Color16To32(color) | 0xFF_00_00_00;
                            allZ[block] = sb.Z;
                        }
                    }
                }
            }
        }
        finally
        {
            fileMap?.Dispose();
            fileStatics?.Dispose();
        }

        // Altitude shading: compare each tile against its south neighbour and
        // darken/lighten (legacy MAG_0 / MAG_1).
        const float MAG_0 = 80f / 100f;
        const float MAG_1 = 100f / 80f;
        for (int mapY = 1; mapY < realHeight - 1; ++mapY)
        {
            int blockCurrent = (mapY + OFFSET_PIX_HALF) * stride + OFFSET_PIX_HALF;
            int blockNext = (mapY + 1 + OFFSET_PIX_HALF) * stride + OFFSET_PIX_HALF;

            for (int mapX = 1; mapX < realWidth - 1; ++mapX)
            {
                sbyte z0 = allZ[++blockCurrent];
                sbyte z1 = allZ[blockNext++];
                if (z0 == z1)
                    continue;

                ref uint cc = ref buffer[blockCurrent];
                if (cc == 0)
                    continue;

                byte r = (byte)(cc & 0xFF);
                byte g = (byte)((cc >> 8) & 0xFF);
                byte b = (byte)((cc >> 16) & 0xFF);
                byte a = (byte)((cc >> 24) & 0xFF);

                if (r != 0 || g != 0 || b != 0)
                {
                    float mag = z0 < z1 ? MAG_0 : MAG_1;
                    r = (byte)Math.Min(0xFF, (int)(r * mag));
                    g = (byte)Math.Min(0xFF, (int)(g * mag));
                    b = (byte)Math.Min(0xFF, (int)(b * mag));
                    cc = (uint)(r | (g << 8) | (b << 16) | (a << 24));
                }
            }
        }

        return buffer;
    }

    // MiniMapPlugin's radar subset of GameObject.CanBeDrawn — keeps no-draw /
    // decorative statics from speckling the bake.
    private static bool CanBeDrawn(TileDataLoader tileData, ushort g)
    {
        switch (g)
        {
            case 0x0001:
            case 0x21BC:
            case 0xA1FE:
            case 0xA1FF:
            case 0xA200:
            case 0xA201:
                return false;

            case 0x9E4C:
            case 0x9E64:
            case 0x9E65:
            case 0x9E7D:
            {
                ref var d = ref tileData.StaticData[g];
                return !d.IsBackground && !d.IsSurface;
            }
        }

        if (g == 0x63D3)
            return false;
        if (g >= 0x2198 && g <= 0x21A4)
            return false;

        if (g < tileData.StaticData.Length)
        {
            ref var data = ref tileData.StaticData[g];
            if (!string.IsNullOrEmpty(data.Name) &&
                data.Name.StartsWith("nodraw", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!data.IsNoDiagonal)
                return true;
        }

        return false;
    }

    // ── Input: pan + zoom ─────────────────────────────────────────────────

    private static void PanZoomInput(
        Res<MouseContext> mouse,
        Res<Profile> profile,
        Res<UOFileManager> files,
        Res<GameContext> gameCtx,
        Res<DragGate> gate,
        ResMut<WorldMapState> stateRes,
        Query<Data<ComputedNode>, Filter<With<WorldMapCanvas>>> canvasQ,
        Query<Data<ComputedNode>, Filter<With<WorldMapMenu>>> menuQ)
    {
        var state = stateRes.Value;

        bool haveCanvas = false;
        float cx = 0, cy = 0, cw = 0, ch = 0;
        foreach (var (_, bb) in canvasQ)
        {
            cx = bb.Ref.Position.X;
            cy = bb.Ref.Position.Y;
            cw = bb.Ref.Size.X;
            ch = bb.Ref.Size.Y;
            haveCanvas = true;
            break;
        }
        if (!haveCanvas)
        {
            state.Scrolling = false;
            return;
        }

        var pos = mouse.Value.Position;
        bool inside = pos.X >= cx && pos.Y >= cy && pos.X < cx + cw && pos.Y < cy + ch;

        bool menuOpen = false;
        foreach (var _ in menuQ) { menuOpen = true; break; }

        int mapIndex = state.ViewMap >= 0 ? state.ViewMap : gameCtx.Value.Map;
        if (mapIndex < 0) return;
        float zoom = WorldMapState.Zooms[Math.Clamp(profile.Value.WorldMapZoomIndex, 0, WorldMapState.Zooms.Length - 1)];

        bool leftHeld = mouse.Value.IsPressed(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Left);
        bool midHeld = mouse.Value.IsPressed(MouseButtonType.Middle) || mouse.Value.IsPressedOnce(MouseButtonType.Middle);

        // Latch a pan: left needs free view (legacy also takes Alt; free view
        // covers it here), middle always pans and enables free view. The gate
        // check yields to the resize grip / window drag, which claim it in
        // PreUpdate (the grip overlaps the canvas corner).
        if (!state.Scrolling && inside && !menuOpen && gate.Value.Mode == ActiveDrag.None)
        {
            bool latchLeft = mouse.Value.IsPressedOnce(MouseButtonType.Left) && profile.Value.WorldMapFreeView;
            bool latchMid = mouse.Value.IsPressedOnce(MouseButtonType.Middle);
            if (latchLeft || latchMid)
            {
                if (latchMid)
                    profile.Value.WorldMapFreeView = true;
                state.Scrolling = true;
                state.ScrollMiddle = latchMid;
                state.ScrollMouseAnchor = pos;
                state.ScrollCenterX = state.CenterX;
                state.ScrollCenterY = state.CenterY;
            }
        }

        if (state.Scrolling)
        {
            bool held = state.ScrollMiddle ? midHeld : leftHeld;
            if (!held)
            {
                state.Scrolling = false;
            }
            else
            {
                var delta = pos - state.ScrollMouseAnchor;
                var scroll = RotatePoint((int)delta.X, (int)delta.Y, 1f / zoom, -1,
                    profile.Value.WorldMapFlipMap ? 45f : 0f);
                state.CenterX = Math.Clamp(state.ScrollCenterX - scroll.X, 0, files.Value.Maps.MapsDefaultSize[mapIndex, 0]);
                state.CenterY = Math.Clamp(state.ScrollCenterY - scroll.Y, 0, files.Value.Maps.MapsDefaultSize[mapIndex, 1]);
            }
        }
    }

    private struct ResizeAnchor
    {
        public bool Active, Claimed;
        public ulong Window;
        public Vector2 Mouse0;
        public float OriginW, OriginH;
    }

    // Drag the corner grip to resize the window (legacy ResizableGump). Bbox
    // latch on press-once + DragGate claim (mirrors JournalPlugin.ResizeDrag);
    // size applies live to the root/canvas/close/grip nodes and persists to
    // Profile.WorldMapWidth/Height.
    private static void ResizeDrag(
        Res<MouseContext> mouse,
        Res<DragGate> gate,
        Res<Profile> profile,
        Local<ResizeAnchor> anchor,
        Query<Data<Node, ComputedNode, WorldMapResizeHandle>> gripQ,
        Query<Data<Node>, Filter<With<WorldMapWindow>>> windowsQ,
        Query<Data<Node>, Filter<With<WorldMapCanvas>>> canvasQ,
        Query<Data<Node>, Filter<With<WorldMapCloseBtn>>> closeQ)
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
                var (_, winNode) = winRow;
                gate.Value.Mode = ActiveDrag.UIWindow;
                anchor.Value = new ResizeAnchor
                {
                    Active = true,
                    Claimed = true,
                    Window = grip.Ref.Window,
                    Mouse0 = pos,
                    OriginW = winNode.Ref.Width.Type == ValType.Px ? winNode.Ref.Width.Value : profile.Value.WorldMapWidth,
                    OriginH = winNode.Ref.Height.Type == ValType.Px ? winNode.Ref.Height.Value : profile.Value.WorldMapHeight,
                };
                break;
            }
        }

        if (!anchor.Value.Active || !windowsQ.TryGet(anchor.Value.Window, out var anchorRow))
            return;

        var delta = mouse.Value.Position - anchor.Value.Mouse0;
        int w = (int)Math.Clamp(anchor.Value.OriginW + delta.X, MinWinSize, MaxWinSize);
        int h = (int)Math.Clamp(anchor.Value.OriginH + delta.Y, MinWinSize, MaxWinSize);

        var (_, root) = anchorRow;
        root.Ref.Width = Val.Px(w);
        root.Ref.Height = Val.Px(h);

        foreach (var (_, node) in canvasQ)
        {
            node.Ref.Width = Val.Px(w - CANVAS_INSET * 2);
            node.Ref.Height = Val.Px(h - CANVAS_INSET * 2);
        }
        foreach (var (_, node) in closeQ)
            node.Ref.Left = Val.Px(w - 22);
        foreach (var (_, node, _, _) in gripQ)
        {
            node.Ref.Left = Val.Px(w - GripSize - 2);
            node.Ref.Top = Val.Px(h - GripSize - 2);
        }

        profile.Value.WorldMapWidth = w;
        profile.Value.WorldMapHeight = h;
    }

    // Wheel zoom over the canvas (legacy OnMouseWheel over the zoom table).
    // Stage.First, ordered before GuiPlugin's RouteWheelToScrollable — that
    // pass consumes the wheel over ANY gump (to stop camera zoom), which would
    // zero the reading before an Update-stage handler ever saw it.
    private static void WheelZoom(
        ResMut<MouseContext> mouse,
        Res<Profile> profile,
        Query<Data<ComputedNode>, Filter<With<WorldMapCanvas>>> canvasQ,
        Query<Data<WorldMapMenu>> menuQ)
    {
        if (mouse.Value.WheelConsumed || mouse.Value.Wheel == 0)
            return;
        foreach (var _ in menuQ)
            return;

        var pos = mouse.Value.Position;
        foreach (var (_, bb) in canvasQ)
        {
            if (pos.X < bb.Ref.Position.X || pos.Y < bb.Ref.Position.Y ||
                pos.X >= bb.Ref.Position.X + bb.Ref.Size.X || pos.Y >= bb.Ref.Position.Y + bb.Ref.Size.Y)
                return;

            int idx = Math.Clamp(profile.Value.WorldMapZoomIndex + (mouse.Value.Wheel > 0 ? 1 : -1),
                0, WorldMapState.Zooms.Length - 1);
            profile.Value.WorldMapZoomIndex = idx;
            mouse.Value.ConsumeWheel();
            return;
        }
    }

    // Free view flips the canvas's UiNoWindowDrag so a press on the map pans
    // instead of dragging the window; with free view off the canvas is a normal
    // opaque child and dragging it moves the window (legacy CanMove dance).
    private static void SyncCanvasDragOptOut(
        Commands commands,
        Res<Profile> profile,
        Local<int> applied, // 0 = unknown, 1 = on, 2 = off
        Query<Data<Node>, Filter<With<WorldMapCanvas>>> canvasQ)
    {
        int want = profile.Value.WorldMapFreeView ? 1 : 2;
        if (applied.Value == want) return;

        bool any = false;
        foreach (var (ent, _) in canvasQ)
        {
            any = true;
            if (want == 1)
                commands.Entity(ent.Ref).Insert<UiNoWindowDrag>();
            else
                commands.Entity(ent.Ref).Remove<UiNoWindowDrag>();
        }
        if (any)
            applied.Value = want;
        else
            applied.Value = 0; // window closed — re-apply on next open
    }

    // ── Per-frame render data ─────────────────────────────────────────────

    private static void BuildRenderData(
        Res<Profile> profile,
        Res<GameContext> gameCtx,
        Res<MouseContext> mouse,
        Res<UOFileManager> files,
        Res<PartyState> party,
        ResMut<WorldMapState> stateRes,
        Query<Data<WorldPosition>, Filter<With<Player>>> playerQ,
        Query<Data<WorldPosition, NetworkSerial>, Filter<With<Mobiles>, Without<Player>>> mobileQ,
        Query<Data<Hits>> hitsQ,
        Query<Data<Notoriety>> notorietyQ,
        Query<Data<EntityName>> nameQ,
        Query<Data<ComputedNode>, Filter<With<WorldMapCanvas>>> canvasQ)
    {
        var state = stateRes.Value;
        var d = state.Render;
        var p = profile.Value;

        bool haveCanvas = false;
        float cx = 0, cy = 0, cw = 0, ch = 0;
        foreach (var (_, bb) in canvasQ)
        {
            cx = bb.Ref.Position.X;
            cy = bb.Ref.Position.Y;
            cw = bb.Ref.Size.X;
            ch = bb.Ref.Size.Y;
            haveCanvas = true;
            break;
        }
        if (!haveCanvas)
            return;

        if (!state.MarkersLoaded)
        {
            LoadMarkers(state);
            state.MarkersLoaded = true;
        }

        int px = int.MinValue, py = int.MinValue;
        sbyte pz = 0;
        foreach (var (_, pos) in playerQ)
        {
            px = pos.Ref.X;
            py = pos.Ref.Y;
            pz = pos.Ref.Z;
            break;
        }

        int mapIndex = state.ViewMap >= 0 ? state.ViewMap : gameCtx.Value.Map;
        bool onViewedMap = state.ViewMap < 0 || state.ViewMap == gameCtx.Value.Map;

        // Follow the player unless panning / free view (legacy AddToRenderLists).
        if (!state.Scrolling && !p.WorldMapFreeView && px != int.MinValue)
        {
            state.CenterX = px;
            state.CenterY = py;
        }

        int zoomIndex = Math.Clamp(p.WorldMapZoomIndex, 0, WorldMapState.Zooms.Length - 1);

        d.Map = state.BakedMap == mapIndex ? state.MapTexture : null;
        d.Loading = state.BakingMap == mapIndex;
        d.MapIndex = mapIndex;
        d.CenterX = state.CenterX;
        d.CenterY = state.CenterY;
        d.Zoom = WorldMapState.Zooms[zoomIndex];
        d.ZoomIndex = zoomIndex;
        d.Flip = p.WorldMapFlipMap;
        d.ShowGrid = p.WorldMapShowGridIfZoomed && d.Zoom >= 4f;
        d.ShowMarkers = p.WorldMapShowMarkers;
        d.ShowMarkerNames = p.WorldMapShowMarkersNames;
        d.MousePos = mouse.Value.Position;
        d.MarkerFiles = state.MarkerFiles;

        d.Dots.Clear();

        // Mobiles: party yellow (name/bar per group flags), allies lime, the
        // rest red (legacy notoriety split).
        if (onViewedMap)
        {
            foreach (var (ent, pos, serial) in mobileQ)
            {
                bool isParty = p.WorldMapShowParty && party.Value.Contains(serial.Ref.Value);
                if (!isParty && !p.WorldMapShowMobiles)
                    continue;

                var dot = new WorldMapRenderData.Dot
                {
                    WX = pos.Ref.X,
                    WY = pos.Ref.Y,
                    Color = XnaColor.Red,
                    Name = null,
                    HpPercent = -1,
                };

                if (isParty)
                {
                    dot.Color = XnaColor.Yellow;
                    if (p.WorldMapShowGroupName && nameQ.TryGet(ent.Ref, out var nameRow))
                    {
                        var (_, name) = nameRow;
                        dot.Name = name.Ref.Value;
                    }
                    if (p.WorldMapShowGroupBar)
                        dot.HpPercent = HpPercent(hitsQ, ent.Ref);
                }
                else if (notorietyQ.TryGet(ent.Ref, out var notoRow))
                {
                    var (_, noto) = notoRow;
                    if (noto.Ref.Value == NotorietyFlag.Ally)
                    {
                        dot.Color = XnaColor.Lime;
                        if (p.WorldMapShowGroupName && nameQ.TryGet(ent.Ref, out var nameRow2))
                        {
                            var (_, name) = nameRow2;
                            dot.Name = name.Ref.Value;
                        }
                        if (p.WorldMapShowGroupBar)
                            dot.HpPercent = HpPercent(hitsQ, ent.Ref);
                    }
                }

                d.Dots.Add(dot);
            }

            // Player last so it always draws on top.
            if (px != int.MinValue)
            {
                int playerHp = -1;
                if (p.WorldMapShowPlayerBar)
                {
                    foreach (var (ent, _) in playerQ)
                    {
                        playerHp = HpPercent(hitsQ, ent.Ref);
                        break;
                    }
                }
                d.Dots.Add(new WorldMapRenderData.Dot
                {
                    WX = px,
                    WY = py,
                    Color = XnaColor.White,
                    Name = p.WorldMapShowPlayerName ? gameCtx.Value.PlayerName : null,
                    HpPercent = playerHp,
                });
            }
        }

        // Coordinates overlay (top-left) + mouse coordinates (bottom-left).
        d.TopText = null;
        d.BottomText = null;
        int mapW = files.Value.Maps.MapsDefaultSize[mapIndex, 0];
        int mapH = files.Value.Maps.MapsDefaultSize[mapIndex, 1];

        if (p.WorldMapShowCoordinates && px != int.MinValue)
        {
            d.TopText = $"{px}, {py} ({pz}) [{zoomIndex}]";
            if (p.WorldMapShowSextantCoordinates &&
                SextantFormat(mapIndex, mapW, mapH, px, py, out var sex))
                d.TopText += "\n" + sex;
        }

        var mpos = mouse.Value.Position;
        if (p.WorldMapShowMouseCoordinates &&
            mpos.X >= cx && mpos.Y >= cy && mpos.X < cx + cw && mpos.Y < cy + ch)
        {
            CanvasToWorld((int)(mpos.X - cx), (int)(mpos.Y - cy), (int)cw, (int)ch,
                state.CenterX, state.CenterY, d.Zoom, d.Flip, out int wx, out int wy);
            d.BottomText = $"{wx} {wy}";
            if (p.WorldMapShowSextantCoordinates &&
                SextantFormat(mapIndex, mapW, mapH, wx, wy, out var sex2))
                d.BottomText += "\n" + sex2;
        }
    }

    private static int HpPercent(Query<Data<Hits>> hitsQ, ulong ent)
    {
        if (!hitsQ.TryGet(ent, out var row))
            return -1;
        var (_, hits) = row;
        if (hits.Ref.MaxValue == 0)
            return -1;
        return Math.Clamp(hits.Ref.Value * 100 / hits.Ref.MaxValue, 0, 100);
    }

    // ── Context menu ──────────────────────────────────────────────────────

    // Right-click released over the world map window (resolved through the
    // shared UiPick hit-test) opens the menu at the cursor; any press outside
    // an open menu dismisses it. PreUpdate so the press never reaches the
    // player-walk systems.
    private static void MenuGesture(
        Commands commands,
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Res<Profile> profile,
        Res<UiZCounter> zCounter,
        Res<UiSurface> surface,
        Res<GameContext> gameCtx,
        ResMut<WorldMapState> stateRes,
        Local<ulong> pressTarget,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> renderedQ,
        Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>> movablesQ,
        Query<Data<TinyEcs.Parent>> parentsQ,
        Query<Data<UiContainsByBounds>> boundsQ,
        Query<Data<WorldMapWindow>> windowsQ,
        Query<Data<ComputedNode>, Filter<With<WorldMapMenu>>> menuQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        var state = stateRes.Value;
        var pos = mouse.Value.Position;

        // Open menu: press-outside dismisses (same as the options dropdown).
        bool menuOpen = false;
        foreach (var (ment, mbb) in menuQ)
        {
            menuOpen = true;
            bool insideMenu =
                pos.X >= mbb.Ref.Position.X && pos.Y >= mbb.Ref.Position.Y &&
                pos.X < mbb.Ref.Position.X + mbb.Ref.Size.X &&
                pos.Y < mbb.Ref.Position.Y + mbb.Ref.Size.Y;

            if (!insideMenu &&
                (mouse.Value.IsPressedOnce(MouseButtonType.Left) || mouse.Value.IsPressedOnce(MouseButtonType.Right)))
            {
                UiHierarchy.DespawnSubtree(commands, ment.Ref, childrenQ);
                pressTarget.Value = 0;
            }
        }
        if (menuOpen)
            return;

        // Latch on right press over the window; open on release over it
        // (UiClick semantics — drag off cancels).
        if (mouse.Value.IsPressedOnce(MouseButtonType.Right))
        {
            pressTarget.Value = TopmostWorldMap(pos, assets.Value, renderedQ, movablesQ, parentsQ, boundsQ, windowsQ);
            return;
        }

        if (!mouse.Value.IsReleased(MouseButtonType.Right) || pressTarget.Value == 0)
            return;

        var target = pressTarget.Value;
        pressTarget.Value = 0;
        if (TopmostWorldMap(pos, assets.Value, renderedQ, movablesQ, parentsQ, boundsQ, windowsQ) != target)
            return;

        // Window-relative spawn position (the menu is a child so it rides the
        // window's z and drags with it).
        var (_, winNode, _) = movablesQ.Get(target);
        float wx = winNode.Ref.Left.Type == ValType.Px ? winNode.Ref.Left.Value : 0f;
        float wy = winNode.Ref.Top.Type == ValType.Px ? winNode.Ref.Top.Value : 0f;
        state.MenuPos = new Vector2(pos.X - wx, pos.Y - wy);
        state.MenuWindow = target;
        SpawnMenu(commands, state, profile.Value, gameCtx.Value, zCounter.Value, target,
            new Vector2(wx, wy), new Vector2(surface.Value.LogicalSize.X, surface.Value.LogicalSize.Y));
    }

    private static ulong TopmostWorldMap(
        Vector2 pos,
        AssetsServer assets,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> renderedQ,
        Query<Data<Node, GlobalZIndex>, Filter<With<UiMovable>>> movablesQ,
        Query<Data<TinyEcs.Parent>> parentsQ,
        Query<Data<UiContainsByBounds>> boundsQ,
        Query<Data<WorldMapWindow>> windowsQ)
    {
        var hit = UiPick.Topmost(pos, assets, renderedQ, parentsQ, boundsQ);
        if (!hit.Found) return 0;
        var owner = UiPick.MovableRoot(hit.Entity, movablesQ, parentsQ);
        return owner != 0 && windowsQ.Contains(owner) ? owner : 0;
    }

    // Rebuild after a toggle row mutated Profile so the pills repaint.
    private static void RefreshMenu(
        Commands commands,
        Res<Profile> profile,
        Res<UiZCounter> zCounter,
        Res<UiSurface> surface,
        Res<GameContext> gameCtx,
        ResMut<WorldMapState> stateRes,
        Query<Data<WorldMapMenu>> menuQ,
        Query<Data<Node>, Filter<With<WorldMapWindow>>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        var state = stateRes.Value;
        if (!state.MenuDirty)
            return;
        state.MenuDirty = false;

        foreach (var (ent, _) in menuQ)
            UiHierarchy.DespawnSubtree(commands, ent.Ref, childrenQ);

        if (state.MenuWindow == 0 || !windowsQ.TryGet(state.MenuWindow, out var winRow))
            return;
        var (_, winNode) = winRow;
        float wx = winNode.Ref.Left.Type == ValType.Px ? winNode.Ref.Left.Value : 0f;
        float wy = winNode.Ref.Top.Type == ValType.Px ? winNode.Ref.Top.Value : 0f;
        SpawnMenu(commands, state, profile.Value, gameCtx.Value, zCounter.Value, state.MenuWindow,
            new Vector2(wx, wy), new Vector2(surface.Value.LogicalSize.X, surface.Value.LogicalSize.Y));
    }

    private static void SpawnMenu(
        Commands commands,
        WorldMapState state,
        Profile profile,
        in GameContext gameCtx,
        UiZCounter zCounter,
        ulong windowRoot,
        Vector2 windowPos,
        Vector2 surfaceSize)
    {
        const int MenuW = 210;
        const int RowH = 22;
        const int Pad = 6;

        var toggles = new (string Label, Func<Profile, bool> Get, Action<Profile, bool> Set)[]
        {
            ("Flip Map", pr => pr.WorldMapFlipMap, (pr, v) => pr.WorldMapFlipMap = v),
            ("Free View", pr => pr.WorldMapFreeView, (pr, v) => pr.WorldMapFreeView = v),
            ("Show Party Members", pr => pr.WorldMapShowParty, (pr, v) => pr.WorldMapShowParty = v),
            ("Show Mobiles", pr => pr.WorldMapShowMobiles, (pr, v) => pr.WorldMapShowMobiles = v),
            ("Show Markers", pr => pr.WorldMapShowMarkers, (pr, v) => pr.WorldMapShowMarkers = v),
            ("Show Marker Names", pr => pr.WorldMapShowMarkersNames, (pr, v) => pr.WorldMapShowMarkersNames = v),
            ("Show Your Name", pr => pr.WorldMapShowPlayerName, (pr, v) => pr.WorldMapShowPlayerName = v),
            ("Show Your Healthbar", pr => pr.WorldMapShowPlayerBar, (pr, v) => pr.WorldMapShowPlayerBar = v),
            ("Show Group Name", pr => pr.WorldMapShowGroupName, (pr, v) => pr.WorldMapShowGroupName = v),
            ("Show Group Healthbar", pr => pr.WorldMapShowGroupBar, (pr, v) => pr.WorldMapShowGroupBar = v),
            ("Show Coordinates", pr => pr.WorldMapShowCoordinates, (pr, v) => pr.WorldMapShowCoordinates = v),
            ("Sextant Coordinates", pr => pr.WorldMapShowSextantCoordinates, (pr, v) => pr.WorldMapShowSextantCoordinates = v),
            ("Mouse Coordinates", pr => pr.WorldMapShowMouseCoordinates, (pr, v) => pr.WorldMapShowMouseCoordinates = v),
            ("Grid If Zoomed", pr => pr.WorldMapShowGridIfZoomed, (pr, v) => pr.WorldMapShowGridIfZoomed = v),
        };

        int viewedMap = state.ViewMap >= 0 ? state.ViewMap : gameCtx.Map;
        int actionRows = 4;
        int menuH = Pad * 2 + (toggles.Length + actionRows) * RowH + (toggles.Length + actionRows - 1) * 2;

        // Clamp to the logical surface (the menu is taller than the window and
        // an absolute child may escape it — but never the screen).
        float absX = Math.Clamp(windowPos.X + state.MenuPos.X, 4, Math.Max(4, surfaceSize.X - MenuW - 4));
        float absY = Math.Clamp(windowPos.Y + state.MenuPos.Y, 4, Math.Max(4, surfaceSize.Y - menuH - 4));

        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                FlexDirection = FlexDirection.Column,
                Left = Val.Px(absX - windowPos.X),
                Top = Val.Px(absY - windowPos.Y),
                Width = Val.Px(MenuW),
                Height = Val.Px(menuH),
                Padding = UiRect.All(Pad),
                Gap = Val.Px(2),
            })
            .Insert(new BackgroundColor(s_panelBg))
            .Insert(BorderRadius.All(8))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>()
            .Insert<WorldMapMenu>();
        ulong rootId = root.Id;
        commands.AddChild(windowRoot, rootId);

        foreach (var (label, get, set) in toggles)
        {
            var getCopy = get;
            var setCopy = set;
            var row = SpawnMenuRow(commands, rootId, MenuW - Pad * 2, RowH, label);
            bool on = getCopy(profile);

            var pill = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(MenuW - Pad * 2 - 40),
                    Top = Val.Px(3),
                    Width = Val.Px(36),
                    Height = Val.Px(RowH - 6),
                    JustifyContent = JustifyContent.Center,
                    AlignItems = AlignItems.Center,
                })
                .Insert(new BackgroundColor(on ? s_toggleOn : s_toggleOff))
                .Insert(BorderRadius.All(8))
                .Insert(new Text(on ? "ON" : "OFF"))
                .Insert(new TextFont { FontId = 1, Size = 10 })
                .Insert(new TextColor(s_textMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>();
            commands.AddChild(row, pill.Id);

            commands.Entity(row).Observe((On<UiClick> _, Res<Profile> pr, ResMut<WorldMapState> st) =>
            {
                setCopy(pr.Value, !getCopy(pr.Value));
                st.Value.MenuDirty = true;
            });
        }

        // Facet cycle (legacy "Change Map" submenu).
        string facetName = viewedMap >= 0 && viewedMap < s_facetNames.Length ? s_facetNames[viewedMap] : viewedMap.ToString();
        var facetRow = SpawnMenuRow(commands, rootId, MenuW - Pad * 2, RowH, $"Facet: {facetName}");
        commands.Entity(facetRow).Observe((On<UiClick> _, Res<Profile> pr, Res<GameContext> ctx, Res<UOFileManager> files, ResMut<WorldMapState> st) =>
        {
            int current = st.Value.ViewMap >= 0 ? st.Value.ViewMap : ctx.Value.Map;
            int next = current;
            for (int i = 1; i <= MapLoader.MAPS_COUNT; i++)
            {
                int candidate = (current + i) % MapLoader.MAPS_COUNT;
                try
                {
                    if (files.Value.Maps.GetMapFile(candidate) != null)
                    {
                        next = candidate;
                        break;
                    }
                }
                catch { /* facet not installed */ }
            }
            if (next == ctx.Value.Map)
            {
                st.Value.ViewMap = -1;
            }
            else
            {
                st.Value.ViewMap = next;
                // Other facets only make sense detached from the player.
                pr.Value.WorldMapFreeView = true;
                st.Value.CenterX = files.Value.Maps.MapsDefaultSize[next, 0] >> 1;
                st.Value.CenterY = files.Value.Maps.MapsDefaultSize[next, 1] >> 1;
            }
            st.Value.MenuDirty = true;
        });

        var addRow = SpawnMenuRow(commands, rootId, MenuW - Pad * 2, RowH, "Add Marker On Player");
        commands.Entity(addRow).Observe((
            On<UiClick> _,
            Res<GameContext> ctx,
            ResMut<WorldMapState> st,
            Query<Data<WorldPosition>, Filter<With<Player>>> pq) =>
        {
            foreach (var (_, pos) in pq)
            {
                AddUserMarker(st.Value, pos.Ref.X, pos.Ref.Y, ctx.Value.Map);
                break;
            }
            st.Value.MenuDirty = true;
        });

        var reloadRow = SpawnMenuRow(commands, rootId, MenuW - Pad * 2, RowH, "Reload Markers");
        commands.Entity(reloadRow).Observe((On<UiClick> _, ResMut<WorldMapState> st) =>
        {
            st.Value.MarkersLoaded = false;
            st.Value.MenuDirty = true;
        });

        var closeRow = SpawnMenuRow(commands, rootId, MenuW - Pad * 2, RowH, "Close Map");
        commands.Entity(closeRow).Observe((
            On<UiClick> _,
            Commands cmd,
            Query<Data<WorldMapWindow>> winQ,
            Query<Data<TinyEcs.Children>> kidsQ) =>
        {
            foreach (var (ent, _) in winQ)
                UiHierarchy.DespawnSubtree(cmd, ent.Ref, kidsQ);
        });
    }

    private static ulong SpawnMenuRow(Commands commands, ulong parent, int w, int h, string label)
    {
        var row = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Width = Val.Px(w),
                Height = Val.Px(h),
                Padding = new UiRect { Left = Val.Px(8), Right = Val.Px(4) },
            })
            .Insert(new BackgroundColor(s_rowBg))
            .Insert(BorderRadius.All(4))
            .Insert(new Text(label))
            .Insert(new TextFont { FontId = 1, Size = 11 })
            .Insert(new TextColor(s_textDim))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        commands.AddChild(parent, row.Id);
        return row.Id;
    }

    // ── Markers ───────────────────────────────────────────────────────────

    private static readonly Dictionary<string, XnaColor> s_colorMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "red", XnaColor.Red },
        { "green", XnaColor.Green },
        { "blue", XnaColor.Blue },
        { "purple", XnaColor.Purple },
        { "black", XnaColor.Black },
        { "yellow", XnaColor.Yellow },
        { "white", XnaColor.White },
        { "none", XnaColor.Transparent },
    };

    private static XnaColor MarkerColor(string name) =>
        s_colorMap.TryGetValue(name ?? string.Empty, out var c) ? c : XnaColor.White;

    private static void LoadMarkers(WorldMapState state)
    {
        state.MarkerFiles = new List<WMapMarkerFile>();
        if (!Directory.Exists(s_markersPath))
            return;

        foreach (var path in Directory.GetFiles(s_markersPath))
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is not (".map" or ".csv" or ".usr" or ".xml"))
                continue;

            var file = new WMapMarkerFile
            {
                Name = Path.GetFileNameWithoutExtension(path),
                FullPath = path,
                IsEditable = ext == ".usr",
            };

            try
            {
                switch (ext)
                {
                    case ".xml":
                        ParseXmlMarkers(path, file.Markers);
                        break;
                    case ".map":
                        ParseUoamMarkers(path, file.Markers);
                        break;
                    default:
                        ParseCsvMarkers(path, file.Markers);
                        break;
                }
            }
            catch
            {
                continue; // malformed user file — skip, don't kill the gump
            }

            if (file.Markers.Count > 0 || file.IsEditable)
                state.MarkerFiles.Add(file);
        }
    }

    // CSV / .usr: X,Y,MAPID,NAME,ICON,COLOR[,ZOOMINDEX]
    private static void ParseCsvMarkers(string path, List<WMapMarker> markers)
    {
        foreach (var line in File.ReadAllLines(path))
        {
            var splits = line.Split(',');
            if (splits.Length < 6)
                continue;
            markers.Add(new WMapMarker
            {
                X = int.Parse(splits[0]),
                Y = int.Parse(splits[1]),
                MapId = int.Parse(splits[2]),
                Name = splits[3],
                Color = MarkerColor(splits[5]),
                ZoomIndex = splits.Length >= 7 && int.TryParse(splits[6], out var zi) ? zi : 3,
            });
        }
    }

    // UOAM .map: "+icon: X Y MAPID name..." — icons aren't loaded, so these
    // draw as blue dots (legacy colored them via the icon texture).
    private static void ParseUoamMarkers(string path, List<WMapMarker> markers)
    {
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || (line[0] != '+' && line[0] != '-'))
                continue;
            int colon = line.IndexOf(':');
            if (colon < 0)
                continue;
            var splits = line[(colon + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (splits.Length < 4)
                continue;
            markers.Add(new WMapMarker
            {
                X = int.Parse(splits[0]),
                Y = int.Parse(splits[1]),
                MapId = int.Parse(splits[2]),
                Name = string.Join(' ', splits[3..]),
                Color = XnaColor.Blue,
                ZoomIndex = 3,
            });
        }
    }

    // Ultima Mapper XML: <Marker X=".." Y=".." Facet=".." Name=".." />
    private static void ParseXmlMarkers(string path, List<WMapMarker> markers)
    {
        using var reader = System.Xml.XmlReader.Create(path);
        while (reader.Read())
        {
            if (reader.NodeType != System.Xml.XmlNodeType.Element || reader.Name != "Marker")
                continue;
            markers.Add(new WMapMarker
            {
                X = int.Parse(reader.GetAttribute("X") ?? "0"),
                Y = int.Parse(reader.GetAttribute("Y") ?? "0"),
                MapId = int.Parse(reader.GetAttribute("Facet") ?? "0"),
                Name = reader.GetAttribute("Name") ?? string.Empty,
                Color = XnaColor.White,
                ZoomIndex = 3,
            });
        }
    }

    private static void AddUserMarker(WorldMapState state, int x, int y, int map)
    {
        var name = $"Marker {x} {y}";
        try
        {
            Directory.CreateDirectory(s_markersPath);
            File.AppendAllText(s_userMarkersPath, $"{x},{y},{map},{name},,purple,4\n");
        }
        catch
        {
            return;
        }
        state.MarkersLoaded = false; // re-read from disk
    }

    // ── Coordinate helpers (legacy ports) ─────────────────────────────────

    internal static Point RotatePoint(int x, int y, float zoom, int dist, float angle)
    {
        x = (int)(x * zoom);
        y = (int)(y * zoom);

        if (angle == 0.0f)
            return new Point(x, y);

        double cos = Math.Cos(dist * Math.PI / 4.0);
        double sin = Math.Sin(dist * Math.PI / 4.0);
        return new Point((int)Math.Round(cos * x - sin * y), (int)Math.Round(sin * x + cos * y));
    }

    internal static void CanvasToWorld(
        int canvasX, int canvasY, int width, int height,
        int centerX, int centerY, float zoom, bool flip,
        out int worldX, out int worldY)
    {
        float newWidth = width / zoom;
        float newHeight = height / zoom;
        float newX = canvasX / zoom;
        float newY = canvasY / zoom;

        if (flip)
        {
            float nw = (newWidth + newHeight) / 1.41f;
            float nh = (newHeight - newWidth) / 1.41f;
            newWidth = (int)nw;
            newHeight = (int)nh;

            float nx = (newX + newY) / 1.41f;
            float ny = (newY - newX) / 1.41f;
            newX = (int)nx;
            newY = (int)ny;
        }

        worldX = centerX - (int)(newWidth / 2) + (int)newX;
        worldY = centerY - (int)(newHeight / 2) + (int)newY;
    }

    // Minimal Sextant.FormatString port (Game/Data/Sextant.cs) — lat/long from
    // the facet's moongate-centred grid.
    private static bool SextantFormat(int mapIndex, int mapWidth, int mapHeight, int x, int y, out string text)
    {
        text = string.Empty;
        const int xWidth = 5120, yHeight = 4096;
        int xCenter, yCenter;

        bool isLargeBritannia = mapIndex <= 1 && mapWidth == 7168 && mapHeight == 4096;
        if (isLargeBritannia)
        {
            if (x >= 0 && y >= 0 && x < 5120 && y < 4096) { xCenter = 1323; yCenter = 1624; }
            else if (x >= 5120 && y >= 2304 && x < 6144 && y < 4096) { xCenter = 5936; yCenter = 3112; }
            else return false;
        }
        else if (x >= 0 && y >= 0 && x < mapWidth && y < mapHeight)
        {
            xCenter = 1323; yCenter = 1624;
        }
        else
        {
            return false;
        }

        double absLong = (double)((x - xCenter) * 360) / xWidth;
        double absLat = (double)((y - yCenter) * 360) / yHeight;

        if (absLong > 180.0) absLong = -180.0 + absLong % 180.0;
        if (absLat > 180.0) absLat = -180.0 + absLat % 180.0;

        bool east = absLong >= 0, south = absLat >= 0;
        if (absLong < 0.0) absLong = -absLong;
        if (absLat < 0.0) absLat = -absLat;

        text = $"{(int)absLat}o {(int)(absLat % 1.0 * 60)}'{(south ? "S" : "N")}, {(int)absLong}o {(int)(absLong % 1.0 * 60)}'{(east ? "E" : "W")}";
        return true;
    }

    // ── Renderer (called from GuiRenderingPlugin's custom-command switch) ──

    public static void DrawWorldMap(UltimaBatcher2D b, Texture2D white, float bbX, float bbY, float bbW, float bbH, WorldMapRenderData d, float z)
    {
        // Lazily hand the cache its device — ECS never initialised it (the
        // legacy boot path did).
        SolidColorTextureCache.Initialize(b.GraphicsDevice);

        int gX = (int)bbX;
        int gY = (int)bbY;
        int gWidth = (int)bbW;
        int gHeight = (int)bbH;
        if (gWidth <= 0 || gHeight <= 0)
            return;

        DrawSolid(b, white, gX, gY, gWidth, gHeight, XnaColor.Black, z);

        if (!b.ClipBegin(gX, gY, gWidth, gHeight))
            return;

        int halfWidth = gWidth >> 1;
        int halfHeight = gHeight >> 1;

        if (d.Map == null || d.Map.IsDisposed)
        {
            var msg = d.Loading ? "Please wait, I'm making the map file..." : "No map loaded";
            var (mw, mh) = UoFontRenderer.MeasureFont(msg, NAME_FONT, 1000, allowHtml: false);
            UoFontRenderer.Draw(b, msg, NAME_FONT, XnaColor.White,
                gX + halfWidth - mw / 2, gY + halfHeight - mh / 2, mw + 4, z, allowHtml: false, border: true);
            b.ClipEnd();
            return;
        }

        // Legacy AddToRenderLists: oversized square dest so the 45° rotation
        // never shows corners, source window centred on the view.
        int size = (int)Math.Max(gWidth * 1.75f, gHeight * 1.75f);
        int sizeZoom = (int)(size / d.Zoom);
        int sizeZoomHalf = sizeZoom >> 1;
        int centerX = d.CenterX + 1;
        int centerY = d.CenterY + 1;

        var destRect = new Rectangle(gX + halfWidth, gY + halfHeight, size, size);
        var srcRect = new Rectangle(centerX - sizeZoomHalf, centerY - sizeZoomHalf, sizeZoom, sizeZoom);
        var origin = new Vector2(srcRect.Width / 2f, srcRect.Height / 2f);
        float rotation = d.Flip ? Microsoft.Xna.Framework.MathHelper.ToRadians(45) : 0f;

        b.Draw(d.Map, destRect, srcRect, Vector3.UnitZ, rotation, origin, SpriteEffects.None, z);

        // Markers (under the dots, like legacy DrawAll order). The hovered one
        // gets its name even when names are off.
        WMapMarker hovered = null;
        if (d.ShowMarkers && d.MarkerFiles != null)
        {
            foreach (var file in d.MarkerFiles)
            {
                foreach (var marker in file.Markers)
                {
                    if (DrawMarker(b, white, d, marker, gX, gY, gWidth, gHeight, halfWidth, halfHeight, z))
                        hovered = marker;
                }
            }
            if (hovered != null)
                DrawNameLabel(b, white, d, hovered.X, hovered.Y, hovered.Name, XnaColor.White,
                    gX, gY, gWidth, gHeight, halfWidth, halfHeight, z, above: 5);
        }

        // Mobile / party / player dots (player is last in the list).
        foreach (var dot in d.Dots)
        {
            var rot = WorldToCanvas(d, dot.WX, dot.WY);
            rot.X += gX + halfWidth;
            rot.Y += gY + halfHeight;
            rot.X = Math.Clamp(rot.X, gX, gX + gWidth - DOT_SIZE);
            rot.Y = Math.Clamp(rot.Y, gY, gY + gHeight - DOT_SIZE);

            DrawSolid(b, white, rot.X - DOT_SIZE_HALF, rot.Y - DOT_SIZE_HALF, DOT_SIZE, DOT_SIZE, dot.Color, z);

            if (!string.IsNullOrEmpty(dot.Name))
                DrawNameLabel(b, white, d, dot.WX, dot.WY, dot.Name, dot.Color,
                    gX, gY, gWidth, gHeight, halfWidth, halfHeight, z, above: 0);

            if (dot.HpPercent >= 0)
                DrawHpBar(b, white, rot.X, rot.Y + DOT_SIZE + 1, dot.HpPercent, z);
        }

        if (d.ShowGrid)
            DrawGrid(b, d, srcRect, gX, gY, halfWidth, halfHeight, z);

        if (!string.IsNullOrEmpty(d.TopText))
            DrawTextLines(b, d.TopText, gX + 6, gY + 6, z);

        if (!string.IsNullOrEmpty(d.BottomText))
        {
            int lines = 1;
            foreach (var ch in d.BottomText)
                if (ch == '\n') lines++;
            DrawTextLines(b, d.BottomText, gX + 5, gY + gHeight - lines * 16 - 6, z);
        }

        b.ClipEnd();
    }

    private static Point WorldToCanvas(WorldMapRenderData d, int wx, int wy)
        => RotatePoint(wx - d.CenterX, wy - d.CenterY, d.Zoom, 1, d.Flip ? 45f : 0f);

    private static bool DrawMarker(
        UltimaBatcher2D b, Texture2D white, WorldMapRenderData d, WMapMarker marker,
        int gX, int gY, int gWidth, int gHeight, int halfWidth, int halfHeight, float z)
    {
        if (marker.MapId != d.MapIndex)
            return false;
        if (d.ZoomIndex < marker.ZoomIndex && marker.Color == XnaColor.Transparent)
            return false;

        var rot = WorldToCanvas(d, marker.X, marker.Y);
        rot.X += gX + halfWidth;
        rot.Y += gY + halfHeight;

        if (rot.X < gX || rot.X > gX + gWidth - DOT_SIZE || rot.Y < gY || rot.Y > gY + gHeight - DOT_SIZE)
            return false;

        DrawSolid(b, white, rot.X - DOT_SIZE_HALF, rot.Y - DOT_SIZE_HALF, DOT_SIZE, DOT_SIZE, marker.Color, z);

        bool showName = d.ShowMarkerNames && !string.IsNullOrEmpty(marker.Name) && d.ZoomIndex > 5;
        if (showName)
        {
            DrawNameLabel(b, white, d, marker.X, marker.Y, marker.Name, XnaColor.White,
                gX, gY, gWidth, gHeight, halfWidth, halfHeight, z, above: 5);
            return false;
        }

        // Hover: report so the caller draws the single hovered name on top.
        return d.MousePos.X >= rot.X - DOT_SIZE && d.MousePos.X <= rot.X + DOT_SIZE_HALF &&
               d.MousePos.Y >= rot.Y - DOT_SIZE && d.MousePos.Y <= rot.Y + DOT_SIZE_HALF &&
               !string.IsNullOrEmpty(marker.Name);
    }

    private static void DrawNameLabel(
        UltimaBatcher2D b, Texture2D white, WorldMapRenderData d,
        int wx, int wy, string name, XnaColor color,
        int gX, int gY, int gWidth, int gHeight, int halfWidth, int halfHeight, float z, int above)
    {
        var rot = WorldToCanvas(d, wx, wy);
        rot.X += gX + halfWidth;
        rot.Y += gY + halfHeight;

        var (tw, th) = UoFontRenderer.MeasureFont(name, NAME_FONT, 600, allowHtml: false);
        if (tw <= 0)
            return;

        if (rot.X + tw / 2 > gX + gWidth) rot.X = gX + gWidth - tw / 2;
        else if (rot.X - tw / 2 < gX) rot.X = gX + tw / 2;
        if (rot.Y + th > gY + gHeight) rot.Y = gY + gHeight - th;
        else if (rot.Y - th < gY) rot.Y = gY + th;

        int xx = rot.X - tw / 2;
        int yy = rot.Y - th - above;

        if (above > 0)
            DrawSolid(b, white, xx - 2, yy - 2, tw + 4, th + 4, new XnaColor(0, 0, 0, 128), z);

        UoFontRenderer.Draw(b, name, NAME_FONT, color, xx, yy, tw + 4, z, allowHtml: false, border: true);
    }

    private static void DrawHpBar(UltimaBatcher2D b, Texture2D white, int x, int y, int percent, float z)
    {
        const int HALF_W = BAR_MAX_WIDTH / 2;
        const int HALF_H = BAR_MAX_HEIGHT / 2;

        DrawSolid(b, white, x - HALF_W - 1, y - HALF_H - 1, BAR_MAX_WIDTH + 2, BAR_MAX_HEIGHT + 2, XnaColor.Black, z);
        DrawSolid(b, white, x - HALF_W, y - HALF_H, BAR_MAX_WIDTH, BAR_MAX_HEIGHT, XnaColor.Red, z);
        int fill = BAR_MAX_WIDTH * Math.Clamp(percent, 0, 100) / 100;
        DrawSolid(b, white, x - HALF_W, y - HALF_H, fill, BAR_MAX_HEIGHT, XnaColor.CornflowerBlue, z);
    }

    private static void DrawGrid(UltimaBatcher2D b, WorldMapRenderData d, Rectangle srcRect, int gX, int gY, int halfWidth, int halfHeight, float z)
    {
        const int GRID_SKIP = 8;
        var texture = SolidColorTextureCache.GetTexture(s_gridColor);

        b.SetBlendState(BlendState.Additive);

        for (int worldY = srcRect.Y / GRID_SKIP * GRID_SKIP; worldY < srcRect.Y + srcRect.Height; worldY += GRID_SKIP)
        {
            var start = ProjectF(d, srcRect.X, worldY, gX, gY, halfWidth, halfHeight);
            var end = ProjectF(d, srcRect.X + srcRect.Width, worldY, gX, gY, halfWidth, halfHeight);
            b.DrawLine(texture, start, end, Vector3.UnitZ, 1, z);
        }

        for (int worldX = srcRect.X / GRID_SKIP * GRID_SKIP; worldX < srcRect.X + srcRect.Width; worldX += GRID_SKIP)
        {
            var start = ProjectF(d, worldX, srcRect.Y, gX, gY, halfWidth, halfHeight);
            var end = ProjectF(d, worldX, srcRect.Y + srcRect.Height, gX, gY, halfWidth, halfHeight);
            b.DrawLine(texture, start, end, Vector3.UnitZ, 1, z);
        }

        b.SetBlendState(null);
    }

    private static Vector2 ProjectF(WorldMapRenderData d, int wx, int wy, int gX, int gY, int halfWidth, int halfHeight)
    {
        var rot = WorldToCanvas(d, wx, wy);
        return new Vector2(rot.X + gX + halfWidth, rot.Y + gY + halfHeight);
    }

    private static void DrawTextLines(UltimaBatcher2D b, string text, int x, int y, float z)
    {
        foreach (var line in text.Split('\n'))
        {
            var (tw, _) = UoFontRenderer.MeasureFont(line, NAME_FONT, 1000, allowHtml: false);
            UoFontRenderer.Draw(b, line, NAME_FONT, XnaColor.White, x, y, tw + 4, z, allowHtml: false, border: true);
            y += 16;
        }
    }

    private static void DrawSolid(UltimaBatcher2D b, Texture2D white, int x, int y, int w, int h, XnaColor color, float z)
    {
        if (w <= 0 || h <= 0)
            return;
        b.Draw(white, new Vector2(x, y), new Rectangle(0, 0, w, h), color, 0f, Vector2.One, z);
    }
}
