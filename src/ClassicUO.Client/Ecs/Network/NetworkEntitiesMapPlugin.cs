using ClassicUO.Game.Data;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct NetworkEntitiesMapPlugin : IPlugin
{
    public void Build(App app)
    {
        app
            .AddObserver((
                             OnRemove<EquipmentSlots> trigger,
                             Commands commands,
                             Res<NetworkEntitiesMap> networkEntities,
                             Query<Data<NetworkSerial>> query
                         ) =>
                         {
                             for (var layer = Layer.Invalid + 1; layer <= Layer.Bank; ++layer)
                             {
                                 var id = trigger.Component[layer];

                                 if (query.Contains(id))
                                 {
                                     (_, var serial) = query.Get(id);
                                     _ = networkEntities.Value.Remove(serial.Ref.Value);
                                 }

                                 commands.Entity(id).Despawn();
                             }
                         })

            .AddObserver((
                             OnAdd<NetworkSerial> trigger,
                             Commands commands,
                             Res<NetworkEntitiesMap> networkEntities
                         ) =>
                         {
                             // networkEntities.Value.Add(commands, trigger.Component.Value, trigger.EntityId);
                         })

            .AddObserver((
                             OnRemove<NetworkSerial> trigger,
                             Res<NetworkEntitiesMap> networkEntities
                         ) =>
                         {
                             networkEntities.Value.Remove(trigger.Component.Value);
                         })

            ;
    }
}
