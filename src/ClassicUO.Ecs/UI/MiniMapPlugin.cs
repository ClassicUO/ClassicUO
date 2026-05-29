// MiniMap (radar) gump — ECS port of Game/UI/Gumps/MiniMapGump.cs.
//
// A single draggable square window: bg gump 0x1392 (small) / 0x1393 (large),
// left-double-click toggles size. The radar terrain + mobile/player dots are
// baked each frame into a Texture2D carried on the root's UOCustomRender.Dynamic
// (option A from the spec): a Stage.Update system does the heavy map-block walk
// with proper Res<UOFileManager> access, the renderer just blits the texture.
//
// Drag / right-click-close / z-stack / pixel-hit come free from UIMovable +
// UOGumpBundle + the new UiHitTest.MiniMap case (shares the bg-gump mask).
//
// v1 scope vs legacy: LAND-only radar. Statics + live multis need the Chunk
// tile-grid that ECS doesn't model as a grid (spec open question — deferred).
// Mobile dots are drawn red (no Notoriety component in ECS yet) — notoriety
// hueing is deferred until a Notoriety component is persisted from packets.

using ClassicUO.Assets;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Tag on the top-bar Map button. MiniMapPlugin's global On<UiClick> observer
// Marker on the window root. Carries the current size toggle so double-click
// can flip it and the bake system can rebuild caches when it changes.
internal struct MiniMapWindow { public bool UseLargeMap; }

internal sealed class MiniMapState
{
    public const ushort SmallGraphic = 0x1392; // 5010
    public const ushort BigGraphic = 0x1393;   // 5011

    // 0xFF080808 is the legacy "blank" sentinel inside the radar gumps — radar
    // pixels are only written where the bg sprite has this fill colour.
    public const uint BlankSentinel = 0xFF080808;

    public bool BlinkOn = true;
    public float NextBlinkMs;

    // Cache state. Rebuild blank+texture when size flips; rebuild radar when the
    // player moves or the map changes.
    public bool CachedLarge;
    public int CachedW = -1, CachedH = -1;
    public int LastX = int.MinValue, LastY = int.MinValue, LastMap = int.MinValue;

    public uint[] Blank;   // bg gump pixels (frame + sentinel interior)
    public uint[] Radar;   // blank + baked radar terrain
    public uint[] Work;    // radar + dots, uploaded to the texture
    public Texture2D Texture;

    public void DisposeTexture()
    {
        Texture?.Dispose();
        Texture = null;
        CachedW = CachedH = -1;
        LastX = LastY = LastMap = int.MinValue;
    }
}

internal readonly struct MiniMapPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new MiniMapState());

        var bakeFn = Bake;
        var disposeFn = DisposeOnLogout;

        app.AddSystem(bakeFn).InStage(Stage.Update).SingleThreaded().Build();
        app.AddSystem(disposeFn).OnExit(GameState.GameScreen).Build();

        // Open is wired from the top-bar Map button via OpenOrFocus (entity-
        // scoped observer in TopBarPlugin, reached by UiClick bubbling).

        // Left double-click toggles small/large (legacy OnMouseDoubleClick).
        app.AddObserver((
            On<UiDoubleClick> trig,
            Res<AssetsServer> assets,
            Query<Data<MiniMapWindow, UiCustom, Node>> q) =>
        {
            if (!q.Contains(trig.EntityId)) return;
            var (_, win, custom, node) = q.Get(trig.EntityId);
            win.Ref.UseLargeMap = !win.Ref.UseLargeMap;
            var bgId = win.Ref.UseLargeMap ? MiniMapState.BigGraphic : MiniMapState.SmallGraphic;
            ref readonly var gi = ref assets.Value.Gumps.GetGump(bgId);
            custom.Ref.Render().AssetId = bgId;
            node.Ref.Width = Val.Px(gi.UV.Width);
            node.Ref.Height = Val.Px(gi.UV.Height);
        });
    }

    // Spawn the minimap window, or focus it (bump z) if already open. Called from
    // the top-bar Map button's UiClick observer.
    public static void OpenOrFocus(Commands commands, GumpBuilder builder, UiZCounter zCounter, Query<Data<MiniMapWindow>> existingQ)
    {
        foreach (var (ent, _) in existingQ)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
            return;
        }

        var spawnPos = new Vector2(550, 30);
        builder.SpawnUOGump(commands, MiniMapState.SmallGraphic, Vector3.UnitZ, spawnPos, zCounter)
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.MiniMap,
                    AssetId = MiniMapState.SmallGraphic,
                    Hue = Vector3.UnitZ,
                }
            })
            .Insert(new MiniMapWindow { UseLargeMap = false });
    }

    private static void Bake(
        Res<Time> time,
        Res<GameContext> gameCtx,
        Res<GraphicsDevice> device,
        Res<AssetsServer> assets,
        Res<UOFileManager> files,
        ResMut<MiniMapState> stateRes,
        Query<Data<UiCustom, MiniMapWindow>> windowQ,
        Query<Data<WorldPosition>, Filter<With<Player>>> playerQ,
        Query<Data<WorldPosition>, Filter<With<Mobiles>, Without<Player>>> mobileQ)
    {
        var state = stateRes.Value;

        // Blink toggle on the engine clock (never wall-clock).
        if (time.Value.Total >= state.NextBlinkMs)
        {
            state.BlinkOn = !state.BlinkOn;
            state.NextBlinkMs = time.Value.Total + 500f;
        }

        // The single minimap window (if any).
        ulong winEnt = 0;
        UOCustomRender payload = null;
        bool large = false;
        foreach (var (ent, custom, win) in windowQ)
        {
            winEnt = ent.Ref;
            payload = custom.Ref.Render();
            large = win.Ref.UseLargeMap;
            break;
        }
        if (winEnt == 0 || payload == null) return;

        int map = gameCtx.Value.Map;
        if (map < 0) return;

        int px = int.MinValue, py = int.MinValue;
        foreach (var (_, pos) in playerQ)
        {
            px = pos.Ref.X; py = pos.Ref.Y; break;
        }
        if (px == int.MinValue) return;

        var bgId = large ? MiniMapState.BigGraphic : MiniMapState.SmallGraphic;
        ref readonly var gi = ref assets.Value.Gumps.GetGump(bgId);
        if (gi.Texture == null || gi.UV.Width <= 0 || gi.UV.Height <= 0) return;
        int w = gi.UV.Width, h = gi.UV.Height;

        // (Re)load blank pixels + (re)alloc buffers + (re)create texture when the
        // size changes (initial open or double-click toggle).
        if (state.CachedW != w || state.CachedH != h || state.CachedLarge != large || state.Texture == null)
        {
            int size = w * h;
            state.Blank = new uint[size];
            state.Radar = new uint[size];
            state.Work = new uint[size];
            gi.Texture.GetData(0, gi.UV, state.Blank, 0, size);
            state.Texture?.Dispose();
            state.Texture = new Texture2D(device.Value, w, h);
            state.CachedW = w; state.CachedH = h; state.CachedLarge = large;
            state.LastX = state.LastY = state.LastMap = int.MinValue; // force radar rebuild
        }

        // Rebuild radar terrain only when the player moved or the map changed.
        if (px != state.LastX || py != state.LastY || map != state.LastMap)
        {
            BakeRadar(files.Value, state, map, px, py, w, h);
            state.LastX = px; state.LastY = py; state.LastMap = map;
        }

        // Per-frame: radar -> work, then stamp dots when the blink phase is on.
        System.Array.Copy(state.Radar, state.Work, state.Radar.Length);
        if (state.BlinkOn)
        {
            int cx = w >> 1, cy = h >> 1;
            uint mobColor = HuesHelper.Color16To32(0x7C00) | 0xFF000000; // red
            uint playerColor = HuesHelper.Color16To32(0x7FFF) | 0xFF000000; // white
            foreach (var (_, pos) in mobileQ)
            {
                int xx = pos.Ref.X - px;
                int yy = pos.Ref.Y - py;
                Stamp2x2(state.Work, w, h, cx + (xx - yy), cy + (xx + yy), mobColor);
            }
            Stamp2x2(state.Work, w, h, cx, cy, playerColor);
        }

        state.Texture.SetData(state.Work);
        payload.Dynamic = state.Texture;
    }

    // Land-only radar bake. Mirrors MiniMapGump.CreateMiniMapTexture: walk the
    // map blocks around the player, project each cell isometrically, and stamp a
    // 2px vertical radar-coloured mark over the blank sentinel pixels.
    private static void BakeRadar(UOFileManager files, MiniMapState state, int map, int lastX, int lastY, int w, int h)
    {
        System.Array.Copy(state.Blank, state.Radar, state.Blank.Length);

        var maps = files.Maps;
        var hues = files.Hues;

        int blockOffsetX = w >> 2;
        int blockOffsetY = h >> 2;
        int gumpCenterX = w >> 1;

        int minBlockX = ((lastX - blockOffsetX) >> 3) - 1;
        int minBlockY = ((lastY - blockOffsetY) >> 3) - 1;
        int maxBlockX = ((lastX + blockOffsetX) >> 3) + 1;
        int maxBlockY = ((lastY + blockOffsetY) >> 3) + 1;
        if (minBlockX < 0) minBlockX = 0;
        if (minBlockY < 0) minBlockY = 0;

        int mapBlockHeight = maps.MapBlocksSize[map, 1];
        int maxBlockIndex = maps.MapBlocksSize[map, 0] * mapBlockHeight;

        for (int i = minBlockX; i <= maxBlockX; i++)
        {
            int blockIndexOffset = i * mapBlockHeight;
            for (int j = minBlockY; j <= maxBlockY; j++)
            {
                int blockIndex = blockIndexOffset + j;
                if (blockIndex >= maxBlockIndex) break;

                ref var im = ref maps.GetIndex(map, i, j);
                if (!im.IsValid()) break;

                im.MapFile.Seek((long)im.MapAddress, System.IO.SeekOrigin.Begin);
                var cells = im.MapFile.Read<MapBlock>().Cells;

                int realBlockX = i << 3;
                int realBlockY = j << 3;

                for (int x = 0; x < 8; x++)
                {
                    int cpx = realBlockX + x - lastX + gumpCenterX;
                    for (int y = 0; y < 8; y++)
                    {
                        int color = cells[(y << 3) + x].TileID;
                        ushort radar = hues.GetRadarColorData(color);

                        int cpy = realBlockY + y - lastY;
                        int gx = cpx - cpy;
                        int gy = cpx + cpy;

                        uint argb = HuesHelper.Color16To32((ushort)(0x8000 | radar)) | 0xFF000000;
                        StampPixel(state.Radar, w, h, gx, gy, argb);
                        StampPixel(state.Radar, w, h, gx, gy + 1, argb);
                    }
                }
            }
        }
    }

    // Radar pixel: only paints over the blank sentinel (matches legacy
    // CreatePixels), so the gump frame stays intact.
    private static void StampPixel(uint[] data, int w, int h, int x, int y, uint color)
    {
        if (x < 0 || x >= w || y < 0 || y >= h) return;
        int idx = y * w + x;
        if (data[idx] == MiniMapState.BlankSentinel)
            data[idx] = color;
    }

    // Dot: 2x2 overwrite (drawn on top, like legacy's separate dot sprites).
    private static void Stamp2x2(uint[] data, int w, int h, int x, int y, uint color)
    {
        for (int dy = 0; dy < 2; dy++)
        for (int dx = 0; dx < 2; dx++)
        {
            int gx = x + dx, gy = y + dy;
            if (gx < 0 || gx >= w || gy < 0 || gy >= h) continue;
            data[gy * w + gx] = color;
        }
    }

    private static void DisposeOnLogout(
        Commands commands,
        ResMut<MiniMapState> state,
        Query<Data<MiniMapWindow>> windowsQ)
    {
        foreach (var (ent, _) in windowsQ)
            commands.Entity(ent.Ref).Despawn();
        state.Value.DisposeTexture();
    }
}
