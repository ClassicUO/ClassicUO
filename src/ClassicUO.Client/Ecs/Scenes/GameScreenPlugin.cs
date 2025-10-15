using System;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Clay_cs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct GameScreenPlugin : IPlugin
{
    const int BORDER_SIZE = 10;

    public void Build(App app)
    {
        var setupFn = Setup;
        var cleanupFn = Cleanup;
        var updateEntitiesCountFn = UpdateEntitiesCount;
        var handleButtonsPressedFn = HandleButtonsPressed;
        var adjustCameraAndBoundsFn = AdjustCameraAndBounds;

        app
            .AddResource<RenderTarget2D>(null)

            .AddSystem(setupFn)
            .OnEnter(GameState.GameScreen)
            .Build()

            .AddSystem(cleanupFn)
            .OnExit(GameState.GameScreen)
            .Build()

            .AddSystem(updateEntitiesCountFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<Time> time, Local<float> lastAccess) =>
            {
                if (time.Value.Total > lastAccess.Value)
                {
                    lastAccess.Value = time.Value.Total + 250f;
                    return true;
                }
                return false;
            })
            .Build()

            .AddSystem(handleButtonsPressedFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build()

            .AddSystem(adjustCameraAndBoundsFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf(w => w.HasResource<Camera>())
            .Build();
    }

    private static void Setup(Commands commands, Res<GumpBuilder> gumpBuilder, Res<ClayUOCommandBuffer> clay)
    {
        var root = commands.Spawn()
            .InsertBundle(new UINodeBundle()
            {
                Node = new UINode()
                {
                    Config = {
                        backgroundColor = new (18f / 255f, 18f / 255f, 18f / 255f, 0f),
                        layout = {
                            sizing = {
                                width = Clay_SizingAxis.Grow(),
                                height = Clay_SizingAxis.Grow(),
                            },
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            padding = Clay_Padding.All(4),
                        }
                    }
                }
            })
            .Insert<GameScene>();

        var floating = new Clay_FloatingElementConfig()
        {
            attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
            clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
            zIndex = -1
        };

        var gameWindowBorder = commands.Spawn()
    .InsertBundle(new UINodeBundle()
    {
        Node = new UINode()
        {
            Config = {
                backgroundColor = new (38f / 255f, 38f / 255f, 38f / 255f, 1),
                layout = {
                    sizing = {
                        width = Clay_SizingAxis.Fixed(BORDER_SIZE),
                        height = Clay_SizingAxis.Fixed(BORDER_SIZE),
                    },
                },
                floating = floating
            }
        }
    })
    .Insert(new UIMouseAction())
    .Insert<GameWindowBorderUI>()
    .Insert<GameScene>();

        var gameWindowBorderResize = commands.Spawn()
    .InsertBundle(new UINodeBundle()
    {
        Node = new UINode()
        {
            Config = {
                backgroundColor = new (1f, 0f, 0f, 1),
                layout = {
                    sizing = {
                        width = Clay_SizingAxis.Fixed(BORDER_SIZE),
                        height = Clay_SizingAxis.Fixed(BORDER_SIZE),
                    },
                },
                floating = floating
            }
        }
    })
    .Insert(new UIMouseAction())
    .Insert<GameWindowBorderResizeUI>()
    .Insert<GameScene>();

        var gameWindow = commands.Spawn()
    .InsertBundle(new UINodeBundle()
    {
        Node = new UINode()
        {
            Config = {
                backgroundColor = new (1f, 1f, 1f, 1f),
                layout = {
                    sizing = {
                        width = Clay_SizingAxis.Fixed(BORDER_SIZE),
                        height = Clay_SizingAxis.Fixed(BORDER_SIZE),
                    },
                },
                floating = floating
            }
        }
    })
    .Insert<GameWindowUI>()
    .Insert<GameScene>();



        var menuBar = commands.Spawn()
    .InsertBundle(new UINodeBundle()
    {
        Node = new UINode()
        {
            Config = {
                backgroundColor = new(0f, 0f, 0f, 1),
                layout =
                {
                    sizing =
                    {
                        width = Clay_SizingAxis.Grow(),
                        height = Clay_SizingAxis.Fit(0, 0),
                    },
                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                    childGap = 4,
                    childAlignment =
                    {
                        x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_RIGHT,
                        y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                    },
                    padding = Clay_Padding.All(4),
                },
                // floating = floating
            }
        }
    })
    // .Set(new UIMouseAction())
    // .Add<UIMovable>()
    .Insert<GameScene>();

        var menuBarItem = commands.Spawn()
    .InsertBundle(new UINodeBundle()
    {
        Node = new UINode()
        {
            Config = {
                backgroundColor = new(0f, 0f, 0.5f, 1),
                layout =
                {
                    sizing =
                    {
                        width = Clay_SizingAxis.Fit(0, 0),
                        height = Clay_SizingAxis.Grow(),
                    },
                    childAlignment =
                    {
                        x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                        y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                    },
                    padding = Clay_Padding.All(4),
                },
            }
        }
    })
    .Insert(new Text()
    {
        Value = "Logout",
        TextConfig = {
                    fontId = 0,
                    fontSize = 18,
                    textColor = new (1, 1, 1, 1),
        },
    })
    .Insert(ButtonAction.Logout)
    .Insert(new UIMouseAction())
    .Insert<GameScene>();

        var menuBarItem2 = commands.Spawn()
    .InsertBundle(new UINodeBundle()
    {
        Node = new UINode()
        {
            Config = {
                backgroundColor = new(0f, 0f, 0.5f, 1),
                layout =
                {
                    sizing =
                    {
                        width = Clay_SizingAxis.Fit(0, 0),
                        height = Clay_SizingAxis.Grow(),
                    },
                    childAlignment =
                    {
                        x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                        y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                    },
                    padding = Clay_Padding.All(4),
                },
            }
        }
    })
    .Insert(new Text()
    {
        Value = "Total entities: 0",
        TextConfig = {
                    fontId = 0,
                    fontSize = 18,
                    textColor = new (1, 1, 1, 1),
        },
    })
    .Insert<GameScene>()
    .Insert<TotalEntitiesMenu>();




        menuBar.AddChild(menuBarItem);
        menuBar.AddChild(menuBarItem2);



        root.AddChild(gameWindowBorder);
        root.AddChild(gameWindowBorderResize);
        root.AddChild(gameWindow);

        // root.AddChild(gameWindowRoot);
        root.AddChild(menuBar);
    }


    class CameraBounds
    {
        public Rectangle Rectangle;
    }

    private static void AdjustCameraAndBounds(
        Res<Camera> camera,
        ResMut<RenderTarget2D> renderTarget,
        Res<ImageCache> imageCache,
        Res<UltimaBatcher2D> batch,
        Res<MouseContext> mouseCtx,
        Local<CameraBounds> lastSize,
        Single<Data<UINode, UIMouseAction>, Filter<With<GameWindowBorderUI>, With<GameScene>>> queryGameWindowBorder,
        Single<Data<UINode, UIMouseAction>, Filter<With<GameWindowBorderResizeUI>, With<GameScene>>> queryGameWindowBorderResize,
        Single<Data<UINode>, Filter<With<GameWindowUI>, With<GameScene>>> queryGameWindow
    )
    {
        (var nodeBorder, var interaction) = queryGameWindowBorder.Get();
        (var nodeBorderResize, var interactionResize) = queryGameWindowBorderResize.Get();


        if (interaction.Ref is { IsPressed: true, Button: Input.MouseButtonType.Left })
        {
            camera.Value.Bounds.X += (int)mouseCtx.Value.PositionOffset.X;
            camera.Value.Bounds.Y += (int)mouseCtx.Value.PositionOffset.Y;
        }

        if (interactionResize.Ref is { IsPressed: true, WasPressed: true, Button: MouseButtonType.Left })
        {
            ref var newBounds = ref lastSize.Value.Rectangle;
            newBounds.Width += (int)mouseCtx.Value.PositionOffset.X;
            newBounds.Height += (int)mouseCtx.Value.PositionOffset.Y;

            if (newBounds.Width >= 300)
                camera.Value.Bounds.Width = newBounds.Width;
            if (newBounds.Height >= 300)
                camera.Value.Bounds.Height = newBounds.Height;
        }
        else
        {
            lastSize.Value.Rectangle = camera.Value.Bounds;
        }

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

        if (renderTarget.Value == null || renderTarget.Value.IsDisposed ||
            renderTarget.Value.Width != batch.Value.GraphicsDevice.PresentationParameters.BackBufferWidth ||
            renderTarget.Value.Height != batch.Value.GraphicsDevice.PresentationParameters.BackBufferHeight)
        {
            renderTarget.Value?.Dispose();
            renderTarget.Value = new(
                batch.Value.GraphicsDevice,
                batch.Value.GraphicsDevice.PresentationParameters.BackBufferWidth,
                batch.Value.GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                SurfaceFormat.Color, DepthFormat.Depth24Stencil8);

            imageCache.Value[renderTarget.Value.GetHashCode()] = renderTarget.Value;
        }
        else
        {
            unsafe
            {
                node.Ref.Config.image.imageData = (void*)renderTarget.Value.GetHashCode();
            }
        }
    }

    private static void HandleButtonsPressed(
        Query<Data<UINode, UIMouseAction, ButtonAction>, Changed<UIMouseAction>> query,
        Res<NextState<GameState>> state
    )
    {
        foreach ((var node, var interaction, var action) in query)
        {
            if (interaction.Ref is { WasPressed: true, IsPressed: false, Button: Input.MouseButtonType.Left })
            {
                switch (action.Ref)
                {
                    case ButtonAction.Logout:
                        Console.WriteLine("Logout button pressed");
                        state.Value.Set(GameState.LoginScreen);
                        break;
                }
            }
        }
    }

    private static void UpdateEntitiesCount(
        Commands commands,
        Query<Data<UINode, Text>, With<TotalEntitiesMenu>> query,
        Query<Empty, With<IsTile>> queryTiles,
        Query<Empty, With<IsStatic>> queryStatics
    )
    {
        var total = 0; // world.EntityCount;
        var countTiles = queryTiles.Count();
        var countStatics = queryStatics.Count();
        foreach ((var node, var text) in query)
        {
            text.Ref.Value = $"Total entities: {total} - tiles: {countTiles} - statics: {countStatics}";
        }
    }

    private static void Cleanup(Commands commands, Query<Data<UINode>, Filter<Without<Parent>, With<GameScene>>> query)
    {
        Console.WriteLine("[GameScreen] cleanup start");
        foreach ((var ent, _) in query)
            commands.Entity(ent.Ref).Despawn();
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
