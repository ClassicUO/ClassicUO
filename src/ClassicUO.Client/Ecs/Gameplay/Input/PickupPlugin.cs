using System;
using ClassicUO.IO;
using ClassicUO.Network;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct PickupPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new GrabbedItem());

        var packetSetup = PacketSetup;
        var pickupItemDelayedFn = PickupItem;
        var pickupItemFn = PickupItem;
        var dropItemFn = DropItem;

        scheduler.OnStartup(packetSetup);

        scheduler.OnUpdate(pickupItemDelayedFn)
            .RunIf((SchedulerState sched) => sched.ResourceExists<SelectedEntity>() && sched.ResourceExists<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            .RunIf((Res<MouseContext> mouseCtx) =>
            {
                if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                {
                    var offset = mouseCtx.Value.PositionOffset;
                    if (offset.Length() > 1)
                        return false;
                }

                return true;
            })
            .RunIf((World world, Res<SelectedEntity> selectedEnt, Query<Data<NetworkSerial>, Filter<With<Items>>> q) =>
            {
                if (!selectedEnt.Value.Entity.IsValid())
                    return false;
                if (!world.Exists(selectedEnt.Value.Entity))
                    return false;

                var entity = world.Entity(selectedEnt.Value.Entity);
                return q.Contains(entity.ID);
            })
            .RunIf((Local<float?> delay, Time time, Res<MouseContext> mouseCtx) =>
            {
                if (mouseCtx.Value.IsReleased(Input.MouseButtonType.Left))
                    delay.Value = null;
                else if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
                    delay.Value = time.Total + 1000f;
                else if (!mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                    return false;

                if (time.Total > delay)
                    return true;

                if (delay.Value.HasValue)
                    Console.WriteLine("waiting time {0}", delay - time.Total);
                return false;
            });

        scheduler.OnUpdate(pickupItemFn)
            .RunIf((SchedulerState sched) => sched.ResourceExists<SelectedEntity>() && sched.ResourceExists<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            .RunIf((Res<MouseContext> mouseCtx) =>
            {
                if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                {
                    var offset = mouseCtx.Value.PositionOffset;
                    if (offset.Length() > 1)
                        return true;
                }

                return false;
            })
            .RunIf((World world, Res<SelectedEntity> selectedEnt, Query<Data<NetworkSerial>, Filter<With<Items>>> q) =>
            {
                if (!selectedEnt.Value.Entity.IsValid())
                    return false;
                if (!world.Exists(selectedEnt.Value.Entity))
                    return false;

                var entity = world.Entity(selectedEnt.Value.Entity);
                return q.Contains(entity.ID);
            });


        scheduler.OnUpdate(dropItemFn)
            .RunIf((SchedulerState sched) => sched.ResourceExists<SelectedEntity>() && sched.ResourceExists<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial != 0)
            .RunIf((Res<MouseContext> mouseCtx) => mouseCtx.Value.IsReleased(Input.MouseButtonType.Left));
    }


    static void PacketSetup(
        Res<PacketsMap> packetsMap,
        Res<GrabbedItem> grabbedItem
    )
    {
        // deny move item
        packetsMap.Value[0x27] = buffer =>
        {
            var reader = new StackDataReader(buffer);
            var code = reader.ReadUInt8();

            grabbedItem.Value.Clear();
        };

        // end draggin item
        packetsMap.Value[0x28] = buffer =>
        {
            grabbedItem.Value.Clear();
        };

        // drop item ok
        packetsMap.Value[0x29] = buffer =>
        {
        };
    }

    static void PickupItem(
        World world,
        Res<SelectedEntity> selectedEntity,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network,
        Query<Data<NetworkSerial, Amount, Graphic, Hue>> q
    )
    {
        var entity = world.Entity(selectedEntity.Value.Entity);
        (var serial, var amount, var graphic, var hue) = q.Get(entity.ID);
        Console.WriteLine("pickup item serial: {0} amount: {1}", serial.Ref.Value, amount.Ref.Value);
        network.Value.Send_PickUpRequest(serial.Ref.Value, (ushort)amount.Ref.Value);

        grabbedItem.Value.Clear();
        grabbedItem.Value.IsActive = true;
        grabbedItem.Value.Serial = serial.Ref.Value;
        grabbedItem.Value.Graphic = graphic.Ref.Value;
        grabbedItem.Value.Hue = hue.Ref.Value;
        grabbedItem.Value.Amount = amount.Ref.Value;
    }

    static void DropItem(
        World world,
        Res<SelectedEntity> selectedEntity,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network,
        Res<NetworkEntitiesMap> networkEntities,
        Query<Data<NetworkSerial, WorldPosition>, Optional<NetworkSerial>> query
    )
    {
        // TODO: add all scenarios

        if (!query.Contains(selectedEntity.Value.Entity))
        {
            Console.WriteLine("no target entity found for drop item");
            return;
        }

        (var targetEntity, var targetSerial, var targetWorldPos) = query.Get(selectedEntity.Value.Entity);
        var serial = targetSerial.IsValid() ? targetSerial.Ref.Value : 0xFFFF_FFFF;

        (ushort targetX, ushort targetY, sbyte targetZ) = targetWorldPos.Ref;
        if (serial != 0xFFFF_FFFF)
            (targetX, targetY, targetZ) = (0, 0, 0);

        Console.WriteLine("drop item to {0}", targetEntity.Ref.ID);
        network.Value.Send_DropRequest(grabbedItem.Value.Serial, targetX, targetY, targetZ, 0, serial);

        grabbedItem.Value.Clear();
    }
}

internal sealed class GrabbedItem
{
    public bool IsActive { get; set; }
    public uint Serial { get; set; }
    public ushort Graphic { get; set; }
    public ushort Hue { get; set; }
    public int Amount { get; set; }


    public void Clear()
    {
        IsActive = false;
        Serial = 0;
        Graphic = 0;
        Hue = 0;
        Amount = 0;
    }
}
