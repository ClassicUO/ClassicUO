// Container packet handling + close detection. UI lifecycle lives in
// `ContainerGumpPlugin` (separate UI entities; see note there about Bevy.UI
// roots needing `Without<Parent>`).
//
//   * Translate 0x24 (open) / 0x25 (single update) / 0x3C (batch update)
//     packets into ECS data components on game entities + emit
//     `ContainerOpenedEvent` / `ContainerSlotEvent`.
//   * Detect close on distance > MAX_CONTAINER_DIST and emit
//     `ContainerClosedEvent`. Right-click close is handled generically in
//     WindowDragPlugin for any UiMovable.

using System;
using ClassicUO.Assets;
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
        var closeContainersTooFarFromPlayerFn = CloseContainersTooFarFromPlayer;

        app.AddSystem(closeContainersTooFarFromPlayerFn)
            .InStage(Stage.Update)
            .RunIf((Query<Data<WorldPosition>, With<Player>> queryPlayer) => queryPlayer.Count() > 0)
            .Build();

        // Per-packet observers replace the boxed EventReader<IPacket> scan.
        app.AddObserver((
            On<PacketReceived<OnOpenContainerPacket_0x24>> trig,
            EventWriter<ContainerOpenedEvent> openedWriter) =>
        {
            var open = trig.Event.Packet;
            openedWriter.Send(new(open.Serial, open.Graphic));
        });

        app.AddObserver((
            On<PacketReceived<OnUpdateContainerPacket_0x25_Pre6017>> trig,
            Commands commands,
            Res<NetworkEntitiesMap> entitiesMap,
            EventWriter<ContainerSlotEvent> writer,
            Query<Data<EquipmentSlots>> equipQ,
            Query<Data<NetworkSerial>> serialQ)
            => HandleUpdateContainer(trig.Event.Packet, commands, entitiesMap, writer, equipQ, serialQ));

        app.AddObserver((
            On<PacketReceived<OnUpdateContainerPacket_0x25_Post6017>> trig,
            Commands commands,
            Res<NetworkEntitiesMap> entitiesMap,
            EventWriter<ContainerSlotEvent> writer,
            Query<Data<EquipmentSlots>> equipQ,
            Query<Data<NetworkSerial>> serialQ)
            => HandleUpdateContainer(trig.Event.Packet, commands, entitiesMap, writer, equipQ, serialQ));

        app.AddObserver((
            On<PacketReceived<OnUpdateContainerItemsPacket_0x3C_Pre6017>> trig,
            Commands commands,
            Res<NetworkEntitiesMap> entitiesMap,
            EventWriter<ContainerSlotEvent> writer,
            Query<Data<EquipmentSlots>> equipQ,
            Query<Data<NetworkSerial>> serialQ)
            => HandleUpdateContainerItems(trig.Event.Packet, commands, entitiesMap, writer, equipQ, serialQ));

        app.AddObserver((
            On<PacketReceived<OnUpdateContainerItemsPacket_0x3C_Post6017>> trig,
            Commands commands,
            Res<NetworkEntitiesMap> entitiesMap,
            EventWriter<ContainerSlotEvent> writer,
            Query<Data<EquipmentSlots>> equipQ,
            Query<Data<NetworkSerial>> serialQ)
            => HandleUpdateContainerItems(trig.Event.Packet, commands, entitiesMap, writer, equipQ, serialQ));

        // Close a container window when its backing item leaves the world. The
        // server sends 0x1D once the container passes out of view range (or it
        // decays / is picked up); OnDeleteObject despawns the entity, firing
        // OnRemove<NetworkSerial>. The distance poll above only fires while the
        // entity is still mapped, so without this a server-side removal would
        // leak the window. Routes through ContainerClosedEvent so TearDownClosedUi
        // owns the despawn + position memory + child cascade (silent close:
        // UserInitiated stays false, matching legacy Dispose).
        app.AddObserver((
            OnRemove<NetworkSerial> trigger,
            Query<Data<ContainerWindow>> windowsQuery,
            EventWriter<ContainerClosedEvent> closedWriter) =>
        {
            var serial = trigger.Component.Value;
            foreach (var (_, window) in windowsQuery)
            {
                if (window.Ref.Serial != serial) continue;
                closedWriter.Send(new ContainerClosedEvent(serial));
                break;
            }
        });
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
            for (var layer = Layer.Invalid + 1; (int)layer < EquipmentSlots.LayerCount; ++layer)
            {
                var slotEnt = slots.Ref[layer];
                if (slotEnt == 0 || !serialQ.TryGet(slotEnt, out var serialRow)) continue;
                var (_, ns) = serialRow;
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

            if (!worldPosQuery.TryGet(root, out var posRow)) continue;
            var (_, pos) = posRow;
            if (Math.Abs(playerPos.Ref.X - pos.Ref.X) >= MAX_CONTAINER_DIST ||
                Math.Abs(playerPos.Ref.Y - pos.Ref.Y) >= MAX_CONTAINER_DIST)
            {
                closedWriter.Send(new ContainerClosedEvent(serial));
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
            if (!parentQuery.TryGet(cur, out var parentRow)) return cur;
            var (_, parent) = parentRow;
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
        EventWriter<ContainerSlotEvent> writer,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<NetworkSerial>> serialQ)
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
        writer.Send(ContainerSlotEvent.Add(
            packet.ContainerSerial, packet.Serial, finalGraphic, packet.Hue, packet.X, packet.Y, amount));
    }

    private static void HandleUpdateContainerItems(
        IUpdateContainerItemsPacket packet,
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        EventWriter<ContainerSlotEvent> writer,
        Query<Data<EquipmentSlots>> equipQ,
        Query<Data<NetworkSerial>> serialQ)
    {
        foreach (var item in packet.Items)
        {
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

            writer.Send(ContainerSlotEvent.Add(
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
// UserInitiated = the player right-clicked the window closed; only then does
// the close sound play (legacy ContainerGump.CloseWithRightClick). Server- and
// distance-driven closes leave it false and stay silent (legacy Dispose).
internal record struct ContainerClosedEvent(uint Serial, bool UserInitiated = false);

internal enum ContainerSlotAction : byte { Add, Remove }

// Single ordered stream for container-slot mutations: an item entering a
// container (0x25/0x3C -> Add, payload inline) or leaving it (0x1D delete /
// 0x2E equip -> Remove, ItemSerial only). Add carries the full payload inline
// so the consumer needn't query the game entity in the same frame — those
// components are still in the deferred command queue when ContainerGumpPlugin
// runs.
//
// Why one stream instead of separate add/remove events: a serial can be both
// added and removed in one packet read, and order decides the final state
// (mount = add-then-equip -> no slot; dismount = delete-then-readd -> slot).
// All PacketReceived observers fire synchronously during one read, so buffer
// order == server emit order — the LAST event for a given serial is the
// server's final intent. The consumer collapses per-serial to that last event,
// which is why no separate sequence counter is needed.
internal record struct ContainerSlotEvent(
    ContainerSlotAction Action,
    uint ContainerSerial,
    uint ItemSerial,
    ushort Graphic,
    ushort Hue,
    ushort X,
    ushort Y,
    ushort Amount)
{
    public static ContainerSlotEvent Add(
        uint containerSerial, uint itemSerial, ushort graphic, ushort hue, ushort x, ushort y, ushort amount)
        => new(ContainerSlotAction.Add, containerSerial, itemSerial, graphic, hue, x, y, amount);

    public static ContainerSlotEvent Remove(uint itemSerial)
        => new(ContainerSlotAction.Remove, 0, itemSerial, 0, 0, 0, 0, 0);
}
