using System;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Clay_cs;
using Microsoft.Xna.Framework;
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


        scheduler.OnUpdate((
            World world,
            Query<Data<UINode, Text>, With<TotalEntitiesMenu>> query,
            Query<Empty, With<IsTile>> queryTiles,
            Query<Empty, With<IsStatic>> queryStatics
        ) =>
        {
            var total = world.EntityCount;
            var countTiles = queryTiles.Count();
            var countStatics = queryStatics.Count();
            foreach ((var node, var text) in query)
            {
                text.Ref.Value = $"Total entities: {total} - tiles: {countTiles} - statics: {countStatics}";
            }


        }, ThreadingMode.Single)
        .RunIf((SchedulerState state) => state.InState(GameState.GameScreen))
        .RunIf((Time time, Local<float> lastAccess) =>
        {
            if (time.Total > lastAccess.Value)
            {
                lastAccess.Value = time.Total + 250f;
                return true;
            }
            return false;
        });

        scheduler.OnUpdate((
            Query<Data<UINode, UIInteractionState, ButtonAction>, Changed<UIInteractionState>> query,
            Res<NetClient> network,
            State<GameState> state
        ) =>
        {
            foreach ((var node, var interaction, var action) in query)
            {
                if (interaction.Ref == UIInteractionState.Released)
                {
                    switch (action.Ref)
                    {
                        case ButtonAction.Logout:
                            Console.WriteLine("Logout button pressed");
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
            Local<Rectangle> lastSize,
            Single<Data<UINode, UIInteractionState>, Filter<With<GameWindowBorderUI>, With<GameScene>>> queryGameWindowBorder,
            Single<Data<UINode, UIInteractionState>, Filter<With<GameWindowBorderResizeUI>, With<GameScene>>> queryGameWindowBorderResize,
            Single<Data<UINode>, Filter<With<GameWindowUI>, With<GameScene>>> queryGameWindow
        ) =>
        {
            (var nodeBorder, var interaction) = queryGameWindowBorder.Get();
            (var nodeBorderResize, var interactionResize) = queryGameWindowBorderResize.Get();

            if (interaction.Ref == UIInteractionState.Pressed)
            {
                if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                {
                    camera.Value.Bounds.X += (int)mouseCtx.Value.PositionOffset.X;
                    camera.Value.Bounds.Y += (int)mouseCtx.Value.PositionOffset.Y;
                }
            }

            if (interactionResize.Ref == UIInteractionState.Pressed && mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
            {
                ref var newBounds = ref lastSize.Value;
                newBounds.Width += (int)mouseCtx.Value.PositionOffset.X;
                newBounds.Height += (int)mouseCtx.Value.PositionOffset.Y;

                if (newBounds.Width >= 300)
                    camera.Value.Bounds.Width = newBounds.Width;
                if (newBounds.Height >= 300)
                    camera.Value.Bounds.Height = newBounds.Height;
            }
            else
            {
                lastSize.Value = camera.Value.Bounds;
            }

            const int BORDER_SIZE = 10;

            nodeBorderResize.Ref.Config.floating.offset = new()
            {
                x = camera.Value.Bounds.X + camera.Value.Bounds.Width + BORDER_SIZE - nodeBorderResize.Ref.Config.layout.sizing.width.size.minMax.max,
                y = camera.Value.Bounds.Y + camera.Value.Bounds.Height + BORDER_SIZE - nodeBorderResize.Ref.Config.layout.sizing.height.size.minMax.max,
            };

            nodeBorder.Ref.Config.floating.offset = new()
            {
                x = camera.Value.Bounds.X - BORDER_SIZE * 0.5f,
                y = camera.Value.Bounds.Y - BORDER_SIZE * 0.5f,
            };
            nodeBorder.Ref.Config.layout.sizing = new()
            {
                width = Clay_SizingAxis.Fixed(camera.Value.Bounds.Width + BORDER_SIZE),
                height = Clay_SizingAxis.Fixed(camera.Value.Bounds.Height + BORDER_SIZE),
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
                        padding = Clay_Padding.All(4),
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
                            height = Clay_SizingAxis.Fit(0,0),
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                        childGap = 4,
                        childAlignment = {
                            x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_RIGHT,
                            y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                        },
                        padding = Clay_Padding.All(4),
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
                            width = Clay_SizingAxis.Fit(0, 0),
                            height = Clay_SizingAxis.Grow(),
                        },
                        childAlignment = {
                            x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                            y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                        },
                        padding =  Clay_Padding.All(4),
                    },
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

        var menuBarItem2 = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0f, 0f, 0.5f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Fit(0, 0),
                            height = Clay_SizingAxis.Grow(),
                        },
                        childAlignment = {
                            x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                            y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                        },
                        padding = Clay_Padding.All(4),
                    },
                }
            })
            .Set(new Text()
            {
                Value = "Total entities: 0",
                TextConfig = {
                    fontId = 0,
                    fontSize = 18,
                    textColor = new (1, 1, 1, 1),
                },
            })
            .Add<GameScene>()
            .Add<TotalEntitiesMenu>();


        var gameWindowRoot = world.Entity()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0f, 0f, 0f, 0f),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Grow(),
                            height = Clay_SizingAxis.Grow(),
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        padding = Clay_Padding.All(4),
                    }
                }
            })
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
                        clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
                        attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_ROOT,
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
                        clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
                        attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_ROOT,
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
                        clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
                        attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_ROOT,
                    }
                }
            })
            .Add<GameWindowUI>()
            .Add<GameScene>();


        menuBar.AddChild(menuBarItem);
        menuBar.AddChild(menuBarItem2);

        gameWindowRoot.AddChild(gameWindowBorder);
        gameWindowRoot.AddChild(gameWindowBorderResize);
        gameWindowRoot.AddChild(gameWindow);


        root.AddChild(menuBar);
        root.AddChild(gameWindowRoot);
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
    private struct TotalEntitiesMenu;

    private enum ButtonAction
    {
        Logout
    }
}
