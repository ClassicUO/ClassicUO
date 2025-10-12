using ClassicUO.Network;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct LoginErrorScreenPlugin : IPlugin
{
    public void Build(App app)
    {
        var cleanupFn = Cleanup;
        var loginErrorSetupFn = LoginErrorInfoSetup;
        var buttonHandlerFn = ButtonHandler;

        app
            .AddSystem(cleanupFn)
            .OnExit(GameState.LoginError)
            .Build()

            .AddSystem(loginErrorSetupFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state, EventReader<LoginErrorsInfoEvent> reader)
                       => reader.HasEvents && state.Value.Current == GameState.LoginError)
            .Build()

            .AddSystem(buttonHandlerFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.LoginError)
            .Build();
    }

    private static void Cleanup(Commands commands, Query<Data<UINode>, Filter<With<LoginErrorScene>, Without<Parent>>> query)
    {
        foreach ((var ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
    }

    private static void LoginErrorInfoSetup(Commands commands, EventReader<LoginErrorsInfoEvent> reader)
    {
        var root = commands.Spawn()
            .Insert<LoginErrorScene>()
            .CreateUINode(new UINode()
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

        var loginErrorLabel = commands.Spawn()
            .Insert<LoginErrorScene>()
            .CreateUINode(new UINode()
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
            .Insert(new Text()
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

        var menu = commands.Spawn()
            .Insert<LoginErrorScene>()
            .CreateUINode(new UINode()
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

        foreach (var ev in reader.Read())
        {
            var serverEnt = commands.Spawn()
                .Insert<LoginErrorScene>()
                .Insert(ev.Error)
                .CreateUINode(new UINode()
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
                .Insert(new Text()
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
                .Insert(new UIMouseAction());

            menu.AddChild(serverEnt);
        }

        var footerMenu = commands.Spawn()
            .Insert<LoginErrorScene>()
            .CreateUINode(new UINode()
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

        var okButtonEntity = commands.Spawn()
            .Insert<LoginErrorScene>()
            .CreateUINode(new UINode()
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
            .Insert(new Text()
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
            .Insert(LoginButtons.Ok)
            .Insert(new UIMouseAction());

        footerMenu.AddChild(okButtonEntity);
        menu.AddChild(footerMenu);
        root.AddChild(loginErrorLabel);
        root.AddChild(menu);
    }

    private static void ButtonHandler(
        Res<NetClient> network,
        Res<NextState<GameState>> state,
        Query<
            Data<LoginButtons, UIMouseAction>,
            Filter<Changed<UIMouseAction>, With<LoginErrorScene>>
        > query
    )
    {
        foreach ((var buttonType, var interaction) in query)
        {
            if (!interaction.Ref.IsPressed)
                continue;

            if (buttonType.Ref == LoginButtons.Ok)
            {
                state.Value.Set(GameState.LoginScreen);
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
