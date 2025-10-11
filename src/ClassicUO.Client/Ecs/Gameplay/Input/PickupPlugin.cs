using System;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct PickupPlugin : IPlugin
{
    public void Build(App app)
    {
        var packetSetup = PacketSetup;
        var pickupItemDelayedFn = PickupItem;
        var pickupItemFn = PickupItem;
        var dropItemFn = DropItem;

        app
            .AddResource(new GrabbedItem())

            .AddSystem(Stage.Startup, packetSetup)

            .AddSystem(pickupItemDelayedFn)
            .InStage(Stage.Update)
            .RunIf(w => w.HasResource<SelectedEntity>() && w.HasResource<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            .RunIf((Res<MouseContext> mouseCtx, Res<Camera> camera, Local<float?> delay, Res<Time> time) =>
            {
                if (!camera.Value.Bounds.Contains((int)mouseCtx.Value.Position.X, (int)mouseCtx.Value.Position.Y))
                {
                    return false;
                }

                if (!mouseCtx.Value.IsPressed(Input.MouseButtonType.Left) && !mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
                {
                    return false;
                }

                var offset = mouseCtx.Value.PositionOffset;
                if (offset.Length() > 1)
                    return false;

                if (mouseCtx.Value.IsReleased(Input.MouseButtonType.Left))
                    delay.Value = null;
                else if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
                    delay.Value = time.Value.Total + 1000f;
                else if (!mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                    return false;

                if (time.Value.Total > delay.Value)
                    return true;

                if (delay.Value.HasValue)
                    Console.WriteLine("waiting time {0}", delay.Value - time.Value.Total);
                return false;
            })
            .RunIf((Res<SelectedEntity> selectedEnt, Query<Data<NetworkSerial>, Filter<With<Items>>> q) =>
            {
                if (!selectedEnt.Value.Entity.IsValid())
                    return false;
                return q.Contains(selectedEnt.Value.Entity);
            })
            .Build()

            .AddSystem(pickupItemFn)
            .InStage(Stage.Update)
            .RunIf(w => w.HasResource<SelectedEntity>() && w.HasResource<GrabbedItem>())
            .RunIf((Res<GrabbedItem> grabbedItem) => grabbedItem.Value.Serial == 0)
            .RunIf((Res<MouseContext> mouseCtx, Res<Camera> camera, Local<float?> delay, Res<Time> time) =>
            {
                if (!camera.Value.Bounds.Contains((int)mouseCtx.Value.Position.X, (int)mouseCtx.Value.Position.Y))
                {
                    return false;
                }

                if (!mouseCtx.Value.IsPressed(Input.MouseButtonType.Left) && !mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
                {
                    return false;
                }

                var offset = mouseCtx.Value.PositionOffset;
                if (offset.Length() > 1)
                    return false;

                if (mouseCtx.Value.IsReleased(Input.MouseButtonType.Left))
                    delay.Value = null;
                else if (mouseCtx.Value.IsPressedOnce(Input.MouseButtonType.Left))
                    delay.Value = time.Value.Total + 1000f;
                else if (!mouseCtx.Value.IsPressed(Input.MouseButtonType.Left))
                    return false;

                if (time.Value.Total > delay.Value)
                    return true;

                if (delay.Value.HasValue)
                    Console.WriteLine("waiting time {0}", delay.Value - time.Value.Total);
                return false;
            })
            .RunIf((Res<SelectedEntity> selectedEnt, Query<Data<NetworkSerial>, Filter<With<Items>>> q) =>
            {
                if (!selectedEnt.Value.Entity.IsValid())
                    return false;
                return q.Contains(selectedEnt.Value.Entity);
            })
            .Build()

            .AddSystem(dropItemFn)
            .InStage(Stage.Update)
            .RunIf(w => w.HasResource<SelectedEntity>() && w.HasResource<GrabbedItem>())
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
            Console.WriteLine("deny move item code: {0:X2}", code);
        };

        // end draggin item
        packetsMap.Value[0x28] = buffer =>
        {
            grabbedItem.Value.Clear();
            Console.WriteLine("end dragging item");
        };

        // drop item ok
        packetsMap.Value[0x29] = buffer =>
        {
            Console.WriteLine("drop item ok");
        };
    }

    static void PickupItem(
        Res<SelectedEntity> selectedEntity,
        Res<GrabbedItem> grabbedItem,
        Res<NetClient> network,
        Query<Data<NetworkSerial, Amount, Graphic, Hue>> q
    )
    {
        (var serial, var amount, var graphic, var hue) = q.Get(selectedEntity.Value.Entity);
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
