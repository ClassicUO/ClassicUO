using ClassicUO.Network;
using Clay_cs;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct LoginErrorScreenPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<LoginErrorsInfoEvent>();

        var cleanupFn = Cleanup;
        var loginErrorSetupFn = LoginErrorInfoSetup;
        var buttonHandlerFn = ButtonHandler;

        scheduler.OnExit(GameState.LoginError, cleanupFn, ThreadingMode.Single);
        scheduler.OnUpdate(loginErrorSetupFn, ThreadingMode.Single)
                 .RunIf((SchedulerState state, EventReader<LoginErrorsInfoEvent> reader)
                     => !reader.IsEmpty && state.InState(GameState.LoginError));
        scheduler.OnUpdate(buttonHandlerFn, ThreadingMode.Single)
                 .RunIf((SchedulerState state) => state.InState(GameState.LoginError));
    }

    private static void Cleanup(Query<Data<UINode>, Filter<With<LoginErrorScene>, Without<Parent>>> query)
    {
        foreach ((var ent, _) in query)
        {
            ent.Ref.Delete();
        }
    }

    private static void LoginErrorInfoSetup(World world, EventReader<LoginErrorsInfoEvent> reader)
    {
        var root = world.Entity()
            .Add<LoginErrorScene>()
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
                        childAlignment = {
                            x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                            y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                        }
                    }
                }
            });

        var loginErrorLabel = world.Entity()
            .Add<LoginErrorScene>()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0.3f, 0.3f, 0.3f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Percent(0.5f),
                            height = Clay_SizingAxis.Fit(0, 0),
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        childAlignment = {
                            x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
                            y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP,
                        },
                        padding = Clay_Padding.All(8),
                        childGap = 4
                    }
                }
            })
            .Set(new Text()
            {
                Value = "Error on login",
                TextConfig =
                {
                    fontId = 0,
                    fontSize = 28,
                    // textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                    textColor = new (1f, 1f, 1f, 1),
                }
            });

        var menu = world.Entity()
            .Add<LoginErrorScene>()
            .Set(new UINode()
            {
                Config = {
                        backgroundColor = new (0.3f, 0.3f, 0.3f, 1),
                        layout = {
                            sizing = {
                                width = Clay_SizingAxis.Percent(0.5f),
                                height = Clay_SizingAxis.Percent(0.5f),
                            },
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            childAlignment = {
                                x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                                y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP,
                            },
                            padding = Clay_Padding.All(8),
                            childGap = 4
                        }
                }
            });

        foreach (var ev in reader)
        {
            var serverEnt = world.Entity()
                .Add<LoginErrorScene>()
                .Set(ev.Error)
                .Set(new UINode()
                {
                    Config = {
                        backgroundColor = new (0.6f, 0.6f, 0.6f, 1),
                        layout = {
                            sizing = {
                                width = Clay_SizingAxis.Percent(0.8f),
                                height = Clay_SizingAxis.Fit(0, 0),
                            },
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            childAlignment = {
                                x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                                y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                            },
                            padding = Clay_Padding.All(8),
                            childGap = 4
                        }
                    }
                })
                .Set(new Text()
                {
                    Value = ev.Error.ErrorMessage,
                    TextConfig =
                    {
                        fontId = 0,
                        fontSize = 24,
                        // textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                        textColor = new (1f, 1f, 1f, 1),
                    }
                })
                .Set(UIInteractionState.None);

            menu.AddChild(serverEnt);
        }

        var footerMenu = world.Entity()
            .Add<LoginErrorScene>()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0, 0, 0, 0),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Grow(),
                            height = Clay_SizingAxis.Grow(),
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        childAlignment = {
                            x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                            y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_BOTTOM,
                        },
                        padding = Clay_Padding.All(8),
                        childGap = 4
                    }
                }
            });

        var okButtonEntity = world.Entity()
            .Add<LoginErrorScene>()
            .Set(new UINode()
            {
                Config = {
                    backgroundColor = new (0.6f, 0.6f, 0.6f, 1),
                    layout = {
                        sizing = {
                            width = Clay_SizingAxis.Percent(0.4f),
                            height = Clay_SizingAxis.Fit(0, 0),
                        },
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        childAlignment = {
                            x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
                            y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
                        },
                        padding = Clay_Padding.All(8),
                        childGap = 4
                    }
                }
            })
            .Set(new Text()
            {
                Value = "OK",
                TextConfig =
                {
                    fontId = 0,
                    fontSize = 24,
                    // textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                    textColor = new (1f, 1f, 1f, 1),
                }
            })
            .Set(LoginButtons.Ok)
            .Set(UIInteractionState.None);

        footerMenu.AddChild(okButtonEntity);
        menu.AddChild(footerMenu);
        root.AddChild(loginErrorLabel);
        root.AddChild(menu);
    }

    private static void ButtonHandler(
        Res<NetClient> network,
        State<GameState> state,
        Query<
            Data<LoginButtons, UIInteractionState>,
            Filter<Changed<UIInteractionState>, With<LoginErrorScene>>
        > query
    )
    {
        foreach ((var buttonType, var interaction) in query)
        {
            if (interaction.Ref != UIInteractionState.Released)
                continue;

            if (buttonType.Ref == LoginButtons.Ok)
            {
                state.Set(GameState.LoginScreen);
                network.Value.Disconnect();
            }
        }
    }

    private struct LoginErrorScene;
    private enum LoginButtons : byte
    {
        Ok
    }
}

internal struct LoginErrorsInfoEvent
{
    public LoginErrorInfo Error;
}

internal record struct LoginErrorInfo(
    string ErrorMessage
);