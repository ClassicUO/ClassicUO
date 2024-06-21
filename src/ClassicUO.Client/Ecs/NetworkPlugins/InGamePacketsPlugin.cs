using System;
using System.Collections.Generic;
using System.Threading;
using ClassicUO.Assets;
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
            // enter world
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

            // login complete
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

            // extended commands
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

            // client version
            packetsMap.Value[0xBD] = buffer => network.Value.Send_ClientVersion(settings.Value.ClientVersion);

            // unicode speech
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

            // update object
            packetsMap.Value[0xD3] = buffer => d3_78(0xD3, buffer);
            packetsMap.Value[0x78] = buffer => d3_78(0x78, buffer);

            // view range
            packetsMap.Value[0xC8] = buffer => {
                var reader = new StackDataReader(buffer);
                var range = reader.ReadUInt8();
            };

            // update item
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

            // damage
            packetsMap.Value[0x0B] = buffer => {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                var damage = reader.ReadUInt16BE();
            };

            // character status
            packetsMap.Value[0x11] = buffer => {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                var name = reader.ReadASCII(30);
                (var hits, var hitsMax) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                var canBeRenamed = reader.ReadBool();
                var type = reader.ReadUInt8();

                if (type > 0)
                {
                    var isFemale = reader.ReadBool();
                    (var str, var dex, var intell) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadUInt16BE());
                    (var stam, var stamMax) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                    (var mana, var manaMax) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                    var gold = reader.ReadUInt32BE();
                    var physicalRes = reader.ReadInt16BE();
                    var weigth = reader.ReadUInt16BE();

                    if (type >= 5)
                    {
                        var weightMax = reader.ReadUInt16BE();
                        var race = reader.ReadUInt8();
                    }

                    if (type >= 3)
                    {
                        var statsCap = reader.ReadInt16BE();
                        (var followers, var maxFollowers) = (reader.ReadUInt8(), reader.ReadUInt8());
                    }

                    if (type >= 4)
                    {
                        var fireRes = reader.ReadInt16BE();
                        var coldRes = reader.ReadInt16BE();
                        var poisonRes = reader.ReadInt16BE();
                        var energyRes = reader.ReadInt16BE();
                        var luck = reader.ReadUInt16BE();
                        (var damageMin, var dagameMax) = (reader.ReadInt16BE(), reader.ReadInt16BE());
                        var thithingPoints = reader.ReadUInt32BE();
                    }

                    if (type >= 6)
                    {
                        var maxPhysicalRes = reader.ReadInt16BE();
                        var maxFireRes = reader.ReadInt16BE();
                        var maxColdRes = reader.ReadInt16BE();
                        var maxPoisonRes = reader.ReadInt16BE();
                        var maxEnergyRes = reader.ReadInt16BE();
                        (var dci, var maxDci) = (reader.ReadInt16BE(), reader.ReadInt16BE());
                        var hci = reader.ReadInt16BE();
                        var ssi = reader.ReadInt16BE();
                        var di = reader.ReadInt16BE();
                        var lrc = reader.ReadInt16BE();
                        var sdi = reader.ReadInt16BE();
                        var fcr = reader.ReadInt16BE();
                        var fc = reader.ReadInt16BE();
                        var lmc = reader.ReadInt16BE();
                    }
                }
            };


            var p_0x16_0x17 = (byte id, Span<byte> buffer) => {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var count = reader.ReadUInt16BE();

                for (var i = 0; i < count; ++i)
                {
                    var type = reader.ReadUInt16BE();
                    var enabled = reader.ReadBool();
                }
            };

            // healthbar update
            packetsMap.Value[0x16] = buffer => p_0x16_0x17(0x16, buffer);
            packetsMap.Value[0x17] = buffer => p_0x16_0x17(0x17, buffer);

            // delete object
            packetsMap.Value[0x1D] = buffer => {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
            };

            // update player
            packetsMap.Value[0x20] = buffer => {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var graphicInc = reader.ReadUInt8();
                var hue = reader.ReadUInt16BE();
                var flags = (Flags)reader.ReadUInt8();
                (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                var serverId = reader.ReadUInt16BE();
                var direction = (Direction)reader.ReadUInt8();
                var z = reader.ReadInt8();
            };

            // deny walk
            packetsMap.Value[0x21] = buffer => {
                var reader = new StackDataReader(buffer);

                byte sequence = reader.ReadUInt8();
                (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                var direction = (Direction) reader.ReadUInt8();
                var z = reader.ReadInt8();
            };

            // confirm walk
            packetsMap.Value[0x22] = buffer => {
                var reader = new StackDataReader(buffer);

                var sequence =  reader.ReadUInt8();
                var notoriety = reader.ReadUInt8();
            };

            // drag animation
            packetsMap.Value[0x23] = buffer => {
                var reader = new StackDataReader(buffer);

                var graphic = reader.ReadUInt16BE();
                var graphicInc = reader.ReadUInt8();
                var hue = reader.ReadUInt16BE();
                var count = reader.ReadUInt16BE();
                var src = reader.ReadUInt32BE();
                (var srcX, var srcY, var srcZ) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt8());
                var dst = reader.ReadUInt32BE();
                (var dstX, var dstY, var dstZ) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt8());
            };

            // open container
            packetsMap.Value[0x24] = buffer => {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
            };

            // update container
            packetsMap.Value[0x25] = buffer => {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var graphicInc = reader.ReadInt8();
                var amount = Math.Max((ushort) 1, reader.ReadUInt16BE());
                (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_6017)
                    reader.Skip(1);

                var containerSerial = reader.ReadUInt32BE();
                var hue = reader.ReadInt16BE();
            };

            // deny move item
            packetsMap.Value[0x27] = buffer => {
                var reader = new StackDataReader(buffer);

                var code = reader.ReadUInt8();
            };

            // end draggin item
            packetsMap.Value[0x28] = buffer => {
            };

            // drpp item ok
            packetsMap.Value[0x29] = buffer => {
            };

            // show death screen
            packetsMap.Value[0x2C] = buffer => {
                var reader = new StackDataReader(buffer);
                var action = reader.ReadUInt8();
            };

            // mobile attributes
            packetsMap.Value[0x2D] = buffer => {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                (var hitsMax, var hits) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                (var manaMax, var mana) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                (var stamMax, var stam) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
            };

            // equip item
            packetsMap.Value[0x2E] = buffer => {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32LE();
                var graphic = reader.ReadUInt16BE();
                var graphicInc = reader.ReadUInt8();
                var container = reader.ReadUInt32BE();
                var hue = reader.ReadUInt16BE();
            };

            // swing
            packetsMap.Value[0x2F] = buffer => {
                var reader = new StackDataReader(buffer);

                reader.Skip(1);
                var opponent = reader.ReadUInt32BE();
                var defender = reader.ReadUInt32BE();
            };

            // update skills
            packetsMap.Value[0x3A] = buffer => {
                var reader = new StackDataReader(buffer);

                var type = reader.ReadUInt8();
                if (type == 0xFE)
                {
                    var count = reader.ReadUInt16BE();
                    for (var i = 0; i < count; ++i)
                    {
                        var haveButton = reader.ReadBool();
                        var nameLen = reader.ReadInt8();
                        var skillName = reader.ReadASCII(nameLen);
                    }
                }
                else
                {
                    var haveCap = type != 0u && type <= 0x03 || type == 0xDF;
                    var singleUpdate = type == 0xFF || type == 0xDF;

                    while (reader.Remaining > 0)
                    {
                        var id = reader.ReadInt16BE();
                        (var real, var @base) = (reader.ReadInt16BE(), reader.ReadInt16BE());
                        var status = (Lock)reader.ReadUInt8();

                        if (haveCap)
                        {
                            var cap = reader.ReadInt16BE();
                        }

                        if (singleUpdate)
                            break;
                    }
                }
            };

            // pathfinding
            packetsMap.Value[0x38] = buffer => {
                var reader = new StackDataReader(buffer);
                var x = reader.ReadUInt16BE();
                var y = reader.ReadUInt16BE();
                var z = reader.ReadUInt16BE();
            };

            // update contained items
            packetsMap.Value[0x3C] = buffer => {
                var reader = new StackDataReader(buffer);

                var count = reader.ReadUInt16BE();

                for (var i = 0; i < count; ++i)
                {
                    var serial = reader.ReadUInt32BE();
                    var graphic = reader.ReadUInt16BE();
                    var graphicInc = reader.ReadUInt8();
                    var amount = Math.Max((ushort) 1, reader.ReadUInt16BE());
                    (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

                    if (gameCtx.Value.ClientVersion >= ClientVersion.CV_6017)
                        reader.Skip(1);

                    var containerSerial = reader.ReadUInt32BE();
                    var hue = reader.ReadUInt16BE();
                }
            };
        }, Stages.Startup);
    }
}