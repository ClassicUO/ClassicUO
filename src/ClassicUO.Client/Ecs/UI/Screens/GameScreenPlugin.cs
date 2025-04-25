using System;
using ClassicUO.Network;
using Clay_cs;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct GameScreenPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        var setupFn = Setup;
        var cleanupFn = Cleanup;

        scheduler.OnEnter(GameState.GameScreen, setupFn, ThreadingMode.Single);
        scheduler.OnExit(GameState.GameScreen, cleanupFn, ThreadingMode.Single);

        scheduler.OnUpdate((Query<Data<UINode, UIInteractionState, ButtonAction>> query, Res<NetClient> network, State<GameState> state) =>
        {
            foreach ((var node, var interaction, var action) in query)
            {
                if (interaction.Ref == UIInteractionState.Released)
                {
                    switch (action.Ref)
                    {
                        case ButtonAction.Logout:
                            Console.WriteLine("Logout button pressed");
                            network.Value.Disconnect();
                            state.Set(GameState.LoginScreen);
                            break;
                    }
                }
            }
        }, ThreadingMode.Single)
        .RunIf((SchedulerState state) => state.InState(GameState.GameScreen));
    }

    private static void Setup(World world, Res<GumpBuilder> gumpBuilder, Res<ClayUOCommandBuffer> clay)
    {
        var root = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (18f / 255f, 18f / 255f, 18f / 255f, 1),
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


        var menuBar = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0f, 0f, 0f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Grow(),
                            height = Clay_SizingAxis.Fixed(25),
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                    }
                }
            })
            .Add<GameScene>();

        var menuBarItem = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0f, 0f, 0f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(100),
                            height = Clay_SizingAxis.Grow(),
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                    }
                }
            })
            .Set(new Text()
            {
                Value = "Logout",
                TextConfig = {
                    fontId = 0,
                    fontSize = 18,
                    textColor = new (1, 1, 1, 1),
                },
            })
            .Set(ButtonAction.Logout)
            .Set(UIInteractionState.None)
            .Add<GameScene>();


        root.AddChild(menuBar);
        menuBar.AddChild(menuBarItem);
    }

    private static void Cleanup(World world, Query<Data<UINode>, Filter<Without<Parent>, With<GameScene>>> query)
    {
        Console.WriteLine("[GameScreen] cleanup start");
        foreach ((var ent, _) in query)
            world.Delete(ent.Ref);
        Console.WriteLine("[GameScreen] cleanup done");
    }

    private struct GameScene;

    private enum ButtonAction
    {
        Logout
    }
}