using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Network.Encryption;
using ClassicUO.Assets;
using ClassicUO.IO;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;


struct OnLoginRequest
{
    public string Username;
    public string Password;
    public string Address;
    public ushort Port;
}

internal struct PacketReceived<T> where T : struct, IPacket
{
    public T Packet;
}

internal sealed class PacketsMap
{
    private delegate void TypedDispatch(Commands commands, ReadOnlySpan<byte> payload);

    private readonly Dictionary<byte, Func<IPacket>> _map = new();
    private readonly Dictionary<byte, TypedDispatch> _typedMap = new();

    public void Add<T>(byte id) where T : struct, IPacket
    {
        var fn = () => (IPacket)default(T);
        _map[id] = fn;
    }

    public void Add<T>() where T : struct, IPacket
    {
        Add<T>(default(T).Id);
    }

    public void AddTyped<T>() where T : struct, IPacket
    {
        var id = default(T).Id;
        _typedMap[id] = (commands, payload) =>
        {
            var reader = new StackDataReader(payload);
            var p = new T();
            p.Fill(reader);
            commands.EmitTrigger(new PacketReceived<T> { Packet = p });
        };
    }

    // Register both boxed (Add) and typed (AddTyped) dispatch. Host gameplay
    // code reads via On<PacketReceived<T>> observers; the boxed IPacket queue
    // exists for mods and aggregate bridges.
    public void Register<T>() where T : struct, IPacket
    {
        Add<T>();
        AddTyped<T>();
    }

    public bool TryGetValue(byte id, out Func<IPacket> fn) => _map.TryGetValue(id, out fn);

    public bool TryDispatch(byte id, Commands commands, ReadOnlySpan<byte> payload)
    {
        if (!_typedMap.TryGetValue(id, out var d)) return false;
        d(commands, payload);
        return true;
    }
}

readonly struct NetworkPlugin : IPlugin
{
    public void Build(App app)
    {
        var setupSocketFn = SetupSocket;
        var handleLoginRequestsFn = HandleLoginRequests;
        var packetReaderFn = PacketReader;


        app
            .AddResource(new PacketsMap())
            .AddResource(new CircularBuffer())
            .AddSystem(Stage.Startup, setupSocketFn)

            .AddSystem(Stage.Startup, (
                Res<PacketsMap> packetsMap,
                Res<GameContext> gameCtx
            ) =>
            {
                packetsMap.Value.Register<OnDamagePacket_0x0B>();
                packetsMap.Value.Register<OnCharacterStatusPacket_0x11>();
                packetsMap.Value.Register<OnHealthBarStatusPacket_0x16>();
                packetsMap.Value.Register<OnHealthBarStatusDetailsPacket_0x17>();
                packetsMap.Value.Register<OnUpdateItemPacket_0x1A>();
                packetsMap.Value.Register<OnEnterWorldPacket_0x1B>();
                packetsMap.Value.Register<OnAsciiSpeechPacket_0x1C>();
                packetsMap.Value.Register<OnDeleteObjectPacket_0x1D>();
                packetsMap.Value.Register<OnUpdatePlayerPacket_0x20>();
                packetsMap.Value.Register<OnDenyWalkPacket_0x21>();
                packetsMap.Value.Register<OnConfirmWalkPacket_0x22>();
                packetsMap.Value.Register<OnDragAnimationPacket_0x23>();
                packetsMap.Value.Register<OnOpenContainerPacket_0x24>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_6017)
                    packetsMap.Value.Register<OnUpdateContainerPacket_0x25_Pre6017>();
                else
                    packetsMap.Value.Register<OnUpdateContainerPacket_0x25_Post6017>();
                packetsMap.Value.Register<OnDenyMoveItemPacket_0x27>();
                packetsMap.Value.Register<OnEndDraggingItemPacket_0x28>();
                packetsMap.Value.Register<OnDropItemOkPacket_0x29>();
                packetsMap.Value.Register<OnShowDeathScreenPacket_0x2C>();
                packetsMap.Value.Register<OnMobileAttributesPacket_0x2D>();
                packetsMap.Value.Register<OnEquipItemPacket_0x2E>();
                packetsMap.Value.Register<OnSwingPacket_0x2F>();
                packetsMap.Value.Register<OnPathfindingPacket_0x38>();
                packetsMap.Value.Register<OnUpdateSkillsPacket_0x3A>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_6017)
                    packetsMap.Value.Register<OnUpdateContainerItemsPacket_0x3C_Pre6017>();
                else
                    packetsMap.Value.Register<OnUpdateContainerItemsPacket_0x3C_Post6017>();
                packetsMap.Value.Register<OnPlayerLightLevelPacket_0x4E>();
                packetsMap.Value.Register<OnServerLightLevelPacket_0x4F>();
                packetsMap.Value.Register<OnSoundEffectPacket_0x54>();
                packetsMap.Value.Register<OnLoginCompletePacket_0x55>();
                packetsMap.Value.Register<OnMapDataPacket_0x56>();
                packetsMap.Value.Register<OnWeatherPacket_0x65>();
                packetsMap.Value.Register<OnBookPagesPacket_0x66>();
                packetsMap.Value.Register<OnTargetCursorPacket_0x6C>();
                packetsMap.Value.Register<OnPlayMusicPacket_0x6D>();
                packetsMap.Value.Register<OnCharacterAnimationPacket_0x6E>();
                packetsMap.Value.Register<OnGraphicEffectPacket_0x70>();
                packetsMap.Value.Register<OnBulletinBoardPacket_0x71>();
                packetsMap.Value.Register<OnWarmodePacket_0x72>();
                packetsMap.Value.Register<OnPingPacket_0x73>();
                packetsMap.Value.Register<OnBuyListPacket_0x74>();
                packetsMap.Value.Register<OnUpdateCharacterPacket_0x77>();
                packetsMap.Value.Register<OnUpdateObjectAltPacket_0x78>();
                packetsMap.Value.Register<OnOpenMenuPacket_0x7C>();
                packetsMap.Value.Register<OnOpenPaperdollPacket_0x88>();
                packetsMap.Value.Register<OnCorpseEquipmentPacket_0x89>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_308Z)
                    packetsMap.Value.Register<OnShowMapPacket_0x90_Pre308Z>();
                else
                    packetsMap.Value.Register<OnShowMapPacket_0x90_Post308Z>();
                packetsMap.Value.Register<OnOpenBookPacket_0x93>();
                packetsMap.Value.Register<OnColorPickerPacket_0x95>();
                packetsMap.Value.Register<OnMovePlayerPacket_0x97>();
                packetsMap.Value.Register<OnUpdateNamePacket_0x98>();
                packetsMap.Value.Register<OnPlaceMultiPacket_0x99>();
                packetsMap.Value.Register<OnAsciiPromptPacket_0x9A>();
                packetsMap.Value.Register<OnSellListPacket_0x9E>();
                packetsMap.Value.Register<OnUpdateHitsPacket_0xA1>();
                packetsMap.Value.Register<OnUpdateManaPacket_0xA2>();
                packetsMap.Value.Register<OnUpdateStaminaPacket_0xA3>();
                packetsMap.Value.Register<OnOpenUrlPacket_0xA5>();
                packetsMap.Value.Register<OnWindowTipPacket_0xA6>();
                packetsMap.Value.Register<OnAttackEntityPacket_0xAA>();
                packetsMap.Value.Register<OnTextEntryDialogPacket_0xAB>();
                packetsMap.Value.Register<OnUnicodeSpeechPacket_0xAE>();
                packetsMap.Value.Register<OnShowDeathActionPacket_0xAF>();
                packetsMap.Value.Register<OnOpenGumpPacket_0xB0>();
                packetsMap.Value.Register<OnChatMessagePacket_0xB2>();
                packetsMap.Value.Register<OnOpenCharacterProfilePacket_0xB8>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_60142)
                    packetsMap.Value.Register<OnLockFeaturesPacket_0xB9_Pre60142>();
                else
                    packetsMap.Value.Register<OnLockFeaturesPacket_0xB9_Post60142>();

                if (gameCtx.Value.ClientVersion < ClientVersion.CV_7090)
                    packetsMap.Value.Register<OnQuestPointerPacket_0xBA_Pre7090>();
                else
                    packetsMap.Value.Register<OnQuestPointerPacket_0xBA_Post7090>();

                packetsMap.Value.Register<OnSeasonChangePacket_0xBC>();
                packetsMap.Value.Register<OnClientVersionPacket_0xBD>();
                packetsMap.Value.Register<OnExtendedCommandPacket_0xBF>();
                packetsMap.Value.Register<OnGraphicEffectC0Packet_0xC0>();
                packetsMap.Value.Register<OnClilocMessagePacket_0xC1>();
                packetsMap.Value.Register<OnUnicodePromptPacket_0xC2>();
                packetsMap.Value.Register<OnGraphicEffectC7Packet_0xC7>();
                packetsMap.Value.Register<OnViewRangePacket_0xC8>();
                packetsMap.Value.Register<OnClilocMessageAffixPacket_0xCC>();
                packetsMap.Value.Register<OnLogoutRequestPacket_0xD1>();
                packetsMap.Value.Register<OnUpdateCharacterAltPacket_0xD2>();
                packetsMap.Value.Register<OnUpdateObjectPacket_0xD3>();
                packetsMap.Value.Register<OnOpenBookAltPacket_0xD4>();
                packetsMap.Value.Register<OnMegaClilocPacket_0xD6>();
                packetsMap.Value.Register<OnCustomHousePacket_0xD8>();
                packetsMap.Value.Register<OnOplInfoPacket_0xDC>();
                packetsMap.Value.Register<OnOpenCompressedGumpPacket_0xDD>();
                packetsMap.Value.Register<OnUpdateMobileStatusPacket_0xDE>();
                packetsMap.Value.Register<OnBuffDebuffPacket_0xDF>();
                packetsMap.Value.Register<OnNewCharacterAnimationPacket_0xE2>();
                packetsMap.Value.Register<OnAddWaypointPacket_0xE5>();
                packetsMap.Value.Register<OnRemoveWaypointPacket_0xE6>();
                packetsMap.Value.Register<OnKrriosClientPacket_0xF0>();
                packetsMap.Value.Register<OnUpdateItemSAPacket_0xF3>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_308Z)
                    packetsMap.Value.Register<OnShowMapFacetPacket_0xF5_Pre308Z>();
                else
                    packetsMap.Value.Register<OnShowMapFacetPacket_0xF5_Post308Z>();
                packetsMap.Value.Register<OnBoatMovingPacket_0xF6>();
                packetsMap.Value.Register<OnPacketListPacket_0xF7>();
            })

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

            .AddObserver(handleLoginRequestsFn)

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
        On<OnLoginRequest> trigger,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        Res<Settings> settings
    )
    {
        network.Value.Connect(trigger.Event.Address, trigger.Event.Port);
        Console.WriteLine("Socket is connected ? {0}", network.Value.IsConnected);

        if (!network.Value.IsConnected)
            return;

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

        network.Value.Send_FirstLogin(trigger.Event.Username, Crypter.Decrypt(trigger.Event.Password));
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
        Res<GameContext> gameCtx,
        Res<CircularBuffer> buffer,
        Local<PacketBuffer> packetBuffer,
        EventWriter<IPacket> queuePackets,
        Commands commands
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

            var payload = sp.Slice(packetHeaderOffset, packetLen - packetHeaderOffset);

            // Dispatch both typed (observer trigger) and boxed (EventWriter<IPacket>)
            // when both are registered for the same id. Host gameplay code now uses
            // observers exclusively; the boxed queue is kept around for mods and
            // future bridges that want a single IPacket stream.
            packetsMap.Value.TryDispatch(packetId, commands, payload);

            if (packetsMap.Value.TryGetValue(packetId, out var fn))
            {
                var reader = new StackDataReader(payload);
                var packet = fn();
                packet.Fill(reader);
                queuePackets.Send(packet);
            }
        }

        network.Value.Flush();
    }
}
