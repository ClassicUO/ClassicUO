using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using TinyEcs;

namespace ClassicUO.Ecs.NetworkPlugins;

using PacketsMap = Dictionary<byte, OnPacket>;

internal readonly struct LoginPacketsPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.OnStartup((
            Res<Settings> settings,
            Res<PacketsMap> packetsMap,
            Res<NetClient> network,
            Res<UOFileManager> fileMaanger,
            Res<GameContext> gameCtx,
            State<GameState> gameState,
            EventWriter<ServerSelectionInfoEvent> serverWriter,
            EventWriter<CharacterSelectionInfoEvent> characterWriter,
            EventWriter<LoginErrorsInfoEvent> loginErrorWriter
        ) =>
        {
            // server list
            packetsMap.Value[0xA8] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var flags = reader.ReadUInt8();
                var count = reader.ReadUInt16BE();
                var serverList = new List<ServerInfo>();

                for (var i = 0; i < count; ++i)
                {
                    var index = reader.ReadUInt16BE();
                    var name = reader.ReadASCII(32, true);
                    var percFull = reader.ReadUInt8();
                    var timeZone = reader.ReadUInt8();
                    var address = reader.ReadUInt32BE();

                    serverList.Add(new(
                        index, name, percFull, timeZone, address
                    ));
                }

                gameState.Set(GameState.ServerSelection);
                serverWriter.Enqueue(new()
                {
                    Servers = serverList
                });
            };

            // characters list
            packetsMap.Value[0xA9] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var charactersCount = reader.ReadUInt8();
                var characters = new List<CharacterInfo>();
                for (uint i = 0; i < charactersCount; ++i)
                {
                    var name = reader.ReadASCII(30).TrimEnd('\0');
                    reader.Skip(30);

                    if (!string.IsNullOrEmpty(name))
                        characters.Add(new(name, i));
                }

                var cityCount = reader.ReadUInt8();
                var cities = new List<TownInfo>();
                var asNew = gameCtx.Value.ClientVersion >= ClientVersion.CV_70130;
                for (var i = 0; i < cityCount; ++i)
                {
                    TownInfo city;
                    if (asNew)
                    {
                        city = new TownInfo(
                            reader.ReadUInt8(),
                            reader.ReadASCII(32),
                            reader.ReadASCII(32),
                            ((ushort)reader.ReadUInt32BE(), (ushort)reader.ReadUInt32BE(), (sbyte)reader.ReadUInt32BE()),
                            reader.ReadUInt32BE(),
                            reader.ReadUInt32BE()
                        );

                        reader.Skip(4);
                    }
                    else
                    {
                        city = new TownInfo(
                            reader.ReadUInt8(),
                            reader.ReadASCII(31),
                            reader.ReadASCII(31),
                            (0, 0, 0), // TODO: X, Y. Z is 0
                            0,
                            0
                        );
                    }

                    cities.Add(city);
                }

                gameCtx.Value.ClientFeatures = (CharacterListFlags)reader.ReadUInt32BE();

                gameState.Set(GameState.CharacterSelection);
                characterWriter.Enqueue(new()
                {
                    Characters = characters,
                    Towns = cities
                });
            };

            // server relay
            packetsMap.Value[0x8C] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var ip = reader.ReadUInt32LE();
                var port = reader.ReadUInt16BE();
                var seed = reader.ReadUInt32BE();

                network.Value.Disconnect();
                network.Value.Connect(new IPAddress(ip).ToString(), port);

                if (network.Value.IsConnected)
                {
                    network.Value.Encryption?.Initialize(false, seed);
                    network.Value.EnableCompression();
                    Span<byte> b = [(byte)(seed >> 24), (byte)(seed >> 16), (byte)(seed >> 8), (byte)seed];
                    network.Value.Send(b, true, true);
                    network.Value.Send_SecondLogin(settings.Value.Username, Crypter.Decrypt(settings.Value.Password), seed);
                }
            };

            // login errors
            void loginErrorsPackets(byte id, ReadOnlySpan<byte> buffer)
            {
                var reader = new StackDataReader(buffer);
                var code = reader.ReadUInt8();

                var errorMsg = ServerErrorMessages.GetError(fileMaanger.Value.Clilocs, id, code);

                gameState.Set(GameState.LoginError);
                loginErrorWriter.Enqueue(new()
                {
                    Error = new(errorMsg)
                });
            }

            packetsMap.Value[0x82] = (buffer) => loginErrorsPackets(0x82, buffer);
            packetsMap.Value[0x85] = (buffer) => loginErrorsPackets(0x85, buffer);
            packetsMap.Value[0x53] = (buffer) => loginErrorsPackets(0x53, buffer);

        }, ThreadingMode.Single);
    }
}
