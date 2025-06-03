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
using World = TinyEcs.World;

namespace ClassicUO.Ecs;

internal readonly struct WorldRenderingPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        // TODO: find a better place to initialize this
        scheduler.AddResource(new Profile()
        {
            GameWindowPosition = new(20, 40)
        });
        scheduler.AddResource(new SelectedEntity());
        scheduler.AddResource(new Viewport());

        scheduler.OnFrameStart((
            Res<MouseContext> mouseCtx,
            Res<KeyboardContext> keyboardCtx,
            Res<GameContext> gameCtx,
            Res<Camera> camera,
            Local<bool> canMove
        ) =>
        {
            if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
            {
                canMove.Value = camera.Value.Bounds.Contains((int)mouseCtx.Value.Position.X, (int)mouseCtx.Value.Position.Y);
            }

            if (canMove && mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
            {
                gameCtx.Value.CenterOffset += mouseCtx.Value.PositionOffset * camera.Value.Zoom;
            }

            if (keyboardCtx.Value.IsPressedOnce(Keys.Space))
            {
                gameCtx.Value.FreeView = !gameCtx.Value.FreeView;
            }
        }, ThreadingMode.Single).RunIf((Res<UoGame> game) => game.Value.IsActive);

        scheduler.OnUpdate
        (
            (Res<GameContext> gameCtx, Query<Data<WorldPosition, ScreenPositionOffset>, With<Player>> playerQuery) =>
            {
                foreach ((var position, var offset) in playerQuery)
                {
                    gameCtx.Value.CenterX = position.Ref.X;
                    gameCtx.Value.CenterY = position.Ref.Y;
                    gameCtx.Value.CenterZ = position.Ref.Z;
                    gameCtx.Value.CenterOffset = offset.Ref.Value * -1;
                }
            },
            ThreadingMode.Single
        ).RunIf((Res<GameContext> gameCtx) => !gameCtx.Value.FreeView);

        var cleanupFn = Cleanup;
        scheduler.OnExit(GameState.GameScreen, cleanupFn, ThreadingMode.Single);


        var beginRenderingFn = BeginRendering;
        var renderingFn = Rendering;
        var showTextOverheadFn = ShowTextOverhead;
        var endRenderingFn = EndRendering;

        var beginRenderingSystem = scheduler.OnAfterUpdate(beginRenderingFn, ThreadingMode.Single);
        var endRenderingSystem = scheduler.OnAfterUpdate(endRenderingFn, ThreadingMode.Single);
        var worldRenderingSystem = scheduler.OnAfterUpdate(renderingFn, ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>())
                 .RunIf((SchedulerState state) => state.InState(GameState.GameScreen))
                 .RunIf((Query<Data<WorldPosition>, With<Player>> playerQuery) => playerQuery.Count() > 0);
        var textOverheadRenderingSystem = scheduler.OnAfterUpdate(showTextOverheadFn, ThreadingMode.Single);

        beginRenderingSystem.RunBefore(worldRenderingSystem);
        worldRenderingSystem.RunBefore(endRenderingSystem);
        textOverheadRenderingSystem.RunAfter(worldRenderingSystem);
    }


    private static void Cleanup(Res<SelectedEntity> selectedEntity)
    {
        selectedEntity.Value.Clear();
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

        public readonly bool IsUnderRoof => IsSameTile && IsTileAhead;
    }

    private static void ShowTextOverhead(
        World world,
        Time time,
        Res<TextOverHeadManager> textOverHeadManager,
        Res<NetworkEntitiesMap> networkEntities,
        Res<UltimaBatcher2D> batcher,
        Res<GameContext> gameCtx,
        Res<Camera> camera,
        Res<UOFileManager> fileManager)
    {
        textOverHeadManager.Value.Update(world, time, networkEntities);
        textOverHeadManager.Value.Render(world, networkEntities, batcher, gameCtx, camera, fileManager.Value.Hues);
    }

    private static void BeginRendering(
        Res<Camera> camera,
        Res<RenderTarget2D> renderTarget,
        Res<UltimaBatcher2D> batch,
        Res<Viewport> viewport
    )
    {
        viewport.Value = batch.Value.GraphicsDevice.Viewport;
        var cameraViewport = camera.Value.GetViewport();

        batch.Value.GraphicsDevice.Viewport = cameraViewport;
        batch.Value.GraphicsDevice.SetRenderTarget(renderTarget);
        batch.Value.GraphicsDevice.Clear(Color.Black);
        // batch.Value.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Transparent, 1.0f, 0);
    }

    private static void EndRendering(Res<UltimaBatcher2D> batch, Res<Viewport> viewport)
    {
        batch.Value.GraphicsDevice.SetRenderTarget(null);
        batch.Value.GraphicsDevice.Viewport = viewport;
    }

    private static void Rendering
    (
        TinyEcs.World world,
        Res<SelectedEntity> selectedEntity,
        Res<GameContext> gameCtx,
        Res<Renderer.UltimaBatcher2D> batch,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Res<Camera> camera,
        Local<(int lastPosX, int lastPosY, int lastPosZ)?> lastPos,
        Local<MaxZInfo> workingZInfo,
        Single<Data<WorldPosition>, With<Player>> queryPlayer,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> queryTiles,
        Query<Data<WorldPosition, Graphic, Hue>, Filter<Without<IsTile>, Without<MobAnimation>, Without<ContainedInto>>> queryStatics,
        Query<Data<WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Facing, MobAnimation, MobileSteps>,
            Filter<Without<ContainedInto>, Optional<Facing>, Optional<MobAnimation>, Optional<MobileSteps>>> queryBodyOnly,
        Query<Data<EquipmentSlots, ScreenPositionOffset, WorldPosition, Graphic, Facing, MobileSteps, MobAnimation>,
            Filter<Without<ContainedInto>, Optional<MobileSteps>, Optional<MobAnimation>>> queryEquipmentSlots
    )
    {
        batch.Value.Begin(null, camera.Value.ViewTransformMatrix);
        batch.Value.SetBrightlight(1.7f);
        batch.Value.SetSampler(SamplerState.PointClamp);
        batch.Value.SetStencil(DepthStencilState.Default);

        (_, var playerPos) = queryPlayer.Get();
        (var playerX, var playerY, var playerZ) = playerPos.Ref;

        int? maxZ = null;
        var playerZ16 = playerZ + 16;
        var playerZ14 = playerZ + 14;

        var calculateZ = !lastPos.Value.HasValue ||
                        lastPos.Value.Value.lastPosX != playerX ||
                        lastPos.Value.Value.lastPosY != playerY ||
                        lastPos.Value.Value.lastPosZ != playerZ;

        var backupZInfo = workingZInfo.Value;

        if (calculateZ)
        {
            workingZInfo.Value.MaxZ = null;
            workingZInfo.Value.MaxZGround = null;
            workingZInfo.Value.MaxZRoof = null;
            workingZInfo.Value.DrawRoof = true;
            workingZInfo.Value.IsSameTile = false;
            workingZInfo.Value.IsTileAhead = false;
            workingZInfo.Value.IsUnderStatic = false;
            lastPos.Value = (playerX, playerY, playerZ);
        }

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

        var center = Isometric.IsoToScreen(gameCtx.Value.CenterX, gameCtx.Value.CenterY, gameCtx.Value.CenterZ);
        center.X -= camera.Value.Bounds.Width / 2f;
        center.Y -= camera.Value.Bounds.Height / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.Value.CenterOffset;

        var mousePos = camera.Value.MouseToWorldPosition2();
        selectedEntity.Value.Clear();

        var cameraBounds = camera.Value.Bounds;

        var drawOffset = (int)(44 / camera.Value.Zoom);
        cameraBounds.Location = camera.Value.ScreenToWorld(new Point(-drawOffset, -drawOffset));
        var s = camera.Value.ScreenToWorld(new Point(cameraBounds.Width + drawOffset, cameraBounds.Height + drawOffset));
        cameraBounds.Width = s.X;
        cameraBounds.Height = s.Y;

        foreach ((
             var entity,
             var worldPos,
             var graphic,
             var stretched) in queryTiles)
        {
            var hide = backupZInfo.MaxZGround.HasValue && worldPos.Ref.Z > backupZInfo.MaxZGround;
            if (!calculateZ && hide)
                continue;

            var position = Isometric.IsoToScreen(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z);
            position -= center;

            if (position.X < cameraBounds.X || position.X > cameraBounds.Width)
                continue;

            if (position.Y > cameraBounds.Height)
                continue;

            if (!CanBeDrawn(gameCtx.Value.ClientVersion, fileManager.Value.TileData, graphic.Ref.Value))
                continue;

            if (calculateZ)
            {
                if (worldPos.Ref.X == playerX && worldPos.Ref.Y == playerY)
                {
                    if ((stretched.IsValid() ? stretched.Ref.AvgZ : worldPos.Ref.Z) > playerZ16)
                    {
                        workingZInfo.Value.MaxZGround = playerZ16;
                    }
                }
            }

            if (hide)
                continue;

            if (stretched.IsValid())
            {
                position.Y += worldPos.Ref.Z << 2;

                if (position.Y - (stretched.Ref.MinZ << 2) < cameraBounds.Y)
                    continue;

                ref readonly var textmapInfo = ref assetsServer.Value.Texmaps.GetTexmap(fileManager.Value.TileData.LandData[graphic.Ref.Value].TexID);
                if (textmapInfo.Texture == null)
                    continue;

                var depthZ = Isometric.GetDepthZ(worldPos.Ref.X, worldPos.Ref.Y, stretched.Ref.AvgZ - 2);
                var color = new Vector3(0, Renderer.ShaderHueTranslator.SHADER_LAND, 1f);

                if (entity.Ref == selectedEntity.Value.Entity)
                {
                    color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                    color.Y = ShaderHueTranslator.SHADER_HUED;
                }

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
                if (position.Y < cameraBounds.Y)
                    continue;

                ref readonly var artInfo = ref assetsServer.Value.Arts.GetLand(graphic.Ref.Value);
                if (artInfo.Texture == null)
                    continue;

                var depthZ = Isometric.GetDepthZ(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z - 2);
                var color = Vector3.UnitZ;

                if (entity.Ref == selectedEntity.Value.Entity)
                {
                    color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                    color.Y = ShaderHueTranslator.SHADER_HUED;
                }

                selectedEntity.Value.IsPointInLand(entity.Ref, depthZ, mousePos, position);

                batch.Value.Draw(
                    artInfo.Texture,
                    position,
                    artInfo.UV,
                    color,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 1f,
                    effects: SpriteEffects.None,
                    depthZ
                );
            }
        }


        foreach ((
            var entity,
            var worldPos,
            var graphic,
            var hue) in queryStatics)
        {
            ref readonly var tileData = ref fileManager.Value.TileData.StaticData[graphic.Ref.Value];

            if (tileData.IsInternal)
                continue;

            // var maxObjectZ = (int)priorityZ;
            // var height = CalculateObjectHeight(ref maxObjectZ, in tileData);

            var hide = tileData.IsRoof && !backupZInfo.DrawRoof;
            hide |= maxZ.HasValue && worldPos.Ref.Z >= maxZ;
            if (!calculateZ && hide)
            {
                continue;
            }

            var position = Isometric.IsoToScreen(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z);
            position -= center;

            if (position.X < cameraBounds.X || position.X > cameraBounds.Width)
                continue;

            if (position.Y < cameraBounds.Y || position.Y > cameraBounds.Height)
                continue;

            if (!CanBeDrawn(gameCtx.Value.ClientVersion, fileManager.Value.TileData, graphic.Ref.Value))
                continue;

            ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(graphic.Ref.Value);
            if (artInfo.Texture == null)
                continue;

            if (calculateZ)
            {
                var tileDataFlags = tileData.Flags;

                if (worldPos.Ref.Z > playerZ14)
                {
                    if (((ulong)tileDataFlags & 0x204) == 0 && tileDataFlags.HasFlag(TileFlag.Roof))
                    {
                        if (worldPos.Ref.X == playerX && worldPos.Ref.Y == playerY)
                            workingZInfo.Value.IsSameTile = true;
                        else if (worldPos.Ref.X == playerX + 1 && worldPos.Ref.Y == playerY + 1)
                            workingZInfo.Value.IsTileAhead = true;
                    }

                    var max = workingZInfo.Value.MaxZRoof ?? 127;

                    if (max > worldPos.Ref.Z)
                    {
                        if (((ulong)tileDataFlags & 0x204) == 0 && tileDataFlags.HasFlag(TileFlag.Roof))
                        {
                            workingZInfo.Value.MaxZRoof = worldPos.Ref.Z;
                            workingZInfo.Value.DrawRoof = false;
                        }
                    }
                }

                if (worldPos.Ref.X == playerX && worldPos.Ref.Y == playerY)
                {
                    if (worldPos.Ref.Z > playerZ14)
                    {
                        var max = workingZInfo.Value.MaxZ ?? 127;

                        if (max > worldPos.Ref.Z)
                        {
                            if (((ulong)tileDataFlags & 0x20004) == 0 && (!tileDataFlags.HasFlag(TileFlag.Roof) || tileDataFlags.HasFlag(TileFlag.Surface)))
                            {
                                workingZInfo.Value.IsUnderStatic = true;
                                workingZInfo.Value.MaxZ = worldPos.Ref.Z;
                                workingZInfo.Value.DrawRoof = false;
                            }
                        }
                    }
                }
            }

            if (hide)
                continue;

            position.X -= (short)((artInfo.UV.Width >> 1) - 22);
            position.Y -= (short)(artInfo.UV.Height - 44);

            var priorityZ = worldPos.Ref.Z;

            if (tileData.IsBackground)
            {
                priorityZ -= 1;
            }

            if (tileData.Height != 0)
            {
                priorityZ += 1;
            }

            if (tileData.IsWall)
            {
                priorityZ += 2;
            }

            if (tileData.IsMultiMovable)
            {
                priorityZ += 1;
            }

            if (entity.Ref.Has<NormalMulti>())
            {
                priorityZ -= 1;
            }

            var depthZ = Isometric.GetDepthZ(worldPos.Ref.X, worldPos.Ref.Y, priorityZ);
            var color = Renderer.ShaderHueTranslator.GetHueVector(hue.Ref.Value, fileManager.Value.TileData.StaticData[graphic.Ref.Value].IsPartialHue, 1f);

            if (entity.Ref == selectedEntity.Value.Entity)
            {
                color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                color.Y = ShaderHueTranslator.SHADER_HUED;
                color.Z = 1f;
            }

            var p = mousePos - position;
            if (assetsServer.Value.Arts.PixelCheck(graphic.Ref.Value, (int)p.X, (int)p.Y))
                selectedEntity.Value.Set(entity.Ref, depthZ);

            batch.Value.Draw(
                artInfo.Texture,
                position,
                artInfo.UV,
                color,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
                depthZ
            );
        }


        foreach ((
            var entity,
            var pos,
            var graphic,
            var hue,
            var serial,
            var offset,
            var direction,
            var animation,
            var steps) in queryBodyOnly)
        {
            if (maxZ.HasValue && pos.Ref.Z >= maxZ)
                continue;

            var uoHue = hue.Ref.Value;
            var priorityZ = pos.Ref.Z;
            var position = Isometric.IsoToScreen(pos.Ref.X, pos.Ref.Y, pos.Ref.Z);
            position -= center;

            Texture2D texture;
            Rectangle? uv;
            var mirror = false;

            //if (ClassicUO.Game.SerialHelper.IsMobile(serial.Value))
            //{

            //}
            //else
            //{
            //    ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(graphic.Value);

            //    if (fileManager.Value.TileData.StaticData[graphic.Value].IsBackground)
            //    {
            //        priorityZ -= 1;
            //    }

            //    if (fileManager.Value.TileData.StaticData[graphic.Value].Height != 0)
            //    {
            //        priorityZ += 1;
            //    }

            //    if (fileManager.Value.TileData.StaticData[graphic.Value].IsWall)
            //    {
            //        priorityZ += 2;
            //    }

            //    if (fileManager.Value.TileData.StaticData[graphic.Value].IsMultiMovable)
            //    {
            //        priorityZ += 1;
            //    }

            //    texture = artInfo.Texture;
            //    uv = artInfo.UV;
            //    position.X -= (short)((artInfo.UV.Width >> 1) - 22);
            //    position.Y -= (short)(artInfo.UV.Height - 44);
            //}

            priorityZ += 2;
            var dir = direction.IsValid() ? direction.Ref.Value : Direction.North;
            (dir, mirror) = FixDirection(dir);

            byte animAction = 0;
            var animIndex = 0;
            if (animation.IsValid())
            {
                animAction = animation.Ref.Action;
                animIndex = animation.Ref.Index;
                if (direction.IsValid())
                    animation.Ref.Direction = (direction.Ref.Value & (~Direction.Running | Direction.Mask));
            }

            var frames = assetsServer.Value.Animations.GetAnimationFrames
            (
                graphic.Ref.Value,
                animAction,
                (byte)dir,
                out var baseHue,
                out var isUop
            );

            if (uoHue == 0)
                uoHue = baseHue;

            ref readonly var frame = ref frames.IsEmpty ?
                ref SpriteInfo.Empty
                :
                ref frames[animIndex % frames.Length];

            texture = frame.Texture;
            uv = frame.UV;
            position.X += 22;
            position.Y += 22;
            if (mirror)
                position.X -= frame.UV.Width - frame.Center.X;
            else
                position.X -= frame.Center.X;
            position.Y -= frame.UV.Height + frame.Center.Y;

            if (texture == null)
                continue;

            var depthZ = Isometric.GetDepthZ(pos.Ref.X, pos.Ref.Y, priorityZ);
            var color = ShaderHueTranslator.GetHueVector(FixHue(uoHue));
            position += offset.Ref.Value;

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

            if (entity.Ref == selectedEntity.Value.Entity)
            {
                color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                color.Y = ShaderHueTranslator.SHADER_HUED;
                color.Z = 1f;
            }

            if (assetsServer.Value.Animations.PixelCheck(
                graphic.Ref.Value,
                animAction,
                (byte)dir,
                isUop,
                animIndex,
                mirror ? (int)(position.X + uv.Value.Width - mousePos.X) : (int)(mousePos.X - position.X),
                (int)(mousePos.Y - position.Y)
            ))
            {
                selectedEntity.Value.Set(entity.Ref, depthZ);
            }

            // if (!Unsafe.IsNullRef(ref steps) && steps.Count > 0)
            // {
            //     ref var step = ref steps[steps.Count - 1];
            //
            //     if (((Direction)step.Direction & Direction.Mask) is Direction.Down or Direction.South or Direction.East)
            //     {
            //         priorityZ = (sbyte)(step.Z + 2);
            //         depthZ = Isometric.GetDepthZ(step.X, step.Y, priorityZ);
            //     }
            // }
            // else
            // {
            //     depthZ = (direction.Value & Direction.Mask) switch
            //     {
            //         Direction.Down => Isometric.GetDepthZ(pos.X + 1, pos.Y + 1, priorityZ - 1),
            //         Direction.South => Isometric.GetDepthZ(pos.X, pos.Y + 1, priorityZ - 1),
            //         Direction.East => Isometric.GetDepthZ(pos.X + 1, pos.Y, priorityZ -1),
            //         _ => depthZ
            //     };
            // }
            // else if ((direction.Value & Direction.Mask) is Direction.Down or Direction.South or Direction.East)
            // {
            //     depthZ = Isometric.GetDepthZ(pos.X + 1, pos.Y + 1, priorityZ);
            // }

            batch.Value.Draw
            (
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


        foreach ((
            var entity,
            var slots,
            var offset,
            var pos,
            var graphic,
            var direction,
            var steps,
            var animation) in queryEquipmentSlots)
        {
            if (maxZ.HasValue && pos.Ref.Z >= maxZ)
                continue;

            if (!Races.IsHuman(graphic.Ref.Value))
                continue;

            var priorityZ = pos.Ref.Z + 2;
            var depthZ = Isometric.GetDepthZ(pos.Ref.X, pos.Ref.Y, priorityZ);

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

            // if (!Unsafe.IsNullRef(ref steps) && steps.Count > 0)
            // {
            //     ref var step = ref steps[steps.Count - 1];
            //
            //     if (((Direction)step.Direction & Direction.Mask) is Direction.Down or Direction.South or Direction.East)
            //     {
            //         priorityZ = (sbyte)(step.Z + 2);
            //         depthZ = Isometric.GetDepthZ(step.X, step.Y, priorityZ);
            //     }
            // }
            // else // if ((direction.Value & Direction.Mask) is Direction.Down or Direction.South or Direction.East)
            // {
            //     depthZ = (direction.Value & Direction.Mask) switch
            //     {
            //         Direction.Down => Isometric.GetDepthZ(pos.X + 1, pos.Y + 1, priorityZ - 1),
            //         Direction.South => Isometric.GetDepthZ(pos.X, pos.Y + 1, priorityZ - 1),
            //         Direction.East => Isometric.GetDepthZ(pos.X + 1, pos.Y, priorityZ -1),
            //         _ => depthZ
            //     };
            // }

            (var dir, var mirror) = FixDirection(animation.Ref.Direction);

            if (!animation.IsValid())
            {
                continue;
            }

            for (int j = -1; j < Constants.USED_LAYER_COUNT; j++)
            {
                var layer = j == -1 ? Layer.Mount : LayerOrder.UsedLayers[(int)animation.Ref.Direction & 0x7, j];
                var layerEnt = slots.Ref[layer];
                if (!layerEnt.IsValid())
                    continue;

                if (!world.Exists(layerEnt))
                {
                    slots.Ref[layer] = 0;
                    continue;
                }

                byte animAction = animation.Ref.Action;
                var graphicLayer = world.Get<Graphic>(layerEnt).Value;
                var hueLayer = world.Get<Hue>(layerEnt).Value;
                var animId = graphicLayer;
                var offsetY = 0;
                if (layer == Layer.Mount)
                {
                    (animId, offsetY) = Mounts.FixMountGraphic(fileManager.Value.TileData, animId);
                    animAction = animation.Ref.MountAction;
                }
                else if (!IsItemCovered2(world, ref slots.Ref, layer))
                {
                    if (fileManager.Value.TileData.StaticData[graphicLayer].AnimID != 0)
                        animId = fileManager.Value.TileData.StaticData[graphicLayer].AnimID;
                }
                else
                {
                    continue;
                }

                var frames = assetsServer.Value.Animations.GetAnimationFrames
                (
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

                var position = Isometric.IsoToScreen(pos.Ref.X, pos.Ref.Y, pos.Ref.Z);
                position.Y -= offsetY;
                position.X += 22;
                position.Y += 22;
                if (mirror)
                    position.X -= frame.UV.Width - frame.Center.X;
                else
                    position.X -= frame.Center.X;
                position.Y -= frame.UV.Height + frame.Center.Y;

                var color = ShaderHueTranslator.GetHueVector(FixHue(hueLayer != 0 ? hueLayer : baseHue));
                position += offset.Ref.Value;


                if (entity.Ref == selectedEntity.Value.Entity)
                {
                    color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                    color.Y = ShaderHueTranslator.SHADER_HUED;
                    color.Z = 1f;
                }

                if (assetsServer.Value.Animations.PixelCheck(
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

                batch.Value.Draw
                (
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


        batch.Value.SetSampler(null);
        batch.Value.SetStencil(null);
        batch.Value.End();
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

    private static bool IsItemCovered2(World world, ref EquipmentSlots slots, Layer layer)
    {
        switch (layer)
        {
            case Layer.Shoes:
                if (slots[Layer.Legs].IsValid() ||
                    (slots[Layer.Pants].IsValid() && world.Exists(slots[Layer.Pants]) && world.Get<Graphic>(slots[Layer.Pants]).Value == 0x1411))
                {
                    return true;
                }
                else if (slots[Layer.Pants].IsValid() &&
                         world.Exists(slots[Layer.Pants]) && (world.Get<Graphic>(slots[Layer.Pants]).Value is 0x0513 or 0x0514) ||
                         (slots[Layer.Robe].IsValid() && world.Exists(slots[Layer.Robe]) && world.Get<Graphic>(slots[Layer.Robe]).Value is 0x0504))
                {
                    return true;
                }
                break;

            case Layer.Pants:
                if (slots[Layer.Legs].IsValid() ||
                    (slots[Layer.Robe].IsValid() && world.Exists(slots[Layer.Robe]) && world.Get<Graphic>(slots[Layer.Robe]).Value == 0x0504))
                {
                    return true;
                }

                if (slots[Layer.Pants].IsValid() && world.Exists(slots[Layer.Pants]) &&
                    world.Get<Graphic>(slots[Layer.Pants]).Value is 0x01EB or 0x03E5 or 0x03EB)
                {
                    if (slots[Layer.Skirt].IsValid() && world.Exists(slots[Layer.Skirt]) &&
                        world.Get<Graphic>(slots[Layer.Skirt]).Value is not 0x01C7 and not 0x01E4)
                    {
                        return true;
                    }

                    if (slots[Layer.Robe].IsValid() && world.Exists(slots[Layer.Robe]) &&
                        world.Get<Graphic>(slots[Layer.Robe]).Value is not 0x0229 and not (>= 0x04E8 and <= 0x04EB))
                    {
                        return true;
                    }
                }
                break;

            case Layer.Tunic:
                if (slots[Layer.Robe].IsValid() && world.Exists(slots[Layer.Robe]))
                {
                    if (world.Get<Graphic>(slots[Layer.Robe]).Value is not 0 and not 0x9985 and not 0x9986 and not 0xA412)
                    {
                        return true;
                    }
                }
                else if (slots[Layer.Tunic].IsValid() && world.Exists(slots[Layer.Tunic]) &&
                         world.Get<Graphic>(slots[Layer.Tunic]).Value == 0x0238)
                {
                    if (slots[Layer.Robe].IsValid() && world.Exists(slots[Layer.Robe]) &&
                        world.Get<Graphic>(slots[Layer.Robe]).Value is not 0x9985 and not 0x9986 and not 0xA412)
                    {
                        return true;
                    }
                }
                break;

            case Layer.Torso:
                if (slots[Layer.Robe].IsValid() && world.Exists(slots[Layer.Robe]) &&
                    world.Get<Graphic>(slots[Layer.Robe]).Value is not 0 and not 0x9985 and not 0x9986 and not 0xA412 and not 0xA2CA)
                {
                    return true;
                }
                else if (slots[Layer.Tunic].IsValid() && world.Exists(slots[Layer.Tunic]) &&
                         world.Get<Graphic>(slots[Layer.Tunic]).Value is not 0x1541 and not 0x1542)
                {
                    if (slots[Layer.Torso].IsValid() && world.Exists(slots[Layer.Torso]) &&
                        world.Get<Graphic>(slots[Layer.Torso]).Value is 0x782A or 0x782B)
                    {
                        return true;
                    }
                }
                break;

            case Layer.Arms:
                if (slots[Layer.Robe].IsValid() && world.Exists(slots[Layer.Robe]) &&
                    world.Get<Graphic>(slots[Layer.Robe]).Value is not 0 and not 0x9985 and not 0x9986 and not 0xA412)
                {
                    return true;
                }
                break;

            case Layer.Helmet:
            case Layer.Hair:
                if (slots[Layer.Robe].IsValid() && world.Exists(slots[Layer.Robe]))
                {
                    ref var gfx = ref world.Get<Graphic>(slots[Layer.Robe]);

                    if (gfx.Value > 0x3173)
                    {
                        if (gfx.Value is 0x4B9D or 0x7816)
                        {
                            return true;
                        }
                    }
                    else if (gfx.Value <= 0x2687)
                    {
                        if (gfx.Value < 0x2683)
                        {
                            if (gfx.Value is >= 0x204E and <= 0x204F)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else if (gfx.Value is 0x2FB9 or 0x3173)
                    {
                        return true;
                    }
                }
                break;
        }

        return false;
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

    public void Set(ulong entity, float depth)
    {
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
