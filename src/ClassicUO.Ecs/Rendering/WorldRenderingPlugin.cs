using System;
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
                        Query<Data<WorldPosition, ScreenPosition>, Changed<WorldPosition>> query) =>
            {
                foreach ((var ent, var worldPos) in query2)
                {
                    var iso = worldPos.Ref.WorldToScreen();
                    commands.Entity(ent.Ref).Insert(new ScreenPosition() { Value = iso });
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
            .Build()

            .AddSystem(renderingFn)
            .InStage(Stage.PostUpdate)
            .SingleThreaded()
            .Label("cuo:rendering:rendering")
            .After("cuo:rendering:begin")
            .RunIf((Commands cmds) => cmds.HasResource<GraphicsDevice>())
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Query<Data<WorldPosition>, With<Player>> playerQuery) => playerQuery.Count() > 0)
            .Build()

            .AddSystem(endRenderingFn)
            .InStage(Stage.PostUpdate)
            .SingleThreaded()
            .Label("cuo:rendering:end")
            .After("cuo:rendering:rendering")
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
        Query<Empty, With<NormalMulti>> qNormalMultis,
        Single<Data<WorldPosition>, With<Player>> queryPlayer,
        Query<Data<WorldPosition, ScreenPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> queryTiles,
        Query<Data<WorldPosition, ScreenPosition, Graphic, Hue, Amount, NetworkSerial>, Filter<Without<IsTile>, Without<MobAnimation>, Without<ContainedInto>, Optional<Amount>, Optional<NetworkSerial>>> queryStatics,
        Query<Data<WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Facing, MobAnimation, MobileSteps, ServerFlags, Notoriety>,
            Filter<Without<ContainedInto>, Optional<Facing>, Optional<MobAnimation>, Optional<MobileSteps>, Optional<ServerFlags>, Optional<Notoriety>>> queryBodyOnly,
        Query<Data<EquipmentSlots, ScreenPositionOffset, WorldPosition, Graphic, Facing, MobileSteps, MobAnimation, ServerFlags, Notoriety>,
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

        var fx = new WorldFx
        {
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

        // Render each layer
        RenderTiles(
            selectedEntity, gameCtx, batch, assetsServer, fileManager,
            camera, profile.Value, in fx, calculateZ, ref workingZInfo, playerX, playerY, playerZ16,
            backupZInfo, maxZ, center, mousePos, cameraBounds, queryTiles);

        RenderStatics(
            selectedEntity, gameCtx, batch, assetsServer, fileManager,
            camera, profile.Value, in fx, calculateZ, ref workingZInfo, playerX, playerY, playerZ14,
            backupZInfo, maxZ, center, mousePos, cameraBounds, qNormalMultis, queryStatics);

        RenderBodies(
            selectedEntity, batch, assetsServer, fileManager,
            profile.Value, in fx, maxZ, center, mousePos, queryBodyOnly);

        RenderEquipment(
            selectedEntity, batch, assetsServer, fileManager,
            profile.Value, in fx, maxZ, center, mousePos, qLayers, queryEquipmentSlots);

        RenderEffects();

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
    }

    private struct RenderScratch
    {
        public (int lastPosX, int lastPosY, int lastPosZ)? LastPos;
        public MaxZInfo ZInfo;
    }

    // Legacy Mobile.IsDead graphics (ghost bodies).
    private static bool IsDeadBody(ushort graphic)
        => graphic == 0x0192 || graphic == 0x0193 ||
           (graphic >= 0x025F && graphic <= 0x0260) ||
           graphic == 0x02B6 || graphic == 0x02B7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Dist(int x0, int y0, int x1, int y1)
        => Math.Max(Math.Abs(x0 - x1), Math.Abs(y0 - y1));

    // Legacy Static._canBeTransparent + TransparentTest — which statics the
    // circle of transparency may see through, given playerZ + 5.
    private static bool TransparentTest(int z5, int objZ, ref readonly StaticTiles data)
    {
        if (objZ <= z5 - data.Height)
            return false;

        if (z5 < objZ && !CanBeTransparent(in data))
            return false;

        return true;
    }

    private static bool CanBeTransparent(ref readonly StaticTiles data)
        => data.Height > 5 || data.Height == 0 ||
           data.IsRoof || (data.IsSurface && data.IsBackground) || data.IsWall ||
           (data.Height == 5 && data.IsSurface && !data.IsBackground);


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
        Query<Data<WorldPosition, ScreenPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> queryTiles)
    {
        // Cache frequently accessed resources
        var tileDataCache = fileManager.Value.TileData;
        var texmaps = assetsServer.Value.Texmaps;
        var arts = assetsServer.Value.Arts;

        // Process all tiles in one pass
        foreach (var (entity, worldPos, screenPos, graphic, stretched) in queryTiles)
        {
            // Early filtering
            var hide = backupZInfo.MaxZGround.HasValue && worldPos.Ref.Z > backupZInfo.MaxZGround;
            if (!calculateZ && hide)
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

            if (hide)
                continue;

            // Legacy LandView hue chain: highlight > out-of-range > dead gray.
            ushort hueOverride = 0;
            if (profile.HighlightGameObjects && entity.Ref == selectedEntity.Value.Entity)
                hueOverride = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
            else if (profile.NoColorObjectsOutOfRange && Dist(worldPos.Ref.X, worldPos.Ref.Y, fx.PlayerX, fx.PlayerY) > fx.ViewRange)
                hueOverride = Constants.OUT_RANGE_COLOR;
            else if (fx.GrayWorld)
                hueOverride = Constants.DEAD_RANGE_COLOR;

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
                    ? new Vector3(hueOverride - 1, ShaderHueTranslator.SHADER_LAND_HUED, 1f)
                    : new Vector3(0, ShaderHueTranslator.SHADER_LAND, 1f);

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
                    ? new Vector3(hueOverride - 1, ShaderHueTranslator.SHADER_HUED, 1f)
                    : Vector3.UnitZ;

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
        Query<Data<WorldPosition, ScreenPosition, Graphic, Hue, Amount, NetworkSerial>, Filter<Without<IsTile>, Without<MobAnimation>, Without<ContainedInto>, Optional<Amount>, Optional<NetworkSerial>>> queryStatics)
    {
        // Cache frequently accessed resources
        var tileDataCache = fileManager.Value.TileData;
        var arts = assetsServer.Value.Arts;

        // Process all statics in one pass with optimized property access
        foreach (var (entity, worldPos, screenPos, graphic, hue, amount, serial) in queryStatics)
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

            if (profile.FieldsType == 2 && tileData.IsAnimated)
            {
                var g = graphic.Ref.Value;
                if (StaticFilters.IsFireField(g)) { drawGraphic = Constants.FIELD_REPLACE_GRAPHIC; hueValue = 0x0020; }
                else if (StaticFilters.IsParalyzeField(g)) { drawGraphic = Constants.FIELD_REPLACE_GRAPHIC; hueValue = 0x0058; }
                else if (StaticFilters.IsEnergyField(g)) { drawGraphic = Constants.FIELD_REPLACE_GRAPHIC; hueValue = 0x0070; }
                else if (StaticFilters.IsPoisonField(g)) { drawGraphic = Constants.FIELD_REPLACE_GRAPHIC; hueValue = 0x0044; }
                else if (StaticFilters.IsWallOfStone(g)) { drawGraphic = Constants.FIELD_REPLACE_GRAPHIC; hueValue = 0x038A; }
            }

            var hide = tileData.IsRoof && (!backupZInfo.DrawRoof || !profile.DrawRoofs);
            hide |= maxZ.HasValue && worldPos.Ref.Z >= maxZ;
            if (!calculateZ && hide)
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

            if (hide)
                continue;

            // Position calculation
            position.X -= (short)((artInfo.UV.Width >> 1) - 22);
            position.Y -= (short)(artInfo.UV.Height - 44);

            // Priority calculation
            var priorityZ = worldPos.Ref.Z;
            if (tileData.IsBackground) priorityZ -= 1;
            if (tileData.Height != 0) priorityZ += 1;
            if (tileData.IsWall) priorityZ += 2;
            if (tileData.IsMultiMovable) priorityZ += 1;
            if (qNormalMultis.Contains(entity.Ref)) priorityZ -= 1;

            var depthZ = Isometric.GetDepthZ(worldPos.Ref.X, worldPos.Ref.Y, priorityZ);

            // Circle of transparency. Full mode flags the shader per sprite
            // (circletrans); gradient mode fades alpha by distance³ from the
            // player (legacy GetGradientCotAlpha) and culls when fully clear.
            var circleTrans = false;
            var alpha = 1f;
            if ((fx.CotFull || fx.CotGradient) && TransparentTest(fx.PlayerZ5, worldPos.Ref.Z, in tileData))
            {
                if (fx.CotFull)
                {
                    circleTrans = !isTree && !tileData.IsFoliage;
                }
                else
                {
                    var dx = iso.X - fx.CotCenter.X;
                    var dy = (iso.Y - 44) - fx.CotCenter.Y;
                    var distSq = dx * dx + dy * dy;
                    if (distSq < fx.CotRadiusSq)
                    {
                        var ratio = MathF.Sqrt(distSq / fx.CotRadiusSq);
                        var alphaByte = (byte)(ratio * ratio * ratio * 255f);
                        if (alphaByte == 0)
                            continue;
                        alpha = alphaByte / 255f;
                    }
                }
            }

            // Legacy StaticView/ItemView hue chain: highlight > out-of-range >
            // dead gray; selected interactable items keep the 0x0035 hover hue.
            var partialHue = tileData.IsPartialHue;
            var isSelected = entity.Ref == selectedEntity.Value.Entity;
            if (profile.HighlightGameObjects && isSelected)
            {
                hueValue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
                partialHue = false;
            }
            else if (profile.NoColorObjectsOutOfRange && Dist(worldPos.Ref.X, worldPos.Ref.Y, fx.PlayerX, fx.PlayerY) > fx.ViewRange)
            {
                hueValue = Constants.OUT_RANGE_COLOR;
                partialHue = false;
            }
            else if (fx.GrayWorld)
            {
                hueValue = Constants.DEAD_RANGE_COLOR;
                partialHue = false;
            }
            else if (isSelected && serial.IsValid() && !qNormalMultis.Contains(entity.Ref))
            {
                hueValue = 0x0035;
            }

            var color = Renderer.ShaderHueTranslator.GetHueVector(hueValue, partialHue, alpha, circletrans: circleTrans);

            // Selection checking
            var p = mousePos - position;
            if (assetsServer.Value.Arts.PixelCheck(drawGraphic, (int)p.X, (int)p.Y))
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
    }

    private static void RenderEffects()
    {
        // TODO: implement
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
        Query<Data<WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Facing, MobAnimation, MobileSteps, ServerFlags, Notoriety>,
            Filter<Without<ContainedInto>, Optional<Facing>, Optional<MobAnimation>, Optional<MobileSteps>, Optional<ServerFlags>, Optional<Notoriety>>> queryBodyOnly)
    {
        // Cache animation service
        var animations = assetsServer.Value.Animations;

        foreach (var (entity, pos, graphic, hue, serial, offset, direction, animation, steps, sFlags, notoriety) in queryBodyOnly)
        {
            // Early filtering
            if (maxZ.HasValue && pos.Ref.Z >= maxZ)
                continue;

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

            var depthZ = Isometric.GetDepthZ(pos.Ref.X, pos.Ref.Y, priorityZ);
            var color = ShaderHueTranslator.GetHueVector(FixHue(uoHue));
            position += offset.Ref.Value;

            // Adjust depth based on offset
            if (offset.Ref.Value.X > 0 && offset.Ref.Value.Y > 0)
            {
                depthZ = Isometric.GetDepthZ(pos.Ref.X + 1, pos.Ref.Y, priorityZ);
            }
            else if (offset.Ref.Value.X == 0 && offset.Ref.Value.Y > 0)
            {
                depthZ = Isometric.GetDepthZ(pos.Ref.X + 1, pos.Ref.Y + 1, priorityZ);
            }
            else if (offset.Ref.Value.X < 0 && offset.Ref.Value.Y > 0)
            {
                depthZ = Isometric.GetDepthZ(pos.Ref.X, pos.Ref.Y + 1, priorityZ);
            }

            if (highlighted)
            {
                color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                color.Y = ShaderHueTranslator.SHADER_HUED;
                color.Z = 1f;
            }

            // Selection checking
            if (animations.PixelCheck(
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
        Query<Data<EquipmentSlots, ScreenPositionOffset, WorldPosition, Graphic, Facing, MobileSteps, MobAnimation, ServerFlags, Notoriety>,
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

        foreach (var (entity, slots, offset, pos, graphic, _, steps, animation, sFlags, notoriety) in queryEquipmentSlots)
        {
            // Early filtering
            if (maxZ.HasValue && pos.Ref.Z >= maxZ)
                continue;

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
            var depthZ = Isometric.GetDepthZ(pos.Ref.X, pos.Ref.Y, priorityZ);

            // Adjust depth based on offset
            if (offset.Ref.Value.X > 0 && offset.Ref.Value.Y > 0)
            {
                depthZ = Isometric.GetDepthZ(pos.Ref.X + 1, pos.Ref.Y, priorityZ);
            }
            else if (offset.Ref.Value.X == 0 && offset.Ref.Value.Y > 0)
            {
                depthZ = Isometric.GetDepthZ(pos.Ref.X + 1, pos.Ref.Y + 1, priorityZ);
            }
            else if (offset.Ref.Value.X < 0 && offset.Ref.Value.Y > 0)
            {
                depthZ = Isometric.GetDepthZ(pos.Ref.X, pos.Ref.Y + 1, priorityZ);
            }

            // Fix direction for animation
            (var dir, var mirror) = FixDirection(animation.Ref.Direction);

            // Equipment draw-order via the shared PaperdollOrder algorithm (same
            // source as the paperdoll gump + main's in-world views): pick a base
            // table by arms/torso AnimID, apply per-graphic reorder rules, filter,
            // then reposition the cloak by facing direction. altTorsoTable gates
            // the female/gargoyle chest-under variant — derived from the body
            // graphic (gargoyle bodies 0x029A/0x029B only exist on CV >= 7000).
            ushort bodyGfx = graphic.Ref.Value;
            bool altTorso = bodyGfx == 0x0191 || bodyGfx == 0x0193 || bodyGfx == 0x025D
                         || bodyGfx == 0x029A || bodyGfx == 0x029B || bodyGfx == 0x02B7;

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
                    FixHue(overrideHue != 0 ? overrideHue : hueLayer != 0 ? hueLayer : baseHue));
                position += offset.Ref.Value;

                if (highlighted)
                {
                    color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                    color.Y = ShaderHueTranslator.SHADER_HUED;
                    color.Z = 1f;
                }

                // Selection checking
                if (animations.PixelCheck(
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


    private static ushort FixHue(ushort hue)
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

    private static byte CalculateObjectHeight(ref int maxObjectZ, ref readonly StaticTiles itemData)
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

                // Hacky way to do not render "nodraw"
                if (!string.IsNullOrEmpty(data.Name) && data.Name.StartsWith("nodraw", StringComparison.OrdinalIgnoreCase))
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

    private static (Direction, bool) FixDirection(Direction dir)
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

    private static bool IsItemCovered2(Query<Data<Graphic, Hue>> qLayer, ref EquipmentSlots slots, Layer layer)
    {
        bool isOk(Layer l, ref EquipmentSlots s, ushort value)
        {
            if (qLayer.TryGet(s[l], out var row))
            {
                (var gfx, _) = row;
                return gfx.Ref.Value == value;
            }
            return false;
        }

        bool isAny(Layer l, ref EquipmentSlots s, params ReadOnlySpan<ushort> values)
        {
            foreach (var v in values)
                if (isOk(l, ref s, v)) return true;
            return false;
        }

        bool isNotAny(Layer l, ref EquipmentSlots s, params ReadOnlySpan<ushort> values)
        {
            if (!qLayer.TryGet(s[l], out var row)) return false;
            (var gfx, _) = row;
            foreach (var v in values)
                if (gfx.Ref.Value == v) return false;
            return true;
        }

        bool inRange(Layer l, ref EquipmentSlots s, ushort min, ushort max)
        {
            if (!qLayer.TryGet(s[l], out var row)) return false;
            (var gfx, _) = row;
            return gfx.Ref.Value >= min && gfx.Ref.Value <= max;
        }

        switch (layer)
        {
            case Layer.Shoes:
                if (slots[Layer.Legs].IsValid() ||
                    (slots[Layer.Pants].IsValid() && isOk(Layer.Pants, ref slots, 0x1411)))
                {
                    return true;
                }
                else if (
                    (slots[Layer.Pants].IsValid() && isAny(Layer.Pants, ref slots, 0x0513, 0x0514)) ||
                    (slots[Layer.Robe].IsValid() && isOk(Layer.Robe, ref slots, 0x0504))
                )
                {
                    return true;
                }
                break;

            case Layer.Pants:
                if (slots[Layer.Legs].IsValid() ||
                    (slots[Layer.Robe].IsValid() && isOk(Layer.Robe, ref slots, 0x0504)))
                {
                    return true;
                }

                if (slots[Layer.Pants].IsValid() && isAny(Layer.Pants, ref slots, 0x01EB, 0x03E5, 0x03EB))
                {
                    if (slots[Layer.Skirt].IsValid() && isNotAny(Layer.Skirt, ref slots, 0x01C7, 0x01E4))
                    {
                        return true;
                    }

                    if (slots[Layer.Robe].IsValid() && qLayer.TryGet(slots[Layer.Robe], out var pantsRobeRow))
                    {
                        (var rgfx, _) = pantsRobeRow;
                        var rv = rgfx.Ref.Value;
                        if (rv != 0x0229 && !(rv >= 0x04E8 && rv <= 0x04EB))
                        {
                            return true;
                        }
                    }
                }
                break;

            case Layer.Tunic:
                if (slots[Layer.Robe].IsValid() && qLayer.Contains(slots[Layer.Robe]))
                {
                    if (isNotAny(Layer.Robe, ref slots, 0x0000, 0x9985, 0x9986, 0xA412))
                    {
                        return true;
                    }
                }
                else if (slots[Layer.Tunic].IsValid() && isOk(Layer.Tunic, ref slots, 0x0238))
                {
                    if (slots[Layer.Robe].IsValid() && isNotAny(Layer.Robe, ref slots, 0x9985, 0x9986, 0xA412))
                    {
                        return true;
                    }
                }
                break;

            case Layer.Torso:
                if (slots[Layer.Robe].IsValid() && isNotAny(Layer.Robe, ref slots, 0x0000, 0x9985, 0x9986, 0xA412, 0xA2CA))
                {
                    return true;
                }
                else if (slots[Layer.Tunic].IsValid() && isNotAny(Layer.Tunic, ref slots, 0x1541, 0x1542))
                {
                    if (slots[Layer.Torso].IsValid() && isAny(Layer.Torso, ref slots, 0x782A, 0x782B))
                    {
                        return true;
                    }
                }
                break;

            case Layer.Arms:
                if (slots[Layer.Robe].IsValid() && isNotAny(Layer.Robe, ref slots, 0x0000, 0x9985, 0x9986, 0xA412))
                {
                    return true;
                }
                break;

            case Layer.Helmet:
            case Layer.Hair:
                if (slots[Layer.Robe].IsValid() && qLayer.TryGet(slots[Layer.Robe], out var helmRobeRow))
                {
                    (var gfx, _) = helmRobeRow;
                    var v = gfx.Ref.Value;

                    if (v > 0x3173)
                    {
                        if (v is 0x4B9D or 0x7816)
                        {
                            return true;
                        }
                    }
                    else if (v <= 0x2687)
                    {
                        if (v < 0x2683)
                        {
                            if (v is >= 0x204E and <= 0x204F)
                            {
                                return true;
                            }
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
                break;
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

    public ulong Entity { get; private set; }
    public float DepthZ { get; private set; }

    // Gated each frame by mouse-in-viewport; off => no world object picks.
    public bool Enabled = true;

    // bypassViewport: UI window claims (paperdoll / container / server gumps)
    // must register even when the cursor is outside Camera.Bounds — gumps live
    // in the side gutters and top bar, which are off the world viewport. The
    // Enabled gate exists only to stop WORLD tile/static/mobile picks out
    // there; UI selection is what drop/pickup target, so it always lands.
    // Without this, releasing a held item over a gutter-parked paperdoll left
    // SelectedEntity at 0 -> DropItem cleared the cursor with no drop packet,
    // so the client dropped the item locally while the server kept dragging.
    public void Set(ulong entity, float depth, bool bypassViewport = false)
    {
        if (!Enabled && !bypassViewport)
            return;

        if (_lastEntity.IsValid() && _lastEntity != entity)
        {
            if (depth >= DepthZ)
            {
                _lastEntity = entity;
                DepthZ = depth;
            }
        }
        else
        {
            _lastEntity = entity;
            DepthZ = depth;
        }
    }

    public void Clear()
    {
        Entity = _lastEntity;
        DepthZ = 0;
        _lastEntity = 0;
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
