using System;
using ClassicUO.Assets;
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
        Res<AssetsServer> assets,
        Res<UOFileManager> files,
        Res<UoGame> game
    )
    {
        var grabbed = grabbedItem.Value;
        ref readonly var artInfo = ref assets.Value.Arts.GetArt(grabbed.Graphic);
        if (artInfo.Texture == null)
            return;

        // A held stack (Amount > 1) draws a second sprite +5/+5 to fake a pile,
        // same as the container/world render (GuiRenderingPlugin).
        var tileData = files.Value.TileData.StaticData;
        bool stacked = grabbed.Amount > 1
            && grabbed.Graphic < tileData.Length
            && tileData[grabbed.Graphic].IsStackable;

        var b = batch.Value;
        // Cursor position is in LOGICAL pixels; the batch otherwise draws in
        // PHYSICAL pixels, so without the dpi scale the held sprite drifts
        // (1 - dpi) * pos away from the cursor (visible as a growing offset
        // toward bottom-right). Matches GuiRenderingPlugin's transform.
        var dpi = game.Value.DpiScale;
        if (dpi <= 0f) dpi = 1f;
        b.Begin(null, Matrix.CreateScale(dpi));
        // PointClamp keeps pixel art crisp at integer DPI; LinearClamp smooths
        // the per-sprite upscale at fractional DPI. Mirrors GuiRenderingPlugin.
        b.SetSampler(dpi == Math.Floor(dpi) ? SamplerState.PointClamp : SamplerState.LinearClamp);

        // Center the held sprite on the cursor (mirrors main's GameCursor.Draw:
        // offset = (W/2, H/2) - ItemHold.MouseOffset). MouseOffset is not yet
        // tracked in the ECS — half-size centering is the visually-correct base
        // case (grab-point precision is a follow-up when MouseOffset is wired).
        var halfSize = new Vector2(artInfo.UV.Width * 0.5f, artInfo.UV.Height * 0.5f);
        var origin = mouseCtx.Value.Position - halfSize;
        var hue = grabbed.Hue == 0 ? Vector3.UnitZ : new Vector3(grabbed.Hue, 1, 1f);

        b.Draw(
            artInfo.Texture,
            origin,
            artInfo.UV,
            hue,
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            0f
        );

        if (stacked)
        {
            // Front sprite of the pile, +5/+5 over the base (matches the
            // container/world stacked render order).
            b.Draw(artInfo.Texture, origin + new Vector2(5, 5), artInfo.UV, hue,
                0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }

        b.SetSampler(null);
        b.End();
    }
}
