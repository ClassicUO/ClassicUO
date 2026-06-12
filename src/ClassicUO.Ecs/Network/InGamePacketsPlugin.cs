using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct MobileBundle : IBundle
{
    public NetworkSerial Serial { get; init; }
    public MobAnimation Animation { get; init; }
    public ScreenPositionOffset Offset { get; init; }

    public void Insert(EntityCommands entity)
    {
        entity.Insert(Serial);
        entity.Insert(Animation);
        entity.Insert(Offset);
        entity.Insert<Mobiles>();
    }
}

internal readonly struct ItemBundle : IBundle
{
    public NetworkSerial Serial { get; init; }

    public void Insert(EntityCommands entity)
    {
        entity.Insert(Serial);
        entity.Insert<Items>();
    }
}

sealed class NetworkEntitiesMap
{
    private readonly Dictionary<uint, ulong> _entities = new();

    public EntityCommands GetOrCreate(Commands commands, uint serial)
    {
        if (_entities.TryGetValue(serial, out var id))
        {
            if (commands.Exists(id))
            {
                return commands.Entity(id);
            }

            _entities.Remove(serial);
        }

        var ent = commands.Spawn();
        if (SerialHelper.IsMobile(serial))
            ent.InsertBundle(new MobileBundle { Serial = new NetworkSerial { Value = serial } });
        else
            ent.InsertBundle(new ItemBundle { Serial = new NetworkSerial { Value = serial } });

        _entities.Add(serial, ent.Id);
        return ent;
    }

    public bool Remove(uint serial)
    {
        return _entities.Remove(serial, out var id);
    }

    public EntityCommands Get(Commands commands, uint serial)
    {
        if (_entities.TryGetValue(serial, out var id))
        {
            if (commands.Exists(id))
            {
                return commands.Entity(id);
            }

            _entities.Remove(serial);
        }

        return default;
    }

    public bool TryGet(Commands commands, uint serial, out EntityCommands entityCommands)
    {
        if (_entities.TryGetValue(serial, out var id))
        {
            if (commands.Exists(id))
            {
                entityCommands = commands.Entity(id);

                return true;
            }

            _entities.Remove(serial);
        }

        entityCommands = default;
        return false;
    }

    public bool TryGet(uint serial, out ulong entityId)
    {
        return _entities.TryGetValue(serial, out entityId);
    }

    public void Clear()
    {
        _entities.Clear();
    }

    public Dictionary<uint, ulong>.Enumerator GetEnumerator()
    {
        return _entities.GetEnumerator();
    }
}

readonly struct InGamePacketsPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new PlayerSkills());
        app.AddResource(new SpellbookStore());
        app.AddResource(new ObjectPropertyLists());

        app.AddObserver<On<PacketReceived<OnEnterWorldPacket_0x1B>>,
            Commands,
            Res<NetworkEntitiesMap>,
            Res<NetClient>,
            Res<Settings>,
            ResMut<GameContext>,
            ResMut<NextState<GameState>>>(OnEnterWorld);

        app.AddObserver<On<PacketReceived<OnLoginCompletePacket_0x55>>,
            Res<NetClient>,
            ResMut<GameContext>>(OnLoginComplete);

        app.AddObserver<On<PacketReceived<OnClientVersionPacket_0xBD>>,
            Res<NetClient>,
            Res<Settings>>(OnClientVersion);

        app.AddObserver<On<PacketReceived<OnUnicodeSpeechPacket_0xAE>>,
            Res<NetClient>,
            Res<Profile>,
            EventWriter<TextOverheadEvent>,
            EventWriter<SystemMessageEvent>>(OnUnicodeSpeech);

        app.AddObserver<On<PacketReceived<OnAsciiSpeechPacket_0x1C>>,
            Res<NetClient>,
            Res<Profile>,
            EventWriter<TextOverheadEvent>,
            EventWriter<SystemMessageEvent>>(OnAsciiSpeech);

        app.AddObserver<On<PacketReceived<OnClilocMessagePacket_0xC1>>,
            Res<UOFileManager>,
            Res<Profile>,
            EventWriter<TextOverheadEvent>,
            EventWriter<SystemMessageEvent>>(OnClilocMessage);

        app.AddObserver<On<PacketReceived<OnViewRangePacket_0xC8>>,
            ResMut<GameContext>>(OnViewRange);

        app.AddObserver<On<PacketReceived<OnExtendedCommandPacket_0xBF>>,
            Commands,
            Res<NetworkEntitiesMap>,
            Res<NetClient>,
            Res<UOFileManager>,
            ResMut<GameContext>,
            Res<DelayedAction>,
            ResMut<SpellbookStore>,
            ResMut<PopupMenuState>,
            InGameQueries>(OnExtendedCommand);

        app.AddObserver<On<PacketReceived<OnUpdateItemPacket_0x1A>>,
            Commands,
            Res<NetworkEntitiesMap>,
            Res<MultiCache>,
            EventWriter<MobileQueuedStep>,
            InGameQueries>(OnUpdateItem);

        app.AddObserver<On<PacketReceived<OnUpdateItemSAPacket_0xF3>>,
            Commands,
            Res<NetworkEntitiesMap>,
            Res<MultiCache>,
            EventWriter<MobileQueuedStep>,
            InGameQueries>(OnUpdateItemSA);

        app.AddObserver<On<PacketReceived<OnUpdateObjectPacket_0xD3>>,
            Commands,
            Res<NetworkEntitiesMap>,
            EventWriter<MobileQueuedStep>,
            InGameQueries>(OnUpdateObjectD3);

        app.AddObserver<On<PacketReceived<OnUpdateObjectAltPacket_0x78>>,
            Commands,
            Res<NetworkEntitiesMap>,
            EventWriter<MobileQueuedStep>,
            InGameQueries>(OnUpdateObject78);

        app.AddObserver<On<PacketReceived<OnDeleteObjectPacket_0x1D>>,
            Commands,
            Res<NetworkEntitiesMap>,
            Res<GameContext>,
            EventWriter<ContainerSlotEvent>,
            Query<Data<EquipmentSlots>>,
            Query<Data<NetworkSerial>>>(OnDeleteObject);

        app.AddObserver<On<PacketReceived<OnUpdatePlayerPacket_0x20>>,
            Commands,
            Res<NetworkEntitiesMap>,
            EventWriter<MobileQueuedStep>>(OnUpdatePlayer);

        app.AddObserver<On<PacketReceived<OnCharacterStatusPacket_0x11>>,
            Commands,
            ResMut<GameContext>,
            Res<NetworkEntitiesMap>,
            Res<Profile>,
            EventWriter<SystemMessageEvent>,
            Query<Data<PlayerData>>>(OnCharacterStatus);

        app.AddObserver<On<PacketReceived<OnMobileAttributesPacket_0x2D>>,
            Commands,
            Res<NetworkEntitiesMap>>(OnMobileAttributes);

        app.AddObserver<On<PacketReceived<OnEquipItemPacket_0x2E>>,
            Commands,
            Res<NetworkEntitiesMap>,
            InGameQueries,
            EventWriter<ContainerSlotEvent>>(OnEquipItem);

        app.AddObserver<On<PacketReceived<OnUpdateManaPacket_0xA2>>,
            Commands,
            Res<NetworkEntitiesMap>>(OnUpdateMana);

        app.AddObserver<On<PacketReceived<OnUpdateStaminaPacket_0xA3>>,
            Commands,
            Res<NetworkEntitiesMap>>(OnUpdateStamina);

        app.AddObserver<On<PacketReceived<OnUpdateHitsPacket_0xA1>>,
            Commands,
            Res<NetworkEntitiesMap>>(OnUpdateHits);

        app.AddObserver<On<PacketReceived<OnLockFeaturesPacket_0xB9_Pre60142>>,
            Res<UOFileManager>, ResMut<GameContext>>(OnLockFeaturesPre);

        app.AddObserver<On<PacketReceived<OnLockFeaturesPacket_0xB9_Post60142>>,
            Res<UOFileManager>, ResMut<GameContext>>(OnLockFeaturesPost);

        app.AddObserver<On<PacketReceived<OnCustomHousePacket_0xD8>>,
            Commands,
            Res<NetworkEntitiesMap>,
            Res<MultiCache>,
            InGameQueries>(OnCustomHouse);

        app.AddObserver<On<PacketReceived<OnSeasonChangePacket_0xBC>>,
            ResMut<GameContext>>(OnSeasonChange);

        app.AddObserver<On<PacketReceived<OnPlayerLightLevelPacket_0x4E>>,
            ResMut<GameContext>>(OnPlayerLightLevel);

        app.AddObserver<On<PacketReceived<OnServerLightLevelPacket_0x4F>>,
            ResMut<GameContext>>(OnServerLightLevel);

        app.AddObserver<On<PacketReceived<OnWarmodePacket_0x72>>,
            Commands,
            Query<Data<ServerFlags>, With<Player>>>(OnWarmode);

        // Empty stubs — every registered packet has at least one observer so
        // the trigger is consumed silently. Replace with a real observer when
        // the packet gets implemented.
        Stub<OnDamagePacket_0x0B>(app);
        Stub<OnHealthBarStatusPacket_0x16>(app);
        Stub<OnHealthBarStatusDetailsPacket_0x17>(app);
        Stub<OnDragAnimationPacket_0x23>(app);
        Stub<OnShowDeathScreenPacket_0x2C>(app);
        Stub<OnSwingPacket_0x2F>(app);
        Stub<OnPathfindingPacket_0x38>(app);
        app.AddObserver<On<PacketReceived<OnUpdateSkillsPacket_0x3A>>,
            ResMut<PlayerSkills>,
            Res<UOFileManager>,
            Res<Profile>,
            EventWriter<SystemMessageEvent>>(OnUpdateSkills);
        // 0x54 (sound effect) and 0x6D (play music) are handled by AudioPlugin.
        Stub<OnWeatherPacket_0x65>(app);
        // 0x66 (book pages) is handled by BookGumpPlugin.
        Stub<OnCharacterAnimationPacket_0x6E>(app);
        Stub<OnGraphicEffectPacket_0x70>(app);
        Stub<OnPingPacket_0x73>(app);
        Stub<OnBuyListPacket_0x74>(app);
        Stub<OnUpdateCharacterPacket_0x77>(app);
        Stub<OnOpenMenuPacket_0x7C>(app);
        Stub<OnCorpseEquipmentPacket_0x89>(app);
        // 0x93 (open book, old header) is handled by BookGumpPlugin.
        Stub<OnColorPickerPacket_0x95>(app);
        Stub<OnMovePlayerPacket_0x97>(app);
        Stub<OnUpdateNamePacket_0x98>(app);
        Stub<OnPlaceMultiPacket_0x99>(app);
        Stub<OnAsciiPromptPacket_0x9A>(app);
        Stub<OnSellListPacket_0x9E>(app);
        Stub<OnOpenUrlPacket_0xA5>(app);
        // 0xA6 (tip / notice scroll) is handled by TipNoticeGumpPlugin.
        Stub<OnAttackEntityPacket_0xAA>(app);
        Stub<OnTextEntryDialogPacket_0xAB>(app);
        Stub<OnShowDeathActionPacket_0xAF>(app);
        Stub<OnOpenGumpPacket_0xB0>(app);
        Stub<OnChatMessagePacket_0xB2>(app);
        // 0xB8 (CharacterProfile) handled by ProfileGumpPlugin.
        Stub<OnGraphicEffectC0Packet_0xC0>(app);
        Stub<OnUnicodePromptPacket_0xC2>(app);
        Stub<OnGraphicEffectC7Packet_0xC7>(app);
        app.AddObserver<On<PacketReceived<OnClilocMessageAffixPacket_0xCC>>,
            Res<UOFileManager>,
            Res<Profile>,
            EventWriter<TextOverheadEvent>,
            EventWriter<SystemMessageEvent>>(OnClilocMessageAffix);
        // 0xD1 reply is handled by LogoutGumpPlugin (client-initiated logout).
        Stub<OnUpdateCharacterAltPacket_0xD2>(app);
        // 0xD4 (open book) is handled by BookGumpPlugin.
        app.AddObserver<On<PacketReceived<OnMegaClilocPacket_0xD6>>,
            Res<UOFileManager>,
            ResMut<GameContext>,
            ResMut<ObjectPropertyLists>>(OnMegaCliloc);
        app.AddObserver<On<PacketReceived<OnOplInfoPacket_0xDC>>,
            ResMut<ObjectPropertyLists>>(OnOplInfo);
        Stub<OnOpenCompressedGumpPacket_0xDD>(app);
        Stub<OnUpdateMobileStatusPacket_0xDE>(app);
        // 0xDF (BuffDebuff) handled by BuffGumpPlugin.
        Stub<OnNewCharacterAnimationPacket_0xE2>(app);
        Stub<OnAddWaypointPacket_0xE5>(app);
        Stub<OnRemoveWaypointPacket_0xE6>(app);
        Stub<OnKrriosClientPacket_0xF0>(app);
        Stub<OnBoatMovingPacket_0xF6>(app);
        Stub<OnPacketListPacket_0xF7>(app);
    }

    static void Stub<T>(App app) where T : struct, IPacket
    {
        app.AddObserver<On<PacketReceived<T>>>(static trigger => { });
    }

    sealed class InGameQueries : ISystemParam
    {
        public Query<Data<HouseRevision>> qHouseRevision { get; } = new();
        public Query<Data<EquipmentSlots>> qEquipmentSlots { get; } = new();
        public Query<Data<WorldPosition, Graphic>> qPosAndGraphic { get; } = new();
        public Query<Empty, With<IsMulti>> qMultis { get; } = new();
        public Query<Empty, With<ContainedInto>> qContainedItems { get; } = new();

        public void Initialize(App app)
        {
            qHouseRevision.Initialize(app);
            qEquipmentSlots.Initialize(app);
            qPosAndGraphic.Initialize(app);
            qMultis.Initialize(app);
            qContainedItems.Initialize(app);
        }

        public void Fetch(App app)
        {
            qHouseRevision.Fetch(app);
            qEquipmentSlots.Fetch(app);
            qPosAndGraphic.Fetch(app);
            qMultis.Fetch(app);
            qContainedItems.Fetch(app);
        }

        public SystemParamAccess GetAccess()
        {
            var access = new SystemParamAccess();
            var houseRev = qHouseRevision.GetAccess();
            var equipSlots = qEquipmentSlots.GetAccess();
            var posAndGraphic = qPosAndGraphic.GetAccess();
            var multis = qMultis.GetAccess();
            var contained = qContainedItems.GetAccess();

            foreach (var read in houseRev.ReadResources) access.ReadResources.Add(read);
            foreach (var write in houseRev.WriteResources) access.WriteResources.Add(write);
            foreach (var read in equipSlots.ReadResources) access.ReadResources.Add(read);
            foreach (var write in equipSlots.WriteResources) access.WriteResources.Add(write);
            foreach (var read in posAndGraphic.ReadResources) access.ReadResources.Add(read);
            foreach (var write in posAndGraphic.WriteResources) access.WriteResources.Add(write);
            foreach (var read in multis.ReadResources) access.ReadResources.Add(read);
            foreach (var write in multis.WriteResources) access.WriteResources.Add(write);
            foreach (var read in contained.ReadResources) access.ReadResources.Add(read);
            foreach (var write in contained.WriteResources) access.WriteResources.Add(write);

            return access;
        }
    }

    static void OnEnterWorld(
        On<PacketReceived<OnEnterWorldPacket_0x1B>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<Settings> settings,
        ResMut<GameContext> gameCtx,
        ResMut<NextState<GameState>> state)
    {
        var packet = trig.Event.Packet;

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_200)
        {
            network.Value.Send_GameWindowSize(800, 400);
            network.Value.Send_Language(settings.Value.Language);
        }

        network.Value.Send_ClientVersion(settings.Value.ClientVersion);
        network.Value.Send_ClickRequest(packet.Serial);
        network.Value.Send_SkillsRequest(packet.Serial);

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_70796)
            network.Value.Send_ShowPublicHouseContent(true);

        gameCtx.Value.CenterX = packet.Position.X;
        gameCtx.Value.CenterY = packet.Position.Y;
        gameCtx.Value.CenterZ = packet.Position.Z;
        gameCtx.Value.PlayerSerial = packet.Serial;

        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Ecs.WorldPosition() { X = packet.Position.X, Y = packet.Position.Y, Z = packet.Position.Z })
            .Insert(new Ecs.Graphic() { Value = packet.Graphic })
            .Insert(new Facing() { Value = packet.Direction })
            .Insert(new MobileSteps() { Index = -1 })
            .Insert<Player>();

        state.Value.Set(GameState.GameScreen);
    }

    static void OnLoginComplete(
        On<PacketReceived<OnLoginCompletePacket_0x55>> trig,
        Res<NetClient> network,
        ResMut<GameContext> gameCtx)
    {
        _ = trig;

        if (gameCtx.Value.PlayerSerial == 0)
            return;

        network.Value.Send_StatusRequest(gameCtx.Value.PlayerSerial);
        network.Value.Send_OpenChat("");
        network.Value.Send_SkillsRequest(gameCtx.Value.PlayerSerial);

        // Mirror main's PacketHandlers.LoginComplete (GameScene.DoubleClickDelayed →
        // UseItemQueue): implicit double-click on local player to coax the server
        // into sending the autoload 0x88 paperdoll packet. The 0x80000000 high bit
        // is the protocol's explicit paperdoll-request flag — UseItemQueue ORs it
        // into every mobile serial. Without it the server runs the default mobile
        // action (Mobile.OnDoubleClick): paperdoll only on foot, and a *dismount*
        // when mounted — so a mounted login never opened the paperdoll.
        network.Value.Send_DoubleClick(gameCtx.Value.PlayerSerial | 0x80000000);

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_306E)
            network.Value.Send_ClientType(gameCtx.Value.Protocol);

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_305D)
            network.Value.Send_ClientViewRange(24);
    }

    static void OnClientVersion(
        On<PacketReceived<OnClientVersionPacket_0xBD>> trig,
        Res<NetClient> network,
        Res<Settings> settings)
    {
        _ = trig;
        network.Value.Send_ClientVersion(settings.Value.ClientVersion);
    }

    // Server messages carry the broadcast serial 0xFFFFFFFF and belong in the
    // bottom-left system log, not over an entity.
    const uint SystemSerial = 0xFFFF_FFFF;

    // Legacy MessageManager.HandleMessage drops ignored guild/alliance chat
    // before ANY display path (overhead, system log, journal) — same here, at
    // the event source.
    static bool IsIgnoredChat(MessageType type, Profile profile)
        => (type == MessageType.Guild && profile.IgnoreGuildMessages)
        || (type == MessageType.Alliance && profile.IgnoreAllianceMessages);

    static void OnUnicodeSpeech(
        On<PacketReceived<OnUnicodeSpeechPacket_0xAE>> trig,
        Res<NetClient> network,
        Res<Profile> profile,
        EventWriter<TextOverheadEvent> textOverHeadQueue,
        EventWriter<SystemMessageEvent> systemMsgQueue)
    {
        var packet = trig.Event.Packet;

        if (packet.IsSystemMessage)
        {
            network.Value.Send(
            [
                0x03, 0x00, 0x28, 0x20, 0x00, 0x34, 0x00, 0x03, 0xDB, 0x13,
                0x14, 0x3F, 0x45, 0x2C, 0x58, 0x0F, 0x5D, 0x44, 0x2E, 0x50,
                0x11, 0xDF, 0x75, 0x5C, 0xE0, 0x3E, 0x71, 0x4F, 0x31, 0x34,
                0x05, 0x4E, 0x18, 0x1E, 0x72, 0x0F, 0x59, 0xAD, 0xF5, 0x00
            ]);
            return;
        }

        if (IsIgnoredChat(packet.MessageType, profile.Value)) return;

        if (packet.Serial == SystemSerial)
        {
            systemMsgQueue.Send(new SystemMessageEvent { Text = packet.Text, Hue = packet.Hue, Font = (byte)packet.Font });
            return;
        }

        textOverHeadQueue.Send(new TextOverheadEvent
        {
            Serial = packet.Serial,
            Name = packet.Name,
            Hue = packet.Hue,
            Font = (byte)packet.Font,
            IsUnicode = true,
            Text = packet.Text,
            MessageType = packet.MessageType
        });
    }

    static void OnAsciiSpeech(
        On<PacketReceived<OnAsciiSpeechPacket_0x1C>> trig,
        Res<NetClient> network,
        Res<Profile> profile,
        EventWriter<TextOverheadEvent> textOverHeadQueue,
        EventWriter<SystemMessageEvent> systemMsgQueue)
    {
        var packet = trig.Event.Packet;

        if (packet.IsSystemMessage)
        {
            network.Value.Send_ACKTalk();
            return;
        }

        if (IsIgnoredChat(packet.MessageType, profile.Value)) return;

        if (packet.Serial == SystemSerial)
        {
            systemMsgQueue.Send(new SystemMessageEvent { Text = packet.Text, Hue = packet.Hue, Font = (byte)packet.Font });
            return;
        }

        textOverHeadQueue.Send(new TextOverheadEvent
        {
            Serial = packet.Serial,
            Name = packet.Name,
            Hue = packet.Hue,
            Font = (byte)packet.Font,
            IsUnicode = false,
            Text = packet.Text,
            MessageType = packet.MessageType
        });
    }

    // 0xC1 cliloc message — server text keyed by a cliloc id with tab-separated
    // arguments. System notices (broadcast serial) go to the bottom-left log;
    // anything tied to an entity (name labels, NPC barks) is overhead text.
    static void OnClilocMessage(
        On<PacketReceived<OnClilocMessagePacket_0xC1>> trig,
        Res<UOFileManager> files,
        Res<Profile> profile,
        EventWriter<TextOverheadEvent> textOverHeadQueue,
        EventWriter<SystemMessageEvent> systemMsgQueue)
    {
        var packet = trig.Event.Packet;
        if (IsIgnoredChat(packet.MessageType, profile.Value)) return;
        var text = files.Value.Clilocs.Translate((int)packet.Cliloc, packet.Arguments);
        if (string.IsNullOrEmpty(text)) return;

        if (packet.Serial == SystemSerial || packet.Serial == 0)
            systemMsgQueue.Send(new SystemMessageEvent { Text = text, Hue = packet.Hue, Font = (byte)packet.Font });
        else
            textOverHeadQueue.Send(new TextOverheadEvent
            {
                Serial = packet.Serial,
                Name = packet.Name,
                Hue = packet.Hue,
                Font = (byte)packet.Font,
                IsUnicode = true,
                Text = text,
                MessageType = packet.MessageType,
            });
    }

    // 0xCC cliloc-with-affix — like 0xC1 but the server tacks an extra
    // prepend/append string onto the translated cliloc, and an affix System flag
    // can force the line into the system log. Mirrors legacy PacketHandlers.
    static void OnClilocMessageAffix(
        On<PacketReceived<OnClilocMessageAffixPacket_0xCC>> trig,
        Res<UOFileManager> files,
        Res<Profile> profile,
        EventWriter<TextOverheadEvent> textOverHeadQueue,
        EventWriter<SystemMessageEvent> systemMsgQueue)
    {
        var packet = trig.Event.Packet;
        if (IsIgnoredChat(packet.MessageType, profile.Value)) return;
        var text = files.Value.Clilocs.Translate((int)packet.Cliloc, packet.Arguments);
        if (string.IsNullOrEmpty(text)) return;

        if (!string.IsNullOrWhiteSpace(packet.Affix))
            text = (packet.AffixType & AffixType.Prepend) != 0
                ? $"{packet.Affix}{text}"
                : $"{text}{packet.Affix}";

        var type = (packet.AffixType & AffixType.System) != 0 ? MessageType.System : packet.MessageType;

        if (packet.Serial == SystemSerial || packet.Serial == 0 || type == MessageType.System)
            systemMsgQueue.Send(new SystemMessageEvent { Text = text, Hue = packet.Hue, Font = (byte)packet.Font });
        else
            textOverHeadQueue.Send(new TextOverheadEvent
            {
                Serial = packet.Serial,
                Name = packet.Name,
                Hue = packet.Hue,
                Font = (byte)packet.Font,
                IsUnicode = true,
                Text = text,
                MessageType = type,
            });
    }

    static void OnViewRange(
        On<PacketReceived<OnViewRangePacket_0xC8>> trig,
        ResMut<GameContext> gameCtx)
    {
        gameCtx.Value.MaxObjectsDistance = trig.Event.Packet.Range;
    }

    static void OnExtendedCommand(
        On<PacketReceived<OnExtendedCommandPacket_0xBF>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<UOFileManager> fileManager,
        ResMut<GameContext> gameCtx,
        Res<DelayedAction> delayedActions,
        ResMut<SpellbookStore> spellbooks,
        ResMut<PopupMenuState> popupMenu,
        InGameQueries queries)
    {
        var packet = trig.Event.Packet;

        if (packet.SpellbookContent.HasValue)
        {
            var sc = packet.SpellbookContent.Value;
            ulong bits = sc.SpellBitfields[0] | ((ulong)sc.SpellBitfields[1] << 32);
            spellbooks.Value.BySerial[sc.Serial] = new SpellbookData { School = SpellSchools.Resolve(sc.Graphic), Bitfields = bits };
            spellbooks.Value.Revision++;
        }

        if (packet.PopupMenu.HasValue)
        {
            var pm = packet.PopupMenu.Value;
            popupMenu.Value.SetPending(pm.Serial, pm.Items);
        }

        switch (packet.Command)
        {
            case 0x08:
                if (packet.MapIndex.HasValue)
                {
                    var mapIdx = packet.MapIndex.Value;
                    fileManager.Value.Maps.LoadMap(mapIdx);
                    if (gameCtx.Value.Map != mapIdx)
                        gameCtx.Value.Map = mapIdx;
                }
                break;

            case 0x1D:
                if (packet.HouseRevisionSerial.HasValue && packet.HouseRevision.HasValue)
                {
                    var serial = packet.HouseRevisionSerial.Value;
                    var revision = packet.HouseRevision.Value;
                    var house = entitiesMap.Value.GetOrCreate(commands, serial);

                    if (queries.qHouseRevision.TryGet(house.Id, out var houseRevRow))
                    {
                        (_, var houseRev) = houseRevRow;
                        if (houseRev.Ref.Value == revision)
                            break;
                    }

                    house.Insert(new HouseRevision { Value = revision });
                    delayedActions.Value.Add(() => network.Value.Send_CustomHouseDataRequest(serial), 1000);
                }
                break;
        }
    }

    static void OnUpdateItem(
        On<PacketReceived<OnUpdateItemPacket_0x1A>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<MultiCache> multiCache,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries)
    {
        var packet = trig.Event.Packet;

        var finalGraphic = (ushort)(packet.Graphic + packet.GraphicIncrement);
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = finalGraphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags });

        if (packet.Amount > 0)
            ent.Insert(new Amount() { Value = packet.Amount });

        if (packet.Type == 2 && !queries.qMultis.Contains(ent.Id))
        {
            ent.Insert<IsMulti>();

            var multiInfo = multiCache.Value.GetMulti(finalGraphic);
            foreach (ref readonly var block in CollectionsMarshal.AsSpan(multiInfo.Blocks))
            {
                if (!block.IsVisible)
                    continue;

                var child = commands.Spawn()
                    .Insert(new Graphic() { Value = block.ID })
                    .Insert(new Hue())
                    .Insert(new WorldPosition()
                    {
                        X = (ushort)(packet.X + block.X),
                        Y = (ushort)(packet.Y + block.Y),
                        Z = (sbyte)(packet.Z + block.Z)
                    })
                    .Insert<NormalMulti>();

                ent.AddChild(child);
            }
        }

        if (Game.SerialHelper.IsMobile(packet.Serial))
        {
            mobileQueuedSteps.Send(new MobileQueuedStep
            {
                Serial = packet.Serial,
                X = packet.X,
                Y = packet.Y,
                Z = packet.Z,
                Direction = packet.Direction
            });
        }
        else
        {
            ent.Insert(new WorldPosition() { X = packet.X, Y = packet.Y, Z = packet.Z })
                .Insert(new Facing() { Value = packet.Direction });
        }
    }

    static void OnUpdateItemSA(
        On<PacketReceived<OnUpdateItemSAPacket_0xF3>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<MultiCache> multiCache,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries)
    {
        var packet = trig.Event.Packet;

        var finalGraphic = (ushort)(packet.Graphic + packet.GraphicIncrement);
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = finalGraphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new Amount() { Value = packet.Amount })
            .Insert(new ServerFlags() { Value = packet.Flags });

        if (Game.SerialHelper.IsMobile(packet.Serial))
        {
            mobileQueuedSteps.Send(new MobileQueuedStep
            {
                Serial = packet.Serial,
                X = packet.X,
                Y = packet.Y,
                Z = packet.Z,
                Direction = packet.Direction
            });
        }
        else
        {
            ent.Insert(new WorldPosition() { X = packet.X, Y = packet.Y, Z = packet.Z })
                .Insert(new Facing() { Value = packet.Direction });
        }

        if (packet.UpdateType == 2 && !queries.qMultis.Contains(ent.Id))
        {
            ent.Insert<IsMulti>();

            var multiInfo = multiCache.Value.GetMulti(finalGraphic);
            foreach (ref readonly var block in CollectionsMarshal.AsSpan(multiInfo.Blocks))
            {
                if (!block.IsVisible)
                    continue;

                var child = commands.Spawn()
                    .Insert(new Graphic() { Value = block.ID })
                    .Insert(new Hue())
                    .Insert(new WorldPosition()
                    {
                        X = (ushort)(packet.X + block.X),
                        Y = (ushort)(packet.Y + block.Y),
                        Z = (sbyte)(packet.Z + block.Z)
                    })
                    .Insert<NormalMulti>();

                ent.AddChild(child);
            }
        }
    }

    static void OnCustomHouse(
        On<PacketReceived<OnCustomHousePacket_0xD8>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<MultiCache> multiCache,
        InGameQueries queries)
    {
        var packet = trig.Event.Packet;

        if (packet.Planes == null || packet.Planes.Count == 0)
            return;

        var parent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);

        if (!queries.qPosAndGraphic.TryGet(parent.Id, out var posAndGraphicRow))
            return;

        (var pos, var graphic) = posAndGraphicRow;
        (var startX, var startY, var startZ) = pos.Ref;

        parent.Insert(new HouseRevision { Value = packet.Revision });

        var multiRect = multiCache.Value.GetMulti(graphic.Ref.Value).Bounds;

        foreach (var plane in packet.Planes)
        {
            if (plane.Data == null || plane.Data.Length == 0)
                continue;

            var reader = new StackDataReader(plane.Data);

            switch (plane.PlaneMode)
            {
                case 0:
                    {
                        var entries = plane.Data.Length / 5;
                        for (var i = 0; i < entries; ++i)
                        {
                            var id = reader.ReadUInt16BE();
                            var offsetX = reader.ReadInt8();
                            var offsetY = reader.ReadInt8();
                            var offsetZ = reader.ReadInt8();

                            var child = commands.Spawn()
                                .Insert(new Graphic() { Value = id })
                                .Insert(new Hue())
                                .Insert(new WorldPosition()
                                {
                                    X = (ushort)(startX + offsetX),
                                    Y = (ushort)(startY + offsetY),
                                    Z = (sbyte)(startZ + offsetZ)
                                })
                                .Insert<CustomMulti>();

                            parent.AddChild(child);
                        }

                        break;
                    }

                case 1:
                    {
                        var planeZ = plane.PlaneZ;
                        var z = planeZ > 0 ? (sbyte)(((planeZ - 1) % 4) * 20 + 7) : (sbyte)0;
                        var entries = plane.Data.Length >> 2;

                        for (var i = 0; i < entries; ++i)
                        {
                            var id = reader.ReadUInt16BE();
                            var offsetX = reader.ReadInt8();
                            var offsetY = reader.ReadInt8();

                            if (id == 0)
                                continue;

                            var child = commands.Spawn()
                                .Insert(new Graphic() { Value = id })
                                .Insert(new Hue())
                                .Insert(new WorldPosition()
                                {
                                    X = (ushort)(startX + offsetX),
                                    Y = (ushort)(startY + offsetY),
                                    Z = (sbyte)(startZ + z)
                                })
                                .Insert<CustomMulti>();

                            parent.AddChild(child);
                        }

                        break;
                    }

                case 2:
                    {
                        var planeZ = plane.PlaneZ;
                        var z = planeZ > 0 ? (sbyte)(((planeZ - 1) % 4) * 20 + 7) : (sbyte)0;

                        short offX;
                        short offY;
                        short multiHeight;

                        if (planeZ <= 0)
                        {
                            offX = (short)multiRect.X;
                            offY = (short)multiRect.Y;
                            multiHeight = (short)(multiRect.Height - multiRect.Y + 2);
                        }
                        else if (planeZ <= 4)
                        {
                            offX = (short)(multiRect.X + 1);
                            offY = (short)(multiRect.Y + 1);
                            multiHeight = (short)(multiRect.Height - multiRect.Y);
                        }
                        else
                        {
                            offX = (short)multiRect.X;
                            offY = (short)multiRect.Y;
                            multiHeight = (short)(multiRect.Height - multiRect.Y + 1);
                        }

                        if (multiHeight <= 0)
                            break;

                        var entries = plane.Data.Length >> 1;

                        for (var i = 0; i < entries; ++i)
                        {
                            var id = reader.ReadUInt16BE();
                            if (id == 0)
                                continue;

                            var relativeX = (sbyte)((i == 0 ? 0 : (i / multiHeight)) + offX);
                            var relativeY = (sbyte)(i % multiHeight + offY);

                            var child = commands.Spawn()
                                .Insert(new Graphic() { Value = id })
                                .Insert(new Hue())
                                .Insert(new WorldPosition()
                                {
                                    X = (ushort)(startX + relativeX),
                                    Y = (ushort)(startY + relativeY),
                                    Z = (sbyte)(startZ + z)
                                })
                                .Insert<CustomMulti>();

                            parent.AddChild(child);
                        }

                        break;
                    }
            }
        }
    }

    static void OnUpdateObjectD3(
        On<PacketReceived<OnUpdateObjectPacket_0xD3>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries)
    {
        var packet = trig.Event.Packet;
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = packet.Graphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags })
            .Insert(new Notoriety() { Value = packet.Notoriety });

        EquipmentSlots slots;
        bool existed = queries.qEquipmentSlots.TryGet(ent.Id, out var slotsRow);
        EquipmentSlots old = default;
        if (existed)
        {
            (_, var existing) = slotsRow;
            old = existing.Ref;
            slots = old;
        }
        else
        {
            slots = new EquipmentSlots();
        }

        if (packet.Equipment != null)
        {
            foreach (var entry in packet.Equipment)
            {
                var child = entitiesMap.Value.GetOrCreate(commands, entry.Serial);
                child.Insert(new Graphic() { Value = entry.Graphic })
                    .Insert(new Hue() { Value = entry.Hue });

                slots[entry.Layer] = child.Id;
            }
        }

        // Skip re-Insert when slots unchanged so the change tick only moves on
        // a real equip/unequip — see EquipmentSlots.ContentEquals.
        if (!existed || !slots.ContentEquals(old))
            ent.Insert(slots);

        mobileQueuedSteps.Send(new MobileQueuedStep
        {
            Serial = packet.Serial,
            X = packet.X,
            Y = packet.Y,
            Z = packet.Z,
            Direction = packet.Direction
        });
    }

    static void OnUpdateObject78(
        On<PacketReceived<OnUpdateObjectAltPacket_0x78>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries)
    {
        var packet = trig.Event.Packet;
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = packet.Graphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags })
            .Insert(new Notoriety() { Value = packet.Notoriety });

        EquipmentSlots slots;
        bool existed = queries.qEquipmentSlots.TryGet(ent.Id, out var slotsRow);
        EquipmentSlots old = default;
        if (existed)
        {
            (_, var existing) = slotsRow;
            old = existing.Ref;
            slots = old;
        }
        else
        {
            slots = new EquipmentSlots();
        }

        if (packet.Equipment != null)
        {
            foreach (var entry in packet.Equipment)
            {
                var child = entitiesMap.Value.GetOrCreate(commands, entry.Serial);
                child.Insert(new Graphic() { Value = entry.Graphic })
                    .Insert(new Hue() { Value = entry.Hue });

                slots[entry.Layer] = child.Id;
            }
        }

        if (!existed || !slots.ContentEquals(old))
            ent.Insert(slots);

        mobileQueuedSteps.Send(new MobileQueuedStep
        {
            Serial = packet.Serial,
            X = packet.X,
            Y = packet.Y,
            Z = packet.Z,
            Direction = packet.Direction
        });
    }

    static void OnDeleteObject(
        On<PacketReceived<OnDeleteObjectPacket_0x1D>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<GameContext> gameCtx,
        EventWriter<ContainerSlotEvent> itemRemoved,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<NetworkSerial>> serialQ)
    {
        if (gameCtx.Value.PlayerSerial == 0)
            return;

        var packet = trig.Event.Packet;
        Console.WriteLine("delete obj from packet: 0x{0:X8}", packet.Serial);

        // Drop the container UI slot if this item was sitting in a gump.
        itemRemoved.Send(ContainerSlotEvent.Remove(packet.Serial));

        if (entitiesMap.Value.TryGet(commands, packet.Serial, out var ent))
        {
            // A destroyed item drops off whatever mobile wore it (legacy:
            // Item.Destroy unlinks it from the layer). Dismount sends 0x1D for
            // the mount item but no equipment update, so without this the mount
            // layer keeps rendering it. Clear by serial — entity-id churn means
            // the slot can hold a different live entity than this delete targets.
            ContainersPlugin.ClearEquipReference(commands, packet.Serial, equipQ, serialQ);
            ent.Despawn();
            // Despawn is deferred (applies at EndDeferred), but the serial map
            // entry is otherwise cleared only by the OnRemove<NetworkSerial>
            // observer — which also fires at EndDeferred. A follow-up packet in
            // the SAME read (e.g. 0x2E equipping the ethereal mount the server
            // just removed from the pack) would then GetOrCreate this doomed
            // entity and pin it into EquipmentSlots[Mount], leaving a dangling
            // slot once the despawn lands. Removing the map entry now forces
            // that GetOrCreate to spawn a fresh, live entity instead.
            entitiesMap.Value.Remove(packet.Serial);
        }
    }

    static void OnUpdatePlayer(
        On<PacketReceived<OnUpdatePlayerPacket_0x20>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<MobileQueuedStep> mobileQueuedSteps)
    {
        var packet = trig.Event.Packet;
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = (ushort)(packet.Graphic + packet.GraphicIncrement) })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags });

        mobileQueuedSteps.Send(new MobileQueuedStep
        {
            Serial = packet.Serial,
            X = packet.X,
            Y = packet.Y,
            Z = packet.Z,
            Direction = packet.Direction
        });
    }

    static void OnCharacterStatus(
        On<PacketReceived<OnCharacterStatusPacket_0x11>> trig,
        Commands commands,
        ResMut<GameContext> gameCtx,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<Profile> profile,
        EventWriter<SystemMessageEvent> systemMsgQueue,
        Query<Data<PlayerData>> playerDataQ)
    {
        var packet = trig.Event.Packet;
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Hits() { Value = packet.Hits, MaxValue = packet.HitsMax });

        // Name carried by the status packet — used by the healthbar's name label.
        if (!string.IsNullOrEmpty(packet.Name))
            ent.Insert(new EntityName() { Value = packet.Name });

        // Cache the player's name for the status bar (the only place it's parsed).
        if (packet.Serial == gameCtx.Value.PlayerSerial)
            gameCtx.Value.PlayerName = packet.Name;

        if (packet.Stamina.HasValue && packet.StaminaMax.HasValue)
        {
            ent.Insert(new Stamina()
            {
                Value = packet.Stamina.Value,
                MaxValue = packet.StaminaMax.Value
            });
        }

        if (packet.Mana.HasValue && packet.ManaMax.HasValue)
        {
            ent.Insert(new Mana()
            {
                Value = packet.Mana.Value,
                MaxValue = packet.ManaMax.Value
            });
        }

        // Populate PlayerData so paperdoll stat panel reads Str/Dex/Int/Gold/
        // Weight off the player entity instead of re-decoding the packet.
        if (packet.Strength.HasValue)
        {
            // Announce the player's str/dex/int deltas as system lines (legacy
            // PacketHandlers 0x11). The previous PlayerData is still readable
            // here — the Insert below lands at the next sync point.
            if (profile.Value.ShowStatsChangedMessage
                && packet.Serial == gameCtx.Value.PlayerSerial
                && entitiesMap.Value.TryGet(packet.Serial, out var playerId)
                && playerDataQ.TryGet(playerId, out var oldRow))
            {
                var (_, old) = oldRow;
                if (old.Ref.Str != 0)
                {
                    SendStatDelta(systemMsgQueue, ResGeneral.Strength,
                        packet.Strength.Value - old.Ref.Str, packet.Strength.Value);
                    SendStatDelta(systemMsgQueue, ResGeneral.Dexterity,
                        packet.Dexterity.GetValueOrDefault() - old.Ref.Dex, packet.Dexterity.GetValueOrDefault());
                    SendStatDelta(systemMsgQueue, ResGeneral.Intelligence,
                        packet.Intelligence.GetValueOrDefault() - old.Ref.Int, packet.Intelligence.GetValueOrDefault());
                }
            }

            var data = new PlayerData
            {
                Str = packet.Strength.GetValueOrDefault(),
                Dex = packet.Dexterity.GetValueOrDefault(),
                Int = packet.Intelligence.GetValueOrDefault(),
                Weight = packet.Weight.GetValueOrDefault(),
                WeightMax = packet.WeightMax.GetValueOrDefault(),
                Gold = packet.Gold.GetValueOrDefault(),
                StatsCap = packet.StatsCap.GetValueOrDefault(),
                Followers = packet.Followers.GetValueOrDefault(),
                FollowersMax = packet.MaxFollowers.GetValueOrDefault(),
                Luck = packet.Luck.GetValueOrDefault(),
                DamageMin = packet.DamageMin.GetValueOrDefault(),
                DamageMax = packet.DamageMax.GetValueOrDefault(),
                PhysicalRes = packet.PhysicalResistance.GetValueOrDefault(),
                FireRes = packet.FireResistance.GetValueOrDefault(),
                ColdRes = packet.ColdResistance.GetValueOrDefault(),
                PoisonRes = packet.PoisonResistance.GetValueOrDefault(),
                EnergyRes = packet.EnergyResistance.GetValueOrDefault(),
                ThithingPoints = packet.TithingPoints.GetValueOrDefault(),
                IsFemale = packet.IsFemale.GetValueOrDefault(),
                Race = packet.Race.HasValue ? (RaceType)packet.Race.Value : RaceType.HUMAN,
                MaxPhysicalRes = packet.MaxPhysicalResistance.GetValueOrDefault(),
                MaxFireRes = packet.MaxFireResistance.GetValueOrDefault(),
                MaxColdRes = packet.MaxColdResistance.GetValueOrDefault(),
                MaxPoisonRes = packet.MaxPoisonResistance.GetValueOrDefault(),
                MaxEnergyRes = packet.MaxEnergyResistance.GetValueOrDefault(),
                DefenseChanceInc = packet.DefenseChanceIncrease.GetValueOrDefault(),
                MaxDefenseChanceInc = packet.MaxDefenseChanceIncrease.GetValueOrDefault(),
                HitChanceInc = packet.HitChanceIncrease.GetValueOrDefault(),
                SwingSpeedInc = packet.SwingSpeedIncrease.GetValueOrDefault(),
                DamageInc = packet.DamageIncrease.GetValueOrDefault(),
                LowerReagentCost = packet.LowerReagentCost.GetValueOrDefault(),
                SpellDamageInc = packet.SpellDamageIncrease.GetValueOrDefault(),
                FasterCastRecovery = packet.FasterCastRecovery.GetValueOrDefault(),
                FasterCasting = packet.FasterCasting.GetValueOrDefault(),
                LowerManaCost = packet.LowerManaCost.GetValueOrDefault(),
            };
            ent.Insert(data);
        }
    }

    // Hue 0x0170 / font 3 mirror legacy GameActions.Print for stat deltas.
    static void SendStatDelta(EventWriter<SystemMessageEvent> queue, string stat, int delta, int value)
    {
        if (delta == 0) return;
        queue.Send(new SystemMessageEvent
        {
            Text = string.Format(ResGeneral.Your0HasChangedBy1ItIsNow2, stat, delta, value),
            Hue = 0x0170,
            Font = 3,
        });
    }

    static void OnMobileAttributes(
        On<PacketReceived<OnMobileAttributesPacket_0x2D>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap)
    {
        var packet = trig.Event.Packet;
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Hits() { Value = packet.Hits, MaxValue = packet.HitsMax })
            .Insert(new Mana() { Value = packet.Mana, MaxValue = packet.ManaMax })
            .Insert(new Stamina() { Value = packet.Stamina, MaxValue = packet.StaminaMax });
    }

    static void OnEquipItem(
        On<PacketReceived<OnEquipItemPacket_0x2E>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        InGameQueries queries,
        EventWriter<ContainerSlotEvent> itemRemoved)
    {
        var packet = trig.Event.Packet;
        var parentEnt = entitiesMap.Value.GetOrCreate(commands, packet.ContainerSerial);
        var childEnt = entitiesMap.Value.GetOrCreate(commands, packet.Serial);

        // Parity with legacy EquipItem -> world.RemoveItemFromContainer: an item
        // moving to an equipment layer leaves whatever container it was in. Drop
        // the contained-item state + detach the ECS parent link, and tell the
        // container gump to despawn its slot — otherwise it lingers as a phantom
        // in the pack (e.g. equipping an ethereal mount from the backpack).
        if (queries.qContainedItems.Contains(childEnt.Id))
        {
            childEnt.Remove<ContainedInto>()
                .Remove<ContainerSlotPosition>();
            commands.RemoveChild(childEnt.Id);
            itemRemoved.Send(ContainerSlotEvent.Remove(packet.Serial));
        }

        childEnt.Insert(new Graphic() { Value = (ushort)(packet.Graphic + packet.GraphicIncrement) })
            .Insert(new Hue() { Value = packet.Hue });

        EquipmentSlots slots;
        bool existed = queries.qEquipmentSlots.TryGet(parentEnt.Id, out var slotsRow);
        EquipmentSlots old = default;
        if (existed)
        {
            (_, var existing) = slotsRow;
            old = existing.Ref;
            slots = old;
        }
        else
        {
            slots = new EquipmentSlots();
        }

        slots[packet.Layer] = childEnt.Id;
        if (!existed || !slots.ContentEquals(old))
            parentEnt.Insert(slots);
    }

    static void OnUpdateMana(
        On<PacketReceived<OnUpdateManaPacket_0xA2>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap)
    {
        var packet = trig.Event.Packet;
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Mana() { Value = packet.Mana, MaxValue = packet.ManaMax });
    }

    static void OnUpdateStamina(
        On<PacketReceived<OnUpdateStaminaPacket_0xA3>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap)
    {
        var packet = trig.Event.Packet;
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Stamina() { Value = packet.Stamina, MaxValue = packet.StaminaMax });
    }

    static void OnUpdateHits(
        On<PacketReceived<OnUpdateHitsPacket_0xA1>> trig,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap)
    {
        var packet = trig.Event.Packet;
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Hits() { Value = packet.Hits, MaxValue = packet.HitsMax });
    }

    static BodyConvFlags ComputeBodyConvFlags(LockedFeatureFlags flags)
    {
        BodyConvFlags bcFlags = 0;

        if (flags.HasFlag(LockedFeatureFlags.UOR))
            bcFlags |= BodyConvFlags.Anim1 | BodyConvFlags.Anim2;
        if (flags.HasFlag(LockedFeatureFlags.LBR))
            bcFlags |= BodyConvFlags.Anim1;
        if (flags.HasFlag(LockedFeatureFlags.AOS))
            bcFlags |= BodyConvFlags.Anim2;
        if (flags.HasFlag(LockedFeatureFlags.SE))
            bcFlags |= BodyConvFlags.Anim3;
        if (flags.HasFlag(LockedFeatureFlags.ML))
            bcFlags |= BodyConvFlags.Anim4;

        return bcFlags;
    }

    static void OnLockFeaturesPre(
        On<PacketReceived<OnLockFeaturesPacket_0xB9_Pre60142>> trig,
        Res<UOFileManager> fileManager,
        ResMut<GameContext> gameCtx)
    {
        gameCtx.Value.LockedFeatures = trig.Event.Packet.Flags;
        var conv = ComputeBodyConvFlags(trig.Event.Packet.Flags);
        fileManager.Value.Animations.ProcessBodyConvDef(conv);
    }

    static void OnLockFeaturesPost(
        On<PacketReceived<OnLockFeaturesPacket_0xB9_Post60142>> trig,
        Res<UOFileManager> fileManager,
        ResMut<GameContext> gameCtx)
    {
        gameCtx.Value.LockedFeatures = trig.Event.Packet.Flags;
        var conv = ComputeBodyConvFlags(trig.Event.Packet.Flags);
        fileManager.Value.Animations.ProcessBodyConvDef(conv);
    }

    static void OnSeasonChange(
        On<PacketReceived<OnSeasonChangePacket_0xBC>> trig,
        ResMut<GameContext> gameCtx)
    {
        // Mirror main's Season handler: stash season + music index on
        // GameContext for terrain art selection. Tile recolor / music swap
        // consume these once those subsystems are wired.
        var packet = trig.Event.Packet;
        gameCtx.Value.Season = packet.Season;
        gameCtx.Value.SeasonMusicIndex = packet.Music;
    }

    static void OnPlayerLightLevel(
        On<PacketReceived<OnPlayerLightLevelPacket_0x4E>> trig,
        ResMut<GameContext> gameCtx)
    {
        // Per-player light radius. Same storage pattern as 0x4F.
        var packet = trig.Event.Packet;
        if (packet.Serial == gameCtx.Value.PlayerSerial)
            gameCtx.Value.PersonalLightLevel = packet.Level;
    }

    static void OnServerLightLevel(
        On<PacketReceived<OnServerLightLevelPacket_0x4F>> trig,
        ResMut<GameContext> gameCtx)
    {
        gameCtx.Value.ServerLightLevel = trig.Event.Packet.Level;
    }

    // Server echoes the player's warmode state via 0x72 (response to our
    // Send_ChangeWarMode + spontaneous server-side flips). Mirror main's
    // PacketHandlers.Warmode by updating the player's ServerFlags. Reinsert
    // via Commands so OnInsert<ServerFlags> fires — paperdoll button refresh
    // hooks that observer to swap peace/war button graphics.
    static void OnWarmode(
        On<PacketReceived<OnWarmodePacket_0x72>> trig,
        Commands commands,
        Query<Data<ServerFlags>, With<Player>> playerQ)
    {
        var enabled = trig.Event.Packet.WarmodeEnabled;
        foreach (var (ent, sf) in playerQ)
        {
            var newFlags = enabled
                ? sf.Ref.Value | Flags.WarMode
                : sf.Ref.Value & ~Flags.WarMode;
            if (newFlags == sf.Ref.Value) continue;
            commands.Entity(ent.Ref).Insert(new ServerFlags { Value = newFlags });
        }
    }

    // 0x3A UpdateSkills. Store each value entry into PlayerSkills indexed by
    // skill id (the gump reads names from the SkillsLoader, so the 0xFE name
    // definition variant carries nothing we keep). Mirrors main's
    // PacketHandlers.UpdateSkills writing into World.Player.Skills.
    static void OnUpdateSkills(
        On<PacketReceived<OnUpdateSkillsPacket_0x3A>> trig,
        ResMut<PlayerSkills> skills,
        Res<UOFileManager> fileManager,
        Res<Profile> profile,
        EventWriter<SystemMessageEvent> systemMsgQueue)
    {
        var packet = trig.Event.Packet;
        if (packet.Values == null) return;

        foreach (var v in packet.Values)
        {
            if (v.Id < 0 || v.Id >= skills.Value.Values.Length) continue;
            ref var slot = ref skills.Value.Values[v.Id];

            // Single-skill updates announce the delta as a system line (legacy
            // PacketHandlers.UpdateSkills): only past the profile threshold
            // (tenths) and only for a skill we've already seen — the initial
            // bulk load is never a single update.
            if (packet.IsSingleUpdate && slot.Received
                && profile.Value.ShowSkillsChangedMessage
                && v.Id < fileManager.Value.Skills.SkillsCount)
            {
                float change = (v.RealValue - slot.Real) / 10f;
                if (change != 0f
                    && Math.Abs(change * 10f) >= profile.Value.ShowSkillsChangedDeltaValue)
                {
                    systemMsgQueue.Send(new SystemMessageEvent
                    {
                        Text = string.Format(
                            ResGeneral.YourSkillIn0Has1By2ItIsNow3,
                            fileManager.Value.Skills.Skills[v.Id].Name,
                            change < 0 ? ResGeneral.Decreased : ResGeneral.Increased,
                            Math.Abs(change),
                            v.RealValue / 10f),
                        Hue = 0x58,
                        Font = 3,
                    });
                }
            }

            slot.Real = (ushort)v.RealValue;
            slot.Base = (ushort)v.BaseValue;
            slot.Lock = v.Status;
            if (v.Cap.HasValue) slot.Cap = (ushort)v.Cap.Value;
            slot.Received = true;
            skills.Value.HasData = true;
        }
    }

    // 0xD6 MegaCliloc: the server's Object Property List. First translated
    // cliloc line is the object name; the rest are property lines joined with
    // newlines. Stored by serial+revision for the tooltip renderer. Mirrors
    // main's PacketHandlers.MegaCliloc.
    static void OnMegaCliloc(
        On<PacketReceived<OnMegaClilocPacket_0xD6>> trig,
        Res<UOFileManager> fileManager,
        ResMut<GameContext> gameCtx,
        ResMut<ObjectPropertyLists> opl)
    {
        var packet = trig.Event.Packet;
        if (packet.Unknown > 1)
            return;

        string name = string.Empty;
        var sb = new StringBuilder();
        bool first = true;

        if (packet.Entries != null)
        {
            foreach (var entry in packet.Entries)
            {
                string str = fileManager.Value.Clilocs.Translate(entry.Cliloc, entry.Argument, true);
                if (str == null)
                    continue;

                str = ApplyClilocColor(entry.Cliloc, str, gameCtx.Value.ClientVersion);

                if (first)
                {
                    name = str;
                    first = false;
                }
                else
                {
                    if (sb.Length != 0)
                        sb.Append('\n');
                    sb.Append(str);
                }
            }
        }

        opl.Value.Add(packet.Serial, packet.Revision, name, sb.ToString());
    }

    // A few clilocs carry a hardcoded colour in the legacy client (the per-pixel
    // strength-requirement red is skipped — it needs the player's stats).
    static string ApplyClilocColor(int cliloc, string str, ClientVersion version)
    {
        switch (cliloc)
        {
            case 1080418:
                return version >= ClientVersion.CV_60143 ? "<basefont color=#40a4fe>" + str + "</basefont>" : str;
            case 1062613:
                return "<basefont color=#FFCC33>" + str + "</basefont>";
            case 1159561:
                return "<basefont color=#b66dff>" + str + "</basefont>";
        }
        return str;
    }

    // 0xDC OPLInfo: the server announces an entity's current OPL revision. If it
    // differs from what we cached (or we have nothing), queue a 0xD6 refresh.
    static void OnOplInfo(
        On<PacketReceived<OnOplInfoPacket_0xDC>> trig,
        ResMut<ObjectPropertyLists> opl)
    {
        var packet = trig.Event.Packet;
        if (!opl.Value.IsRevisionEquals(packet.Serial, packet.Revision))
            opl.Value.ForceRequest(packet.Serial);
    }
}
