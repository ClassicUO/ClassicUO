using System.Collections.Generic;
using ClassicUO.Network;
using Clay_cs;
using TinyEcs;

namespace ClassicUO.Ecs;

[TinyPlugin]
internal readonly partial struct ServerSelectionPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<ServerSelectionInfoEvent>();

        var cleanupFn = Cleanup;

        scheduler.OnExit(GameState.ServerSelection, cleanupFn, ThreadingMode.Single);
    }


    private static void Cleanup(Query<Data<UINode>, Filter<With<ServerSelectionScene>, Without<Parent>>> query)
    {
        foreach ((var ent, _) in query)
        {
            ent.Ref.Delete();
        }
    }


    private static bool HasServerSelectionInfoEvent(EventReader<ServerSelectionInfoEvent> reader) => !reader.IsEmpty;

    private static bool IsInServerSelection(SchedulerState state) => state.InState(GameState.ServerSelection);



    [TinySystem(Stages.Update, ThreadingMode.Single)]
    [RunIf(nameof(HasServerSelectionInfoEvent))]
    [RunIf(nameof(IsInServerSelection))]
    private static void ServerInfoSetup(World world, EventReader<ServerSelectionInfoEvent> reader)
    {
        var root = world.Entity()
            .Add<ServerSelectionScene>()
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

        var serverSelectionLabel = world.Entity()
            .Add<ServerSelectionScene>()
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
                Value = "Select the server",
                TextConfig =
                {
                    fontId = 0,
                    fontSize = 28,
                    // textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                    textColor = new (1f, 1f, 1f, 1),
                }
            });

        var menu = world.Entity()
            .Add<ServerSelectionScene>()
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
                    },
                    clip = {
                        vertical = true
                    }
                }
            });

        root.AddChild(serverSelectionLabel);
        root.AddChild(menu);


        foreach (var ev in reader)
        {
            if (ev.Servers == null) continue;

            foreach (var server in ev.Servers)
            {
                var serverEnt = world.Entity()
                    .Add<ServerSelectionScene>()
                    .Set(server)
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
                        Value = server.Name,
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
        }
    }


    [TinySystem(Stages.Update, ThreadingMode.Single)]
    [RunIf(nameof(IsInServerSelection))]
    private static void ServerSelected(
        Res<NetClient> network,
        Query<
            Data<ServerInfo, UIInteractionState>,
            Filter<Changed<UIInteractionState>, With<ServerSelectionScene>>
        > query)
    {
        foreach ((var serverInfo, var interaction) in query)
        {
            if (interaction.Ref == UIInteractionState.Released)
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
