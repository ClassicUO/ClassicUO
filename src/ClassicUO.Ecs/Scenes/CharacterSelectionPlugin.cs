using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Utility;
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
        // Selection lives in a resource, not a closure box: the 0xA9 list can be
        // re-pushed (e.g. after a character delete) while still in
        // CharacterSelection, which re-runs the setup system and rebuilds the
        // rows — a closure-captured selectedRef/rows would then be stale (rule 4).
        app.AddResource(new CharacterSelectionState());

        // Single click selects/highlights a row. Global observer keyed on the
        // CharacterInfo every row + label carries (same proven pattern as the
        // double-click observer below); recolours all name labels by querying
        // them, instead of capturing a per-build `rows` list.
        app.AddObserver((
            On<UiClick> trig,
            Commands commands,
            ResMut<CharacterSelectionState> sel,
            Query<Data<CharacterInfo>> charQ,
            Query<Data<CharacterInfo, TextColor>> labelQ) =>
        {
            if (!charQ.TryGet(trig.EntityId, out var charRow)) return;
            var (_, ch) = charRow;
            sel.Value.SelectedIndex = ch.Ref.Index;

            var normal = UoFontRuntime.AsciiHue(0x034F);
            var highlight = UoFontRuntime.AsciiHue(0x0021);
            foreach (var (lbl, info, _) in labelQ)
                commands.Entity(lbl.Ref).Insert(new TextColor(info.Ref.Index == ch.Ref.Index ? highlight : normal));
        });

        app.AddObserver((
            On<UiDoubleClick> trig,
            Res<NetClient> net,
            Res<GameContext> ctx,
            ResMut<ProfileContext> profileCtx,
            Query<Data<CharacterInfo>> charQ) =>
        {
            if (!charQ.TryGet(trig.EntityId, out var charRow)) return;
            var (_, ch) = charRow;
            profileCtx.Value.CharacterName = ch.Ref.Name;
            net.Value.Send_SelectCharacter(ch.Ref.Index, ch.Ref.Name, net.Value.LocalIP, ctx.Value.Protocol);
        });
    }

    private static void Cleanup(
        Commands commands,
        Query<Data<Node>, Filter<With<CharacterSelectionScene>>> query)
        => LoginSceneHelpers.DespawnAll<CharacterSelectionScene>(commands, query);

    // Mirrors main's CharacterSelectionGump (Game/UI/Gumps/Login/CharacterSelectionGump.cs):
    // chest background, ResizePic 0x0A28 panel, "Character Selection" header,
    // character rows, New/Delete/Prev/Next buttons. Coords copied verbatim from main.
    private static void CharacterInfoSetup(
        Commands commands,
        Res<GumpBuilder> gumpBuilder,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        ResMut<CharacterSelectionState> selState,
        Query<Data<Node>, Filter<With<CharacterSelectionScene>>> existing,
        EventReader<CharacterSelectionInfoEvent> reader)
    {
        // A re-pushed 0xA9 (e.g. after a character delete) re-runs this system
        // while still in CharacterSelection — despawn the previous panel first so
        // rows don't stack. Selection state is the resource, not a closure, so
        // there is nothing stale to carry across rebuilds.
        foreach ((var ent, _) in existing)
            commands.Entity(ent.Ref).Despawn();
        selState.Value.SelectedIndex = uint.MaxValue;

        // Drain the list up front so the panel can size to the char count before
        // spawning (legacy reads loginScene.Characters first). Empty slots are
        // already stripped by the 0xA9 parser.
        var chars = new List<CharacterInfo>();
        foreach (var ev in reader.Read())
        {
            if (ev.Characters == null) continue;
            foreach (var c in ev.Characters)
                if (!string.IsNullOrEmpty(c.Name)) chars.Add(c);
        }

        // Legacy CharacterSelectionGump layout: modern clients (>= CV_6040), or
        // 5020+ with >5 chars, grow the panel and shift title/rows/New+Delete down
        // by yBonus so 6–7 rows fit; otherwise the 5-slot layout.
        int listTitleY = 106, yOffset = 150, yBonus = 0;
        var ver = gameCtx.Value.ClientVersion;
        if (ver >= ClientVersion.CV_6040 || (ver >= ClientVersion.CV_5020 && chars.Count > 5))
        {
            listTitleY = 96;
            yOffset = 125;
            yBonus = 45;
        }
        var maxChars = MaxCharacterSlots(gameCtx.Value.ClientFeatures);
        var locked = gameCtx.Value.LockedFeatures;

        var mainMenu = LoginSceneHelpers.SpawnCanvas<CharacterSelectionScene>(commands);

        // Tiled wallpaper + UO flag (LoginBackground parity). Main's
        // CharacterSelectionGump does NOT add the chest 0x014E — wallpaper
        // shows through behind the 0x0A28 panel.
        LoginSceneHelpers.AddWallpaper<CharacterSelectionScene>(commands, gumpBuilder.Value, mainMenu, new XnaVector2(640, 480));

        // Inner panel 0x0A28 — sized per main (408 x 343, +yBonus for 6–7 slots).
        mainMenu.AddChild(gumpBuilder.Value.AddGumpNinePatch(
            commands, 0x0A28, XnaVector3.UnitZ,
            new XnaVector2(160, 70), new XnaVector2(408, 343 + yBonus))
            .Insert<CharacterSelectionScene>());

        // Title: OOP draws it ASCII font 2, hue 0x0386. TextColor carries the
        // hue (renderer bakes it; font 2 is partial so the glyph keeps its
        // near-white native colour).
        var titleColor = UoFontRuntime.AsciiHue(0x0386);

        // Header.
        AddLabel(commands, mainMenu, "Character Selection", 267, listTitleY, titleColor);

        // Matches main's CharacterEntryGump: single click highlights the
        // name (SelectCharacter), double click logs in (LoginCharacter).
        // Bevy.UI emits UiDoubleClick (two UiClicks within its
        // DoubleClickWindow on the same entity), so route login through an
        // On<UiDoubleClick> observer — no hand-rolled timestamp gap.
        // OOP CharacterEntryGump: ASCII font 5, NORMAL_COLOR 0x034F,
        // SELECTED_COLOR 0x0021 (red). TextColor carries the hue.
        var normalColor = UoFontRuntime.AsciiHue(0x034F);
        var highlightColor = UoFontRuntime.AsciiHue(0x0021);
        ulong firstLabelEnt = 0;
        uint firstIndex = uint.MaxValue;

        // Character rows.
        var posInList = 0;
        var valid = 0;
        foreach (var character in chars)
        {
            // Legacy per-slot caps: stop at ClientFeatures.MaxChars, and at 5
            // when the account isn't unlocked for the 6th/7th slot.
            valid++;
            if (valid > maxChars) break;
            if (locked != 0 && (locked & LockedFeatureFlags.SeventhCharacterSlot) == 0
                && valid == 6 && (locked & LockedFeatureFlags.SixthCharacterSlot) == 0)
                break;

            var idx = character.Index;

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
                .Insert(Interaction.None);

            var labelEnt = SpawnRowLabel(commands, rowEnt, character.Name, normalColor);
            // Clicking the name (not just the tan border) selects too, and the
            // label carries CharacterInfo so single/double clicks on the text
            // route through the same global observers as the tan border.
            commands.Entity(labelEnt)
                .Insert(Interaction.None)
                .Insert(character);
            if (firstLabelEnt == 0)
            {
                firstLabelEnt = labelEnt;
                firstIndex = idx;
            }
            mainMenu.AddChild(rowEnt);
            posInList++;
        }

        // Default highlight: main's CharacterSelectionGump starts with the
        // first character pre-selected (_selectedCharacter = 0). Mirror that
        // so Next/Enter acts on a sensible default without a prior click.
        if (firstLabelEnt != 0)
        {
            selState.Value.SelectedIndex = firstIndex;
            commands.Entity(firstLabelEnt).Insert(new TextColor(highlightColor));
        }

        // New character button (Buttons.New = 0x159D/0x159F/0x159E) —
        // mirrors main's Buttons.New: loginScene.StartCharCreation(). Hidden once
        // used slots reach the server's cap (legacy CanCreateChar / MaxChars).
        if (posInList < maxChars)
        {
            mainMenu.AddChild(gumpBuilder.Value.AddButton(
                commands, (0x159D, 0x159F, 0x159E), XnaVector3.UnitZ, new XnaVector2(224, 350 + yBonus))
                .Insert<CharacterSelectionScene>()
                .Observe((On<UiClick> _, ResMut<NextState<GameState>> state) =>
                    state.Value.Set(GameState.CharacterCreation)));
        }

        // Delete button (0x159A/0x159C/0x159B).
        mainMenu.AddChild(gumpBuilder.Value.AddButton(
            commands, (0x159A, 0x159C, 0x159B), XnaVector3.UnitZ, new XnaVector2(442, 350 + yBonus))
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
            .Observe((On<UiClick> _,
                      Res<CharacterSelectionState> sel,
                      Res<NetClient> net,
                      Res<GameContext> ctx,
                      ResMut<ProfileContext> profileCtx,
                      Query<Data<CharacterInfo>> charQ) =>
            {
                var idx = sel.Value.SelectedIndex;
                if (idx == uint.MaxValue) return;
                foreach (var (_, ci) in charQ)
                {
                    if (ci.Ref.Index != idx) continue;
                    profileCtx.Value.CharacterName = ci.Ref.Name;
                    net.Value.Send_SelectCharacter(
                        ci.Ref.Index, ci.Ref.Name,
                        net.Value.LocalIP, ctx.Value.Protocol);
                    return;
                }
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

    // Server-driven character-slot cap (legacy ClientFeatures.MaxChars).
    private static uint MaxCharacterSlots(CharacterListFlags feats)
    {
        if ((feats & CharacterListFlags.CLF_ONE_CHARACTER_SLOT) != 0) return 1;
        if ((feats & CharacterListFlags.CLF_7_CHARACTER_SLOT) != 0) return 7;
        if ((feats & CharacterListFlags.CLF_6_CHARACTER_SLOT) != 0) return 6;
        return 5;
    }

    // Which character row is currently highlighted. A resource (not a closure
    // box) so it survives a 0xA9 re-push that rebuilds the rows; the global
    // click/Next observers read it without capturing per-build state.
    internal sealed class CharacterSelectionState
    {
        public uint SelectedIndex = uint.MaxValue;
    }
}

// Scene root marker — promoted to namespace-level internal so the modding
// registry (same assembly) can key a WIT scene path to it.
// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct CharacterSelectionScene;

internal struct CharacterSelectionInfoEvent
{
    public List<CharacterInfo> Characters;
    public List<TownInfo> Towns;
}

// Last 0xA9 character list, kept as a resource (the event above is consumed
// the frame it arrives). Character creation reads Towns for the city map and
// Characters for the first free slot index.
internal sealed class CharacterListSnapshot
{
    public List<CharacterInfo> Characters = new();
    public List<TownInfo> Towns = new();
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
