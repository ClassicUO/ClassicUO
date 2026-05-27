using System.Collections.Generic;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

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

    // Mirrors main's ServerSelectionGump (Game/UI/Gumps/Login/ServerSelectionGump.cs):
    // chest background, header labels, sort buttons, ResizePic 0x0DAC scroll area,
    // one row per server, Prev/Next arrows. Coords copied verbatim from main.
    private static void ServerInfoSetup(
        Commands commands,
        Res<GumpBuilder> gumpBuilder,
        Res<NetClient> network,
        EventReader<ServerSelectionInfoEvent> reader)
    {
        // Root: full window, top-left anchored. mainMenu = chest's 640x480 canvas.
        var root = commands.Spawn()
            .Insert<ServerSelectionScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Width = Val.Percent(100),
                Height = Val.Percent(100),
            });

        var mainMenu = commands.Spawn()
            .Insert<ServerSelectionScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Start,
                AlignItems = AlignItems.Start,
                Width = Val.Px(640),
                Height = Val.Px(480),
            });
        root.AddChild(mainMenu);

        // Tiled wallpaper + UO flag (LoginBackground parity). Main's
        // ServerSelectionGump does NOT add the chest 0x014E — only wallpaper
        // shows through behind the scroll panel.
        mainMenu.AddChild(gumpBuilder.Value.AddGumpTiled(
            commands, 0x0150, XnaVector3.UnitZ,
            new XnaVector2(0, 0), new XnaVector2(640, 480))
            .Insert<ServerSelectionScene>());
        mainMenu.AddChild(gumpBuilder.Value.AddGump(
            commands, 0x0151, XnaVector3.UnitZ, new XnaVector2(0, 4))
            .Insert<ServerSelectionScene>());

        // Prev arrow (back to login).
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x15A1, 0x15A3, 0x15A2), XnaVector3.UnitZ, new XnaVector2(586, 445))
            .Insert<ServerSelectionScene>());

        // Next arrow (confirm selection).
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x15A4, 0x15A6, 0x15A5), XnaVector3.UnitZ, new XnaVector2(610, 445))
            .Observe((On<UiClick> _) =>
            {
                // main's Next/Earth: SelectServer(GetServerIndexFromSettings()).
                // We don't track the highlight; just confirm the first server.
                network.Value.Send_SelectServer(0);
            })
            .Insert<ServerSelectionScene>());

        // Header labels (textColor 0x0481 approximated by light cream).
        var labelColor = new ClayColor(255, 234, 196, 255);
        AddLabel(commands, mainMenu, "Select which shard to play on:", 155, 70, labelColor);
        AddLabel(commands, mainMenu, "Latency:", 400, 70, labelColor);
        AddLabel(commands, mainMenu, "Packet Loss:", 470, 70, labelColor);
        AddLabel(commands, mainMenu, "Sort by:", 153, 368, labelColor);

        // Sort buttons (TimeZone / Full / Connection).
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x093B, 0x093C, 0x093D), XnaVector3.UnitZ, new XnaVector2(230, 366))
            .Insert<ServerSelectionScene>());
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x093E, 0x093F, 0x0940), XnaVector3.UnitZ, new XnaVector2(338, 366))
            .Insert<ServerSelectionScene>());
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x0941, 0x0942, 0x0943), XnaVector3.UnitZ, new XnaVector2(446, 366))
            .Insert<ServerSelectionScene>());

        // World globe background + Earth button.
        mainMenu.AddChild(gumpBuilder.Value.AddGump(
            commands, 0x0589, XnaVector3.UnitZ, new XnaVector2(150, 390))
            .Insert<ServerSelectionScene>());
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x15E8, 0x15EA, 0x15E9), XnaVector3.UnitZ, new XnaVector2(160, 400))
            .Observe((On<UiClick> _) => network.Value.Send_SelectServer(0))
            .Insert<ServerSelectionScene>());

        // Scroll-area background — ResizePic 0x0DAC sized to match main (393-14 x 271).
        mainMenu.AddChild(gumpBuilder.Value.AddGumpNinePatch(
            commands, 0x0DAC, XnaVector3.UnitZ,
            new XnaVector2(150, 90), new XnaVector2(393 - 14, 271))
            .Insert<ServerSelectionScene>());

        // Server rows. Main reserves Y=106..356 inside the scroll bg (16 px top
        // pad). Stack rows 25 px tall starting at Y=106.
        var rowY = 106;
        foreach (var ev in reader.Read())
        {
            if (ev.Servers == null) continue;

            foreach (var server in ev.Servers)
            {
                var capturedServer = server;
                var rowEnt = commands.Spawn()
                    .Insert<ServerSelectionScene>()
                    .Insert(server)
                    .Insert(new Node
                    {
                        PositionType = PositionType.Absolute,
                        Left = Val.Px(150 + 5),
                        Top = Val.Px(rowY),
                        Width = Val.Px(370),
                        Height = Val.Px(25),
                    })
                    .Insert(Interaction.None)
                    .Observe((On<UiClick> _) =>
                    {
                        network.Value.Send_SelectServer((byte)capturedServer.Index);
                    });

                AddLabelChild(commands, rowEnt, server.Name, 74, 4, labelColor);
                AddLabelChild(commands, rowEnt, "-", 250, 4, labelColor);
                AddLabelChild(commands, rowEnt, "-", 320, 4, labelColor);

                mainMenu.AddChild(rowEnt);
                rowY += 25;
            }
        }
    }

    private static void AddLabel(
        Commands commands, EntityCommands parent,
        string text, int x, int y, ClayColor color)
    {
        var label = commands.Spawn()
            .Insert<ServerSelectionScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(x),
                Top = Val.Px(y),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = 0, Size = 12 })
            .Insert(new TextColor(color));
        parent.AddChild(label);
    }

    private static void AddLabelChild(
        Commands commands, EntityCommands parent,
        string text, int x, int y, ClayColor color)
    {
        var label = commands.Spawn()
            .Insert<ServerSelectionScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(x),
                Top = Val.Px(y),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = 0, Size = 12 })
            .Insert(new TextColor(color));
        parent.AddChild(label);
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
