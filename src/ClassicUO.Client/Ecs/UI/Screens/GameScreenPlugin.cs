using ClassicUO.Renderer;
using Clay_cs;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct GameScreenPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        var setupFn = Setup;
        var updateCameraFn = UpdateCamera;

        scheduler.AddResource(new Camera(0.5f, 2.5f, 0.1f));

        scheduler.AddSystem(setupFn, Stages.Startup, ThreadingMode.Single)
                 .RunIf((Res<GameState> state) => state == GameState.GameScreen);

        scheduler.AddSystems([
            scheduler.AddSystem(updateCameraFn, Stages.Update, ThreadingMode.Single)
        ], Stages.Update, ThreadingMode.Single)
        ;//.RunIf((Res<GameState> state) => state == GameState.GameScreen);
    }

    private static void Setup(World world, Res<GumpBuilder> gumpBuilder, Res<ClayUOCommandBuffer> clay)
    {
        var root = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0.1f, 0.1f, 0.1f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Grow(),
                            height = Clay_SizingAxis.Grow(),
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    }
                }
            })
            .Add<GameScene>();


        // root.AddChild(
        //     gumpBuilder.Value.AddGump(
        //         0x014E,
        //         Vector3.UnitZ,
        //         new(0, 0)
        //     ).Add<GameScene>()
        // );
    }

    private static void UpdateCamera(Time time, Res<Camera> camera, Res<MouseContext> mouseCtx)
    {
        var mousePos = mouseCtx.Value.Position;

        if (mouseCtx.Value.Wheel > 0)
            camera.Value.ZoomIn();
        else if (mouseCtx.Value.Wheel < 0)
            camera.Value.ZoomOut();
        camera.Value.Bounds = new(300, 50, 800, 600); // TODO: get the actual window size
        camera.Value.Update(true, time.Total, new((int)mousePos.X, (int)mousePos.Y));
    }

    private struct GameScene;
}