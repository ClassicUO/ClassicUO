using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Ecs.Modding.Host;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct LoginPacketsPlugin : IPlugin
{
    public void Build(App app)
    {
        app
            .AddSystem(Stage.Startup, (Res<PacketsMap> packetsMap) =>
            {
                packetsMap.Value.Register<OnLoginErrorPacket_0x53>();
                packetsMap.Value.Register<OnLoginErrorPacket_0x82>();
                packetsMap.Value.Register<OnLoginErrorPacket_0x85>();
                packetsMap.Value.Register<OnServerRelayPacket_0x8C>();
                packetsMap.Value.Register<OnServerListPacket_0xA8>();
                packetsMap.Value.Register<OnCharacterListPacket_0xA9>();
            })

            ;

        // Per-packet observers — fire on the typed PacketReceived<T> trigger
        // emitted by NetworkPlugin's typed dispatch. No EventReader<IPacket>
        // scan, no RunIf gate, no boxed type-test cascade.
        app.AddObserver((
            On<PacketReceived<OnServerListPacket_0xA8>> trig,
            ResMut<NextState<GameState>> gameState,
            EventWriter<ServerSelectionInfoEvent> serverWriter,
            EventWriter<HostMessage> hostMsgsWriter)
            => HandleServerListPacket(trig.Event.Packet, gameState, serverWriter, hostMsgsWriter));

        app.AddObserver((
            On<PacketReceived<OnCharacterListPacket_0xA9>> trig,
            ResMut<GameContext> gameCtx,
            ResMut<NextState<GameState>> gameState,
            ResMut<CharacterListSnapshot> snapshot,
            EventWriter<CharacterSelectionInfoEvent> characterWriter,
            EventWriter<HostMessage> hostMsgsWriter)
            => HandleCharacterListPacket(trig.Event.Packet, gameCtx, gameState, snapshot, characterWriter, hostMsgsWriter));

        app.AddObserver((
            On<PacketReceived<OnServerRelayPacket_0x8C>> trig,
            Res<Settings> settings,
            Res<NetClient> network)
            => HandleServerRelayPacket(trig.Event.Packet, settings, network));

        app.AddObserver((
            On<PacketReceived<OnLoginErrorPacket_0x82>> trig,
            Res<UOFileManager> fileManager,
            ResMut<NextState<GameState>> gameState,
            EventWriter<LoginErrorsInfoEvent> loginErrorWriter)
            => HandleLoginErrorPacket(trig.Event.Packet.Id, trig.Event.Packet, fileManager, gameState, loginErrorWriter));

        app.AddObserver((
            On<PacketReceived<OnLoginErrorPacket_0x85>> trig,
            Res<UOFileManager> fileManager,
            ResMut<NextState<GameState>> gameState,
            EventWriter<LoginErrorsInfoEvent> loginErrorWriter)
            => HandleLoginErrorPacket(trig.Event.Packet.Id, trig.Event.Packet, fileManager, gameState, loginErrorWriter));

        app.AddObserver((
            On<PacketReceived<OnLoginErrorPacket_0x53>> trig,
            Res<UOFileManager> fileManager,
            ResMut<NextState<GameState>> gameState,
            EventWriter<LoginErrorsInfoEvent> loginErrorWriter)
            => HandleLoginErrorPacket(trig.Event.Packet.Id, trig.Event.Packet, fileManager, gameState, loginErrorWriter));
    }

    static void HandleServerListPacket(
        OnServerListPacket_0xA8 packet,
        ResMut<NextState<GameState>> gameState,
        EventWriter<ServerSelectionInfoEvent> serverWriter,
        EventWriter<HostMessage> hostMsgsWriter
    )
    {
        gameState.Value.Set(GameState.ServerSelection);
        serverWriter.Send(new ServerSelectionInfoEvent
        {
            Servers = packet.Servers
        });

        hostMsgsWriter.Send(new HostMessage.ServerLoginResponse(packet.Flags, packet.Servers));
    }

    static void HandleCharacterListPacket(
        OnCharacterListPacket_0xA9 packet,
        ResMut<GameContext> gameCtx,
        ResMut<NextState<GameState>> gameState,
        ResMut<CharacterListSnapshot> snapshot,
        EventWriter<CharacterSelectionInfoEvent> characterWriter,
        EventWriter<HostMessage> hostMsgsWriter
    )
    {
        var (towns, flags) = ParseTownsAndFlags(packet, gameCtx.Value.ClientVersion);
        gameCtx.Value.ClientFeatures = flags;

        // Kept for flows that need the data after the event is consumed —
        // character creation reads towns (city step) and the first free slot.
        snapshot.Value.Characters = packet.Characters;
        snapshot.Value.Towns = towns;

        gameState.Value.Set(GameState.CharacterSelection);
        characterWriter.Send(new CharacterSelectionInfoEvent
        {
            Characters = packet.Characters,
            Towns = towns
        });

        hostMsgsWriter.Send(new HostMessage.LoginResponse(flags, packet.Characters, towns));
    }

    static void HandleServerRelayPacket(
        OnServerRelayPacket_0x8C packet,
        Res<Settings> settings,
        Res<NetClient> network
    )
    {
        network.Value.Disconnect();
        network.Value.Connect(new IPAddress(packet.Ip).ToString(), packet.Port);

        if (network.Value.IsConnected)
        {
            network.Value.Encryption?.Initialize(false, packet.Seed);
            network.Value.EnableCompression();
            Span<byte> seedBytes = stackalloc byte[4]
            {
                (byte)(packet.Seed >> 24),
                (byte)(packet.Seed >> 16),
                (byte)(packet.Seed >> 8),
                (byte)packet.Seed
            };

            network.Value.Send(seedBytes, true, true);
            network.Value.Send_SecondLogin(settings.Value.Username, Crypter.Decrypt(settings.Value.Password), packet.Seed);
        }
    }

    static void HandleLoginErrorPacket(
        byte packetId,
        ILoginErrorPacket packet,
        Res<UOFileManager> fileManager,
        ResMut<NextState<GameState>> gameState,
        EventWriter<LoginErrorsInfoEvent> loginErrorWriter
    )
    {
        var errorMsg = ServerErrorMessages.GetError(fileManager.Value.Clilocs, packetId, packet.Code);

        gameState.Value.Set(GameState.LoginError);
        loginErrorWriter.Send(new LoginErrorsInfoEvent
        {
            Error = new(errorMsg)
        });
    }

    // Parse the city block then the CharacterListFlags that follow it. The flags
    // sit right after the (version-sized) city entries; a trailing (short)-1 on
    // 70130+ clients is left unread. Reading flags here — not in the packet's
    // Fill — is the only place the client version is known to size the cities.
    static (List<TownInfo> towns, CharacterListFlags flags) ParseTownsAndFlags(
        OnCharacterListPacket_0xA9 packet, ClientVersion clientVersion)
    {
        var towns = new List<TownInfo>();
        if (packet.CityData.Length == 0)
            return (towns, 0);

        var reader = new StackDataReader(packet.CityData);
        var useNewFormat = clientVersion >= ClientVersion.CV_70130;

        for (var i = 0; i < packet.CityCount && reader.Remaining > 0; ++i)
        {
            var index = reader.ReadUInt8();

            if (useNewFormat && reader.Remaining >= 32 + 32 + sizeof(uint) * 5)
            {
                var name = reader.ReadASCII(32);
                var building = reader.ReadASCII(32);
                var x = (ushort)reader.ReadUInt32BE();
                var y = (ushort)reader.ReadUInt32BE();
                var z = (sbyte)reader.ReadUInt32BE();
                var map = reader.ReadUInt32BE();
                var cliloc = reader.ReadUInt32BE();

                // skip unknown padding
                if (reader.Remaining >= sizeof(uint))
                    reader.Skip(sizeof(uint));

                towns.Add(new TownInfo(index, name, building, (x, y, z), map, cliloc));
            }
            else
            {
                if (reader.Remaining < 31 + 31)
                    break;

                var name = reader.ReadASCII(31);
                var building = reader.ReadASCII(31);
                towns.Add(new TownInfo(index, name, building, (0, 0, 0), 0, 0));
            }
        }

        var flags = reader.Remaining >= sizeof(uint)
            ? (CharacterListFlags)reader.ReadUInt32BE()
            : (CharacterListFlags)0;
        return (towns, flags);
    }
}
