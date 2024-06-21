using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs.NetworkPlugins;

using PacketsMap = Dictionary<byte, OnPacket>;
using NetworkEntitiesMap = Dictionary<uint, EcsID>;


readonly struct InGamePacketsPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddSystem((
            Res<Settings> settings,
            Res<PacketsMap> packetsMap,
            Res<NetworkEntitiesMap> networkEntitiesMap,
            Res<NetClient> network,
            Res<GameContext> gameCtx,
            EventWriter<OnNewChunkRequest> chunkRequests,
            Res<AssetsServer> assetsServer,
            TinyEcs.World world
        ) => {
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
                    network.Value.Send_Language(settings.Value.Language);
                }

                network.Value.Send_ClientVersion(settings.Value.ClientVersion);
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

                var isFlipped = false;
                var direction = (byte)(dir & Direction.Up);
                Assets.AnimationsLoader.Instance.GetAnimDirection(ref direction, ref isFlipped);
                var frames = assetsServer.Value.Animations.GetAnimationFrames(graphic, 0, direction,
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

            packetsMap.Value[0xBD] = buffer => network.Value.Send_ClientVersion(settings.Value.ClientVersion);

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

            packetsMap.Value[0x1A] = buffer => {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                ushort count = 0;
                byte graphicInc = 0;
                byte direction = 0;
                ushort hue = 0;
                byte flags = 0;
                byte type = 0;

                if ((serial & 0x80000000) != 0)
                {
                    serial &= 0x7FFFFFFF;
                    count = 1;
                }

                var graphic = reader.ReadUInt16BE();

                if ((graphic & 0x8000) != 0)
                {
                    graphic &= 0x7FFF;
                    graphicInc = reader.ReadUInt8();
                }

                if (count > 0)
                {
                    count = reader.ReadUInt16BE();
                }
                else
                {
                    count++;
                }

                var x = reader.ReadUInt16BE();

                if ((x & 0x8000) != 0)
                {
                    x &= 0x7FFF;
                    direction = 1;
                }

                var y = reader.ReadUInt16BE();

                if ((y & 0x8000) != 0)
                {
                    y &= 0x7FFF;
                    hue = 1;
                }

                if ((y & 0x4000) != 0)
                {
                    y &= 0x3FFF;
                    flags = 1;
                }

                if (direction != 0)
                {
                    direction = reader.ReadUInt8();
                }

                var z = reader.ReadInt8();

                if (hue != 0)
                {
                    hue = reader.ReadUInt16BE();
                }

                if (flags != 0)
                {
                    flags = reader.ReadUInt8();
                }

                //if (graphic != 0x2006)
                //    graphic += graphicInc;

                if (graphic >= 0x4000)
                {
                    //graphic -= 0x4000;
                    type = 2;
                }
            };

        }, Stages.Startup);
    }
}