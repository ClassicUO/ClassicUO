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
            .AddSystem(Stage.Startup, (Res<PacketsMap2> packetsMap) =>
            {
                void create<T>() where T : IPacket, new()
                {
                    var temp = new T();
                    if (!packetsMap.Value.ContainsKey(temp.Id))
                    {
                        packetsMap.Value.Add(temp.Id, () => new T());
                    }
                }

                create<OnServerListPacket_0xA8>();
                create<OnCharacterListPacket_0xA9>();
                create<OnServerRelayPacket_0x8C>();
                create<OnLoginErrorPacket_0x82>();
                create<OnLoginErrorPacket_0x85>();
                create<OnLoginErrorPacket_0x53>();
            })

            .AddSystem(
                (
                    EventReader<IPacket> packets,
                    Res<Settings> settings,
                    Res<NetClient> network,
                    Res<UOFileManager> fileManager,
                    ResMut<GameContext> gameCtx,
                    ResMut<NextState<GameState>> gameState,
                    EventWriter<ServerSelectionInfoEvent> serverWriter,
                    EventWriter<CharacterSelectionInfoEvent> characterWriter,
                    EventWriter<LoginErrorsInfoEvent> loginErrorWriter,
                    EventWriter<HostMessage> hostMsgsWriter
                ) =>
                {
                    foreach (var packet in packets.Read())
                    {
                        switch (packet)
                        {
                            case OnServerListPacket_0xA8 serverList:
                                HandleServerListPacket(serverList, gameState, serverWriter, hostMsgsWriter);
                                break;

                            case OnCharacterListPacket_0xA9 characterList:
                                HandleCharacterListPacket(characterList, gameCtx, gameState, characterWriter, hostMsgsWriter);
                                break;

                            case OnServerRelayPacket_0x8C serverRelay:
                                HandleServerRelayPacket(serverRelay, settings, network);
                                break;

                            case OnLoginErrorPacket_0x82 loginError82:
                                HandleLoginErrorPacket(packet.Id, loginError82, fileManager, gameState, loginErrorWriter);
                                break;

                            case OnLoginErrorPacket_0x85 loginError85:
                                HandleLoginErrorPacket(packet.Id, loginError85, fileManager, gameState, loginErrorWriter);
                                break;

                            case OnLoginErrorPacket_0x53 loginError53:
                                HandleLoginErrorPacket(packet.Id, loginError53, fileManager, gameState, loginErrorWriter);
                                break;
                        }
                    }
                })
            .InStage(Stage.Update)
            .RunIf((EventReader<IPacket> packets) => packets.HasEvents)
            .Build();
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
        EventWriter<CharacterSelectionInfoEvent> characterWriter,
        EventWriter<HostMessage> hostMsgsWriter
    )
    {
        var towns = ParseTowns(packet, gameCtx.Value.ClientVersion);
        gameCtx.Value.ClientFeatures = packet.Flags;

        gameState.Value.Set(GameState.CharacterSelection);
        characterWriter.Send(new CharacterSelectionInfoEvent
        {
            Characters = packet.Characters,
            Towns = towns
        });

        hostMsgsWriter.Send(new HostMessage.LoginResponse(packet.Flags, packet.Characters, towns));
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

    static List<TownInfo> ParseTowns(OnCharacterListPacket_0xA9 packet, ClientVersion clientVersion)
    {
        var towns = new List<TownInfo>();
        if (packet.CityData.Length == 0 || packet.CityCount == 0)
            return towns;

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

        return towns;
    }
}
