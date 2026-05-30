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
using ClassicUO.Game.Data;
using ClassicUO.Network;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct ContainersPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new ContainerSeq());

        var closeContainersTooFarFromPlayerFn = CloseContainersTooFarFromPlayer;

        app.AddSystem(closeContainersTooFarFromPlayerFn)
            .InStage(Stage.Update)
            .RunIf((Query<Data<WorldPosition>, With<Player>> queryPlayer) => queryPlayer.Count() > 0)
            .Build();

        // Per-packet observers replace the boxed EventReader<IPacket> scan.
        app.AddObserver((
            On<PacketReceived<OnOpenContainerPacket_0x24>> trig,
            EventWriter<ContainerOpenedEvent> openedWriter,
            EventWriter<HostMessage> hostMsgs) =>
        {
            var open = trig.Event.Packet;
            openedWriter.Send(new(open.Serial, open.Graphic));
            hostMsgs.Send(new HostMessage.ContainerOpened(open.Serial, open.Graphic));
        });

        app.AddObserver((
            On<PacketReceived<OnUpdateContainerPacket_0x25_Pre6017>> trig,
            Commands commands,
            Res<NetworkEntitiesMap> entitiesMap,
            EventWriter<ContainerUpdateEvent> writer,
            EventWriter<HostMessage> hostMsgs,
            Query<Data<EquipmentSlots>> equipQ,
            Query<Data<NetworkSerial>> serialQ,
            ResMut<ContainerSeq> seq)
            => HandleUpdateContainer(trig.Event.Packet, commands, entitiesMap, writer, hostMsgs, equipQ, serialQ, seq));

        app.AddObserver((
            On<PacketReceived<OnUpdateContainerPacket_0x25_Post6017>> trig,
            Commands commands,
            Res<NetworkEntitiesMap> entitiesMap,
            EventWriter<ContainerUpdateEvent> writer,
            EventWriter<HostMessage> hostMsgs,
            Query<Data<EquipmentSlots>> equipQ,
            Query<Data<NetworkSerial>> serialQ,
            ResMut<ContainerSeq> seq)
            => HandleUpdateContainer(trig.Event.Packet, commands, entitiesMap, writer, hostMsgs, equipQ, serialQ, seq));

        app.AddObserver((
            On<PacketReceived<OnUpdateContainerItemsPacket_0x3C_Pre6017>> trig,
            Commands commands,
            Res<NetworkEntitiesMap> entitiesMap,
            EventWriter<ContainerUpdateEvent> writer,
            EventWriter<HostMessage> hostMsgs,
            Query<Data<EquipmentSlots>> equipQ,
            Query<Data<NetworkSerial>> serialQ,
            ResMut<ContainerSeq> seq)
            => HandleUpdateContainerItems(trig.Event.Packet, commands, entitiesMap, writer, hostMsgs, equipQ, serialQ, seq));

        app.AddObserver((
            On<PacketReceived<OnUpdateContainerItemsPacket_0x3C_Post6017>> trig,
            Commands commands,
            Res<NetworkEntitiesMap> entitiesMap,
            EventWriter<ContainerUpdateEvent> writer,
            EventWriter<HostMessage> hostMsgs,
            Query<Data<EquipmentSlots>> equipQ,
            Query<Data<NetworkSerial>> serialQ,
            ResMut<ContainerSeq> seq)
            => HandleUpdateContainerItems(trig.Event.Packet, commands, entitiesMap, writer, hostMsgs, equipQ, serialQ, seq));
    }

    // An item just entered a container, so it can no longer be worn. Clear any
    // mobile's EquipmentSlots reference to it (e.g. double-clicking yourself
    // dismounts: the mount item moves back into the pack and must vanish from
    // the mount layer). Mirrors legacy Mobile losing the item from its layer
    // list when item.Container changes. Re-Insert bumps the Changed tick so the
    // paperdoll refresh path fires — in-place InlineArray writes don't.
    // Clear any mobile's EquipmentSlots reference to the item with this serial.
    // Matches by SERIAL (via each slot entity's NetworkSerial), NOT by entity
    // id: GetOrCreate churns entity ids for a serial (despawn + immediate map
    // remove + recreate), so the slot can hold a still-live entity whose id no
    // longer equals the map's current entity for that serial. Serial is stable.
    // Re-Insert bumps the Changed tick so the paperdoll refresh path fires.
    internal static void ClearEquipReference(
        Commands commands,
        uint serial,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<NetworkSerial>> serialQ)
    {
        foreach (var (mob, slots) in equipQ)
        {
            bool changed = false;
            for (var layer = Layer.Invalid + 1; layer <= Layer.Bank; ++layer)
            {
                var slotEnt = slots.Ref[layer];
                if (slotEnt == 0 || !serialQ.Contains(slotEnt)) continue;
                var (_, ns) = serialQ.Get(slotEnt);
                if (ns.Ref.Value != serial) continue;
                slots.Ref[layer] = 0;
                changed = true;
            }
            if (changed)
                commands.Entity(mob.Ref).Insert(slots.Ref);
        }
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

    private static void HandleUpdateContainer(
        IUpdateContainerPacket packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<ContainerUpdateEvent> writer,
        EventWriter<HostMessage> hostMsgs,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<NetworkSerial>> serialQ,
        ResMut<ContainerSeq> seq)
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
        ClearEquipReference(commands, packet.Serial, equipQ, serialQ);
        Console.WriteLine("[PKT-0x25 ADD] container=0x{0:X8} item=0x{1:X8} graphic=0x{2:X4} pos=({3},{4}) amount={5}",
            packet.ContainerSerial, packet.Serial, finalGraphic, packet.X, packet.Y, amount);
        writer.Send(new ContainerUpdateEvent(
            packet.ContainerSerial, packet.Serial, finalGraphic, packet.Hue, packet.X, packet.Y, amount, seq.Value.Next()));

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
        EventWriter<HostMessage> hostMsgs,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<NetworkSerial>> serialQ,
        ResMut<ContainerSeq> seq)
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
            ClearEquipReference(commands, item.Serial, equipQ, serialQ);

            Console.WriteLine("[PKT-0x3C ITEM] container=0x{0:X8} item=0x{1:X8} graphic=0x{2:X4} pos=({3},{4}) amount={5}",
                item.ContainerSerial, item.Serial, finalGraphic, item.X, item.Y, item.Amount);
            writer.Send(new ContainerUpdateEvent(
                item.ContainerSerial, item.Serial, finalGraphic, item.Hue, item.X, item.Y, item.Amount, seq.Value.Next()));
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
// Item left a container (deleted via 0x1D, or equipped via 0x2E). The game
// entity is handled by the packet observer; this drops the matching container
// UI slot so the backpack/chest gump stops drawing a phantom. Seq is a global
// emit order stamp shared with ContainerUpdateEvent: when a serial is both
// added and removed in one packet read (mount = add-then-equip; dismount =
// delete-then-readd), the higher Seq is the server's final intent and wins.
internal record struct ContainerItemRemovedEvent(uint ItemSerial, ulong Seq);
// Carries the item payload inline so consumers don't have to query the game
// entity in the same frame — those components are still in the deferred
// command queue when ContainerGumpPlugin runs. See ContainerItemRemovedEvent
// for Seq semantics.
internal record struct ContainerUpdateEvent(
    uint Serial,
    uint ItemSerial,
    ushort Graphic,
    ushort Hue,
    ushort X,
    ushort Y,
    ushort Amount,
    ulong Seq);

// Monotonic emit-order counter shared by container add (ContainerUpdateEvent)
// and remove (ContainerItemRemovedEvent) so the UI can resolve which action a
// server sent last for a given item within one packet read. Never reset —
// ulong wrap is not reachable in a session.
internal sealed class ContainerSeq
{
    private ulong _value;
    public ulong Next() => ++_value;
}
