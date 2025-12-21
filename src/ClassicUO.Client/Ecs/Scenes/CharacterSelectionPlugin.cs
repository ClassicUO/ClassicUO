using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Clay;
using TinyEcs.UI.Bevy;
using TinyEcs.UI;

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

    private static void Cleanup(Commands commands, Query<Data<ClayNode>, Filter<With<CharacterSelectionScene>, Without<Parent>>> query)
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
            .Insert(ClayNode.Configure()
                .WidthGrow()
                .HeightGrow()
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                .Background(51, 51, 51, 255)
                .Build());

        var characterSelectionLabel = commands.Spawn()
            .Insert<CharacterSelectionScene>()
            .Insert(ClayNode.Configure()
                .WidthPercent(0.5f)
                .HeightFit()
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP)
                .Padding(8)
                .Gap(4)
                .Background(76, 76, 76, 255)
                .Text("Select the character", 28, new Clay_Color(255, 255, 255, 255))
                .Build());

        var menuNode = ClayNode.Configure()
            .WidthPercent(0.5f)
            .HeightPercent(0.5f)
            .Column()
            .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP)
            .Padding(8)
            .Gap(4)
            .Background(76, 76, 76, 255)
            .Build();

        menuNode.Clip = new Clay_ClipElementConfig { vertical = true };

        var menu = commands.Spawn()
            .Insert<CharacterSelectionScene>()
            .Insert(menuNode);

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
                    .Insert(ClayNode.Configure()
                        .WidthPercent(0.8f)
                        .HeightFit()
                        .Column()
                        .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                        .Padding(8)
                        .Gap(4)
                        .Background(153, 153, 153, 255)
                        .CornerRadius(8)
                        .Text(character.Name, 24, new Clay_Color(255, 255, 255, 255))
                        .Build())
                    .Observe<On<ClayPointerEvent>, Res<NetClient>, Res<GameContext>, Query<Data<CharacterInfo>, With<CharacterSelectionScene>>>(CharacterSelected);

                menu.AddChild(characterEnt);
            }
        }
    }

    private static void CharacterSelected(
        On<ClayPointerEvent> trigger,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        Query<Data<CharacterInfo>, With<CharacterSelectionScene>> query
    )
    {
        if (!trigger.Event.IsLeftButton || trigger.Event.EventType != ClayPointerEventType.Click)
            return;

        if (!query.Contains(trigger.EntityId))
            return;

        var (_, characterInfo) = query.Get(trigger.EntityId);
        network.Value.Send_SelectCharacter(
            characterInfo.Ref.Index,
            characterInfo.Ref.Name,
            network.Value.LocalIP,
            gameCtx.Value.Protocol
        );
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
