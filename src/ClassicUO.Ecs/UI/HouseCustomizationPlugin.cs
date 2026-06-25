// House customization (design tool) — ECS port of legacy
// HouseCustomizationManager.cs + HouseCustomizationGump.cs.
//
// Server-authoritative: the client opens the design palette (0xBF 0x20 type 4),
// the player paints walls/floors/etc on the house tiles, and every add/delete
// is a 0xD7 sub-command. The server validates and re-sends the whole house via
// 0xD8, which OnCustomHouse re-renders (it now despawns the previous CustomMulti
// children first). So this code never mutates the house locally — it only owns
// the editor STATE, the palette UI, and translating clicks into 0xD7 sends.
//
// State lives in the HouseCustomizationState resource (the legacy manager
// singleton); the palette is a normal UO gump window rebuilt from that state
// whenever it goes Dirty.

using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO.Assets;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Marks the palette window root; Content is the child everything is rebuilt under.
// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct HouseDesignWindow { public ulong Content; public uint Serial; }

// Client-generated floor placeholder tile (legacy 0x0496 grid). It is the
// build surface — you place pieces onto it — and the visual floor guide. Not a
// CustomMulti, so the 0xD8 child-clear leaves it alone and it isn't counted.
internal struct HouseFloorMarker;

// Per-piece floor-vision render mode (set by ApplyFloorVision, read by the world
// static render's ProcessFade so the fade system targets the right value — no
// AlphaFade tug-of-war / flicker). 0 = normal, 1 = translucent, 2 = hidden.
internal struct HouseVisionFade { public byte Mode; }

// Semi-transparent ghost of the selected piece following the cursor tile.
// Not a CustomMulti (not counted/erasable); deliberately NOT excluded from the
// world hit-test — when the cursor moves it sits on the previous tile, so
// SelectedEntity resolves to the new tile that frame and the ghost follows.
internal struct HouseDesignPreview;

internal sealed class HouseCustomizationState
{
    // Object tables, parsed once from the UO data .txt files.
    public readonly List<CustomHouseWallCategory> Walls = new();
    public readonly List<CustomHouseFloor> Floors = new();
    public readonly List<CustomHouseDoor> Doors = new();
    public readonly List<CustomHouseMiscCategory> Miscs = new();
    public readonly List<CustomHouseStair> Stairs = new();
    public readonly List<CustomHouseTeleport> Teleports = new();
    public readonly List<CustomHouseRoofCategory> Roofs = new();
    public readonly List<CustomHousePlaceInfo> ObjectsInfo = new();
    public bool Loaded;

    // Editor state (legacy fields).
    public bool Active;
    public uint Serial;
    public int FoundationX, FoundationY, FoundationZ;
    public int Category = -1, Page, MaxPage = 1, CurrentFloor = 1, FloorCount = 4, RoofZ = 1, MinHouseZ = -120;
    public int Components, Fixtures, MaxComponents, MaxFixtures;
    public bool Erasing, SeekTile, ShowWindow, CombinedStair;
    public ushort SelectedGraphic;
    public CUSTOM_HOUSE_GUMP_STATE State = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;
    public Point StartPos, EndPos;
    public readonly int[] FloorVisionState = new int[4];
    public Rectangle Bounds;
    public bool Dirty;
    public int MarkersFloor = -1; // floor the placeholder grid is currently built for

    public void LoadData(UOFileManager files, int lockedFeatures)
    {
        ParseFileWithCategory<CustomHouseWall, CustomHouseWallCategory>(Walls, files.GetUOFilePath("walls.txt"), lockedFeatures);
        ParseFile(Floors, files.GetUOFilePath("floors.txt"), lockedFeatures);
        ParseFile(Doors, files.GetUOFilePath("doors.txt"), lockedFeatures);
        ParseFileWithCategory<CustomHouseMisc, CustomHouseMiscCategory>(Miscs, files.GetUOFilePath("misc.txt"), lockedFeatures);
        ParseFile(Stairs, files.GetUOFilePath("stairs.txt"), lockedFeatures);
        ParseFile(Teleports, files.GetUOFilePath("teleprts.txt"), lockedFeatures);
        ParseFileWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(Roofs, files.GetUOFilePath("roof.txt"), lockedFeatures);
        ParseFile(ObjectsInfo, files.GetUOFilePath("suppinfo.txt"), lockedFeatures);
        Loaded = true;
    }

    public void Begin(uint serial, int fx, int fy, int fz, Rectangle multiBounds)
    {
        Active = true;
        Serial = serial;
        FoundationX = fx;
        FoundationY = fy;
        FoundationZ = fz;
        Category = -1;
        Page = 0;
        State = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;
        CurrentFloor = 1;
        RoofZ = 1;
        Erasing = SeekTile = ShowWindow = CombinedStair = false;
        SelectedGraphic = 0;
        Array.Clear(FloorVisionState);
        MarkersFloor = -1;
        InitializeHouse(fx, fy, fz, multiBounds);
        UpdateMaxPage();
        Dirty = true;
    }

    private void InitializeHouse(int fx, int fy, int fz, Rectangle multiBounds)
    {
        MinHouseZ = fz + 7;

        StartPos.X = fx + multiBounds.X + 1;
        StartPos.Y = fy + multiBounds.Y + 1;
        EndPos.X = fx + multiBounds.Width + 1;
        EndPos.Y = fy + multiBounds.Height + 1;

        int width = Math.Abs(EndPos.X - StartPos.X);
        int height = Math.Abs(EndPos.Y - StartPos.Y);

        Bounds = new Rectangle(StartPos.X - 1, StartPos.Y - 1, width + 1, height + 2);
        FloorCount = (width >= 13 || height >= 13) ? 4 : 3;

        int plotWidth = width + 1;
        int plotHeight = height + 1;
        int componentsOnFloor = (plotWidth - 1) * (plotHeight - 1);

        MaxComponents = FloorCount * (componentsOnFloor + 2 * (plotWidth + plotHeight) - 4)
            - (int)(FloorCount * componentsOnFloor * -0.25) + 2 * plotWidth + 3 * plotHeight - 5;
        MaxFixtures = MaxComponents / 20;
    }

    public void SetState(CUSTOM_HOUSE_GUMP_STATE state)
    {
        State = state;
        Category = -1;
        Page = 0;
        SelectedGraphic = 0;
        CombinedStair = false;
        Erasing = false;
        SeekTile = false;
        UpdateMaxPage();
        Dirty = true;
    }

    public void SetCategory(int category)
    {
        Category = category;
        Page = 0;
        SelectedGraphic = 0;
        CombinedStair = false;
        Erasing = false;
        SeekTile = false;
        UpdateMaxPage();
        Dirty = true;
    }

    public void Select(ushort graphic, bool combinedStair)
    {
        Erasing = false;
        SeekTile = false;
        CombinedStair = combinedStair;
        SelectedGraphic = graphic;
        Dirty = true;
    }

    public void PageDelta(int delta)
    {
        if (MaxPage <= 1) return;
        Page += delta;
        if (Page < 0) Page = MaxPage - 1;
        else if (Page >= MaxPage) Page = 0;
        SelectedGraphic = 0;
        Dirty = true;
    }

    public void UpdateMaxPage()
    {
        MaxPage = 1;
        switch (State)
        {
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL:
                MaxPage = Category == -1 ? (int)Math.Ceiling(Walls.Count / 16.0f) : CategoryItemCount<CustomHouseWall, CustomHouseWallCategory>(Walls, Category);
                break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF:
                MaxPage = Category == -1 ? (int)Math.Ceiling(Roofs.Count / 16.0f) : CategoryItemCount<CustomHouseRoof, CustomHouseRoofCategory>(Roofs, Category);
                break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC:
                MaxPage = Category == -1 ? (int)Math.Ceiling(Miscs.Count / 16.0f) : CategoryItemCount<CustomHouseMisc, CustomHouseMiscCategory>(Miscs, Category);
                break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR: MaxPage = Doors.Count; break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR: MaxPage = Floors.Count; break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR: MaxPage = Stairs.Count; break;
        }
        if (MaxPage < 1) MaxPage = 1;
        if (Page >= MaxPage) Page = MaxPage - 1;
        if (Page < 0) Page = 0;
    }

    private static int CategoryItemCount<TItem, TCat>(List<TCat> list, int category)
        where TItem : CustomHouseObject where TCat : CustomHouseObjectCategory<TItem>
    {
        foreach (var c in list) if (c.Index == category) return Math.Max(1, c.Items.Count);
        return 1;
    }

    // Find the first table containing `graphic`; reports its editor state.
    public bool ExistsInList(ushort graphic, out CUSTOM_HOUSE_GUMP_STATE state)
    {
        state = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL;
        if (SeekCategory<CustomHouseWall, CustomHouseWallCategory>(Walls, graphic)) { state = CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL; return true; }
        if (SeekList(Floors, graphic)) { state = CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR; return true; }
        if (SeekList(Doors, graphic)) { state = CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR; return true; }
        if (SeekCategory<CustomHouseMisc, CustomHouseMiscCategory>(Miscs, graphic)) { state = CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC; return true; }
        if (SeekList(Stairs, graphic)) { state = CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR; return true; }
        if (SeekCategory<CustomHouseRoof, CustomHouseRoofCategory>(Roofs, graphic)) { state = CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF; return true; }
        if (SeekList(Teleports, graphic)) { state = CUSTOM_HOUSE_GUMP_STATE.CHGS_FIXTURE; return true; }
        return false;
    }

    private static bool SeekList<T>(List<T> list, ushort graphic) where T : CustomHouseObject
    {
        foreach (var item in list)
            if (item.Contains(graphic) != -1)
                return true;
        return false;
    }

    private static bool SeekCategory<TItem, TCat>(List<TCat> list, ushort graphic)
        where TItem : CustomHouseObject where TCat : CustomHouseObjectCategory<TItem>
    {
        foreach (var cat in list)
            foreach (var item in cat.Items)
                if (item.Contains(graphic) != -1)
                    return true;
        return false;
    }

    private void ParseFile<T>(List<T> list, string path, int lockedFeatures) where T : CustomHouseObject, new()
    {
        if (!File.Exists(path)) return;
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var item = new T();
            if (item.Parse(line) && (item.FeatureMask == 0 || (lockedFeatures & item.FeatureMask) != 0))
                list.Add(item);
        }
    }

    private void ParseFileWithCategory<T, U>(List<U> list, string path, int lockedFeatures)
        where T : CustomHouseObject, new() where U : CustomHouseObjectCategory<T>, new()
    {
        if (!File.Exists(path)) return;
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var item = new T();
            if (!item.Parse(line)) continue;
            if (item.FeatureMask != 0 && (lockedFeatures & item.FeatureMask) == 0) continue;

            U cat = null;
            foreach (var c in list)
                if (c.Index == item.Category) { cat = c; break; }
            if (cat == null)
            {
                cat = new U { Index = item.Category };
                list.Add(cat);
            }
            cat.Items.Add(item);
        }
    }
}

internal readonly struct HouseCustomizationPlugin : IPlugin
{
    // Palette frame chrome.
    private const ushort LeftFrame = 0x55F0, TopTiled = 0x55F1, RightFrame4 = 0x55F2, RightFrame3 = 0x55F9, ContentBg = 0x0E14;
    private const int WinW = 665, WinH = 170;
    private const int ContentX = 121, ContentY = 36, ContentW = 397, ContentH = 120;
    private const int Cols = 8;

    public void Build(App app)
    {
        app.AddResource(new HouseCustomizationState());

        // 0xBF 0x20: type 4 = begin design, type 5 = end. (1/2/3 are server-side
        // update/remove/move acks the legacy client also ignores.)
        app.AddObserver((
            On<PacketReceived<OnExtendedCommandPacket_0xBF>> trig,
            ResMut<HouseCustomizationState> st,
            Res<UOFileManager> files,
            Res<NetClient> net,
            Res<GameContext> ctx,
            Res<NetworkEntitiesMap> emap,
            Res<MultiCache> multis,
            Query<Data<WorldPosition, Graphic>> foundationQ) =>
        {
            if (trig.Event.Packet.HouseCustomization is not { } hc) return;

            if (hc.Type == 4)
            {
                if (!st.Value.Loaded)
                    st.Value.LoadData(files.Value, (int)ctx.Value.LockedFeatures);

                int fx = hc.X, fy = hc.Y, fz = hc.Z;
                var bounds = Rectangle.Empty;
                if (emap.Value.TryGet(hc.Serial, out var foundationId) && foundationQ.TryGet(foundationId, out var row))
                {
                    var (_, pos, graphic) = row;
                    fx = pos.Ref.X; fy = pos.Ref.Y; fz = pos.Ref.Z;
                    bounds = multis.Value.GetMulti(graphic.Ref.Value).Bounds;
                }

                st.Value.Begin(hc.Serial, fx, fy, fz, bounds);
                net.Value.Send_CustomHouseDataRequest(hc.Serial);
            }
            else if (hc.Type == 5)
            {
                st.Value.Active = false; // rebuild system despawns the window
            }
        });

        // Closing the palette (right-click, server end, or logout) tells the
        // server we left design mode (legacy HouseCustomizationGump.Dispose).
        app.AddObserver((
            OnRemove<HouseDesignWindow> _,
            ResMut<HouseCustomizationState> st,
            Res<NetClient> net,
            Res<GameContext> ctx) =>
        {
            st.Value.Active = false;
            net.Value.Send_CustomHouseBuildingExit(ctx.Value.PlayerSerial);
        });

        var rebuildFn = Rebuild;
        app.AddSystem(rebuildFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var placeFn = WorldPlacement;
        app.AddSystem(placeFn).InStage(Stage.First)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .RunIf((Res<HouseCustomizationState> st) => st.Value.Active).Build();

        var markersFn = SyncFloorMarkers;
        app.AddSystem(markersFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        // Drive the targeting-reticle cursor while a design tool is armed (the
        // cursor system reads this flag; it can't take the state resource itself).
        app.AddSystem((Res<HouseCustomizationState> st, ResMut<GameCursorState> cursor) =>
            cursor.Value.DesignReticle = st.Value.Active
                && (st.Value.SelectedGraphic != 0 || st.Value.Erasing || st.Value.SeekTile))
            .InStage(Stage.Update).Build();

        var previewFn = SyncPreview;
        app.AddSystem(previewFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var visionFn = ApplyFloorVision;
        app.AddSystem(visionFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .RunIf((Res<HouseCustomizationState> st) => st.Value.Active).Build();

        // Escape cancels the armed tool (legacy TargetManager.CancelTarget resets
        // the design selection). No server target is held, so just clear locally.
        app.AddSystem((Res<KeyboardContext> kb, ResMut<HouseCustomizationState> st) =>
        {
            if (!st.Value.Active || !kb.Value.IsPressedOnce(Keys.Escape)) return;
            if (st.Value.SelectedGraphic == 0 && !st.Value.Erasing && !st.Value.SeekTile) return;
            st.Value.SelectedGraphic = 0;
            st.Value.Erasing = false;
            st.Value.SeekTile = false;
            st.Value.CombinedStair = false;
            st.Value.Dirty = true;
        }).InStage(Stage.First)
          .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var despawnFn = DespawnOnExit;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
    }

    private static void DespawnOnExit(
        Commands commands,
        Query<Data<HouseDesignWindow>> windowsQ,
        Query<Data<WorldPosition>, With<HouseFloorMarker>> markersQ,
        Query<Data<WorldPosition>, With<HouseDesignPreview>> previewQ)
    {
        foreach (var (ent, _) in windowsQ) commands.Entity(ent.Ref).Despawn();
        foreach (var (ent, _) in markersQ) commands.Entity(ent.Ref).Despawn();
        foreach (var (ent, _) in previewQ) commands.Entity(ent.Ref).Despawn();
    }

    // Build (and rebuild on floor change) the 0x0496 placeholder floor grid for
    // floors 0..CurrentFloor-1 — legacy GenerateFloorPlace. These are the build
    // surface (placement hit-tests onto them) and the visual floor guide. They
    // are not CustomMulti, so the 0xD8 echo's child-clear leaves them intact.
    private static void SyncFloorMarkers(
        Commands commands,
        ResMut<HouseCustomizationState> stRes,
        Res<NetworkEntitiesMap> emap,
        Query<Data<WorldPosition>, With<HouseFloorMarker>> markersQ)
    {
        var st = stRes.Value;

        if (!st.Active)
        {
            if (st.MarkersFloor != -1)
            {
                foreach (var (ent, _) in markersQ) commands.Entity(ent.Ref).Despawn();
                st.MarkersFloor = -1;
            }
            return;
        }

        if (st.MarkersFloor == st.CurrentFloor) return;
        if (!emap.Value.TryGet(st.Serial, out var houseId)) return;

        foreach (var (ent, _) in markersQ) commands.Entity(ent.Ref).Despawn();
        st.MarkersFloor = st.CurrentFloor;

        for (int i = 0; i < st.CurrentFloor; i++)
        {
            int z = st.FoundationZ + 7 + i * 20;
            ushort color = (ushort)(0x0051 + i * 5);
            for (int x = st.Bounds.X; x < st.EndPos.X; x++)
            {
                for (int y = st.Bounds.Y; y < st.EndPos.Y; y++)
                {
                    ushort hue = (x == st.Bounds.X || y == st.Bounds.Y) ? (ushort)0x34 : color;
                    var m = commands.Spawn()
                        .Insert(new Graphic { Value = 0x0496 })
                        .Insert(new Hue { Value = hue })
                        .Insert(new WorldPosition { X = (ushort)x, Y = (ushort)y, Z = (sbyte)z })
                        // Translucent like legacy (0x0496 grid spawned CHMOF_TRANSPARENT);
                        // Mode 1 makes the static render's ProcessFade target alpha 178.
                        .Insert(new HouseVisionFade { Mode = 1 })
                        .Insert<HouseFloorMarker>();
                    commands.AddChild(houseId, m.Id);
                }
            }
        }
    }

    // Semi-transparent ghost of the selected piece on the tile under the cursor
    // — follows ANY tile (legacy aims everywhere), natural hue when the tile is a
    // valid (in-plot) placement, red (0x002B, legacy CHMOF_INCORRECT_PLACE) when
    // not. Driven straight from the mouse (not SelectedEntity) so it can't lock
    // onto its own pick. Reuses one entity; alpha refreshed to 178 each frame so
    // the world fade can't climb it opaque.
    private static void SyncPreview(
        Commands commands,
        ResMut<HouseCustomizationState> stRes,
        Res<SelectedEntity> selected,
        Query<Data<WorldPosition, Graphic>> worldQ,
        Query<Data<WorldPosition, ScreenPosition, Graphic, Hue, AlphaFade>, With<HouseDesignPreview>> previewQ)
    {
        var st = stRes.Value;
        bool show = st.Active && st.SelectedGraphic != 0 && !st.Erasing && !st.SeekTile;

        ulong prevEnt = 0;
        foreach (var (e, _, _, _, _, _) in previewQ) { prevEnt = e.Ref; break; }

        // The tile under the cursor = the render's own pixel-perfect pick (same one
        // placement uses), so the ghost lands exactly where the piece will. The
        // ghost is excluded from that pick (IgnoreEntity) so it can't pick itself.
        var hit = selected.Value.Entity;
        if (!show || hit == 0 || !worldQ.TryGet(hit, out var hitRow))
        {
            if (prevEnt != 0) commands.Entity(prevEnt).Despawn();
            selected.Value.IgnoreEntity = 0;
            return;
        }

        var (_, hitPos, _) = hitRow;
        int tx = hitPos.Ref.X, ty = hitPos.Ref.Y;

        // Render at the piece's placement Z (legacy OnTargetWorld: roof lifts by
        // (RoofZ-2)*3, a single stair sits at the foundation Z), +1 so it sorts
        // ABOVE whatever already occupies the tile (a floor would otherwise hide
        // behind the existing floor/foundation — low static priority).
        sbyte renderZ = (sbyte)(PlacementZ(st) + 1);
        ushort hue = st.Bounds.Contains(tx, ty) ? (ushort)0 : (ushort)0x002B;
        var screen = Isometric.IsoToScreen((ushort)tx, (ushort)ty, renderZ);

        if (prevEnt != 0 && previewQ.TryGet(prevEnt, out var prow))
        {
            var (_, wp, sp, g, h, af) = prow;
            wp.Ref.X = (ushort)tx; wp.Ref.Y = (ushort)ty; wp.Ref.Z = renderZ;
            sp.Ref.Value = screen; // set directly — field mutation doesn't retrigger iso recompute
            g.Ref.Value = st.SelectedGraphic;
            h.Ref.Value = hue;
            af.Ref.Value = 178;
            selected.Value.IgnoreEntity = prevEnt; // keep the ghost out of the world pick
        }
        else
        {
            var e = commands.Spawn()
                .Insert(new Graphic { Value = st.SelectedGraphic })
                .Insert(new Hue { Value = hue })
                .Insert(new WorldPosition { X = (ushort)tx, Y = (ushort)ty, Z = renderZ })
                .Insert(new ScreenPosition { Value = screen })
                .Insert(new AlphaFade { Value = 178 })
                .Insert<HouseDesignPreview>();
            selected.Value.IgnoreEntity = e.Id;
        }
    }

    private static void Rebuild(
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> zCounter,
        ResMut<HouseCustomizationState> stRes,
        Query<Data<HouseDesignWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ,
        Query<Empty, With<CustomMulti>> customMultiQ,
        Query<Data<Graphic>> graphicQ,
        Res<NetworkEntitiesMap> emap,
        Res<ObjectPropertyLists> opl)
    {
        var st = stRes.Value;

        ulong windowEnt = 0, content = 0;
        foreach (var (ent, w) in windowsQ) { windowEnt = ent.Ref; content = w.Ref.Content; break; }

        if (st.Active && windowEnt == 0)
        {
            // Frameless movable root (legacy composes the palette from frame
            // pieces, no single background sprite).
            var root = commands.SpawnBundle(new UOGumpBundle
            {
                Kind = UOCustomKind.None,
                Position = new Vector2(50, 50),
                Size = new Vector2(WinW, WinH),
                Hue = Vector3.UnitZ,
                ZOrder = zCounter.Value.Bump(),
            });
            var contentEnt = commands.Spawn()
                .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(0), Top = Val.Px(0), Width = Val.Px(WinW), Height = Val.Px(WinH) });
            commands.AddChild(root.Id, contentEnt.Id);
            commands.Entity(root.Id).Insert(new HouseDesignWindow { Content = contentEnt.Id, Serial = st.Serial });
            st.Dirty = true;
            return;
        }

        if (!st.Active && windowEnt != 0)
        {
            commands.Entity(windowEnt).Despawn();
            return;
        }

        if (!st.Active || windowEnt == 0 || !st.Dirty) return;
        st.Dirty = false;
        st.UpdateMaxPage();

        // Clear previous content children.
        if (childrenQ.TryGet(content, out var kidsRow))
        {
            var (_, kids) = kidsRow;
            foreach (var cid in kids.Ref) commands.Entity(cid).Despawn();
        }

        RecountComponents(st, emap.Value, childrenQ, customMultiQ, graphicQ);
        SeedDesignTooltips(opl.Value);
        BuildChrome(commands, builder.Value, content, st);
        BuildContent(commands, builder.Value, assets.Value, content, st);
    }

    // ---- chrome (frame, tabs, toggles, floor nav, counters) ----

    private static void BuildChrome(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st)
    {
        var z = Vector3.UnitZ;

        // Frame + content background.
        Child(commands, content, builder.AddGumpTiled(commands, ContentBg, z, new Vector2(ContentX, ContentY), new Vector2(ContentW, ContentH)));
        Child(commands, content, builder.AddGump(commands, LeftFrame, z, new Vector2(0, 17)));
        Child(commands, content, builder.AddGumpTiled(commands, TopTiled, z, new Vector2(153, 17), new Vector2(333, 154)));
        Child(commands, content, builder.AddGump(commands, st.FloorCount == 4 ? RightFrame4 : RightFrame3, z, new Vector2(486, 17)));

        // Category tabs.
        Tab(commands, builder, content, (0x5654, 0x5656, 0x5655), 9, 41, CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL, TT + 1);
        Tab(commands, builder, content, (0x5657, 0x5659, 0x5658), 39, 40, CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR, TT + 2);
        Tab(commands, builder, content, (0x565A, 0x565C, 0x565B), 70, 40, CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR, TT + 3);
        Tab(commands, builder, content, (0x565D, 0x565F, 0x565E), 9, 72, CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR, TT + 4);
        Tab(commands, builder, content, (0x5788, 0x578A, 0x5789), 39, 72, CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF, TT + 5);
        Tab(commands, builder, content, (0x5663, 0x5665, 0x5664), 69, 72, CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC, TT + 6);
        Tab(commands, builder, content, (0x566C, 0x566E, 0x566D), 69, 100, CUSTOM_HOUSE_GUMP_STATE.CHGS_MENU, TT + 7);

        // Erase toggle (sprite +1 while active).
        var eraseN = (ushort)(0x5666 + (st.Erasing ? 1 : 0));
        var erase = builder.AddButton(commands, (eraseN, 0x5668, 0x5667), z, new Vector2(9, 100)).Insert<UiContainsByBounds>();
        Tip(erase, eraseN, TT + 8);
        erase.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) =>
        {
            s.Value.Erasing = !s.Value.Erasing;
            s.Value.SeekTile = false;
            s.Value.SelectedGraphic = 0;
            s.Value.CombinedStair = false;
            s.Value.Dirty = true;
        });
        Child(commands, content, erase);

        // Eyedropper / seek toggle.
        var seekN = (ushort)(0x5669 + (st.SeekTile ? 1 : 0));
        var seek = builder.AddButton(commands, (seekN, 0x566B, 0x566A), z, new Vector2(39, 100)).Insert<UiContainsByBounds>();
        Tip(seek, seekN, TT + 9);
        seek.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) =>
        {
            s.Value.SeekTile = !s.Value.SeekTile;
            s.Value.Erasing = false;
            s.Value.SelectedGraphic = 0;
            s.Value.CombinedStair = false;
            s.Value.Dirty = true;
        });
        Child(commands, content, seek);

        // Pagination.
        if (st.MaxPage > 1)
        {
            var prev = builder.AddButton(commands, (0x5625, 0x5627, 0x5626), z, new Vector2(110, 63)).Insert<UiContainsByBounds>();
            Tip(prev, 0x5625, TT + 10);
            prev.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) => s.Value.PageDelta(-1));
            Child(commands, content, prev);

            var next = builder.AddButton(commands, (0x5628, 0x562A, 0x5629), z, new Vector2(510, 63)).Insert<UiContainsByBounds>();
            Tip(next, 0x5628, TT + 11);
            next.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) => s.Value.PageDelta(1));
            Child(commands, content, next);
        }

        BuildFloorNav(commands, builder, content, st);

        // Footer counters (legacy: components RIGHT-aligned to x=82, ":" at 84,
        // fixtures left at 94, cost left at 524; all font 9 UO-ascii, bare numbers,
        // hue 0x0481 normal / 0x0026 at the cap).
        var compHue = st.Components >= st.MaxComponents ? (ushort)0x0026 : (ushort)0x0481;
        var fixHue = st.Fixtures >= st.MaxFixtures ? (ushort)0x0026 : (ushort)0x0481;
        string compText = st.Components.ToString();
        int compW = UoFontRenderer.Measure(compText, 9, 0, isHtml: false, 0, htmlBg: false, ascii: true).Width;
        Child(commands, content, Label(commands, compText, 82 - compW, 142, compHue));
        Child(commands, content, Label(commands, ":", 84, 142, 0x0481));
        Child(commands, content, Label(commands, st.Fixtures.ToString(), 94, 142, fixHue));
        Child(commands, content, Label(commands, ((st.Components + st.Fixtures) * 500).ToString(), 524, 142, 0x0481));
    }

    private static void Tab(Commands commands, GumpBuilder builder, ulong content, (ushort, ushort, ushort) ids, int x, int y, CUSTOM_HOUSE_GUMP_STATE state, uint tip)
    {
        var btn = builder.AddButton(commands, ids, Vector3.UnitZ, new Vector2(x, y)).Insert<UiContainsByBounds>();
        Tip(btn, ids.Item1, tip);
        btn.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) => s.Value.SetState(state));
        Child(commands, content, btn);
    }

    // Hover-tooltip support: stamp a synthetic OPL serial on a button's render
    // (same path as the spellbook / racial book) so TooltipPlugin shows its text.
    private const uint TT = 0x7F00_0000;

    private static EntityCommands Tip(EntityCommands btn, ushort normalArt, uint serial)
        => btn.Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = normalArt, Hue = Vector3.UnitZ, TooltipSerial = serial } });

    private static void SeedDesignTooltips(ObjectPropertyLists opl)
    {
        void T(uint s, string t) => opl.Add(s, 1, t, string.Empty, 0);
        T(TT + 1, "Walls"); T(TT + 2, "Doors"); T(TT + 3, "Floors"); T(TT + 4, "Stairs");
        T(TT + 5, "Roofs"); T(TT + 6, "Miscellaneous"); T(TT + 7, "System Menu");
        T(TT + 8, "Erase"); T(TT + 9, "Eyedropper");
        T(TT + 10, "Previous Page"); T(TT + 11, "Next Page");
        T(TT + 12, "Go to Category"); T(TT + 13, "Toggle Window"); T(TT + 14, "Lower Roof Z"); T(TT + 15, "Raise Roof Z");
        T(TT + 20, "Backup"); T(TT + 21, "Restore"); T(TT + 22, "Synchronize");
        T(TT + 23, "Clear"); T(TT + 24, "Commit"); T(TT + 25, "Revert");
        for (uint f = 1; f <= 4; f++) { T(TT + 30 + f, $"Go to floor {f}"); T(TT + 40 + f, $"Floor {f} visibility"); }
    }

    // Visibility-button art bases (legacy HouseCustomizationGump.cs:253-256).
    // floor 1 uses VisGfx1; floors 2/3/4 all use VisGfx2 (legacy quirk — VisGfx3
    // is declared but never indexed; replicated as-is for parity).
    private static readonly ushort[] VisGfx1 = { 0x572E, 0x5734, 0x5731 };
    private static readonly ushort[] VisGfx2 = { 0x5725, 0x5728, 0x572B };
    private static readonly int[] VisAssoc = { 0, 1, 2, 1, 2, 1, 2 }; // FloorVisionState -> art variant

    // Per-floor go-button art + Y rows (legacy lines 259-466). The 3-floor house
    // promotes floor 3 to the "top floor" art family; floor 4 only exists at FC4.
    private static void BuildFloorNav(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st)
    {
        int[] visY = { 108, 86, 64, 42 };
        int[] smallX = { 583, 583, 582, 583 };
        int[] smallY = { 96, 73, 56, 42 };
        int[] largeY = { 103, 86, 69, 50 };

        for (int f = 0; f < st.FloorCount; f++)
        {
            int n = f + 1;
            bool active = st.CurrentFloor == n;
            int off = active ? 3 : 0;
            int off2 = active ? 4 : 0;

            // Go-button art family by floor (and FloorCount for floor 3).
            ushort smallN, smallP, largeN, largeP, largeO;
            switch (f)
            {
                case 0: smallN = 0x56CD; smallP = 0x56D1; largeN = 0x56F6; largeP = 0x56F8; largeO = 0x56F7; break;
                case 1: smallN = 0x56CE; smallP = 0x56D2; largeN = 0x56F0; largeP = 0x56F2; largeO = 0x56F1; break;
                case 2:
                    if (st.FloorCount == 4) { smallN = 0x56CE; smallP = 0x56D2; largeN = 0x56F0; largeP = 0x56F2; largeO = 0x56F1; }
                    else { smallN = 0x56D0; smallP = 0x56D4; largeN = 0x56EA; largeP = 0x56EC; largeO = 0x56EB; }
                    break;
                default: smallN = 0x56D0; smallP = 0x56D4; largeN = 0x56EA; largeP = 0x56EC; largeO = 0x56EB; break;
            }

            // Visibility toggle (cycles FloorVisionState 0..6).
            var visArr = f == 0 ? VisGfx1 : VisGfx2;
            ushort visBase = visArr[VisAssoc[st.FloorVisionState[f]]];
            int floorIdx = f;
            var vis = builder.AddButton(commands, (visBase, (ushort)(visBase + 2), (ushort)(visBase + 1)), Vector3.UnitZ, new Vector2(533, visY[f]))
                .Insert<UiContainsByBounds>();
            Tip(vis, visBase, TT + 40 + (uint)n);
            vis.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) =>
            {
                s.Value.FloorVisionState[floorIdx] = (s.Value.FloorVisionState[floorIdx] + 1) % 7;
                s.Value.Dirty = true;
            });
            Child(commands, content, vis);

            // Two go-to-floor buttons (small + large); both jump to floor n.
            var small = builder.AddButton(commands, ((ushort)(smallN + off2), smallP, (ushort)(smallN + off2)), Vector3.UnitZ, new Vector2(smallX[f], smallY[f]))
                .Insert<UiContainsByBounds>();
            Tip(small, (ushort)(smallN + off2), TT + 30 + (uint)n);
            small.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s, Res<NetClient> net, Res<GameContext> ctx) => GoToFloor(s.Value, net.Value, ctx.Value, n));
            Child(commands, content, small);

            var large = builder.AddButton(commands, ((ushort)(largeN + off), (ushort)(largeP + off), (ushort)(largeO + off)), Vector3.UnitZ, new Vector2(623, largeY[f]))
                .Insert<UiContainsByBounds>();
            Tip(large, (ushort)(largeN + off), TT + 30 + (uint)n);
            large.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s, Res<NetClient> net, Res<GameContext> ctx) => GoToFloor(s.Value, net.Value, ctx.Value, n));
            Child(commands, content, large);
        }
    }

    // Per-floor visibility render (legacy GenerateFloorPlace + MultiView): map
    // each placed piece's Z to a floor, classify it floor-type (Floors list) vs
    // content-type, and apply the floor's FloorVisionState — full (255),
    // translucent (192, legacy AlphaHue), or hidden (0). Set each frame so the
    // world fade can't override it; on exit ProcessFade restores 255 naturally.
    private static void ApplyFloorVision(
        Res<HouseCustomizationState> stRes,
        Query<Data<WorldPosition, Graphic, HouseVisionFade>, With<CustomMulti>> piecesQ)
    {
        var st = stRes.Value;
        int baseZ = st.FoundationZ + 7;
        foreach (var (_, wp, g, vf) in piecesQ)
        {
            int rel = wp.Ref.Z - baseZ;
            int floor = rel / 20;
            if (rel < 0 || floor < 0 || floor >= 4) { vf.Ref.Mode = 0; continue; }

            int vis = st.FloorVisionState[floor];
            bool isFloor = st.ExistsInList(g.Ref.Value, out var gs) && gs == CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR;

            byte mode = 0; // 0 normal, 1 translucent, 2 hidden
            if (vis == (int)CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_ALL)
                mode = 2;
            else if (isFloor)
            {
                if (vis == (int)CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_FLOOR) mode = 2;
                else if (vis == (int)CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_FLOOR
                      || vis == (int)CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSLUCENT_FLOOR) mode = 1;
            }
            else
            {
                if (vis == (int)CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_HIDE_CONTENT) mode = 2;
                else if (vis == (int)CUSTOM_HOUSE_FLOOR_VISION_STATE.CHGVS_TRANSPARENT_CONTENT) mode = 1;
            }
            vf.Ref.Mode = mode;
        }
    }

    private static void GoToFloor(HouseCustomizationState st, NetClient net, GameContext ctx, int floor)
    {
        if (st.CurrentFloor == floor) return;
        st.CurrentFloor = floor;
        Array.Clear(st.FloorVisionState); // legacy resets ALL floors' vision on a floor change
        net.Send_CustomHouseGoToFloor(ctx.PlayerSerial, (byte)floor);
        st.Dirty = true;
    }

    // ---- content (per tab) ----

    private static void BuildContent(Commands commands, GumpBuilder builder, AssetsServer assets, ulong content, HouseCustomizationState st)
    {
        switch (st.State)
        {
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_WALL: BuildWalls(commands, builder, content, st); break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR: BuildDoors(commands, builder, content, st); break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_FLOOR: BuildFloors(commands, builder, content, st); break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR: BuildStairs(commands, builder, content, st); break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF: BuildRoofs(commands, builder, content, st); break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_MISC: BuildMiscs(commands, builder, content, st); break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_MENU: BuildMenu(commands, builder, content, st); break;
        }
    }

    private static void BuildWalls(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st)
    {
        if (st.Category == -1)
        {
            var cats = new List<(ushort, int)>();
            int start = st.Page * 16;
            for (int i = start; i < st.Walls.Count && i < start + 16; i++)
                cats.Add((st.Walls[i].Items.Count > 0 ? st.Walls[i].Items[0].Graphics[4] : (ushort)0, st.Walls[i].Index));
            BuildCategoryGrid(commands, builder, content, cats, 60);
            return;
        }

        var cat = FindCategory<CustomHouseWall, CustomHouseWallCategory>(st.Walls, st.Category);
        if (cat == null || st.Page >= cat.Items.Count) return;
        var item = cat.Items[st.Page];
        var graphics = st.ShowWindow ? item.WindowGraphics : item.Graphics;
        var cells = new List<(ushort graphic, bool combined)>();
        foreach (var g in graphics) cells.Add((g, false));
        BuildSelectGrid(commands, builder, content, cells, 120);
        AddCategoryControls(commands, builder, content, st, wallWindowToggle: true);
    }

    private static void BuildDoors(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st)
    {
        if (st.Page >= st.Doors.Count) return;
        var cells = new List<(ushort, bool)>();
        foreach (var g in st.Doors[st.Page].Graphics) cells.Add((g, false));
        BuildSelectGrid(commands, builder, content, cells, 120);
    }

    private static void BuildFloors(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st)
    {
        if (st.Page >= st.Floors.Count) return;
        var cells = new List<(ushort, bool)>();
        foreach (var g in st.Floors[st.Page].Graphics) cells.Add((g, false));
        BuildSelectGrid(commands, builder, content, cells, 60);
    }

    private static void BuildStairs(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st)
    {
        if (st.Page >= st.Stairs.Count) return;
        var stair = st.Stairs[st.Page];
        var cells = new List<(ushort, bool)>();
        // Top section: single direction pieces N/E/S/W -> combined staircases.
        for (int i = 5; i < 9; i++) cells.Add((stair.Graphics[i], true));
        // Bottom section: squared/rounded + block + north -> individual pieces.
        for (int i = 0; i < 6; i++) cells.Add((stair.Graphics[i], false));
        BuildSelectGrid(commands, builder, content, cells, 60);
    }

    private static void BuildRoofs(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st)
    {
        if (st.Category == -1)
        {
            var cats = new List<(ushort, int)>();
            int start = st.Page * 16;
            for (int i = start; i < st.Roofs.Count && i < start + 16; i++)
                cats.Add((st.Roofs[i].Items.Count > 0 ? st.Roofs[i].Items[0].Graphics[4] : (ushort)0, st.Roofs[i].Index));
            BuildCategoryGrid(commands, builder, content, cats, 60);
            return;
        }

        var cat = FindCategory<CustomHouseRoof, CustomHouseRoofCategory>(st.Roofs, st.Category);
        if (cat == null || st.Page >= cat.Items.Count) return;
        var cells = new List<(ushort, bool)>();
        foreach (var g in cat.Items[st.Page].Graphics) cells.Add((g, false));
        BuildSelectGrid(commands, builder, content, cells, 60);
        AddCategoryControls(commands, builder, content, st, roofZ: true);
    }

    private static void BuildMiscs(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st)
    {
        if (st.Category == -1)
        {
            var cats = new List<(ushort, int)>();
            int start = st.Page * 16;
            for (int i = start; i < st.Miscs.Count && i < start + 16; i++)
                cats.Add((st.Miscs[i].Items.Count > 0 ? st.Miscs[i].Items[0].Graphics[4] : (ushort)0, st.Miscs[i].Index));
            BuildCategoryGrid(commands, builder, content, cats, 60);
            return;
        }

        var cat = FindCategory<CustomHouseMisc, CustomHouseMiscCategory>(st.Miscs, st.Category);
        if (cat == null || st.Page >= cat.Items.Count) return;
        var cells = new List<(ushort, bool)>();
        foreach (var g in cat.Items[st.Page].Graphics) cells.Add((g, false));
        BuildSelectGrid(commands, builder, content, cells, 120);
        AddCategoryControls(commands, builder, content, st);
    }

    private static void BuildMenu(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st)
    {
        MenuButton(commands, builder, content, "Backup", 150, 50, 0);
        MenuButton(commands, builder, content, "Restore", 150, 90, 1);
        MenuButton(commands, builder, content, "Sync", 270, 50, 2);
        MenuButton(commands, builder, content, "Clear", 270, 90, 3);
        MenuButton(commands, builder, content, "Commit", 390, 50, 4);
        MenuButton(commands, builder, content, "Revert", 390, 90, 5);
    }

    private static void MenuButton(Commands commands, GumpBuilder builder, ulong content, string label, int x, int y, int action)
    {
        var btn = TextButton(commands, builder, label, x, y, 100);
        Tip(btn, 0x098D, TT + 20 + (uint)action);
        btn.Observe((On<UiClick> _, Res<NetClient> net, Res<GameContext> ctx) =>
        {
            uint ps = ctx.Value.PlayerSerial;
            switch (action)
            {
                case 0: net.Value.Send_CustomHouseBackup(ps); break;
                case 1: net.Value.Send_CustomHouseRestore(ps); break;
                case 2: net.Value.Send_CustomHouseSync(ps); break;
                case 3: net.Value.Send_CustomHouseClear(ps); break;
                case 4: net.Value.Send_CustomHouseCommit(ps); break;
                case 5: net.Value.Send_CustomHouseRevert(ps); break;
            }
        });
        Child(commands, content, btn);
    }

    private static void AddCategoryControls(Commands commands, GumpBuilder builder, ulong content, HouseCustomizationState st, bool wallWindowToggle = false, bool roofZ = false)
    {
        // "Go to category" back button.
        var back = builder.AddButton(commands, (0x5622, 0x5624, 0x5623), Vector3.UnitZ, new Vector2(167, 5)).Insert<UiContainsByBounds>();
        Tip(back, 0x5622, TT + 12);
        back.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) => s.Value.SetCategory(-1));
        Child(commands, content, back);

        if (wallWindowToggle)
        {
            var ids = st.ShowWindow ? ((ushort)0x562E, (ushort)0x5630, (ushort)0x562F) : ((ushort)0x562B, (ushort)0x562D, (ushort)0x562C);
            var toggle = builder.AddButton(commands, ids, Vector3.UnitZ, new Vector2(228, 9)).Insert<UiContainsByBounds>();
            Tip(toggle, ids.Item1, TT + 13);
            toggle.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) => { s.Value.ShowWindow = !s.Value.ShowWindow; s.Value.Dirty = true; });
            Child(commands, content, toggle);
        }

        if (roofZ)
        {
            var down = builder.AddButton(commands, (0x578B, 0x578D, 0x578C), Vector3.UnitZ, new Vector2(305, 5)).Insert<UiContainsByBounds>();
            Tip(down, 0x578B, TT + 14);
            down.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) => { if (s.Value.RoofZ > 1) { s.Value.RoofZ--; s.Value.Dirty = true; } });
            Child(commands, content, down);

            var up = builder.AddButton(commands, (0x578E, 0x5790, 0x578F), Vector3.UnitZ, new Vector2(349, 5)).Insert<UiContainsByBounds>();
            Tip(up, 0x578E, TT + 15);
            up.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) => { if (s.Value.RoofZ < 6) { s.Value.RoofZ++; s.Value.Dirty = true; } });
            Child(commands, content, up);

            Child(commands, content, Label(commands, $"Z {st.RoofZ}", 405, 15, 0x0481));
        }
    }

    // ---- grids ----

    private static void BuildSelectGrid(Commands commands, GumpBuilder builder, ulong content, List<(ushort graphic, bool combined)> cells, int cellH)
    {
        var grid = NewGrid(commands, content);
        ulong row = 0;
        int col = Cols;
        foreach (var (graphic, combined) in cells)
        {
            if (graphic == 0) continue;
            if (col >= Cols) { row = NewRow(commands, grid, cellH); col = 0; }
            ushort g = graphic;
            bool c = combined;
            var cell = ArtCell(commands, g, cellH);
            cell.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) => s.Value.Select(g, c));
            commands.AddChild(row, cell.Id);
            col++;
        }
    }

    private static void BuildCategoryGrid(Commands commands, GumpBuilder builder, ulong content, List<(ushort graphic, int index)> cells, int cellH)
    {
        var grid = NewGrid(commands, content);
        ulong row = 0;
        int col = Cols;
        foreach (var (graphic, index) in cells)
        {
            if (col >= Cols) { row = NewRow(commands, grid, cellH); col = 0; }
            int idx = index;
            var cell = ArtCell(commands, graphic, cellH);
            cell.Observe((On<UiClick> _, ResMut<HouseCustomizationState> s) => s.Value.SetCategory(idx));
            commands.AddChild(row, cell.Id);
            col++;
        }
    }

    private static ulong NewGrid(Commands commands, ulong content)
    {
        var grid = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(ContentX), Top = Val.Px(ContentY), Width = Val.Px(ContentW), Height = Val.Px(ContentH), Display = Display.Flex, FlexDirection = FlexDirection.Column });
        commands.AddChild(content, grid.Id);
        return grid.Id;
    }

    private static ulong NewRow(Commands commands, ulong grid, int cellH)
    {
        var row = commands.Spawn()
            .Insert(new Node { Display = Display.Flex, FlexDirection = FlexDirection.Row, Width = Val.Auto, Height = Val.Px(cellH) })
            .Insert(Interaction.None);
        commands.AddChild(grid, row.Id);
        return row.Id;
    }

    private static EntityCommands ArtCell(Commands commands, ushort graphic, int cellH)
        => commands.Spawn()
            .Insert(new Node { Display = Display.Flex, Width = Val.Px(48), Height = Val.Px(cellH) })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Art, AssetId = graphic, Hue = Vector3.UnitZ, ArtRealBounds = true } })
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UiNoWindowDrag>();

    // ---- small widgets ----

    private static EntityCommands TextButton(Commands commands, GumpBuilder builder, string label, int x, int y, int width)
    {
        // Generic UO button background (0x098D) with a centered label.
        var btn = builder.AddButton(commands, (0x098D, 0x098D, 0x098D), Vector3.UnitZ, new Vector2(x, y)).Insert<UiContainsByBounds>();
        var text = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(6), Top = Val.Px(4), Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(label))
            .Insert(new TextFont { FontId = (ushort)(1 | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(0x0036)))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();
        commands.AddChild(btn.Id, text.Id);
        return btn;
    }

    private static EntityCommands Label(Commands commands, string text, int x, int y, ushort hue)
        => commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(x), Top = Val.Px(y), Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(hue)));

    private static void Child(Commands commands, ulong parent, EntityCommands child) => commands.AddChild(parent, child.Id);

    private static TCat FindCategory<TItem, TCat>(List<TCat> list, int index)
        where TItem : CustomHouseObject where TCat : CustomHouseObjectCategory<TItem>
    {
        foreach (var c in list) if (c.Index == index) return c;
        return null;
    }

    // Count placed custom components / fixtures from the live house children
    // (legacy gump Update loop over House.Components).
    private static void RecountComponents(
        HouseCustomizationState st, NetworkEntitiesMap emap,
        Query<Data<TinyEcs.Children>> houseChildrenQ,
        Query<Empty, With<CustomMulti>> customMultiQ,
        Query<Data<Graphic>> graphicQ)
    {
        st.Components = 0;
        st.Fixtures = 0;
        if (!emap.TryGet(st.Serial, out var houseId)) return;
        if (!houseChildrenQ.TryGet(houseId, out var row)) return;
        var (_, kids) = row;
        foreach (var cid in kids.Ref)
        {
            if (!customMultiQ.Contains(cid)) continue;
            if (!graphicQ.TryGet(cid, out var gRow)) continue;
            var (_, g) = gRow;
            if (st.ExistsInList(g.Ref.Value, out var state))
            {
                if (state == CUSTOM_HOUSE_GUMP_STATE.CHGS_DOOR || state == CUSTOM_HOUSE_GUMP_STATE.CHGS_FIXTURE)
                    st.Fixtures++;
                else
                    st.Components++;
            }
        }
    }

    // ---- world placement / erase / seek ----

    private static void WorldPlacement(
        Commands commands,
        Res<MouseContext> mouse,
        Res<SelectedEntity> selected,
        ResMut<HouseCustomizationState> stRes,
        Res<NetClient> net,
        Res<GameContext> ctx,
        Res<NetworkEntitiesMap> emap,
        Local<double> lastPlaceMs,
        Local<Point> lastPos,
        Res<Time> time,
        Query<Data<WorldPosition, Graphic>> worldQ,
        Query<Data<WorldPosition, Graphic>, With<CustomMulti>> customMultiQ,
        Query<Empty, With<IsMulti>> multiQ,
        Query<Data<WorldPosition>, With<Player>> playerQ)
    {
        var st = stRes.Value;
        if (!st.Active) return;

        bool active = st.SelectedGraphic != 0 || st.Erasing || st.SeekTile;
        bool press = mouse.Value.IsPressedOnce(MouseButtonType.Left);
        bool held = mouse.Value.IsPressed(MouseButtonType.Left) || press;
        if (!active || !held)
            return;

        var ent = selected.Value.Entity;
        if (ent == 0 || !worldQ.TryGet(ent, out var row))
            return;
        var (_, posRef, gRef) = row;
        int px = posRef.Ref.X, py = posRef.Ref.Y;
        sbyte pz = posRef.Ref.Z;
        ushort hitGraphic = gRef.Ref.Value;

        if (!st.Bounds.Contains(px, py))
            return;

        // The click lands on the house plot: design mode owns it (legacy bypasses
        // normal click handling here), so movement/pickup don't also fire.
        mouse.Value.Consume(MouseButtonType.Left);

        // Throttle continuous painting + skip repeats on the same tile.
        if (px == lastPos.Value.X && py == lastPos.Value.Y && time.Value.Total - lastPlaceMs.Value < 50) return;

        int playerZ = 0;
        foreach (var (_, ppos) in playerQ) { playerZ = ppos.Ref.Z; break; }
        int zOffset = st.CurrentFloor == 1 ? -7 : -3;

        bool isMulti = customMultiQ.Contains(ent) || multiQ.Contains(ent);

        if (st.SeekTile)
        {
            if (isMulti && st.ExistsInList(hitGraphic, out var seekState))
            {
                st.State = seekState;
                st.Category = -1;
                st.SeekTile = false;
                st.SelectedGraphic = hitGraphic;
                st.UpdateMaxPage();
                st.Dirty = true;
                lastPlaceMs.Value = time.Value.Total; lastPos.Value = new Point(px, py);
            }
            return;
        }

        if (pz < playerZ + zOffset || pz >= playerZ + 20)
            return;

        lastPlaceMs.Value = time.Value.Total; lastPos.Value = new Point(px, py);

        emap.Value.TryGet(st.Serial, out var houseId);

        if (st.Erasing)
        {
            // Delete the topmost custom piece at the cursor: prefer the hovered
            // SelectedObject when it's a real piece, else (the floor marker / land
            // is on top) find the highest-Z CustomMulti at this tile.
            ulong target = 0;
            ushort delGfx = 0;
            sbyte delZ = 0;
            if (customMultiQ.Contains(ent))
            {
                target = ent; delGfx = hitGraphic; delZ = pz;
            }
            else
            {
                sbyte bestZ = sbyte.MinValue;
                foreach (var (e, wp, g) in customMultiQ)
                {
                    if (wp.Ref.X != px || wp.Ref.Y != py) continue;
                    if (target == 0 || wp.Ref.Z >= bestZ)
                    {
                        bestZ = wp.Ref.Z; target = e.Ref; delGfx = g.Ref.Value; delZ = wp.Ref.Z;
                    }
                }
            }

            if (target == 0)
                return;

            int relZ = delZ - st.FoundationZ;
            net.Value.Send_CustomHouseDeleteItem(ctx.Value.PlayerSerial, delGfx, px - st.FoundationX, py - st.FoundationY, relZ);
            commands.Entity(target).Despawn(); // optimistic remove (legacy place.Destroy)
            return;
        }

        if (st.SelectedGraphic == 0) return;
        SendPlacement(st, net.Value, ctx.Value.PlayerSerial, commands, houseId, px, py);
    }

    // World Z a piece of the current state lands at (legacy OnTargetWorld): the
    // floor base, +(RoofZ-2)*3 for roofs, the foundation Z for a single stair.
    private static sbyte PlacementZ(HouseCustomizationState st)
    {
        int baseZ = st.FoundationZ + 7 + (st.CurrentFloor - 1) * 20;
        return st.State switch
        {
            CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF => (sbyte)(baseZ + (st.RoofZ - 2) * 3),
            CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR => (sbyte)st.FoundationZ,
            _ => (sbyte)baseZ,
        };
    }

    // Translate the selected object + editor state into the 0xD7 add command,
    // mirroring HouseCustomizationManager.OnTargetWorld + CanBuildHere offsets.
    // Also spawns the piece locally (legacy house.Add) so it renders instantly —
    // this shard doesn't push an unsolicited 0xD8, so without the optimistic add
    // a placed piece wouldn't show until the gump is reopened. A later 0xD8 echo
    // (if any) reconciles by clearing + respawning all CustomMulti children.
    private static void SendPlacement(HouseCustomizationState st, NetClient net, uint ps, Commands commands, ulong houseId, int px, int py)
    {
        int fx = st.FoundationX, fy = st.FoundationY;
        int baseZ = st.FoundationZ + 7 + (st.CurrentFloor - 1) * 20;

        if (st.CombinedStair && st.State == CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR)
        {
            if (st.Page < 0 || st.Page >= st.Stairs.Count) return;
            var stair = st.Stairs[st.Page];
            ushort multi = 0;
            if (st.SelectedGraphic == stair.North) multi = (ushort)stair.MultiNorth;
            else if (st.SelectedGraphic == stair.East) multi = (ushort)stair.MultiEast;
            else if (st.SelectedGraphic == stair.South) multi = (ushort)stair.MultiSouth;
            else if (st.SelectedGraphic == stair.West) multi = (ushort)stair.MultiWest;
            if (multi != 0)
                net.Send_CustomHouseAddStair(ps, multi, px - fx, py - fy);
            // Combined staircase expands into many pieces server-side; let the
            // reopen / echo render it rather than reproducing the block layout.
            return;
        }

        switch (st.State)
        {
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_ROOF:
                int rz = (st.RoofZ - 2) * 3;
                net.Send_CustomHouseAddRoof(ps, st.SelectedGraphic, px - fx, py - fy, rz);
                SpawnLocalPiece(commands, houseId, st.SelectedGraphic, px, py, baseZ + rz);
                break;
            case CUSTOM_HOUSE_GUMP_STATE.CHGS_STAIR:
                // Single stair piece sits one tile south (CanBuildHere Y+1), at the
                // foundation base Z (legacy stair z = foundationItem.Z).
                net.Send_CustomHouseAddItem(ps, st.SelectedGraphic, px - fx, py - fy + 1);
                SpawnLocalPiece(commands, houseId, st.SelectedGraphic, px, py + 1, st.FoundationZ);
                break;
            default:
                net.Send_CustomHouseAddItem(ps, st.SelectedGraphic, px - fx, py - fy);
                SpawnLocalPiece(commands, houseId, st.SelectedGraphic, px, py, baseZ);
                break;
        }
    }

    // Spawn a placed piece as a CustomMulti house child (renders like the server
    // planes; backfilled with ScreenPosition/AlphaFade by the world render pass).
    private static void SpawnLocalPiece(Commands commands, ulong houseId, ushort graphic, int x, int y, int z)
    {
        if (houseId == 0) return;
        var m = commands.Spawn()
            .Insert(new Graphic { Value = graphic })
            .Insert(new Hue())
            .Insert(new WorldPosition { X = (ushort)x, Y = (ushort)y, Z = (sbyte)z })
            .Insert(new HouseVisionFade())
            .Insert<CustomMulti>();
        commands.AddChild(houseId, m.Id);
    }
}
