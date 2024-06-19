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
                sb.DrawStretchedLand(
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
        }, Stages.AfterUpdate)
        .RunIf((SchedulerState state) => state.ResourceExists<GraphicsDevice>());
    }
}