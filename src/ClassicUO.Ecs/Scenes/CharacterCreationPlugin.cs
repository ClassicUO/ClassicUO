// Port of main's Game/UI/Gumps/CharCreation/* to the ECS branch.
//
// The legacy flow is a single paged gump (CharCreationGump) hosting four step
// pages. Here each step is a sub-state (CharCreationStep) entered from
// GameState.CharacterCreation; every step spawns its UI under a per-step root
// so OnExit teardown is one despawn (children cascade). All cross-step choices
// (gender/race/hues/profession/stats/skills/city) live in the
// CharCreationContext resource — never in closures (rebuilds would reset them).
//
// Coordinates are copied verbatim from main:
//   CreateCharAppearanceGump.cs / CreateCharProfessionGump.cs /
//   CreateCharTradeGump.cs / CreateCharCityGump.cs.

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using ClayColor = Clay.Color;
using MathHelperUO = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Ecs;

internal enum CharCreationStep : byte
{
    Inactive,
    Appearance,
    Profession,
    Trade,
    City
}

internal readonly struct CharacterCreationPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new CharacterListSnapshot());
        app.AddResource(new CharCreationContext());
        app.AddState(CharCreationStep.Inactive);

        var setupFn = Setup;
        var teardownFn = Teardown;
        var spawnAppearanceFn = SpawnAppearance;
        var rebuildAppearanceFn = RebuildAppearanceDynamic;
        var spawnProfessionFn = SpawnProfessionChrome;
        var rebuildProfessionFn = RebuildProfessionEntries;
        var spawnTradeFn = SpawnTrade;
        var spawnCityFn = SpawnCityChrome;
        var rebuildCityFn = RebuildCity;
        var sliderLabelsFn = UpdateSliderValueLabels;

        app
            .AddSystem(setupFn)
            .OnEnter(GameState.CharacterCreation)
            .Build()

            .AddSystem(teardownFn)
            .OnExit(GameState.CharacterCreation)
            .Build()

            .AddSystem(spawnAppearanceFn)
            .OnEnter(CharCreationStep.Appearance)
            .Build()

            .AddSystem((Commands cmd, Query<Data<Node>, Filter<With<AppearanceUI>>> q) => DespawnAll(cmd, q))
            .OnExit(CharCreationStep.Appearance)
            .Build()

            .AddSystem(rebuildAppearanceFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<CharCreationStep>> s, Res<CharCreationContext> ctx)
                => s.Value.Current == CharCreationStep.Appearance && ctx.Value.AppearanceDirty)
            .Build()

            .AddSystem(spawnProfessionFn)
            .OnEnter(CharCreationStep.Profession)
            .Build()

            .AddSystem((Commands cmd, Query<Data<Node>, Filter<With<ProfessionUI>>> q) => DespawnAll(cmd, q))
            .OnExit(CharCreationStep.Profession)
            .Build()

            .AddSystem(rebuildProfessionFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<CharCreationStep>> s, Res<CharCreationContext> ctx)
                => s.Value.Current == CharCreationStep.Profession && ctx.Value.ProfessionDirty)
            .Build()

            .AddSystem(spawnTradeFn)
            .OnEnter(CharCreationStep.Trade)
            .Build()

            .AddSystem((Commands cmd, Query<Data<Node>, Filter<With<TradeUI>>> q) => DespawnAll(cmd, q))
            .OnExit(CharCreationStep.Trade)
            .Build()

            // Spawned from Update (not OnEnter — see SpawnAppearance note) and
            // respawned when a skill pick changes context state so the combo
            // label shows the selection.
            .AddSystem((Commands cmd, CharCreationParams pp, Query<Data<Node>, Filter<With<TradeUI>>> q) =>
            {
                var c = pp.Ctx.Value;
                c.TradeDirty = false;
                DespawnAll(cmd, q);
                SpawnTradeInner(cmd, pp, reset: c.TradeNeedsReset);
                c.TradeNeedsReset = false;
            })
            .InStage(Stage.Update)
            .RunIf((Res<State<CharCreationStep>> s, Res<CharCreationContext> ctx)
                => s.Value.Current == CharCreationStep.Trade && ctx.Value.TradeDirty)
            .Build()

            .AddSystem(sliderLabelsFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<CharCreationStep>> s) => s.Value.Current == CharCreationStep.Trade)
            .Build()

            .AddSystem(spawnCityFn)
            .OnEnter(CharCreationStep.City)
            .Build()

            .AddSystem((Commands cmd, Query<Data<Node>, Filter<With<CityUI>>> q) => DespawnAll(cmd, q))
            .OnExit(CharCreationStep.City)
            .Build()

            .AddSystem(rebuildCityFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<CharCreationStep>> s, Res<CharCreationContext> ctx)
                => s.Value.Current == CharCreationStep.City && ctx.Value.CityDirty)
            .Build();
    }

    private static void DespawnAll<T>(Commands cmd, Query<Data<Node>, Filter<With<T>>> q) where T : struct
    {
        foreach (var (ent, _) in q) cmd.Entity(ent.Ref).Despawn();
    }

    // ------------------------------------------------------------------
    // Base scene (wallpaper) + context reset
    // ------------------------------------------------------------------

    private static void Setup(
        Commands commands,
        Res<GumpBuilder> builder,
        Res<GameContext> gameCtx,
        Res<CharCreationContext> ctx,
        ResMut<NextState<CharCreationStep>> step)
    {
        var root = commands.Spawn()
            .Insert<CharCreationScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                Width = Val.Percent(100),
                Height = Val.Percent(100),
            });

        var mainMenu = commands.Spawn()
            .Insert<CharCreationScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                FlexDirection = FlexDirection.Column,
                Width = Val.Px(640),
                Height = Val.Px(480),
            });
        root.AddChild(mainMenu);

        // Tiled wallpaper + UO flag, same as the other login-scene screens.
        mainMenu.AddChild(builder.Value.AddGumpTiled(
            commands, 0x0150, Vector3.UnitZ, new Vector2(0, 0), new Vector2(640, 480))
            .Insert<CharCreationScene>());
        mainMenu.AddChild(builder.Value.AddGump(
            commands, 0x0151, Vector3.UnitZ, new Vector2(0, 4))
            .Insert<CharCreationScene>());

        ctx.Value.Reset(gameCtx.Value.ClientVersion);
        ctx.Value.MenuEntity = mainMenu.Id;

        step.Value.Set(CharCreationStep.Appearance);
    }

    private static void Teardown(
        Commands commands,
        Query<Data<Node>, Filter<With<CharCreationScene>>> q,
        ResMut<NextState<CharCreationStep>> step)
    {
        foreach (var (ent, _) in q) commands.Entity(ent.Ref).Despawn();
        step.Value.Set(CharCreationStep.Inactive);
    }

    // ------------------------------------------------------------------
    // Step 1: appearance
    // ------------------------------------------------------------------

    // IMPORTANT: spawn step UI from Update-stage rebuild systems, NOT from the
    // OnEnter(CharCreationStep.*) handlers — entities spawned during the
    // sub-state transition pass render fine but never receive Interaction
    // updates (hover/press/UiClick all dead). OnEnter only creates the step
    // root and flips the dirty flag; the rebuild system does the real spawn.
    private static void SpawnAppearance(Commands commands, CharCreationParams p)
    {
        var ctx = p.Ctx.Value;
        var stepRoot = SpawnStepRoot(commands, ctx.MenuEntity);
        stepRoot.Insert<AppearanceUI>();
        ctx.AppearanceStepRoot = stepRoot.Id;
        ctx.AppearanceDirty = true;
    }

    // The whole appearance page. Rebuilt wholesale on any gender/race/hue/hair
    // change (mirrors legacy HandleGenreChange / HandleRaceChanged which remove
    // + re-add the controls).
    private static void RebuildAppearanceDynamic(Commands commands, CharCreationParams p,
        Query<Data<Node>, Filter<With<AppearanceDynamic>>> dynQ,
        Query<Data<Text>, Filter<With<CharNameInput>>> nameQ)
    {
        var ctx = p.Ctx.Value;
        ctx.AppearanceDirty = false;

        // Keep whatever the user typed so the rebuild doesn't wipe the name.
        foreach (var (_, t) in nameQ)
        {
            ctx.Name = t.Ref.Value ?? string.Empty;
        }

        foreach (var (ent, _) in dynQ) commands.Entity(ent.Ref).Despawn();

        var builder = p.Builder.Value;
        var files = p.Files.Value;
        var version = p.GameCtx.Value.ClientVersion;
        var race = ctx.Race;
        var isFemale = ctx.IsFemale;

        // Race availability is server-driven, not client-version-driven. Legacy
        // CreateCharAppearanceGump: elf = CLF_ELVEN_RACE + ML expansion;
        // gargoyle = SA expansion (the CV_60144 art guard still applies).
        var locks = p.GameCtx.Value.LockedFeatures;
        var feats = p.GameCtx.Value.ClientFeatures;
        bool allowElf = (feats & CharacterListFlags.CLF_ELVEN_RACE) != 0 && locks.HasFlag(LockedFeatureFlags.ML);
        bool allowGarg = locks.HasFlag(LockedFeatureFlags.SA);

        var dyn = commands.Spawn()
            .Insert<AppearanceDynamic>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(640), Height = Val.Px(480),
            });
        commands.AddChild(ctx.AppearanceStepRoot, dyn.Id);
        var dynId = dyn.Id;

        // --- chrome: option panels + name banner ---
        commands.AddChild(dynId, builder.AddGumpNinePatch(
            commands, 0x0E10, Vector3.UnitZ, new Vector2(82, 125), new Vector2(151, 310)).Id);
        commands.AddChild(dynId, builder.AddGumpNinePatch(
            commands, 0x0E10, Vector3.UnitZ, new Vector2(475, 125), new Vector2(151, 310)).Id);
        commands.AddChild(dynId, builder.AddGump(commands, 0x0709, Vector3.UnitZ, new Vector2(280, 53)).Id);
        commands.AddChild(dynId, builder.AddGump(commands, 0x070A, Vector3.UnitZ, new Vector2(240, 73)).Id);
        commands.AddChild(dynId, builder.AddGumpTiled(
            commands, 0x070B, Vector3.UnitZ, new Vector2(248, 73), new Vector2(215, 16)).Id);
        commands.AddChild(dynId, builder.AddGump(commands, 0x070C, Vector3.UnitZ, new Vector2(463, 73)).Id);
        commands.AddChild(dynId, builder.AddGump(commands, 0x0708, Vector3.UnitZ, new Vector2(238, 98)).Id);

        // Name entry (legacy StbTextBox font 5 hue 1 at 257,65 200x20). The
        // frame is an invisible custom leaf so the whole box is focusable.
        var nameFrame = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(257), Top = Val.Px(65),
                Width = Val.Px(200), Height = Val.Px(20),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None } });
        var nameFieldFont = new TextFont { FontId = (ushort)(5 | UoFontRuntime.AsciiFlag), Size = 16 };
        var nameTextId = GuiPlugin.SpawnTextField(
            commands, nameFrame, new Vector2(2, 1), nameFieldFont, UoFontRuntime.AsciiHue(1),
            ctx.Name ?? string.Empty, masked: false);
        commands.Entity(nameTextId).Insert<CharNameInput>();
        commands.AddChild(dynId, nameFrame.Id);

        // Prev — mirrors CharCreationGump.StepBack on the first page:
        // loginScene.StepBack → back to the character list. Legacy rebuilds
        // CharacterSelectionGump from the cached Characters list (no new 0xA9);
        // here CharacterInfoSetup is event-driven, so replay the event from the
        // snapshot or the selection screen comes up empty.
        commands.AddChild(dynId, builder.AddButton(
            commands, (0x15A1, 0x15A3, 0x15A2), Vector3.UnitZ, new Vector2(586, 445))
            .Observe((
                On<UiClick> _,
                Res<CharacterListSnapshot> snap,
                EventWriter<CharacterSelectionInfoEvent> charWriter,
                ResMut<NextState<GameState>> state) =>
            {
                state.Value.Set(GameState.CharacterSelection);
                charWriter.Send(new CharacterSelectionInfoEvent
                {
                    Characters = snap.Value.Characters,
                    Towns = snap.Value.Towns
                });
            }).Id);

        // Next — validate the name, then go to profession selection.
        commands.AddChild(dynId, builder.AddButton(
            commands, (0x15A4, 0x15A6, 0x15A5), Vector3.UnitZ, new Vector2(610, 445))
            .Observe((
                On<UiClick> trig,
                Commands cmd,
                Res<CharCreationContext> c,
                Res<GameContext> g,
                Res<UOFileManager> filesInner,
                Res<GumpBuilder> b,
                Res<AssetsServer> assets,
                Res<UiSurface> surface,
                Res<UiZCounter> zc,
                ResMut<NextState<CharCreationStep>> next,
                Single<Data<Text>, Filter<With<CharNameInput>>> nameQInner) =>
            {
                (_, var nameText) = nameQInner.Get();
                var name = nameText.Ref.Value ?? string.Empty;
                int invalid = ValidateName(name, g.Value.ClientVersion);
                if (invalid > 0)
                {
                    var msg = filesInner.Value.Clilocs.GetString(invalid) ?? "Invalid name.";
                    MessageBoxGumpPlugin.Open(cmd, b.Value, assets.Value, surface.Value, zc.Value,
                        400, 300, msg, MessageButtonType.OK);
                    return;
                }

                c.Value.Name = name;
                next.Value.Set(CharCreationStep.Profession);
            }).Id);

        // --- gender radios (0x0768 off / 0x0767 on) + word buttons ---
        SpawnRadio(commands, builder, dynId, new Vector2(425, 435), !isFemale,
            static c => { if (c.IsFemale) { c.IsFemale = false; c.HairIndex = 1; c.BeardIndex = 0; c.AppearanceDirty = true; } });
        SpawnRadio(commands, builder, dynId, new Vector2(425, 455), isFemale,
            static c => { if (!c.IsFemale) { c.IsFemale = true; c.HairIndex = 1; c.BeardIndex = 0; c.AppearanceDirty = true; } });
        SpawnWordButton(commands, builder, dynId, (0x0710, 0x0712, 0x0711), new Vector2(445, 435),
            static c => { if (c.IsFemale) { c.IsFemale = false; c.HairIndex = 1; c.BeardIndex = 0; c.AppearanceDirty = true; } });
        SpawnWordButton(commands, builder, dynId, (0x070D, 0x070F, 0x070E), new Vector2(445, 455),
            static c => { if (!c.IsFemale) { c.IsFemale = true; c.HairIndex = 1; c.BeardIndex = 0; c.AppearanceDirty = true; } });

        // --- race radios + word buttons ---
        SpawnRadio(commands, builder, dynId, new Vector2(180, 435), race == RaceType.HUMAN,
            static c => SelectRace(c, RaceType.HUMAN));
        SpawnWordButton(commands, builder, dynId, (0x0702, 0x0704, 0x0703), new Vector2(200, 435),
            static c => SelectRace(c, RaceType.HUMAN));
        if (allowElf)
        {
            SpawnRadio(commands, builder, dynId, new Vector2(180, 455), race == RaceType.ELF,
                static c => SelectRace(c, RaceType.ELF));
            SpawnWordButton(commands, builder, dynId, (0x0705, 0x0707, 0x0706), new Vector2(200, 455),
                static c => SelectRace(c, RaceType.ELF));
        }

        if (allowGarg && version >= ClientVersion.CV_60144)
        {
            SpawnRadio(commands, builder, dynId, new Vector2(60, 435), race == RaceType.GARGOYLE,
                static c => SelectRace(c, RaceType.GARGOYLE));
            SpawnWordButton(commands, builder, dynId, (0x07D3, 0x07D5, 0x07D4), new Vector2(80, 435),
                static c => SelectRace(c, RaceType.GARGOYLE));
        }

        // --- hair / facial hair combos (left panel) ---
        var (hairClilocs, hairGraphics) = CharacterCreationValues.GetHairComboContent(isFemale, race);
        var hairLabels = hairClilocs.Select(id => files.Clilocs.GetString(id) ?? id.ToString()).ToArray();
        if (ctx.HairIndex >= hairLabels.Length) ctx.HairIndex = hairLabels.Length > 0 ? 1 : 0;

        AddAsciiLabel(commands, dynId,
            files.Clilocs.GetString(race == RaceType.GARGOYLE ? 1112309 : 3000121) ?? "Hair",
            98, 140, 9, 0);
        SpawnCombo(commands, builder, dynId, ctx.AppearanceStepRoot,
            new Vector2(97, 155), 120, hairLabels, ctx.HairIndex, string.Empty, 200,
            static (i, c) => { c.HairIndex = i; c.AppearanceDirty = true; });

        bool hasFacial = !isFemale && race != RaceType.ELF;
        if (hasFacial)
        {
            var (facialClilocs, _) = CharacterCreationValues.GetFacialHairComboContent(race);
            var facialLabels = facialClilocs.Select(id => files.Clilocs.GetString(id) ?? id.ToString()).ToArray();
            if (ctx.BeardIndex >= facialLabels.Length) ctx.BeardIndex = 0;

            AddAsciiLabel(commands, dynId,
                files.Clilocs.GetString(race == RaceType.GARGOYLE ? 1112511 : 3000122) ?? "Facial Hair",
                98, 184, 9, 0);
            SpawnCombo(commands, builder, dynId, ctx.AppearanceStepRoot,
                new Vector2(97, 199), 120, facialLabels, ctx.BeardIndex, string.Empty, 200,
                static (i, c) => { c.BeardIndex = i; c.AppearanceDirty = true; });
        }

        // --- color pickers (right panel) ---
        var skinPallet = CharacterCreationValues.GetSkinPallet(race);
        SpawnColorPicker(commands, files, dynId, ctx.AppearanceStepRoot, ctx,
            489, 141, 3000183, skinPallet, 8, skinPallet.Length >> 3,
            CreationLayer.Skin);

        SpawnColorPicker(commands, files, dynId, ctx.AppearanceStepRoot, ctx,
            489, 183, 3000440, null, 10, 20, CreationLayer.Shirt);

        if (race != RaceType.GARGOYLE)
        {
            SpawnColorPicker(commands, files, dynId, ctx.AppearanceStepRoot, ctx,
                489, 225, 3000441, null, 10, 20, CreationLayer.Pants);
        }

        var hairPallet = CharacterCreationValues.GetHairPallet(race);
        SpawnColorPicker(commands, files, dynId, ctx.AppearanceStepRoot, ctx,
            489, 267, race == RaceType.GARGOYLE ? 1112322 : 3000184, hairPallet, 8, hairPallet.Length >> 3,
            CreationLayer.Hair);

        if (hasFacial)
        {
            SpawnColorPicker(commands, files, dynId, ctx.AppearanceStepRoot, ctx,
                489, 309, race == RaceType.GARGOYLE ? 1112512 : 3000446, hairPallet, 8, hairPallet.Length >> 3,
                CreationLayer.Beard);
        }

        // --- paperdoll preview ---
        SpawnPreview(commands, builder, p.Assets.Value, files, dynId, ctx, hairGraphics);
    }

    private static void SelectRace(CharCreationContext c, RaceType race)
    {
        if (c.Race == race) return;
        c.Race = race;
        c.Colors.Clear();
        c.HairIndex = 1;
        c.BeardIndex = 0;
        c.AppearanceDirty = true;
    }

    private static void SpawnRadio(
        Commands commands, GumpBuilder builder, ulong parent, Vector2 pos, bool selected,
        Action<CharCreationContext> onClick)
    {
        var ids = selected
            ? ((ushort)0x0767, (ushort)0x0767, (ushort)0x0767)
            : ((ushort)0x0768, (ushort)0x0767, (ushort)0x0768);
        var btn = builder.AddButton(commands, ids, Vector3.UnitZ, pos)
            .Observe((On<UiClick> _, Res<CharCreationContext> c) => onClick(c.Value));
        commands.AddChild(parent, btn.Id);
    }

    private static void SpawnWordButton(
        Commands commands, GumpBuilder builder, ulong parent,
        (ushort, ushort, ushort) ids, Vector2 pos,
        Action<CharCreationContext> onClick)
    {
        var btn = builder.AddButton(commands, ids, Vector3.UnitZ, pos)
            .Observe((On<UiClick> _, Res<CharCreationContext> c) => onClick(c.Value));
        commands.AddChild(parent, btn.Id);
    }

    // ------------------------------------------------------------------
    // Paperdoll preview (legacy PaperDollInteractable at 262,135 built from
    // the simulated character: body gump + worn starter clothes + hair).
    // ------------------------------------------------------------------

    private static void SpawnPreview(
        Commands commands, GumpBuilder builder, AssetsServer assets, UOFileManager files,
        ulong parent, CharCreationContext ctx, int[] hairGraphics)
    {
        var origin = new Vector2(262, 135);
        var race = ctx.Race;
        var isFemale = ctx.IsFemale;

        ushort bodyGump = race switch
        {
            RaceType.ELF => isFemale ? (ushort)0x000F : (ushort)0x000E,
            RaceType.GARGOYLE => isFemale ? (ushort)0x0299 : (ushort)0x029A,
            _ => isFemale ? (ushort)0x000D : (ushort)0x000C,
        };

        var skinHue = GetColor(ctx, CreationLayer.Skin, CharacterCreationValues.GetSkinPallet(race));
        AddPreviewLayer(commands, builder, parent, origin, bodyGump, skinHue);

        var tileData = files.TileData.StaticData;
        var shirtHue = GetColor(ctx, CreationLayer.Shirt, null);
        var pantsHue = GetColor(ctx, CreationLayer.Pants, null);

        void Item(ushort graphic, ushort hue)
        {
            if (graphic == 0 || graphic >= tileData.Length) return;
            var animId = tileData[graphic].AnimID;
            if (animId == 0) return;
            var id = (ushort)(animId + (isFemale ? Constants.FEMALE_GUMP_OFFSET : Constants.MALE_GUMP_OFFSET));
            ref readonly var info = ref assets.Gumps.GetGump(id);
            if (info.UV.Width == 0)
            {
                id = (ushort)(animId + Constants.MALE_GUMP_OFFSET);
            }
            AddPreviewLayer(commands, builder, parent, origin, id, hue);
        }

        switch (race)
        {
            case RaceType.GARGOYLE:
                Item(0x4001, shirtHue); // robe
                break;
            default:
                Item(0x1710, 0x0384); // shoes
                Item(isFemale ? (ushort)0x1531 : (ushort)0x152F, pantsHue); // skirt / pants
                Item(0x1518, shirtHue); // shirt
                break;
        }

        // Hair + facial hair (combo graphics are item ids; 0 = bald).
        if (ctx.HairIndex >= 0 && ctx.HairIndex < hairGraphics.Length)
        {
            var hairHue = GetColor(ctx, CreationLayer.Hair, CharacterCreationValues.GetHairPallet(race));
            Item((ushort)hairGraphics[ctx.HairIndex], hairHue);
        }

        if (!isFemale && race != RaceType.ELF)
        {
            var (_, facialGraphics) = CharacterCreationValues.GetFacialHairComboContent(race);
            if (ctx.BeardIndex >= 0 && ctx.BeardIndex < facialGraphics.Length)
            {
                var beardHue = GetColor(ctx, CreationLayer.Beard, CharacterCreationValues.GetHairPallet(race));
                Item((ushort)facialGraphics[ctx.BeardIndex], beardHue);
            }
        }
    }

    private static void AddPreviewLayer(
        Commands commands, GumpBuilder builder, ulong parent, Vector2 origin, ushort gumpId, ushort hue)
    {
        commands.AddChild(parent, builder.AddGump(commands, gumpId, ToShaderHue(hue), origin).Id);
    }

    // ------------------------------------------------------------------
    // Combo (legacy Combobox): closed 0x0BB8 box + label + arrow; click opens
    // an option list that despawns on selection.
    // ------------------------------------------------------------------

    private static void SpawnCombo(
        Commands commands,
        GumpBuilder builder,
        ulong parent,
        ulong overlayParent,
        Vector2 pos,
        int width,
        string[] items,
        int selectedIndex,
        string placeholder,
        int maxHeight,
        Action<int, CharCreationContext> onSelect)
    {
        var box = builder.AddGumpNinePatch(commands, 0x0BB8, Vector3.UnitZ, pos, new Vector2(width, 25))
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UiNoWindowDrag>();
        commands.AddChild(parent, box.Id);

        box.Observe((On<UiClick> _, Commands cmd) =>
            SpawnComboOverlay(cmd, builder, overlayParent, pos, width, items, maxHeight, onSelect));

        // The label and the arrow paint topmost over the box, and Clay routes
        // the click to the topmost element — without their own handlers they
        // swallow it (the CharacterSelection row-label trap). Same opener on all
        // three.
        var text = selectedIndex >= 0 && selectedIndex < items.Length ? items[selectedIndex] : placeholder;
        var label = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px((int)pos.X + 4), Top = Val.Px((int)pos.Y + 5),
                Width = Val.Auto, Height = Val.Auto,
            })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 14 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(0x0453)))
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UiNoWindowDrag>()
            .Observe((On<UiClick> _, Commands cmd) =>
                SpawnComboOverlay(cmd, builder, overlayParent, pos, width, items, maxHeight, onSelect));
        commands.AddChild(parent, label.Id);

        var arrow = builder.AddGump(commands, 0x00FC, Vector3.UnitZ,
            new Vector2(pos.X + width - 18, pos.Y + 2))
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UiNoWindowDrag>()
            .Observe((On<UiClick> _, Commands cmd) =>
                SpawnComboOverlay(cmd, builder, overlayParent, pos, width, items, maxHeight, onSelect));
        commands.AddChild(parent, arrow.Id);
    }

    private static void SpawnComboOverlay(
        Commands commands,
        GumpBuilder builder,
        ulong overlayParent,
        Vector2 pos,
        int width,
        string[] items,
        int maxHeight,
        Action<int, CharCreationContext> onSelect)
    {
        const int RowH = 15;
        int contentH = items.Length * RowH + 4;
        int listH = Math.Min(contentH, maxHeight);
        bool scrolls = contentH > maxHeight;

        var overlay = builder.AddGumpNinePatch(commands, 0x0BB8, Vector3.UnitZ, pos, new Vector2(width, listH))
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UiNoWindowDrag>();
        commands.AddChild(overlayParent, overlay.Id);
        var overlayId = overlay.Id;

        var viewport = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PositionType = PositionType.Absolute,
                Left = Val.Px(2), Top = Val.Px(2),
                Width = Val.Px(width - (scrolls ? 18 : 4)),
                Height = Val.Px(listH - 4),
                Overflow = Overflow.Scroll,
            })
            .Insert(new ScrollPosition());
        commands.AddChild(overlayId, viewport.Id);

        for (int i = 0; i < items.Length; i++)
        {
            int idx = i;
            var row = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    Width = Val.Px(width - (scrolls ? 18 : 4)),
                    Height = Val.Px(RowH),
                })
                .Insert(new Text(items[i] ?? string.Empty))
                .Insert(new TextFont { FontId = (ushort)(9 | UoFontRuntime.AsciiFlag), Size = 12 })
                .Insert(new TextColor(UoFontRuntime.AsciiHue(0x0453)))
                .Insert(Interaction.None)
                .Insert<UiContainsByBounds>()
                .Insert<UiNoWindowDrag>()
                .Observe((On<UiClick> _, Res<CharCreationContext> c, Commands cmd) =>
                {
                    onSelect(idx, c.Value);
                    cmd.Entity(overlayId).Despawn();
                });
            commands.AddChild(viewport.Id, row.Id);
        }

        if (scrolls)
        {
            commands.AddChild(overlayId, builder.AddVScrollbar(
                commands, viewport.Id, new Vector2(width - 16, 2), listH - 4).Id);
        }
    }

    // ------------------------------------------------------------------
    // Color picker (legacy CustomColorPicker): label + 121x23 swatch showing
    // the current hue; click opens the palette grid at (489,141).
    // ------------------------------------------------------------------

    private static void SpawnColorPicker(
        Commands commands,
        UOFileManager files,
        ulong parent,
        ulong overlayParent,
        CharCreationContext ctx,
        int x, int y,
        int labelCliloc,
        ushort[] pallet,
        int rows, int columns,
        CreationLayer layer)
    {
        // Default selection mirrors legacy ColorBox initial hue:
        // (pallet?[0] ?? 1) + 1.
        if (!ctx.Colors.ContainsKey(layer))
        {
            ctx.Colors[layer] = (0, (ushort)((pallet != null && pallet.Length > 0 ? pallet[0] : (ushort)1) + 1));
        }

        AddAsciiLabel(commands, parent, files.Clilocs.GetString(labelCliloc) ?? string.Empty, x, y, 9, 0);

        var current = ctx.Colors[layer].Hue;
        var swatchColors = new uint[1];
        swatchColors[0] = BakeHue(files.Hues, current);

        var swatch = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(x + 1), Top = Val.Px(y + 15),
                Width = Val.Px(121), Height = Val.Px(23),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.HueGrid,
                    GridRows = 1, GridCols = 1, CellW = 121, CellH = 23,
                    SelectedIndex = -1, GridColors = swatchColors,
                }
            })
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UiNoWindowDrag>();
        commands.AddChild(parent, swatch.Id);

        var hues = files.Hues;
        swatch.Observe((On<UiClick> _, Commands cmd, Res<CharCreationContext> c) =>
            SpawnPaletteOverlay(cmd, hues, overlayParent, pallet, rows, columns, layer,
                c.Value.Colors.TryGetValue(layer, out var cur) ? cur.Index : 0));
    }

    private static void SpawnPaletteOverlay(
        Commands commands,
        HuesLoader hues,
        ulong overlayParent,
        ushort[] pallet,
        int rows, int columns,
        CreationLayer layer,
        int selectedIndex)
    {
        // Legacy ColorPickerBox geometry: spawned at (489,141), cell sizes
        // 125/columns x 280/rows; default Graduation = 1 for the null-pallet
        // (shirt/pants) grid: hue(i) = grad + 2 + 5*i.
        int cellW = 125 / columns;
        int cellH = 280 / rows;
        int count = rows * columns;

        var gridHues = new ushort[count];
        var gridColors = new uint[count];
        for (int i = 0; i < count; i++)
        {
            ushort hue = pallet != null && i < pallet.Length
                ? (ushort)(pallet[i] + 1)
                : (ushort)(3 + 5 * i);
            gridHues[i] = hue;
            gridColors[i] = BakeHue(hues, hue);
        }

        var render = new UOCustomRender
        {
            Kind = UOCustomKind.HueGrid,
            GridRows = rows, GridCols = columns, CellW = cellW, CellH = cellH,
            SelectedIndex = selectedIndex, GridColors = gridColors,
        };

        var grid = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(489), Top = Val.Px(141),
                Width = Val.Px(columns * cellW), Height = Val.Px(rows * cellH),
            })
            .Insert(new UiCustom { Data = render })
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UiNoWindowDrag>();
        commands.AddChild(overlayParent, grid.Id);
        var gridId = grid.Id;

        grid.Observe((
            On<UiPointerDown> t,
            Query<Data<ComputedNode>> geomQ,
            Res<CharCreationContext> c,
            Commands cmd) =>
        {
            if (!geomQ.TryGet(gridId, out var geomRow)) return;
            var (_, cn) = geomRow;
            var bb = cn.Ref;
            if (bb.Size.X <= 0 || bb.Size.Y <= 0) return;
            int col = Math.Clamp((int)((t.Event.Position.X - bb.Position.X) / bb.Size.X * columns), 0, columns - 1);
            int row = Math.Clamp((int)((t.Event.Position.Y - bb.Position.Y) / bb.Size.Y * rows), 0, rows - 1);
            int idx = row * columns + col;
            c.Value.Colors[layer] = (idx, gridHues[idx]);
            c.Value.AppearanceDirty = true;
            cmd.Entity(gridId).Despawn();
        });
    }

    private static uint BakeHue(HuesLoader hues, ushort hue)
    {
        uint c = hues.GetPolygoneColor(30, hue);
        if ((c >> 24) == 0) c |= 0xFF000000u;
        return c;
    }

    // ------------------------------------------------------------------
    // Step 2: profession
    // ------------------------------------------------------------------

    private static void SpawnProfessionChrome(Commands commands, CharCreationParams p)
    {
        var ctx = p.Ctx.Value;
        var stepRoot = SpawnStepRoot(commands, ctx.MenuEntity);
        stepRoot.Insert<ProfessionUI>();
        ctx.ProfessionStepRoot = stepRoot.Id;
        ctx.ProfessionPage = null;
        ctx.ProfessionDirty = true;
    }

    // Shared "fancy scroll" chrome of the profession/trade pages: ResizePic
    // 2600 + top decorations + title (cliloc 3000326).
    private static void SpawnScrollChrome(
        Commands commands, GumpBuilder builder, UOFileManager files, ulong parent, int titleX)
    {
        commands.AddChild(parent, builder.AddGumpNinePatch(
            commands, 2600, Vector3.UnitZ, new Vector2(100, 80), new Vector2(470, 372)).Id);
        commands.AddChild(parent, builder.AddGump(commands, 0x0589, Vector3.UnitZ, new Vector2(291, 42)).Id);
        commands.AddChild(parent, builder.AddGump(commands, 0x058B, Vector3.UnitZ, new Vector2(214, 58)).Id);
        commands.AddChild(parent, builder.AddGump(commands, 0x15A9, Vector3.UnitZ, new Vector2(300, 51)).Id);

        AddAsciiLabel(commands, parent,
            files.Clilocs.GetString(3000326) ?? "Choose a Trade for Your Character",
            titleX, 132, 2, 0x0386);
    }

    private static void RebuildProfessionEntries(Commands commands, CharCreationParams p,
        Query<Data<Node>, Filter<With<ProfessionEntries>>> entriesQ)
    {
        var ctx = p.Ctx.Value;
        ctx.ProfessionDirty = false;

        foreach (var (ent, _) in entriesQ) commands.Entity(ent.Ref).Despawn();

        var builder = p.Builder.Value;
        var files = p.Files.Value;

        List<ProfessionInfo> professions;
        if (ctx.ProfessionPage == null
            || !files.Professions.Professions.TryGetValue(ctx.ProfessionPage, out professions)
            || professions == null)
        {
            professions = new List<ProfessionInfo>(files.Professions.Professions.Keys);
        }

        var group = commands.Spawn()
            .Insert<ProfessionEntries>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(640), Height = Val.Px(480),
            });
        commands.AddChild(ctx.ProfessionStepRoot, group.Id);
        var groupId = group.Id;

        SpawnScrollChrome(commands, builder, files, groupId, 158);

        // Prev — back to appearance (or, inside a category, back to the top list).
        commands.AddChild(groupId, builder.AddButton(
            commands, (0x15A1, 0x15A3, 0x15A2), Vector3.UnitZ, new Vector2(586, 445))
            .Observe((On<UiClick> _, Res<CharCreationContext> c, ResMut<NextState<CharCreationStep>> next) =>
            {
                if (c.Value.ProfessionPage != null)
                {
                    c.Value.ProfessionPage = null;
                    c.Value.ProfessionDirty = true;
                }
                else
                {
                    next.Value.Set(CharCreationStep.Appearance);
                }
            }).Id);

        for (int i = 0; i < professions.Count; i++)
        {
            int cx = i % 2;
            int cy = i >> 1;
            SpawnProfessionEntry(commands, builder, files, groupId,
                145 + cx * 195, 168 + cy * 70, professions[i]);
        }
    }

    private static void SpawnProfessionEntry(
        Commands commands, GumpBuilder builder, UOFileManager files, ulong parent,
        int x, int y, ProfessionInfo info)
    {
        // The name label and the profession graphic paint topmost over the
        // ninepatch, and Clay routes the click to the topmost element — all
        // three need the same handler or the label/graphic swallow it.
        var onClick = (
            On<UiClick> _,
            Res<CharCreationContext> c,
            Res<UOFileManager> filesInner,
            Res<GameContext> g,
            ResMut<NextState<CharCreationStep>> next) =>
        {
            if (info.Type == ProfessionLoader.PROF_TYPE.CATEGORY
                && filesInner.Value.Professions.Professions.TryGetValue(info, out var children)
                && children != null)
            {
                c.Value.ProfessionPage = info;
                c.Value.ProfessionDirty = true;
                return;
            }

            ApplyProfession(c.Value, info, g.Value.ClientVersion, filesInner.Value);
            next.Value.Set(info.DescriptionIndex > 0 ? CharCreationStep.City : CharCreationStep.Trade);
        };

        var bg = builder.AddGumpNinePatch(commands, 3000, Vector3.UnitZ,
            new Vector2(x, y), new Vector2(175, 34))
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Observe(onClick);
        commands.AddChild(parent, bg.Id);

        var name = files.Clilocs.GetString(info.Localization) ?? info.Name ?? string.Empty;
        var label = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(x + 7), Top = Val.Px(y + 8),
                Width = Val.Auto, Height = Val.Auto,
            })
            .Insert(new Text(name))
            .Insert(new TextFont { FontId = 1, Size = 16 })
            .Insert(new TextColor(s_unicodeBlack))
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Observe(onClick);
        commands.AddChild(parent, label.Id);

        if (info.Graphic != 0)
        {
            var gfx = builder.AddGump(
                commands, info.Graphic, Vector3.UnitZ, new Vector2(x + 121, y - 12))
                .Insert(Interaction.None)
                .Insert<UiContainsByBounds>()
                .Observe(onClick);
            commands.AddChild(parent, gfx.Id);
        }
    }

    private static void ApplyProfession(
        CharCreationContext ctx, ProfessionInfo info, ClientVersion version, UOFileManager files)
    {
        int count = SkillsCount(version);
        ctx.Skills.Clear();

        for (int i = 0; i < count; i++)
        {
            int skillIndex = info.SkillDefVal[i, 0];
            if (skillIndex < 0 || skillIndex >= files.Skills.SkillsCount) continue;
            ctx.Skills.Add(((byte)skillIndex, (byte)info.SkillDefVal[i, 1]));
        }

        ctx.Strength = (ushort)info.StatsVal[0];
        ctx.Intelligence = (ushort)info.StatsVal[1];
        ctx.Dexterity = (ushort)info.StatsVal[2];
        ctx.Profession = info;
    }

    private static int SkillsCount(ClientVersion version)
        => version >= ClientVersion.CV_70160 ? 4 : 3;

    // ------------------------------------------------------------------
    // Step 3: trade (custom stats + skills)
    // ------------------------------------------------------------------

    // OnEnter only flags the rebuild (see SpawnAppearance note on why the
    // spawn must happen in an Update-stage system).
    private static void SpawnTrade(Commands commands, CharCreationParams p)
    {
        p.Ctx.Value.TradeNeedsReset = true;
        p.Ctx.Value.TradeDirty = true;
    }

    private static void SpawnTradeInner(Commands commands, CharCreationParams p, bool reset)
    {
        var ctx = p.Ctx.Value;
        var builder = p.Builder.Value;
        var files = p.Files.Value;
        var version = p.GameCtx.Value.ClientVersion;
        var locks = p.GameCtx.Value.LockedFeatures;

        var stepRoot = SpawnStepRoot(commands, ctx.MenuEntity);
        stepRoot.Insert<TradeUI>();
        var stepRootId = stepRoot.Id;

        SpawnScrollChrome(commands, builder, files, stepRootId, 148);

        var (defSkills, defStats) = ProfessionInfo.GetDefaults(version);
        int skillsCount = SkillsCount(version);

        // attribute labels + sliders
        var statClilocs = new[] { 3000111, 3000112, 3000113 }; // Strength / Dexterity / Intelligence
        var statY = new[] { 170, 250, 330 };
        var statSliderY = new[] { 196, 276, 356 };
        int statPool = defStats[0] + defStats[1] + defStats[2];

        if (reset)
        {
            ctx.TradeStats[0] = defStats[0];
            ctx.TradeStats[1] = defStats[1];
            ctx.TradeStats[2] = defStats[2];
        }

        var statSliderIds = new ulong[3];
        for (int i = 0; i < 3; i++)
        {
            AddUnicodeLabel(commands, stepRootId,
                files.Clilocs.GetString(statClilocs[i]) ?? string.Empty, 158, statY[i], 1);

            statSliderIds[i] = AddTradeSlider(commands, builder, stepRootId, 10, 60, ctx.TradeStats[i],
                new Vector2(164, statSliderY[i]), 164 + 96, statSliderY[i]);
        }
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            commands.Entity(statSliderIds[i]).Observe((
                On<SliderChanged> _,
                Query<Data<Slider>> sliders,
                Res<CharCreationContext> c) =>
                ParisAdjust(sliders, statSliderIds, idx, statPool, 10, c.Value.TradeStats));
        }

        // skill list (legacy CreateCharTradeGump filters)
        var skillList = files.Skills.SortedSkills
            .Where(s =>
                s.Index != 47 && // Stealth
                s.Index != 48 && // RemoveTrap
                s.Index != 54 && // Spellweaving
                (ctx.Race == RaceType.GARGOYLE || s.Index != 57)) // Throwing
            .Where(s => locks.HasFlag(LockedFeatureFlags.AOS) || (s.Index != 51 && s.Index != 50 && s.Index != 49))
            .Where(s => locks.HasFlag(LockedFeatureFlags.SE) || (s.Index != 52 && s.Index != 53))
            .Where(s => locks.HasFlag(LockedFeatureFlags.SA) || (s.Index != 55 && s.Index != 56))
            .ToList();

        if (ctx.Race == RaceType.GARGOYLE)
        {
            skillList.RemoveAll(s => s.Index == 31); // Archery
        }

        ctx.TradeSkillList = skillList;
        var skillNames = skillList.Select(s => s.Name).ToArray();
        int skillPool = 0;

        var skillSliderIds = new ulong[skillsCount];
        int y = 172;
        for (int i = 0; i < skillsCount; i++)
        {
            if (reset)
            {
                ctx.TradeSkillIdx[i] = -1;
                ctx.TradeSkillValues[i] = defSkills[i, 1];
            }
            skillPool += defSkills[i, 1];

            int comboIdx = i;
            SpawnCombo(commands, builder, stepRootId, stepRootId,
                new Vector2(344, y), 182, skillNames, ctx.TradeSkillIdx[i], "Click here", 200,
                (sel, c) => { c.TradeSkillIdx[comboIdx] = sel; c.TradeDirty = true; });

            skillSliderIds[i] = AddTradeSlider(commands, builder, stepRootId, 0, 50, ctx.TradeSkillValues[i],
                new Vector2(344, y + 32), 344 + 96, y + 32);

            y += 70;
        }
        for (int i = 0; i < skillsCount; i++)
        {
            int idx = i;
            commands.Entity(skillSliderIds[i]).Observe((
                On<SliderChanged> _,
                Query<Data<Slider>> sliders,
                Res<CharCreationContext> c) =>
                ParisAdjust(sliders, skillSliderIds, idx, skillPool, 0, c.Value.TradeSkillValues));
        }

        // Prev — StepBack(1): back to profession selection.
        commands.AddChild(stepRootId, builder.AddButton(
            commands, (0x15A1, 0x15A3, 0x15A2), Vector3.UnitZ, new Vector2(586, 445))
            .Observe((On<UiClick> _, ResMut<NextState<CharCreationStep>> next) =>
                next.Value.Set(CharCreationStep.Profession)).Id);

        // Next — validate skill picks (all chosen, no duplicates), store and go
        // to city selection.
        commands.AddChild(stepRootId, builder.AddButton(
            commands, (0x15A4, 0x15A6, 0x15A5), Vector3.UnitZ, new Vector2(610, 445))
            .Observe((
                On<UiClick> _,
                Commands cmd,
                Res<CharCreationContext> c,
                Res<UOFileManager> filesInner,
                Res<GumpBuilder> b,
                Res<AssetsServer> assets,
                Res<UiSurface> surface,
                Res<UiZCounter> zc,
                Res<GameContext> g,
                ResMut<NextState<CharCreationStep>> next) =>
            {
                int count = SkillsCount(g.Value.ClientVersion);
                var picks = c.Value.TradeSkillIdx.AsSpan(0, count);

                bool allPicked = true;
                bool duplicated = false;
                for (int i = 0; i < count; i++)
                {
                    if (picks[i] < 0) { allPicked = false; break; }
                    for (int j = i + 1; j < count; j++)
                    {
                        if (picks[i] == picks[j]) duplicated = true;
                    }
                }

                if (!allPicked || duplicated)
                {
                    var msg = filesInner.Value.Clilocs.GetString(1080032)
                        ?? "You must have three unique skills chosen!";
                    MessageBoxGumpPlugin.Open(cmd, b.Value, assets.Value, surface.Value, zc.Value,
                        400, 300, msg, MessageButtonType.OK);
                    return;
                }

                c.Value.Skills.Clear();
                for (int i = 0; i < count; i++)
                {
                    var entry = c.Value.TradeSkillList[picks[i]];
                    c.Value.Skills.Add(((byte)entry.Index, (byte)c.Value.TradeSkillValues[i]));
                }

                c.Value.Strength = (ushort)c.Value.TradeStats[0];
                c.Value.Intelligence = (ushort)c.Value.TradeStats[1];
                c.Value.Dexterity = (ushort)c.Value.TradeStats[2];

                next.Value.Set(CharCreationStep.City);
            }).Id);
    }

    // Legacy HSliderBar "paris" semantics: the group shares one pool — pushing
    // a slider up steals from the others (down to their min); if nothing is
    // left to steal the moved slider is clamped back.
    private static void ParisAdjust(
        Query<Data<Slider>> sliders, ulong[] ids, int changed, int pool, int min, int[] store)
    {
        Span<int> vals = stackalloc int[ids.Length];
        int sum = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            if (!sliders.TryGet(ids[i], out var row)) return;
            var (_, s) = row;
            vals[i] = (int)MathF.Round(s.Ref.Value);
            sum += vals[i];
        }

        int excess = sum - pool;
        for (int i = 0; i < ids.Length && excess > 0; i++)
        {
            if (i == changed) continue;
            int take = Math.Min(excess, vals[i] - min);
            vals[i] -= take;
            excess -= take;
        }
        if (excess > 0)
        {
            vals[changed] -= excess;
        }

        for (int i = 0; i < ids.Length; i++)
        {
            var (_, s) = sliders.Get(ids[i]);
            s.Ref.Value = vals[i];
            store[i] = vals[i];
        }
    }

    // Spawn a trade-step HSlider, parent it, attach its live value label.
    // Legacy HSliderBarStyle.MetalWidgetRecessedBar: 93px wide, track
    // 213/214/215, thumb 216 — shared by the stat and skill slider rows.
    private static ulong AddTradeSlider(
        Commands commands, GumpBuilder builder, ulong parent,
        int min, int max, int value, Vector2 pos, int labelX, int labelY)
    {
        var slider = builder.AddHSlider(commands, min, max, value, pos, 93, thumbGump: 216, trackGumps: (213, 214, 215));
        commands.AddChild(parent, slider.Id);
        AddSliderValueLabel(commands, parent, slider.Id, labelX, labelY);
        return slider.Id;
    }

    private static void AddSliderValueLabel(
        Commands commands, ulong parent, ulong sliderId, int x, int y)
    {
        var label = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(x), Top = Val.Px(y),
                Width = Val.Auto, Height = Val.Auto,
            })
            .Insert(new Text(string.Empty))
            // Legacy HSliderBar value text: unicode font 0 hue 0 (near-black).
            .Insert(new TextFont { FontId = 0, Size = 14 })
            .Insert(new TextColor(s_unicodeBlack))
            .Insert(new SliderValueLabel { Slider = sliderId });
        commands.AddChild(parent, label.Id);
    }

    private static void UpdateSliderValueLabels(
        Query<Data<Text, SliderValueLabel>> labels,
        Query<Data<Slider>> sliders)
    {
        foreach (var (_, text, link) in labels)
        {
            if (!sliders.TryGet(link.Ref.Slider, out var row)) continue;
            var (_, s) = row;
            var v = ((int)MathF.Round(s.Ref.Value)).ToString();
            if (text.Ref.Value != v) text.Ref = new Text(v);
        }
    }

    // ------------------------------------------------------------------
    // Step 4: city selection
    // ------------------------------------------------------------------

    private static readonly string[] s_cityNames =
    {
        "Felucca", "Trammel", "Ilshenar", "Malas", "Tokuno", "Ter Mur"
    };

    private static readonly (int X, int Y)[] s_townButtonsText =
    {
        (105, 130), (245, 90), (165, 200), (395, 160), (200, 305),
        (335, 250), (160, 395), (100, 250), (270, 130)
    };

    private static void SpawnCityChrome(Commands commands, CharCreationParams p)
    {
        var ctx = p.Ctx.Value;
        var stepRoot = SpawnStepRoot(commands, ctx.MenuEntity);
        stepRoot.Insert<CityUI>();
        ctx.CityStepRoot = stepRoot.Id;

        // Default city: index 0 on >=7.0.13, else the 4th (legacy fallback).
        var towns = p.Snapshot.Value.Towns;
        var version = p.GameCtx.Value.ClientVersion;
        if (towns.Count > 0)
        {
            int def = version >= ClientVersion.CV_70130 || towns.Count < 4 ? 0 : 3;
            ctx.CityIndex = towns[Math.Min(def, towns.Count - 1)].Index;
        }

        ctx.CityDirty = true;
    }

    private static void RebuildCity(Commands commands, CharCreationParams p,
        Query<Data<Node>, Filter<With<CityDynamic>>> dynQ)
    {
        var ctx = p.Ctx.Value;
        ctx.CityDirty = false;

        foreach (var (ent, _) in dynQ) commands.Entity(ent.Ref).Despawn();

        var builder = p.Builder.Value;
        var files = p.Files.Value;
        var version = p.GameCtx.Value.ClientVersion;
        var towns = p.Snapshot.Value.Towns;
        var useNewFormat = version >= ClientVersion.CV_70130;

        var dyn = commands.Spawn()
            .Insert<CityDynamic>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(640), Height = Val.Px(480),
            });
        commands.AddChild(ctx.CityStepRoot, dyn.Id);
        var dynId = dyn.Id;

        TownInfo selected = default;
        bool hasSelected = false;
        foreach (var t in towns)
        {
            if (t.Index == ctx.CityIndex) { selected = t; hasSelected = true; break; }
        }
        if (!hasSelected && towns.Count > 0) { selected = towns[0]; }

        uint facet = useNewFormat ? Math.Min(selected.Map, 5u) : 0;

        if (useNewFormat)
        {
            commands.AddChild(dynId, builder.AddGump(
                commands, (ushort)(0x15D9 + facet), Vector3.UnitZ, new Vector2(62, 54)).Id);
            commands.AddChild(dynId, builder.AddGump(
                commands, 0x15DF, Vector3.UnitZ, new Vector2(57, 49)).Id);
            // Legacy facet label: unicode font 0 hue 0x0481 (light gray).
            AddUnicodeLabel(commands, dynId, s_cityNames[(int)facet], 240, 440, 0, ClayColor.White);
        }
        else
        {
            commands.AddChild(dynId, builder.AddGump(
                commands, 0x1598, Vector3.UnitZ, new Vector2(57, 49)).Id);
        }

        // City description (legacy HtmlControl 452,60 175x367).
        var desc = selected.ClilocDescription != 0
            ? files.Clilocs.GetString((int)selected.ClilocDescription) ?? selected.Name
            : selected.Name;
        SpawnCityDescription(commands, dynId, desc);

        // Town buttons + labels.
        for (int i = 0; i < towns.Count; i++)
        {
            var town = towns[i];
            int x = 0, y = 0;

            if (useNewFormat && town.ClilocDescription != 0)
            {
                int cityFacet = (int)Math.Min(town.Map, 5u);
                x = 62 + MathHelperUO.PercetangeOf(
                    files.Maps.MapsDefaultSize[cityFacet, 0] - 2048, town.Position.X, 383);
                y = 54 + MathHelperUO.PercetangeOf(
                    files.Maps.MapsDefaultSize[cityFacet, 1], town.Position.Y, 384);
            }
            else if (i < s_townButtonsText.Length)
            {
                x = s_townButtonsText[i].X;
                y = s_townButtonsText[i].Y;
            }

            bool isSelected = town.Index == ctx.CityIndex;
            byte townIdx = town.Index;

            var btnIds = isSelected
                ? ((ushort)0x04BA, (ushort)0x04BA, (ushort)0x04BA)
                : ((ushort)0x04B9, (ushort)0x04BA, (ushort)0x04BA);
            commands.AddChild(dynId, builder.AddButton(commands, btnIds, Vector3.UnitZ, new Vector2(x, y))
                .Observe((On<UiClick> _, Res<CharCreationContext> c) =>
                {
                    c.Value.CityIndex = townIdx;
                    c.Value.CityDirty = true;
                }).Id);

            // Label above the button; shift left if it would clip the map edge.
            var (lw, _) = UoFontRenderer.Measure(town.Name, 3, 0, isHtml: false, 0, htmlBg: false, ascii: true);
            int lx = x;
            if (lx + lw >= 383) lx -= 60;

            var label = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(lx), Top = Val.Px(y - 20),
                    Width = Val.Auto, Height = Val.Auto,
                })
                .Insert(new Text(town.Name))
                .Insert(new TextFont { FontId = (ushort)(3 | UoFontRuntime.AsciiFlag), Size = 14 })
                .Insert(new TextColor(UoFontRuntime.AsciiHue(isSelected ? (ushort)0x0481 : (ushort)0x0058)))
                .Insert(Interaction.None)
                .Insert<UiContainsByBounds>()
                .Observe((On<UiClick> _, Res<CharCreationContext> c) =>
                {
                    c.Value.CityIndex = townIdx;
                    c.Value.CityDirty = true;
                });
            commands.AddChild(dynId, label.Id);
        }

        // Prev — StepBack: professions with a fixed description skip the trade
        // page, so back out two steps in that case.
        commands.AddChild(dynId, builder.AddButton(
            commands, (0x15A1, 0x15A3, 0x15A2), Vector3.UnitZ, new Vector2(586, 445))
            .Observe((On<UiClick> _, Res<CharCreationContext> c, ResMut<NextState<CharCreationStep>> next) =>
            {
                var prof = c.Value.Profession;
                next.Value.Set(prof != null && prof.DescriptionIndex > 0
                    ? CharCreationStep.Profession
                    : CharCreationStep.Trade);
            }).Id);

        // Finish — build and send the create-character packet.
        commands.AddChild(dynId, builder.AddButton(
            commands, (0x15A4, 0x15A6, 0x15A5), Vector3.UnitZ, new Vector2(610, 445))
            .Observe((
                On<UiClick> _,
                Res<CharCreationContext> c,
                Res<GameContext> g,
                Res<NetClient> net,
                Res<CharacterListSnapshot> snapshot) =>
                FinishCreation(c.Value, g.Value, net.Value, snapshot.Value)).Id);
    }

    private static void SpawnCityDescription(Commands commands, ulong parent, string text)
    {
        var frame = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Column,
                PositionType = PositionType.Absolute,
                Left = Val.Px(452), Top = Val.Px(60),
                Width = Val.Px(175), Height = Val.Px(367),
                Overflow = Overflow.Scroll,
            })
            .Insert(new ScrollPosition());
        commands.AddChild(parent, frame.Id);

        var (w, h) = UoFontRenderer.Measure(text, 1, 175, isHtml: true, 0, htmlBg: false);
        var body = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                Width = Val.Px(Math.Max(w, 1)),
                Height = Val.Px(Math.Max(h, 1)),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.WrappedText,
                    Hue = Vector3.UnitZ,
                    Text = text,
                    TextFont = 1,
                    WrapWidth = 175,
                    IsHtml = true,
                    HtmlStartColor = 0xFFFFFFFF,
                    HtmlBg = false,
                },
            });
        commands.AddChild(frame.Id, body.Id);
    }

    private static void FinishCreation(
        CharCreationContext ctx, in GameContext gameCtx, NetClient net, CharacterListSnapshot snapshot)
    {
        var race = ctx.Race;
        var isFemale = ctx.IsFemale;

        // Skills, highest first (legacy OrderByDescending take skillcount).
        var ordered = ctx.Skills.OrderByDescending(s => s.Value).ToArray();
        Span<(byte, byte)> skills = stackalloc (byte, byte)[Math.Min(ordered.Length, 4)];
        for (int i = 0; i < skills.Length; i++) skills[i] = ordered[i];

        // First free character slot.
        uint slot = 0;
        while (snapshot.Characters.Any(ch => ch.Index == slot)) slot++;

        var (_, hairGraphics) = CharacterCreationValues.GetHairComboContent(isFemale, race);
        ushort hairGraphic = ctx.HairIndex >= 0 && ctx.HairIndex < hairGraphics.Length
            ? (ushort)hairGraphics[ctx.HairIndex] : (ushort)0;
        ushort hairHue = GetColor(ctx, CreationLayer.Hair, CharacterCreationValues.GetHairPallet(race));

        ushort beardGraphic = 0;
        ushort beardHue = 0;
        if (!isFemale && race != RaceType.ELF)
        {
            var (_, facialGraphics) = CharacterCreationValues.GetFacialHairComboContent(race);
            if (ctx.BeardIndex >= 0 && ctx.BeardIndex < facialGraphics.Length)
            {
                beardGraphic = (ushort)facialGraphics[ctx.BeardIndex];
            }
            beardHue = GetColor(ctx, CreationLayer.Beard, CharacterCreationValues.GetHairPallet(race));
        }
        if (beardGraphic == 0) beardHue = 0;
        if (hairGraphic == 0) hairHue = 0;

        ushort skinHue = GetColor(ctx, CreationLayer.Skin, CharacterCreationValues.GetSkinPallet(race));
        ushort shirtHue = GetColor(ctx, CreationLayer.Shirt, null);
        ushort pantsHue = race == RaceType.GARGOYLE ? (ushort)0 : GetColor(ctx, CreationLayer.Pants, null);

        byte profession = ctx.Profession != null ? (byte)ctx.Profession.DescriptionIndex : (byte)0;

        net.Send_CreateCharacter(
            gameCtx.ClientVersion,
            gameCtx.Protocol,
            ctx.Name,
            isFemale,
            race,
            (byte)ctx.Strength,
            (byte)ctx.Dexterity,
            (byte)ctx.Intelligence,
            skills,
            skinHue,
            hairGraphic,
            hairHue,
            beardGraphic,
            beardHue,
            shirtHue,
            pantsHue,
            ctx.CityIndex,
            slot,
            net.LocalIP,
            profession);
    }

    // ------------------------------------------------------------------
    // Shared helpers
    // ------------------------------------------------------------------

    private static EntityCommands SpawnStepRoot(Commands commands, ulong menuEntity)
    {
        var stepRoot = commands.Spawn()
            .Insert<CharCreationScene>()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(640), Height = Val.Px(480),
            });
        commands.AddChild(menuEntity, stepRoot.Id);
        return stepRoot;
    }

    private static void AddAsciiLabel(
        Commands commands, ulong parent, string text, int x, int y, byte font, ushort hue)
    {
        var label = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(x), Top = Val.Px(y),
                Width = Val.Auto, Height = Val.Auto,
            })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = (ushort)(font | UoFontRuntime.AsciiFlag), Size = 14 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(hue)));
        commands.AddChild(parent, label.Id);
    }

    // Legacy unicode Label with hue 0 bakes the near-black sentinel colour
    // (see UoFontPlugin.UnicodeBakedColor) — match it, don't render white.
    private static readonly ClayColor s_unicodeBlack = new(1, 1, 1, 255);

    private static void AddUnicodeLabel(
        Commands commands, ulong parent, string text, int x, int y, byte font, ClayColor? color = null)
    {
        var label = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(x), Top = Val.Px(y),
                Width = Val.Auto, Height = Val.Auto,
            })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = font, Size = 16 })
            .Insert(new TextColor(color ?? s_unicodeBlack));
        commands.AddChild(parent, label.Id);
    }

    private static ushort GetColor(CharCreationContext ctx, CreationLayer layer, ushort[] pallet)
    {
        if (ctx.Colors.TryGetValue(layer, out var entry)) return entry.Hue;
        return (ushort)((pallet != null && pallet.Length > 0 ? pallet[0] : (ushort)1) + 1);
    }

    // Hue 0 must select the pass-through shader (see PaperdollPlugin note).
    private static Vector3 ToShaderHue(ushort hue)
        => hue == 0 ? Vector3.UnitZ : new Vector3(hue, 1f, 1f);

    // Legacy CreateCharAppearanceGump.Validate — returns the error cliloc, or
    // 0 when the name is acceptable.
    internal static int ValidateName(string name, ClientVersion version)
    {
        return Validate(
            name, 2, 16, true, false, true, 1,
            s_spaceDashPeriodQuote,
            version >= ClientVersion.CV_5020 ? s_disallowed : Array.Empty<string>(),
            s_startDisallowed);
    }

    private static int Validate(
        string name, int minLength, int maxLength, bool allowLetters, bool allowDigits,
        bool noExceptionsAtStart, int maxExceptions, char[] exceptions,
        string[] disallowed, string[] startDisallowed)
    {
        if (string.IsNullOrEmpty(name) || name.Length < minLength) return 3000612;
        if (name.Length > maxLength) return 3000611;
        if (name.Trim() != name) return 3000611;

        int exceptCount = 0;
        name = name.ToLowerInvariant();

        if (!allowLetters || !allowDigits || exceptions.Length > 0 && (noExceptionsAtStart || maxExceptions < int.MaxValue))
        {
            for (int i = 0; i < name.Length; ++i)
            {
                char c = name[i];

                if (c >= 'a' && c <= 'z')
                {
                    exceptCount = 0;
                }
                else if (c >= '0' && c <= '9')
                {
                    if (!allowDigits) return 3000611;
                    exceptCount = 0;
                }
                else
                {
                    bool except = false;
                    for (int j = 0; !except && j < exceptions.Length; ++j)
                    {
                        if (c == exceptions[j]) except = true;
                    }

                    if (!except || i == 0 && noExceptionsAtStart) return 3000611;
                    if (exceptCount++ == maxExceptions) return 3000611;
                }
            }
        }

        for (int i = 0; i < disallowed.Length; ++i)
        {
            int indexOf = name.IndexOf(disallowed[i], StringComparison.Ordinal);
            if (indexOf == -1) continue;

            bool badPrefix = indexOf == 0;
            for (int j = 0; !badPrefix && j < exceptions.Length; ++j)
            {
                badPrefix = name[indexOf - 1] == exceptions[j];
            }
            if (!badPrefix) continue;

            bool badSuffix = indexOf + disallowed[i].Length >= name.Length;
            for (int j = 0; !badSuffix && j < exceptions.Length; ++j)
            {
                badSuffix = name[indexOf + disallowed[i].Length] == exceptions[j];
            }
            if (badSuffix) return 3000611;
        }

        for (int i = 0; i < startDisallowed.Length; ++i)
        {
            if (name.StartsWith(startDisallowed[i], StringComparison.Ordinal)) return 3000611;
        }

        return 0;
    }

    private static readonly char[] s_spaceDashPeriodQuote = { ' ', '-', '.', '\'' };

    private static readonly string[] s_startDisallowed =
    {
        "seer", "counselor", "gm", "admin", "lady", "lord"
    };

    private static readonly string[] s_disallowed =
    {
        "jigaboo", "chigaboo", "wop", "kyke", "kike", "tit", "spic", "prick", "piss", "lezbo",
        "lesbo", "felatio", "dyke", "dildo", "chinc", "chink", "cunnilingus", "cum", "cocksucker",
        "cock", "clitoris", "clit", "ass", "hitler", "penis", "nigga", "nigger", "klit", "kunt",
        "jiz", "jism", "jerkoff", "jackoff", "goddamn", "fag", "blowjob", "bitch", "asshole",
        "dick", "pussy", "snatch", "cunt", "twat", "shit", "fuck", "tailor", "smith", "scholar",
        "rogue", "novice", "neophyte", "merchant", "medium", "master", "mage", "lb", "journeyman",
        "grandmaster", "fisherman", "expert", "chef", "carpenter", "british", "blackthorne",
        "blackthorn", "beggar", "archer", "apprentice", "adept", "gamemaster", "frozen",
        "squelched", "invulnerable", "osi", "origin"
    };

    // Markers --------------------------------------------------------------

    private struct AppearanceUI;
    private struct AppearanceDynamic;
    private struct ProfessionUI;
    private struct ProfessionEntries;
    private struct TradeUI;
    private struct CityUI;
    private struct CityDynamic;
    private struct CharNameInput;

    private struct SliderValueLabel
    {
        public ulong Slider;
    }
}

// Scene root marker — promoted to namespace-level internal so the modding
// registry (same assembly) can key a WIT scene path to it.
internal struct CharCreationScene;

internal enum CreationLayer : byte
{
    Skin,
    Shirt,
    Pants,
    Hair,
    Beard
}

// All cross-step character-creation choices. A resource (never closure state):
// the step UIs despawn/respawn freely and must re-read the live values.
internal sealed class CharCreationContext
{
    public ulong MenuEntity;
    public ulong AppearanceStepRoot;
    public ulong ProfessionStepRoot;
    public ulong CityStepRoot;

    public string Name = string.Empty;
    public bool IsFemale;
    public RaceType Race = RaceType.HUMAN;
    public int HairIndex = 1;
    public int BeardIndex;
    public readonly Dictionary<CreationLayer, (int Index, ushort Hue)> Colors = new();

    public ProfessionInfo Profession;
    public ProfessionInfo ProfessionPage;
    public ushort Strength = 60;
    public ushort Intelligence = 15;
    public ushort Dexterity = 15;
    public readonly List<(byte Index, byte Value)> Skills = new();

    public List<SkillEntry> TradeSkillList = new();
    public readonly int[] TradeSkillIdx = new int[4];
    public readonly int[] TradeSkillValues = new int[4];
    public readonly int[] TradeStats = new int[3];

    public byte CityIndex;

    public bool AppearanceDirty;
    public bool ProfessionDirty;
    public bool TradeDirty;
    public bool TradeNeedsReset;
    public bool CityDirty;

    public void Reset(ClientVersion version)
    {
        Name = string.Empty;
        IsFemale = false;
        Race = RaceType.HUMAN;
        HairIndex = 1;
        BeardIndex = 0;
        Colors.Clear();
        Profession = null;
        ProfessionPage = null;
        Skills.Clear();
        TradeSkillList = new List<SkillEntry>();
        Array.Fill(TradeSkillIdx, -1);
        CityIndex = 0;
        AppearanceDirty = false;
        ProfessionDirty = false;
        TradeDirty = false;
        CityDirty = false;

        var (_, defStats) = ProfessionInfo.GetDefaults(version);
        Strength = (ushort)defStats[0];
        Intelligence = (ushort)defStats[1];
        Dexterity = (ushort)defStats[2];
    }
}

internal sealed class CharCreationParams : CompositeSystemParam
{
    public readonly Res<AssetsServer> Assets;
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<UOFileManager> Files;
    public readonly Res<GameContext> GameCtx;
    public readonly Res<CharCreationContext> Ctx;
    public readonly Res<CharacterListSnapshot> Snapshot;

    public CharCreationParams()
    {
        Assets = Add(new Res<AssetsServer>());
        Builder = Add(new Res<GumpBuilder>());
        Files = Add(new Res<UOFileManager>());
        GameCtx = Add(new Res<GameContext>());
        Ctx = Add(new Res<CharCreationContext>());
        Snapshot = Add(new Res<CharacterListSnapshot>());
    }
}
