using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Runtime.CompilerServices;
using ClassicUO.Utility;
using TinyEcs;
using TinyEcs.Bevy;
using World = TinyEcs.World;

namespace ClassicUO.Ecs;

// Bundles render inputs that don't fit the Rendering system's 16-param budget:
// the normal-multi membership query (forwarded to RenderStatics) plus the
// keyboard + party state the feet aura needs.
internal sealed class WorldAuraInputs : CompositeSystemParam
{
    public readonly Query<Empty, With<NormalMulti>> NormalMultis;
    public readonly Res<KeyboardContext> Keyboard;
    public readonly Res<PartyState> Party;
    public readonly Res<EffectsState> Effects;

    public WorldAuraInputs()
    {
        NormalMultis = Add(new Query<Empty, With<NormalMulti>>());
        Keyboard = Add(new Res<KeyboardContext>());
        Party = Add(new Res<PartyState>());
        Effects = Add(new Res<EffectsState>());
    }
}

internal readonly struct WorldRenderingPlugin : IPlugin
{
    public void Build(App app)
    {
        var cleanupFn = Cleanup;
        var beginRenderingFn = BeginRendering;
        var renderingFn = Rendering;
        var endRenderingFn = EndRendering;

        // TODO: find a better place to initialize this
        app.AddResource(new Profile()
        {
            GameWindowPosition = new(20, 40)
        });
        app.AddResource(new SelectedEntity());
        app.AddResource(new Viewport());

        app
            .AddSystem((Res<MouseContext> mouseCtx, Res<KeyboardContext> keyboardCtx, ResMut<GameContext> gameCtx, Res<Camera> camera, Res<GrabbedItem> grabbed, Local<bool> canMove) =>
            {
                if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
                {
                    canMove.Value = camera.Value.Bounds.Contains((int)mouseCtx.Value.Position.X, (int)mouseCtx.Value.Position.Y);
                }

                // Suspend camera-offset dragging while an item is held by the
                // cursor — otherwise hauling an item across the world view
                // pans the camera by the same delta.
                if (canMove.Value && mouseCtx.Value.IsPressed(Input.MouseButtonType.Left) && grabbed.Value.Serial == 0)
                {
                    gameCtx.Value.CenterOffset += mouseCtx.Value.PositionOffset * camera.Value.Zoom;
                }

                if (keyboardCtx.Value.IsPressedOnce(Keys.Space))
                {
                    gameCtx.Value.FreeView = !gameCtx.Value.FreeView;
                }
            })
            .InStage(Stage.First)
            .RunIf((Res<UoGame> game) => game.Value.IsActive)
            .Build()

            .AddSystem((ResMut<GameContext> gameCtx, Query<Data<WorldPosition, ScreenPositionOffset>, With<Player>> playerQuery) =>
            {
                foreach ((var position, var offset) in playerQuery)
                {
                    gameCtx.Value.CenterX = position.Ref.X;
                    gameCtx.Value.CenterY = position.Ref.Y;
                    gameCtx.Value.CenterZ = position.Ref.Z;
                    gameCtx.Value.CenterOffset = offset.Ref.Value * -1;
                }
            })
            .InStage(Stage.Update)
            .RunIf((Res<GameContext> gameCtx) => !gameCtx.Value.FreeView)
            .Build()

            .AddSystem(cleanupFn)
            .OnExit(GameState.GameScreen)
            .Build()

            .AddSystem((Commands commands,
                        Query<Data<WorldPosition>, Without<ScreenPosition>> query2,
                        Query<Data<WorldPosition, ScreenPosition>, Changed<WorldPosition>> query,
                        Query<Data<WorldPosition>, Without<AlphaFade>> queryNoFade) =>
            {
                foreach ((var ent, var worldPos) in query2)
                {
                    var iso = worldPos.Ref.WorldToScreen();
                    commands.Entity(ent.Ref).Insert(new ScreenPosition() { Value = iso });
                }

                foreach ((var ent, _) in queryNoFade)
                {
                    commands.Entity(ent.Ref).Insert(new AlphaFade());
                }

                foreach ((var worldPos, var screenPos) in query)
                {
                    var iso = worldPos.Ref.WorldToScreen();
                    screenPos.Ref.Value = iso;
                }
            })
            .InStage(Stage.Update)
            .Build()

            .AddSystem(beginRenderingFn)
            .InStage(Stage.PostUpdate)
            .SingleThreaded()
            .Label("cuo:rendering:begin")
            .RunIf((Res<UoGame> game) => game.Value.IsDrawable)
            .Build()

            .AddSystem(renderingFn)
            .InStage(Stage.PostUpdate)
            .SingleThreaded()
            .Label("cuo:rendering:rendering")
            .After("cuo:rendering:begin")
            .RunIf((Commands cmds) => cmds.HasResource<GraphicsDevice>())
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Query<Data<WorldPosition>, With<Player>> playerQuery) => playerQuery.Count() > 0)
            .RunIf((Res<UoGame> game) => game.Value.IsDrawable)
            .Build()

            .AddSystem(endRenderingFn)
            .InStage(Stage.PostUpdate)
            .SingleThreaded()
            .Label("cuo:rendering:end")
            .After("cuo:rendering:rendering")
            .RunIf((Res<UoGame> game) => game.Value.IsDrawable)
            .Build();
    }

    private static void Cleanup(Res<SelectedEntity> selectedEntity)
    {
        selectedEntity.Value.Clear();
    }

    private static void BeginRendering(
        Res<Camera> camera,
        Res<RenderTarget2D> renderTarget,
        Res<UltimaBatcher2D> batch,
        ResMut<Viewport> viewport
    )
    {
        viewport.Value = batch.Value.GraphicsDevice.Viewport;

        // RT may not exist on the very first frame after GameScreen entry
        // (BindRenderTarget runs in the same stage, no ordering guarantee).
        // Bail rather than NPE — the next frame will succeed.
        if (renderTarget.Value == null) return;

        // World RT now sized at LOGICAL viewport dim (matches main's
        // backbuffer/DpiScale sizing). Set viewport to full RT so the
        // camera transform projects into the full RT space; Clay's
        // panel sampler does the final upscale to display dim.
        batch.Value.GraphicsDevice.SetRenderTarget(renderTarget.Value);
        batch.Value.GraphicsDevice.Viewport = new Viewport(0, 0, renderTarget.Value.Width, renderTarget.Value.Height);
        batch.Value.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);
    }

    private static void EndRendering(Res<UltimaBatcher2D> batch, Res<Viewport> viewport)
    {
        batch.Value.GraphicsDevice.SetRenderTarget(null);
        batch.Value.GraphicsDevice.Clear(ClearOptions.Target, new Color(18f / 255f, 18f / 255f, 18f / 255f, 1f), 0, 0);
        batch.Value.GraphicsDevice.Viewport = viewport.Value;
    }

    private static void Rendering(
        Res<SelectedEntity> selectedEntity,
        Res<GameContext> gameCtx,
        Res<Renderer.UltimaBatcher2D> batch,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Res<Camera> camera,
        Res<Profile> profile,
        Res<Time> time,
        Local<RenderScratch> scratch,
        Query<Data<Graphic, Hue>> qLayers,
        WorldAuraInputs auraInputs,
        Single<Data<WorldPosition>, With<Player>> queryPlayer,
        Query<Data<WorldPosition, ScreenPosition, Graphic, TileStretched, AlphaFade>, Filter<With<IsTile>, Optional<TileStretched>>> queryTiles,
        Query<Data<WorldPosition, ScreenPosition, Graphic, Hue, Amount, NetworkSerial, Facing, AlphaFade, HouseVisionFade>, Filter<Without<IsTile>, Without<MobAnimation>, Without<ContainedInto>, Optional<Amount>, Optional<NetworkSerial>, Optional<Facing>, Optional<HouseVisionFade>>> queryStatics,
        Query<Data<WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Facing, MobAnimation, MobileSteps, ServerFlags, Notoriety, AlphaFade>,
            Filter<Without<ContainedInto>, Optional<Facing>, Optional<MobAnimation>, Optional<MobileSteps>, Optional<ServerFlags>, Optional<Notoriety>>> queryBodyOnly,
        Query<Data<EquipmentSlots, ScreenPositionOffset, WorldPosition, Graphic, Facing, MobileSteps, MobAnimation, ServerFlags, Notoriety, AlphaFade>,
            Filter<Without<ContainedInto>, Optional<MobileSteps>, Optional<MobAnimation>, Optional<ServerFlags>, Optional<Notoriety>>> queryEquipmentSlots
    )
    {
        // Setup rendering state. World RT is sized at logical viewport
        // dim (see BindRenderTarget); use the bare camera transform so
        // the world renders at logical scale, then Clay's panel sampler
        // upscales to display size — matches main's pixel-art aesthetic.
        batch.Value.Begin(null, camera.Value.ViewTransformMatrix);
        batch.Value.SetBrightlight(profile.Value.TerrainShadowsLevel * 0.1f);
        batch.Value.SetSampler(SamplerState.PointClamp);
        batch.Value.SetStencil(DepthStencilState.Default);

        // Shader-side circle of transparency (type 0 = full). Gradient mode
        // (type 1) is CPU alpha, computed per static below.
        var cotFull = profile.Value.UseCircleOfTransparency && profile.Value.CircleOfTransparencyType != 1;
        batch.Value.SetCircleOfTransparencyRadius(
            cotFull ? profile.Value.CircleOfTransparencyRadius / camera.Value.Zoom : 0f);

        // Get player position and calculate visibility information
        (var playerEnt, var playerPos) = queryPlayer.Get();
        (var playerX, var playerY, var playerZ) = playerPos.Ref;

        int? maxZ = null;
        var playerZ16 = playerZ + 16;
        var playerZ14 = playerZ + 14;

        var playerDead = false;
        if (qLayers.TryGet(playerEnt.Ref, out var playerGfxRow))
        {
            (var pGfx, _) = playerGfxRow;
            playerDead = IsDeadBody(pGfx.Ref.Value);
        }

        var waterScale = Vector2.One;
        if (profile.Value.AnimatedWaterEffect)
        {
            var sin = MathF.Sin(time.Value.Total / 1000f);
            var cos = MathF.Cos(time.Value.Total / 1000f);
            waterScale = new Vector2(1.1f + sin * 0.1f, 1.1f + cos * 0.5f * 0.1f);
        }

        // Fade tick — legacy GameScene._alphaChanged: alpha steps at most once
        // per ALPHA_TIME ms, not per frame.
        var alphaChanged = time.Value.Total > scratch.Value.AlphaTime;
        if (alphaChanged)
            scratch.Value.AlphaTime = time.Value.Total + Constants.ALPHA_TIME;

        // Advance animated-static frame offsets (legacy AnimatedStaticsManager).
        scratch.Value.AnimStatics ??= new AnimatedStatics();
        scratch.Value.AnimStatics.Tick(fileManager.Value, time.Value.Total, profile.Value.FieldsType);

        var fx = new WorldFx
        {
            AlphaChanged = alphaChanged,
            ObjectsFading = profile.Value.UseObjectsFading,
            GrayWorld = playerDead && profile.Value.EnableBlackWhiteEffect,
            ViewRange = gameCtx.Value.MaxObjectsDistance,
            PlayerX = playerX,
            PlayerY = playerY,
            PlayerZ5 = playerZ + 5,
            WaterScale = waterScale,
            CotFull = cotFull,
            CotGradient = profile.Value.UseCircleOfTransparency && profile.Value.CircleOfTransparencyType == 1,
            CotRadiusSq = (float)profile.Value.CircleOfTransparencyRadius * profile.Value.CircleOfTransparencyRadius,
            CotCenter = Isometric.IsoToScreen(playerX, playerY, playerZ),
        };

        ref var lastPos = ref scratch.Value.LastPos;
        ref var workingZInfo = ref scratch.Value.ZInfo;

        var calculateZ = !lastPos.HasValue ||
                        lastPos.Value.lastPosX != playerX ||
                        lastPos.Value.lastPosY != playerY ||
                        lastPos.Value.lastPosZ != playerZ;

        var backupZInfo = workingZInfo;

        if (calculateZ)
        {
            workingZInfo.MaxZ = null;
            workingZInfo.MaxZGround = null;
            workingZInfo.MaxZRoof = null;
            workingZInfo.DrawRoof = true;
            workingZInfo.IsSameTile = false;
            workingZInfo.IsTileAhead = false;
            workingZInfo.IsUnderStatic = false;
            lastPos = (playerX, playerY, playerZ);
        }

        // Calculate maxZ based on environment
        if (backupZInfo.IsUnderStatic && backupZInfo.IsUnderRoof)
        {
            maxZ = backupZInfo.MaxZRoof < backupZInfo.MaxZ ? backupZInfo.MaxZRoof : backupZInfo.MaxZ;
        }
        else if (backupZInfo.IsUnderStatic)
        {
            maxZ = backupZInfo.MaxZ;
        }
        else if (backupZInfo.IsUnderRoof)
        {
            maxZ = backupZInfo.MaxZRoof;
        }
        else
        {
            backupZInfo.DrawRoof = true;
        }

        if (backupZInfo.MaxZGround.HasValue && backupZInfo.MaxZGround < maxZ)
        {
            maxZ = backupZInfo.MaxZGround.Value;
        }

        if (maxZ.HasValue && maxZ < playerZ16)
        {
            maxZ = playerZ16;
        }

        // Foliage transparency — legacy CheckIfBehindATree marking pass runs
        // before the draw so this frame's statics fade with fresh targets.
        var foliageMarks = scratch.Value.FoliageMarks ??= new HashSet<long>();
        MarkFoliage(
            foliageMarks, assetsServer, fileManager, profile.Value,
            playerX, playerY, maxZ,
            PlayerScreenRect(assetsServer, playerEnt.Ref, queryBodyOnly),
            queryStatics);
        fx.FoliageMarks = foliageMarks;
        fx.FoliageDraws = scratch.Value.FoliageDraws ??= new List<FoliageDraw>();

        // Calculate camera-related values once
        var center = Isometric.IsoToScreen(gameCtx.Value.CenterX, gameCtx.Value.CenterY, gameCtx.Value.CenterZ);
        center.X -= camera.Value.Bounds.Width / 2f;
        center.Y -= camera.Value.Bounds.Height / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.Value.CenterOffset;

        var mousePos = camera.Value.MouseToWorldPosition2();
        selectedEntity.Value.Enabled = camera.Value.IsMouseInsideBounds();
        selectedEntity.Value.Clear();

        var cameraBounds = camera.Value.Bounds;
        var drawOffset = (int)(44 / camera.Value.Zoom);
        cameraBounds.Location = camera.Value.ScreenToWorld(new Point(-drawOffset, -drawOffset));
        var s = camera.Value.ScreenToWorld(new Point(cameraBounds.Width + drawOffset, cameraBounds.Height + drawOffset));
        cameraBounds.Width = s.X;
        cameraBounds.Height = s.Y;

        // Reset the dynamic-light buffer; RenderStatics refills it from the
        // sources it actually draws (consumed by LightingPlugin after the world
        // pass ends). The occlusion scratch is reused across frames.
        gameCtx.Value.Lights.Count = 0;
        var lightOccluders = scratch.Value.LightOccluders ??= new Dictionary<long, int>();
        var pendingLights = scratch.Value.PendingLights ??= new List<PendingLight>();
        lightOccluders.Clear();
        pendingLights.Clear();

        // Render each layer
        RenderTiles(
            selectedEntity, gameCtx, batch, assetsServer, fileManager,
            camera, profile.Value, in fx, calculateZ, ref workingZInfo, playerX, playerY, playerZ16,
            backupZInfo, maxZ, center, mousePos, cameraBounds, queryTiles);

        RenderStatics(
            selectedEntity, gameCtx, batch, assetsServer, fileManager,
            camera, profile.Value, in fx, calculateZ, ref workingZInfo, playerX, playerY, playerZ14,
            backupZInfo, maxZ, center, mousePos, cameraBounds, auraInputs.NormalMultis, queryStatics,
            lightOccluders, pendingLights, scratch.Value.AnimStatics);

        // Feet-aura gating (legacy AuraManager.IsEnabled): mode 1 needs the
        // player's warmode flag, mode 2 needs Ctrl+Shift held.
        bool auraCtrlShift =
            (auraInputs.Keyboard.Value.IsPressed(Keys.LeftControl) || auraInputs.Keyboard.Value.IsPressed(Keys.RightControl)) &&
            (auraInputs.Keyboard.Value.IsPressed(Keys.LeftShift) || auraInputs.Keyboard.Value.IsPressed(Keys.RightShift));
        bool auraWarmode = false;
        if (profile.Value.AuraUnderFeetType == 1 && queryBodyOnly.TryGet(playerEnt.Ref, out var pbRow))
        {
            var (_, _, _, _, _, _, _, _, pFlags, _, _) = pbRow;
            auraWarmode = pFlags.IsValid() && (pFlags.Ref.Value & Flags.WarMode) != 0;
        }
        bool auraFeet = AuraOverlay.FeetEnabled(profile.Value, auraWarmode, auraCtrlShift);

        // Feet auras as a ground-decal pass: depth buffer OFF so the circle isn't
        // clipped by the tile in front and doesn't occlude mobiles behind it. Runs
        // after statics, before bodies — the bodies (drawn next, depth ON) paint
        // over it, so it reads as "under the feet".
        if (auraFeet)
            RenderFeetAuras(batch, profile.Value, center, auraInputs.Party.Value, queryBodyOnly);

        RenderBodies(
            selectedEntity, batch, assetsServer, fileManager,
            profile.Value, in fx, maxZ, center, mousePos, queryBodyOnly);

        RenderEquipment(
            selectedEntity, batch, assetsServer, fileManager,
            profile.Value, in fx, maxZ, center, mousePos, qLayers, queryEquipmentSlots);

        RenderEffects(
            batch, assetsServer, fileManager, profile.Value,
            auraInputs.Effects.Value, center, in fx, playerX, playerY, playerZ);

        // Late transparent pass — foliage deferred from RenderStatics blends
        // over the mobiles drawn above; depth test still hides it behind
        // statics in front of it.
        foreach (ref var d in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(fx.FoliageDraws))
        {
            batch.Value.Draw(
                d.Texture,
                d.Position,
                d.UV,
                d.Color,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
                d.Depth
            );
        }
        fx.FoliageDraws.Clear();

        // Clean up resources - only change state if necessary
        batch.Value.SetSampler(null);
        batch.Value.SetStencil(null);
        batch.Value.SetCircleOfTransparencyRadius(0f);
        batch.Value.End();
    }

    // Per-frame derived render settings shared by the Render* helpers —
    // computed once from Profile + player state at the top of Rendering.
    private struct WorldFx
    {
        public bool GrayWorld;          // player dead + EnableBlackWhiteEffect
        public int ViewRange;           // server view range (0xC8)
        public int PlayerX, PlayerY, PlayerZ5;
        public Vector2 WaterScale;      // AnimatedWaterEffect overlay scale
        public bool CotFull, CotGradient;
        public float CotRadiusSq;
        public Vector2 CotCenter;       // player iso screen pos
        public bool AlphaChanged;       // ALPHA_TIME tick elapsed this frame
        public bool ObjectsFading;      // Profile.UseObjectsFading
        public HashSet<long> FoliageMarks; // foliage keys hiding the player this frame
        public List<FoliageDraw> FoliageDraws; // semi-transparent foliage, drawn after mobiles
    }

    private struct RenderScratch
    {
        public (int lastPosX, int lastPosY, int lastPosZ)? LastPos;
        public MaxZInfo ZInfo;
        public float AlphaTime;
        public HashSet<long> FoliageMarks;
        public List<FoliageDraw> FoliageDraws;
        // Dynamic-light occlusion (legacy GameScene.AddLight overhang test).
        // LightOccluders maps a light tile (occluderX-1, occluderY-1) → the
        // tallest non-transparent static/multi z (below the roof ceiling) on
        // its SE neighbour; a light is dropped if that z reaches lightZ+5.
        // Lights are collected during the static pass and resolved after it so
        // the occluder map is complete (an occluder may be iterated after the
        // light it blocks).
        public Dictionary<long, int> LightOccluders;
        public List<PendingLight> PendingLights;
        public AnimatedStatics AnimStatics;
    }

    private struct PendingLight
    {
        public ushort Graphic;
        public byte Id;
        public float DrawX, DrawY;
        public int TileX, TileY, Z;
    }

    // Static art flagged IsAnimated (animdata.mul) cycles through frame deltas.
    // Legacy AnimatedStaticsManager mutates Arts.Entries[gfx+0x4000].AnimOffset
    // each tick; we keep an offset-by-graphic table instead and add it to the
    // displayed graphic at render. Ticked from the world render system (it owns
    // Res<Time>/files/profile and stays under the 16-param system cap).
    internal sealed class AnimatedStatics
    {
        private struct Entry { public ushort Index; public float Time; public byte AnimIndex; public bool IsField; }

        private Entry[] _entries = Array.Empty<Entry>();
        private sbyte[] _offsets = Array.Empty<sbyte>();
        private float _processTime;
        private bool _init;

        public sbyte Offset(ushort graphic) => graphic < _offsets.Length ? _offsets[graphic] : (sbyte)0;

        public void Tick(UOFileManager files, float total, int fieldsType)
        {
            var animData = files.AnimData;
            if (animData.AnimDataFile == null)
                return;

            if (!_init)
            {
                _init = true;
                var staticData = files.TileData.StaticData;
                _offsets = new sbyte[staticData.Length];
                var list = new List<Entry>();
                for (int i = 0; i < staticData.Length; i++)
                {
                    if (!staticData[i].IsAnimated)
                        continue;
                    var g = (ushort)i;
                    list.Add(new Entry
                    {
                        Index = g,
                        IsField = StaticFilters.IsFireField(g) || StaticFilters.IsParalyzeField(g)
                            || StaticFilters.IsEnergyField(g) || StaticFilters.IsPoisonField(g),
                    });
                }
                _entries = list.ToArray();
            }

            if (_entries.Length == 0 || _processTime >= total)
                return;

            // legacy AnimatedStaticsManager: ITEM_EFFECT_ANIMATION_DELAY * 2.
            float delay = Constants.ITEM_EFFECT_ANIMATION_DELAY * 2;
            float nextTime = total + 250;
            bool noAnimatedField = fieldsType != 0;

            for (int i = 0; i < _entries.Length; i++)
            {
                ref var o = ref _entries[i];

                if (noAnimatedField && o.IsField)
                {
                    o.AnimIndex = 0;
                    _offsets[o.Index] = 0;
                    continue;
                }

                if (o.Time < total)
                {
                    var info = animData.CalculateCurrentGraphic(o.Index);
                    byte offset = o.AnimIndex;

                    o.Time = info.FrameInterval > 0 ? total + info.FrameInterval * delay + 1 : total + delay;

                    if (offset < info.FrameCount)
                        _offsets[o.Index] = info.FrameAt(offset++);

                    if (offset >= info.FrameCount)
                        offset = 0;

                    o.AnimIndex = offset;
                }

                if (o.Time < nextTime)
                    nextTime = o.Time;
            }

            _processTime = nextTime;
        }
    }

    // Semi-transparent foliage can't draw in the statics pass: it would write
    // depth before the player is drawn and the depth test culls the player's
    // sprite instead of blending the leaves over it (legacy draws back-to-front
    // per tile, so foliage always blends over an already-drawn mobile).
    private struct FoliageDraw
    {
        public Texture2D Texture;
        public Vector2 Position;
        public Rectangle UV;
        public Vector3 Color;
        public float Depth;
    }

    // Legacy GameSceneDrawingSorting._treeInfos: graphic ranges whose pieces
    // form one tree; marking any piece marks the whole diagonal run.
    private static readonly (ushort Start, ushort End)[] _treeUnions =
    {
        (0x0D45, 0x0D4C),
        (0x0D5C, 0x0D62),
        (0x0D73, 0x0D79),
        (0x0D87, 0x0D8B),
        (0x12BE, 0x12C7),
        (0x0D4D, 0x0D53),
        (0x0D63, 0x0D69),
        (0x0D7A, 0x0D7F),
        (0x0D8C, 0x0D90)
    };

    internal static long FoliageKey(ushort graphic, int x, int y, sbyte z)
        => ((long)graphic << 40) | ((long)(ushort)x << 24) | ((long)(ushort)y << 8) | (byte)z;

    // Legacy GameScene._rectanglePlayer — the player sprite's screen-space
    // bounds, derived from the current body animation frame (FrameInfo).
    private static Rectangle? PlayerScreenRect(
        Res<AssetsServer> assetsServer,
        ulong playerEnt,
        Query<Data<WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Facing, MobAnimation, MobileSteps, ServerFlags, Notoriety, AlphaFade>,
            Filter<Without<ContainedInto>, Optional<Facing>, Optional<MobAnimation>, Optional<MobileSteps>, Optional<ServerFlags>, Optional<Notoriety>>> queryBodyOnly)
    {
        if (!queryBodyOnly.TryGet(playerEnt, out var row))
            return null;

        (var pos, var graphic, _, _, var offset, var direction, var animation, _, _, _, _) = row;

        var dir = direction.IsValid() ? direction.Ref.Value : Direction.North;
        (dir, var mirror) = FixDirection(dir);

        byte animAction = 0;
        var animIndex = 0;
        if (animation.IsValid())
        {
            animAction = animation.Ref.Action;
            animIndex = animation.Ref.Index;
        }

        var frames = assetsServer.Value.Animations.GetAnimationFrames(
            graphic.Ref.Value, animAction, (byte)dir, out _, out _);
        if (frames.IsEmpty)
            return null;

        ref readonly var frame = ref frames[animIndex % frames.Length];
        if (frame.Texture == null)
            return null;

        // Legacy MobileView FrameInfo: xx/yy are the frame's offset from the
        // tile anchor; View.GetOnScreenRectangle adds +22 and the walk offset.
        var iso = pos.Ref.WorldToScreen();
        var xx = mirror ? -(frame.UV.Width - frame.Center.X) : -frame.Center.X;
        var yy = -(frame.UV.Height + frame.Center.Y + 3);

        return new Rectangle(
            (int)(iso.X + xx + 22 + offset.Ref.Value.X),
            (int)(iso.Y + yy + 22 + offset.Ref.Value.Y),
            frame.UV.Width,
            frame.UV.Height);
    }

    // Legacy CheckIfBehindATree: foliage below maxZ whose art bounds overlap
    // the player sprite (player standing north-west of it) goes transparent;
    // tree-union pieces are marked as one tree.
    private static void MarkFoliage(
        HashSet<long> marks,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Profile profile,
        int playerX,
        int playerY,
        int? maxZ,
        Rectangle? playerRect,
        Query<Data<WorldPosition, ScreenPosition, Graphic, Hue, Amount, NetworkSerial, Facing, AlphaFade, HouseVisionFade>, Filter<Without<IsTile>, Without<MobAnimation>, Without<ContainedInto>, Optional<Amount>, Optional<NetworkSerial>, Optional<Facing>, Optional<HouseVisionFade>>> queryStatics)
    {
        marks.Clear();

        if (!playerRect.HasValue || profile.TreeToStumps)
            return;

        var tileDataCache = fileManager.Value.TileData;
        var arts = assetsServer.Value.Arts;
        var pRect = playerRect.Value;

        foreach (var (_, worldPos, screenPos, graphic, _, _, _, _, _, _) in queryStatics)
        {
            var g = graphic.Ref.Value;
            ref readonly var tileData = ref tileDataCache.StaticData[g];

            if (!tileData.IsFoliage)
                continue;

            if (maxZ.HasValue && worldPos.Ref.Z >= maxZ)
                continue;

            if (profile.HideVegetation && !tileData.IsMultiMovable &&
                StaticFilters.IsVegetation(g, in tileData))
                continue;

            int x = worldPos.Ref.X;
            int y = worldPos.Ref.Y;

            var check = playerX <= x && playerY <= y
                || playerY <= y && playerX <= x + 1
                || playerX <= x && playerY <= y + 1;
            if (!check)
                continue;

            var rect = arts.GetRealArtBounds(g);
            var iso = screenPos.Ref.Value;
            rect.X = (int)iso.X - (rect.Width >> 1) + rect.X;
            rect.Y = (int)iso.Y - rect.Height + rect.Y;

            if (!rect.Intersects(pRect))
                continue;

            MarkFoliageUnion(marks, g, x, y, worldPos.Ref.Z);
        }
    }

    // Legacy IsFoliageUnion + ApplyFoliageTransparency, keyed on
    // (graphic, x, y, z) instead of walking map tile lists.
    private static void MarkFoliageUnion(HashSet<long> marks, ushort graphic, int x, int y, sbyte z)
    {
        for (var i = 0; i < _treeUnions.Length; i++)
        {
            (var start, var end) = _treeUnions[i];

            if (start <= graphic && graphic <= end)
            {
                while (graphic > start)
                {
                    graphic--;
                    x--;
                    y++;
                }

                for (graphic = start; graphic <= end; graphic++, x++, y--)
                {
                    marks.Add(FoliageKey(graphic, x, y, z));
                }

                return;
            }
        }

        marks.Add(FoliageKey(graphic, x, y, z));
    }

    // Legacy GameSceneDrawingSorting.ProcessAlpha: step the entity's alpha
    // toward its target (0 hidden, 178 translucent, 0xFF visible) once per
    // ALPHA_TIME tick. Returns false when the object is fully faded out and
    // can be skipped entirely.
    private static bool ProcessFade(ref byte alpha, bool hidden, bool translucent, ref readonly WorldFx fx)
    {
        if (hidden)
            return fx.AlphaChanged ? CalculateAlpha(ref alpha, 0, fx.ObjectsFading) : alpha != 0;

        if (translucent)
        {
            if (fx.AlphaChanged)
                CalculateAlpha(ref alpha, 178, fx.ObjectsFading);
            return true;
        }

        // Foliage never reaches this branch — RenderStatics routes visible
        // foliage through the foliage-transparency targets instead (legacy
        // ProcessAlpha excludes IsFoliage from fade-in).
        if (fx.AlphaChanged && alpha != 0xFF)
            CalculateAlpha(ref alpha, 0xFF, fx.ObjectsFading);
        return true;
    }

    internal static bool CalculateAlpha(ref byte alphaHue, int maxAlpha, bool useObjectsFading)
    {
        if (!useObjectsFading)
        {
            alphaHue = (byte)maxAlpha;
            return maxAlpha != 0;
        }

        var result = false;
        var alpha = (int)alphaHue;

        if (alpha > maxAlpha)
        {
            alpha -= 25;
            if (alpha < maxAlpha)
                alpha = maxAlpha;
            result = true;
        }
        else if (alpha < maxAlpha)
        {
            alpha += 25;
            if (alpha > maxAlpha)
                alpha = maxAlpha;
            result = true;
        }

        alphaHue = (byte)alpha;
        return result;
    }

    // Legacy Mobile.IsDead graphics (ghost bodies).
    internal static bool IsDeadBody(ushort graphic)
        => graphic == 0x0192 || graphic == 0x0193 ||
           (graphic >= 0x025F && graphic <= 0x0260) ||
           graphic == 0x02B6 || graphic == 0x02B7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Dist(int x0, int y0, int x1, int y1)
        => Math.Max(Math.Abs(x0 - x1), Math.Abs(y0 - y1));

    // Legacy Static._canBeTransparent + TransparentTest — which statics the
    // circle of transparency may see through, given playerZ + 5.
    internal static bool TransparentTest(int z5, int objZ, ref readonly StaticTiles data)
    {
        if (objZ <= z5 - data.Height)
            return false;

        if (z5 < objZ && !CanBeTransparent(in data))
            return false;

        return true;
    }

    internal static bool CanBeTransparent(ref readonly StaticTiles data)
        => data.Height > 5 || data.Height == 0 ||
           data.IsRoof || (data.IsSurface && data.IsBackground) || data.IsWall ||
           (data.Height == 5 && data.IsSurface && !data.IsBackground);

    // Legacy StaticView depth-sort weighting: background sinks, raised / wall /
    // multi tiles lift, a normal-multi piece sinks. qNormalMultis membership is
    // resolved by the caller (a query) and passed in. Pure so the weighting is
    // unit-tested.
    internal static int StaticPriorityZ(int z, ref readonly StaticTiles data, bool isNormalMulti)
    {
        if (data.IsBackground) z -= 1;
        if (data.Height != 0) z += 1;
        if (data.IsWall) z += 2;
        if (data.IsMultiMovable) z += 1;
        if (isNormalMulti) z -= 1;
        return z;
    }

    // Gradient circle-of-transparency (legacy GetGradientCotAlpha): inside the
    // radius alpha fades by distance-cubed from the player; returns false when
    // the sprite is fully clear (cull) and true otherwise (outside the radius
    // leaves alpha at 1). `iso` is the pre-center-subtract screen position.
    internal static bool TryGradientCotAlpha(Vector2 iso, Vector2 cotCenter, float cotRadiusSq, out float alpha)
    {
        alpha = 1f;
        var dx = iso.X - cotCenter.X;
        var dy = (iso.Y - 44) - cotCenter.Y;
        var distSq = dx * dx + dy * dy;
        if (distSq < cotRadiusSq)
        {
            var ratio = MathF.Sqrt(distSq / cotRadiusSq);
            var alphaByte = (byte)(ratio * ratio * ratio * 255f);
            if (alphaByte == 0)
                return false;
            alpha = alphaByte / 255f;
        }
        return true;
    }

    // Legacy StaticView/ItemView hue precedence: highlight > out-of-range gray >
    // dead-world gray > selected-interactable hover, else the base hue. The
    // caller pre-computes each condition (they read Profile / WorldFx /
    // selection); this locks the order + partial-hue suppression.
    internal static ushort ResolveStaticHue(
        bool highlight, bool outOfRange, bool grayWorld, bool selectedInteractable,
        ushort baseHue, bool basePartialHue, out bool partialHue)
    {
        partialHue = basePartialHue;
        if (highlight)
        {
            partialHue = false;
            return Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
        }
        if (outOfRange)
        {
            partialHue = false;
            return Constants.OUT_RANGE_COLOR;
        }
        if (grayWorld)
        {
            partialHue = false;
            return Constants.DEAD_RANGE_COLOR;
        }
        if (selectedInteractable)
            return 0x0035;
        return baseHue;
    }

    // Legacy FieldsType==2 field replacement: an animated field static draws one
    // replacement graphic tinted per field kind. Pure (StaticFilters); the
    // FieldsType / IsAnimated gate stays in the caller.
    internal static bool TryFieldReplace(ushort graphic, out ushort drawGraphic, out ushort hue)
    {
        drawGraphic = Constants.FIELD_REPLACE_GRAPHIC;
        if (StaticFilters.IsFireField(graphic)) { hue = 0x0020; return true; }
        if (StaticFilters.IsParalyzeField(graphic)) { hue = 0x0058; return true; }
        if (StaticFilters.IsEnergyField(graphic)) { hue = 0x0070; return true; }
        if (StaticFilters.IsPoisonField(graphic)) { hue = 0x0044; return true; }
        if (StaticFilters.IsWallOfStone(graphic)) { hue = 0x038A; return true; }
        drawGraphic = 0; hue = 0;
        return false;
    }


    private static void RenderTiles(
        Res<SelectedEntity> selectedEntity,
        Res<GameContext> gameCtx,
        Res<UltimaBatcher2D> batch,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Res<Camera> camera,
        Profile profile,
        ref readonly WorldFx fx,
        bool calculateZ,
        ref MaxZInfo workingZInfo,
        int playerX,
        int playerY,
        int playerZ16,
        MaxZInfo backupZInfo,
        int? maxZ,
        Vector2 center,
        Vector2 mousePos,
        Rectangle cameraBounds,
        Query<Data<WorldPosition, ScreenPosition, Graphic, TileStretched, AlphaFade>, Filter<With<IsTile>, Optional<TileStretched>>> queryTiles)
    {
        // Cache frequently accessed resources
        var tileDataCache = fileManager.Value.TileData;
        var texmaps = assetsServer.Value.Texmaps;
        var arts = assetsServer.Value.Arts;

        // Process all tiles in one pass
        foreach (var (entity, worldPos, screenPos, graphic, stretched, fade) in queryTiles)
        {
            // Early filtering — fully faded and no tick this frame: skip cheap.
            var hide = backupZInfo.MaxZGround.HasValue && worldPos.Ref.Z > backupZInfo.MaxZGround;
            if (!calculateZ && hide && !fx.AlphaChanged && fade.Ref.Value == 0)
                continue;

            // Calculate position only once
            var iso = screenPos.Ref.Value;
            Vector2.Subtract(ref iso, ref center, out var position);

            // Quick bounds checking for early exit
            if (position.X < cameraBounds.X || position.X > cameraBounds.Width ||
                position.Y > cameraBounds.Height)
                continue;

            if (!CanBeDrawn(gameCtx.Value.ClientVersion, tileDataCache, graphic.Ref.Value))
                continue;

            // Z-calculations (only if needed)
            if (calculateZ && worldPos.Ref.X == playerX && worldPos.Ref.Y == playerY)
            {
                if ((stretched.IsValid() ? stretched.Ref.AvgZ : worldPos.Ref.Z) > playerZ16)
                {
                    workingZInfo.MaxZGround = playerZ16;
                }
            }

            if (!ProcessFade(ref fade.Ref.Value, hide, false, in fx))
                continue;

            var alpha = fade.Ref.Value / 255f;

            // Legacy LandView hue chain: highlight > out-of-range > dead gray.
            ushort hueOverride = ResolveLandHue(
                highlight: profile.HighlightGameObjects && entity.Ref == selectedEntity.Value.Entity,
                outOfRange: profile.NoColorObjectsOutOfRange && Dist(worldPos.Ref.X, worldPos.Ref.Y, fx.PlayerX, fx.PlayerY) > fx.ViewRange,
                grayWorld: fx.GrayWorld);

            if (stretched.IsValid())
            {
                // Handle stretched land
                position.Y += worldPos.Ref.Z << 2;

                if (position.Y - (stretched.Ref.MinZ << 2) < cameraBounds.Y)
                    continue;

                ref readonly var textmapInfo = ref texmaps.GetTexmap(tileDataCache.LandData[graphic.Ref.Value].TexID);
                if (textmapInfo.Texture == null)
                    continue;

                var depthZ = Isometric.GetDepthZ(worldPos.Ref.X, worldPos.Ref.Y, stretched.Ref.AvgZ - 2);
                var color = hueOverride != 0
                    ? new Vector3(hueOverride - 1, ShaderHueTranslator.SHADER_LAND_HUED, alpha)
                    : new Vector3(0, ShaderHueTranslator.SHADER_LAND, alpha);

                selectedEntity.Value.IsPointInStretchedLand(
                    entity.Ref,
                    depthZ,
                    in stretched.Ref.Offset,
                    mousePos,
                    position
                );

                batch.Value.DrawStretchedLand(
                    textmapInfo.Texture,
                    position,
                    textmapInfo.UV,
                    ref stretched.Ref.Offset,
                    ref stretched.Ref.NormalTop,
                    ref stretched.Ref.NormalRight,
                    ref stretched.Ref.NormalLeft,
                    ref stretched.Ref.NormalBottom,
                    color,
                    depthZ
                );
            }
            else
            {
                // Handle regular land
                if (position.Y < cameraBounds.Y)
                    continue;

                ref readonly var artInfo = ref arts.GetLand(graphic.Ref.Value);
                if (artInfo.Texture == null)
                    continue;

                var depthZ = Isometric.GetDepthZ(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z - 2);
                var color = hueOverride != 0
                    ? new Vector3(hueOverride - 1, ShaderHueTranslator.SHADER_HUED, alpha)
                    : new Vector3(0, 0, alpha);

                selectedEntity.Value.IsPointInLand(entity.Ref, depthZ, mousePos, position);

                var scale = Vector2.One;
                if (profile.AnimatedWaterEffect && tileDataCache.LandData[graphic.Ref.Value].IsWet)
                {
                    // Base sprite + the wave-scaled overlay (legacy LandView).
                    batch.Value.Draw(
                        artInfo.Texture,
                        position,
                        artInfo.UV,
                        color,
                        rotation: 0f,
                        origin: Vector2.Zero,
                        scale,
                        effects: SpriteEffects.None,
                        depthZ
                    );

                    scale = fx.WaterScale;
                }

                batch.Value.Draw(
                    artInfo.Texture,
                    position,
                    artInfo.UV,
                    color,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale,
                    effects: SpriteEffects.None,
                    depthZ
                );
            }
        }
    }

    private static void RenderStatics(
        Res<SelectedEntity> selectedEntity,
        Res<GameContext> gameCtx,
        Res<UltimaBatcher2D> batch,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Res<Camera> camera,
        Profile profile,
        ref readonly WorldFx fx,
        bool calculateZ,
        ref MaxZInfo workingZInfo,
        int playerX,
        int playerY,
        int playerZ14,
        MaxZInfo backupZInfo,
        int? maxZ,
        Vector2 center,
        Vector2 mousePos,
        Rectangle cameraBounds,
        Query<Empty, With<NormalMulti>> qNormalMultis,
        Query<Data<WorldPosition, ScreenPosition, Graphic, Hue, Amount, NetworkSerial, Facing, AlphaFade, HouseVisionFade>, Filter<Without<IsTile>, Without<MobAnimation>, Without<ContainedInto>, Optional<Amount>, Optional<NetworkSerial>, Optional<Facing>, Optional<HouseVisionFade>>> queryStatics,
        Dictionary<long, int> lightOccluders,
        List<PendingLight> pendingLights,
        AnimatedStatics animStatics)
    {
        // Cache frequently accessed resources
        var tileDataCache = fileManager.Value.TileData;
        var arts = assetsServer.Value.Arts;
        var useColoredLights = profile.UseColoredLights;
        var lightCeiling = maxZ ?? 127;

        // Process all statics in one pass with optimized property access
        foreach (var (entity, worldPos, screenPos, graphic, hue, amount, serial, facing, fade, vision) in queryStatics)
        {
            ref readonly var tileData = ref tileDataCache.StaticData[graphic.Ref.Value];
            int amountVal = amount.IsValid() ? amount.Ref.Value : 1;
            // Coins draw a pile sprite (base+1 / base+2) by amount.
            ushort drawGraphic = ItemGraphics.Displayed(graphic.Ref.Value, amountVal);
            ushort hueValue = hue.Ref.Value;

            // Early filtering
            if (tileData.IsInternal)
                continue;

            // World filters (legacy GameSceneDrawingSorting + StaticView/ItemView).
            var isTree = StaticFilters.IsTree(graphic.Ref.Value, in tileData);
            if (profile.TreeToStumps && tileData.IsFoliage && !tileData.IsMultiMovable)
                continue;

            if (profile.HideVegetation && !tileData.IsMultiMovable &&
                StaticFilters.IsVegetation(graphic.Ref.Value, in tileData))
                continue;

            if (isTree && profile.TreeToStumps)
                drawGraphic = Constants.TREE_REPLACE_GRAPHIC;

            if (profile.FieldsType == 2 && tileData.IsAnimated &&
                TryFieldReplace(graphic.Ref.Value, out var fieldGraphic, out var fieldHue))
            {
                drawGraphic = fieldGraphic;
                hueValue = fieldHue;
            }
            else if (tileData.IsAnimated)
            {
                // animdata.mul frame delta added to the displayed graphic.
                drawGraphic = (ushort)(drawGraphic + animStatics.Offset(graphic.Ref.Value));
            }

            var hide = tileData.IsRoof && (!backupZInfo.DrawRoof || !profile.DrawRoofs);
            hide |= maxZ.HasValue && worldPos.Ref.Z >= maxZ;
            // Fully faded and no tick this frame: skip before art/bounds work.
            if (!calculateZ && hide && !fx.AlphaChanged && fade.Ref.Value == 0)
                continue;

            // Calculate position only once
            var iso = screenPos.Ref.Value;
            Vector2.Subtract(ref iso, ref center, out var position);

            // Quick bounds checking for early exit
            if (position.X < cameraBounds.X || position.X > cameraBounds.Width ||
                position.Y < cameraBounds.Y || position.Y > cameraBounds.Height)
                continue;

            if (!CanBeDrawn(gameCtx.Value.ClientVersion, tileDataCache, graphic.Ref.Value))
                continue;

            ref readonly var artInfo = ref arts.GetArt(drawGraphic);
            if (artInfo.Texture == null)
                continue;

            var isNormalMulti = qNormalMultis.Contains(entity.Ref);

            // Dynamic-light occlusion (legacy GameScene.AddLight overhang test):
            // a non-transparent static/multi below the roof ceiling blocks the
            // light of a source on its NW neighbour. Record it under that
            // neighbour's tile, keeping the tallest blocker. Dynamic items
            // (NetworkSerial, not a multi) never occlude — matches legacy, and
            // is what stops interior floor-lights lighting through house walls
            // (walls are multis).
            if (!tileData.IsTransparent && worldPos.Ref.Z < lightCeiling
                && (isNormalMulti || !serial.IsValid()))
            {
                var key = TileKey(worldPos.Ref.X - 1, worldPos.Ref.Y - 1);
                if (!lightOccluders.TryGetValue(key, out var oz) || worldPos.Ref.Z > oz)
                    lightOccluders[key] = worldPos.Ref.Z;
            }

            // Collect light sources that survived camera-bounds (above) and the
            // roof/maxZ ceiling (!hide); resolved against the occluder map after
            // the pass (mirrors legacy AddLight-from-Draw). position is still
            // screenPos-center here (the sprite top-left offset happens below),
            // matching the old +22 centring. World items carry the light id on
            // Facing; map statics use the tile-data layer.
            if (!hide &&
                (tileData.IsLight || (graphic.Ref.Value >= 0x3E02 && graphic.Ref.Value <= 0x3E0B)
                    || (graphic.Ref.Value >= 0x3914 && graphic.Ref.Value <= 0x3929)))
            {
                pendingLights.Add(new PendingLight
                {
                    Graphic = graphic.Ref.Value,
                    Id = serial.IsValid() && facing.IsValid() ? (byte)facing.Ref.Value : tileData.Layer,
                    DrawX = position.X + 22f,
                    DrawY = position.Y + 22f,
                    TileX = worldPos.Ref.X,
                    TileY = worldPos.Ref.Y,
                    Z = worldPos.Ref.Z,
                });
            }

            // Z-calculations (only if needed)
            if (calculateZ)
            {
                var tileDataFlags = tileData.Flags;

                if (worldPos.Ref.Z > playerZ14)
                {
                    if (((ulong)tileDataFlags & 0x204) == 0 && tileDataFlags.HasFlag(TileFlag.Roof))
                    {
                        if (worldPos.Ref.X == playerX && worldPos.Ref.Y == playerY)
                            workingZInfo.IsSameTile = true;
                        else if (worldPos.Ref.X == playerX + 1 && worldPos.Ref.Y == playerY + 1)
                            workingZInfo.IsTileAhead = true;
                    }

                    var max = workingZInfo.MaxZRoof ?? 127;

                    if (max > worldPos.Ref.Z)
                    {
                        if (((ulong)tileDataFlags & 0x204) == 0 && tileDataFlags.HasFlag(TileFlag.Roof))
                        {
                            workingZInfo.MaxZRoof = worldPos.Ref.Z;
                            workingZInfo.DrawRoof = false;
                        }
                    }
                }

                if (worldPos.Ref.X == playerX && worldPos.Ref.Y == playerY)
                {
                    if (worldPos.Ref.Z > playerZ14)
                    {
                        var max = workingZInfo.MaxZ ?? 127;

                        if (max > worldPos.Ref.Z)
                        {
                            if (((ulong)tileDataFlags & 0x20004) == 0 && (!tileDataFlags.HasFlag(TileFlag.Roof) || tileDataFlags.HasFlag(TileFlag.Surface)))
                            {
                                workingZInfo.IsUnderStatic = true;
                                workingZInfo.MaxZ = worldPos.Ref.Z;
                                workingZInfo.DrawRoof = false;
                            }
                        }
                    }
                }
            }

            // Fading-out objects (above maxZ / hidden roofs) keep drawing until
            // the alpha reaches 0; selection is suppressed meanwhile (legacy
            // meshed-static path).
            var fadingOut = hide;
            var foliageTransparent = false;
            if (!hide && tileData.IsFoliage)
            {
                // Legacy GameScene foliage loop: the foliage system owns the
                // alpha of visible foliage (FOLIAGE_ALPHA when hiding the
                // player, fade back to opaque otherwise) — ProcessFade's
                // fade-in branch never runs for it (legacy ProcessAlpha
                // excludes IsFoliage).
                foliageTransparent = fx.FoliageMarks.Contains(
                    FoliageKey(graphic.Ref.Value, worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z));
                if (fx.AlphaChanged)
                    CalculateAlpha(ref fade.Ref.Value,
                        foliageTransparent ? Constants.FOLIAGE_ALPHA : 0xFF, fx.ObjectsFading);
            }
            else
            {
                // House-design floor vision: let the fade system target the
                // right value (translucent 178 / hidden 0) instead of ApplyFloor
                // Vision fighting it via AlphaFade, which flickered.
                bool vHide = hide;
                bool vTrans = tileData.IsTranslucent;
                if (vision.IsValid())
                {
                    var mode = vision.Ref.Mode;
                    if (mode == 2) vHide = true;
                    else if (mode == 1) vTrans = true;
                }
                if (!ProcessFade(ref fade.Ref.Value, vHide, vTrans, in fx))
                    continue;
            }

            // Position calculation
            position.X -= (short)((artInfo.UV.Width >> 1) - 22);
            position.Y -= (short)(artInfo.UV.Height - 44);

            // Priority calculation
            var priorityZ = StaticPriorityZ(worldPos.Ref.Z, in tileData, isNormalMulti);

            var depthZ = Isometric.GetDepthZ(worldPos.Ref.X, worldPos.Ref.Y, priorityZ);

            // Circle of transparency. Full mode flags the shader per sprite
            // (circletrans); gradient mode fades alpha by distance³ from the
            // player (legacy GetGradientCotAlpha) and culls when fully clear.
            var circleTrans = false;
            var alpha = 1f;
            if ((fx.CotFull || fx.CotGradient) && TransparentTest(fx.PlayerZ5, worldPos.Ref.Z, in tileData))
            {
                if (fx.CotFull)
                    circleTrans = !isTree && !tileData.IsFoliage;
                else if (!TryGradientCotAlpha(iso, fx.CotCenter, fx.CotRadiusSq, out alpha))
                    continue;
            }

            // Legacy StaticView/ItemView hue chain: highlight > out-of-range >
            // dead gray; selected interactable items keep the 0x0035 hover hue.
            var isSelected = entity.Ref == selectedEntity.Value.Entity;
            hueValue = ResolveStaticHue(
                highlight: profile.HighlightGameObjects && isSelected,
                outOfRange: profile.NoColorObjectsOutOfRange && Dist(worldPos.Ref.X, worldPos.Ref.Y, fx.PlayerX, fx.PlayerY) > fx.ViewRange,
                grayWorld: fx.GrayWorld,
                selectedInteractable: isSelected && serial.IsValid() && !isNormalMulti,
                baseHue: hueValue,
                basePartialHue: tileData.IsPartialHue,
                out var partialHue);

            alpha *= fade.Ref.Value / 255f;

            var color = Renderer.ShaderHueTranslator.GetHueVector(hueValue, partialHue, alpha, circletrans: circleTrans);

            // Selection checking — transparent foliage is click-through
            // (legacy Static.CheckMouseSelection FoliageIndex match).
            var p = mousePos - position;
            if (!fadingOut && !foliageTransparent &&
                assetsServer.Value.Arts.PixelCheck(drawGraphic, (int)p.X, (int)p.Y))
                selectedEntity.Value.Set(entity.Ref, depthZ);

            // Trees, foliage and rocks cast a flat shadow (legacy StaticView →
            // DrawStaticAnimated shadow pass, 0.25 below the sprite depth).
            if (profile.ShadowsEnabled && profile.ShadowsStatics &&
                (isTree || tileData.IsFoliage || StaticFilters.IsRock(graphic.Ref.Value)))
            {
                batch.Value.DrawShadow(artInfo.Texture, position, artInfo.UV, false, depthZ - 0.25f);
            }

            // Stacked pile: a back sprite -5/-5 behind the main one (legacy
            // ItemView.DrawStaticAnimated(graphic, posX-5, posY-5) then posX,posY).
            // Coins are excluded — their amount shows through the pile graphic.
            if (ItemGraphics.DrawStacked(graphic.Ref.Value, amountVal, tileData.IsStackable))
            {
                batch.Value.Draw(
                    artInfo.Texture,
                    position - new Vector2(5, 5),
                    artInfo.UV,
                    color,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 1f,
                    effects: SpriteEffects.None,
                    depthZ
                );
            }

            // Defer semi-transparent foliage to the post-mobiles pass — see
            // FoliageDraw. Opaque foliage stays in this pass (no blend issue).
            if (tileData.IsFoliage && fade.Ref.Value < 0xFF)
            {
                fx.FoliageDraws.Add(new FoliageDraw
                {
                    Texture = artInfo.Texture,
                    Position = position,
                    UV = artInfo.UV,
                    Color = color,
                    Depth = depthZ
                });
                continue;
            }

            var scale = Vector2.One;
            if (profile.AnimatedWaterEffect && tileData.IsWet)
            {
                // Base sprite + the wave-scaled overlay (legacy DrawStaticAnimated).
                batch.Value.Draw(
                    artInfo.Texture,
                    position,
                    artInfo.UV,
                    color,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale,
                    effects: SpriteEffects.None,
                    depthZ
                );

                scale = fx.WaterScale;
            }

            // Draw the static
            batch.Value.Draw(
                artInfo.Texture,
                position,
                artInfo.UV,
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                scale,
                effects: SpriteEffects.None,
                depthZ
            );
        }

        // Resolve collected lights against the occluder map (legacy AddLight
        // overhang test): drop a light if its SE neighbour holds a blocker at
        // lightZ+5 or above. Done after the pass so the map is complete.
        foreach (ref readonly var pl in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(pendingLights))
        {
            if (lightOccluders.TryGetValue(TileKey(pl.TileX, pl.TileY), out var oz) && oz >= pl.Z + 5)
                continue;
            LightingPlugin.Push(gameCtx.Value.Lights, pl.Graphic, pl.Id, pl.DrawX, pl.DrawY, useColoredLights);
        }
    }

    private static long TileKey(int x, int y) => ((long)(uint)x << 32) | (uint)y;

    // Effect blend modes (legacy GameEffect's Lazy<BlendState> set). Immutable
    // GPU state descriptors, built once.
    private static readonly BlendState _multiplyBlend = new()
    { ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.SourceColor };
    private static readonly BlendState _screenBlend = new()
    { ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One };
    private static readonly BlendState _screenLessBlend = new()
    { ColorSourceBlend = Blend.DestinationColor, ColorDestinationBlend = Blend.InverseSourceAlpha };
    private static readonly BlendState _normalHalfBlend = new()
    { ColorSourceBlend = Blend.DestinationColor, ColorDestinationBlend = Blend.SourceColor };
    private static readonly BlendState _shadowBlueBlend = new()
    { ColorSourceBlend = Blend.SourceColor, ColorDestinationBlend = Blend.InverseSourceColor, ColorBlendFunction = BlendFunction.ReverseSubtract };
    // Premultiplied-alpha additive. XNA's BlendState.Additive is (SourceAlpha,
    // One) which double-applies alpha on the (premultiplied) UO atlas textures
    // and renders the sprite invisible; (One, One) is the correct additive.
    private static readonly BlendState _additiveBlend = new()
    { ColorSourceBlend = Blend.One, ColorDestinationBlend = Blend.One };

    private static BlendState EffectBlend(GraphicEffectBlendMode mode) => mode switch
    {
        GraphicEffectBlendMode.Multiply => _multiplyBlend,
        GraphicEffectBlendMode.Screen or GraphicEffectBlendMode.ScreenMore => _screenBlend,
        GraphicEffectBlendMode.ScreenLess => _screenLessBlend,
        GraphicEffectBlendMode.NormalHalfTransparent => _normalHalfBlend,
        GraphicEffectBlendMode.ShadowBlue => _shadowBlueBlend,
        _ => null,
    };

    // Port of GameEffectView / LightningEffectView / DragEffect.Draw. Effects
    // are drawn after mobiles/equipment, inside the world batch (depth on), so
    // they sit over the tile they occupy. Source/target tiles and Offset are
    // maintained by EffectsPlugin's update pass.
    private static void RenderEffects(
        Res<UltimaBatcher2D> batch,
        Res<AssetsServer> assets,
        Res<UOFileManager> fileManager,
        Profile profile,
        EffectsState state,
        Vector2 center,
        ref readonly WorldFx fx,
        int playerX,
        int playerY,
        int playerZ)
    {
        if (state.Effects.Count == 0)
            return;

        var arts = assets.Value.Arts;
        var gumps = assets.Value.Gumps;
        var tileDataCache = fileManager.Value.TileData;

        foreach (ref readonly var e in System.Runtime.InteropServices.CollectionsMarshal.AsSpan(state.Effects))
        {
            if (e.Destroyed)
                continue;

            ushort hue = e.Hue;
            if (fx.GrayWorld)
                hue = Constants.DEAD_RANGE_COLOR;

            var iso = Isometric.IsoToScreen(e.SrcX, e.SrcY, e.SrcZ);
            Vector2.Subtract(ref iso, ref center, out var pos);
            // Bias the depth on top of statics sharing the tile (legacy effects
            // sort just above their source).
            var depthZ = Isometric.GetDepthZ(e.SrcX, e.SrcY, e.SrcZ + 5) + 0.5f;

            if (e.Kind == EffectKind.Lightning)
            {
                ref readonly var gumpInfo = ref gumps.GetGump(e.AnimationGraphic);
                if (gumpInfo.Texture == null)
                    continue;

                pos.X -= gumpInfo.UV.Width >> 1;
                pos.Y -= gumpInfo.UV.Height;

                var hv = ShaderHueTranslator.GetHueVector(hue, false, 1);
                hv.Y = hv.X > 1.0f ? ShaderHueTranslator.SHADER_LIGHTS : ShaderHueTranslator.SHADER_NONE;

                batch.Value.SetBlendState(_additiveBlend);
                batch.Value.Draw(gumpInfo.Texture, pos, gumpInfo.UV, hv, 0f, Vector2.Zero, 1f, SpriteEffects.None, depthZ);
                batch.Value.SetBlendState(null);
                continue;
            }

            if (e.AnimationGraphic == 0xFFFF)
                continue;

            ref readonly var artInfo = ref arts.GetArt(e.AnimationGraphic);
            if (artInfo.Texture == null)
                continue;

            ref readonly var data = ref tileDataCache.StaticData[e.Graphic];
            var hueVec = ShaderHueTranslator.GetHueVector(
                hue, data.IsPartialHue, data.IsTranslucent ? 0.5f : 1f, effect: true);

            if (e.Kind == EffectKind.Drag)
            {
                // Legacy DragEffect.Draw offset convention.
                var dx = pos.X - (e.Offset.X + 22) - ((artInfo.UV.Width >> 1) - 22);
                var dy = pos.Y - (-e.Offset.Y + 22) - (artInfo.UV.Height - 44);
                batch.Value.Draw(artInfo.Texture, new Vector2(dx, dy), artInfo.UV, hueVec, 0f, Vector2.Zero, 1f, SpriteEffects.None, depthZ);
                continue;
            }

            // Fixed / Moving — GameEffectView (rotated static, blended).
            pos.X += e.Offset.X;
            pos.Y += e.Offset.Y;
            var rect = new Rectangle(
                (int)(pos.X - ((artInfo.UV.Width >> 1) - 22)),
                (int)(pos.Y - (artInfo.UV.Height - 44)),
                artInfo.UV.Width,
                artInfo.UV.Height);

            var blend = EffectBlend(e.Blend);
            if (blend != null)
                batch.Value.SetBlendState(blend);

            batch.Value.Draw(artInfo.Texture, rect, artInfo.UV, hueVec, e.AngleToTarget, Vector2.Zero, SpriteEffects.None, depthZ);

            if (blend != null)
                batch.Value.SetBlendState(null);
        }
    }

    // Feet-aura ground decal (legacy MobileView aura). Depth buffer OFF so the
    // circle paints over the ground without being clipped by the tile in front
    // and without writing depth (which would occlude mobiles behind it). Runs
    // after statics, before bodies, so the bodies paint over it = under the feet.
    private static void RenderFeetAuras(
        Res<UltimaBatcher2D> batch,
        Profile profile,
        Vector2 center,
        PartyState party,
        Query<Data<WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Facing, MobAnimation, MobileSteps, ServerFlags, Notoriety, AlphaFade>,
            Filter<Without<ContainedInto>, Optional<Facing>, Optional<MobAnimation>, Optional<MobileSteps>, Optional<ServerFlags>, Optional<Notoriety>>> queryBodyOnly)
    {
        batch.Value.SetStencil(DepthStencilState.None);
        foreach (var (_, pos, _, _, serial, offset, _, _, _, _, notoriety, _) in queryBodyOnly)
        {
            if (!SerialHelper.IsMobile(serial.Ref.Value))
                continue;

            var feet = pos.Ref.WorldToScreen() - center;
            feet.X += 22f;
            feet.Y += 22f;
            feet += offset.Ref.Value;

            var notoFlag = notoriety.IsValid() ? notoriety.Ref.Value : NotorietyFlag.Unknown;
            ushort auraHue = AuraOverlay.FeetHue(profile, notoFlag, party.Contains(serial.Ref.Value));
            AuraOverlay.DrawCircle(batch.Value, (int)feet.X, (int)feet.Y, auraHue);
        }
        batch.Value.SetStencil(DepthStencilState.Default);
    }

    private static void RenderBodies(
        Res<SelectedEntity> selectedEntity,
        Res<UltimaBatcher2D> batch,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Profile profile,
        ref readonly WorldFx fx,
        int? maxZ,
        Vector2 center,
        Vector2 mousePos,
        Query<Data<WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Facing, MobAnimation, MobileSteps, ServerFlags, Notoriety, AlphaFade>,
            Filter<Without<ContainedInto>, Optional<Facing>, Optional<MobAnimation>, Optional<MobileSteps>, Optional<ServerFlags>, Optional<Notoriety>>> queryBodyOnly)
    {
        // Cache animation service
        var animations = assetsServer.Value.Animations;

        foreach (var (entity, pos, graphic, hue, serial, offset, direction, animation, steps, sFlags, notoriety, fade) in queryBodyOnly)
        {
            // Early filtering — fade out above maxZ instead of hard-culling.
            // The body owns the fade step; RenderEquipment reads the value.
            var aboveMaxZ = maxZ.HasValue && pos.Ref.Z >= maxZ;
            if (!ProcessFade(ref fade.Ref.Value, aboveMaxZ, false, in fx))
                continue;

            var fadeAlpha = fade.Ref.Value / 255f;

            var priorityZ = pos.Ref.Z;
            var iso = pos.Ref.WorldToScreen();
            Vector2.Subtract(ref iso, ref center, out var position);

            // Direction handling
            priorityZ += 2;
            var dir = direction.IsValid() ? direction.Ref.Value : Direction.North;
            (dir, var mirror) = FixDirection(dir);

            // Animation data
            byte animAction = 0;
            var animIndex = 0;
            if (animation.IsValid())
            {
                animAction = animation.Ref.Action;
                animIndex = animation.Ref.Index;
                if (direction.IsValid())
                    animation.Ref.Direction = (direction.Ref.Value & (~Direction.Running | Direction.Mask));
            }

            // Get animation frames
            var frames = animations.GetAnimationFrames(
                graphic.Ref.Value,
                animAction,
                (byte)dir,
                out var baseHue,
                out var isUop
            );

            var uoHue = hue.Ref.Value == 0 ? baseHue : hue.Ref.Value;

            var mobFlags = sFlags.IsValid() ? sFlags.Ref.Value : Flags.None;
            var notoFlag = notoriety.IsValid() ? notoriety.Ref.Value : NotorietyFlag.Unknown;
            var isDead = IsDeadBody(graphic.Ref.Value);
            var isHidden = (mobFlags & Flags.Hidden) != 0;
            var highlighted = profile.HighlightGameObjects && entity.Ref == selectedEntity.Value.Entity;

            // Legacy MobileView hue chain (out-of-range > dead gray > hidden >
            // non-human ghost > poison/paralyze/invul status highlights).
            var overrideHue = MobOverrideHue(profile, in fx, highlighted, pos.Ref.X, pos.Ref.Y,
                graphic.Ref.Value, mobFlags, notoFlag, isDead, isHidden);
            if (overrideHue != 0)
                uoHue = overrideHue;

            // Get current frame
            ref readonly var frame = ref frames.IsEmpty ?
                ref SpriteInfo.Empty
                :
                ref frames[animIndex % frames.Length];

            var texture = frame.Texture;
            var uv = frame.UV;

            // Skip if no texture
            if (texture == null)
                continue;

            // Calculate position
            position.X += 22;
            position.Y += 22;
            if (mirror)
                position.X -= frame.UV.Width - frame.Center.X;
            else
                position.X -= frame.Center.X;
            position.Y -= frame.UV.Height + frame.Center.Y;

            var depthZ = BodyDepthZ(pos.Ref.X, pos.Ref.Y, priorityZ, offset.Ref.Value);
            var color = ShaderHueTranslator.GetHueVector(FixHue(uoHue), false, fadeAlpha);
            position += offset.Ref.Value;

            if (highlighted)
            {
                color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                color.Y = ShaderHueTranslator.SHADER_HUED;
                color.Z = fadeAlpha;
            }

            // Selection checking
            if (!aboveMaxZ && animations.PixelCheck(
                graphic.Ref.Value,
                animAction,
                (byte)dir,
                isUop,
                animIndex,
                mirror ? (int)(position.X + uv.Width - mousePos.X) : (int)(mousePos.X - position.X),
                (int)(mousePos.Y - position.Y)
            ))
            {
                selectedEntity.Value.Set(entity.Ref, depthZ);
            }

            // Legacy MobileView: living, visible mobiles cast a shadow.
            if (profile.ShadowsEnabled && !isDead && !isHidden)
            {
                batch.Value.DrawShadow(texture, position, uv, mirror, depthZ);
            }

            // Draw the body
            batch.Value.Draw(
                texture,
                position,
                uv,
                color,
                0f,
                Vector2.Zero,
                1f,
                mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                depthZ
            );
        }
    }

    // Legacy MobileView.Draw hue-override chain, minus the targeting/attack
    // notoriety cases (no target manager hooked into rendering yet).
    private static ushort MobOverrideHue(
        Profile profile,
        ref readonly WorldFx fx,
        bool highlighted,
        int x,
        int y,
        ushort bodyGraphic,
        Flags mobFlags,
        NotorietyFlag notoFlag,
        bool isDead,
        bool isHidden)
    {
        if (highlighted)
            return 0;

        if (profile.NoColorObjectsOutOfRange && Dist(x, y, fx.PlayerX, fx.PlayerY) > fx.ViewRange)
            return Constants.OUT_RANGE_COLOR;

        if (fx.GrayWorld)
            return Constants.DEAD_RANGE_COLOR;

        if (isHidden)
            return 0x038E;

        if (isDead)
            return Races.IsHuman(bodyGraphic) ? (ushort)0 : (ushort)0x0386;

        ushort overrideHue = 0;

        // NOTE: pre-7.0 poison flag; SA+ buff-based poison state isn't tracked.
        if (profile.HighlightMobilesByPoisoned && (mobFlags & Flags.Poisoned) != 0)
            overrideHue = profile.PoisonHue;

        if (profile.HighlightMobilesByParalize && (mobFlags & Flags.Frozen) != 0 && notoFlag != NotorietyFlag.Invulnerable)
            overrideHue = profile.ParalyzedHue;

        if (profile.HighlightMobilesByInvul && notoFlag != NotorietyFlag.Invulnerable && (mobFlags & Flags.YellowBar) != 0)
            overrideHue = profile.InvulnerableHue;

        return overrideHue;
    }

    // A mid-step mobile's screen offset pushes it toward the next tile; its
    // depth must sort against that tile, not its origin. Shared by RenderBodies
    // + RenderEquipment so a body and its worn items keep one depth. Pure
    // (Isometric.GetDepthZ) so the per-quadrant nudge is unit-tested.
    internal static float BodyDepthZ(int x, int y, int priorityZ, Vector2 offset)
    {
        if (offset.X > 0 && offset.Y > 0) return Isometric.GetDepthZ(x + 1, y, priorityZ);
        if (offset.X == 0 && offset.Y > 0) return Isometric.GetDepthZ(x + 1, y + 1, priorityZ);
        if (offset.X < 0 && offset.Y > 0) return Isometric.GetDepthZ(x, y + 1, priorityZ);
        return Isometric.GetDepthZ(x, y, priorityZ);
    }

    // Legacy LandView hue chain: highlight > out-of-range gray > dead-world
    // gray, else 0 (no override). Caller pre-computes the conditions.
    internal static ushort ResolveLandHue(bool highlight, bool outOfRange, bool grayWorld)
    {
        if (highlight) return Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
        if (outOfRange) return Constants.OUT_RANGE_COLOR;
        if (grayWorld) return Constants.DEAD_RANGE_COLOR;
        return 0;
    }

    // Bodies using the female/gargoyle chest-under-torso draw-order variant
    // (legacy PaperdollOrder altTorsoTable gate). Gargoyle bodies only exist on
    // CV >= 7000 but the id test itself is version-independent.
    internal static bool IsAltTorsoBody(ushort bodyGfx)
        => bodyGfx == 0x0191 || bodyGfx == 0x0193 || bodyGfx == 0x025D
        || bodyGfx == 0x029A || bodyGfx == 0x029B || bodyGfx == 0x02B7;

    private static void RenderEquipment(
        Res<SelectedEntity> selectedEntity,
        Res<UltimaBatcher2D> batch,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Profile profile,
        ref readonly WorldFx fx,
        int? maxZ,
        Vector2 center,
        Vector2 mousePos,
        Query<Data<Graphic, Hue>> qLayers,
        Query<Data<EquipmentSlots, ScreenPositionOffset, WorldPosition, Graphic, Facing, MobileSteps, MobAnimation, ServerFlags, Notoriety, AlphaFade>,
            Filter<Without<ContainedInto>, Optional<MobileSteps>, Optional<MobAnimation>, Optional<ServerFlags>, Optional<Notoriety>>> queryEquipmentSlots)
    {
        // Cache frequently accessed resources
        var tileDataCache = fileManager.Value.TileData;
        var animations = assetsServer.Value.Animations;

        // Equipment draw-order scratch — allocated once and reused per entity
        // (stackalloc inside the loop would grow the stack each iteration).
        Span<ushort> equipGfx = stackalloc ushort[PaperdollOrder.N];
        Span<Layer> rawOrder = stackalloc Layer[PaperdollOrder.N];
        Span<Layer> drawOrder = stackalloc Layer[PaperdollOrder.N];

        foreach (var (entity, slots, offset, pos, graphic, _, steps, animation, sFlags, notoriety, fade) in queryEquipmentSlots)
        {
            // Early filtering — RenderBodies already stepped this mobile's
            // fade this frame; equipment only mirrors the value.
            var aboveMaxZ = maxZ.HasValue && pos.Ref.Z >= maxZ;
            if (aboveMaxZ && fade.Ref.Value == 0)
                continue;

            var fadeAlpha = fade.Ref.Value / 255f;

            if (!Races.IsHuman(graphic.Ref.Value))
                continue;

            if (!animation.IsValid())
                continue;

            var mobFlags = sFlags.IsValid() ? sFlags.Ref.Value : Flags.None;
            var notoFlag = notoriety.IsValid() ? notoriety.Ref.Value : NotorietyFlag.Unknown;
            var isDead = IsDeadBody(graphic.Ref.Value);
            var isHidden = (mobFlags & Flags.Hidden) != 0;
            var highlighted = profile.HighlightGameObjects && entity.Ref == selectedEntity.Value.Entity;

            // Same override the body got — equipment recolors with its mobile.
            var overrideHue = MobOverrideHue(profile, in fx, highlighted, pos.Ref.X, pos.Ref.Y,
                graphic.Ref.Value, mobFlags, notoFlag, isDead, isHidden);

            // Calculate priority and depth
            var priorityZ = pos.Ref.Z + 2;
            var depthZ = BodyDepthZ(pos.Ref.X, pos.Ref.Y, priorityZ, offset.Ref.Value);

            // Fix direction for animation
            (var dir, var mirror) = FixDirection(animation.Ref.Direction);

            // Equipment draw-order via the shared PaperdollOrder algorithm (same
            // source as the paperdoll gump + main's in-world views): pick a base
            // table by arms/torso AnimID, apply per-graphic reorder rules, filter,
            // then reposition the cloak by facing direction. altTorsoTable gates
            // the female/gargoyle chest-under variant — derived from the body
            // graphic (gargoyle bodies 0x029A/0x029B only exist on CV >= 7000).
            bool altTorso = IsAltTorsoBody(graphic.Ref.Value);

            equipGfx.Clear();
            for (int l = (int)Layer.OneHanded; l <= (int)Layer.Legs; l++)
            {
                var le = slots.Ref[(Layer)l];
                if (!le.IsValid() || !qLayers.TryGet(le, out var equipRow)) continue;
                var (lg, _) = equipRow;
                var lgfx = lg.Ref.Value;
                if (lgfx != 0) equipGfx[l] = tileDataCache.StaticData[lgfx].AnimID;
            }

            PaperdollOrder.Build(equipGfx, altTorso, rawOrder);
            int layerCount = PaperdollOrder.Filter(rawOrder, includeBackpack: false, drawOrder);
            layerCount = PaperdollOrder.ApplyDirectionCloak(drawOrder, layerCount, (byte)((int)animation.Ref.Direction & 0x7));

            // Process each equipment layer — Mount first (j == -1), then the
            // PaperdollOrder result back-to-front.
            for (int j = -1; j < layerCount; j++)
            {
                var layer = j == -1 ? Layer.Mount : drawOrder[j];
                var layerEnt = slots.Ref[layer];

                // Skip invalid or hidden layers
                if (!layerEnt.IsValid())
                    continue;

                if (!qLayers.TryGet(layerEnt, out var layerRow))
                {
                    // slots.Ref[layer] = 0;
                    continue;
                }

                if (layer != Layer.Mount && IsItemCovered2(qLayers, ref slots.Ref, layer))
                    continue;

                (var gfx, var hue) = layerRow;

                // Get layer data
                byte animAction = animation.Ref.Action;
                var graphicLayer = gfx.Ref.Value;
                var hueLayer = hue.Ref.Value;
                var animId = graphicLayer;
                var offsetY = 0;

                // Handle mount layer specially
                if (layer == Layer.Mount)
                {
                    (animId, offsetY) = Mounts.FixMountGraphic(fileManager.Value.TileData, animId);
                    animAction = animation.Ref.MountAction;
                }
                else if (tileDataCache.StaticData[graphicLayer].AnimID != 0)
                {
                    animId = tileDataCache.StaticData[graphicLayer].AnimID;
                }

                // Get animation frames
                var frames = animations.GetAnimationFrames(
                    animId,
                    animAction,
                    (byte)dir,
                    out var baseHue,
                    out var isUop
                );

                ref readonly var frame = ref frames.IsEmpty ?
                    ref SpriteInfo.Empty
                    :
                    ref frames[animation.Ref.Index % frames.Length];

                if (frame.Texture == null)
                    continue;

                // Calculate position
                var position = pos.Ref.WorldToScreen();
                position.Y -= offsetY;
                position.X += 22;
                position.Y += 22;
                if (mirror)
                    position.X -= frame.UV.Width - frame.Center.X;
                else
                    position.X -= frame.Center.X;
                position.Y -= frame.UV.Height + frame.Center.Y;

                var color = ShaderHueTranslator.GetHueVector(
                    FixHue(overrideHue != 0 ? overrideHue : hueLayer != 0 ? hueLayer : baseHue),
                    false, fadeAlpha);
                position += offset.Ref.Value;

                if (highlighted)
                {
                    color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                    color.Y = ShaderHueTranslator.SHADER_HUED;
                    color.Z = fadeAlpha;
                }

                // Selection checking
                if (!aboveMaxZ && animations.PixelCheck(
                        animId,
                        animAction,
                        (byte)dir,
                        isUop,
                        animation.Ref.Index,
                        mirror ? (int)((position.X - center.X) + frame.UV.Width - mousePos.X) : (int)(mousePos.X - (position.X - center.X)),
                        (int)(mousePos.Y - (position.Y - center.Y))
                    ))
                {
                    selectedEntity.Value.Set(entity.Ref, depthZ);
                }

                // Draw the equipment piece
                batch.Value.Draw(
                    frame.Texture,
                    position - center,
                    frame.UV,
                    color,
                    0f,
                    Vector2.Zero,
                    1f,
                    mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    depthZ + (j == -1 ? -0.01f : 0f) // hack to bring the mount back to the body
                );
            }
        }
    }


    internal static ushort FixHue(ushort hue)
    {
        var fixedColor = (ushort)(hue & 0x3FFF);

        if (fixedColor != 0)
        {
            if (fixedColor >= 0x0BB8)
            {
                fixedColor = 1;
            }

            fixedColor |= (ushort)(hue & 0xC000);
        }
        else
        {
            fixedColor = (ushort)(hue & 0x8000);
        }

        return fixedColor;
    }

    internal static byte CalculateObjectHeight(ref int maxObjectZ, ref readonly StaticTiles itemData)
    {
        if (
            itemData.Height != 0xFF /*&& itemData.Flags != 0*/
        )
        {
            byte height = itemData.Height;

            if (itemData.Height == 0)
            {
                if (!itemData.IsBackground && !itemData.IsSurface)
                {
                    height = 10;
                }
            }

            if ((itemData.Flags & TileFlag.Bridge) != 0)
            {
                height /= 2;
            }

            maxObjectZ += height;

            return height;
        }

        return 0xFF;
    }

    private static bool CanBeDrawn(ClientVersion version, TileDataLoader tileData, ushort g)
    {
        switch (g)
        {
            case 0x0001:
            case 0x21BC:
            case 0xA1FE:
            case 0xA1FF:
            case 0xA200:
            case 0xA201:
                //case 0x5690:
                return false;

            case 0x9E4C:
            case 0x9E64:
            case 0x9E65:
            case 0x9E7D:
                ref var data = ref tileData.StaticData[g];

                return !data.IsBackground && !data.IsSurface;
        }

        if (g != 0x63D3)
        {
            if (g >= 0x2198 && g <= 0x21A4)
            {
                return false;
            }

            // Easel fix.
            // In older clients the tiledata flag for this
            // item contains NoDiagonal for some reason.
            // So the next check will make the item invisible.
            if (g == 0x0F65 && version < ClientVersion.CV_60144)
            {
                return true;
            }

            if (g < tileData.StaticData.Length)
            {
                ref var data = ref tileData.StaticData[g];

                // Hacky way to do not render "nodraw" (frozen lookup — see MapDrawHelpers)
                if (MapDrawHelpers.IsNoDraw(tileData, g))
                    return false;

                if (
                    !data.IsNoDiagonal
                    || data.IsAnimated
                // && world.Player != null
                // && world.Player.Race == RaceType.GARGOYLE
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static (Direction, bool) FixDirection(Direction dir)
    {
        dir &= ~Direction.Running;
        dir &= Direction.Mask;
        var mirror = false;

        switch (dir)
        {
            case Direction.East:
            case Direction.South:
                mirror = dir == Direction.East;
                dir = Direction.Right;

                break;

            case Direction.Right:
            case Direction.Left:
                mirror = dir == Direction.Right;
                dir = Direction.East;

                break;

            case Direction.North:
            case Direction.West:
                mirror = dir == Direction.North;
                dir = Direction.Down;

                break;

            case Direction.Down:
                dir = Direction.North;

                break;

            case Direction.Up:
                dir = Direction.South;

                break;
        }

        return (dir, mirror);
    }

    // Whether the item worn on `layer` is hidden by something layered over it
    // (robe / legs / etc.). A faithful port of legacy ItemGump.IsCovered; split
    // per layer so each rule set is readable. The graphic-id constants are raw
    // UO art ids and intentionally opaque. Tested in WorldRenderingTests.
    internal static bool IsItemCovered2(Query<Data<Graphic, Hue>> qLayer, ref EquipmentSlots slots, Layer layer)
        => layer switch
        {
            Layer.Shoes => CoversShoes(qLayer, in slots),
            Layer.Pants => CoversPants(qLayer, in slots),
            Layer.Tunic => CoversTunic(qLayer, in slots),
            Layer.Torso => CoversTorso(qLayer, in slots),
            Layer.Arms => CoversArms(qLayer, in slots),
            Layer.Helmet or Layer.Hair => CoversHelmetOrHair(qLayer, in slots),
            _ => false,
        };

    static bool LayerGraphicIs(Query<Data<Graphic, Hue>> qLayer, in EquipmentSlots s, Layer l, ushort value)
    {
        if (qLayer.TryGet(s[l], out var row))
        {
            (var gfx, _) = row;
            return gfx.Ref.Value == value;
        }
        return false;
    }

    static bool LayerGraphicIsAny(Query<Data<Graphic, Hue>> qLayer, in EquipmentSlots s, Layer l, params ReadOnlySpan<ushort> values)
    {
        foreach (var v in values)
            if (LayerGraphicIs(qLayer, in s, l, v)) return true;
        return false;
    }

    static bool LayerGraphicIsNotAny(Query<Data<Graphic, Hue>> qLayer, in EquipmentSlots s, Layer l, params ReadOnlySpan<ushort> values)
    {
        if (!qLayer.TryGet(s[l], out var row)) return false;
        (var gfx, _) = row;
        foreach (var v in values)
            if (gfx.Ref.Value == v) return false;
        return true;
    }

    static bool CoversShoes(Query<Data<Graphic, Hue>> qLayer, in EquipmentSlots slots)
    {
        if (slots[Layer.Legs].IsValid() ||
            (slots[Layer.Pants].IsValid() && LayerGraphicIs(qLayer, in slots, Layer.Pants, 0x1411)))
            return true;

        return (slots[Layer.Pants].IsValid() && LayerGraphicIsAny(qLayer, in slots, Layer.Pants, 0x0513, 0x0514)) ||
               (slots[Layer.Robe].IsValid() && LayerGraphicIs(qLayer, in slots, Layer.Robe, 0x0504));
    }

    static bool CoversPants(Query<Data<Graphic, Hue>> qLayer, in EquipmentSlots slots)
    {
        if (slots[Layer.Legs].IsValid() ||
            (slots[Layer.Robe].IsValid() && LayerGraphicIs(qLayer, in slots, Layer.Robe, 0x0504)))
            return true;

        if (slots[Layer.Pants].IsValid() && LayerGraphicIsAny(qLayer, in slots, Layer.Pants, 0x01EB, 0x03E5, 0x03EB))
        {
            if (slots[Layer.Skirt].IsValid() && LayerGraphicIsNotAny(qLayer, in slots, Layer.Skirt, 0x01C7, 0x01E4))
                return true;

            if (slots[Layer.Robe].IsValid() && qLayer.TryGet(slots[Layer.Robe], out var pantsRobeRow))
            {
                (var rgfx, _) = pantsRobeRow;
                var rv = rgfx.Ref.Value;
                if (rv != 0x0229 && !(rv >= 0x04E8 && rv <= 0x04EB))
                    return true;
            }
        }
        return false;
    }

    static bool CoversTunic(Query<Data<Graphic, Hue>> qLayer, in EquipmentSlots slots)
    {
        if (slots[Layer.Robe].IsValid() && qLayer.Contains(slots[Layer.Robe]))
            return LayerGraphicIsNotAny(qLayer, in slots, Layer.Robe, 0x0000, 0x9985, 0x9986, 0xA412);

        if (slots[Layer.Tunic].IsValid() && LayerGraphicIs(qLayer, in slots, Layer.Tunic, 0x0238))
            return slots[Layer.Robe].IsValid() && LayerGraphicIsNotAny(qLayer, in slots, Layer.Robe, 0x9985, 0x9986, 0xA412);

        return false;
    }

    static bool CoversTorso(Query<Data<Graphic, Hue>> qLayer, in EquipmentSlots slots)
    {
        if (slots[Layer.Robe].IsValid() && LayerGraphicIsNotAny(qLayer, in slots, Layer.Robe, 0x0000, 0x9985, 0x9986, 0xA412, 0xA2CA))
            return true;

        if (slots[Layer.Tunic].IsValid() && LayerGraphicIsNotAny(qLayer, in slots, Layer.Tunic, 0x1541, 0x1542))
            return slots[Layer.Torso].IsValid() && LayerGraphicIsAny(qLayer, in slots, Layer.Torso, 0x782A, 0x782B);

        return false;
    }

    static bool CoversArms(Query<Data<Graphic, Hue>> qLayer, in EquipmentSlots slots)
        => slots[Layer.Robe].IsValid() && LayerGraphicIsNotAny(qLayer, in slots, Layer.Robe, 0x0000, 0x9985, 0x9986, 0xA412);

    static bool CoversHelmetOrHair(Query<Data<Graphic, Hue>> qLayer, in EquipmentSlots slots)
    {
        if (slots[Layer.Robe].IsValid() && qLayer.TryGet(slots[Layer.Robe], out var helmRobeRow))
        {
            (var gfx, _) = helmRobeRow;
            var v = gfx.Ref.Value;

            if (v > 0x3173)
            {
                if (v is 0x4B9D or 0x7816)
                    return true;
            }
            else if (v <= 0x2687)
            {
                if (v < 0x2683)
                {
                    if (v is >= 0x204E and <= 0x204F)
                        return true;
                }
                else
                {
                    return true;
                }
            }
            else if (v is 0x2FB9 or 0x3173)
            {
                return true;
            }
        }
        return false;
    }



    private struct MaxZInfo
    {
        public int? MaxZ;
        public int? MaxZGround;
        public int? MaxZRoof;
        public bool DrawRoof;
        public bool IsSameTile;
        public bool IsTileAhead;
        public bool IsUnderStatic;

        public bool IsUnderRoof => IsSameTile && IsTileAhead;
    }
}


internal sealed class SelectedEntity
{
    private static readonly bool[,] _InternalArea = new bool[44, 44];

    static SelectedEntity()
    {
        for (int y = 21, i = 0; y >= 0; --y, i++)
        {
            for (int x = 0; x < 22; x++)
            {
                if (x < i)
                {
                    continue;
                }

                _InternalArea[x, y] = _InternalArea[43 - x, 43 - y] = _InternalArea[43 - x, y] = _InternalArea[x, 43 - y] = true;
            }
        }
    }

    private ulong _lastEntity;
    private bool _lastIsText;

    public ulong Entity { get; private set; }
    public float DepthZ { get; private set; }

    // True when the winning pick came from overhead TEXT rather than the
    // object's own pixels. Legacy keeps the TextObject itself in
    // SelectedObject.Object — actions route to the owner, but anything
    // gated on "hovering the entity proper" (tooltip) must not fire.
    public bool IsText { get; private set; }

    // Gated each frame by mouse-in-viewport; off => no world object picks.
    public bool Enabled = true;

    // An entity the pick must never select (house-design placement ghost): it is
    // a renderable static sitting on the cursor tile, so it would otherwise win
    // the pick and get the selection-highlight hue. Set by HouseCustomization.
    public ulong IgnoreEntity;

    // Same intent for the multi-placement preview (legacy MultiView excludes
    // IsHousePreview from selection): a whole house of ghost blocks sits on the
    // cursor tiles, so without this they'd win the pick and the base tile would
    // resolve to a preview block — the multi jitters. Maintained by
    // SyncMultiPreview; empty (and skipped) in normal play.
    public readonly HashSet<ulong> IgnorePickEntities = new();

    // bypassViewport: UI window claims (paperdoll / container / server gumps)
    // must register even when the cursor is outside Camera.Bounds — gumps live
    // in the side gutters and top bar, which are off the world viewport. The
    // Enabled gate exists only to stop WORLD tile/static/mobile picks out
    // there; UI selection is what drop/pickup target, so it always lands.
    // Without this, releasing a held item over a gutter-parked paperdoll left
    // SelectedEntity at 0 -> DropItem cleared the cursor with no drop packet,
    // so the client dropped the item locally while the server kept dragging.
    public void Set(ulong entity, float depth, bool bypassViewport = false, bool isText = false)
    {
        if (!Enabled && !bypassViewport)
            return;

        if (IgnoreEntity != 0 && entity == IgnoreEntity)
            return;

        if (IgnorePickEntities.Count != 0 && IgnorePickEntities.Contains(entity))
            return;

        if (_lastEntity.IsValid() && _lastEntity != entity)
        {
            if (depth >= DepthZ)
            {
                _lastEntity = entity;
                DepthZ = depth;
                _lastIsText = isText;
            }
        }
        else
        {
            _lastEntity = entity;
            DepthZ = depth;
            _lastIsText = isText;
        }
    }

    public void Clear()
    {
        Entity = _lastEntity;
        DepthZ = 0;
        _lastEntity = 0;
        IsText = _lastIsText;
        _lastIsText = false;
    }

    public void IsPointInStretchedLand(ulong entity, float depthZ, ref readonly UltimaBatcher2D.YOffsets yOffsets, Vector2 mousePosition, Vector2 position)
    {
        //y -= 22;
        position.X += 22f;

        var testX = mousePosition.X - position.X;
        var testY = mousePosition.Y;

        var y0 = -yOffsets.Top;
        var y1 = 22 - yOffsets.Left;
        var y2 = 44 - yOffsets.Bottom;
        var y3 = 22 - yOffsets.Right;

        var contains = testY >= testX * (y1 - y0) / -22 + position.Y + y0 &&
            testY >= testX * (y3 - y0) / 22 + position.Y + y0 &&
            testY <= testX * (y3 - y2) / 22 + position.Y + y2 &&
            testY <= testX * (y1 - y2) / -22 + position.Y + y2;

        if (contains)
            Set(entity, depthZ);
    }

    public void IsPointInLand(ulong entity, float depthZ, Vector2 mousePos, Vector2 position)
    {
        position.X = mousePos.X - position.X;
        position.Y = mousePos.Y - position.Y;

        var contains = position.X >= 0 && position.X < 44 && position.Y >= 0 && position.Y < 44 && _InternalArea[(int)position.X, (int)position.Y];

        if (contains)
            Set(entity, depthZ);
    }
}
