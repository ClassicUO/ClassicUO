using System;
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
        var hoverFn = ServerNameHover;

        // One-shot send latch (legacy LoginScene.SelectServer is idempotent via
        // the login-step guard; without it a double-click sends the packet twice).
        app.AddResource(new ServerSelectLatch());

        app
            .AddSystem(cleanupFn)
            .OnExit(GameState.ServerSelection)
            .Build()

            // Re-arm the latch each time the screen is entered.
            .AddSystem((ResMut<ServerSelectLatch> latch) => latch.Value.Sent = false)
            .OnEnter(GameState.ServerSelection)
            .Build()

            .AddSystem(serverInfoSetupFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state, EventReader<ServerSelectionInfoEvent> reader)
                       => reader.HasEvents && state.Value.Current == GameState.ServerSelection)
            .Build()

            // Highlight a server row's name while the row is hovered (legacy
            // HoveredLabel: normal 0x034F → selected 0x0021).
            .AddSystem(hoverFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.ServerSelection)
            .Build();
    }

    private static void Cleanup(
        Commands commands,
        Query<Data<Node>, Filter<With<ServerSelectionScene>>> query)
        => LoginSceneHelpers.DespawnAll<ServerSelectionScene>(commands, query);

    // Mirrors main's ServerSelectionGump (Game/UI/Gumps/Login/ServerSelectionGump.cs):
    // chest background, header labels, sort buttons, ResizePic 0x0DAC scroll area,
    // one row per server, Prev/Next arrows. Coords copied verbatim from main.
    private static void ServerInfoSetup(
        Commands commands,
        Res<GumpBuilder> gumpBuilder,
        EventReader<ServerSelectionInfoEvent> reader)
    {
        // Root + chest's 640x480 canvas.
        var mainMenu = LoginSceneHelpers.SpawnCanvas<ServerSelectionScene>(commands);

        // Tiled wallpaper + UO flag (LoginBackground parity). Main's
        // ServerSelectionGump does NOT add the chest 0x014E — only wallpaper
        // shows through behind the scroll panel.
        LoginSceneHelpers.AddWallpaper<ServerSelectionScene>(commands, gumpBuilder.Value, mainMenu, new XnaVector2(640, 480));

        // Prev arrow (back to login).
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x15A1, 0x15A3, 0x15A2), XnaVector3.UnitZ, new XnaVector2(586, 445))
            .Insert<ServerSelectionScene>());

        // Next arrow (confirm selection). main's Next/Earth:
        // SelectServer(GetServerIndexFromSettings()). We don't track the
        // highlight; just confirm the first server.
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x15A4, 0x15A6, 0x15A5), XnaVector3.UnitZ, new XnaVector2(610, 445))
            .Observe((On<UiClick> _, ResMut<ServerSelectLatch> latch, Res<NetClient> net) =>
                SelectServer(latch.Value, net.Value, 0))
            .Insert<ServerSelectionScene>());

        // Header labels: OOP (CV>=500A) draws these unicode, font 1, hue
        // 0xFFFF — i.e. plain white. Match exactly.
        var headerColor = new ClayColor(255, 255, 255, 255);
        AddLabel(commands, mainMenu, "Select which shard to play on:", 155, 70, headerColor);
        AddLabel(commands, mainMenu, "Latency:", 400, 70, headerColor);
        AddLabel(commands, mainMenu, "Packet Loss:", 470, 70, headerColor);
        AddLabel(commands, mainMenu, "Sort by:", 153, 368, headerColor);

        // Row text: OOP ServerEntryGump uses ASCII font 5, hue 0x034F. The
        // TextColor carries the hue (renderer bakes it per pixel).
        var rowColor = UoFontRuntime.AsciiHue(0x034F);

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
            .Observe((On<UiClick> _, ResMut<ServerSelectLatch> latch, Res<NetClient> net) =>
                SelectServer(latch.Value, net.Value, 0))
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
                var serverIndex = (byte)server.Index;

                // Click anywhere on the row selects (latched so a double-click
                // sends once). The name label is an absolute child = a Clay
                // floating element, which captures the pointer over the text — so
                // it needs its OWN click+Interaction (else a click/hover on the
                // name wouldn't reach the row). ServerNameHover checks both the
                // row and the name label so hover works over the whole row.
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
                    .Observe((On<UiClick> _, ResMut<ServerSelectLatch> latch, Res<NetClient> net) =>
                        SelectServer(latch.Value, net.Value, serverIndex));

                var nameLabel = AddLabelChild(commands, rowEnt, server.Name, 74, 4, rowColor);
                commands.Entity(nameLabel)
                    .Insert(Interaction.None)
                    .Observe((On<UiClick> _, ResMut<ServerSelectLatch> latch, Res<NetClient> net) =>
                        SelectServer(latch.Value, net.Value, serverIndex));
                AddLabelChild(commands, rowEnt, "-", 250, 4, rowColor);
                AddLabelChild(commands, rowEnt, "-", 320, 4, rowColor);
                commands.Entity(rowEnt.Id).Insert(new ServerRowUI { NameLabel = nameLabel });

                mainMenu.AddChild(rowEnt);
                rowY += 25;
            }
        }
    }

    // Send the SelectServer packet at most once per visit (legacy
    // LoginScene.SelectServer guards on the login step; ECS uses a latch).
    private static void SelectServer(ServerSelectLatch latch, NetClient net, byte index)
    {
        if (latch.Sent) return;
        latch.Sent = true;
        net.Send_SelectServer(index);
    }

    // Recolour each row's name label to the selected hue while the row OR its
    // (floating, pointer-capturing) name label is hovered (legacy HoveredLabel
    // OnMouseEnter/Exit highlights on row hover).
    private static void ServerNameHover(
        Query<Data<Interaction, ServerRowUI>> rowsQ,
        Query<Data<Interaction>> interQ,
        Query<Data<TextColor>> colorsQ)
    {
        var over = UoFontRuntime.AsciiHue(0x0021);
        var normal = UoFontRuntime.AsciiHue(0x034F);
        foreach (var (_, rowInter, row) in rowsQ)
        {
            var labelId = row.Ref.NameLabel;
            if (!colorsQ.Contains(labelId)) continue;

            bool hovered = IsHover(rowInter.Ref);
            if (!hovered && interQ.Contains(labelId))
            {
                var (_, labelInter) = interQ.Get(labelId);
                hovered = IsHover(labelInter.Ref);
            }

            var (_, color) = colorsQ.Get(labelId);
            color.Ref = new TextColor(hovered ? over : normal);
        }
    }

    private static bool IsHover(Interaction i) => i == Interaction.Hovered || i == Interaction.Pressed;

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
            .Insert(new TextFont { FontId = 1, Size = 12 })
            .Insert(new TextColor(color));
        parent.AddChild(label);
    }

    private static ulong AddLabelChild(
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
            .Insert(new TextFont { FontId = (ushort)(5 | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(color));
        parent.AddChild(label);
        return label.Id;
    }

    // Links a server row to its name label so the hover system can recolour it.
    private struct ServerRowUI { public ulong NameLabel; }
}

// Scene root marker — promoted to namespace-level internal so the modding
// registry (same assembly) can key a WIT scene path to it.
// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct ServerSelectionScene;

// One-shot guard so a double-click on a server row (or Next/Earth) sends the
// SelectServer packet once. Re-armed on entering the screen.
internal sealed class ServerSelectLatch { public bool Sent; }

// cuo:modding contract type — do not merge/rename (queried by WIT path).
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
