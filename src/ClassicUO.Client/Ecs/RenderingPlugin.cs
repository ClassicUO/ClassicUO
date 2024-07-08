using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration;
using ClassicUO.Ecs.NetworkPlugins;
using ClassicUO.Game;
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
            Res<NetworkEntitiesMap> entitiesMap,
            Query<Graphic,
                (With<NetworkSerial>, Without<Relation<ContainedInto, Wildcard>>, With<Relation<EquippedItem, Wildcard>>)> queryEquip,
            Query<NetworkSerial, (Without<Renderable>, Without<Relation<ContainedInto, Wildcard>>)> queryAddRender,
            TinyEcs.World world
        ) => {
            queryEquip.Each((EntityView ent, ref Graphic graphic) =>
            {
                // sync equipment position with the parent
                var id = ent.Target<EquippedItem>();
                var parent = world.Entity(id);

                if (parent.Has<WorldPosition>())
                    ent.Set(parent.Get<WorldPosition>());

                // if (parent.Has<Facing>())
                //     ent.Set(parent.Get<Facing>());

                if (parent.Has<MobAnimation>())
                    ent.Set(parent.Get<MobAnimation>());
            });

            queryAddRender.Each((EntityView ent, ref NetworkSerial serial) =>
            {
                ent.Set(new Renderable());
            });
        });

        scheduler.AddSystem(static (
            Query<(WorldPosition, Graphic, Hue, Renderable, NetworkSerial, Optional<Facing>, Optional<MobAnimation>),
                (Without<Relation<ContainedInto, Wildcard>>, Without<Relation<EquippedItem, Wildcard>>)> query,
            Query<(WorldPosition, Graphic, Hue, Renderable, NetworkSerial, Optional<MobAnimation>),
                (Without<Relation<ContainedInto, Wildcard>>, With<Relation<EquippedItem, Wildcard>>)> queryEquip,
            Res<AssetsServer> assetsServer,
            Res<Assets.TileDataLoader> tiledataLoader,
            TinyEcs.World world,
            Local<Dictionary<Layer, EntityView>> dict
        ) => {
            query.Each((EntityView ent, ref WorldPosition pos, ref Graphic graphic, ref Hue hue,
                ref Renderable renderable, ref NetworkSerial serial,
                ref Facing direction, ref MobAnimation animation) =>
            {
                var uoHue = hue.Value;
                var priorityZ = pos.Z;
                renderable.Position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z);

                if (ClassicUO.Game.SerialHelper.IsMobile(serial.Value))
                {
                    priorityZ += 2;
                    var dir = Unsafe.IsNullRef(ref direction) ? Direction.North : direction.Value;
                    (dir, var mirror) = FixDirection(dir);

                    byte animAction = 0;
                    var animIndex = 0;
                    if (!Unsafe.IsNullRef(ref animation))
                    {
                        animAction = animation.Action;
                        animIndex = animation.Index;
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

                    renderable.Texture = frame.Texture;
                    renderable.UV = frame.UV;
                    renderable.Flip = mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    renderable.Position.X += 22;
                    renderable.Position.Y += 22;
                    if (mirror)
                        renderable.Position.X -= frame.UV.Width - frame.Center.X;
                    else
                        renderable.Position.X -= frame.Center.X;
                    renderable.Position.Y -= frame.UV.Height + frame.Center.Y;
                }
                else
                {
                    ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(graphic.Value);

                    if (tiledataLoader.Value.StaticData[graphic.Value].IsBackground)
                    {
                        priorityZ -= 1;
                    }

                    if (tiledataLoader.Value.StaticData[graphic.Value].Height != 0)
                    {
                        priorityZ += 1;
                    }

                    if (tiledataLoader.Value.StaticData[graphic.Value].IsMultiMovable)
                    {
                        priorityZ += 1;
                    }

                    renderable.Texture = artInfo.Texture;
                    renderable.UV = artInfo.UV;
                    renderable.Position.X -= (short)((artInfo.UV.Width >> 1) - 22);
                    renderable.Position.Y -= (short)(artInfo.UV.Height - 44);
                }

                renderable.Z = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ);
                renderable.Color = ShaderHueTranslator.GetHueVector(FixHue(uoHue));
            });


            queryEquip.Each((EntityView ent, ref WorldPosition pos, ref Graphic graphic, ref Hue hue,
                ref Renderable renderable, ref NetworkSerial serial, ref MobAnimation animation) =>
            {
                var priorityZ = pos.Z + 2;
                renderable.Position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z);

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
                    renderable.Texture = null;
                    return;
                }

                ref var equip = ref ent.Get<EquippedItem>(act);
                var orderKey = 0;
                if (equip.Layer == Layer.Mount)
                {
                    animId = Mounts.FixMountGraphic(tiledataLoader, animId);
                    animAction = animation.MountAction;
                }
                else if (_layerOrders[(int)animation.Direction & 7].TryGetValue(equip.Layer, out orderKey) &&
                    !IsItemCovered(dict.Value, world, act, equip.Layer))
                {
                    if (tiledataLoader.Value.StaticData[graphic.Value].AnimID != 0)
                        animId = tiledataLoader.Value.StaticData[graphic.Value].AnimID;
                }
                else
                {
                    renderable.Texture = null;
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

                renderable.Texture = frame.Texture;
                renderable.UV = frame.UV;
                renderable.Flip = mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                renderable.Position.X += 22;
                renderable.Position.Y += 22;
                if (mirror)
                    renderable.Position.X -= frame.UV.Width - frame.Center.X;
                else
                    renderable.Position.X -= frame.Center.X;
                renderable.Position.Y -= frame.UV.Height + frame.Center.Y;

                // TODO: priority Z based on layer ordering
                renderable.Z = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ) + (orderKey * 0.01f);
                renderable.Color = ShaderHueTranslator.GetHueVector(FixHue(hue.Value != 0 ? hue.Value : baseHue));
            });
        });

        scheduler.AddSystem((Res<MouseContext> mouseCtx, Res<KeyboardState> keyboard, Res<GameContext> gameCtx) =>
        {
            if (mouseCtx.Value.OldState.LeftButton == ButtonState.Pressed && mouseCtx.Value.NewState.LeftButton == ButtonState.Pressed)
            {
                gameCtx.Value.CenterOffset.X += mouseCtx.Value.NewState.X - mouseCtx.Value.OldState.X;
                gameCtx.Value.CenterOffset.Y += mouseCtx.Value.NewState.Y - mouseCtx.Value.OldState.Y;
            }

            if (keyboard.Value.IsKeyDown(Keys.Space))
            {
                gameCtx.Value.CenterOffset = Vector2.Zero;
            }
        }, Stages.FrameStart).RunIf((Res<UoGame> game) => game.Value.IsActive);

        scheduler.AddSystem(static (
            Res<GraphicsDevice> device,
            Res<Renderer.UltimaBatcher2D> batch,
            Res<GameContext> gameCtx,
            Res<MouseContext> mouseCtx,
            Query<(Renderable, TileStretched), Without<Relation<ContainedInto, Wildcard>>> queryTiles,
            Query<Renderable, (Without<TileStretched>, Without<MobAnimation>, Without<Relation<ContainedInto, Wildcard>>)> queryStatic,
            Query<Renderable, (With<MobAnimation>, Without<TileStretched>, Without<Relation<ContainedInto, Wildcard>>)> queryAnimations
        ) => {
            device.Value.Clear(Color.Black);

            var center = Isometric.IsoToScreen(gameCtx.Value.CenterX, gameCtx.Value.CenterY, gameCtx.Value.CenterZ);
            center.X -= device.Value.PresentationParameters.BackBufferWidth / 2f;
            center.Y -= device.Value.PresentationParameters.BackBufferHeight / 2f;
            center -= gameCtx.Value.CenterOffset;

            var sb = batch.Value;
            var matrix = Matrix.Identity;
            //matrix = Matrix.CreateScale(0.45f);

            sb.Begin(null, matrix);
            sb.SetBrightlight(1.7f);
            sb.SetSampler(SamplerState.PointClamp);
            sb.SetStencil(DepthStencilState.Default);

            queryTiles.Each((ref Renderable renderable, ref TileStretched stretched) =>
            {
                if (renderable.Texture != null)
                    sb.DrawStretchedLand
                    (
                        renderable.Texture,
                        renderable.Position - center,
                        renderable.UV,
                        ref stretched.Offset,
                        ref stretched.NormalTop,
                        ref stretched.NormalRight,
                        ref stretched.NormalLeft,
                        ref stretched.NormalBottom,
                        renderable.Color,
                        renderable.Z
                    );
            });

            queryStatic.Each((ref Renderable renderable) =>
            {
                if (renderable.Texture != null)
                    sb.Draw
                    (
                        renderable.Texture,
                        renderable.Position - center,
                        renderable.UV,
                        renderable.Color,
                        renderable.Rotation,
                        renderable.Origin,
                        renderable.Scale,
                        renderable.Flip,
                        renderable.Z
                    );
            });

            // NOTE: i cant mix statics with animation like ethereals because
            //       the transparent pixels get overraided somehow
            queryAnimations.Each((ref Renderable renderable) =>
            {
                if (renderable.Texture != null)
                    sb.Draw
                    (
                        renderable.Texture,
                        renderable.Position - center,
                        renderable.UV,
                        renderable.Color,
                        renderable.Rotation,
                        renderable.Origin,
                        renderable.Scale,
                        renderable.Flip,
                        renderable.Z
                    );
            });
            sb.SetSampler(null);
            sb.SetStencil(null);
            sb.End();
            device.Value.Present();
        }, Stages.AfterUpdate, ThreadingMode.Single)
        .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());
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

    static bool IsItemCovered(Dictionary<Layer, EntityView> dict, TinyEcs.World world, EcsID parent, Layer layer)
    {
        dict ??= new Dictionary<Layer, EntityView>();
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
                else if (pants.ID.IsValid && (pants.Get<Graphic>().Value is 0x0513 or 0x0514) ||
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

                if (pants.ID.IsValid && pants.Get<Graphic>().Value is 0x01EB or 0x03E5 or 0x03EB)
                {
                    if (dict.TryGetValue(Layer.Skirt, out var skirt) && skirt.Get<Graphic>().Value is not 0x01C7 and not 0x01E4)
                    {
                        return true;
                    }

                    if (robe1.ID.IsValid && robe1.Get<Graphic>().Value is not 0x0229 and not (>= 0x04E8 and <= 0x04EB))
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
                    if (robe2.ID.IsValid && robe2.Get<Graphic>().Value is not 0x9985 and not 0x9986 and not 0xA412)
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