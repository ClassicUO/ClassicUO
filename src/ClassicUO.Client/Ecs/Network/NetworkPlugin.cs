using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Network;
using ClassicUO.Utility;
using TinyEcs;
using ClassicUO.Network.Encryption;
using ClassicUO.Assets;
using System.Runtime.InteropServices;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using TinyEcs.Bevy;
using ClassicUO.Game.Managers;
using Microsoft.Xna.Framework;

namespace ClassicUO.Ecs;

delegate void OnPacket(ReadOnlySpan<byte> buffer);

struct OnLoginRequest
{
    public string Username;
    public string Password;
    public string Address;
    public ushort Port;
}

internal sealed class PacketsMap : Dictionary<byte, OnPacket>;

internal sealed class PacketsMap2 : Dictionary<byte, Func<IPacket>>;




readonly struct NetworkPlugin : IPlugin
{
    static readonly HashSet<byte> HandledPacketIds = new()
    {
        0x1B, // OnEnterWorld
        0x55, // OnLoginComplete
        0xBD, // OnClientVersion
        0xAE, // OnUnicodeSpeech
        0xC8, // OnViewRange
        0x1C, // OnAsciiSpeech
        0xBF, // OnExtendedCommand
        0x1A, // OnUpdateItem
        0x11, // OnCharacterStatus
        0xD3, // OnUpdateObject
        0x78, // OnUpdateObjectAlt
        0x1D, // OnDeleteObject
        0x20, // OnUpdatePlayer
        0x2D, // OnMobileAttributes
        0x2E, // OnEquipItem
        0xA1, // OnUpdateHits
        0xA2, // OnUpdateMana
        0xA3, // OnUpdateStamina
        0xD8, // OnCustomHouse
        0xF3  // OnUpdateItemSA
    };

    public void Build(App app)
    {
        var setupSocketFn = SetupSocket;
        var handleLoginRequestsFn = HandleLoginRequests;
        var packetReaderFn = PacketReader;

        app.AddResource(new CircularBuffer());
        app.AddResource(new PacketsMap());
        app.AddResource(new PacketsMap2());

        app
            .AddSystem(Stage.Startup, setupSocketFn)

            .AddSystem(Stage.Startup, (Res<PacketsMap2> packetsMap) =>
            {
                void create<T>() where T : IPacket, new()
                {
                    var packet = new T();
                    var fn = () => (IPacket)new T();
                    packetsMap.Value.Add(packet.Id, fn);
                }

                create<OnEnterWorldPacket_0x1B>();
                create<OnLoginCompletePacket_0x55>();
                create<OnExtendedCommandPacket_0xBF>();
                create<OnClientVersionPacket_0xBD>();
                create<OnUnicodeSpeechPacket_0xAE>();
                create<OnUpdateObjectPacket_0xD3>();
                create<OnUpdateObjectAltPacket_0x78>();
                create<OnViewRangePacket_0xC8>();
                create<OnAsciiSpeechPacket_0x1C>();
                create<OnUpdateItemPacket_0x1A>();
                create<OnDamagePacket_0x0B>();
                create<OnCharacterStatusPacket_0x11>();
                create<OnDenyWalkPacket_0x21>();
                create<OnConfirmWalkPacket_0x22>();
                create<OnOpenContainerPacket_0x24>();
                create<OnUpdateContainerPacket_0x25>();
                create<OnDenyMoveItemPacket_0x27>();
                create<OnEndDraggingItemPacket_0x28>();
                create<OnDropItemOkPacket_0x29>();
                create<OnHealthBarStatusPacket_0x16>();
                create<OnHealthBarStatusDetailsPacket_0x17>();
                create<OnDeleteObjectPacket_0x1D>();
                create<OnUpdatePlayerPacket_0x20>();
                create<OnDragAnimationPacket_0x23>();
                create<OnShowDeathScreenPacket_0x2C>();
                create<OnMobileAttributesPacket_0x2D>();
                create<OnEquipItemPacket_0x2E>();
                create<OnSwingPacket_0x2F>();
                create<OnUpdateSkillsPacket_0x3A>();
                create<OnPathfindingPacket_0x38>();
                create<OnUpdateContainerItemsPacket_0x3C>();
                create<OnPlayerLightLevelPacket_0x4E>();
                create<OnServerLightLevelPacket_0x4F>();
                create<OnSoundEffectPacket_0x54>();
                create<OnPlayMusicPacket_0x6D>();
                create<OnMapDataPacket_0x56>();
                create<OnWeatherPacket_0x65>();
                create<OnBookPagesPacket_0x66>();
                create<OnCharacterAnimationPacket_0x6E>();
                create<OnGraphicEffectPacket_0x70>();
                create<OnGraphicEffectC0Packet_0xC0>();
                create<OnGraphicEffectC7Packet_0xC7>();
                create<OnBulletinBoardPacket_0x71>();
                create<OnWarmodePacket_0x72>();
                create<OnPingPacket_0x73>();
                create<OnBuyListPacket_0x74>();
                create<OnUpdateCharacterPacket_0x77>();
                create<OnUpdateCharacterAltPacket_0xD2>();
                create<OnOpenMenuPacket_0x7C>();
                create<OnOpenPaperdollPacket_0x88>();
                create<OnCorpseEquipmentPacket_0x89>();
                create<OnShowMapPacket_0x90>();
                create<OnShowMapFacetPacket_0xF5>();
                create<OnOpenBookPacket_0x93>();
                create<OnOpenBookAltPacket_0xD4>();
                create<OnColorPickerPacket_0x95>();
                create<OnMovePlayerPacket_0x97>();
                create<OnUpdateNamePacket_0x98>();
                create<OnPlaceMultiPacket_0x99>();
                create<OnAsciiPromptPacket_0x9A>();
                create<OnSellListPacket_0x9E>();
                create<OnUpdateHitsPacket_0xA1>();
                create<OnUpdateManaPacket_0xA2>();
                create<OnUpdateStaminaPacket_0xA3>();
                create<OnOpenUrlPacket_0xA5>();
                create<OnWindowTipPacket_0xA6>();
                create<OnAttackEntityPacket_0xAA>();
                create<OnTextEntryDialogPacket_0xAB>();
                create<OnShowDeathActionPacket_0xAF>();
                create<OnOpenGumpPacket_0xB0>();
                create<OnChatMessagePacket_0xB2>();
                create<OnOpenCharacterProfilePacket_0xB8>();
                create<OnLockFeaturesPacket_0xB9>();
                create<OnQuestPointerPacket_0xBA>();
                create<OnSeasonChangePacket_0xBC>();
                create<OnClilocMessagePacket_0xC1>();
                create<OnClilocMessageAffixPacket_0xCC>();
                create<OnUnicodePromptPacket_0xC2>();
                create<OnLogoutRequestPacket_0xD1>();
                create<OnMegaClilocPacket_0xD6>();
                create<OnCustomHousePacket_0xD8>();
                create<OnOplInfoPacket_0xDC>();
                create<OnOpenCompressedGumpPacket_0xDD>();
                create<OnUpdateMobileStatusPacket_0xDE>();
                create<OnBuffDebuffPacket_0xDF>();
                create<OnNewCharacterAnimationPacket_0xE2>();
                create<OnAddWaypointPacket_0xE5>();
                create<OnRemoveWaypointPacket_0xE6>();
                create<OnKrriosClientPacket_0xF0>();
                create<OnUpdateItemSAPacket_0xF3>();
                create<OnBoatMovingPacket_0xF6>();
                create<OnPacketListPacket_0xF7>();
            })

            .AddSystem((
                EventReader<IPacket> reader,
                Commands commands,
                Res<NetworkEntitiesMap> entitiesMap,
                Res<NetClient> network,
                Res<Settings> settings,
                Res<UOFileManager> fileManager,
                Res<Profile> profile,
                ResMut<GameContext> gameCtx,
                Res<MultiCache> multiCache,
                Res<DelayedAction> delayedActions,
                ResMut<NextState<GameState>> state,
                EventWriter<MobileQueuedStep> mobileQueuedSteps,
                EventWriter<TextOverheadEvent> textOverHeadQueue,
                InGameQueries queries
            ) =>
            {
                foreach (var packet in reader.Read())
                {
                    if (HandlePacket(
                        packet,
                        commands,
                        entitiesMap,
                        network,
                        settings,
                        fileManager,
                        profile,
                        gameCtx,
                        multiCache,
                        delayedActions,
                        state,
                        mobileQueuedSteps,
                        textOverHeadQueue,
                        queries))
                    {
                        continue;
                    }

                    if (HandledPacketIds.Contains(packet.Id))
                    {
                        Console.WriteLine("Unhandled packet event: ID 0x{0:X2}", packet.Id);
                    }
                }
            })
            .InStage(Stage.Update)
            .RunIf((EventReader<IPacket> reader) => reader.HasEvents)
            .Build()

            .AddPlugin<LoginPacketsPlugin>()
            .AddPlugin<InGamePacketsPlugin>()

            .AddSystem((Res<NetClient> network, Res<CircularBuffer> buffer, ResMut<GameContext> gameCtx) =>
            {
                gameCtx.Value.Map = -1;
                gameCtx.Value.PlayerSerial = 0;
                network.Value.Disconnect();
                buffer.Value.Clear();
            })
            .OnExit(GameState.GameScreen)
            .Build()

            .AddSystem((Res<NetClient> network) => network.Value.Send_Ping(0xFF))
            .InStage(Stage.Update)
            .RunIf((Res<GameContext> gameCtx, Res<NetClient> network) => network.Value!.IsConnected && gameCtx.Value.PlayerSerial != 0)
            .RunIf((Res<Time> time, Local<float> updateTime) =>
            {
                if (updateTime.Value >= time.Value.Total)
                    return false;

                updateTime.Value = time.Value.Total + 1000f;
                return true;
            })
            .Build()

            .AddSystem(handleLoginRequestsFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<OnLoginRequest> loginRequests) => loginRequests.HasEvents)
            .Build()

            .AddSystem(packetReaderFn)
            .InStage(Stage.Update)
            .RunIf((Res<NetClient> network) => network.Value!.IsConnected)
            .Build();
    }

    void SetupSocket(Res<Settings> settings, Res<NetClient> socket, Res<UOFileManager> fileManager, Commands commands)
    {
        settings.Value.Encryption = (byte)socket.Value.Load(fileManager.Value.Version, (EncryptionType)settings.Value.Encryption);
    }

    void HandleLoginRequests(
        EventReader<OnLoginRequest> loginRequests,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        Res<Settings> settings
    )
    {
        foreach (var request in loginRequests.Read())
        {
            network.Value.Connect(request.Address, request.Port);
            Console.WriteLine("Socket is connected ? {0}", network.Value.IsConnected);

            if (!network.Value.IsConnected)
                continue;

            network.Value.Encryption?.Initialize(true, network.Value.LocalIP);

            if (gameCtx.Value.ClientVersion >= ClientVersion.CV_6040)
            {
                // NOTE: im forcing the use of latest client just for convenience rn
                var major = (byte)((uint)gameCtx.Value.ClientVersion >> 24);
                var minor = (byte)((uint)gameCtx.Value.ClientVersion >> 16);
                var build = (byte)((uint)gameCtx.Value.ClientVersion >> 8);
                var extra = (byte)gameCtx.Value.ClientVersion;

                network.Value.Send_Seed(network.Value.LocalIP, major, minor, build, extra);
            }
            else
            {
                network.Value.Send_Seed_Old(network.Value.LocalIP);
            }

            network.Value.Send_FirstLogin(request.Username, Crypter.Decrypt(request.Password));

            break;
        }
    }

    sealed class PacketBuffer
    {
        public PacketBuffer() => Buffer = new byte[1024 * 4];

        public byte[] Buffer;
    }

    void PacketReader(
        Query<Data<WasmMod>> queryMods,
        Res<NetClient> network,
        Res<PacketsMap> packetsMap,
        Res<PacketsMap2> packetsMap2,
        Res<CircularBuffer> buffer,
        Local<PacketBuffer> packetBuffer,
        EventWriter<IPacket> queuePackets
    )
    {
        var availableData = network.Value.CollectAvailableData();
        var span = availableData.AsSpan();
        if (!span.IsEmpty)
            buffer.Value.Enqueue(span);

        while (buffer.Value.Length > 0)
        {
            var packetId = buffer.Value[0];
            var packetLen = (int)network.Value.PacketsTable.GetPacketLength(packetId);
            var packetHeaderOffset = sizeof(byte);

            if (packetLen == -1)
            {
                if (buffer.Value.Length < 3)
                    break;

                var b0 = buffer.Value[1];
                var b1 = buffer.Value[2];

                packetLen = (b0 << 8) | b1;
                packetHeaderOffset += sizeof(ushort);
            }

            if (buffer.Value.Length < packetLen)
            {
                Console.WriteLine("needs more data for packet 0x{0:X2}", packetId);
                break;
            }

            while (packetLen > packetBuffer.Value.Buffer.Length)
                Array.Resize(ref packetBuffer.Value.Buffer, packetBuffer.Value.Buffer.Length * 2);

            _ = buffer.Value.Dequeue(packetBuffer.Value.Buffer, 0, packetLen);

            // Console.WriteLine(">> packet-in: ID 0x{0:X2} | Len: {1}", packetId, packetLen);

            var sp = packetBuffer.Value.Buffer.AsSpan(0, packetLen + packetHeaderOffset);

            foreach ((_, var mod) in queryMods)
            {
                if (mod.Ref.Mod.Plugin.FunctionExists("packet_recv"))
                {
                    var res = mod.Ref.Mod.Plugin.Call("packet_recv", sp);
                    if (res.IsEmpty)
                    {
                        sp = [];
                    }
                    else
                    {
                        res.CopyTo(sp);
                    }
                }
            }

            if (sp.IsEmpty)
                continue;


            if (packetsMap2.Value.TryGetValue(packetId, out var fn))
            {
                var payload = sp.Slice(packetHeaderOffset, packetLen - packetHeaderOffset);
                var reader = new StackDataReader(payload);
                var packet = fn();
                packet.Fill(reader);
                queuePackets.Send(packet);
            }

            // if (packetsMap.Value.TryGetValue(packetId, out var handler) && !HandledPacketIds.Contains(packetId))
            // {
            //     handler(payload);
            // }
        }

        network.Value.Flush();
    }

    sealed class InGameQueries : ISystemParam
    {
        public Query<Data<HouseRevision>> qHouseRevision { get; } = new();
        public Query<Data<EquipmentSlots>> qEquipmentSlots { get; } = new();
        public Query<Data<WorldPosition, Graphic>> qPosAndGraphic { get; } = new();
        public Query<Empty, With<IsMulti>> qMultis { get; } = new();

        public void Initialize(World world)
        {
            qHouseRevision.Initialize(world);
            qEquipmentSlots.Initialize(world);
            qPosAndGraphic.Initialize(world);
            qMultis.Initialize(world);
        }

        public void Fetch(World world)
        {
            qHouseRevision.Fetch(world);
            qEquipmentSlots.Fetch(world);
            qPosAndGraphic.Fetch(world);
            qMultis.Fetch(world);
        }

        public SystemParamAccess GetAccess()
        {
            var access = new SystemParamAccess();
            var houseRev = qHouseRevision.GetAccess();
            var equipSlots = qEquipmentSlots.GetAccess();
            var posAndGraphic = qPosAndGraphic.GetAccess();
            var multis = qMultis.GetAccess();

            foreach (var read in houseRev.ReadResources) access.ReadResources.Add(read);
            foreach (var write in houseRev.WriteResources) access.WriteResources.Add(write);
            foreach (var read in equipSlots.ReadResources) access.ReadResources.Add(read);
            foreach (var write in equipSlots.WriteResources) access.WriteResources.Add(write);
            foreach (var read in posAndGraphic.ReadResources) access.ReadResources.Add(read);
            foreach (var write in posAndGraphic.WriteResources) access.WriteResources.Add(write);
            foreach (var read in multis.ReadResources) access.ReadResources.Add(read);
            foreach (var write in multis.WriteResources) access.WriteResources.Add(write);

            return access;
        }
    }

    static bool HandlePacket(
        IPacket packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<Settings> settings,
        Res<UOFileManager> fileManager,
        Res<Profile> profile,
        ResMut<GameContext> gameCtx,
        Res<MultiCache> multiCache,
        Res<DelayedAction> delayedActions,
        ResMut<NextState<GameState>> state,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        EventWriter<TextOverheadEvent> textOverHeadQueue,
        InGameQueries queries
    )
    {
        _ = profile;

        switch (packet)
        {
            case OnEnterWorldPacket_0x1B enterWorld:
                HandleEnterWorld(enterWorld, commands, entitiesMap, network, settings, gameCtx, state);
                return true;

            case OnLoginCompletePacket_0x55 loginComplete:
                HandleLoginComplete(loginComplete, network, gameCtx, settings);
                return true;

            case OnClientVersionPacket_0xBD clientVersion:
                HandleClientVersion(clientVersion, network, settings);
                return true;

            case OnUnicodeSpeechPacket_0xAE unicodeSpeech:
                HandleUnicodeSpeech(unicodeSpeech, network, textOverHeadQueue);
                return true;

            case OnViewRangePacket_0xC8 viewRange:
                HandleViewRange(viewRange, gameCtx);
                return true;

            case OnAsciiSpeechPacket_0x1C asciiSpeech:
                HandleAsciiSpeech(asciiSpeech, network, textOverHeadQueue);
                return true;

            case OnExtendedCommandPacket_0xBF extendedCommand:
                HandleExtendedCommand(
                    extendedCommand,
                    commands,
                    entitiesMap,
                    network,
                    settings,
                    fileManager,
                    gameCtx,
                    multiCache,
                    delayedActions,
                    mobileQueuedSteps,
                    textOverHeadQueue,
                    queries
                );
                return true;

            case OnUpdateItemPacket_0x1A updateItem:
                HandleUpdateItem(updateItem, commands, entitiesMap, multiCache, gameCtx, mobileQueuedSteps, queries);
                return true;

            case OnUpdateItemSAPacket_0xF3 updateItemSA:
                HandleUpdateItemSA(updateItemSA, commands, entitiesMap, multiCache, mobileQueuedSteps, queries);
                return true;

            case OnUpdateObjectPacket_0xD3 updateObject:
                HandleUpdateObject(updateObject, commands, entitiesMap, mobileQueuedSteps, queries);
                return true;

            case OnUpdateObjectAltPacket_0x78 updateObjectAlt:
                HandleUpdateObject(updateObjectAlt, commands, entitiesMap, mobileQueuedSteps, queries);
                return true;

            case OnDeleteObjectPacket_0x1D deleteObject:
                HandleDeleteObject(deleteObject, entitiesMap, gameCtx);
                return true;

            case OnUpdatePlayerPacket_0x20 updatePlayer:
                HandleUpdatePlayer(updatePlayer, commands, entitiesMap, mobileQueuedSteps);
                return true;

            case OnCharacterStatusPacket_0x11 status:
                HandleCharacterStatus(status, commands, entitiesMap);
                return true;

            case OnMobileAttributesPacket_0x2D mobileAttributes:
                HandleMobileAttributes(mobileAttributes, commands, entitiesMap);
                return true;

            case OnEquipItemPacket_0x2E equipItem:
                HandleEquipItem(equipItem, commands, entitiesMap, queries);
                return true;

            case OnUpdateHitsPacket_0xA1 updateHits:
                HandleUpdateHits(updateHits, commands, entitiesMap);
                return true;

            case OnUpdateManaPacket_0xA2 updateMana:
                HandleUpdateMana(updateMana, commands, entitiesMap);
                return true;

            case OnUpdateStaminaPacket_0xA3 updateStamina:
                HandleUpdateStamina(updateStamina, commands, entitiesMap);
                return true;

            case OnCustomHousePacket_0xD8 customHouse:
                HandleCustomHouse(customHouse, commands, entitiesMap, multiCache, queries);
                return true;

            default:
                return false;
        }
    }

    static void HandleEnterWorld(
        OnEnterWorldPacket_0x1B packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<Settings> settings,
        ResMut<GameContext> gameCtx,
        ResMut<NextState<GameState>> state
    )
    {
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

    static void HandleLoginComplete(
        OnLoginCompletePacket_0x55 packet,
        Res<NetClient> network,
        ResMut<GameContext> gameCtx,
        Res<Settings> settings
    )
    {
        if (gameCtx.Value.PlayerSerial == 0)
            return;

        network.Value.Send_StatusRequest(gameCtx.Value.PlayerSerial);
        network.Value.Send_OpenChat("");
        network.Value.Send_SkillsRequest(gameCtx.Value.PlayerSerial);

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_306E)
            network.Value.Send_ClientType(gameCtx.Value.Protocol);

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_305D)
            network.Value.Send_ClientViewRange(24);
    }

    static void HandleClientVersion(
        OnClientVersionPacket_0xBD packet,
        Res<NetClient> network,
        Res<Settings> settings
    ) => network.Value.Send_ClientVersion(settings.Value.ClientVersion);

    static void HandleUnicodeSpeech(
        OnUnicodeSpeechPacket_0xAE packet,
        Res<NetClient> network,
        EventWriter<TextOverheadEvent> textOverHeadQueue
    )
    {
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

        Console.WriteLine("[0xAE] {0} says: '{1}'", packet.Name, packet.Text);

        textOverHeadQueue.Send(new TextOverheadEvent
        {
            Serial = packet.Serial,
            Name = packet.Name,
            Hue = packet.Hue,
            Font = (byte)packet.Font,
            Text = packet.Text,
            MessageType = packet.MessageType
        });
    }

    static void HandleAsciiSpeech(
        OnAsciiSpeechPacket_0x1C packet,
        Res<NetClient> network,
        EventWriter<TextOverheadEvent> textOverHeadQueue
    )
    {
        if (packet.IsSystemMessage)
        {
            network.Value.Send_ACKTalk();
            return;
        }

        Console.WriteLine("[0x1C] {0} says: '{1}'", packet.Name, packet.Text);

        textOverHeadQueue.Send(new TextOverheadEvent
        {
            Serial = packet.Serial,
            Name = packet.Name,
            Hue = packet.Hue,
            Font = (byte)packet.Font,
            Text = packet.Text,
            MessageType = packet.MessageType
        });
    }

    static void HandleViewRange(
        OnViewRangePacket_0xC8 packet,
        ResMut<GameContext> gameCtx
    ) => gameCtx.Value.MaxObjectsDistance = packet.Range;

    static void HandleExtendedCommand(
        OnExtendedCommandPacket_0xBF packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<Settings> settings,
        Res<UOFileManager> fileManager,
        ResMut<GameContext> gameCtx,
        Res<MultiCache> multiCache,
        Res<DelayedAction> delayedActions,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        EventWriter<TextOverheadEvent> textOverHeadQueue,
        InGameQueries queries
    )
    {
        _ = settings;
        _ = multiCache;
        _ = mobileQueuedSteps;
        _ = textOverHeadQueue;

        switch (packet.Command)
        {
            case 0x08:
                HandleExtendedCommand_MapChange(packet, fileManager, gameCtx);
                break;

            case 0x1D:
                HandleExtendedCommand_HouseRevision(packet, commands, entitiesMap, network, delayedActions, queries);
                break;
        }
    }

    static void HandleExtendedCommand_MapChange(
        OnExtendedCommandPacket_0xBF packet,
        Res<UOFileManager> fileManager,
        ResMut<GameContext> gameCtx
    )
    {
        if (!packet.MapIndex.HasValue)
            return;

        var mapIdx = packet.MapIndex.Value;
        fileManager.Value.Maps.LoadMap(mapIdx);

        if (gameCtx.Value.Map != mapIdx)
        {
            gameCtx.Value.Map = mapIdx;
        }
    }

    static void HandleExtendedCommand_HouseRevision(
        OnExtendedCommandPacket_0xBF packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<DelayedAction> delayedActions,
        InGameQueries queries
    )
    {
        if (!packet.HouseRevisionSerial.HasValue || !packet.HouseRevision.HasValue)
            return;

        var serial = packet.HouseRevisionSerial.Value;
        var revision = packet.HouseRevision.Value;

        var house = entitiesMap.Value.GetOrCreate(commands, serial);

        if (queries.qHouseRevision.Contains(house.Id))
        {
            (_, var houseRev) = queries.qHouseRevision.Get(house.Id);
            if (houseRev.Ref.Value == revision)
                return;
        }

        house.Insert(new HouseRevision { Value = revision });
        delayedActions.Value.Add(() => network.Value.Send_CustomHouseDataRequest(serial), 1000);
    }

    static void HandleUpdateItem(
        OnUpdateItemPacket_0x1A packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<MultiCache> multiCache,
        ResMut<GameContext> gameCtx,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries
    )
    {
        _ = gameCtx;

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

    static void HandleUpdateItemSA(
        OnUpdateItemSAPacket_0xF3 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<MultiCache> multiCache,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries
    )
    {
        var finalGraphic = (ushort)(packet.Graphic + packet.GraphicIncrement);
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = finalGraphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new Amount() { Value = packet.Amount })
            .Insert(new ServerFlags() { Value = packet.Flags });

        if (ClassicUO.Game.SerialHelper.IsMobile(packet.Serial))
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

    static void HandleCustomHouse(
        OnCustomHousePacket_0xD8 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<MultiCache> multiCache,
        InGameQueries queries
    )
    {
        if (packet.Planes == null || packet.Planes.Count == 0)
            return;

        var parent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);

        if (!queries.qPosAndGraphic.Contains(parent.Id))
            return;

        (var pos, var graphic) = queries.qPosAndGraphic.Get(parent.Id);
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

    static void HandleUpdateObject(
        OnUpdateObjectPacket_0xD3 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = packet.Graphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags });

        EquipmentSlots slots;
        if (queries.qEquipmentSlots.Contains(ent.Id))
        {
            (_, var existing) = queries.qEquipmentSlots.Get(ent.Id);
            slots = existing.Ref;
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

    static void HandleUpdateObject(
        OnUpdateObjectAltPacket_0x78 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = packet.Graphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags });

        EquipmentSlots slots;
        if (queries.qEquipmentSlots.Contains(ent.Id))
        {
            (_, var existing) = queries.qEquipmentSlots.Get(ent.Id);
            slots = existing.Ref;
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

    static void HandleDeleteObject(
        OnDeleteObjectPacket_0x1D packet,
        Res<NetworkEntitiesMap> entitiesMap,
        ResMut<GameContext> gameCtx
    )
    {
        if (gameCtx.Value.PlayerSerial == 0)
            return;

        Console.WriteLine("delete obj from packet: 0x{0:X8}", packet.Serial);
        entitiesMap.Value.Remove(packet.Serial);
    }

    static void HandleUpdatePlayer(
        OnUpdatePlayerPacket_0x20 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<MobileQueuedStep> mobileQueuedSteps
    )
    {
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

    static void HandleCharacterStatus(
        OnCharacterStatusPacket_0x11 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Hits() { Value = packet.Hits, MaxValue = packet.HitsMax });

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
    }

    static void HandleMobileAttributes(
        OnMobileAttributesPacket_0x2D packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Hits() { Value = packet.Hits, MaxValue = packet.HitsMax })
            .Insert(new Mana() { Value = packet.Mana, MaxValue = packet.ManaMax })
            .Insert(new Stamina() { Value = packet.Stamina, MaxValue = packet.StaminaMax });
    }

    static void HandleEquipItem(
        OnEquipItemPacket_0x2E packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        InGameQueries queries
    )
    {
        var parentEnt = entitiesMap.Value.GetOrCreate(commands, packet.ContainerSerial);
        var childEnt = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        childEnt.Insert(new Graphic() { Value = (ushort)(packet.Graphic + packet.GraphicIncrement) })
            .Insert(new Hue() { Value = packet.Hue });

        EquipmentSlots slots;
        if (queries.qEquipmentSlots.Contains(parentEnt.Id))
        {
            (_, var existing) = queries.qEquipmentSlots.Get(parentEnt.Id);
            slots = existing.Ref;
        }
        else
        {
            slots = new EquipmentSlots();
        }

        slots[packet.Layer] = childEnt.Id;
        parentEnt.Insert(slots);
    }

    static void HandleUpdateHits(
        OnUpdateHitsPacket_0xA1 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Hits() { Value = packet.Hits, MaxValue = packet.HitsMax });
    }

    static void HandleUpdateMana(
        OnUpdateManaPacket_0xA2 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Mana() { Value = packet.Mana, MaxValue = packet.ManaMax });
    }

    static void HandleUpdateStamina(
        OnUpdateStaminaPacket_0xA3 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Stamina() { Value = packet.Stamina, MaxValue = packet.StaminaMax });
    }
}
