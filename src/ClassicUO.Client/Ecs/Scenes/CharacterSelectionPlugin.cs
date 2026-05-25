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

    // Mirrors main's CharacterSelectionGump (Game/UI/Gumps/Login/CharacterSelectionGump.cs):
    // chest background, ResizePic 0x0A28 panel, "Character Selection" header,
    // character rows, New/Delete/Prev/Next buttons. Coords copied verbatim from main.
    private static void CharacterInfoSetup(
        Commands commands,
        Res<GumpBuilder> gumpBuilder,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        EventReader<CharacterSelectionInfoEvent> reader)
    {
        var root = commands.Spawn()
            .Insert<CharacterSelectionScene>()
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
            .Insert<CharacterSelectionScene>()
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
        // CharacterSelectionGump does NOT add the chest 0x014E — wallpaper
        // shows through behind the 0x0A28 panel.
        mainMenu.AddChild(gumpBuilder.Value.AddGumpTiled(
            commands, 0x0150, XnaVector3.UnitZ,
            new XnaVector2(0, 0), new XnaVector2(640, 480))
            .Insert<CharacterSelectionScene>());
        mainMenu.AddChild(gumpBuilder.Value.AddGump(
            commands, 0x0151, XnaVector3.UnitZ, new XnaVector2(0, 4))
            .Insert<CharacterSelectionScene>());

        // Inner panel 0x0A28 — sized per main (408 x 343).
        mainMenu.AddChild(gumpBuilder.Value.AddGumpNinePatch(
            commands, 0x0A28, XnaVector3.UnitZ,
            new XnaVector2(160, 70), new XnaVector2(408, 343))
            .Insert<CharacterSelectionScene>());

        var labelColor = new ClayColor(255, 234, 196, 255);

        // Header.
        AddLabel(commands, mainMenu, "Character Selection", 267, 106, labelColor);

        // Matches main's CharacterEntryGump: single click highlights the
        // name (SelectCharacter), double click logs in (LoginCharacter).
        // No UiDoubleClick in Bevy.UI yet, so synthesize double-click via
        // last-click timestamp on the selected row. Threshold mirrors
        // typical OS double-click (500 ms).
        const long DoubleClickWindowMs = 500;
        var selectedRef = new SelectedRowState { Index = uint.MaxValue, LastClickTicks = 0 };
        var rows = new List<(uint Index, ulong LabelEnt, string Name)>();
        var normalColor = labelColor;
        var highlightColor = new ClayColor(255, 100, 100, 255);

        // Character rows.
        var yOffset = 150;
        var posInList = 0;
        foreach (var ev in reader.Read())
        {
            if (ev.Characters == null) continue;

            foreach (var character in ev.Characters)
            {
                if (string.IsNullOrEmpty(character.Name)) continue;

                var capturedCharacter = character;
                var idx = character.Index;

                // Shared click handler — single-click highlights, double-click
                // within window logs in. Attached to both row and label so a
                // click on the name text is treated the same as a click on
                // the row's tan border (Clay's hit-test routes to whichever
                // entity is topmost under the cursor).
                Action<Commands> handleRowClick = innerCmd =>
                {
                    var now = System.Environment.TickCount64;

                    if (selectedRef.Index == idx
                        && (now - selectedRef.LastClickTicks) <= DoubleClickWindowMs)
                    {
                        network.Value.Send_SelectCharacter(
                            capturedCharacter.Index,
                            capturedCharacter.Name,
                            network.Value.LocalIP,
                            gameCtx.Value.Protocol);
                        return;
                    }

                    selectedRef.Index = idx;
                    selectedRef.LastClickTicks = now;
                    foreach (var (rowIdx, lbl, _) in rows)
                    {
                        var color = rowIdx == idx ? highlightColor : normalColor;
                        innerCmd.Entity(lbl).Insert(new TextColor(color));
                    }
                };

                // Tan ResizePic bg (0x0BB8) — same frame main draws around
                // each character row (CharacterEntryGump.ctor in main).
                var rowEnt = gumpBuilder.Value.AddGumpNinePatch(
                    commands, 0x0BB8, XnaVector3.UnitZ,
                    new XnaVector2(224, yOffset + posInList * 40),
                    new XnaVector2(280, 30))
                    .Insert<CharacterSelectionScene>()
                    .Insert(character)
                    .Insert(Interaction.None)
                    .Observe((On<UiClick> _, Commands innerCmd) => handleRowClick(innerCmd));

                var labelEnt = SpawnLabelChild(commands, rowEnt, character.Name, 100, 8, normalColor);
                // Make the label itself clickable so clicking on the name
                // (not just the tan border) selects/logs in the row.
                commands.Entity(labelEnt)
                    .Insert(Interaction.None)
                    .Observe((On<UiClick> _, Commands innerCmd) => handleRowClick(innerCmd));
                rows.Add((idx, labelEnt, character.Name));
                mainMenu.AddChild(rowEnt);
                posInList++;
            }
        }

        // Default highlight: main's CharacterSelectionGump starts with the
        // first character pre-selected (_selectedCharacter = 0). Mirror that
        // so Next/Enter acts on a sensible default without a prior click.
        if (rows.Count > 0)
        {
            selectedRef.Index = rows[0].Index;
            selectedRef.LastClickTicks = 0;
            commands.Entity(rows[0].LabelEnt).Insert(new TextColor(highlightColor));
        }

        // New character button (Buttons.New = 0x159D/0x159F/0x159E).
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x159D, 0x159F, 0x159E), XnaVector3.UnitZ, new XnaVector2(224, 350))
            .Insert<CharacterSelectionScene>());

        // Delete button (0x159A/0x159C/0x159B).
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x159A, 0x159C, 0x159B), XnaVector3.UnitZ, new XnaVector2(442, 350))
            .Insert<CharacterSelectionScene>());

        // Prev arrow — mirrors main's CharacterSelectionGump Buttons.Prev:
        // loginScene.StepBack(). On CharacterSelection that disconnects the
        // socket and returns to the login screen (LoginSteps.Main).
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x15A1, 0x15A3, 0x15A2), XnaVector3.UnitZ, new XnaVector2(586, 445))
            .Insert<CharacterSelectionScene>()
            .Observe((On<UiClick> _, ResMut<NextState<GameState>> state) =>
            {
                network.Value.Disconnect();
                state.Value.Set(GameState.LoginScreen);
            }));

        // Next arrow — mirrors main's CharacterSelectionGump Buttons.Next:
        // LoginCharacter(_selectedCharacter). Use the highlighted row, or
        // first row if user hasn't clicked one yet.
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x15A4, 0x15A6, 0x15A5), XnaVector3.UnitZ, new XnaVector2(610, 445))
            .Insert<CharacterSelectionScene>()
            .Observe((On<UiClick> _) =>
            {
                if (rows.Count == 0) return;
                var idx = selectedRef.Index;
                (uint Index, ulong LabelEnt, string Name) entry = default;
                if (idx != uint.MaxValue)
                {
                    foreach (var r in rows)
                    {
                        if (r.Index == idx) { entry = r; break; }
                    }
                }
                if (entry.Name == null) entry = rows[0];
                network.Value.Send_SelectCharacter(
                    entry.Index, entry.Name,
                    network.Value.LocalIP,
                    gameCtx.Value.Protocol);
            }));
    }

    private static void AddLabel(
        Commands commands, EntityCommands parent,
        string text, int x, int y, ClayColor color)
    {
        var label = commands.Spawn()
            .Insert<CharacterSelectionScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(x),
                Top = Val.Px(y),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = 0, Size = 14 })
            .Insert(new TextColor(color));
        parent.AddChild(label);
    }

    private static void AddLabelChild(
        Commands commands, EntityCommands parent,
        string text, int x, int y, ClayColor color)
    {
        SpawnLabelChild(commands, parent, text, x, y, color);
    }

    // Spawn label and return its entity id so the caller can mutate
    // TextColor on selection events.
    private static ulong SpawnLabelChild(
        Commands commands, EntityCommands parent,
        string text, int x, int y, ClayColor color)
    {
        var label = commands.Spawn()
            .Insert<CharacterSelectionScene>()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(x),
                Top = Val.Px(y),
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = 0, Size = 14 })
            .Insert(new TextColor(color));
        parent.AddChild(label);
        return label.Id;
    }

    private struct CharacterSelectionScene;

    // Mutable closure box for the click handler to track which row is
    // currently highlighted plus the time of the last click on that row
    // (drives the double-click → login window).
    private sealed class SelectedRowState
    {
        public uint Index;
        public long LastClickTicks;
    }
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
