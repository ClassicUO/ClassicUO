// Container packet handling + close detection. UI lifecycle lives in
// `ContainerGumpPlugin` (separate UI entities; see note there about Bevy.UI
// roots needing `Without<Parent>`).
//
//   * Translate 0x24 (open) / 0x25 (single update) / 0x3C (batch update)
//     packets into ECS data components on game entities + emit
//     `ContainerOpenedEvent` / `ContainerUpdateEvent`.
//   * Detect close on distance > MAX_CONTAINER_DIST and emit
//     `ContainerClosedEvent`. Right-click close is handled generically in
//     WindowDragPlugin for any UIMovable.

using System;
using ClassicUO.Assets;
using ClassicUO.Ecs.Modding.Host;
using ClassicUO.Network;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct ContainersPlugin : IPlugin
{
    public void Build(App app)
    {
        var processPacketsFn = ProcessContainerPackets;
        var closeContainersTooFarFromPlayerFn = CloseContainersTooFarFromPlayer;

        app
            .AddSystem(processPacketsFn)
                .InStage(Stage.Update)
                .RunIf((EventReader<IPacket> reader) => reader.HasEvents)
                .Build()

            .AddSystem(closeContainersTooFarFromPlayerFn)
                .InStage(Stage.Update)
                .RunIf((Query<Data<WorldPosition>, With<Player>> queryPlayer) => queryPlayer.Count() > 0)
                .Build();
    }

    private static void CloseContainersTooFarFromPlayer(
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<ContainerWindow>> windowsQuery,
        Query<Data<WorldPosition>> worldPosQuery,
        Query<Data<TinyEcs.Parent>> parentQuery,
        Single<Data<WorldPosition>, With<Player>> queryPlayer,
        EventWriter<HostMessage> hostMsgs,
        EventWriter<ContainerClosedEvent> closedWriter)
    {
        const int MAX_CONTAINER_DIST = 5;
        (var playerEnt, var playerPos) = queryPlayer.Get();

        foreach (var (_, window) in windowsQuery)
        {
            var serial = window.Ref.Serial;
            if (!entitiesMap.Value.TryGet(serial, out var gameEnt)) continue;

            // Walk up the parent chain to find the entity that actually owns
            // a world tile (chest on ground / corpse / mobile). Nested
            // containers only carry ContainerSlotPosition — no WorldPosition
            // — so reading their position would be a hole in the data model.
            var root = ResolveRootHolder(gameEnt, parentQuery);
            if (root == playerEnt.Ref) continue;

            if (!worldPosQuery.Contains(root)) continue;
            var (_, pos) = worldPosQuery.Get(root);
            if (Math.Abs(playerPos.Ref.X - pos.Ref.X) >= MAX_CONTAINER_DIST ||
                Math.Abs(playerPos.Ref.Y - pos.Ref.Y) >= MAX_CONTAINER_DIST)
            {
                closedWriter.Send(new ContainerClosedEvent(serial));
                hostMsgs.Send(new HostMessage.ContainerClosed(serial));
            }
        }
    }

    // Walk TinyEcs.Parent links up to the topmost owner. Hard cap on depth so a
    // malformed cycle can't spin forever.
    private static ulong ResolveRootHolder(ulong start, Query<Data<TinyEcs.Parent>> parentQuery)
    {
        var cur = start;
        for (int i = 0; i < 16; i++)
        {
            if (!parentQuery.Contains(cur)) return cur;
            var (_, parent) = parentQuery.Get(cur);
            var pid = (ulong)parent.Ref.Id;
            if (pid == 0 || pid == cur) return cur;
            cur = pid;
        }
        return cur;
    }

    private static void ProcessContainerPackets(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<ContainerOpenedEvent> openedWriter,
        EventWriter<HostMessage> hostMsgs,
        EventReader<IPacket> packets)
    {
        foreach (var packet in packets.Read())
        {
            switch (packet)
            {
                case OnOpenContainerPacket_0x24 open:
                    openedWriter.Send(new(open.Serial, open.Graphic));
                    hostMsgs.Send(new HostMessage.ContainerOpened(open.Serial, open.Graphic));
                    break;

                case OnUpdateContainerPacket_0x25_Pre6017 updatePre:
                    HandleUpdateContainer(updatePre, commands, entitiesMap, writer, hostMsgs);
                    break;

                case OnUpdateContainerPacket_0x25_Post6017 updatePost:
                    HandleUpdateContainer(updatePost, commands, entitiesMap, writer, hostMsgs);
                    break;

                case OnUpdateContainerItemsPacket_0x3C_Pre6017 updateItemsPre:
                    HandleUpdateContainerItems(updateItemsPre, commands, entitiesMap, writer, hostMsgs);
                    break;

                case OnUpdateContainerItemsPacket_0x3C_Post6017 updateItemsPost:
                    HandleUpdateContainerItems(updateItemsPost, commands, entitiesMap, writer, hostMsgs);
                    break;
            }
        }
    }

    private static void HandleUpdateContainer(
        IUpdateContainerPacket packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<HostMessage> hostMsgs)
    {
        var finalGraphic = (ushort)(packet.Graphic + packet.GraphicIncrement);
        var amount = packet.Amount == 0 ? (ushort)1 : packet.Amount;
        var gridIdx = packet.HasGridIndex ? packet.GridIndex : (byte)0;

        var ent = entitiesMap.Value.GetOrCreate(commands, packet.Serial);
        var parentEnt = entitiesMap.Value.GetOrCreate(commands, packet.ContainerSerial)
            .Insert<IsContainer>();

        // Note: don't Remove<WorldPosition> here. Server can despawn the
        // game entity (0x1D) in the same frame; a queued Detach on a dead
        // entity panics in TinyEcs.World.Detach. Stale WorldPosition is
        // harmless because every distance / drop consumer prefers
        // ContainerSlotPosition when present.
        ent.Insert(new Graphic() { Value = finalGraphic })
            .Insert(new ContainerSlotPosition() { X = packet.X, Y = packet.Y, GridIndex = gridIdx })
            .Insert(new Hue() { Value = packet.Hue })
            .Insert(new Amount() { Value = amount })
            .Insert<ContainedInto>();

        parentEnt.AddChild(ent);
        Console.WriteLine("[PKT-0x25 ADD] container=0x{0:X8} item=0x{1:X8} graphic=0x{2:X4} pos=({3},{4}) amount={5}",
            packet.ContainerSerial, packet.Serial, finalGraphic, packet.X, packet.Y, amount);
        writer.Send(new ContainerUpdateEvent(
            packet.ContainerSerial, packet.Serial, finalGraphic, packet.Hue, packet.X, packet.Y, amount));

        hostMsgs.Send(new HostMessage.ContainerItemAdded(
            packet.ContainerSerial,
            packet.Serial,
            finalGraphic,
            amount,
            packet.X,
            packet.Y,
            gridIdx,
            packet.Hue));
    }

    private static void HandleUpdateContainerItems(
        IUpdateContainerItemsPacket packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<HostMessage> hostMsgs)
    {
        foreach (var item in packet.Items)
        {
            hostMsgs.Send(new HostMessage.ContainerItemAdded(
                item.ContainerSerial,
                item.Serial,
                (ushort)(item.Graphic + item.GraphicInc),
                item.Amount,
                item.X,
                item.Y,
                item.GridIndex,
                item.Hue));

            var parentEnt = entitiesMap.Value.GetOrCreate(commands, item.ContainerSerial)
                .Insert<IsContainer>();
            var ent = entitiesMap.Value.GetOrCreate(commands, item.Serial);

            var finalGraphic = (ushort)(item.Graphic + item.GraphicInc);
            // See HandleUpdateContainer above: don't Remove<WorldPosition>
            // here — same despawn-race panic risk.
            ent.Insert(new Graphic() { Value = finalGraphic })
                .Insert(new Hue() { Value = item.Hue })
                .Insert(new ContainerSlotPosition() { X = item.X, Y = item.Y, GridIndex = item.GridIndex })
                .Insert(new Amount() { Value = item.Amount })
                .Insert<ContainedInto>();
            parentEnt.AddChild(ent);

            Console.WriteLine("[PKT-0x3C ITEM] container=0x{0:X8} item=0x{1:X8} graphic=0x{2:X4} pos=({3},{4}) amount={5}",
                item.ContainerSerial, item.Serial, finalGraphic, item.X, item.Y, item.Amount);
            writer.Send(new ContainerUpdateEvent(
                item.ContainerSerial, item.Serial, finalGraphic, item.Hue, item.X, item.Y, item.Amount));
        }
    }
}

// Snapshot of a movable container window's initial placement & size. Read by
// WindowDragPlugin / persistence work.
internal struct FloatingWindowState
{
    public float InitialX;
    public float InitialY;
    public float InitialWidth;
    public float InitialHeight;
}

internal record struct ContainerOpenedEvent(uint Serial, ushort Graphic);
internal record struct ContainerClosedEvent(uint Serial);
// Carries the item payload inline so consumers don't have to query the game
// entity in the same frame — those components are still in the deferred
// command queue when ContainerGumpPlugin runs.
internal record struct ContainerUpdateEvent(
    uint Serial,
    uint ItemSerial,
    ushort Graphic,
    ushort Hue,
    ushort X,
    ushort Y,
    ushort Amount);
