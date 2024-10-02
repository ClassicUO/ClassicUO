using System;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using static TinyEcs.Defaults;
using World = TinyEcs.World;

namespace ClassicUO.Ecs;

readonly struct WorldRenderingPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddSystem((Res<MouseContext> mouseCtx, Res<KeyboardContext> keyboardCtx, Res<GameContext> gameCtx) =>
        {
            if (mouseCtx.Value.OldState.LeftButton == ButtonState.Pressed && mouseCtx.Value.NewState.LeftButton == ButtonState.Pressed)
            {
                gameCtx.Value.CenterOffset.X += mouseCtx.Value.NewState.X - mouseCtx.Value.OldState.X;
                gameCtx.Value.CenterOffset.Y += mouseCtx.Value.NewState.Y - mouseCtx.Value.OldState.Y;
            }

            if (keyboardCtx.Value.OldState.IsKeyUp(Keys.Space) && keyboardCtx.Value.NewState.IsKeyDown(Keys.Space))
            {
                gameCtx.Value.FreeView = !gameCtx.Value.FreeView;
            }
        }, Stages.FrameStart).RunIf((Res<UoGame> game) => game.Value.IsActive);

        scheduler.AddSystem
        (
            (Res<GameContext> gameCtx, Query<(WorldPosition, ScreenPositionOffset), With<Player>> playerQuery) =>
            {
                playerQuery.Each(
                    (ref WorldPosition position, ref ScreenPositionOffset offset) =>
                    {
                        gameCtx.Value.CenterX = position.X;
                        gameCtx.Value.CenterY = position.Y;
                        gameCtx.Value.CenterZ = position.Z;
                        gameCtx.Value.CenterOffset = offset.Value * -1;
                    });
            },
            threadingType: ThreadingMode.Single
        ).RunIf((Res<GameContext> gameCtx) => !gameCtx.Value.FreeView);

        var renderingFn = Rendering;
        scheduler.AddSystem(renderingFn, Stages.AfterUpdate, ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>())
                 .RunIf((Query<WorldPosition, With<Player>> playerQuery) => playerQuery.Count() > 0);
    }


    void Rendering
    (
        TinyEcs.World world,
        Res<GameContext> gameCtx,
        Res<Renderer.UltimaBatcher2D> batch,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Local<(int lastPosX, int lastPosY)?> lastPos,
        Local<(int? maxZ, int? maxZGround, int? maxZRoof, bool drawRoof)> localZInfo,
        Query<WorldPosition, With<Player>> queryPlayer,
        Query<(WorldPosition, Graphic, Optional<TileStretched>), With<IsTile>> queryTiles,
        Query<(WorldPosition, Graphic, Hue), (Without<IsTile>, Without<MobAnimation>, Without<Pair<ContainedInto, Wildcard>>)> queryStatics,
        Query<(WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Optional<Facing>, Optional<MobAnimation>, Optional<MobileSteps>),
            Without<Pair<ContainedInto, Wildcard>>> queryBodyOnly,
        Query<(EquipmentSlots, ScreenPositionOffset, WorldPosition, Graphic, Facing, Optional<MobileSteps>, Optional<MobAnimation>),
            Without<Pair<ContainedInto, Wildcard>>> queryEquipmentSlots
    )
    {
        ref var playerPos = ref queryPlayer.Single<WorldPosition>();

        int? maxZ = null;
        // if (!lastPos.Value.HasValue || lastPos.Value.Value.lastPosX != playerPos.X || lastPos.Value.Value.lastPosY != playerPos.Y)
        {
            localZInfo.Value.maxZ = null;
            localZInfo.Value.maxZGround = null;
            localZInfo.Value.maxZRoof = null;
            localZInfo.Value.drawRoof = true;
            var playerZ16 = playerPos.Z + 16;
            var playerZ14 = playerPos.Z + 14;
            (var chunkX, var chunkY) = (playerPos.X, playerPos.Y);

            foreach ((var entities, var posSpan, var stretchedSpan) in queryTiles.Iter<WorldPosition, TileStretched>())
            {
                for (var i = 0; i < entities.Length; ++i)
                {
                    ref var pos = ref posSpan[i];
                    ref var stretched = ref stretchedSpan.IsEmpty ? ref Unsafe.NullRef<TileStretched>() : ref stretchedSpan[i];

                    if (Math.Abs(pos.X - chunkX) <= 0 && Math.Abs(pos.Y - chunkY) <= 0)
                    {
                        var tileZ = pos.Z;
                        if (!Unsafe.IsNullRef(ref stretched))
                        {
                            tileZ = stretched.AvgZ;
                        }

                        if (tileZ > playerZ16)
                        {
                            localZInfo.Value.maxZGround = playerZ16;
                        }
                    }
                }
            }

            var isUnderRoof = false;
            var isUnderStatic = false;

            foreach ((var entities, var posSpan, var graphicSpan) in queryStatics.Iter<WorldPosition, Graphic>())
            {
                for (var i = 0; i < entities.Length; ++i)
                {
                    ref var pos = ref posSpan[i];
                    ref var graphic = ref graphicSpan[i];

                    // if (Math.Abs(pos.X - chunkX) <= 0 && Math.Abs(pos.Y - chunkY) <= 0)
                    {
                        var tileDataFlags = fileManager.Value.TileData.StaticData[graphic.Value].Flags;

                        if ((tileDataFlags & (TileFlag.Roof)) != 0)
                        {
                            if (pos.Z > playerZ16)
                            {
                                if (Math.Abs(pos.X - chunkX) <= 0 && Math.Abs(pos.Y - chunkY) <= 0)
                                    isUnderRoof = true;

                                var max = localZInfo.Value.maxZRoof ?? 127;
                                if (max > pos.Z)
                                {
                                    localZInfo.Value.maxZRoof = pos.Z;
                                    localZInfo.Value.drawRoof = false;
                                }
                            }
                        }

                        if ((tileDataFlags & (TileFlag.Surface | TileFlag.Transparent)) != 0)
                        {
                            if (pos.Z > playerZ16)
                            {
                                if (Math.Abs(pos.X - chunkX) <= 0 && Math.Abs(pos.Y - chunkY) <= 0)
                                {
                                    // localZInfo.Value.maxZ = pos.Z;
                                    isUnderStatic = true;

                                    var max = localZInfo.Value.maxZ ?? 127;
                                    if (max > pos.Z)
                                    {
                                        localZInfo.Value.maxZ = pos.Z;
                                        localZInfo.Value.drawRoof = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

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

            if (maxZ.HasValue && maxZ < playerZ16)
            {
                maxZ = playerZ16;
            }
        }

        var center = Isometric.IsoToScreen(gameCtx.Value.CenterX, gameCtx.Value.CenterY, gameCtx.Value.CenterZ);
        center.X -= batch.Value.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f;
        center.Y -= batch.Value.GraphicsDevice.PresentationParameters.BackBufferHeight / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.Value.CenterOffset;


        var matrix = Matrix.Identity;
        //matrix = Matrix.CreateScale(0.45f);

        batch.Value.Begin(null, matrix);
        batch.Value.SetBrightlight(1.7f);
        batch.Value.SetSampler(SamplerState.PointClamp);
        batch.Value.SetStencil(DepthStencilState.Default);

        queryTiles.Each((ref WorldPosition worldPos, ref Graphic graphic, ref TileStretched stretched) =>
        {
            if (localZInfo.Value.maxZGround.HasValue && worldPos.Z > localZInfo.Value.maxZGround)
                return;

            var isStretched = !Unsafe.IsNullRef(ref stretched);

            if (isStretched)
            {
                ref readonly var textmapInfo = ref assetsServer.Value.Texmaps.GetTexmap(fileManager.Value.TileData.LandData[graphic.Value].TexID);
                if (textmapInfo.Texture == null)
                    return;

                var position = Isometric.IsoToScreen(worldPos.X, worldPos.Y, worldPos.Z);
                position.Y += worldPos.Z << 2;
                var depthZ = Isometric.GetDepthZ(worldPos.X, worldPos.Y, stretched.AvgZ - 2);
                var color = new Vector3(0, Renderer.ShaderHueTranslator.SHADER_LAND, 1f);

                batch.Value.DrawStretchedLand(
                    textmapInfo.Texture,
                    position - center,
                    textmapInfo.UV,
                    ref stretched.Offset,
                    ref stretched.NormalTop,
                    ref stretched.NormalRight,
                    ref stretched.NormalLeft,
                    ref stretched.NormalBottom,
                    color,
                    depthZ
                );
            }
            else
            {
                ref readonly var artInfo = ref assetsServer.Value.Arts.GetLand(graphic.Value);
                if (artInfo.Texture == null)
                    return;

                var position = Isometric.IsoToScreen(worldPos.X, worldPos.Y, worldPos.Z);
                var depthZ = Isometric.GetDepthZ(worldPos.X, worldPos.Y, worldPos.Z - 2);
                var color = Vector3.UnitZ;

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
        });

        queryStatics.Each((ref WorldPosition worldPos, ref Graphic graphic, ref Hue hue) =>
        {
            if (maxZ.HasValue && worldPos.Z >= maxZ)
                return;

            if (fileManager.Value.TileData.StaticData[graphic.Value].IsRoof && !localZInfo.Value.drawRoof)
                return;

            ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(graphic.Value);
            if (artInfo.Texture == null)
                return;

            var priorityZ = worldPos.Z;

            if (fileManager.Value.TileData.StaticData[graphic.Value].IsBackground)
            {
                priorityZ -= 1;
            }

            if (fileManager.Value.TileData.StaticData[graphic.Value].Height != 0)
            {
                priorityZ += 1;
            }

            if (fileManager.Value.TileData.StaticData[graphic.Value].IsWall)
            {
                priorityZ += 2;
            }

            if (fileManager.Value.TileData.StaticData[graphic.Value].IsMultiMovable)
            {
                priorityZ += 1;
            }

            var position = Isometric.IsoToScreen(worldPos.X, worldPos.Y, worldPos.Z);
            position.X -= (short)((artInfo.UV.Width >> 1) - 22);
            position.Y -= (short)(artInfo.UV.Height - 44);
            var depthZ = Isometric.GetDepthZ(worldPos.X, worldPos.Y, priorityZ);
            var color = Renderer.ShaderHueTranslator.GetHueVector(hue.Value, fileManager.Value.TileData.StaticData[graphic.Value].IsPartialHue, 1f);

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
        });

        queryBodyOnly.Each((
            ref WorldPosition pos,
            ref Graphic graphic,
            ref Hue hue,
            ref NetworkSerial serial,
            ref ScreenPositionOffset offset,
            ref Facing direction,
            ref MobAnimation animation,
            ref MobileSteps steps
        ) =>
            {
                var uoHue = hue.Value;
                var priorityZ = pos.Z;
                var position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z);
                Texture2D texture;
                Rectangle? uv;
                var mirror = false;

                if (ClassicUO.Game.SerialHelper.IsMobile(serial.Value))
                {
                    priorityZ += 2;
                    var dir = Unsafe.IsNullRef(ref direction) ? Direction.North : direction.Value;
                    (dir, mirror) = FixDirection(dir);

                    byte animAction = 0;
                    var animIndex = 0;
                    if (!Unsafe.IsNullRef(ref animation))
                    {
                        animAction = animation.Action;
                        animIndex = animation.Index;
                        if (!Unsafe.IsNullRef(ref direction))
                            animation.Direction = (direction.Value & (~Direction.Running | Direction.Mask));
                    }

                    var frames = assetsServer.Value.Animations.GetAnimationFrames
                    (
                        graphic.Value,
                        animAction,
                        (byte) dir,
                        out var baseHue,
                        out var _
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
                }
                else
                {
                    ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(graphic.Value);

                    if (fileManager.Value.TileData.StaticData[graphic.Value].IsBackground)
                    {
                        priorityZ -= 1;
                    }

                    if (fileManager.Value.TileData.StaticData[graphic.Value].Height != 0)
                    {
                        priorityZ += 1;
                    }

                    if (fileManager.Value.TileData.StaticData[graphic.Value].IsWall)
                    {
                        priorityZ += 2;
                    }

                    if (fileManager.Value.TileData.StaticData[graphic.Value].IsMultiMovable)
                    {
                        priorityZ += 1;
                    }

                    texture = artInfo.Texture;
                    uv = artInfo.UV;
                    position.X -= (short)((artInfo.UV.Width >> 1) - 22);
                    position.Y -= (short)(artInfo.UV.Height - 44);
                }

                if (texture == null)
                    return;

                var depthZ = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ);
                var color = ShaderHueTranslator.GetHueVector(FixHue(uoHue));
                position += offset.Value;

                if (offset.Value.X > 0 && offset.Value.Y > 0)
                {
                    depthZ = Isometric.GetDepthZ(pos.X + 1, pos.Y, priorityZ);
                }
                else if (offset.Value.X == 0 && offset.Value.Y > 0)
                {
                    depthZ = Isometric.GetDepthZ(pos.X + 1, pos.Y + 1, priorityZ);
                }
                else if (offset.Value.X < 0 && offset.Value.Y > 0)
                {
                    depthZ = Isometric.GetDepthZ(pos.X, pos.Y + 1, priorityZ);
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
            });

            queryEquipmentSlots.Each(
         (
                ref EquipmentSlots slots,
                ref ScreenPositionOffset offset,
                ref WorldPosition pos,
                ref Graphic graphic,
                ref Facing direction,
                ref MobileSteps steps,
                ref MobAnimation animation
            ) =>
            {
                if (!Races.IsHuman(graphic.Value))
                    return;

                var priorityZ = pos.Z + 2;
                var depthZ = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ);

                if (offset.Value.X > 0 && offset.Value.Y > 0)
                {
                    depthZ = Isometric.GetDepthZ(pos.X + 1, pos.Y, priorityZ);
                }
                else if (offset.Value.X == 0 && offset.Value.Y > 0)
                {
                    depthZ = Isometric.GetDepthZ(pos.X + 1, pos.Y + 1, priorityZ);
                }
                else if (offset.Value.X < 0 && offset.Value.Y > 0)
                {
                    depthZ = Isometric.GetDepthZ(pos.X, pos.Y + 1, priorityZ);
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

                (var dir, var mirror) = FixDirection(animation.Direction);

                if (!Unsafe.IsNullRef(ref animation))
                {
                    for (int i = -1; i < Constants.USED_LAYER_COUNT; i++)
                    {
                        var layer = i == -1 ? Layer.Mount : LayerOrder.UsedLayers[(int)animation.Direction & 0x7, i];
                        var layerEnt = slots[layer];
                        if (!layerEnt.IsValid())
                            continue;

                        if (!world.Exists(layerEnt))
                        {
                            slots[layer] = 0;
                            continue;
                        }

                        byte animAction = animation.Action;
                        var graphicLayer = world.Get<Graphic>(layerEnt).Value;
                        var hueLayer = world.Get<Hue>(layerEnt).Value;
                        var animId = graphicLayer;
                        if (layer == Layer.Mount)
                        {
                            animId = Mounts.FixMountGraphic(fileManager.Value.TileData, animId);
                            animAction = animation.MountAction;
                        }
                        else if (!IsItemCovered2(world, ref slots, layer))
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
                            (byte) dir,
                            out var baseHue,
                            out var _
                        );

                        ref readonly var frame = ref frames.IsEmpty ?
                            ref SpriteInfo.Empty
                            :
                            ref frames[animation.Index % frames.Length];

                        if (frame.Texture == null)
                            continue;

                        var position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z);
                        position.X += 22;
                        position.Y += 22;
                        if (mirror)
                            position.X -= frame.UV.Width - frame.Center.X;
                        else
                            position.X -= frame.Center.X;
                        position.Y -= frame.UV.Height + frame.Center.Y;

                        var color = ShaderHueTranslator.GetHueVector(FixHue(hueLayer != 0 ? hueLayer : baseHue));
                        position += offset.Value;

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
                            depthZ
                        );
                    }
                }
            });

            batch.Value.SetSampler(null);
            batch.Value.SetStencil(null);
            batch.Value.End();
    }


    static ushort FixHue(ushort hue)
    {
        var fixedColor = (ushort) (hue & 0x3FFF);

        if (fixedColor != 0)
        {
            if (fixedColor >= 0x0BB8)
            {
                fixedColor = 1;
            }

            fixedColor |= (ushort) (hue & 0xC000);
        }
        else
        {
            fixedColor = (ushort) (hue & 0x8000);
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
