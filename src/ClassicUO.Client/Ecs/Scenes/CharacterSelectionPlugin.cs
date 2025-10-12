using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct CharacterSelectionPlugin : IPlugin
{
    public void Build(App app)
    {
        var cleanupFn = Cleanup;
        var characterInfoSetupFn = CharacterInfoSetup;
        var characterSelectedFn = CharacterSelected;

        app
            .AddSystem(cleanupFn)
            .OnExit(GameState.CharacterSelection)
            .Build()

            .AddSystem(characterInfoSetupFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state, EventReader<CharacterSelectionInfoEvent> reader)
                       => reader.HasEvents && state.Value.Current == GameState.CharacterSelection)
            .Build()

            .AddSystem(characterSelectedFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.CharacterSelection)
            .Build();
    }

    private static void Cleanup(Commands commands, Query<Data<UINode>, Filter<With<CharacterSelectionScene>, Without<Parent>>> query)
    {
        foreach ((var ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
    }

    private static void CharacterInfoSetup(Commands commands, EventReader<CharacterSelectionInfoEvent> reader)
    {
        var root = commands.Spawn()
            .Insert<CharacterSelectionScene>()
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

        var characterSelectionLabel = commands.Spawn()
            .Insert<CharacterSelectionScene>()
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
                Value = "Select the character",
                TextConfig =
                {
                    fontId = 0,
                    fontSize = 28,
                    // textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                    textColor = new (1f, 1f, 1f, 1),
                }
            });

        var menu = commands.Spawn()
            .Insert<CharacterSelectionScene>()
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
                        childGap = 4,
                    },
                    clip = {
                        vertical = true
                    }
                }
            });

        root.AddChild(characterSelectionLabel);
        root.AddChild(menu);


        foreach (var ev in reader.Read())
        {
            if (ev.Characters == null) continue;

            foreach (var character in ev.Characters)
            {
                var characterEnt = commands.Spawn()
                    .Insert<CharacterSelectionScene>()
                    .Insert(character)
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
                        Value = character.Name,
                        TextConfig =
                        {
                            fontId = 0,
                            fontSize = 24,
                            // textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            textColor = new (1f, 1f, 1f, 1),
                        }
                    })
                    .Insert(new UIMouseAction());

                menu.AddChild(characterEnt);
            }
        }
    }

    private static void CharacterSelected(
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        Query<
            Data<CharacterInfo, UIMouseAction>,
            Filter<Changed<UIMouseAction>, With<CharacterSelectionScene>>
        > query)
    {
        foreach ((var characterInfo, var interaction) in query)
        {
            if (interaction.Ref is { IsPressed: true, Button: MouseButtonType.Left })
            {
                network.Value.Send_SelectCharacter(
                    characterInfo.Ref.Index,
                    characterInfo.Ref.Name,
                    network.Value.LocalIP,
                    gameCtx.Value.Protocol
                );
            }
        }
    }

    private struct CharacterSelectionScene;
}

internal struct CharacterSelectionInfoEvent
{
    public List<CharacterInfo> Characters;
    public List<TownInfo> Towns;
}

internal record struct CharacterInfo(
    string Name,
    uint Index
);

internal record struct TownInfo(
    byte Index,
    string Name,
    string Building,
    (ushort X, ushort Y, sbyte Z) Position,
    uint Map,
    uint ClilocDescription
);
