using System.Collections.Generic;
using ClassicUO.Network;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct ServerSelectionPlugin : IPlugin
{
    public void Build(App app)
    {
        var cleanupFn = Cleanup;
        var serverInfoSetupFn = ServerInfoSetup;

        app
            .AddSystem(cleanupFn)
            .OnExit(GameState.ServerSelection)
            .Build()

            .AddSystem(serverInfoSetupFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state, EventReader<ServerSelectionInfoEvent> reader)
                       => reader.HasEvents && state.Value.Current == GameState.ServerSelection)
            .Build();
    }

    private static void Cleanup(
        Commands commands,
        Query<Data<Node>, Filter<With<ServerSelectionScene>>> query)
    {
        foreach ((var ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
    }

    private static void ServerInfoSetup(
        Commands commands,
        Res<NetClient> network,
        EventReader<ServerSelectionInfoEvent> reader)
    {
        // Root: full-screen vertical column, centered.
        var root = commands.Spawn()
            .Insert<ServerSelectionScene>()
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
            .Insert<ServerSelectionScene>()
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
            .Insert(new Text("Select the server"))
            .Insert(new TextFont { FontId = 0, Size = 28 })
            .Insert(new TextColor(ClayColor.White));
        commands.AddChild(rootId, header.Id);

        // Scrollable menu container.
        var menu = commands.Spawn()
            .Insert<ServerSelectionScene>()
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
            if (ev.Servers == null) continue;

            foreach (var server in ev.Servers)
            {
                var capturedServer = server;
                var entry = commands.Spawn()
                    .Insert<ServerSelectionScene>()
                    .Insert(server)
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
                    .Insert(new Text(server.Name))
                    .Insert(new TextFont { FontId = 0, Size = 24 })
                    .Insert(new TextColor(ClayColor.White))
                    .Insert(Interaction.None)
                    .Insert(new FocusPolicy { Block = true })
                    .Observe<On<UiClick>>(_ =>
                    {
                        network.Value.Send_SelectServer((byte)capturedServer.Index);
                    });
                commands.AddChild(menuId, entry.Id);
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
