using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;


internal readonly struct CursorPlugin : IPlugin
{
    public void Build(App app)
    {
        var renderCursorFn = RenderCursor;

        app
            .AddSystem(renderCursorFn)
            .InStage(Stage.PostUpdate)
            .SingleThreaded()
            .RunIf(w => w.HasResource<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial != 0 && grabbedItem.Value.Graphic != 0);
    }


    private static void RenderCursor(
        Res<UltimaBatcher2D> batch,
        Res<MouseContext> mouseCtx,
        Res<GrabbedItem> grabbedItem,
        Res<AssetsServer> assets
    )
    {
        var grabbed = grabbedItem.Value;
        ref readonly var artInfo = ref assets.Value.Arts.GetArt(grabbed.Graphic);

        var b = batch.Value;
        b.Begin();

        b.Draw(
            artInfo.Texture,
            mouseCtx.Value.Position,
            artInfo.UV,
            grabbed.Hue == 0 ? Vector3.UnitZ : new(grabbed.Hue, 1, 1f),
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            0f
        );

        b.End();
    }
}
