using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;

namespace ClassicUO.Ecs;


[TinyPlugin]
internal readonly partial struct CursorPlugin
{
    public void Build(Scheduler scheduler)
    {

    }


    private static bool IsGrabbedEntityValid(Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial != 0;

    [TinySystem(Stages.AfterUpdate, ThreadingMode.Single)]
    [RunIf(nameof(IsGrabbedEntityValid))]
    void RenderCursor(
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
            grabbed.Hue == 0 ? Vector3.UnitZ : new (grabbed.Hue, 1, 1f),
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            0f
        );

        b.End();
    }
}
