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

internal sealed class PacketsMap : Dictionary<byte, Func<IPacket>>;

struct CustomPacket : IPacket
{
    public byte Id => throw new NotImplementedException();

    public void Fill(StackDataReader reader)
    {
        throw new NotImplementedException();
    }
}


readonly struct NetworkPlugin : IPlugin
{
    public void Build(App app)
    {
        var setupSocketFn = SetupSocket;
        var handleLoginRequestsFn = HandleLoginRequests;
        var packetReaderFn = PacketReader;

        app.AddResource(new CircularBuffer());
        app.AddResource(new PacketsMap());

        app
            .AddSystem(Stage.Startup, setupSocketFn)

            .AddSystem(Stage.Startup, (
                Res<PacketsMap> packetsMap,
                Res<GameContext> gameCtx
            ) =>
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
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_6017)
                    create<OnUpdateContainerPacket_0x25_Pre6017>();
                else
                    create<OnUpdateContainerPacket_0x25_Post6017>();
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
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_6017)
                    create<OnUpdateContainerItemsPacket_0x3C_Pre6017>();
                else
                    create<OnUpdateContainerItemsPacket_0x3C_Post6017>();
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
                create<OnShowMapPacket_0x90_Pre308Z>();
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_308Z)
                    create<OnShowMapFacetPacket_0xF5_Pre308Z>();
                else
                    create<OnShowMapFacetPacket_0xF5_Post308Z>();
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
                if (gameCtx.Value.ClientVersion < ClientVersion.CV_60142)
                    create<OnLockFeaturesPacket_0xB9_Pre60142>();
                else
                    create<OnLockFeaturesPacket_0xB9_Post60142>();

                if (gameCtx.Value.ClientVersion < ClientVersion.CV_7090)
                    create<OnQuestPointerPacket_0xBA_Pre7090>();
                else
                    create<OnQuestPointerPacket_0xBA_Post7090>();

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
