using System.Runtime.CompilerServices;
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
                if (world.Exists(id))
                {
                    var parent = world.Entity(id);

                    if (parent.Has<WorldPosition>())
                        ent.Set(parent.Get<WorldPosition>());

                    // if (parent.Has<Facing>())
                    //     ent.Set(parent.Get<Facing>());

                    if (parent.Has<MobAnimation>())
                        ent.Set(parent.Get<MobAnimation>());
                }
                else if (id.IsValid)
                {

                }
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
            Res<GameContext> gameCtx
        ) => {
            query.Each((EntityView ent, ref WorldPosition pos, ref Graphic graphic, ref Hue hue,
                ref Renderable renderable, ref NetworkSerial serial,
                ref Facing direction, ref MobAnimation animation) =>
            {
                var uoHue = hue.Value;
                var priorityZ = pos.Z;
                renderable.Position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z);

                // TODO: mount?
                // if (ent.Has<EquippedItem, Wildcard>())
                // {
                //     var target = ent.Target<EquippedItem>();
                //     var action = ent.Get<EquippedItem>(target);
                //     if (action.Layer == Layer.Mount)
                //     {

                //     }
                // }

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

                    //var animId = Mounts.FixMountGraphic(tiledataLoader, graphic.Value);
                    // if (tiledataLoader.Value.StaticData[graphic.Value].AnimID != 0)
                    //     animId = tiledataLoader.Value.StaticData[graphic.Value].AnimID;

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
                var uoHue = hue.Value;
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
                ref var equip = ref ent.Get<EquippedItem>(act);
                if (equip.Layer == Layer.Mount)
                {
                    // if (world.Exists(act))
                    // {
                    //     ref var parentSer = ref world.Get<NetworkSerial>(act);
                    //     if (parentSer.Value == gameCtx.Value.PlayerSerial)
                    //     {

                    //     }
                    // }

                    animId = Mounts.FixMountGraphic(tiledataLoader, animId);
                    animAction = animation.MountAction;
                }
                else if (tiledataLoader.Value.StaticData[graphic.Value].AnimID != 0)
                    animId = tiledataLoader.Value.StaticData[graphic.Value].AnimID;

                (var dir, var mirror) = FixDirection(animation.Direction);

                var frames = assetsServer.Value.Animations.GetAnimationFrames
                (
                    animId,
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

                // TODO: priority Z based on layer ordering
                renderable.Z = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ);
                renderable.Color = ShaderHueTranslator.GetHueVector(FixHue(uoHue));
            });
        });

        scheduler.AddSystem(static (
            Res<GraphicsDevice> device,
            Res<Renderer.UltimaBatcher2D> batch,
            Res<GameContext> gameCtx,
            Res<MouseContext> mouseCtx,
            Query<Renderable, (Without<TileStretched>, Without<Relation<ContainedInto, Wildcard>>)> query,
            Query<(Renderable, TileStretched), Without<Relation<ContainedInto, Wildcard>>> queryTiles
        ) => {
            device.Value.Clear(Color.Black);

            var center = Isometric.IsoToScreen(gameCtx.Value.CenterX, gameCtx.Value.CenterY, gameCtx.Value.CenterZ);
            center.X -= device.Value.PresentationParameters.BackBufferWidth / 2f;
            center.Y -= device.Value.PresentationParameters.BackBufferHeight / 2f;

            if (mouseCtx.Value.NewState.LeftButton == ButtonState.Pressed)
            {
                gameCtx.Value.CenterOffset.X += mouseCtx.Value.NewState.X - mouseCtx.Value.OldState.X;
                gameCtx.Value.CenterOffset.Y += mouseCtx.Value.NewState.Y - mouseCtx.Value.OldState.Y;
            }

            center -= gameCtx.Value.CenterOffset;

            var sb = batch.Value;
            sb.Begin();
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
            query.Each((ref Renderable renderable) =>
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
}