using System.Runtime.CompilerServices;
using ClassicUO.Game.Data;
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
            Query<Graphic,
                (With<NetworkSerial>, Without<Relation<ContainedInto, Wildcard>>, With<Relation<EquippedItem, Wildcard>>)> queryEquip,
            Query<(WorldPosition, Graphic, NetworkSerial, Optional<Relation<EquippedItem, Wildcard>>),
                (Without<Renderable>, Without<Relation<ContainedInto, Wildcard>>)> query,
            Res<AssetsServer> assetsServer,
            Res<Assets.TileDataLoader> tiledataLoader,
            TinyEcs.World world
        ) => {

            queryEquip.Each((EntityView ent, ref Graphic graphic) =>
            {
                // sync equipment position with the parent
                var parent = world.Entity(ent.Target<EquippedItem>());
                if (parent.Has<WorldPosition>())
                    ent.Set(parent.Get<WorldPosition>());
            });

            query.Each((EntityView ent, ref WorldPosition pos, ref Graphic graphic, ref NetworkSerial serial, ref Relation<EquippedItem, Wildcard> equip) =>
            {
                var priorityZ = pos.Z;
                if (!Unsafe.IsNullRef(ref equip))
                {

                }
                if (ClassicUO.Game.SerialHelper.IsMobile(serial.Value))
                {
                    var frames = assetsServer.Value.Animations.GetAnimationFrames
                    (
                        graphic.Value,
                        0,
                        0,
                        out var _,
                        out var _
                    );

                    ent.Set(new Renderable()
                    {
                        //Texture = frames[0].Texture,
                        Position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z),
                        Color = Vector3.UnitZ,
                        //UV = frames[0].UV,
                        Z = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ + 2)
                    });
                    ent.Set(new MobAnimation() { });
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

                    ent.Set(new Renderable()
                    {
                        Texture = artInfo.Texture,
                        Position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z),
                        Color = Vector3.UnitZ,
                        UV = artInfo.UV,
                        Z = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ)
                    });
                }
            });
        });

        scheduler.AddSystem(static (
            Query<(WorldPosition, Graphic, Renderable, NetworkSerial, Optional<Facing>, Optional<MobAnimation>),
                Without<Relation<ContainedInto, Wildcard>>> query,
            Res<AssetsServer> assetsServer,
            Res<Assets.TileDataLoader> tiledataLoader,
            TinyEcs.World world
        ) => {
            query.Each((ref WorldPosition pos, ref Graphic graphic,
                ref Renderable renderable, ref NetworkSerial serial,
                ref Facing direction, ref MobAnimation animation) =>
            {
                var priorityZ = pos.Z;

                if (ClassicUO.Game.SerialHelper.IsMobile(serial.Value))
                {
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
                        out var _,
                        out var _
                    );

                    renderable.Texture = frames.IsEmpty ? null : frames[animIndex].Texture;
                    renderable.UV = frames.IsEmpty ? Rectangle.Empty : frames[animIndex].UV;
                    renderable.Position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z);
                    renderable.Position.X -= 22;
                    renderable.Position.Y -= 22;
                    renderable.Z = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ + 2);
                    renderable.Flip = mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
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

                    renderable.Position = Isometric.IsoToScreen(pos.X, pos.Y, pos.Z);
                    renderable.Position.X -= (short)((artInfo.UV.Width >> 1) - 22);
                    renderable.Position.Y -= (short)(artInfo.UV.Height - 44);
                    renderable.Z = Isometric.GetDepthZ(pos.X, pos.Y, priorityZ);
                }
            });
        });

        scheduler.AddSystem(static (
            Res<GraphicsDevice> device,
            Res<Renderer.UltimaBatcher2D> batch,
            Res<GameContext> gameCtx,
            Res<MouseContext> mouseCtx,
            Query<Renderable, (Without<TileStretched>, Without<Relation<ContainedInto, Wildcard>>, Without<EquippedItem>)> query,
            Query<(Renderable, TileStretched), (Without<Relation<ContainedInto, Wildcard>>, Without<EquippedItem>)> queryTiles
        ) => {
            device.Value.Clear(Color.AliceBlue);

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
            sb.SetStencil(null);
            sb.End();
            device.Value.Present();
        }, Stages.AfterUpdate, ThreadingMode.Single)
        .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static (Direction, bool) FixDirection(Direction dir)
    {
        dir &= ~(Direction.Running);
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

            case 0:
            case Direction.West:
                mirror = dir == 0;
                dir = Direction.Down;

                break;

            case Direction.Down:
                dir = 0;

                break;

            case Direction.Up:
                dir = Direction.South;

                break;
        }

        return (dir, mirror);
    }
}