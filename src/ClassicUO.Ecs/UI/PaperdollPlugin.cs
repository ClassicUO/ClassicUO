// Port of main's Game/UI/Gumps/PaperDollGump.cs to the ECS branch.
//
// Spawn lifecycle: server pushes OnOpenPaperdollPacket_0x88 -> Spawn system
// queries existing PaperdollWindow entities for the same serial. Found ->
// focus (bump z, refocus). Not found -> build a fresh window tree on a new
// entity, parented to nothing (Bevy.UI floating root) so Clay places it
// independently.
//
// Movability / right-click close / topmost-on-click / click-capture come
// from the shared UiMovable infra in WindowDragPlugin — paperdoll does NOT
// reimplement any of those (rule 4). The bg root carries the UiMovable
// marker via GumpBuilder.AddGumpRoot; everything else falls out.
//
// Body + equipment overlays mirror PaperDollInteractable.UpdateUI: pick a
// body graphic from mobile.Graphic, then layer equipped item gumps using
// MALE_GUMP_OFFSET / FEMALE_GUMP_OFFSET + tile data AnimID. Each overlay
// entity carries PaperdollBodyChild { WindowEntity } so the refresh system
// can despawn-and-rebuild on EquipmentSlots change without disturbing the
// static frame / button / label children.

using System;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;
using GameLayer = ClassicUO.Game.Data.Layer;

namespace ClassicUO.Ecs;

internal readonly struct PaperdollPlugin : IPlugin
{
    // Equipment slot frame positions, mirroring main's BuildGump: left column
    // (9 slots) at x=2, right column (6 slots) at x=162, each 21px apart from
    // y=70. Each slot draws a tiled bg + 0x2344 border frame; the equipped
    // item's art icon is layered between them.
    private static readonly (GameLayer Layer, int X, int Y)[] s_slotLayout = BuildSlotLayout();

    private static (GameLayer, int, int)[] BuildSlotLayout()
    {
        var left = new[]
        {
            GameLayer.Helmet, GameLayer.Earrings, GameLayer.Necklace,
            GameLayer.Ring, GameLayer.Bracelet, GameLayer.Tunic,
            GameLayer.OneHanded, GameLayer.TwoHanded, GameLayer.Talisman,
        };
        var right = new[]
        {
            GameLayer.Robe, GameLayer.Gloves, GameLayer.Pants,
            GameLayer.Arms, GameLayer.Cloak, GameLayer.Shoes,
        };
        var list = new System.Collections.Generic.List<(GameLayer, int, int)>(left.Length + right.Length);
        for (int i = 0; i < left.Length; i++)
            list.Add((left[i], 2, 70 + 21 * i));
        for (int i = 0; i < right.Length; i++)
            list.Add((right[i], 162, 70 + 21 * i));
        return list.ToArray();
    }

    public void Build(App app)
    {
        var disposeFn = DisposeOnLogout;

        app.AddSystem(disposeFn)
            .OnExit(GameState.GameScreen)
            .Build();

        // Profile / party / book scroll gump-pics render behind the paperdoll
        // body, so Clay's bounds Interaction can't route their double-clicks
        // (body bbox covers them). Resolve via UiPick (pixel-perfect): the body's
        // transparent corner pixels pass the hit through to the scroll.
        var scrollFn = ScrollDClick;
        app.AddSystem(scrollFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        // Backpack double-click. Bevy.UI synthesizes UiDoubleClick from
        // two UiClick events within UiClayContext.DoubleClickWindow on the
        // same entity; this observer just routes that to Send_DoubleClick
        // on the backpack's NetworkSerial (carried on the sprite via
        // PaperdollBackpackUI).
        app.AddObserver((
            On<UiDoubleClick> trig,
            Res<NetClient> net,
            Query<Data<PaperdollBackpackUI>> bpQ) =>
        {
            if (!bpQ.TryGet(trig.EntityId, out var bpRow)) return;
            var (_, bp) = bpRow;
            net.Value.Send_DoubleClick(bp.Ref.ItemSerial);
        });

        // 0x88 (open paperdoll) -> spawn (or focus existing) window. Observer
        // on the typed packet trigger; no polling, no EventReader<IPacket>
        // scan per frame.
        app.AddObserver<On<PacketReceived<OnOpenPaperdollPacket_0x88>>, Commands, PaperdollSpawnParams>(SpawnOnOpenPaperdoll);

        // Event-driven refresh: rebuild body+overlays only when a mobile's
        // EquipmentSlots is actually (re)inserted. Packet handlers dirty-check
        // before insert (EquipmentSlots.ContentEquals), so this fires on real
        // equip/unequip only — no per-frame Changed scan, no polling system.
        // Commands is passed top-level (not inside the composite) so the
        // observer harness auto-applies its deferred spawns/despawns.
        app.AddObserver<OnInsert<EquipmentSlots>, Commands, PaperdollRebuildParams>(RebuildOnEquip);

        // War mode bit flips ServerFlags.Value -> swap the peace/war button's
        // UOButton triplet. OnWarmode (0x72) reinserts ServerFlags on the
        // player; this observer matches buttons by MobileSerial and mutates
        // UOButton.Normal/Pressed/Over in place (UpdateUOButtonsState then
        // picks the right asset id per frame from Interaction state).
        app.AddObserver((
            OnInsert<ServerFlags> trigger,
            Query<Data<NetworkSerial>> serialQ,
            Query<Data<UOButton, PaperdollWarModeButton>> btnQ) =>
        {
            if (!serialQ.TryGet(trigger.EntityId, out var serialRow)) return;
            var (_, ns) = serialRow;
            var serial = ns.Ref.Value;
            bool isWar = (trigger.Component.Value & Flags.WarMode) != 0;
            var ids = isWar ? (n: (ushort)0x07E8, p: (ushort)0x07E9, o: (ushort)0x07EA)
                            : (n: (ushort)0x07E5, p: (ushort)0x07E6, o: (ushort)0x07E7);
            foreach (var (_, btn, tag) in btnQ)
            {
                if (tag.Ref.MobileSerial != serial) continue;
                btn.Ref.Normal = ids.n;
                btn.Ref.Pressed = ids.p;
                btn.Ref.Over = ids.o;
            }
        });
    }

    private static void DisposeOnLogout(
        Commands commands,
        Query<Data<TinyEcs.Children>, Filter<With<PaperdollWindow>>> windowsQ)
    {
        foreach (var (ent, _) in windowsQ)
            commands.Entity(ent.Ref).Despawn();
    }

    private static void SpawnOnOpenPaperdoll(
        On<PacketReceived<OnOpenPaperdollPacket_0x88>> trig,
        Commands commands,
        PaperdollSpawnParams p)
    {
        var packet = trig.Event.Packet;

        // Already open for this serial -> focus instead of duplicating.
        ulong existing = 0;
        foreach (var (ent, w) in p.ExistingQ)
        {
            if (w.Ref.Serial == packet.Serial) { existing = ent.Ref; break; }
        }
        if (existing != 0)
        {
            var newZ = p.ZCounter.Value.Bump();
            commands.Entity(existing).Insert(new GlobalZIndex(newZ));
            return;
        }

        var tileData = p.Files.Value.TileData.StaticData;
        BuildWindow(commands, p.Assets.Value, p.Builder.Value, p.GameCtx.Value, tileData,
            p.Entities.Value, p.GraphicHueQ, p.EquipQ, p.GraphicHueQ, p.SerialQ, p.FlagsQ,
            p.ZCounter.Value, p.ExistingQ.Count(),
            packet.Serial, packet.Title, packet.Flags);
    }

    // Observer: a mobile's EquipmentSlots was (re)inserted. Rebuild the
    // body+overlay subtree of every open paperdoll targeting that mobile.
    // Fires only on a real equip/unequip — packet handlers dirty-check
    // before insert (EquipmentSlots.ContentEquals) — so there is no polling
    // system and no per-frame Changed scan. trigger.EntityId is the mobile
    // entity; resolve its NetworkSerial to match PaperdollWindow.Serial.
    private static void RebuildOnEquip(
        OnInsert<EquipmentSlots> trigger,
        Commands commands,
        PaperdollRebuildParams p)
    {
        var mobileEnt = trigger.EntityId;
        if (!p.SerialQ.TryGet(mobileEnt, out var serialRow)) return;
        var (_, mns) = serialRow;
        var serial = mns.Ref.Value;

        if (p.WindowsQ.Count() == 0) return;
        var tileData = p.Files.Value.TileData.StaticData;

        foreach (var (windowEnt, win) in p.WindowsQ)
        {
            if (win.Ref.Serial != serial) continue;

            foreach (var (childEnt, child) in p.BodyChildQ)
            {
                if (child.Ref.WindowEntity != windowEnt.Ref) continue;
                commands.Entity(childEnt.Ref).Despawn();
            }

            BuildBodyAndOverlays(commands, p.Assets.Value, p.Builder.Value,
                p.GameCtx.Value, tileData, p.Entities.Value,
                p.GraphicHueQ, p.EquipQ, p.GraphicHueQ, p.SerialQ,
                windowEnt.Ref, serial);
        }
    }

    private static void BuildWindow(
        Commands commands,
        AssetsServer assets,
        GumpBuilder builder,
        GameContext gameCtx,
        ClassicUO.Assets.StaticTiles[] tileData,
        NetworkEntitiesMap entitiesMap,
        Query<Data<Graphic, Hue>, Filter<Optional<Hue>>> mobileGraphicQ,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<Graphic, Hue>, Filter<Optional<Hue>>> itemQ,
        Query<Data<NetworkSerial>> itemSerialQ,
        Query<Data<ServerFlags>> flagsQ,
        UiZCounter zCounter,
        int existingWindowCount,
        uint serial,
        string title,
        byte flags)
    {
        bool isPlayer = serial == gameCtx.PlayerSerial;
        ushort bgId = (ushort)(isPlayer ? 0x07D0 : 0x07D1);
        bool showBooks = (gameCtx.ClientFeatures & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0
            && isPlayer;
        bool showRacialBook = showBooks && gameCtx.ClientVersion >= ClientVersion.CV_7000;

        // Stagger spawn position so multiple paperdolls don't overlap pixel-
        // perfect. Mirrors main's UIManager.SavePosition fallback (anchored
        // to mouse pos when no saved position exists) — close enough for v1.
        var spawnPos = new Vector2(100 + (existingWindowCount & 0x07) * 24, 100);

        var root = builder.SpawnUOGump(commands, bgId, Vector3.UnitZ, spawnPos, zCounter)
            .Insert(new PaperdollWindow
            {
                Serial = serial,
                IsPlayer = isPlayer,
            });

        // Combat / racial-abilities book + profile / party scroll gump-pics.
        // OOP fires on left double-click. Combat at (156, 200) when
        // ClientFeatures.PaperdollBooks; racial at (23, 200) when CV >= 7000;
        // profile/party at the bottom-left (0x07D2).
        //
        // Added BEFORE the body so the scroll art renders behind the body /
        // equipment overlays (legacy paint order) — see BehindBodyScrollCount,
        // which inserts the body just after these. Because they sit behind the
        // body, Clay's bounds-based Interaction (the body's bbox covers them)
        // can't route their clicks, so they carry PaperdollScrollUI and are
        // resolved by ScrollDClick via UiPick (pixel-perfect — the body's
        // transparent corner pixels pass the click through). Keep this add order
        // (combat, racial, profile, party) in sync with BehindBodyScrollCount.
        if (showBooks)
        {
            var combatBook = builder.AddGump(commands, 0x2B34, Vector3.UnitZ, new Vector2(156, 200))
                .Insert(new PaperdollScrollUI { Kind = ScrollKind.CombatBook });
            commands.AddChild(root.Id, combatBook.Id);

            if (showRacialBook)
            {
                var racialBook = builder.AddGump(commands, 0x2B28, Vector3.UnitZ, new Vector2(23, 200))
                    .Insert(new PaperdollScrollUI { Kind = ScrollKind.RacialBook });
                commands.AddChild(root.Id, racialBook.Id);
            }
        }

        {
            int profileX = 25;
            if (showRacialBook) profileX += 14;
            var profilePic = builder.AddGump(commands, 0x07D2, Vector3.UnitZ, new Vector2(profileX, 196))
                .Insert(new PaperdollScrollUI { Kind = ScrollKind.Profile, Serial = serial });
            commands.AddChild(root.Id, profilePic.Id);

            if (isPlayer)
            {
                int partyX = profileX + 14;
                var partyPic = builder.AddGump(commands, 0x07D2, Vector3.UnitZ, new Vector2(partyX, 196))
                    .Insert(new PaperdollScrollUI { Kind = ScrollKind.Party });
                commands.AddChild(root.Id, partyPic.Id);
            }
        }

        // Body, equipment overlays, and the equipment-slot frames + item icons
        // are all built here (and rebuilt by the equip observer). Slots live in
        // BuildBodyAndOverlays so the bg→icon→frame paint order is stable and
        // the icon tracks equip changes.
        BuildBodyAndOverlays(commands, assets, builder, gameCtx, tileData, entitiesMap,
            mobileGraphicQ, equipQ, itemQ, itemSerialQ, root.Id, serial);

        if (isPlayer)
        {
            // Player-only button column at x=185. Buttons fire on release
            // (UiClick) per the UO Gump contract — UiPointerDown would
            // double-fire if the user drags out and back, and OOP's
            // ButtonAction.Activate also fires on mouse-up.
            var btnHelp = builder.AddButton(commands, (0x07EF, 0x07F0, 0x07F1), Vector3.UnitZ, new Vector2(185, 44 + 27 * 0));
            btnHelp.Observe((On<UiClick> _, Res<NetClient> net) => net.Value.Send_HelpRequest());
            commands.AddChild(root.Id, btnHelp.Id);

            // Options: OOP opens OptionsGump (client-side). The ECS OptionsGump
            // is a small launcher window (see OptionsGumpPlugin).
            var btnOptions = builder.AddButton(commands, (0x07D6, 0x07D7, 0x07D8), Vector3.UnitZ, new Vector2(185, 44 + 27 * 1));
            btnOptions.Observe((On<UiClick> _,
                                Commands cmd,
                                Res<UiZCounter> z,
                                Res<UiSurface> surf,
                                Res<OptionsUiState> st,
                                Query<Data<OptionsWindow>> existing) =>
                OptionsGumpPlugin.OpenOrFocus(cmd, z.Value, surf.Value, st.Value, existing));
            commands.AddChild(root.Id, btnOptions.Id);

            var btnLogout = builder.AddButton(commands, (0x07D9, 0x07DA, 0x07DB), Vector3.UnitZ, new Vector2(185, 44 + 27 * 2));
            btnLogout.Observe((On<UiClick> _,
                               Commands cmd,
                               Res<GumpBuilder> gb,
                               Res<AssetsServer> a,
                               Res<UiZCounter> z,
                               Res<UiSurface> surf,
                               Query<Data<LogoutGumpWindow>> existing) =>
                LogoutGumpPlugin.OpenOrFocus(cmd, gb.Value, a.Value, z.Value, surf.Value, existing));
            commands.AddChild(root.Id, btnLogout.Id);

            // Quests button (CV >= 500A — assume so). 0xD7 with subcommand
            // 0x32 mirrors main's GameActions.RequestQuestMenu.
            var btnQuests = builder.AddButton(commands, (0x57B5, 0x57B7, 0x57B6), Vector3.UnitZ, new Vector2(185, 44 + 27 * 3));
            btnQuests.Observe((On<UiClick> _, Res<NetClient> net, Res<GameContext> ctx) =>
            {
                if (ctx.Value.PlayerSerial != 0)
                    net.Value.Send_QuestMenuRequest(ctx.Value.PlayerSerial);
            });
            commands.AddChild(root.Id, btnQuests.Id);

            // Skills: re-request server-side skills (refresh values) and open
            // (or focus) the StandardSkillsGump.
            var btnSkills = builder.AddButton(commands, (0x07DF, 0x07E0, 0x07E1), Vector3.UnitZ, new Vector2(185, 44 + 27 * 4));
            btnSkills.Observe((On<UiClick> _,
                               Commands cmd,
                               Res<NetClient> net,
                               Res<GameContext> ctx,
                               Res<GumpBuilder> gb,
                               Res<AssetsServer> a,
                               Res<UiZCounter> z,
                               Res<PlayerSkills> sk,
                               Query<Data<SkillsWindow>> existing) =>
            {
                if (ctx.Value.PlayerSerial != 0)
                    net.Value.Send_SkillsRequest(ctx.Value.PlayerSerial);
                SkillsGumpPlugin.OpenOrFocus(cmd, gb.Value, a.Value, z.Value, sk.Value, existing);
            });
            commands.AddChild(root.Id, btnSkills.Id);

            // Guild: mirrors OOP's GameActions.OpenGuildGump → 0xD7 subcommand
            // 0x28 with trailing 0x0A. Server pushes the guild menu gump.
            var btnGuild = builder.AddButton(commands, (0x57B2, 0x57B4, 0x57B3), Vector3.UnitZ, new Vector2(185, 44 + 27 * 5));
            btnGuild.Observe((On<UiClick> _, Res<NetClient> net, Res<GameContext> ctx) =>
            {
                if (ctx.Value.PlayerSerial != 0)
                    net.Value.Send_GuildMenuRequest(ctx.Value.PlayerSerial);
            });
            commands.AddChild(root.Id, btnGuild.Id);

            // Peace/war toggle. Initial graphic mirrors player's current
            // ServerFlags.WarMode bit; click sends Send_ChangeWarMode(!current);
            // server echoes 0x72 -> OnWarmode reinserts ServerFlags ->
            // RefreshWarModeButtons observer flips this button's UOButton
            // gump triplet (peace 0x07E5/6/7 <-> war 0x07E8/9/A).
            bool isWarMode = false;
            if (entitiesMap.TryGet(serial, out var playerEnt) && flagsQ.TryGet(playerEnt, out var flagsRow))
            {
                var (_, sf) = flagsRow;
                isWarMode = (sf.Ref.Value & Flags.WarMode) != 0;
            }
            var warIds = isWarMode ? (0x07E8, 0x07E9, 0x07EA) : (0x07E5, 0x07E6, 0x07E7);
            var btnPeaceWar = builder.AddButton(commands, ((ushort)warIds.Item1, (ushort)warIds.Item2, (ushort)warIds.Item3), Vector3.UnitZ, new Vector2(185, 44 + 27 * 6))
                .Insert(new PaperdollWarModeButton { MobileSerial = serial });
            btnPeaceWar.Observe((On<UiClick> _, Res<NetClient> net, Query<Data<ServerFlags>, With<Player>> playerQ) =>
            {
                bool current = false;
                foreach (var (_, sf) in playerQ)
                {
                    current = (sf.Ref.Value & Flags.WarMode) != 0;
                    break;
                }
                net.Value.Send_ChangeWarMode(!current);
            });
            commands.AddChild(root.Id, btnPeaceWar.Id);
        }

        // Status button — re-request character status (server pushes 0x11) and
        // open the player status bar (single instance, version-based layout).
        var capturedPlayerSerial = gameCtx.PlayerSerial;
        var btnStatus = builder.AddButton(commands, (0x07EB, 0x07EC, 0x07ED), Vector3.UnitZ, new Vector2(185, 44 + 27 * 7));
        btnStatus.Observe((On<UiClick> _,
                           Res<NetClient> net,
                           Commands cmd,
                           Res<GumpBuilder> gb,
                           Res<AssetsServer> assets,
                           Res<GameContext> ctx,
                           Res<ClassicUO.Configuration.Profile> profile,
                           Res<UiZCounter> z,
                           Query<Data<StatusBarWindow>> existing) =>
        {
            if (capturedPlayerSerial != 0)
                net.Value.Send_StatusRequest(capturedPlayerSerial);
            StatusBarPlugin.OpenOrFocus(cmd, gb.Value, assets.Value, ctx.Value, profile.Value, z.Value, existing);
        });
        commands.AddChild(root.Id, btnStatus.Id);

        // Virtue gump (0x0071) at (80, 4). OOP fires on left double-click —
        // route the synthesized UiDoubleClick to Send_GumpResponse mirroring
        // VirtueMenu_MouseDoubleClickEvent.
        {
            var capturedSerial = serial;
            var virtuePic = builder.AddGump(commands, 0x0071, Vector3.UnitZ, new Vector2(80, 4))
                .Insert(Interaction.None);
            virtuePic.Observe((On<UiDoubleClick> _, Res<NetClient> net, Res<GameContext> ctx) =>
            {
                if (ctx.Value.PlayerSerial != 0)
                    net.Value.Send_GumpResponse(ctx.Value.PlayerSerial, 0x000001CD, 0x00000001,
                        new uint[] { capturedSerial }, Array.Empty<Tuple<ushort, string>>());
            });
            commands.AddChild(root.Id, virtuePic.Id);
        }

        // Title label (39, 262): ASCII font 1, hue 0x0386, maxWidth 185 — matches
        // OOP PaperdollGump's `new Label("", false, 0x0386, 185, font: 1)`. Uses a
        // WrappedText custom node (pre-wrapped via FontsLoader at maxWidth) rather
        // than a plain Bevy.UI Text label: Clay's word-wrap doesn't drive the UO
        // atlas, so long titles must be wrapped at spawn and the node sized to the
        // measured height.
        var titleText = title?.Trim() ?? string.Empty;
        const int titleW = 185;
        var (_, titleH) = UoFontRenderer.Measure(titleText, font: 1, titleW, isHtml: false, 0, htmlBg: false, ascii: true);
        var titleEnt = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(39), Top = Val.Px(262),
                Width = Val.Px(titleW), Height = Val.Px(Math.Max(titleH, 18)),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.WrappedText,
                    Hue = Vector3.UnitZ,
                    Text = titleText,
                    TextFont = 1,
                    TextHue = 0x0386,
                    WrapWidth = titleW,
                    IsHtml = false,
                    TextAscii = true,
                }
            });
        commands.AddChild(root.Id, titleEnt.Id);
    }

    // Pixel-perfect double-click dispatch for the scroll gump-pics behind the
    // body. UiPick.Topmost over the raw mouse passes through the body's
    // transparent corner pixels to the scroll, then we fire the OOP action by
    // kind. Mirrors HealthBarPlugin.WindowInteractions' UiPick double-click.
    private static void ScrollDClick(
        Commands commands,
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Res<NetClient> net,
        Res<GumpBuilder> builder,
        Res<UiZCounter> zCounter,
        Res<GameContext> gameCtx,
        Res<PartyState> party,
        Res<NetworkEntitiesMap> map,
        PaperdollScrollParams p)
    {
        if (!mouse.Value.IsPressedDouble(Input.MouseButtonType.Left)) return;

        var hit = UiPick.Topmost(mouse.Value.Position, assets.Value, p.Rendered);
        if (!hit.Found || !p.Scrolls.TryGet(hit.Entity, out var scrollRow)) return;

        var (_, sc) = scrollRow;
        switch (sc.Ref.Kind)
        {
            case ScrollKind.Profile:
                net.Value.Send_ProfileRequest(sc.Ref.Serial);
                break;
            case ScrollKind.Party:
                PartyGumpPlugin.OpenOrFocus(commands, builder.Value, assets.Value, zCounter.Value,
                    party.Value, gameCtx.Value.PlayerSerial, map.Value, p.Names, p.PartyWindows);
                break;
            case ScrollKind.CombatBook:
                CombatBookGumpPlugin.OpenOrFocus(commands, builder.Value, assets.Value, zCounter.Value,
                    gameCtx.Value, p.CombatBooks);
                break;
            case ScrollKind.RacialBook:
                var race = RaceType.HUMAN;
                foreach (var (_, pd) in p.Player) { race = pd.Ref.Race; break; }
                RacialBookGumpPlugin.OpenOrFocus(commands, builder.Value, assets.Value, zCounter.Value,
                    race, p.RacialBooks);
                break;
        }
    }

    // Number of scroll gump-pics BuildWindow adds at the front of root.Children
    // before the body (combat book, racial book, profile, party). Must match the
    // adds in BuildWindow exactly — it's the start index for the body subtree so
    // the body paints over the scrolls. Profile is always present; party only for
    // the player; the books gate on PaladinNecromancer tooltips + client version.
    internal static int BehindBodyScrollCount(GameContext gameCtx, uint serial)
    {
        bool isPlayer = serial == gameCtx.PlayerSerial;
        bool showBooks = (gameCtx.ClientFeatures & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0 && isPlayer;
        bool showRacialBook = showBooks && gameCtx.ClientVersion >= ClientVersion.CV_7000;
        return 1                              // profile (player + other)
             + (isPlayer ? 1 : 0)             // party manifest
             + (showBooks ? 1 : 0)            // combat book
             + (showRacialBook ? 1 : 0);      // racial book
    }

    // Body sprite + equipment overlays. Split out from BuildWindow so the
    // refresh system can rebuild just this subtree on EquipmentSlots change.
    // Every entity spawned here is tagged PaperdollBodyChild { WindowEntity }
    // so the refresh can despawn precisely these without touching frames /
    // buttons / labels.
    private static void BuildBodyAndOverlays(
        Commands commands,
        AssetsServer assets,
        GumpBuilder builder,
        GameContext gameCtx,
        ClassicUO.Assets.StaticTiles[] tileData,
        NetworkEntitiesMap entitiesMap,
        Query<Data<Graphic, Hue>, Filter<Optional<Hue>>> mobileGraphicQ,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<Graphic, Hue>, Filter<Optional<Hue>>> itemQ,
        Query<Data<NetworkSerial>> itemSerialQ,
        ulong rootId,
        uint serial)
    {
        // Mobile lookup for body graphic + equipment iteration. Tolerant of
        // a missing mobile entity (paperdoll for an unloaded NPC just gets a
        // blank background — preferable to crashing the spawn).
        ushort mobileGraphic = 0;
        ushort mobileHue = 0;
        EquipmentSlots? slots = null;
        if (entitiesMap.TryGet(serial, out var mobileEnt))
        {
            if (mobileGraphicQ.TryGet(mobileEnt, out var mobileGfxRow))
            {
                var (_, gfx, hue) = mobileGfxRow;
                mobileGraphic = gfx.Ref.Value;
                if (hue.IsValid()) mobileHue = hue.Ref.Value;
            }
            if (equipQ.TryGet(mobileEnt, out var equipRow))
            {
                var (_, s) = equipRow;
                slots = s.Ref;
            }
        }

        bool isFemale = ResolveIsFemale(mobileGraphic);
        var bodyId = ResolveBodyGraphic(mobileGraphic, isFemale);
        if (bodyId == 0) return;

        // Body children insert just AFTER the scroll gump-pics (combat / racial
        // book, profile, party), which BuildWindow adds first so they occupy the
        // front of root.Children. Inserting body after them puts the body +
        // equipment overlays ON TOP of the scroll art (legacy paint order) while
        // still UNDER the appended chrome (buttons / virtue / title) — so wide
        // gumps (cloaks/robes) can't cover the x=185 button column. On rebuild
        // the scrolls persist at the front, so the same start index re-lands the
        // body between scrolls and chrome.
        int childIdx = BehindBodyScrollCount(gameCtx, serial);

        // Body uses partial hue so the skin tone tints only the grayscale skin
        // pixels — the baked-in white underwear stays white (legacy sets
        // IsPartialHue=true on the body GumpPic).
        var bodyHue = ToShaderHue(mobileHue, partial: true);
        var body = builder.AddGump(commands, bodyId, bodyHue, new Vector2(8, 19))
            .Insert(new PaperdollBodyChild { WindowEntity = rootId });
        commands.AddChild(rootId, body.Id, childIdx++);

        // Equipment slot frames (always) + the equipped item's art icon, in
        // bg → icon → frame paint order. The 0x2344 frame is a hollow border,
        // so the centered icon shows through it. Icons carry PaperdollEquipUI
        // so each slot is draggable (lift the item out) like the body overlays.
        // AddArtSized clamps oversized art to the 19x20 slot (renderer scales
        // down + centers, never upscales).
        foreach (var (slLayer, sx, sy) in s_slotLayout)
        {
            // Resolve the equipped item (art + serial) up front so the slot bg
            // and frame can carry it too: the tooltip fires for the whole 19x20
            // slot square (bg/frame/icon all resolve the same item), not just the
            // item icon's opaque pixels (OOP shows it anywhere over the slot).
            ushort slotArtId = 0;
            Vector3 slotArtHue = Vector3.UnitZ;
            uint slotItemSerial = 0;
            if (slots.HasValue)
            {
                var slotItemEnt = slots.Value[slLayer];
                if (slotItemEnt != 0 && itemQ.TryGet(slotItemEnt, out var slotItemRow) && itemSerialQ.TryGet(slotItemEnt, out var slotSerialRow))
                {
                    var (_, sig, sih) = slotItemRow;
                    if (sig.Ref.Value != 0)
                    {
                        slotArtId = sig.Ref.Value;
                        slotArtHue = sih.IsValid() ? ToShaderHue((ushort)(sih.Ref.Value & 0x3FFF), IsPartialHue(slotArtId, tileData)) : Vector3.UnitZ;
                        var (_, slNs) = slotSerialRow;
                        slotItemSerial = slNs.Ref.Value;
                    }
                }
            }

            var slotBg = builder.AddGumpTiled(commands, 0x243A, Vector3.UnitZ, new Vector2(sx, sy), new Vector2(19, 20))
                .Insert(new PaperdollBodyChild { WindowEntity = rootId })
                .Insert(new PaperdollSlot { MobileSerial = serial, Layer = slLayer, ItemSerial = slotItemSerial });
            commands.AddChild(rootId, slotBg.Id, childIdx++);

            if (slotArtId != 0)
            {
                // No Interaction component: PickupPlugin's latch reads
                // bbox+PixelHit directly via equipUiQ — Interaction is
                // unused. Adding it makes Bevy.UI's InteractionSystem
                // route hovers/presses to this icon, which steals
                // UiPointerDown from buttons/chrome when the icon's
                // bbox+opaque-pixel overlaps theirs.
                var icon = builder.AddArtSized(commands, slotArtId, slotArtHue, new Vector2(sx, sy), new Vector2(19, 20))
                    .Insert(new PaperdollBodyChild { WindowEntity = rootId })
                    .Insert(new PaperdollEquipUI
                    {
                        ItemSerial = slotItemSerial,
                        Layer = slLayer,
                        MobileSerial = serial,
                        WindowEntity = rootId,
                    })
                    // Same slot serial as bg/frame so the tooltip resolves via the
                    // single PaperdollSlot query regardless of which slot element
                    // is the topmost hit.
                    .Insert(new PaperdollSlot { MobileSerial = serial, Layer = slLayer, ItemSerial = slotItemSerial });
                commands.AddChild(rootId, icon.Id, childIdx++);
            }

            var slotFrame = builder.AddGump(commands, 0x2344, Vector3.UnitZ, new Vector2(sx, sy))
                .Insert(new PaperdollBodyChild { WindowEntity = rootId })
                .Insert(new PaperdollSlot { MobileSerial = serial, Layer = slLayer, ItemSerial = slotItemSerial });
            commands.AddChild(rootId, slotFrame.Id, childIdx++);
        }

        if (!slots.HasValue) return;

        // Equipment draw-order via the shared PaperdollOrder algorithm (same as
        // the in-world views and main's PaperDollInteractable): pick a base
        // table by arms/torso AnimID, apply per-graphic reorder rules, then
        // paint back-to-front. gfx[layer] = the item's tile AnimID — the rules
        // are keyed on the gump/equip display id, not the world tile graphic.
        bool isGargoyle = Races.IsGargoyle(gameCtx.ClientVersion, mobileGraphic);

        Span<ushort> equipGfx = stackalloc ushort[PaperdollOrder.N];
        equipGfx.Clear();
        for (int l = (int)GameLayer.OneHanded; l <= (int)GameLayer.Legs; l++)
        {
            var le = slots.Value[(GameLayer)l];
            if (le == 0 || !itemQ.TryGet(le, out var layerRow)) continue;
            var (_, lg, _) = layerRow;
            var lgfx = lg.Ref.Value;
            if (lgfx != 0 && lgfx < tileData.Length) equipGfx[l] = tileData[lgfx].AnimID;
        }

        Span<GameLayer> rawOrder = stackalloc GameLayer[PaperdollOrder.N];
        PaperdollOrder.Build(equipGfx, isFemale || isGargoyle, rawOrder);
        Span<GameLayer> drawOrder = stackalloc GameLayer[PaperdollOrder.N];
        int orderCount = PaperdollOrder.Filter(rawOrder, includeBackpack: false, drawOrder);

        for (int oi = 0; oi < orderCount; oi++)
        {
            var layer = drawOrder[oi];
            var itemEnt = slots.Value[layer];
            if (itemEnt == 0) continue;
            if (!itemQ.TryGet(itemEnt, out var overlayRow)) continue;

            var (_, ig, ih) = overlayRow;
            var itemGraphic = ig.Ref.Value;
            if (itemGraphic == 0 || itemGraphic >= tileData.Length) continue;
            var animId = tileData[itemGraphic].AnimID;
            if (animId == 0) continue;

            var offset = isFemale ? Constants.FEMALE_GUMP_OFFSET : Constants.MALE_GUMP_OFFSET;
            var equipGump = (ushort)(animId + offset);
            // Try male fallback if female gump missing (main does the same
            // — IsAnimExistsInGump swaps offset on miss).
            ref readonly var info = ref assets.Gumps.GetGump(equipGump);
            if (info.Texture == null)
            {
                var altOffset = isFemale ? Constants.MALE_GUMP_OFFSET : Constants.FEMALE_GUMP_OFFSET;
                equipGump = (ushort)(animId + altOffset);
            }

            var equipHue = ih.IsValid() ? ToShaderHue((ushort)(ih.Ref.Value & 0x3FFF), IsPartialHue(itemGraphic, tileData)) : Vector3.UnitZ;
            var equipPic = builder.AddGump(commands, equipGump, equipHue, new Vector2(8, 19))
                .Insert(new PaperdollBodyChild { WindowEntity = rootId });

            // Liftability mirrors main's PaperDollInteractable.CanLift: Hair and
            // Beard are never liftable (they're part of the body). Liftable
            // overlays carry PaperdollEquipUI so PickupPlugin's latch (bbox +
            // PixelHit, no Interaction filter) can resolve them; non-liftable
            // ones just render. No Interaction component on purpose — adding it
            // would put the overlay in Bevy.UI's interactives query and let its
            // bbox+opaque-pixel steal UiPointerDown from buttons/chrome it
            // overlaps (wide cloaks/robes can reach the x=185 button column).
            bool liftable = layer != GameLayer.Hair && layer != GameLayer.Beard;
            if (liftable && itemSerialQ.TryGet(itemEnt, out var liftSerialRow))
            {
                var (_, itemNs) = liftSerialRow;
                equipPic.Insert(new PaperdollEquipUI
                {
                    ItemSerial = itemNs.Ref.Value,
                    Layer = layer,
                    MobileSerial = serial,
                    WindowEntity = rootId,
                })
                // Same PaperdollSlot the slot squares carry, so TooltipPlugin's
                // slotQ resolves the worn item's OPL when hovering the body art
                // (TrackAndRender is at its 16-param cap — no room for a separate
                // PaperdollEquipUI query).
                .Insert(new PaperdollSlot { MobileSerial = serial, Layer = layer, ItemSerial = itemNs.Ref.Value });
            }
            commands.AddChild(rootId, equipPic.Id, childIdx++);
        }

        // Backpack overlay. Layer.Backpack uses MALE_GUMP_OFFSET regardless
        // of body sex (matches main). Skip the per-profile backpack-style
        // swap (Suede / Polar Bear / Ghoul) — that's a settings concern.
        var backpackEnt = slots.Value[GameLayer.Backpack];
        if (backpackEnt != 0 && itemQ.TryGet(backpackEnt, out var backpackRow))
        {
            var (_, bg, bh) = backpackRow;
            var backpackGraphic = bg.Ref.Value;
            if (backpackGraphic != 0 && backpackGraphic < tileData.Length)
            {
                var bpAnimId = tileData[backpackGraphic].AnimID;
                if (bpAnimId != 0)
                {
                    var bpGump = (ushort)(bpAnimId + Constants.MALE_GUMP_OFFSET);
                    // Main shifts the backpack left by 6 when PaperdollBooks
                    // is on so the combat-book sprite has room. Cheap check:
                    // re-derive from GameContext.
                    int bx = (gameCtx.ClientFeatures & CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS) != 0 ? 6 : 0;
                    var bpHue = bh.IsValid() ? ToShaderHue((ushort)(bh.Ref.Value & 0x3FFF)) : Vector3.UnitZ;
                    // Body sprite lives at (8, 19) absolute; backpack offsets
                    // from there (-bx, 0). Backpack is NOT liftable in main
                    // (GumpPicEquipment with no CanLift), but IS double-click-
                    // to-open: synthesize dclick via UiClick + timestamp gap
                    // (Bevy.UI has no UiDoubleClick). 350ms matches main's
                    // Mouse.MOUSE_DELAY_DOUBLE_CLICK. Interaction.None on the
                    // sprite so UiClick is routed here; drag on body area
                    // still works because UiMovable lives on root (drag latch
                    // reads root's Interaction, not children's).
                    var bpPic = builder.AddGump(commands, bpGump, bpHue, new Vector2(8 - bx, 19))
                        .Insert(new PaperdollBodyChild { WindowEntity = rootId });
                    if (itemSerialQ.TryGet(backpackEnt, out var bpSerialRow))
                    {
                        var (_, bpNs) = bpSerialRow;
                        bpPic.Insert(Interaction.None)
                             .Insert(new PaperdollBackpackUI
                             {
                                 ItemSerial = bpNs.Ref.Value,
                                 MobileSerial = serial,
                             })
                             // Tooltip via slotQ (same as slots/equipment overlays).
                             .Insert(new PaperdollSlot { MobileSerial = serial, Layer = GameLayer.Backpack, ItemSerial = bpNs.Ref.Value });
                    }
                    commands.AddChild(rootId, bpPic.Id, childIdx++);
                }
            }
        }
    }

    // Mirrors PaperDollInteractable.UpdateUI's body picker.
    internal static ushort ResolveBodyGraphic(ushort mobileGraphic, bool isFemale)
    {
        return mobileGraphic switch
        {
            0x0191 or 0x0193 => 0x000D,
            0x025D           => 0x000E,
            0x025E           => 0x000F,
            0x029A or 0x02B6 => 0x029A,
            0x029B or 0x02B7 => 0x0299,
            0x04E5           => 0xC835,
            0x03DB           => 0x000C,
            _ => isFemale ? (ushort)0x000D : (ushort)0x000C,
        };
    }

    // Hue=0 means "no hue applied" — must select the pass-through shader
    // (Vector3.UnitZ), NOT (0, 1, 1) which selects SHADER_HUED with hue 0
    // and renders the sprite black/transparent depending on backend.
    //
    // partial=true selects SHADER_PARTIAL_HUED (mode 2 in IsometricWorld.fx):
    // the hue tints only grayscale (r==g==b) pixels, leaving baked-in color
    // alone — white underwear on the body, dyed-cloth accents on equipment.
    // Mirrors legacy GumpPic.IsPartialHue. (The hue index here is the raw
    // 1-based server hue, not GetHueVector's -1 form — that off-by-one is the
    // shared ToShaderHue convention across the ECS UI plugins, left untouched.)
    private static Vector3 ToShaderHue(ushort hue, bool partial = false)
        => hue == 0 ? Vector3.UnitZ : new Vector3(hue, partial ? 2f : 1f, 1f);

    // Per-item partial-hue flag from tiledata (mirrors legacy
    // GumpPicEquipment's IsPartialHue = ItemData.IsPartialHue).
    private static bool IsPartialHue(ushort graphic, ClassicUO.Assets.StaticTiles[] tileData)
        => graphic < tileData.Length && tileData[graphic].IsPartialHue;

    internal static bool ResolveIsFemale(ushort mobileGraphic)
    {
        // Best-effort: known female body ids. PlayerData.IsFemale would be
        // more authoritative but reads a separate component; treat unknown
        // ids as male (matches main's default fall-through).
        return mobileGraphic == 0x0191 || mobileGraphic == 0x0193
            || mobileGraphic == 0x025D || mobileGraphic == 0x029B
            || mobileGraphic == 0x02B7;
    }
}

// Marker carried by the paperdoll bg root. Lets DisposeOnLogout walk all
// open windows and lets duplicate-open detection deduplicate by serial.
internal struct PaperdollWindow
{
    public uint Serial;
    public bool IsPlayer;
}

internal enum ScrollKind : byte { Profile, Party, CombatBook, RacialBook }

// Tag on the profile / party / combat-book / racial-book scroll gump-pics.
// These render BEHIND the paperdoll body, so Clay's bounds-based Interaction
// can't route their double-clicks (the body's bbox covers them). ScrollDClick
// resolves them via UiPick (pixel-perfect — the body's transparent corner
// pixels pass the hit through) and fires the action by Kind. Serial is the
// paperdoll's mobile serial (used only by the profile request).
internal struct PaperdollScrollUI
{
    public ScrollKind Kind;
    public uint Serial;
}

// Queries for ScrollDClick, bundled so the system stays under the param-arity
// limit. Rendered is the UiPick element set; the rest resolve the scroll under
// the cursor and the open/focus targets for each scroll action.
internal sealed class PaperdollScrollParams : CompositeSystemParam
{
    public readonly Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> Rendered;
    public readonly Query<Data<PaperdollScrollUI>> Scrolls;
    public readonly Query<Data<EntityName>> Names;
    public readonly Query<Data<PlayerData>, With<Player>> Player;
    public readonly Query<Data<CombatBookWindow>> CombatBooks;
    public readonly Query<Data<RacialBookWindow>> RacialBooks;
    public readonly Query<Data<PartyManifestWindow>> PartyWindows;

    public PaperdollScrollParams()
    {
        Rendered     = Add(new Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>>());
        Scrolls      = Add(new Query<Data<PaperdollScrollUI>>());
        Names        = Add(new Query<Data<EntityName>>());
        Player       = Add(new Query<Data<PlayerData>, With<Player>>());
        CombatBooks  = Add(new Query<Data<CombatBookWindow>>());
        RacialBooks  = Add(new Query<Data<RacialBookWindow>>());
        PartyWindows = Add(new Query<Data<PartyManifestWindow>>());
    }
}

// Carried by each of the 15 equipment slot frames. Future UpdateSlots
// system (not in v1) will query this to refresh the slot's child item icon
// when the mobile's EquipmentSlots changes.
internal struct PaperdollSlot
{
    public uint MobileSerial;
    public GameLayer Layer;
    // Serial of the item occupying this slot (0 = empty). Lets the tooltip fire
    // for the whole slot square (bg + frame carry it), not just the item icon.
    public uint ItemSerial;
}

// Tag on the body sprite + each equipment overlay sprite. Refresh system
// queries this to despawn the dynamic subtree precisely, leaving static
// children (slot frames, buttons, profile pics, label) untouched.
internal struct PaperdollBodyChild
{
    public ulong WindowEntity;
}

// Carried by the player's peace/war toggle button. RefreshWarModeButtons
// observer matches MobileSerial to the player whose ServerFlags just
// flipped and rewrites UOButton's Normal/Pressed/Over to the right gump
// triplet (peace 0x07E5/6/7 or war 0x07E8/9/A).
internal struct PaperdollWarModeButton
{
    public uint MobileSerial;
}

// Carried by the backpack overlay sprite on a paperdoll. The global
// UiClick observer matches this to know which clicked sprite is a
// double-click target + which item to Send_DoubleClick on.
internal struct PaperdollBackpackUI
{
    public uint ItemSerial;
    public uint MobileSerial;
}

// Carried by each equipment overlay sprite that represents a real equipped
// item (clothing/armor/backpack/etc). PickupPlugin uses this to (a) hit-
// test paperdoll drags on press and (b) resolve to the backing game
// entity via NetworkEntitiesMap, then send Send_PickUpRequest. Mirrors
// main's PaperDollInteractable.GumpPicEquipment role.
internal struct PaperdollEquipUI
{
    public uint ItemSerial;
    public GameLayer Layer;
    public uint MobileSerial;
    public ulong WindowEntity;
}

// Composite param for the OnInsert<EquipmentSlots> observer. Bundles the
// resources + queries the rebuild needs into one param so the observer's
// generic arity stays small. Commands is NOT here — it's passed top-level
// so the observer harness auto-applies its deferred ops. GraphicHueQ doubles
// for mobile + item lookups; SerialQ for mobile + item serials.
internal sealed class PaperdollRebuildParams : CompositeSystemParam
{
    public readonly Res<NetworkEntitiesMap> Entities;
    public readonly Res<AssetsServer> Assets;
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<GameContext> GameCtx;
    public readonly Res<UOFileManager> Files;
    public readonly Query<Data<NetworkSerial>> SerialQ;
    public readonly Query<Data<PaperdollWindow>> WindowsQ;
    public readonly Query<Data<PaperdollBodyChild>> BodyChildQ;
    public readonly Query<Data<Graphic, Hue>, Filter<Optional<Hue>>> GraphicHueQ;
    public readonly Query<Data<EquipmentSlots>> EquipQ;

    public PaperdollRebuildParams()
    {
        Entities    = Add(new Res<NetworkEntitiesMap>());
        Assets      = Add(new Res<AssetsServer>());
        Builder     = Add(new Res<GumpBuilder>());
        GameCtx     = Add(new Res<GameContext>());
        Files       = Add(new Res<UOFileManager>());
        SerialQ     = Add(new Query<Data<NetworkSerial>>());
        WindowsQ    = Add(new Query<Data<PaperdollWindow>>());
        BodyChildQ  = Add(new Query<Data<PaperdollBodyChild>>());
        GraphicHueQ = Add(new Query<Data<Graphic, Hue>, Filter<Optional<Hue>>>());
        EquipQ      = Add(new Query<Data<EquipmentSlots>>());
    }
}

// Composite param for the 0x88 (open paperdoll) observer. Same resources
// as the rebuild path plus UiZCounter (for focus bump) + FlagsQ (initial
// peace/war button graphic). GraphicHueQ is reused for both mobile body
// graphic and item icon graphics — same component signature.
internal sealed class PaperdollSpawnParams : CompositeSystemParam
{
    public readonly Res<NetworkEntitiesMap> Entities;
    public readonly Res<AssetsServer> Assets;
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<GameContext> GameCtx;
    public readonly Res<UOFileManager> Files;
    public readonly Res<UiZCounter> ZCounter;
    public readonly Query<Data<PaperdollWindow>> ExistingQ;
    public readonly Query<Data<NetworkSerial>> SerialQ;
    public readonly Query<Data<Graphic, Hue>, Filter<Optional<Hue>>> GraphicHueQ;
    public readonly Query<Data<EquipmentSlots>> EquipQ;
    public readonly Query<Data<ServerFlags>> FlagsQ;

    public PaperdollSpawnParams()
    {
        Entities    = Add(new Res<NetworkEntitiesMap>());
        Assets      = Add(new Res<AssetsServer>());
        Builder     = Add(new Res<GumpBuilder>());
        GameCtx     = Add(new Res<GameContext>());
        Files       = Add(new Res<UOFileManager>());
        ZCounter    = Add(new Res<UiZCounter>());
        ExistingQ   = Add(new Query<Data<PaperdollWindow>>());
        SerialQ     = Add(new Query<Data<NetworkSerial>>());
        GraphicHueQ = Add(new Query<Data<Graphic, Hue>, Filter<Optional<Hue>>>());
        EquipQ      = Add(new Query<Data<EquipmentSlots>>());
        FlagsQ      = Add(new Query<Data<ServerFlags>>());
    }
}
