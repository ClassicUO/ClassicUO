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

        // Double-click a character row (or its name label) logs that character
        // in — mirrors OOP CharacterEntryGump's double-click = LoginCharacter.
        // Global observer keyed on the CharacterInfo component each row + label
        // carries (the proven UiDoubleClick pattern; entity-scoped .Observe is
        // only used for UiClick). Bevy.UI synthesizes UiDoubleClick from two
        // UiClicks within its DoubleClickWindow.
        app.AddObserver((
            On<UiDoubleClick> trig,
            Res<NetClient> net,
            Res<GameContext> ctx,
            Query<Data<CharacterInfo>> charQ) =>
        {
            if (!charQ.Contains(trig.EntityId)) return;
            var (_, ch) = charQ.Get(trig.EntityId);
            net.Value.Send_SelectCharacter(ch.Ref.Index, ch.Ref.Name, net.Value.LocalIP, ctx.Value.Protocol);
        });
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

        // Title: OOP draws it ASCII font 2, hue 0x0386. TextColor carries the
        // hue (renderer bakes it; font 2 is partial so the glyph keeps its
        // near-white native colour).
        var titleColor = UoFontRuntime.AsciiHue(0x0386);

        // Header.
        AddLabel(commands, mainMenu, "Character Selection", 267, 106, titleColor);

        // Matches main's CharacterEntryGump: single click highlights the
        // name (SelectCharacter), double click logs in (LoginCharacter).
        // Bevy.UI emits UiDoubleClick (two UiClicks within its
        // DoubleClickWindow on the same entity), so route login through an
        // On<UiDoubleClick> observer — no hand-rolled timestamp gap.
        var selectedRef = new SelectedRowState { Index = uint.MaxValue };
        var rows = new List<(uint Index, ulong LabelEnt, string Name)>();
        // OOP CharacterEntryGump: ASCII font 5, NORMAL_COLOR 0x034F,
        // SELECTED_COLOR 0x0021 (red). TextColor carries the hue.
        var normalColor = UoFontRuntime.AsciiHue(0x034F);
        var highlightColor = UoFontRuntime.AsciiHue(0x0021);

        // Character rows.
        var yOffset = 150;
        var posInList = 0;
        foreach (var ev in reader.Read())
        {
            if (ev.Characters == null) continue;

            foreach (var character in ev.Characters)
            {
                if (string.IsNullOrEmpty(character.Name)) continue;

                var idx = character.Index;

                // Single-click selects/highlights the row. Attached to both row
                // and label so a click on the name text behaves like a click on
                // the tan border (Clay routes to whichever entity is topmost).
                Action<Commands> handleRowSelect = innerCmd =>
                {
                    selectedRef.Index = idx;
                    foreach (var (rowIdx, lbl, _) in rows)
                    {
                        var color = rowIdx == idx ? highlightColor : normalColor;
                        innerCmd.Entity(lbl).Insert(new TextColor(color));
                    }
                };

                // Tan ResizePic bg (0x0BB8) — same frame main draws around
                // each character row (CharacterEntryGump.ctor in main). Carries
                // the CharacterInfo so the global UiDoubleClick observer can log
                // it in.
                // Center the name in the 280x30 row via flex (both axes). The row
                // is a custom-render (GumpNinePatch) node; its gump fill now paints
                // BEFORE its children (Clay.NET ClayContext custom-command order
                // fix), so a flow label renders on top — no Absolute / manual
                // measure. Mirrors OOP CharacterEntryGump (Label maxwidth 270,
                // TS_CENTER). The override Node is the last Node insert, which wins.
                var rowTop = yOffset + posInList * 40;
                var rowEnt = gumpBuilder.Value.AddGumpNinePatch(
                    commands, 0x0BB8, XnaVector3.UnitZ,
                    new XnaVector2(224, rowTop),
                    new XnaVector2(280, 30))
                    .Insert(new Node
                    {
                        Display = Display.Flex,
                        PositionType = PositionType.Absolute,
                        Left = Val.Px(224), Top = Val.Px(rowTop),
                        Width = Val.Px(280), Height = Val.Px(30),
                        JustifyContent = JustifyContent.Center,
                        AlignItems = AlignItems.Center,
                    })
                    .Insert<CharacterSelectionScene>()
                    .Insert(character)
                    .Insert(Interaction.None)
                    .Observe((On<UiClick> _, Commands innerCmd) => handleRowSelect(innerCmd));

                var labelEnt = SpawnRowLabel(commands, rowEnt, character.Name, normalColor);
                // Clicking the name (not just the tan border) selects too, and
                // the label carries CharacterInfo so a double-click on the text
                // logs in via the same global observer.
                commands.Entity(labelEnt)
                    .Insert(Interaction.None)
                    .Insert(character)
                    .Observe((On<UiClick> _, Commands innerCmd) => handleRowSelect(innerCmd));
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
            .Insert(new TextFont { FontId = (ushort)(2 | UoFontRuntime.AsciiFlag), Size = 14 })
            .Insert(new TextColor(color));
        parent.AddChild(label);
    }

    private static void AddLabelChild(
        Commands commands, EntityCommands parent,
        string text, int x, int y, ClayColor color)
    {
        SpawnLabelChild(commands, parent, text, x, y, color);
    }

    // Row name label: a flow (non-absolute) child so the row's
    // JustifyContent/AlignItems = Center centers it in both axes. Auto size lets
    // Clay measure the glyph run. Returns the id so selection can recolour it.
    private static ulong SpawnRowLabel(
        Commands commands, EntityCommands parent, string text, ClayColor color)
    {
        var label = commands.Spawn()
            .Insert<CharacterSelectionScene>()
            .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = (ushort)(5 | UoFontRuntime.AsciiFlag), Size = 14 })
            .Insert(new TextColor(color));
        parent.AddChild(label);
        return label.Id;
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
            .Insert(new TextFont { FontId = (ushort)(5 | UoFontRuntime.AsciiFlag), Size = 14 })
            .Insert(new TextColor(color));
        parent.AddChild(label);
        return label.Id;
    }

    private struct CharacterSelectionScene;

    // Mutable closure box tracking which row is currently highlighted, so the
    // Next arrow logs in the selected character.
    private sealed class SelectedRowState
    {
        public uint Index;
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
