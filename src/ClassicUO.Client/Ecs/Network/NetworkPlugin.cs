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

delegate void OnPacket(ReadOnlySpan<byte> buffer);

struct OnLoginRequest
{
    public string Username;
    public string Password;
    public string Address;
    public ushort Port;
}

internal sealed class PacketsMap
{
    private readonly Dictionary<byte, Func<IPacket>> _map = new();

    public void Add<T>(byte id) where T : IPacket, new()
    {
        var fn = () => (IPacket)new T();
        _map[id] = fn;
    }

    public void Add<T>() where T : IPacket, new()
    {
        var temp = new T();
        Add<T>(temp.Id);
    }

    public bool TryGetValue(byte id, out Func<IPacket> fn) => _map.TryGetValue(id, out fn);
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
                packetsMap.Value.Add<OnEnterWorldPacket_0x1B>();
                packetsMap.Value.Add<OnLoginCompletePacket_0x55>();
                packetsMap.Value.Add<OnExtendedCommandPacket_0xBF>();
                packetsMap.Value.Add<OnClientVersionPacket_0xBD>();
                packetsMap.Value.Add<OnUnicodeSpeechPacket_0xAE>();
                packetsMap.Value.Add<OnUpdateObjectPacket_0xD3>();
                packetsMap.Value.Add<OnUpdateObjectAltPacket_0x78>();
                packetsMap.Value.Add<OnViewRangePacket_0xC8>();
                packetsMap.Value.Add<OnAsciiSpeechPacket_0x1C>();
                packetsMap.Value.Add<OnUpdateItemPacket_0x1A>();
                packetsMap.Value.Add<OnDamagePacket_0x0B>();
                packetsMap.Value.Add<OnCharacterStatusPacket_0x11>();
                packetsMap.Value.Add<OnDenyWalkPacket_0x21>();
                packetsMap.Value.Add<OnConfirmWalkPacket_0x22>();
                packetsMap.Value.Add<OnOpenContainerPacket_0x24>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_6017)
                    packetsMap.Value.Add<OnUpdateContainerPacket_0x25_Pre6017>();
                else
                    packetsMap.Value.Add<OnUpdateContainerPacket_0x25_Post6017>();
                packetsMap.Value.Add<OnDenyMoveItemPacket_0x27>();
                packetsMap.Value.Add<OnEndDraggingItemPacket_0x28>();
                packetsMap.Value.Add<OnDropItemOkPacket_0x29>();
                packetsMap.Value.Add<OnHealthBarStatusPacket_0x16>();
                packetsMap.Value.Add<OnHealthBarStatusDetailsPacket_0x17>();
                packetsMap.Value.Add<OnDeleteObjectPacket_0x1D>();
                packetsMap.Value.Add<OnUpdatePlayerPacket_0x20>();
                packetsMap.Value.Add<OnDragAnimationPacket_0x23>();
                packetsMap.Value.Add<OnShowDeathScreenPacket_0x2C>();
                packetsMap.Value.Add<OnMobileAttributesPacket_0x2D>();
                packetsMap.Value.Add<OnEquipItemPacket_0x2E>();
                packetsMap.Value.Add<OnSwingPacket_0x2F>();
                packetsMap.Value.Add<OnUpdateSkillsPacket_0x3A>();
                packetsMap.Value.Add<OnPathfindingPacket_0x38>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_6017)
                    packetsMap.Value.Add<OnUpdateContainerItemsPacket_0x3C_Pre6017>();
                else
                    packetsMap.Value.Add<OnUpdateContainerItemsPacket_0x3C_Post6017>();
                packetsMap.Value.Add<OnPlayerLightLevelPacket_0x4E>();
                packetsMap.Value.Add<OnServerLightLevelPacket_0x4F>();
                packetsMap.Value.Add<OnSoundEffectPacket_0x54>();
                packetsMap.Value.Add<OnPlayMusicPacket_0x6D>();
                packetsMap.Value.Add<OnMapDataPacket_0x56>();
                packetsMap.Value.Add<OnWeatherPacket_0x65>();
                packetsMap.Value.Add<OnBookPagesPacket_0x66>();
                packetsMap.Value.Add<OnCharacterAnimationPacket_0x6E>();
                packetsMap.Value.Add<OnGraphicEffectPacket_0x70>();
                packetsMap.Value.Add<OnGraphicEffectC0Packet_0xC0>();
                packetsMap.Value.Add<OnGraphicEffectC7Packet_0xC7>();
                packetsMap.Value.Add<OnBulletinBoardPacket_0x71>();
                packetsMap.Value.Add<OnWarmodePacket_0x72>();
                packetsMap.Value.Add<OnPingPacket_0x73>();
                packetsMap.Value.Add<OnBuyListPacket_0x74>();
                packetsMap.Value.Add<OnUpdateCharacterPacket_0x77>();
                packetsMap.Value.Add<OnUpdateCharacterAltPacket_0xD2>();
                packetsMap.Value.Add<OnOpenMenuPacket_0x7C>();
                packetsMap.Value.Add<OnOpenPaperdollPacket_0x88>();
                packetsMap.Value.Add<OnCorpseEquipmentPacket_0x89>();
                packetsMap.Value.Add<OnShowMapPacket_0x90_Pre308Z>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_308Z)
                    packetsMap.Value.Add<OnShowMapFacetPacket_0xF5_Pre308Z>();
                else
                    packetsMap.Value.Add<OnShowMapFacetPacket_0xF5_Post308Z>();
                packetsMap.Value.Add<OnOpenBookPacket_0x93>();
                packetsMap.Value.Add<OnOpenBookAltPacket_0xD4>();
                packetsMap.Value.Add<OnColorPickerPacket_0x95>();
                packetsMap.Value.Add<OnMovePlayerPacket_0x97>();
                packetsMap.Value.Add<OnUpdateNamePacket_0x98>();
                packetsMap.Value.Add<OnPlaceMultiPacket_0x99>();
                packetsMap.Value.Add<OnAsciiPromptPacket_0x9A>();
                packetsMap.Value.Add<OnSellListPacket_0x9E>();
                packetsMap.Value.Add<OnUpdateHitsPacket_0xA1>();
                packetsMap.Value.Add<OnUpdateManaPacket_0xA2>();
                packetsMap.Value.Add<OnUpdateStaminaPacket_0xA3>();
                packetsMap.Value.Add<OnOpenUrlPacket_0xA5>();
                packetsMap.Value.Add<OnWindowTipPacket_0xA6>();
                packetsMap.Value.Add<OnAttackEntityPacket_0xAA>();
                packetsMap.Value.Add<OnTextEntryDialogPacket_0xAB>();
                packetsMap.Value.Add<OnShowDeathActionPacket_0xAF>();
                packetsMap.Value.Add<OnOpenGumpPacket_0xB0>();
                packetsMap.Value.Add<OnChatMessagePacket_0xB2>();
                packetsMap.Value.Add<OnOpenCharacterProfilePacket_0xB8>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_60142)
                    packetsMap.Value.Add<OnLockFeaturesPacket_0xB9_Pre60142>();
                else
                    packetsMap.Value.Add<OnLockFeaturesPacket_0xB9_Post60142>();

                if (gameCtx.Value.ClientVersion < ClientVersion.CV_7090)
                    packetsMap.Value.Add<OnQuestPointerPacket_0xBA_Pre7090>();
                else
                    packetsMap.Value.Add<OnQuestPointerPacket_0xBA_Post7090>();

                packetsMap.Value.Add<OnSeasonChangePacket_0xBC>();
                packetsMap.Value.Add<OnClilocMessagePacket_0xC1>();
                packetsMap.Value.Add<OnClilocMessageAffixPacket_0xCC>();
                packetsMap.Value.Add<OnUnicodePromptPacket_0xC2>();
                packetsMap.Value.Add<OnLogoutRequestPacket_0xD1>();
                packetsMap.Value.Add<OnMegaClilocPacket_0xD6>();
                packetsMap.Value.Add<OnCustomHousePacket_0xD8>();
                packetsMap.Value.Add<OnOplInfoPacket_0xDC>();
                packetsMap.Value.Add<OnOpenCompressedGumpPacket_0xDD>();
                packetsMap.Value.Add<OnUpdateMobileStatusPacket_0xDE>();
                packetsMap.Value.Add<OnBuffDebuffPacket_0xDF>();
                packetsMap.Value.Add<OnNewCharacterAnimationPacket_0xE2>();
                packetsMap.Value.Add<OnAddWaypointPacket_0xE5>();
                packetsMap.Value.Add<OnRemoveWaypointPacket_0xE6>();
                packetsMap.Value.Add<OnKrriosClientPacket_0xF0>();
                packetsMap.Value.Add<OnUpdateItemSAPacket_0xF3>();
                packetsMap.Value.Add<OnBoatMovingPacket_0xF6>();
                packetsMap.Value.Add<OnPacketListPacket_0xF7>();
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
        Res<GameContext> gameCtx,
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

            if (packetsMap.Value.TryGetValue(packetId, out var fn))
            {
                var payload = sp.Slice(packetHeaderOffset, packetLen - packetHeaderOffset);
                var reader = new StackDataReader(payload);
                var packet = fn();
                packet.Fill(reader);
                queuePackets.Send(packet);
            }
        }

        network.Value.Flush();
    }
}
