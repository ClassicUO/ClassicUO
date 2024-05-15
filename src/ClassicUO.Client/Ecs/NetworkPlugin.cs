using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs;

using PacketsMap = Dictionary<byte, OnPacket>;

delegate void OnPacket(Span<byte> buffer);

readonly struct NetworkPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new NetClient());
        scheduler.AddResource(new PacketsMap());

        scheduler.AddSystem((
            Res<PacketsMap> packetsMap,
            Res<NetClient> network,
            Res<GameContext> gameCtx,
            EventWriter<OnNewChunkRequest> chunkRequests,
            Res<AssetsServer> assetsServer,
            TinyEcs.World world
        ) => {
            packetsMap.Value[0xA8] = buffer => {
                var reader = new StackDataReader(buffer);
                var flags = reader.ReadUInt8();
                var count = reader.ReadUInt16BE();
                var serverList = new List<(ushort index, string name)>();

                for (var i = 0; i < count; ++i)
                {
                    var index = reader.ReadUInt16BE();
                    var name = reader.ReadASCII(32, true);
                    var percFull = reader.ReadUInt8();
                    var timeZone = reader.ReadUInt8();
                    var address = reader.ReadUInt32BE();

                    serverList.Add((index, name));
                    Console.WriteLine("server entry -> {0}", name);
                }

                network.Value.Send_SelectServer((byte) serverList[0].index);
            };

            packetsMap.Value[0xA9] = buffer => {
                var reader = new StackDataReader(buffer);
                var charactersCount = reader.ReadUInt8();
                var characterNames = new List<string>();
                for (var i = 0; i < charactersCount; ++i)
                {
                    characterNames.Add(reader.ReadASCII(30).TrimEnd('\0'));
                    reader.Skip(30);
                }

                var cityCount = reader.ReadUInt8();
                // bla bla

                network.Value.Send_SelectCharacter(0, characterNames[0], network.Value.LocalIP, gameCtx.Value.Protocol);
            };

            packetsMap.Value[0x8C] = buffer => {
                var reader = new StackDataReader(buffer);
                var ip = reader.ReadUInt32LE();
                var port = reader.ReadUInt16BE();
                var seed = reader.ReadUInt32BE();

                network.Value.Disconnect();
                network.Value.Connect(new IPAddress(ip).ToString(), port);

                if (network.Value.IsConnected)
                {
                    network.Value.EnableCompression();
                    Span<byte> b = [(byte)(seed >> 24), (byte)(seed >> 16), (byte)(seed >> 8), (byte)seed];
                    network.Value.Send(b, true, true);
                    network.Value.Send_SecondLogin(Settings.GlobalSettings.Username, Crypter.Decrypt(Settings.GlobalSettings.Password), seed);
                }
            };

            packetsMap.Value[0x1B] = buffer => {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                reader.Skip(4);
                var graphic = reader.ReadUInt16BE();
                var x = reader.ReadUInt16BE();
                var y = reader.ReadUInt16BE();
                var z = (sbyte) reader.ReadUInt16BE();
                var dir = (Direction) reader.ReadUInt8();
                reader.Skip(9);
                var mapWidth = reader.ReadUInt16BE();
                var mapHeight = reader.ReadUInt16BE();

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_200)
                {
                    network.Value.Send_GameWindowSize(800, 400);
                    network.Value.Send_Language(Settings.GlobalSettings.Language);
                }

                network.Value.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);
                network.Value.Send_ClickRequest(serial);
                network.Value.Send_SkillsRequest(serial);

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_70796)
                    network.Value.Send_ShowPublicHouseContent(true);

                gameCtx.Value.CenterX = x;
                gameCtx.Value.CenterY = y;
                gameCtx.Value.CenterZ = z;
                gameCtx.Value.PlayerSerial = serial;
                gameCtx.Value.MaxMapWidth = mapWidth;
                gameCtx.Value.MaxMapHeight = mapHeight;

                var offset = 8;
                chunkRequests.Enqueue(new() {
                    Map = gameCtx.Value.Map,
                    RangeStartX = Math.Max(0, x / 8 - offset),
                    RangeStartY = Math.Max(0, y / 8 - offset),
                    RangeEndX = Math.Min(mapWidth / 8, x / 8 + offset),
                    RangeEndY = Math.Min(mapHeight / 8, y / 8 + offset),
                });

                var frames = assetsServer.Value.Animations.GetAnimationFrames(graphic, 0, (byte)(dir & Direction.Up),
                                                                                out var hue, out var _);
                var ent = world.Entity()
                    .Set(new Ecs.NetworkSerial() { Value = serial })
                    .Set(new Ecs.WorldPosition() { X = x, Y = y, Z = z })
                    .Set(new Ecs.Graphic() { Value = graphic })
                    .Set(new Renderable()
                    {
                        Color = Vector3.UnitZ,
                        Position = Isometric.IsoToScreen(x, y, z) + new Vector2(frames[0].Center.X, frames[0].Center.Y),
                        Texture = frames[0].Texture,
                        UV = frames[0].UV,
                        Z = Isometric.GetDepthZ(x, y, z + 1)
                    });
            };

            packetsMap.Value[0x55] = buffer => {
                if (gameCtx.Value.PlayerSerial == 0)
                    return;

                network.Value.Send_StatusRequest(gameCtx.Value.PlayerSerial);
                network.Value.Send_OpenChat("");

                network.Value.Send_SkillsRequest(gameCtx.Value.PlayerSerial);
                network.Value.Send_DoubleClick(gameCtx.Value.PlayerSerial);

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_306E)
                    network.Value.Send_ClientType(gameCtx.Value.Protocol);

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_305D)
                    network.Value.Send_ClientViewRange(24);
            };

            packetsMap.Value[0xBF] = buffer => {
                var reader = new StackDataReader(buffer);
                var cmd = reader.ReadUInt16BE();

                if (cmd == 8)
                {
                    var mapIndex = reader.ReadUInt8();
                    if (gameCtx.Value.Map != mapIndex)
                    {
                        gameCtx.Value.Map = mapIndex;
                    }
                }
            };

            packetsMap.Value[0xBD] = buffer => network.Value.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);

            packetsMap.Value[0xAE] = buffer => {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var msgType = (MessageType) reader.ReadUInt8();
                var hue = reader.ReadUInt16BE();
                var font = reader.ReadUInt16BE();
                var lang = reader.ReadASCII(4);
                var name = reader.ReadASCII(30);

                if (serial == 0 && graphic == 0 && msgType == MessageType.Regular &&
                    font == 0xFFFF && hue == 0xFFFF && name.Equals("system", StringComparison.InvariantCultureIgnoreCase))
                {
                    network.Value.Send([
                        0x03, 0x00, 0x28, 0x20, 0x00, 0x34, 0x00, 0x03, 0xdb, 0x13,
                        0x14, 0x3f, 0x45, 0x2c, 0x58, 0x0f, 0x5d, 0x44, 0x2e, 0x50,
                        0x11, 0xdf, 0x75, 0x5c, 0xe0, 0x3e, 0x71, 0x4f, 0x31, 0x34,
                        0x05, 0x4e, 0x18, 0x1e, 0x72, 0x0f, 0x59, 0xad, 0xf5, 0x00
                    ]);
                }
                else
                {
                    var text = reader.ReadUnicodeBE();
                    Console.WriteLine("0xAE: {0}", text);
                }
            };


            var d3_78 = (byte id, Span<byte> buffer) => {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var x = reader.ReadUInt16BE();
                var y = reader.ReadUInt16BE();
                var z = reader.ReadInt8();
                var dir = (Direction)reader.ReadUInt8();
                var hue = reader.ReadUInt16BE();
                var flags = (Flags)reader.ReadUInt8();
                var notoriety = (NotorietyFlag)reader.ReadUInt8();

                if (id == 0xD3)
                    reader.Skip(sizeof(ushort) * 3);

                // Texture2D texture;
                // Rectangle uv;

                // if (SerialHelper.IsItem(serial))
                // {
                //     ref readonly var artInfo = ref assetsServer.Value.Arts.GetArt(graphic);
                //     texture = artInfo.Texture;
                //     uv = artInfo.UV;
                // }
                // else
                // {
                //     var frames = assetsServer.Value.Animations.GetAnimationFrames(graphic, 0, (byte)(dir & Direction.Up), out var _, out var _);
                //     texture = frames[0].Texture;
                //     uv = frames[0].UV;
                // }

                // var ent = world.Entity()
                //     .Set(new Ecs.NetworkSerial() { Value = serial })
                //     .Set(new Ecs.Graphic() { Value = graphic })
                //     .Set(new Ecs.WorldPosition() { X = x, Y = y, Z = z })
                //     .Set(new Renderable()
                //     {
                //         Texture = texture,
                //         UV = uv,
                //         Color = Vector3.UnitZ,
                //         Position = Isometric.IsoToScreen(x, y, z),
                //         Z = Isometric.GetDepthZ(x, y, z + 1)
                //     });

                uint itemSerial;
                while ((itemSerial = reader.ReadUInt32BE()) != 0)
                {
                    var itemGraphic = reader.ReadUInt16BE();
                    var layer = (Layer)reader.ReadUInt8();
                    ushort itemHue = 0;

                    if (gameCtx.Value.ClientVersion>= ClientVersion.CV_70331)
                        itemHue = reader.ReadUInt16BE();
                    else if ((itemGraphic & 0x8000) != 0)
                    {
                        itemGraphic &= 0x7FFF;
                        itemHue = reader.ReadUInt16BE();
                    }

                    // var child = world.Entity()
                    //     .Set(new Ecs.NetworkSerial() { Value = itemSerial })
                    //     .Set(new Ecs.Graphic() { Value  = itemGraphic });

                    // ent.AddChild(child);
                }
            };

            packetsMap.Value[0xD3] = buffer => d3_78(0xD3, buffer);
            packetsMap.Value[0x78] = buffer => d3_78(0x78, buffer);

            packetsMap.Value[0xC8] = buffer => {
                var reader = new StackDataReader(buffer);
                var range = reader.ReadUInt8();
            };

        }, Stages.Startup);

        scheduler.AddSystem((Res<NetClient> network, Res<GameContext> gameCtx) => {
            PacketsTable.AdjustPacketSizeByVersion(gameCtx.Value.ClientVersion);
            network.Value.Connect(Settings.GlobalSettings.IP, Settings.GlobalSettings.Port);

            Console.WriteLine("Socket is connected ? {0}", network.Value.IsConnected);

            if (gameCtx.Value.ClientVersion >= ClientVersion.CV_6040)
            {
                // NOTE: im forcing the use of latest client just for convenience rn
                var major = (byte) ((uint)gameCtx.Value.ClientVersion >> 24);
                var minor = (byte) ((uint)gameCtx.Value.ClientVersion >> 16);
                var build = (byte) ((uint)gameCtx.Value.ClientVersion >> 8);
                var extra = (byte) gameCtx.Value.ClientVersion;

                network.Value.Send_Seed(network.Value.LocalIP, major, minor, build, extra);
            }
            else
            {
                network.Value.Send_Seed_Old(network.Value.LocalIP);
            }

            network.Value.Send_FirstLogin(Settings.GlobalSettings.Username, Crypter.Decrypt(Settings.GlobalSettings.Password));
        }, Stages.Startup);

        scheduler.AddSystem((Res<NetClient> network, Res<PacketsMap> packetsMap) => {
            var availableData = network.Value.CollectAvailableData();
            if (availableData.Count != 0)
            {
                var realBuffer = availableData.AsSpan();
                while (!realBuffer.IsEmpty)
                {
                    var packetId = realBuffer[0];
                    var packetLen = PacketsTable.GetPacketLength(packetId);
                    var packetHeaderOffset = sizeof(byte);

                    if (packetLen == -1)
                    {
                        if (realBuffer.Length < 3)
                            return;

                        packetLen = BinaryPrimitives.ReadInt16BigEndian(realBuffer[packetHeaderOffset..]);
                        packetHeaderOffset += sizeof(ushort);
                    }

                    Console.WriteLine(">> packet-in: ID 0x{0:X2} | Len: {1}", packetId, packetLen);

                    if (packetsMap.Value.TryGetValue(packetId, out var handler))
                    {
                        handler(realBuffer[packetHeaderOffset .. packetLen]);
                    }

                    realBuffer = realBuffer[packetLen ..];
                }
            }

            network.Value.Flush();
        }).RunIf((Res<NetClient> network) => network.Value!.IsConnected);
    }
}