using System.Collections.Generic;
using ClassicUO.Network;
using ClassicUO.Input;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.UI.Clay;
using TinyEcs.UI.Bevy;
using TinyEcs.UI;

namespace ClassicUO.Ecs;

internal readonly struct ServerSelectionPlugin : IPlugin
{
    public void Build(App app)
    {
        var cleanupFn = Cleanup;
        var serverInfoSetupFn = ServerInfoSetup;
        // var serverSelectedFn = ServerSelected;

        app
            .AddSystem(cleanupFn)
            .OnExit(GameState.ServerSelection)
            .Build()

            .AddSystem(serverInfoSetupFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state, EventReader<ServerSelectionInfoEvent> reader)
                       => reader.HasEvents && state.Value.Current == GameState.ServerSelection)
            .Build();

        // TODO: Re-enable server selection handling with Clay's event system
        // .AddSystem(serverSelectedFn)
        // .InStage(Stage.Update)
        // .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.ServerSelection)
        // .Build();
    }


    private static void Cleanup(Commands commands, Query<Data<ClayNode>, Filter<With<ServerSelectionScene>, Without<Parent>>> query)
    {
        foreach ((var ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
    }

    private static void ServerInfoSetup(Commands commands, EventReader<ServerSelectionInfoEvent> reader)
    {
        var root = commands.Spawn()
            .Insert<ServerSelectionScene>()
            .Insert(ClayNode.Configure()
                .WidthGrow()
                .HeightGrow()
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                .Background(51, 51, 51, 255)
                .Build());

        var serverSelectionLabel = commands.Spawn()
            .Insert<ServerSelectionScene>()
            .Insert(ClayNode.Configure()
                .WidthPercent(0.5f)
                .HeightFit()
                .Column()
                .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP)
                .Padding(8)
                .Gap(4)
                .Background(76, 76, 76, 255)
                .CornerRadius(8)
                .Text("Select the server", 28, new Clay_Color(255, 255, 255, 255))
                .Build());

        var menuNode = ClayNode.Configure()
            .WidthPercent(0.5f)
            .HeightPercent(0.5f)
            .Column()
            .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP)
            .Padding(8)
            .Gap(4)
            .Background(76, 76, 76, 255)
            .CornerRadius(8)
            .Build();

        menuNode.Clip = new Clay_ClipElementConfig { vertical = true };

        var menu = commands.Spawn()
            .Insert<ServerSelectionScene>()
            .Insert(menuNode)
            .Observe<On<ClayPointerEvent>, Res<NetClient>, Query<Data<ServerInfo>, With<ServerSelectionScene>>>(ServerSelected);

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
                    .Insert(ClayNode.Configure()
                        .WidthPercent(0.8f)
                        .HeightFit()
                        .Column()
                        .Align(Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER, Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER)
                        .Padding(8)
                        .Gap(4)
                        .Background(153, 153, 153, 255)
                        .CornerRadius(8)
                        .Text(server.Name, 24, new Clay_Color(255, 255, 255, 255))
                        .Build());

                // TODO: Add interaction handling through Clay's event system
                // For now, we'll need to handle server selection differently

                menu.AddChild(serverEnt);
            }
        }
    }

    private static void ServerSelected(
        On<ClayPointerEvent> trigger,
        Res<NetClient> network,
        Query<Data<ServerInfo>, With<ServerSelectionScene>> query
    )
    {
        if (!trigger.Event.IsLeftButton || trigger.Event.EventType != ClayPointerEventType.Click)
            return;

        if (!query.Contains(trigger.EntityId))
            return;

        var (_, serverInfo) = query.Get(trigger.EntityId);
        network.Value.Send_SelectServer((byte)serverInfo.Ref.Index);
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
