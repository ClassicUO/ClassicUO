using System;
using ClassicUO.Network;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct PickupPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new GrabbedItem());

        var pickupItemDelayedFn = PickupItem;
        var pickupItemFn = PickupItem;
        var dropItemFn = DropItem;

        scheduler.OnUpdate(pickupItemDelayedFn, ThreadingMode.Single)
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

        scheduler.OnUpdate(pickupItemFn, ThreadingMode.Single)
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


        scheduler.OnUpdate(dropItemFn, ThreadingMode.Single)
            .RunIf((SchedulerState sched) => sched.ResourceExists<SelectedEntity>() && sched.ResourceExists<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial != 0)
            .RunIf((Res<MouseContext> mouseCtx) => mouseCtx.Value.IsReleased(Input.MouseButtonType.Left));
    }

    void PickupItem(
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

        grabbedItem.Value.Serial = serial.Ref.Value;
        grabbedItem.Value.Graphic = graphic.Ref.Value;
        grabbedItem.Value.Hue = hue.Ref.Value;
        grabbedItem.Value.Amount = amount.Ref.Value;
    }

    void DropItem(
        World world,
        Res<SelectedEntity> selectedEntity,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network,
        Res<NetworkEntitiesMap> networkEntities
    )
    {
        // TODO: add all scenarios
        var targetEntity = world.Entity(selectedEntity.Value.Entity);
        var targetSerial = targetEntity.Has<NetworkSerial>() ? targetEntity.Get<NetworkSerial>().Value : 0xFFFF_FFFF;
        (ushort targetX, ushort targetY, sbyte targetZ) = targetEntity.Get<WorldPosition>();
        if (targetSerial != 0xFFFF_FFFF)
            (targetX, targetY, targetZ) = (0, 0, 0);

        Console.WriteLine("drop item to {0}", targetEntity.ID);
        network.Value.Send_DropRequest(grabbedItem.Value.Serial, targetX, targetY, targetZ, 0, targetSerial);

        grabbedItem.Value.Clear();
    }
}

internal sealed class GrabbedItem
{
    public uint Serial { get; set; }
    public ushort Graphic { get; set; }
    public ushort Hue { get; set; }
    public int Amount { get; set; }


    public void Clear()
    {
        Serial = 0;
        Graphic = 0;
        Hue = 0;
        Amount = 0;
    }
}
