using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Runtime.CompilerServices;
using TinyEcs;
using World = TinyEcs.World;

namespace ClassicUO.Ecs;

readonly struct WorldRenderingPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new SelectedEntity());

        scheduler.OnFrameStart((Res<MouseContext> mouseCtx, Res<KeyboardContext> keyboardCtx, Res<GameContext> gameCtx, Res<Camera> camera) =>
        {
            if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
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

        var renderingFn = Rendering;
        scheduler.OnAfterUpdate(renderingFn, ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>())
                 .RunIf((SchedulerState state) => state.InState(GameState.GameScreen))
                 .RunIf((Query<Data<WorldPosition>, With<Player>> playerQuery) => playerQuery.Count() > 0);
    }


    void Rendering
    (
        TinyEcs.World world,
        Res<SelectedEntity> selectedEntity,
        Res<GameContext> gameCtx,
        Res<Renderer.UltimaBatcher2D> batch,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Res<Camera> camera,
        Local<(int lastPosX, int lastPosY, int lastPosZ)?> lastPos,
        Local<(int? maxZ, int? maxZGround, int? maxZRoof, bool drawRoof)> localZInfo,
        Query<Data<WorldPosition>, With<Player>> queryPlayer,
        Query<Data<WorldPosition, Graphic, TileStretched>, Filter<With<IsTile>, Optional<TileStretched>>> queryTiles,
        Query<Data<WorldPosition, Graphic, Hue>, Filter<Without<IsTile>, Without<MobAnimation>, Without<ContainedInto>>> queryStatics,
        Query<Data<WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Facing, MobAnimation, MobileSteps>,
            Filter<Without<ContainedInto>, Optional<Facing>, Optional<MobAnimation>, Optional<MobileSteps>>> queryBodyOnly,
        Query<Data<EquipmentSlots, ScreenPositionOffset, WorldPosition, Graphic, Facing, MobileSteps, MobAnimation>,
            Filter<Without<ContainedInto>, Optional<MobileSteps>, Optional<MobAnimation>>> queryEquipmentSlots
    )
    {
        (_, var playerPos) = queryPlayer.Single();
        (var playerX, var playerY, var playerZ) = playerPos.Ref;

        int? maxZ = null;
        if (!lastPos.Value.HasValue ||
            lastPos.Value.Value.lastPosX != playerX ||
            lastPos.Value.Value.lastPosY != playerY ||
            lastPos.Value.Value.lastPosZ != playerZ)
        {
            localZInfo.Value.maxZ = null;
            localZInfo.Value.maxZGround = null;
            localZInfo.Value.maxZRoof = null;
            localZInfo.Value.drawRoof = true;
            var playerZ16 = playerZ + 16;
            var playerZ14 = playerZ + 14;

            foreach ((var pos, var _, var stretched) in queryTiles)
            {
                if (pos.Ref.X == playerX && pos.Ref.Y == playerY)
                {
                    var tileZ = pos.Ref.Z;
                    if (!Unsafe.IsNullRef(ref stretched.Ref))
                    {
                        tileZ = stretched.Ref.AvgZ;
                    }

                    if (tileZ > playerZ16)
                    {
                        localZInfo.Value.maxZGround = playerZ16;
                    }
                }
            }

            var isUnderStatic = false;
            (var isSameTile, var isTileAhead) = (false, false);

            foreach ((var pos, var graphic, var _) in queryStatics)
            {
                var tileDataFlags = fileManager.Value.TileData.StaticData[graphic.Ref.Value].Flags;

                if (pos.Ref.Z > playerZ14)
                {
                    if (((ulong)tileDataFlags & 0x204) == 0 && tileDataFlags.HasFlag(TileFlag.Roof))
                    {
                        if (pos.Ref.X == playerX && pos.Ref.Y == playerY)
                            isSameTile = true;
                        else if (pos.Ref.X == playerX + 1 && pos.Ref.Y == playerY + 1)
                            isTileAhead = true;
                    }

                    var max = localZInfo.Value.maxZRoof ?? 127;

                    if (max > pos.Ref.Z)
                    {
                        if (((ulong)tileDataFlags & 0x204) == 0 && tileDataFlags.HasFlag(TileFlag.Roof))
                        {
                            localZInfo.Value.maxZRoof = pos.Ref.Z;
                            localZInfo.Value.drawRoof = false;
                        }
                    }
                }

                if (pos.Ref.X == playerX && pos.Ref.Y == playerY)
                {
                    if (pos.Ref.Z > playerZ14)
                    {
                        var max = localZInfo.Value.maxZ ?? 127;

                        if (max > pos.Ref.Z)
                        {
                            if (((ulong)tileDataFlags & 0x20004) == 0 && (!tileDataFlags.HasFlag(TileFlag.Roof) || tileDataFlags.HasFlag(TileFlag.Surface)))
                            {
                                isUnderStatic = true;
                                localZInfo.Value.maxZ = pos.Ref.Z;
                                localZInfo.Value.drawRoof = false;
                            }
                        }
                    }
                }
            }


            var isUnderRoof = isSameTile && isTileAhead;

            if (isUnderStatic && isUnderRoof)
            {
                if (localZInfo.Value.maxZRoof < localZInfo.Value.maxZ)
                    maxZ = localZInfo.Value.maxZRoof;
                else
                    maxZ = localZInfo.Value.maxZ;
            }
            else if (isUnderStatic)
            {
                maxZ = localZInfo.Value.maxZ;
            }
            else if (isUnderRoof)
            {
                maxZ = localZInfo.Value.maxZRoof;
            }
            else
            {
                localZInfo.Value.drawRoof = true;
            }

            if (localZInfo.Value.maxZGround.HasValue && localZInfo.Value.maxZGround < maxZ)
            {
                maxZ = localZInfo.Value.maxZGround.Value;
            }

            if (maxZ.HasValue && maxZ < playerZ16)
            {
                maxZ = playerZ16;
            }
        }

        var center = Isometric.IsoToScreen(gameCtx.Value.CenterX, gameCtx.Value.CenterY, gameCtx.Value.CenterZ);
        center.X -= camera.Value.Bounds.Width / 2f;
        center.Y -= camera.Value.Bounds.Height / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.Value.CenterOffset;


        var viewportBackup = batch.Value.GraphicsDevice.Viewport;
        var cameraViewport = camera.Value.GetViewport();
        var matrix = camera.Value.ViewTransformMatrix;

        batch.Value.GraphicsDevice.Viewport = cameraViewport;
        batch.Value.Begin(null, matrix);
        batch.Value.SetBrightlight(1.7f);
        batch.Value.SetSampler(SamplerState.PointClamp);
        batch.Value.SetStencil(DepthStencilState.Default);

        var mousePos = camera.Value.MouseToWorldPosition2();
        // var mousePos = camera.Value.ScreenToWorld(mouseContext.Value.Position);
        selectedEntity.Value.Clear();

        foreach ((var entity, var worldPos, var graphic, var stretched) in queryTiles)
        {
            if (localZInfo.Value.maxZGround.HasValue && worldPos.Ref.Z > localZInfo.Value.maxZGround)
                continue;

            var isStretched = !Unsafe.IsNullRef(ref stretched.Ref);

            if (isStretched)
            {
                ref readonly var textmapInfo = ref assetsServer.Value.Texmaps.GetTexmap(fileManager.Value.TileData.LandData[graphic.Ref.Value].TexID);
                if (textmapInfo.Texture == null)
                    continue;

                var position = Isometric.IsoToScreen(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z);
                position.Y += worldPos.Ref.Z << 2;
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
                    position - center);

                batch.Value.DrawStretchedLand(
                    textmapInfo.Texture,
                    position - center,
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
                ref readonly var artInfo = ref assetsServer.Value.Arts.GetLand(graphic.Ref.Value);
                if (artInfo.Texture == null)
                    continue;

                var position = Isometric.IsoToScreen(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z);
                var depthZ = Isometric.GetDepthZ(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z - 2);
                var color = Vector3.UnitZ;

                if (entity.Ref == selectedEntity.Value.Entity)
                {
                    color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                    color.Y = ShaderHueTranslator.SHADER_HUED;
                }

                selectedEntity.Value.IsPointInLand(entity.Ref, depthZ, mousePos, position - center);

                batch.Value.Draw(
                    artInfo.Texture,
                    position - center,
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


        foreach ((var entity, var worldPos, var graphic, var hue) in queryStatics)
        {
            if (maxZ.HasValue && worldPos.Ref.Z >= maxZ)
                continue;

            if (fileManager.Value.TileData.StaticData[graphic.Ref.Value].IsRoof && !localZInfo.Value.drawRoof)
                continue;

            ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(graphic.Ref.Value);
            if (artInfo.Texture == null)
                continue;

            var priorityZ = worldPos.Ref.Z;

            if (fileManager.Value.TileData.StaticData[graphic.Ref.Value].IsBackground)
            {
                priorityZ -= 1;
            }

            if (fileManager.Value.TileData.StaticData[graphic.Ref.Value].Height != 0)
            {
                priorityZ += 1;
            }

            if (fileManager.Value.TileData.StaticData[graphic.Ref.Value].IsWall)
            {
                priorityZ += 2;
            }

            if (fileManager.Value.TileData.StaticData[graphic.Ref.Value].IsMultiMovable)
            {
                priorityZ += 1;
            }

            var position = Isometric.IsoToScreen(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z);
            position.X -= (short)((artInfo.UV.Width >> 1) - 22);
            position.Y -= (short)(artInfo.UV.Height - 44);
            var depthZ = Isometric.GetDepthZ(worldPos.Ref.X, worldPos.Ref.Y, priorityZ);
            var color = Renderer.ShaderHueTranslator.GetHueVector(hue.Ref.Value, fileManager.Value.TileData.StaticData[graphic.Ref.Value].IsPartialHue, 1f);

            if (entity.Ref == selectedEntity.Value.Entity)
            {
                color.X = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE - 1;
                color.Y = ShaderHueTranslator.SHADER_HUED;
                color.Z = 1f;
            }

            var p = mousePos - (position - center);
            if (assetsServer.Value.Arts.PixelCheck(graphic.Ref.Value, (int)p.X, (int)p.Y))
                selectedEntity.Value.Set(entity.Ref, depthZ);

            batch.Value.Draw(
                artInfo.Texture,
                position - center,
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
            var dir = Unsafe.IsNullRef(ref direction.Ref) ? Direction.North : direction.Ref.Value;
            (dir, mirror) = FixDirection(dir);

            byte animAction = 0;
            var animIndex = 0;
            if (!Unsafe.IsNullRef(ref animation.Ref))
            {
                animAction = animation.Ref.Action;
                animIndex = animation.Ref.Index;
                if (!Unsafe.IsNullRef(ref direction.Ref))
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
                mirror ? (int)((position.X - center.X) + uv.Value.Width - mousePos.X) : (int)(mousePos.X - (position.X - center.X)),
                (int)(mousePos.Y - (position.Y - center.Y))
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
                position - center,
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

            if (!Unsafe.IsNullRef(ref animation.Ref))
            {
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
        }

        batch.Value.SetSampler(null);
        batch.Value.SetStencil(null);
        batch.Value.End();

        batch.Value.GraphicsDevice.Viewport = viewportBackup;
    }


    static ushort FixHue(ushort hue)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static (Direction, bool) FixDirection(Direction dir)
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

    static bool IsItemCovered2(World world, ref EquipmentSlots slots, Layer layer)
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
