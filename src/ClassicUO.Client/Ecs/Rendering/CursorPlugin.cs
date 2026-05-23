using System;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;


internal readonly struct CursorPlugin : IPlugin
{
    public void Build(App app)
    {
        var renderCursorFn = RenderCursor;

        // Run in UiRenderStage so the cursor draws after GUI composition but
        // before Stage.Last (Present). Stage.Last races against FnaPlugin's
        // device.Present() — order is undefined and Present can run first,
        // making the cursor disappear until the next frame.
        app
            .AddSystem(renderCursorFn)
            .InStage(UiPlugin.UiRenderStage)
            .SingleThreaded()
            .After("cuo:gui_rendering")
            .RunIf((Commands cmds) => cmds.HasResource<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial != 0 && grabbedItem.Value.Graphic != 0)
            .Build();
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
        if (artInfo.Texture == null)
        {
            Console.WriteLine("[CURSOR-RENDER] no texture for graphic=0x{0:X4}", grabbed.Graphic);
            return;
        }

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
