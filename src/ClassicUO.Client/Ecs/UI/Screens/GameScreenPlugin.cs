using Clay_cs;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct GameScreenPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        var setupFn = Setup;

        scheduler.AddSystem(setupFn, Stages.Startup, ThreadingMode.Single)
                 .RunIf((Res<GameState> state) => state == GameState.GameScreen);
    }

    private static void Setup(World world, Res<GumpBuilder> gumpBuilder, Res<ClayUOCommandBuffer> clay)
    {
        var root = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0.2f, 0.2f, 0.2f, 1),
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
    }

    private struct GameScene;
}