using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using TinyEcs;

namespace ClassicUO.Ecs;

using static TinyEcs.Defaults;
using PacketsMap = Dictionary<byte, OnPacket>;

sealed class NetworkEntitiesMap
{
    private readonly Dictionary<uint, ulong> _entities = new ();

    public EntityView GetOrCreate(TinyEcs.World world, uint serial)
    {
        if (_entities.TryGetValue(serial, out var id))
        {
            if (world.Exists(id))
            {
                return world.Entity(id);
            }

            _entities.Remove(serial);
        }

        var ent = world.Entity()
            .Set(new NetworkSerial() { Value = serial });

        if (SerialHelper.IsMobile(serial))
        {
            ent.Set(new MobAnimation());
            ent.Set(new ScreenPositionOffset());
        }
        _entities.Add(serial, ent.ID);

        Console.WriteLine("created: serial: 0x{0:X8} | ecsId: {1}", serial, ent.ID);

        return ent;
    }

    public bool Remove(TinyEcs.World world, uint serial)
    {
        var result = false;
        if (_entities.Remove(serial, out var id))
        {
            if (!world.Exists(id))
                return false;

            // Some entities might have a network entity associated [child].
            // It's needed to remove from the dict these children entities.
            // Filter search:
            // - (*, id) && (NetworkSerial)
            // - (id, *) && (NetworkSerial)
            // Suddenly the world.Delete(id) call will delete the children ecs side.
            var term0 = new QueryTerm(IDOp.Pair(Wildcard.ID, id), TermOp.With);
            var term1 = new QueryTerm(IDOp.Pair(id, Wildcard.ID), TermOp.With);
            var term2 = new QueryTerm(world.Entity<NetworkSerial>(), TermOp.DataAccess);
            // var term3 = new QueryTerm(IDOp.Pair(world.Entity<EquippedItem>(), id), TermOp.Without);

            world.BeginDeferred();
            var iterator = world.GetQueryIterator([term0, term2]);
            while (iterator.Next(out var arch))
            {
                var index = arch.GetComponentIndex<NetworkSerial>();
                foreach (ref readonly var chunk in arch)
                {
                    var span = chunk.GetSpan<NetworkSerial>(index);
                    foreach (ref var ser in span)
                    {
                        Console.WriteLine("  removing serial: 0x{0:X8} associated to 0x{1:X8}", ser.Value, serial);
                        if (!Remove(world, ser.Value))
                        {

                        }
                    }
                }
            }

            iterator = world.GetQueryIterator([term1, term2]);
            while (iterator.Next(out var arch))
            {
                var index = arch.GetComponentIndex<NetworkSerial>();
                foreach (ref readonly var chunk in arch)
                {
                    var span = chunk.GetSpan<NetworkSerial>(index);
                    foreach (ref var ser in span)
                    {
                        Console.WriteLine("  removing serial: 0x{0:X8} associated to 0x{1:X8}", ser.Value, serial);
                        if (!Remove(world, ser.Value))
                        {

                        }
                    }
                }
            }
            world.EndDeferred();

            // // we want to keep the equipments for some reason lol
            // var term4 = new QueryTerm(IDOp.Pair(world.Entity<EquippedItem>(), id), TermOp.With);
            // var id2 = id;
            // var q2 = world.QueryRaw(term2, term4);
            // q2.Each((EntityView ent, ref NetworkSerial ser) =>
            // {
            //     Console.WriteLine("  unsetting equipment serial: 0x{0:X8} associated to 0x{1:X8}", ser.Value, serial);
            //     //ent.Unset<EquippedItem>(id2);
            //     Remove(world, ser.Value);
            // });

            world.Delete(id);
            result = true;
        }

        Console.WriteLine("deleted: serial: 0x{0:X8} | ecsId: {1} | result: {2}", serial, id, result);

        return result;
    }
}

readonly struct InGamePacketsPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new NetworkEntitiesMap());

        scheduler.AddSystem((
            Res<NetworkEntitiesMap> entitiesMap,
            Res<Settings> settings,
            Res<PacketsMap> packetsMap,
            Res<NetClient> network,
            Res<UOFileManager> fileManager,
            Res<GameContext> gameCtx,
            EventWriter<AcceptedStep> acceptedSteps,
            EventWriter<RejectedStep> rejectedSteps,
            EventWriter<MobileQueuedStep> mobileQueuedSteps,
            TinyEcs.World world
        ) =>
        {
            // enter world
            packetsMap.Value[0x1B] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                reader.Skip(4);
                var graphic = reader.ReadUInt16BE();
                (var x, var y, var z) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), (sbyte)reader.ReadUInt16BE());
                var dir = (Direction)reader.ReadUInt8();
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

                var ent = entitiesMap.Value.GetOrCreate(world, serial);
                ent.Set(new Ecs.WorldPosition() { X = x, Y = y, Z = z })
                    .Set(new Ecs.Graphic() { Value = graphic })
                    .Set(new Facing() { Value = dir })
                    .Set(new MobileSteps())
                    .Add<Player>();
            };

            // login complete
            packetsMap.Value[0x55] = buffer =>
            {
                if (gameCtx.Value.PlayerSerial == 0)
                    return;

                network.Value.Send_StatusRequest(gameCtx.Value.PlayerSerial);
                network.Value.Send_OpenChat("");

                network.Value.Send_SkillsRequest(gameCtx.Value.PlayerSerial);
                //network.Value.Send_DoubleClick(gameCtx.Value.PlayerSerial);

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_306E)
                    network.Value.Send_ClientType(gameCtx.Value.Protocol);

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_305D)
                    network.Value.Send_ClientViewRange(24);
            };

            // extended commands
            packetsMap.Value[0xBF] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var cmd = reader.ReadUInt16BE();

                switch (cmd)
                {
                    default:
                        break;

                    // fast walk
                    case 1:
                        var key0 = reader.ReadUInt32BE();
                        var key1 = reader.ReadUInt32BE();
                        var key2 = reader.ReadUInt32BE();
                        var key3 = reader.ReadUInt32BE();
                        var key4 = reader.ReadUInt32BE();
                        var key5 = reader.ReadUInt32BE();
                        break;

                    // fast walk
                    case 2:
                        var newKey = reader.ReadUInt32BE();
                        break;

                    // close generic gump
                    case 4:
                        var gumpSerial = reader.ReadUInt32BE();
                        var button = reader.ReadInt32BE();
                        break;

                    // party
                    case 6:
                        break;

                    // map change
                    case 8:
                        var mapIndex = reader.ReadUInt8();
                        fileManager.Value.Maps.LoadMap(mapIndex);

                        if (gameCtx.Value.Map != mapIndex)
                        {
                            gameCtx.Value.Map = mapIndex;
                        }
                        break;

                    // close statusbar
                    case 0x0C:
                        var healthBarSerial = reader.ReadUInt32BE();
                        break;

                    // display equip info
                    case 0x10:
                        var itemSerial = reader.ReadUInt32BE();
                        var cliloc = reader.ReadUInt32BE();
                        var sentinel = reader.ReadUInt32BE();
                        if (sentinel == 0xFFFFFFFD)
                        {

                        }

                        var ownerNameLen = reader.ReadUInt16BE();
                        var ownerName = reader.ReadASCII(ownerNameLen);
                        sentinel = reader.ReadUInt32BE();
                        if (sentinel == 0xFFFFFFFC)
                        {

                        }

                        while (reader.Remaining > 0)
                        {
                            var num = reader.ReadUInt32BE();
                            if (num == 0xFFFF_FFFF)
                                break;
                            var charges = reader.ReadInt16BE();
                        }
                        break;

                    // show ctx menu
                    case 0x14:
                        break;

                    // close local gump:
                    case 0x16:
                        var type = reader.ReadUInt32BE();
                        gumpSerial = reader.ReadUInt32BE();
                        break;

                    // map patches
                    case 0x18:
                        break;

                    // stats
                    case 0x19:
                        var version = reader.ReadUInt8();
                        var serial = reader.ReadUInt32BE();
                        break;

                    // spellbook content
                    case 0x1B:
                        reader.Skip(sizeof(ushort));
                        var spellBookSerial = reader.ReadUInt32BE();
                        var spellBookGraphic = reader.ReadUInt16BE();
                        type = reader.ReadUInt16BE();

                        for (var i = 0; i < 2; ++i)
                        {
                            var spells = 0u;
                            for (var j = 0; j < 4; j++)
                                spells |= (uint)(reader.ReadUInt8() << (i * 8));

                            for (var j = 0; j < 32; j++)
                            {
                                if ((spellBookGraphic & (1 << j)) != 0)
                                {

                                }
                            }
                        }
                        break;

                    // house revision
                    case 0x1D:
                        serial = reader.ReadUInt32BE();
                        var revision = reader.ReadUInt32BE();
                        break;

                    // house customization menu
                    case 0x20:
                        serial = reader.ReadUInt32BE();
                        type = reader.ReadUInt8();
                        var graphic = reader.ReadUInt16BE();
                        (var x, var y, var z) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt8());
                        break;

                    // reset abilities
                    case 0x21:
                        break;

                    // damage
                    case 0x22:
                        reader.Skip(sizeof(byte));
                        serial = reader.ReadUInt32BE();
                        var damage = reader.ReadUInt8();
                        break;

                    // spellbook icon on/off
                    case 0x25:
                        var spell = reader.ReadUInt16BE();
                        var isActive = reader.ReadBool();
                        break;

                    // player speed mode
                    case 0x26:
                        var speedMode = (CharacterSpeedType)reader.ReadUInt8();
                        if (speedMode > CharacterSpeedType.FastUnmountAndCantRun)
                            speedMode = 0;

                        break;

                    // change race
                    case 0x2A:
                        var isFemale = reader.ReadBool();
                        var race = (RaceType)reader.ReadUInt8();
                        break;

                    // statue animation
                    case 0x2B:
                        serial = reader.ReadUInt16BE();
                        var animId = reader.ReadUInt8();
                        var frameCount = reader.ReadUInt8();
                        break;
                }
            };

            // client version
            packetsMap.Value[0xBD] = buffer => network.Value.Send_ClientVersion(settings.Value.ClientVersion);

            // unicode speech
            packetsMap.Value[0xAE] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var msgType = (MessageType)reader.ReadUInt8();
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
                    Console.WriteLine("{0} says: '{1}'", name, text);
                }
            };

            // update object
            var d3_78 = (byte id, ReadOnlySpan<byte> buffer) =>
            {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                (var x, var y, var z) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt8());
                var dir = (Direction)reader.ReadUInt8();
                var hue = reader.ReadUInt16BE();
                var flags = (Flags)reader.ReadUInt8();
                var notoriety = (NotorietyFlag)reader.ReadUInt8();

                if (id == 0xD3)
                    reader.Skip(sizeof(ushort) * 3);

                var parentEnt = entitiesMap.Value.GetOrCreate(world, serial);
                parentEnt
                    .Set(new Graphic() { Value = graphic })
                    // .Set(new WorldPosition() { X = x, Y = y, Z = z })
                    .Set(new Hue() { Value = hue });
                    //.Set(new Facing() { Value = dir });

                var slots = parentEnt.Has<EquipmentSlots>() ? parentEnt.Get<EquipmentSlots>() : new EquipmentSlots();

                uint itemSerial;
                while ((itemSerial = reader.ReadUInt32BE()) != 0)
                {
                    var itemGraphic = reader.ReadUInt16BE();
                    var layer = (Layer)reader.ReadUInt8();
                    ushort itemHue = 0;

                    if (gameCtx.Value.ClientVersion >= ClientVersion.CV_70331)
                        itemHue = reader.ReadUInt16BE();
                    else if ((itemGraphic & 0x8000) != 0)
                    {
                        itemGraphic &= 0x7FFF;
                        itemHue = reader.ReadUInt16BE();
                    }

                    var child = entitiesMap.Value.GetOrCreate(world, itemSerial);
                    child.Set(new Graphic() { Value = itemGraphic })
                        .Set(new Hue() { Value = itemHue });
                    // child.Set(new EquippedItem() { Layer = layer }, parentEnt);

                    slots[layer] = child;

                    Console.WriteLine("equip serial 0x{0:X8} | parentId: {1}", itemSerial, parentEnt.ID);
                }

                parentEnt.Set(slots);


                mobileQueuedSteps.Enqueue(new ()
                {
                    Serial = serial,
                    X = x,
                    Y = y,
                    Z = z,
                    Direction = dir
                });
            };
            packetsMap.Value[0xD3] = buffer => d3_78(0xD3, buffer);
            packetsMap.Value[0x78] = buffer => d3_78(0x78, buffer);

            // view range
            packetsMap.Value[0xC8] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var range = reader.ReadUInt8();
            };

            // update item
            packetsMap.Value[0x1A] = buffer =>
            {
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

                var ent = entitiesMap.Value.GetOrCreate(world, serial);
                ent.Set(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                    .Set(new Hue() { Value = hue });
                    // .Set(new WorldPosition() { X = x, Y = y, Z = z })
                    //.Set(new Facing() { Value = (Direction)direction });

                mobileQueuedSteps.Enqueue(new ()
                {
                    Serial = serial,
                    X = x,
                    Y = y,
                    Z = z,
                    Direction = (Direction) direction,
                });
            };

            // damage
            packetsMap.Value[0x0B] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                var damage = reader.ReadUInt16BE();
            };

            // character status
            packetsMap.Value[0x11] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                var name = reader.ReadASCII(30);
                (var hits, var hitsMax) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                var canBeRenamed = reader.ReadBool();
                var type = reader.ReadUInt8();

                var ent = entitiesMap.Value.GetOrCreate(world, serial);
                ent.Set(new Hitpoints() { Value = hits, MaxValue = hitsMax });

                if (type > 0)
                {
                    var isFemale = reader.ReadBool();
                    (var str, var dex, var intell) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadUInt16BE());
                    (var stam, var stamMax) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                    (var mana, var manaMax) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                    var gold = reader.ReadUInt32BE();
                    var physicalRes = reader.ReadInt16BE();
                    var weigth = reader.ReadUInt16BE();

                    ent.Set(new Stamina() { Value = stam, MaxValue = stamMax });
                    ent.Set(new Mana() { Value = mana, MaxValue = manaMax });


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

            // healthbar update
            var p_0x16_0x17 = (byte id, ReadOnlySpan<byte> buffer) =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var count = reader.ReadUInt16BE();

                for (var i = 0; i < count; ++i)
                {
                    var type = reader.ReadUInt16BE();
                    var enabled = reader.ReadBool();
                }
            };
            packetsMap.Value[0x16] = buffer => p_0x16_0x17(0x16, buffer);
            packetsMap.Value[0x17] = buffer => p_0x16_0x17(0x17, buffer);

            // delete object
            packetsMap.Value[0x1D] = buffer =>
            {
                if (gameCtx.Value.PlayerSerial == 0) return;
                var reader = new StackDataReader(buffer);
                var serial = reader.ReadUInt32BE();
                var deleted = entitiesMap.Value.Remove(world, serial);
            };

            // update player
            packetsMap.Value[0x20] = buffer =>
            {
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

                var ent = entitiesMap.Value.GetOrCreate(world, serial);
                ent.Set(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                    .Set(new Hue() { Value = hue });
                    //.Set(new WorldPosition() { X = x, Y = y, Z = z })
                    //.Set(new Facing() { Value = direction });

                mobileQueuedSteps.Enqueue(new ()
                {
                    Serial = serial,
                    X = x,
                    Y = y,
                    Z = z,
                    Direction = direction
                });
            };

            // deny walk
            packetsMap.Value[0x21] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                byte sequence = reader.ReadUInt8();
                (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                var direction = (Direction)reader.ReadUInt8();
                var z = reader.ReadInt8();

                rejectedSteps.Enqueue(new()
                {
                    Sequence = sequence,
                    Direction = direction,
                    X = x, Y = y, Z = z
                });
            };

            // confirm walk
            packetsMap.Value[0x22] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var sequence = reader.ReadUInt8();
                var notoriety = (NotorietyFlag) reader.ReadUInt8();

                acceptedSteps.Enqueue(new()
                {
                    Sequence = sequence,
                    Notoriety = notoriety
                });
            };

            // drag animation
            packetsMap.Value[0x23] = buffer =>
            {
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
            packetsMap.Value[0x24] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
            };

            // update container
            packetsMap.Value[0x25] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var graphicInc = reader.ReadInt8();
                var amount = Math.Max((ushort)1, reader.ReadUInt16BE());
                (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_6017)
                    reader.Skip(1);

                var containerSerial = reader.ReadUInt32BE();
                var hue = reader.ReadUInt16BE();

                var ent = entitiesMap.Value.GetOrCreate(world, serial);
                var parentEnt = entitiesMap.Value.GetOrCreate(world, containerSerial);

                ent.Set(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                    .Set(new WorldPosition() { X = x, Y = y, Z = 0 })
                    .Set(new Hue() { Value = hue })
                    .Set(new Amount() { Value = amount })
                    .Add<ContainedInto>(parentEnt);
            };

            // deny move item
            packetsMap.Value[0x27] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var code = reader.ReadUInt8();
            };

            // end draggin item
            packetsMap.Value[0x28] = buffer =>
            {
            };

            // drpp item ok
            packetsMap.Value[0x29] = buffer =>
            {
            };

            // show death screen
            packetsMap.Value[0x2C] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var action = reader.ReadUInt8();
            };

            // mobile attributes
            packetsMap.Value[0x2D] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                (var hitsMax, var hits) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                (var manaMax, var mana) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                (var stamMax, var stam) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

                entitiesMap.Value.GetOrCreate(world, serial)
                    .Set(new Hitpoints() { Value = hits, MaxValue = hitsMax })
                    .Set(new Mana() { Value = mana, MaxValue = manaMax })
                    .Set(new Stamina() { Value = stam, MaxValue = stamMax });
            };

            // equip item
            packetsMap.Value[0x2E] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var graphicInc = reader.ReadUInt8();
                var layer = (Layer)reader.ReadUInt8();
                var container = reader.ReadUInt32BE();
                var hue = reader.ReadUInt16BE();

                var parentEnt = entitiesMap.Value.GetOrCreate(world, container);
                var childEnt = entitiesMap.Value.GetOrCreate(world, serial);
                childEnt.Set(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                    .Set(new Hue() { Value = hue });
                // childEnt.Set(new EquippedItem() { Layer = layer }, parentEnt);

                var slots = parentEnt.Has<EquipmentSlots>() ? parentEnt.Get<EquipmentSlots>() : new EquipmentSlots();
                slots[layer] = childEnt;
                parentEnt.Set(slots);
                Console.WriteLine("equip serial 0x{0:X8} | parentId: {1}", serial, parentEnt.ID);
                //.Set<ContainedInto>(parentEnt);
            };

            // swing
            packetsMap.Value[0x2F] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                reader.Skip(1);
                var opponent = reader.ReadUInt32BE();
                var defender = reader.ReadUInt32BE();
            };

            // update skills
            packetsMap.Value[0x3A] = buffer =>
            {
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
            packetsMap.Value[0x38] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                (var x, var y, var z) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadUInt16BE());
            };

            // update contained items
            packetsMap.Value[0x3C] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var count = reader.ReadUInt16BE();

                for (var i = 0; i < count; ++i)
                {
                    var serial = reader.ReadUInt32BE();
                    var graphic = reader.ReadUInt16BE();
                    var graphicInc = reader.ReadUInt8();
                    var amount = Math.Max((ushort)1, reader.ReadUInt16BE());
                    (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                    var gridIdx = gameCtx.Value.ClientVersion < ClientVersion.CV_6017 ?
                         0 : reader.ReadUInt8();
                    var containerSerial = reader.ReadUInt32BE();
                    var hue = reader.ReadUInt16BE();

                    var parentEnt = entitiesMap.Value.GetOrCreate(world, containerSerial);
                    var childEnt = entitiesMap.Value.GetOrCreate(world, serial);
                    childEnt.Set(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                        .Set(new Hue() { Value = hue })
                        .Set(new WorldPosition() { X = x, Y = y, Z = (sbyte)gridIdx })
                        .Set(new Amount() { Value = amount })
                        .Add<ContainedInto>(parentEnt);
                }
            };

            // player light level
            packetsMap.Value[0x4E] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var level = reader.ReadUInt8();
            };

            // server light level
            packetsMap.Value[0x4F] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var level = reader.ReadUInt8();
            };

            // sound effect
            packetsMap.Value[0x54] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                reader.Skip(1);
                var index = reader.ReadUInt16BE();
                var audio = reader.ReadUInt16BE();
                (var x, var y, var z) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt16BE());
            };

            // music
            packetsMap.Value[0x6D] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var index = reader.ReadUInt16BE();
            };

            // map data
            packetsMap.Value[0x56] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var mapMsgType = (MapMessageType)reader.ReadUInt8();
                var plotEnabled = reader.ReadBool();
                (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
            };

            // weather
            packetsMap.Value[0x65] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var weatherType = (WeatherType)reader.ReadUInt8();
                var count = reader.ReadUInt8();
                var temp = reader.ReadUInt8();
            };

            // books
            packetsMap.Value[0x66] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var pageCount = reader.ReadUInt16BE();

                for (var i = 0; i < pageCount; ++i)
                {
                    var pageNum = reader.ReadUInt16BE();
                    var linesCount = reader.ReadUInt16BE();

                    for (var line = 0; line < linesCount; ++line)
                    {
                        // TODO: check if book has been open using 0xD4 or 0x93
                        var lineText = reader.ReadASCII();
                    }
                }
            };

            // character animation
            packetsMap.Value[0x6E] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var action = reader.ReadUInt16BE();
                var frameCount = reader.ReadUInt16BE();
                var repeatForNTimes = reader.ReadUInt16BE();
                var backward = reader.ReadBool();
                var loop = reader.ReadBool();
                var delay = reader.ReadUInt8();

                // var ent = entitiesMap.Value.GetOrCreate(world, serial);
                // var index = ClassicUO.Game.GameObjects.Mobile.GetReplacedObjectAnimation(ent.Get<Graphic>().Value, action);

                // ent.Set(new MobAnimation()
                //     {
                //         Index = index,
                //         FramesCount = frameCount,
                //         Interval = delay,
                //         RepeatMode = repeatForNTimes,
                //         RepeatModeCount = repeatForNTimes,
                //         IsFromServer = true,
                //         ForwardDirection = !backward,
                //         Run = true
                //     });
            };

            // graphical effects
            var c0_c7_70 = (byte id, ReadOnlySpan<byte> buffer) =>
            {
                var reader = new StackDataReader(buffer);

                var effectType = (GraphicEffectType)reader.ReadUInt8();

                if (id == 0x70)
                {
                    reader.Skip(8);
                    var val = reader.ReadUInt16BE();
                }

                var src = reader.ReadUInt32BE();
                var target = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                (var srcX, var srcY, var srcZ) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt8());
                (var targetX, var targetY, var targetZ) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt8());
                var speed = reader.ReadUInt8();
                var duration = reader.ReadUInt8();
                var unk = reader.ReadUInt16BE();
                var fixedDir = reader.ReadBool();
                var willExplode = reader.ReadBool();
                if (id != 0x70)
                {
                    var hue = reader.ReadUInt32BE();
                    var blendMode = (GraphicEffectBlendMode)reader.ReadUInt32BE();

                    if (id == 0xC7)
                    {
                        var tileId = reader.ReadUInt16BE();
                        var explodeEffect = reader.ReadUInt16BE();
                        var explodeSound = reader.ReadUInt16BE();
                        var serial = reader.ReadUInt32BE();
                        var layer = reader.ReadUInt8();
                        reader.Skip(2);
                    }
                }
            };
            packetsMap.Value[0x70] = buffer => c0_c7_70(0x70, buffer);
            packetsMap.Value[0xC0] = buffer => c0_c7_70(0xC0, buffer);
            packetsMap.Value[0xC7] = buffer => c0_c7_70(0xC7, buffer);

            // bulleting board
            packetsMap.Value[0x71] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var type = reader.ReadUInt8();
                var boardSerial = reader.ReadUInt32BE();

                switch (type)
                {
                    case 0:
                        break;
                    case 1:
                        {
                            var serial = reader.ReadUInt32BE();
                            var parentId = reader.ReadUInt32BE();
                            var len = reader.ReadUInt8();
                            var text = reader.ReadASCII(len, true);
                            len = reader.ReadUInt8();
                            text += " " + reader.ReadASCII(len, true);
                            len = reader.ReadUInt8();
                            text += " " + reader.ReadASCII(len, true);
                        }
                        break;
                    case 2:
                        {
                            var len = reader.ReadUInt8();
                            var author = reader.ReadASCII(len, true);
                            len = reader.ReadUInt8();
                            var subject = reader.ReadASCII(len, true);
                            len = reader.ReadUInt8();
                            var dateTime = reader.ReadASCII(len, true);

                            reader.Skip(4);
                            var unk = reader.ReadUInt8();
                            if (unk > 0)
                                reader.Skip(unk * 4);
                            var lines = reader.ReadUInt8();
                            for (var i = 0; i < lines; ++i)
                            {
                                var lineLen = reader.ReadUInt8();
                                var text = reader.ReadASCII(lineLen, true);
                            }
                        }
                        break;
                }
            };

            // warmode
            packetsMap.Value[0x72] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var warmodeEnabled = reader.ReadBool();
            };

            // ping
            packetsMap.Value[0x73] = buffer =>
            {
                var reader = new StackDataReader(buffer);
                var sequence = reader.ReadUInt8();
            };

            // buy list
            packetsMap.Value[0x74] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var container = reader.ReadUInt32BE();
                var count = reader.ReadUInt8();

                for (var i = 0; i < count; ++i)
                {
                    var price = reader.ReadUInt32BE();
                    var nameLen = reader.ReadUInt8();
                    var name = reader.ReadASCII(nameLen);
                }
            };

            // update character
            var d2_77 = (byte id, ReadOnlySpan<byte> buffer) =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                (var x, var y, var z) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt8());
                var direction = (Direction)reader.ReadUInt8();
                var hue = reader.ReadUInt16BE();
                var flags = (Flags)reader.ReadUInt8();
                var notoriety = (NotorietyFlag)reader.ReadUInt8();

                var ent = entitiesMap.Value.GetOrCreate(world, serial);
                ent.Set(new Graphic() { Value = graphic })
                    .Set(new Hue() { Value = hue });
                    // .Set(new WorldPosition() { X = x, Y = y, Z = z })
                    //.Set(new Facing() { Value = direction });

                mobileQueuedSteps.Enqueue(new ()
                {
                    Serial = serial,
                    X = x,
                    Y = y,
                    Z = z,
                    Direction = direction
                });
            };
            packetsMap.Value[0x77] = buffer => d2_77(0x77, buffer);
            packetsMap.Value[0xD2] = buffer => d2_77(0xD2, buffer);

            // open menu
            packetsMap.Value[0x7C] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();

                var id = reader.ReadUInt16BE();
                var nameLen = reader.ReadUInt8();
                var name = reader.ReadASCII(nameLen);
                var count = reader.ReadUInt8();

                for (var i = 0; i < count; ++i)
                {
                    var menuId = reader.ReadUInt16BE();
                    var hue = reader.ReadUInt16BE();
                    var responseLen = reader.ReadUInt8();
                    var response = reader.ReadASCII(responseLen);
                }
            };

            // open paperdoll
            packetsMap.Value[0x88] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var title = reader.ReadASCII(60);
                var flags = reader.ReadUInt8();
            };

            // corpse equipment
            packetsMap.Value[0x89] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();

                var parentEnt = entitiesMap.Value.GetOrCreate(world, serial);

                var slots = parentEnt.Has<EquipmentSlots>() ? parentEnt.Get<EquipmentSlots>() : new EquipmentSlots();

                var layer = Layer.Invalid;
                uint itemSerial = 0;
                while ((layer = (Layer)reader.ReadUInt8()) != Layer.Invalid &&
                     (itemSerial = reader.ReadUInt32BE()) != 0)
                {
                    var childEnt = entitiesMap.Value.GetOrCreate(world, itemSerial);
                    // childEnt.Set(new EquippedItem() { Layer = layer }, parentEnt);

                    slots[layer] = childEnt;
                }

                parentEnt.Set(slots);
            };

            // show map
            var f5_90 = (byte id, ReadOnlySpan<byte> buffer) =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var gumpId = reader.ReadUInt16BE();
                (var startX, var startY) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                (var endX, var endY) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                (var width, var height) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

                if (id == 0xF5 || gameCtx.Value.ClientVersion >= ClientVersion.CV_308Z)
                {
                    var facet = id == 0xF5 ? reader.ReadUInt16BE() : 0;
                }
            };
            packetsMap.Value[0x90] = buffer => f5_90(0x90, buffer);
            packetsMap.Value[0xF5] = buffer => f5_90(0xF5, buffer);

            // open book
            var d4_93 = (byte id, ReadOnlySpan<byte> buffer) =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var isOldPacket = id == 0x93;
                var isEditable = reader.ReadBool();

                if (isOldPacket)
                {
                    reader.Skip(1);
                }
                else
                {
                    isEditable = reader.ReadBool();
                }

                var pageCount = reader.ReadUInt16BE();
                var title = isOldPacket ? reader.ReadASCII(60, true) :
                    reader.ReadASCII(reader.ReadUInt16BE(), true);
                var author = isOldPacket ? reader.ReadASCII(30, true) :
                    reader.ReadASCII(reader.ReadUInt16BE(), true);
            };
            packetsMap.Value[0x93] = buffer => d4_93(0x93, buffer);
            packetsMap.Value[0xD4] = buffer => d4_93(0xD4, buffer);

            // color picker
            packetsMap.Value[0x95] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                reader.Skip(2);
                var graphic = reader.ReadUInt16BE();
            };

            // move player
            packetsMap.Value[0x97] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var direction = (Direction)reader.ReadUInt8();
            };

            // update name
            packetsMap.Value[0x98] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var name = reader.ReadASCII();
            };

            // place multi
            packetsMap.Value[0x99] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var onGround = reader.ReadBool();
                var targetId = reader.ReadUInt32BE();
                var flags = reader.ReadUInt8();
                reader.Skip(18);
                var multiId = reader.ReadUInt16BE();
                (var offX, var offY, var offZ) = (reader.ReadInt16BE(), reader.ReadInt16BE(), reader.ReadInt16BE());
                var hue = reader.ReadUInt16BE();
            };

            // ascii prompt
            packetsMap.Value[0x9A] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var promptId = reader.ReadUInt32BE();
                var type = reader.ReadUInt32BE();
                var text = reader.ReadASCII();
            };

            // sell list
            packetsMap.Value[0x9E] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var count = reader.ReadUInt16BE();

                for (var i = 0; i < count; ++i)
                {
                    var itemSerial = reader.ReadUInt32BE();
                    var graphic = reader.ReadUInt16BE();
                    var hue = reader.ReadUInt16BE();
                    var amount = reader.ReadUInt16BE();
                    var price = reader.ReadUInt16BE();
                    var nameLen = reader.ReadUInt16BE();
                    var name = reader.ReadASCII(nameLen);
                }
            };

            // update hits
            packetsMap.Value[0xA1] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                (var hitsMax, var hits) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

                entitiesMap.Value.GetOrCreate(world, serial)
                    .Set(new Hitpoints() { Value = hits, MaxValue = hitsMax });
            };

            // update mana
            packetsMap.Value[0xA2] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                (var manaMax, var mana) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

                entitiesMap.Value.GetOrCreate(world, serial)
                    .Set(new Mana() { Value = mana, MaxValue = manaMax });
            };

            // update stam
            packetsMap.Value[0xA3] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                (var stamMax, var stam) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

                entitiesMap.Value.GetOrCreate(world, serial)
                    .Set(new Stamina() { Value = stam, MaxValue = stamMax });
            };

            // open url
            packetsMap.Value[0xA5] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var url = reader.ReadASCII();
            };

            // window tip
            packetsMap.Value[0xA6] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var flags = reader.ReadUInt8();
                var serial = reader.ReadUInt32BE();
                var textLen = reader.ReadUInt16BE();
                var text = reader.ReadASCII(textLen);
            };

            // attack entity
            packetsMap.Value[0xAA] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
            };

            // text entry dialog
            packetsMap.Value[0xAB] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var parentId = reader.ReadUInt8();
                var buttonId = reader.ReadUInt8();
                var textLen = reader.ReadUInt16BE();
                var text = reader.ReadASCII(textLen);
                var showCancel = reader.ReadBool();
                var variant = reader.ReadUInt8();
                var maxLength = reader.ReadUInt32BE();
                var descLen = reader.ReadUInt16BE();
                var desc = reader.ReadASCII(descLen);
            };

            // show death action
            packetsMap.Value[0xAF] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var corpseSerial = reader.ReadUInt32BE();
                var running = reader.ReadUInt32BE();
            };

            // open gump
            packetsMap.Value[0xB0] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var sender = reader.ReadUInt32BE();
                var gumpId = reader.ReadUInt32BE();
                (var x, var y) = (reader.ReadInt32BE(), reader.ReadInt32BE());
                var cmdLen = reader.ReadUInt16BE();
                var cmd = reader.ReadASCII(cmdLen);
                var linesCount = reader.ReadUInt16BE();

                for (var i = 0; i < linesCount; ++i)
                {
                    var length = reader.ReadUInt16BE();
                    var text = reader.ReadUnicodeBE(length);
                }
            };

            // chat message
            packetsMap.Value[0xB2] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var cmd = reader.ReadUInt16BE();
                // TODO
            };

            // open character profile
            packetsMap.Value[0xB8] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var header = reader.ReadASCII();
                var footer = reader.ReadUnicodeBE();
                var body = reader.ReadUnicodeBE();
            };

            // lock features
            packetsMap.Value[0xB9] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var flags = gameCtx.Value.ClientVersion >= ClientVersion.CV_60142 ?
                    (LockedFeatureFlags)reader.ReadUInt32BE() :
                    (LockedFeatureFlags)reader.ReadUInt16BE();

                BodyConvFlags bcFlags = 0;
                if (flags.HasFlag(LockedFeatureFlags.UOR))
                    bcFlags |= BodyConvFlags.Anim1 | BodyConvFlags.Anim2;
                if (flags.HasFlag(LockedFeatureFlags.LBR))
                    bcFlags |= BodyConvFlags.Anim1;
                if (flags.HasFlag(LockedFeatureFlags.AOS))
                    bcFlags |= BodyConvFlags.Anim2;
                if (flags.HasFlag(LockedFeatureFlags.SE))
                    bcFlags |= BodyConvFlags.Anim3;
                if (flags.HasFlag(LockedFeatureFlags.ML))
                    bcFlags |= BodyConvFlags.Anim4;

                fileManager.Value.Animations.ProcessBodyConvDef(bcFlags);
            };

            // show quest pointer
            packetsMap.Value[0xBA] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var display = reader.ReadBool();
                (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_7090)
                {
                    var serial = reader.ReadUInt32BE();
                }
            };

            // seasons
            packetsMap.Value[0xBC] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var season = reader.ReadUInt8();
                var music = reader.ReadUInt8();
            };

            // cliloc
            var c1_cc = (byte id, ReadOnlySpan<byte> buffer) =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var msgType = (MessageType)reader.ReadUInt8();
                var hue = reader.ReadUInt16BE();
                var font = reader.ReadUInt16BE();
                var cliloc = reader.ReadUInt32BE();
                var affixType = id == 0xCC ? (AffixType)reader.ReadUInt8() : 0;
                var name = reader.ReadASCII(30);
                var affix = id == 0xCC ? reader.ReadASCII() : string.Empty;
                var arguments = id == 0xCC ? reader.ReadUnicodeBE() : reader.ReadUnicodeLE(reader.Remaining / 2);
            };
            packetsMap.Value[0xC1] = buffer => c1_cc(0xC1, buffer);
            packetsMap.Value[0xCC] = buffer => c1_cc(0xCC, buffer);

            // unicode prompt
            packetsMap.Value[0xC2] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var messageId = reader.ReadUInt32BE();
                // TODO: more data
            };

            // logout request
            packetsMap.Value[0xD1] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var disconnect = reader.ReadBool();
            };

            // megacliloc
            packetsMap.Value[0xD6] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var unk = reader.ReadUInt16BE();
                var serial = reader.ReadUInt32BE();
                reader.Skip(2);
                var revision = reader.ReadUInt32BE();

                var cliloc = 0;
                while ((cliloc = reader.ReadInt32BE()) != 0)
                {
                    var len = reader.ReadUInt16BE();
                    var argument = len > 0 ? reader.ReadUnicodeLE(len / 2) : string.Empty;
                }
            };

            // custom house
            packetsMap.Value[0xD8] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var compressed = reader.ReadUInt8() == 0x03;
                var response = reader.ReadBool();
                var serial = reader.ReadUInt32BE();
                var revision = reader.ReadUInt32BE();
                reader.Skip(4);

                var planesCount = reader.ReadUInt8();

                for (var i = 0; i < planesCount; ++i)
                {
                    var header = reader.ReadUInt32BE();
                    // TODO: read the house data
                }
            };

            // opl info
            packetsMap.Value[0xDC] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var revision = reader.ReadUInt32BE();
            };

            // open compressed gump
            packetsMap.Value[0xDD] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var sender = reader.ReadUInt32BE();
                var gumpId = reader.ReadUInt32BE();
                (var x, var y) = (reader.ReadUInt32BE(), reader.ReadUInt32BE());
                var compressedLen = reader.ReadUInt32BE() - 4;
                var decLen = reader.ReadUInt32BE();

                reader.Skip((int)compressedLen);
                var linesCount = reader.ReadUInt32BE();

                if (linesCount > 0)
                {
                    compressedLen = reader.ReadUInt32BE() - 4;
                    decLen = reader.ReadUInt32BE();

                    reader.Skip((int)compressedLen);
                }
            };

            // update mobile status
            packetsMap.Value[0xDE] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var status = reader.ReadUInt8();
                if (status == 1)
                {
                    var opponent = reader.ReadUInt32BE();
                }
            };

            // buff debuff
            packetsMap.Value[0xDF] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var iconType = (BuffIconType)reader.ReadUInt16BE();
                var count = reader.ReadUInt16BE();
                if (count == 0)
                {
                    // TODO: remove
                }
                else
                {
                    for (var i = 0; i < count; ++i)
                    {
                        var srcType = reader.ReadUInt16BE();
                        reader.Skip(2);
                        var icon = reader.ReadUInt16BE();
                        var queueIdx = reader.ReadUInt16BE();
                        reader.Skip(4);
                        var timer = reader.ReadUInt16BE();
                        reader.Skip(3);

                        var titleCliloc = reader.ReadUInt32BE();
                        var descrCliloc = reader.ReadUInt32BE();
                        var wtfCliloc = reader.ReadUInt32BE();

                        var argsLen = reader.ReadUInt16BE();
                        var str = reader.ReadUnicodeLE(2);
                        var args = str + reader.ReadUnicodeLE();

                        argsLen = reader.ReadUInt16BE();
                        var args2 = reader.ReadUInt16LE();

                        argsLen = reader.ReadUInt16BE();
                        var args3 = reader.ReadUInt16LE();
                    }
                }
            };

            // new character anim
            packetsMap.Value[0xE2] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var type = reader.ReadUInt16BE();
                var action = reader.ReadUInt16BE();
                var mode = reader.ReadUInt8();
                // TODO
                // var group = ClassicUO.Game.GameObjects.Mobile.GetObjectNewAnimation()

                // entitiesMap.Value.GetOrCreate(world, serial)
                //     .Set(new MobAnimation()
                //     {
                //         Index = 1,

                //     });
            };

            // add waypoint
            packetsMap.Value[0xE5] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                (var x, var y, var z) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt8());
                var map = reader.ReadUInt8();
                var waypointType = (WaypointsType)reader.ReadUInt16BE();
                var ignoreObject = reader.ReadUInt16BE() != 0;
                var cliloc = reader.ReadUInt32BE();
                var name = reader.ReadUnicodeLE();
            };

            // remove waypoint
            packetsMap.Value[0xE6] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
            };

            // krrios client
            packetsMap.Value[0xF0] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var type = reader.ReadUInt8();

                switch (type)
                {
                    case 0:
                        break;
                    case 1:
                    case 2:
                        var locations = type == 1 || reader.ReadBool();
                        uint serial = 0;
                        while ((serial = reader.ReadUInt32BE()) != 0)
                        {
                            (var x, var y) = (reader.ReadUInt16BE(), reader.ReadUInt16BE());
                            var map = reader.ReadUInt8();
                            var hits = type == 1 ? 0 : reader.ReadUInt8();
                        }
                        break;
                    case 3:
                        break;
                    case 4:
                        break;
                    case 0xF0:
                        break;
                    case 0xFE:
                        network.Value.Send_RazorACK();
                        break;
                }
            };

            // update item SA
            packetsMap.Value[0xF3] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                reader.Skip(2);
                var type = reader.ReadUInt8();
                var serial = reader.ReadUInt32BE();
                var graphic = reader.ReadUInt16BE();
                var graphicInc = reader.ReadUInt8();
                var amount = reader.ReadUInt16BE();
                var unk = reader.ReadUInt16BE();
                (var x, var y, var z) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadInt8());
                var dir = (Direction)reader.ReadUInt8();
                var hue = reader.ReadUInt16BE();
                var flags = (Flags)reader.ReadUInt8();
                var unk2 = reader.ReadUInt16BE();

                var ent = entitiesMap.Value.GetOrCreate(world, serial);
                ent.Set(new Graphic() { Value = (ushort)(graphic + graphicInc) })
                    .Set(new Hue() { Value = hue })
                    .Set(new WorldPosition() { X = x, Y = y, Z = z })
                    .Set(new Facing() { Value = dir })
                    .Set(new Amount() { Value = amount });
            };

            // boat moving
            packetsMap.Value[0xF6] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var serial = reader.ReadUInt32BE();
                var speed = reader.ReadUInt8();
                var movingDir = (Direction)reader.ReadUInt8();
                var facingDir = (Direction)reader.ReadUInt8();
                (var x, var y, var z) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadUInt16BE());

                var count = reader.ReadUInt16BE();
                for (var i = 0; i < count; ++i)
                {
                    var entitySerial = reader.ReadUInt32BE();
                    (var entX, var entY, var entZ) = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), reader.ReadUInt16BE());

                    var ent = entitiesMap.Value.GetOrCreate(world, serial);
                    ent.Set(new WorldPosition() { X = entX, Y = entY, Z = (sbyte)entZ });
                }
            };

            // packet list
            packetsMap.Value[0xF7] = buffer =>
            {
                var reader = new StackDataReader(buffer);

                var count = reader.ReadUInt16BE();
                for (var i = 0; i < count; ++i)
                {
                    var id = reader.ReadUInt8();
                    if (id == 0xF3)
                    {
                        if (packetsMap.Value.TryGetValue(id, out var fn))
                        {
                            fn(reader.Buffer.Slice(reader.Position));
                        }
                    }
                }
            };
        }, Stages.Startup, ThreadingMode.Single);
    }
}
