using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using static TinyEcs.Defaults;

namespace ClassicUO.Ecs;

readonly struct RenderingPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddSystem(static (
            TinyEcs.World world,
            Query<Graphic,
                (With<NetworkSerial>, Without<Pair<ContainedInto, Wildcard>>, With<Pair<EquippedItem, Wildcard>>)> queryEquip
        ) => {
            queryEquip.Each((EntityView ent, ref Graphic graphic) =>
            {
                // sync equipment position with the parent
                var id = ent.Target<EquippedItem>();
                var parent = world.Entity(id);

                if (parent.Has<WorldPosition>())
                    ent.Set(parent.Get<WorldPosition>());

                if (parent.Has<MobAnimation>())
                    ent.Set(parent.Get<MobAnimation>());
            });
        }, threadingType: ThreadingMode.Single);

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


        var beginFn = BeginRendering;
        var renderingFn = Rendering;
        var endFn = EndRendering;

        scheduler.AddSystem(beginFn, Stages.AfterUpdate, ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());
        scheduler.AddSystem(renderingFn, Stages.AfterUpdate, ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());
        scheduler.AddSystem(endFn, Stages.AfterUpdate, ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());
    }

    void BeginRendering(Res<Renderer.UltimaBatcher2D> batch)
    {
        var sb = batch.Value;
        var matrix = Matrix.Identity;
        //matrix = Matrix.CreateScale(0.45f);

        sb.GraphicsDevice.Clear(Color.Black);

        sb.Begin(null, matrix);
        sb.SetBrightlight(1.7f);
        sb.SetSampler(SamplerState.PointClamp);
        sb.SetStencil(DepthStencilState.Default);
    }

    void EndRendering(Res<Renderer.UltimaBatcher2D> batch)
    {
        var sb = batch.Value;
        sb.SetSampler(null);
        sb.SetStencil(null);
        sb.End();
        sb.GraphicsDevice.Present();
    }

    void Rendering
    (
        TinyEcs.World world,
        Res<GameContext> gameCtx,
        Res<Renderer.UltimaBatcher2D> batch,
        Res<AssetsServer> assetsServer,
        Res<UOFileManager> fileManager,
        Local<Dictionary<Layer, EntityView>> dict,
        Query<(WorldPosition, Graphic, Optional<TileStretched>), With<IsTile>> queryTiles,
        Query<(WorldPosition, Graphic, Hue), (Without<IsTile>, Without<MobAnimation>, Without<Pair<ContainedInto, Wildcard>>)> queryStatics,
        Query<(WorldPosition, Graphic, Hue, NetworkSerial, ScreenPositionOffset, Optional<Facing>, Optional<MobAnimation>),
            (Without<Pair<ContainedInto, Wildcard>>, Without<Pair<EquippedItem, Wildcard>>)> queryBodyOnly,
        Query<(WorldPosition, Graphic, Hue, NetworkSerial, Optional<MobAnimation>),
            (Without<Pair<ContainedInto, Wildcard>>, With<Pair<EquippedItem, Wildcard>>)> queryEquipment
    )
    {
        var center = Isometric.IsoToScreen(gameCtx.Value.CenterX, gameCtx.Value.CenterY, gameCtx.Value.CenterZ);
        center.X -= batch.Value.GraphicsDevice.PresentationParameters.BackBufferWidth / 2f;
        center.Y -= batch.Value.GraphicsDevice.PresentationParameters.BackBufferHeight / 2f;
        center -= gameCtx.Value.CenterOffset;

        queryTiles.Each(
            (ref WorldPosition worldPos, ref Graphic graphic, ref TileStretched stretched) =>
            {
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

        queryStatics.Each(
            (ref WorldPosition worldPos, ref Graphic graphic, ref Hue hue) =>
            {
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
            ref MobAnimation animation
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


            queryEquipment.Each((
                EntityView ent,
                ref WorldPosition pos,
                ref Graphic graphic,
                ref Hue hue,
                ref NetworkSerial serial,
                ref MobAnimation animation
            ) =>
            {
                var priorityZ = pos.Z + 2;
                var position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z);

                var animId = graphic.Value;
                byte animAction = 0;
                var animIndex = 0;
                if (!Unsafe.IsNullRef(ref animation))
                {
                    animAction = animation.Action;
                    animIndex = animation.Index;
                }

                var act = ent.Target<EquippedItem>();
                if (!Races.IsHuman(world.Get<Graphic>(act).Value))
                {
                    return;
                }

                ref var equip = ref ent.Get<EquippedItem>(act);
                var orderKey = 0;
                if (equip.Layer == Layer.Mount && !Unsafe.IsNullRef(ref animation))
                {
                    animId = Mounts.FixMountGraphic(fileManager.Value.TileData, animId);
                    animAction = animation.MountAction;
                }
                else if (!Unsafe.IsNullRef(ref animation) && _layerOrders[(int)animation.Direction & 7].TryGetValue(equip.Layer, out orderKey) &&
                    !IsItemCovered(dict.Value ??= new Dictionary<Layer, EntityView>(), world, act, equip.Layer))
                {
                    if (fileManager.Value.TileData.StaticData[graphic.Value].AnimID != 0)
                        animId = fileManager.Value.TileData.StaticData[graphic.Value].AnimID;
                }
                else
                {
                    return;
                }

                (var dir, var mirror) = FixDirection(animation.Direction);

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
                    ref frames[animIndex % frames.Length];

                if (frame.Texture == null)
                    return;

                position.X += 22;
                position.Y += 22;
                if (mirror)
                    position.X -= frame.UV.Width - frame.Center.X;
                else
                    position.X -= frame.Center.X;
                position.Y -= frame.UV.Height + frame.Center.Y;

                // TODO: priority Z based on layer ordering
                var depthZ = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ) + (orderKey * 0.01f);
                var color = ShaderHueTranslator.GetHueVector(FixHue(hue.Value != 0 ? hue.Value : baseHue));
                position += world.Get<ScreenPositionOffset>(act).Value;

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
            });
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

    static RenderingPlugin()
    {
        var usedLayers = ClassicUO.Game.Data.LayerOrder.UsedLayers;

        _layerOrders = new Dictionary<Layer, int>[usedLayers.GetLength(0)];

        for (var i = 0; i < usedLayers.GetLength(0); ++i)
        {
            var dict = new Dictionary<Layer, int>();
            for (var k = 0; k < usedLayers.GetLength(1); ++k)
            {
                var layer = usedLayers[i, k];
                dict.Add(layer, k);
            }
            _layerOrders[i] = dict;
        }
    }

    private static readonly Dictionary<Layer, int>[] _layerOrders;

    static bool IsItemCovered(Dictionary<Layer, EntityView> dict, TinyEcs.World world, ulong parent, Layer layer)
    {
        dict.Clear();
        var term0 = new QueryTerm(IDOp.Pair(world.Entity<EquippedItem>(), parent), TermOp.With);
        var term1 = new QueryTerm(world.Entity<NetworkSerial>(), TermOp.DataAccess);

        var query = world.QueryRaw(term0, term1);
        query.Each((EntityView ent, ref NetworkSerial serial) =>
        {
            ref var equip = ref ent.Get<EquippedItem>(parent);
            dict[equip.Layer] = ent;
        });

        switch (layer)
        {
            case Layer.Shoes:
                if (dict.TryGetValue(Layer.Legs, out var legs) ||
                    (dict.TryGetValue(Layer.Pants, out var pants) && pants.Get<Graphic>().Value == 0x1411))
                {
                    return true;
                }
                else if (pants.ID.IsValid() && (pants.Get<Graphic>().Value is 0x0513 or 0x0514) ||
                    (dict.TryGetValue(Layer.Robe, out var robe) && robe.Get<Graphic>().Value is 0x0504))
                {
                    return true;
                }
                break;

            case Layer.Pants:
                dict.TryGetValue(Layer.Pants, out pants);
                if (dict.TryGetValue(Layer.Legs, out legs) ||
                    (dict.TryGetValue(Layer.Robe, out var robe1) &&
                        robe1.Get<Graphic>().Value == 0x0504))
                {
                    return true;
                }

                if (pants.ID.IsValid() && pants.Get<Graphic>().Value is 0x01EB or 0x03E5 or 0x03EB)
                {
                    if (dict.TryGetValue(Layer.Skirt, out var skirt) && skirt.Get<Graphic>().Value is not 0x01C7 and not 0x01E4)
                    {
                        return true;
                    }

                    if (robe1.ID.IsValid() && robe1.Get<Graphic>().Value is not 0x0229 and not (>= 0x04E8 and <= 0x04EB))
                    {
                        return true;
                    }
                }
                break;

            case Layer.Tunic:
                if (dict.TryGetValue(Layer.Robe, out var robe2))
                {
                    if (robe2.Get<Graphic>().Value is not 0 and not 0x9985 and not 0x9986 and not 0xA412)
                    {
                        return true;
                    }
                }
                else if (dict.TryGetValue(Layer.Tunic, out var tunic) && tunic.Get<Graphic>().Value == 0x0238)
                {
                    if (robe2.ID.IsValid() && robe2.Get<Graphic>().Value is not 0x9985 and not 0x9986 and not 0xA412)
                    {
                        return true;
                    }
                }
                break;

            case Layer.Torso:
                if (dict.TryGetValue(Layer.Robe, out var robe3)
                    && robe3.Get<Graphic>().Value is not 0 and not 0x9985 and not 0x9986 and not 0xA412 and not 0xA2CA)
                {
                    return true;
                }
                else if (dict.TryGetValue(Layer.Tunic, out var tunic) && tunic.Get<Graphic>().Value is not 0x1541 and not 0x1542)
                {
                    if (dict.TryGetValue(Layer.Torso, out var torso) && torso.Get<Graphic>().Value is 0x782A or 0x782B)
                    {
                        return true;
                    }
                }
                break;

            case Layer.Arms:
                if (dict.TryGetValue(Layer.Robe, out var robe4) &&
                    robe4.Get<Graphic>().Value is not 0 and not 0x9985 and not 0x9986 and not 0xA412)
                {
                    return true;
                }
                break;

            case Layer.Helmet:
            case Layer.Hair:
                if (dict.TryGetValue(Layer.Robe, out var robe5))
                {
                    ref var gfx = ref robe5.Get<Graphic>();

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
