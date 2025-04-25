using System;
using ClassicUO.Network;
using ClassicUO.Renderer;
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

        scheduler.OnUpdate((
            Res<Camera> camera,
            Res<MouseContext> mouseCtx,
            Single<Data<UINode, UIInteractionState>, Filter<With<GameWindowBorderUI>, With<GameScene>>> queryGameWindowBorder,
            Single<Data<UINode, UIInteractionState>, Filter<With<GameWindowBorderResizeUI>, With<GameScene>>> queryGameWindowBorderResize,
            Single<Data<UINode>, Filter<With<GameWindowUI>, With<GameScene>>> queryGameWindow
        ) =>
        {
            (_, var nodeBorder, var interaction) = queryGameWindowBorder.Get();
            (_, var nodeBorderResize, var interactionResize) = queryGameWindowBorderResize.Get();

            if (interaction.Ref == UIInteractionState.Pressed)
            {
                if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                {
                    camera.Value.Bounds.X += (int)mouseCtx.Value.PositionOffset.X;
                    camera.Value.Bounds.Y += (int)mouseCtx.Value.PositionOffset.Y;
                }
            }

            if (interactionResize.Ref == UIInteractionState.Pressed)
            {
                if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                {
                    camera.Value.Bounds.Width += (int)mouseCtx.Value.PositionOffset.X;
                    camera.Value.Bounds.Height += (int)mouseCtx.Value.PositionOffset.Y;

                    camera.Value.Bounds.Width = Math.Max(300, camera.Value.Bounds.Width);
                    camera.Value.Bounds.Height = Math.Max(300, camera.Value.Bounds.Height);
                }
            }

            nodeBorderResize.Ref.Config.floating.offset = new()
            {
                x = camera.Value.Bounds.X + camera.Value.Bounds.Width + 10 - nodeBorderResize.Ref.Config.layout.sizing.width.size.minMax.max,
                y = camera.Value.Bounds.Y + camera.Value.Bounds.Height + 10 - nodeBorderResize.Ref.Config.layout.sizing.height.size.minMax.max,
            };


            nodeBorder.Ref.Config.floating.offset = new()
            {
                x = camera.Value.Bounds.X - 5,
                y = camera.Value.Bounds.Y - 5,
            };
            nodeBorder.Ref.Config.layout.sizing = new()
            {
                width = Clay_SizingAxis.Fixed(camera.Value.Bounds.Width + 10),
                height = Clay_SizingAxis.Fixed(camera.Value.Bounds.Height + 10),
            };


            (_, var node) = queryGameWindow.Get();
            node.Ref.Config.floating.offset = new()
            {
                x = camera.Value.Bounds.X,
                y = camera.Value.Bounds.Y,
            };
            node.Ref.Config.layout.sizing = new()
            {
                width = Clay_SizingAxis.Fixed(camera.Value.Bounds.Width),
                height = Clay_SizingAxis.Fixed(camera.Value.Bounds.Height),
            };

        }, ThreadingMode.Single)
        .RunIf((SchedulerState state) => state.InState(GameState.GameScreen) && state.ResourceExists<Camera>());
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
                    backgroundColor = new (0f, 0f, 0.5f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(100),
                            height = Clay_SizingAxis.Grow(),
                        }
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



        var gameWindowBorder = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (38f / 255f, 38f / 255f, 38f / 255f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(10),
                            height = Clay_SizingAxis.Fixed(10),
                        },
                    },
                    floating = {
                        attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
                        zIndex = 0,
                    }
                }
            })
            .Set(UIInteractionState.None)
            .Add<GameWindowBorderUI>()
            .Add<GameScene>();

        var gameWindowBorderResize = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (1f, 0f, 0f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(10),
                            height = Clay_SizingAxis.Fixed(10),
                        },
                    },
                    floating = {
                        attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
                        zIndex = 0,
                    }
                }
            })
            .Set(UIInteractionState.None)
            .Add<GameWindowBorderResizeUI>()
            .Add<GameScene>();

        var gameWindow = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0f, 0f, 0f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fixed(10),
                            height = Clay_SizingAxis.Fixed(10),
                        },
                    },
                    floating = {
                        attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
                        zIndex = 0,
                    }
                }
            })
            .Add<GameWindowUI>()
            .Add<GameScene>();


        root.AddChild(menuBar);
        menuBar.AddChild(menuBarItem);


        root.AddChild(gameWindowBorder);
        root.AddChild(gameWindowBorderResize);
        root.AddChild(gameWindow);
    }

    private static void Cleanup(World world, Query<Data<UINode>, Filter<Without<Parent>, With<GameScene>>> query)
    {
        Console.WriteLine("[GameScreen] cleanup start");
        foreach ((var ent, _) in query)
            world.Delete(ent.Ref);
        Console.WriteLine("[GameScreen] cleanup done");
    }

    private struct GameScene;
    private struct GameWindowUI;
    private struct GameWindowBorderUI;
    private struct GameWindowBorderResizeUI;

    private enum ButtonAction
    {
        Logout
    }
}