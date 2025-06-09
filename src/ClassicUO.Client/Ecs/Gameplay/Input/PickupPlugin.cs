using System;
using ClassicUO.Network;
using TinyEcs;

namespace ClassicUO.Ecs;

[TinyPlugin]
internal readonly partial struct PickupPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new GrabbedItem());
    }


    private static bool OnResourcesExists(SchedulerState sched) => sched.ResourceExists<SelectedEntity>() && sched.ResourceExists<GrabbedItem>();

    private static bool OnGrabbedItemIsZero(Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0;

    private static bool OnSelectedEntityIsValid(World world, Res<SelectedEntity> selectedEnt, Query<Data<NetworkSerial>, Filter<With<Items>>> q)
    {
        if (!selectedEnt.Value.Entity.IsValid())
            return false;
        if (!world.Exists(selectedEnt.Value.Entity))
            return false;

        var entity = world.Entity(selectedEnt.Value.Entity);
        return q.Contains(entity.ID);
    }

    private static bool OnMouseIsMoved(Res<MouseContext> mouseCtx)
    {
        if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
        {
            var offset = mouseCtx.Value.PositionOffset;
            if (offset.Length() > 1)
                return true;
        }

        return false;
    }

    private static bool OnMouseIsMovedInversed(Res<MouseContext> mouseCtx)
    {
        if (mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
        {
            var offset = mouseCtx.Value.PositionOffset;
            if (offset.Length() > 1)
                return false;
        }

        return true;
    }

    private static bool OnPickupItemCheckDelay(Local<float?> delay, Time time, Res<MouseContext> mouseCtx, Res<GrabbedItem> grabbedEntity)
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
    }



    [TinySystem(Stages.Update, ThreadingMode.Single)]
    [RunIf(nameof(OnResourcesExists))]
    [RunIf(nameof(OnGrabbedItemIsZero))]
    [RunIf(nameof(OnSelectedEntityIsValid))]
    [RunIf(nameof(OnMouseIsMoved))]
    private static void PickupItem(
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

    [TinySystem(Stages.Update, ThreadingMode.Single)]
    [RunIf(nameof(OnResourcesExists))]
    [RunIf(nameof(OnGrabbedItemIsZero))]
    [RunIf(nameof(OnSelectedEntityIsValid))]
    [RunIf(nameof(OnMouseIsMovedInversed))]
    [RunIf(nameof(OnPickupItemCheckDelay))]
    private static void PickupItemDelayed(
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


    private static bool OnGrabbedEntityIsNotZero(Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial != 0;

    private static bool IsMouseLeftReleased(Res<MouseContext> mouseCtx) => mouseCtx.Value.IsReleased(Input.MouseButtonType.Left);

    [TinySystem(Stages.Update, ThreadingMode.Single)]
    [RunIf(nameof(OnResourcesExists))]
    [RunIf(nameof(OnGrabbedEntityIsNotZero))]
    [RunIf(nameof(IsMouseLeftReleased))]
    private static void DropItem(
        World world,
        Res<SelectedEntity> selectedEntity,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network
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
