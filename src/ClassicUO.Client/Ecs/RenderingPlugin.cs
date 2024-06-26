using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinyEcs;

namespace ClassicUO.Ecs;

readonly struct RenderingPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddSystem(static (
            Query<(WorldPosition, Graphic), (With<NetworkSerial>, Without<Renderable>, Without<ContainedInto>)> query,
            Res<AssetsServer> assetsServer,
            Res<Assets.TileDataLoader> tiledataLoader,
            TinyEcs.World world
        ) => {
            query.Each((EntityView ent, ref WorldPosition pos, ref Graphic graphic) =>
            {
                ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(graphic.Value);

                var priorityZ = pos.Z;

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
            });
        });

        scheduler.AddSystem(static (
            Query<(WorldPosition, Graphic, Renderable), (With<NetworkSerial>, Without<ContainedInto>)> query,
            Res<AssetsServer> assetsServer,
            Res<Assets.TileDataLoader> tiledataLoader,
            TinyEcs.World world
        ) => {
            query.Each((ref WorldPosition pos, ref Graphic graphic, ref Renderable renderable) =>
            {
                ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(graphic.Value);

                var priorityZ = pos.Z;

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
            });
        });

        scheduler.AddSystem(static (
            Res<GraphicsDevice> device,
            Res<Renderer.UltimaBatcher2D> batch,
            Res<GameContext> gameCtx,
            Res<MouseContext> mouseCtx,
            Query<Renderable, Without<TileStretched>> query,
            Query<(Renderable, TileStretched)> queryTiles
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
                )
            );
            query.Each((ref Renderable renderable) =>
                sb.Draw
                (
                    renderable.Texture,
                    renderable.Position - center,
                    renderable.UV,
                    renderable.Color,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    renderable.Z
                )
            );
            sb.SetStencil(null);
            sb.End();
            device.Value.Present();
        }, Stages.AfterUpdate, ThreadingMode.Single)
        .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());
    }
}