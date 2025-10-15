using System.Collections.Generic;
using ClassicUO.Network;
using ClassicUO.Input;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct ServerSelectionPlugin : IPlugin
{
    public void Build(App app)
    {
        var cleanupFn = Cleanup;
        var serverInfoSetupFn = ServerInfoSetup;
        var serverSelectedFn = ServerSelected;

        app
            .AddSystem(cleanupFn)
            .OnExit(GameState.ServerSelection)
            .Build()

            .AddSystem(serverInfoSetupFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state, EventReader<ServerSelectionInfoEvent> reader)
                       => reader.HasEvents && state.Value.Current == GameState.ServerSelection)
            .Build()

            .AddSystem(serverSelectedFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.ServerSelection)
            .Build();
    }


    private static void Cleanup(Commands commands, Query<Data<UINode>, Filter<With<ServerSelectionScene>, Without<Parent>>> query)
    {
        foreach ((var ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
    }

    private static void ServerInfoSetup(Commands commands, EventReader<ServerSelectionInfoEvent> reader)
    {
        var cornerRadius = Clay_CornerRadius.All(8);

        var root = commands.Spawn()
            .Insert<ServerSelectionScene>()
            .InsertBundle(new UINodeBundle()
            {
                Node = new UINode()
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
                }
            });

        var serverSelectionLabel = commands.Spawn()
    .Insert<ServerSelectionScene>()
    .InsertBundle(new UINodeBundle()
    {
        Node = new UINode()
        {
            Config = {
                cornerRadius = cornerRadius,
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
        }
    })
    .Insert(new Text()
    {
        Value = "Select the server",
        TextConfig =
        {
                    fontId = 0,
                    fontSize = 28,
                    // textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                    textColor = new (1f, 1f, 1f, 1),
        }
    });

        var menu = commands.Spawn()
    .Insert<ServerSelectionScene>()
    .InsertBundle(new UINodeBundle()
    {
        Node = new UINode()
        {
            Config = {
                cornerRadius = cornerRadius,
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
                },
                clip = {
                    vertical = true
                }
            }
        }
    });

        root.AddChild(serverSelectionLabel);
        root.AddChild(menu);


        foreach (var ev in reader.Read())
        {
            if (ev.Servers == null) continue;

            foreach (var server in ev.Servers)
            {
                var serverEnt = commands.Spawn()
                    .Insert<ServerSelectionScene>()
                    .Insert(server)
                    .InsertBundle(new UINodeBundle()
                    {
                        Node = new UINode()
                        {
                            Config = {
                            cornerRadius = cornerRadius,
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
                        }
                    })
                    .Insert(new Text()
                    {
                        Value = server.Name,
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
        }
    }

    private static void ServerSelected(
        Res<NetClient> network,
        Query<
            Data<ServerInfo, UIMouseAction>,
            Filter<Changed<UIMouseAction>, With<ServerSelectionScene>>
        > query)
    {
        foreach ((var serverInfo, var interaction) in query)
        {
            if (interaction.Ref is { WasPressed: true, IsPressed: false, Button: MouseButtonType.Left })
            {
                network.Value.Send_SelectServer((byte)serverInfo.Ref.Index);
            }
        }
    }

    private struct ServerSelectionScene;
}


internal struct ServerSelectionInfoEvent
{
    public List<ServerInfo> Servers;
}

internal record struct ServerInfo(
    int Index,
    string Name,
    byte PercentFull,
    byte TimeZone,
    uint Ip
);
