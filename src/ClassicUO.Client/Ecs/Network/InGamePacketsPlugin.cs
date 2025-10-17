using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using World = TinyEcs.World;

namespace ClassicUO.Ecs;

internal readonly struct MobileBundle : IBundle
{
    public NetworkSerial Serial { get; init; }
    public MobAnimation Animation { get; init; }
    public ScreenPositionOffset Offset { get; init; }

    public void Insert(EntityView entity)
    {
        entity.Set(Serial);
        entity.Set(Animation);
        entity.Set(Offset);
        entity.Set<Mobiles>();
    }

    public void Insert(EntityCommands entity)
    {
        entity.Insert(Serial);
        entity.Insert(Animation);
        entity.Insert(Offset);
        entity.Insert<Mobiles>();
    }
}

internal readonly struct ItemBundle : IBundle
{
    public NetworkSerial Serial { get; init; }

    public void Insert(EntityView entity)
    {
        entity.Set(Serial);
        entity.Set<Items>();
    }

    public void Insert(EntityCommands entity)
    {
        entity.Insert(Serial);
        entity.Insert<Items>();
    }
}

sealed class NetworkEntitiesMap
{
    private readonly Dictionary<uint, ulong> _entities = new();

    public EntityCommands GetOrCreate(Commands commands, uint serial)
    {
        if (_entities.TryGetValue(serial, out var id))
        {
            if (commands.Exists(id))
            {
                return commands.Entity(id);
            }

            _entities.Remove(serial);
        }

        var ent = commands.Spawn();
        if (SerialHelper.IsMobile(serial))
            ent.InsertBundle(new MobileBundle { Serial = new NetworkSerial { Value = serial } });
        else
            ent.InsertBundle(new ItemBundle { Serial = new NetworkSerial { Value = serial } });

        _entities.Add(serial, ent.Id);
        return ent;
    }

    public bool Remove(uint serial)
    {
        return _entities.Remove(serial, out var id);
    }

    public EntityCommands Get(Commands commands, uint serial)
    {
        if (_entities.TryGetValue(serial, out var id))
        {
            if (commands.Exists(id))
            {
                return commands.Entity(id);
            }

            _entities.Remove(serial);
        }

        return default;
    }

    public void Clear()
    {
        _entities.Clear();
    }

    public Dictionary<uint, ulong>.Enumerator GetEnumerator()
    {
        return _entities.GetEnumerator();
    }
}

readonly struct InGamePacketsPlugin : IPlugin
{
    public void Build(App app)
    {
        app
            .AddSystem((
                EventReader<IPacket> reader,
                Commands commands,
                Res<NetworkEntitiesMap> entitiesMap,
                Res<NetClient> network,
                Res<Settings> settings,
                Res<UOFileManager> fileManager,
                Res<Profile> profile,
                ResMut<GameContext> gameCtx,
                Res<MultiCache> multiCache,
                Res<DelayedAction> delayedActions,
                ResMut<NextState<GameState>> state,
                EventWriter<MobileQueuedStep> mobileQueuedSteps,
                EventWriter<TextOverheadEvent> textOverHeadQueue,
                InGameQueries queries
            ) =>
            {
                foreach (var packet in reader.Read())
                {
                    HandlePacket(
                        packet,
                        commands,
                        entitiesMap,
                        network,
                        settings,
                        fileManager,
                        profile,
                        gameCtx,
                        multiCache,
                        delayedActions,
                        state,
                        mobileQueuedSteps,
                        textOverHeadQueue,
                        queries);
                }
            })
            .InStage(Stage.Update)
            .RunIf((EventReader<IPacket> reader) => reader.HasEvents)
            .Build();
    }


    sealed class InGameQueries : ISystemParam
    {
        public Query<Data<HouseRevision>> qHouseRevision { get; } = new();
        public Query<Data<EquipmentSlots>> qEquipmentSlots { get; } = new();
        public Query<Data<WorldPosition, Graphic>> qPosAndGraphic { get; } = new();
        public Query<Empty, With<IsMulti>> qMultis { get; } = new();

        public void Initialize(World world)
        {
            qHouseRevision.Initialize(world);
            qEquipmentSlots.Initialize(world);
            qPosAndGraphic.Initialize(world);
            qMultis.Initialize(world);
        }

        public void Fetch(World world)
        {
            qHouseRevision.Fetch(world);
            qEquipmentSlots.Fetch(world);
            qPosAndGraphic.Fetch(world);
            qMultis.Fetch(world);
        }

        public SystemParamAccess GetAccess()
        {
            var access = new SystemParamAccess();
            var houseRev = qHouseRevision.GetAccess();
            var equipSlots = qEquipmentSlots.GetAccess();
            var posAndGraphic = qPosAndGraphic.GetAccess();
            var multis = qMultis.GetAccess();

            foreach (var read in houseRev.ReadResources) access.ReadResources.Add(read);
            foreach (var write in houseRev.WriteResources) access.WriteResources.Add(write);
            foreach (var read in equipSlots.ReadResources) access.ReadResources.Add(read);
            foreach (var write in equipSlots.WriteResources) access.WriteResources.Add(write);
            foreach (var read in posAndGraphic.ReadResources) access.ReadResources.Add(read);
            foreach (var write in posAndGraphic.WriteResources) access.WriteResources.Add(write);
            foreach (var read in multis.ReadResources) access.ReadResources.Add(read);
            foreach (var write in multis.WriteResources) access.WriteResources.Add(write);

            return access;
        }
    }

    static void HandlePacket(
        IPacket packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<Settings> settings,
        Res<UOFileManager> fileManager,
        Res<Profile> profile,
        ResMut<GameContext> gameCtx,
        Res<MultiCache> multiCache,
        Res<DelayedAction> delayedActions,
        ResMut<NextState<GameState>> state,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        EventWriter<TextOverheadEvent> textOverHeadQueue,
        InGameQueries queries
    )
    {
        _ = profile;

        switch (packet)
        {
            case OnEnterWorldPacket_0x1B enterWorld:
                HandleEnterWorld(enterWorld, commands, entitiesMap, network, settings, gameCtx, state);
                break;

            case OnLoginCompletePacket_0x55 loginComplete:
                HandleLoginComplete(loginComplete, network, gameCtx, settings);
                break;

            case OnClientVersionPacket_0xBD clientVersion:
                HandleClientVersion(clientVersion, network, settings);
                break;

            case OnUnicodeSpeechPacket_0xAE unicodeSpeech:
                HandleUnicodeSpeech(unicodeSpeech, network, textOverHeadQueue);
                break;

            case OnViewRangePacket_0xC8 viewRange:
                HandleViewRange(viewRange, gameCtx);
                break;

            case OnAsciiSpeechPacket_0x1C asciiSpeech:
                HandleAsciiSpeech(asciiSpeech, network, textOverHeadQueue);
                break;

            case OnExtendedCommandPacket_0xBF extendedCommand:
                HandleExtendedCommand(
                    extendedCommand,
                    commands,
                    entitiesMap,
                    network,
                    settings,
                    fileManager,
                    gameCtx,
                    multiCache,
                    delayedActions,
                    mobileQueuedSteps,
                    textOverHeadQueue,
                    queries
                );
                break;

            case OnUpdateItemPacket_0x1A updateItem:
                HandleUpdateItem(updateItem, commands, entitiesMap, multiCache, gameCtx, mobileQueuedSteps, queries);
                break;

            case OnUpdateItemSAPacket_0xF3 updateItemSA:
                HandleUpdateItemSA(updateItemSA, commands, entitiesMap, multiCache, mobileQueuedSteps, queries);
                break;

            case OnUpdateObjectPacket_0xD3 updateObject:
                HandleUpdateObject(updateObject, commands, entitiesMap, mobileQueuedSteps, queries);
                break;

            case OnUpdateObjectAltPacket_0x78 updateObjectAlt:
                HandleUpdateObject(updateObjectAlt, commands, entitiesMap, mobileQueuedSteps, queries);
                break;

            case OnDeleteObjectPacket_0x1D deleteObject:
                HandleDeleteObject(deleteObject, entitiesMap, gameCtx);
                break;

            case OnUpdatePlayerPacket_0x20 updatePlayer:
                HandleUpdatePlayer(updatePlayer, commands, entitiesMap, mobileQueuedSteps);
                break;

            case OnCharacterStatusPacket_0x11 status:
                HandleCharacterStatus(status, commands, entitiesMap);
                break;

            case OnMobileAttributesPacket_0x2D mobileAttributes:
                HandleMobileAttributes(mobileAttributes, commands, entitiesMap);
                break;

            case OnEquipItemPacket_0x2E equipItem:
                HandleEquipItem(equipItem, commands, entitiesMap, queries);
                break;

            case OnUpdateHitsPacket_0xA1 updateHits:
                HandleUpdateHits(updateHits, commands, entitiesMap);
                break;

            case OnUpdateManaPacket_0xA2 updateMana:
                HandleUpdateMana(updateMana, commands, entitiesMap);
                break;

            case OnUpdateStaminaPacket_0xA3 updateStamina:
                HandleUpdateStamina(updateStamina, commands, entitiesMap);
                break;

            case OnLockFeaturesPacket_0xB9_Pre60142 lockFeaturesPre:
                HandleLockFeatures(lockFeaturesPre.Flags, fileManager);
                break;

            case OnLockFeaturesPacket_0xB9_Post60142 lockFeaturesPost:
                HandleLockFeatures(lockFeaturesPost.Flags, fileManager);
                break;

            case OnQuestPointerPacket_0xBA_Pre7090 questPointerPre:
                HandleQuestPointer(questPointerPre.Display, questPointerPre.X, questPointerPre.Y, null);
                break;

            case OnQuestPointerPacket_0xBA_Post7090 questPointerPost:
                HandleQuestPointer(questPointerPost.Display, questPointerPost.X, questPointerPost.Y, questPointerPost.Serial);
                break;

            case OnShowMapPacket_0x90_Pre308Z showMapPre:
                HandleShowMap(showMapPre.Serial, showMapPre.GumpId, showMapPre.StartX, showMapPre.StartY, showMapPre.EndX, showMapPre.EndY, showMapPre.Width, showMapPre.Height, null);
                break;

            case OnShowMapPacket_0x90_Post308Z showMapPost:
                HandleShowMap(showMapPost.Serial, showMapPost.GumpId, showMapPost.StartX, showMapPost.StartY, showMapPost.EndX, showMapPost.EndY, showMapPost.Width, showMapPost.Height, showMapPost.Facet);
                break;

            case OnShowMapFacetPacket_0xF5_Pre308Z showFacetPre:
                HandleShowMapFacet(showFacetPre.Serial, showFacetPre.GumpId, showFacetPre.StartX, showFacetPre.StartY, showFacetPre.EndX, showFacetPre.EndY, showFacetPre.Width, showFacetPre.Height, 0);
                break;

            case OnShowMapFacetPacket_0xF5_Post308Z showFacetPost:
                HandleShowMapFacet(showFacetPost.Serial, showFacetPost.GumpId, showFacetPost.StartX, showFacetPost.StartY, showFacetPost.EndX, showFacetPost.EndY, showFacetPost.Width, showFacetPost.Height, showFacetPost.Facet);
                break;
            case OnCustomHousePacket_0xD8 customHouse:
                HandleCustomHouse(customHouse, commands, entitiesMap, multiCache, queries);
                break;

            default:
                Console.WriteLine("Unhandled packet 0x{0:X2}", packet.Id);
                break;
        }
    }

    static void HandleEnterWorld(
        OnEnterWorldPacket_0x1B packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<Settings> settings,
        ResMut<GameContext> gameCtx,
        ResMut<NextState<GameState>> state
    )
    {
        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_200)
        {
            network.Value.Send_GameWindowSize(800, 400);
            network.Value.Send_Language(settings.Value.Language);
        }

        network.Value.Send_ClientVersion(settings.Value.ClientVersion);
        network.Value.Send_ClickRequest(packet.Serial);
        network.Value.Send_SkillsRequest(packet.Serial);

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_70796)
            network.Value.Send_ShowPublicHouseContent(true);

        gameCtx.Value.CenterX = packet.Position.X;
        gameCtx.Value.CenterY = packet.Position.Y;
        gameCtx.Value.CenterZ = packet.Position.Z;
        gameCtx.Value.PlayerSerial = packet.Serial;

        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Ecs.WorldPosition() { X = packet.Position.X, Y = packet.Position.Y, Z = packet.Position.Z })
            .Insert(new Ecs.Graphic() { Value = packet.Graphic })
            .Insert(new Facing() { Value = packet.Direction })
            .Insert(new MobileSteps() { Index = -1 })
            .Insert<Player>();

        state.Value.Set(GameState.GameScreen);
    }

    static void HandleLoginComplete(
        OnLoginCompletePacket_0x55 packet,
        Res<NetClient> network,
        ResMut<GameContext> gameCtx,
        Res<Settings> settings
    )
    {
        if (gameCtx.Value.PlayerSerial == 0)
            return;

        network.Value.Send_StatusRequest(gameCtx.Value.PlayerSerial);
        network.Value.Send_OpenChat("");
        network.Value.Send_SkillsRequest(gameCtx.Value.PlayerSerial);

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_306E)
            network.Value.Send_ClientType(gameCtx.Value.Protocol);

        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_305D)
            network.Value.Send_ClientViewRange(24);
    }

    static void HandleClientVersion(
        OnClientVersionPacket_0xBD packet,
        Res<NetClient> network,
        Res<Settings> settings
    ) => network.Value.Send_ClientVersion(settings.Value.ClientVersion);

    static void HandleUnicodeSpeech(
        OnUnicodeSpeechPacket_0xAE packet,
        Res<NetClient> network,
        EventWriter<TextOverheadEvent> textOverHeadQueue
    )
    {
        if (packet.IsSystemMessage)
        {
            network.Value.Send(
            [
                0x03, 0x00, 0x28, 0x20, 0x00, 0x34, 0x00, 0x03, 0xDB, 0x13,
                0x14, 0x3F, 0x45, 0x2C, 0x58, 0x0F, 0x5D, 0x44, 0x2E, 0x50,
                0x11, 0xDF, 0x75, 0x5C, 0xE0, 0x3E, 0x71, 0x4F, 0x31, 0x34,
                0x05, 0x4E, 0x18, 0x1E, 0x72, 0x0F, 0x59, 0xAD, 0xF5, 0x00
            ]);
            return;
        }

        Console.WriteLine("[0xAE] {0} says: '{1}'", packet.Name, packet.Text);

        textOverHeadQueue.Send(new TextOverheadEvent
        {
            Serial = packet.Serial,
            Name = packet.Name,
            Hue = packet.Hue,
            Font = (byte)packet.Font,
            Text = packet.Text,
            MessageType = packet.MessageType
        });
    }

    static void HandleAsciiSpeech(
        OnAsciiSpeechPacket_0x1C packet,
        Res<NetClient> network,
        EventWriter<TextOverheadEvent> textOverHeadQueue
    )
    {
        if (packet.IsSystemMessage)
        {
            network.Value.Send_ACKTalk();
            return;
        }

        Console.WriteLine("[0x1C] {0} says: '{1}'", packet.Name, packet.Text);

        textOverHeadQueue.Send(new TextOverheadEvent
        {
            Serial = packet.Serial,
            Name = packet.Name,
            Hue = packet.Hue,
            Font = (byte)packet.Font,
            Text = packet.Text,
            MessageType = packet.MessageType
        });
    }

    static void HandleViewRange(
        OnViewRangePacket_0xC8 packet,
        ResMut<GameContext> gameCtx
    ) => gameCtx.Value.MaxObjectsDistance = packet.Range;

    static void HandleExtendedCommand(
        OnExtendedCommandPacket_0xBF packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<Settings> settings,
        Res<UOFileManager> fileManager,
        ResMut<GameContext> gameCtx,
        Res<MultiCache> multiCache,
        Res<DelayedAction> delayedActions,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        EventWriter<TextOverheadEvent> textOverHeadQueue,
        InGameQueries queries
    )
    {
        _ = settings;
        _ = multiCache;
        _ = mobileQueuedSteps;
        _ = textOverHeadQueue;

        switch (packet.Command)
        {
            case 0x08:
                HandleExtendedCommand_MapChange(packet, fileManager, gameCtx);
                break;

            case 0x1D:
                HandleExtendedCommand_HouseRevision(packet, commands, entitiesMap, network, delayedActions, queries);
                break;
        }
    }

    static void HandleExtendedCommand_MapChange(
        OnExtendedCommandPacket_0xBF packet,
        Res<UOFileManager> fileManager,
        ResMut<GameContext> gameCtx
    )
    {
        if (!packet.MapIndex.HasValue)
            return;

        var mapIdx = packet.MapIndex.Value;
        fileManager.Value.Maps.LoadMap(mapIdx);

        if (gameCtx.Value.Map != mapIdx)
        {
            gameCtx.Value.Map = mapIdx;
        }
    }

    static void HandleExtendedCommand_HouseRevision(
        OnExtendedCommandPacket_0xBF packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<NetClient> network,
        Res<DelayedAction> delayedActions,
        InGameQueries queries
    )
    {
        if (!packet.HouseRevisionSerial.HasValue || !packet.HouseRevision.HasValue)
            return;

        var serial = packet.HouseRevisionSerial.Value;
        var revision = packet.HouseRevision.Value;

        var house = entitiesMap.Value.GetOrCreate(commands, serial);

        if (queries.qHouseRevision.Contains(house.Id))
        {
            (_, var houseRev) = queries.qHouseRevision.Get(house.Id);
            if (houseRev.Ref.Value == revision)
                return;
        }

        house.Insert(new HouseRevision { Value = revision });
        delayedActions.Value.Add(() => network.Value.Send_CustomHouseDataRequest(serial), 1000);
    }

    static void HandleUpdateItem(
        OnUpdateItemPacket_0x1A packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<MultiCache> multiCache,
        ResMut<GameContext> gameCtx,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries
    )
    {
        _ = gameCtx;

        var finalGraphic = (ushort)(packet.Graphic + packet.GraphicIncrement);
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = finalGraphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags });

        if (packet.Amount > 0)
            ent.Insert(new Amount() { Value = packet.Amount });

        if (packet.Type == 2 && !queries.qMultis.Contains(ent.Id))
        {
            ent.Insert<IsMulti>();

            var multiInfo = multiCache.Value.GetMulti(finalGraphic);
            foreach (ref readonly var block in CollectionsMarshal.AsSpan(multiInfo.Blocks))
            {
                if (!block.IsVisible)
                    continue;

                var child = commands.Spawn()
                    .Insert(new Graphic() { Value = block.ID })
                    .Insert(new Hue())
                    .Insert(new WorldPosition()
                    {
                        X = (ushort)(packet.X + block.X),
                        Y = (ushort)(packet.Y + block.Y),
                        Z = (sbyte)(packet.Z + block.Z)
                    })
                    .Insert<NormalMulti>();

                ent.AddChild(child);
            }
        }

        if (Game.SerialHelper.IsMobile(packet.Serial))
        {
            mobileQueuedSteps.Send(new MobileQueuedStep
            {
                Serial = packet.Serial,
                X = packet.X,
                Y = packet.Y,
                Z = packet.Z,
                Direction = packet.Direction
            });
        }
        else
        {
            ent.Insert(new WorldPosition() { X = packet.X, Y = packet.Y, Z = packet.Z })
                .Insert(new Facing() { Value = packet.Direction });
        }
    }

    static void HandleUpdateItemSA(
        OnUpdateItemSAPacket_0xF3 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<MultiCache> multiCache,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries
    )
    {
        var finalGraphic = (ushort)(packet.Graphic + packet.GraphicIncrement);
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = finalGraphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new Amount() { Value = packet.Amount })
            .Insert(new ServerFlags() { Value = packet.Flags });

        if (ClassicUO.Game.SerialHelper.IsMobile(packet.Serial))
        {
            mobileQueuedSteps.Send(new MobileQueuedStep
            {
                Serial = packet.Serial,
                X = packet.X,
                Y = packet.Y,
                Z = packet.Z,
                Direction = packet.Direction
            });
        }
        else
        {
            ent.Insert(new WorldPosition() { X = packet.X, Y = packet.Y, Z = packet.Z })
                .Insert(new Facing() { Value = packet.Direction });
        }

        if (packet.UpdateType == 2 && !queries.qMultis.Contains(ent.Id))
        {
            ent.Insert<IsMulti>();

            var multiInfo = multiCache.Value.GetMulti(finalGraphic);
            foreach (ref readonly var block in CollectionsMarshal.AsSpan(multiInfo.Blocks))
            {
                if (!block.IsVisible)
                    continue;

                var child = commands.Spawn()
                    .Insert(new Graphic() { Value = block.ID })
                    .Insert(new Hue())
                    .Insert(new WorldPosition()
                    {
                        X = (ushort)(packet.X + block.X),
                        Y = (ushort)(packet.Y + block.Y),
                        Z = (sbyte)(packet.Z + block.Z)
                    })
                    .Insert<NormalMulti>();

                ent.AddChild(child);
            }
        }
    }

    static void HandleCustomHouse(
        OnCustomHousePacket_0xD8 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<MultiCache> multiCache,
        InGameQueries queries
    )
    {
        if (packet.Planes == null || packet.Planes.Count == 0)
            return;

        var parent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);

        if (!queries.qPosAndGraphic.Contains(parent.Id))
            return;

        (var pos, var graphic) = queries.qPosAndGraphic.Get(parent.Id);
        (var startX, var startY, var startZ) = pos.Ref;

        parent.Insert(new HouseRevision { Value = packet.Revision });

        var multiRect = multiCache.Value.GetMulti(graphic.Ref.Value).Bounds;

        foreach (var plane in packet.Planes)
        {
            if (plane.Data == null || plane.Data.Length == 0)
                continue;

            var reader = new StackDataReader(plane.Data);

            switch (plane.PlaneMode)
            {
                case 0:
                    {
                        var entries = plane.Data.Length / 5;
                        for (var i = 0; i < entries; ++i)
                        {
                            var id = reader.ReadUInt16BE();
                            var offsetX = reader.ReadInt8();
                            var offsetY = reader.ReadInt8();
                            var offsetZ = reader.ReadInt8();

                            var child = commands.Spawn()
                                .Insert(new Graphic() { Value = id })
                                .Insert(new Hue())
                                .Insert(new WorldPosition()
                                {
                                    X = (ushort)(startX + offsetX),
                                    Y = (ushort)(startY + offsetY),
                                    Z = (sbyte)(startZ + offsetZ)
                                })
                                .Insert<CustomMulti>();

                            parent.AddChild(child);
                        }

                        break;
                    }

                case 1:
                    {
                        var planeZ = plane.PlaneZ;
                        var z = planeZ > 0 ? (sbyte)(((planeZ - 1) % 4) * 20 + 7) : (sbyte)0;
                        var entries = plane.Data.Length >> 2;

                        for (var i = 0; i < entries; ++i)
                        {
                            var id = reader.ReadUInt16BE();
                            var offsetX = reader.ReadInt8();
                            var offsetY = reader.ReadInt8();

                            if (id == 0)
                                continue;

                            var child = commands.Spawn()
                                .Insert(new Graphic() { Value = id })
                                .Insert(new Hue())
                                .Insert(new WorldPosition()
                                {
                                    X = (ushort)(startX + offsetX),
                                    Y = (ushort)(startY + offsetY),
                                    Z = (sbyte)(startZ + z)
                                })
                                .Insert<CustomMulti>();

                            parent.AddChild(child);
                        }

                        break;
                    }

                case 2:
                    {
                        var planeZ = plane.PlaneZ;
                        var z = planeZ > 0 ? (sbyte)(((planeZ - 1) % 4) * 20 + 7) : (sbyte)0;

                        short offX;
                        short offY;
                        short multiHeight;

                        if (planeZ <= 0)
                        {
                            offX = (short)multiRect.X;
                            offY = (short)multiRect.Y;
                            multiHeight = (short)(multiRect.Height - multiRect.Y + 2);
                        }
                        else if (planeZ <= 4)
                        {
                            offX = (short)(multiRect.X + 1);
                            offY = (short)(multiRect.Y + 1);
                            multiHeight = (short)(multiRect.Height - multiRect.Y);
                        }
                        else
                        {
                            offX = (short)multiRect.X;
                            offY = (short)multiRect.Y;
                            multiHeight = (short)(multiRect.Height - multiRect.Y + 1);
                        }

                        if (multiHeight <= 0)
                            break;

                        var entries = plane.Data.Length >> 1;

                        for (var i = 0; i < entries; ++i)
                        {
                            var id = reader.ReadUInt16BE();
                            if (id == 0)
                                continue;

                            var relativeX = (sbyte)((i == 0 ? 0 : (i / multiHeight)) + offX);
                            var relativeY = (sbyte)(i % multiHeight + offY);

                            var child = commands.Spawn()
                                .Insert(new Graphic() { Value = id })
                                .Insert(new Hue())
                                .Insert(new WorldPosition()
                                {
                                    X = (ushort)(startX + relativeX),
                                    Y = (ushort)(startY + relativeY),
                                    Z = (sbyte)(startZ + z)
                                })
                                .Insert<CustomMulti>();

                            parent.AddChild(child);
                        }

                        break;
                    }
            }
        }
    }

    static void HandleUpdateObject(
        OnUpdateObjectPacket_0xD3 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = packet.Graphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags });

        EquipmentSlots slots;
        if (queries.qEquipmentSlots.Contains(ent.Id))
        {
            (_, var existing) = queries.qEquipmentSlots.Get(ent.Id);
            slots = existing.Ref;
        }
        else
        {
            slots = new EquipmentSlots();
        }

        if (packet.Equipment != null)
        {
            foreach (var entry in packet.Equipment)
            {
                var child = entitiesMap.Value.GetOrCreate(commands, entry.Serial);
                child.Insert(new Graphic() { Value = entry.Graphic })
                    .Insert(new Hue() { Value = entry.Hue });

                slots[entry.Layer] = child.Id;
            }
        }

        ent.Insert(slots);

        mobileQueuedSteps.Send(new MobileQueuedStep
        {
            Serial = packet.Serial,
            X = packet.X,
            Y = packet.Y,
            Z = packet.Z,
            Direction = packet.Direction
        });
    }

    static void HandleUpdateObject(
        OnUpdateObjectAltPacket_0x78 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<MobileQueuedStep> mobileQueuedSteps,
        InGameQueries queries
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = packet.Graphic })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags });

        EquipmentSlots slots;
        if (queries.qEquipmentSlots.Contains(ent.Id))
        {
            (_, var existing) = queries.qEquipmentSlots.Get(ent.Id);
            slots = existing.Ref;
        }
        else
        {
            slots = new EquipmentSlots();
        }

        if (packet.Equipment != null)
        {
            foreach (var entry in packet.Equipment)
            {
                var child = entitiesMap.Value.GetOrCreate(commands, entry.Serial);
                child.Insert(new Graphic() { Value = entry.Graphic })
                    .Insert(new Hue() { Value = entry.Hue });

                slots[entry.Layer] = child.Id;
            }
        }

        ent.Insert(slots);

        mobileQueuedSteps.Send(new MobileQueuedStep
        {
            Serial = packet.Serial,
            X = packet.X,
            Y = packet.Y,
            Z = packet.Z,
            Direction = packet.Direction
        });
    }

    static void HandleDeleteObject(
        OnDeleteObjectPacket_0x1D packet,
        Res<NetworkEntitiesMap> entitiesMap,
        ResMut<GameContext> gameCtx
    )
    {
        if (gameCtx.Value.PlayerSerial == 0)
            return;

        Console.WriteLine("delete obj from packet: 0x{0:X8}", packet.Serial);
        entitiesMap.Value.Remove(packet.Serial);
    }

    static void HandleUpdatePlayer(
        OnUpdatePlayerPacket_0x20 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<MobileQueuedStep> mobileQueuedSteps
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Graphic() { Value = (ushort)(packet.Graphic + packet.GraphicIncrement) })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new ServerFlags() { Value = packet.Flags });

        mobileQueuedSteps.Send(new MobileQueuedStep
        {
            Serial = packet.Serial,
            X = packet.X,
            Y = packet.Y,
            Z = packet.Z,
            Direction = packet.Direction
        });
    }

    static void HandleCharacterStatus(
        OnCharacterStatusPacket_0x11 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Hits() { Value = packet.Hits, MaxValue = packet.HitsMax });

        if (packet.Stamina.HasValue && packet.StaminaMax.HasValue)
        {
            ent.Insert(new Stamina()
            {
                Value = packet.Stamina.Value,
                MaxValue = packet.StaminaMax.Value
            });
        }

        if (packet.Mana.HasValue && packet.ManaMax.HasValue)
        {
            ent.Insert(new Mana()
            {
                Value = packet.Mana.Value,
                MaxValue = packet.ManaMax.Value
            });
        }
    }

    static void HandleMobileAttributes(
        OnMobileAttributesPacket_0x2D packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Hits() { Value = packet.Hits, MaxValue = packet.HitsMax })
            .Insert(new Mana() { Value = packet.Mana, MaxValue = packet.ManaMax })
            .Insert(new Stamina() { Value = packet.Stamina, MaxValue = packet.StaminaMax });
    }

    static void HandleEquipItem(
        OnEquipItemPacket_0x2E packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        InGameQueries queries
    )
    {
        var parentEnt = entitiesMap.Value.GetOrCreate(commands, packet.ContainerSerial);
        var childEnt = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        childEnt.Insert(new Graphic() { Value = (ushort)(packet.Graphic + packet.GraphicIncrement) })
            .Insert(new Hue() { Value = packet.Hue });

        EquipmentSlots slots;
        if (queries.qEquipmentSlots.Contains(parentEnt.Id))
        {
            (_, var existing) = queries.qEquipmentSlots.Get(parentEnt.Id);
            slots = existing.Ref;
        }
        else
        {
            slots = new EquipmentSlots();
        }

        slots[packet.Layer] = childEnt.Id;
        parentEnt.Insert(slots);
    }

    static void HandleUpdateHits(
        OnUpdateHitsPacket_0xA1 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Hits() { Value = packet.Hits, MaxValue = packet.HitsMax });
    }

    static void HandleUpdateMana(
        OnUpdateManaPacket_0xA2 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Mana() { Value = packet.Mana, MaxValue = packet.ManaMax });
    }

    static void HandleUpdateStamina(
        OnUpdateStaminaPacket_0xA3 packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap
    )
    {
        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        ent.Insert(new Stamina() { Value = packet.Stamina, MaxValue = packet.StaminaMax });
    }

    static BodyConvFlags ComputeBodyConvFlags(LockedFeatureFlags flags)
    {
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

        return bcFlags;
    }

    static void HandleLockFeatures(
        LockedFeatureFlags flags,
        Res<UOFileManager> fileManager
    )
    {
        var conv = ComputeBodyConvFlags(flags);
        fileManager.Value.Animations.ProcessBodyConvDef(conv);
    }

    static void HandleQuestPointer(bool display, ushort x, ushort y, uint? serial)
    {
        _ = display;
        _ = x;
        _ = y;
        _ = serial;
    }

    static void HandleShowMap(
        uint serial,
        ushort gumpId,
        ushort startX,
        ushort startY,
        ushort endX,
        ushort endY,
        ushort width,
        ushort height,
        ushort? facet
    )
    {
        _ = serial;
        _ = gumpId;
        _ = startX;
        _ = startY;
        _ = endX;
        _ = endY;
        _ = width;
        _ = height;
        _ = facet;
    }

    static void HandleShowMapFacet(
        uint serial,
        ushort gumpId,
        ushort startX,
        ushort startY,
        ushort endX,
        ushort endY,
        ushort width,
        ushort height,
        ushort facet
    )
    {
        _ = serial;
        _ = gumpId;
        _ = startX;
        _ = startY;
        _ = endX;
        _ = endY;
        _ = width;
        _ = height;
        _ = facet;
    }
}
