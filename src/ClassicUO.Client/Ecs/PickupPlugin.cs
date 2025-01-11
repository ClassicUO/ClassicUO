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

        scheduler.AddSystem(pickupItemDelayedFn, threadingType: ThreadingMode.Single)
            .RunIf((SchedulerState sched) => sched.ResourceExists<SelectedEntity>() && sched.ResourceExists<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            .RunIf((World world, Res<SelectedEntity> selectedEnt) => {
                if (!selectedEnt.Value.Entity.IsValid())
                    return false;
                if (!world.Exists(selectedEnt.Value.Entity))
                    return false;

                var entity = world.Entity(selectedEnt.Value.Entity);
                return entity.Has<Items>() && entity.Has<NetworkSerial>();
            })
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
            .RunIf((Local<float?> delay, Time time, Res<MouseContext> mouseCtx, Res<GrabbedItem> grabbedEntity) => {
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

        scheduler.AddSystem(pickupItemFn, threadingType: ThreadingMode.Single)
            .RunIf((SchedulerState sched) => sched.ResourceExists<SelectedEntity>() && sched.ResourceExists<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            .RunIf((World world, Res<SelectedEntity> selectedEnt) =>
            {
                if (!selectedEnt.Value.Entity.IsValid())
                    return false;
                if (!world.Exists(selectedEnt.Value.Entity))
                    return false;

                var entity = world.Entity(selectedEnt.Value.Entity);
                return entity.Has<Items>() && entity.Has<NetworkSerial>();
            })
            .RunIf((Res<MouseContext> mouseCtx) =>
            {
                if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                {
                    var offset = mouseCtx.Value.PositionOffset;
                    if (offset.Length() > 1)
                        return true;
                }

                return false;
            });


        scheduler.AddSystem(dropItemFn, threadingType: ThreadingMode.Single)
            .RunIf((SchedulerState sched) => sched.ResourceExists<SelectedEntity>() && sched.ResourceExists<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial != 0)
            .RunIf((Res<MouseContext> mouseCtx) => mouseCtx.Value.IsReleased(Input.MouseButtonType.Left));


        scheduler.AddSystem((Res<MouseContext> mouseCtx, Res<GrabbedItem> grabbedEntity) =>
        {
            if (mouseCtx.Value.IsReleased(Input.MouseButtonType.Left))
                grabbedEntity.Value.Serial = 0;
        }, threadingType: ThreadingMode.Single)
        .RunIf((SchedulerState sched) => sched.ResourceExists<SelectedEntity>() && sched.ResourceExists<GrabbedItem>());
    }

    void PickupItem(
        World world,
        Res<SelectedEntity> selectedEntity,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network
    )
    {
        var entity = world.Entity(selectedEntity.Value.Entity);
        var serial = entity.Get<NetworkSerial>().Value;
        var amount = Math.Min(1, entity.Get<Amount>().Value);
        Console.WriteLine("pickup item serial: {0} amount: {1}", serial, amount);
        network.Value.Send_PickUpRequest(serial, (ushort)amount);
        grabbedItem.Value.Serial = serial;
    }

    void DropItem(
        World world,
        Res<SelectedEntity> selectedEntity,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network,
        Res<NetworkEntitiesMap> networkEntities
        // Query<Data<NetworkSerial>, Filter<Optional<NetworkSerial>>> query
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
    }
}

internal sealed class GrabbedItem
{
    public uint Serial { get; set; }
}