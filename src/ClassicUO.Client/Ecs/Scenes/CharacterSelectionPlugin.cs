using System.Collections.Generic;
using ClassicUO.Network;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct CharacterSelectionPlugin : IPlugin
{
    public void Build(App app)
    {
        var cleanupFn = Cleanup;
        var characterInfoSetupFn = CharacterInfoSetup;

        app
            .AddSystem(cleanupFn)
            .OnExit(GameState.CharacterSelection)
            .Build()

            .AddSystem(characterInfoSetupFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state, EventReader<CharacterSelectionInfoEvent> reader)
                       => reader.HasEvents && state.Value.Current == GameState.CharacterSelection)
            .Build();
    }

    private static void Cleanup(
        Commands commands,
        Query<Data<Node>, Filter<With<CharacterSelectionScene>>> query)
    {
        foreach ((var ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
    }

    private static void CharacterInfoSetup(
        Commands commands,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        EventReader<CharacterSelectionInfoEvent> reader)
    {
        // Root: full-screen vertical column, centered.
        var root = commands.Spawn()
            .Insert<CharacterSelectionScene>()
            .Insert(new Node
            {
                Width = Val.Percent(100f),
                Height = Val.Percent(100f),
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Padding = UiRect.All(8),
                Gap = Val.Px(4),
            })
            .Insert(new BackgroundColor(new ClayColor(51, 51, 51, 255)));
        var rootId = root.Id;

        // Title label.
        var header = commands.Spawn()
            .Insert<CharacterSelectionScene>()
            .Insert(new Node
            {
                Width = Val.Percent(50f),
                Height = Val.Auto,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Padding = UiRect.All(8),
                Gap = Val.Px(4),
            })
            .Insert(new BackgroundColor(new ClayColor(76, 76, 76, 255)))
            .Insert(BorderRadius.All(8))
            .Insert(new Text("Select the character"))
            .Insert(new TextFont { FontId = 0, Size = 28 })
            .Insert(new TextColor(ClayColor.White));
        commands.AddChild(rootId, header.Id);

        // Scrollable menu container.
        var menu = commands.Spawn()
            .Insert<CharacterSelectionScene>()
            .Insert(new Node
            {
                Width = Val.Percent(50f),
                Height = Val.Percent(50f),
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Center,
                Padding = UiRect.All(8),
                Gap = Val.Px(4),
                Overflow = Overflow.Scroll,
            })
            .Insert(new BackgroundColor(new ClayColor(76, 76, 76, 255)))
            .Insert(BorderRadius.All(8))
            .Insert(new ScrollPosition());
        commands.AddChild(rootId, menu.Id);
        var menuId = menu.Id;

        foreach (var ev in reader.Read())
        {
            if (ev.Characters == null) continue;

            foreach (var character in ev.Characters)
            {
                var capturedCharacter = character;
                var entry = commands.Spawn()
                    .Insert<CharacterSelectionScene>()
                    .Insert(character)
                    .Insert(new Node
                    {
                        Width = Val.Percent(80f),
                        Height = Val.Auto,
                        FlexDirection = FlexDirection.Column,
                        JustifyContent = JustifyContent.Center,
                        AlignItems = AlignItems.Center,
                        Padding = UiRect.All(8),
                        Gap = Val.Px(4),
                    })
                    .Insert(new BackgroundColor(new ClayColor(153, 153, 153, 255)))
                    .Insert(BorderRadius.All(8))
                    .Insert(new Text(character.Name))
                    .Insert(new TextFont { FontId = 0, Size = 24 })
                    .Insert(new TextColor(ClayColor.White))
                    .Insert(Interaction.None)
                    .Insert(new FocusPolicy { Block = true })
                    .Observe<On<UiClick>>(_ =>
                    {
                        network.Value.Send_SelectCharacter(
                            capturedCharacter.Index,
                            capturedCharacter.Name,
                            network.Value.LocalIP,
                            gameCtx.Value.Protocol
                        );
                    });
                commands.AddChild(menuId, entry.Id);
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
