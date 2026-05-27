using ClassicUO.Game.Data;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct NetworkEntitiesMapPlugin : IPlugin
{
    public void Build(App app)
    {
        app
            .AddResource(new NetworkEntitiesMap())

            .AddSystem((
                Commands commands,
                Res<NetworkEntitiesMap> entitiesMap) =>
            {
                foreach ((var serial, var ent) in entitiesMap.Value)
                {
                    commands.Entity(ent).Despawn();
                }

                entitiesMap.Value.Clear();
            })
            .OnExit(GameState.GameScreen)
            .Build()

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
                OnRemove<NetworkSerial> trigger,
                Res<NetworkEntitiesMap> networkEntities
            ) =>
            {
                networkEntities.Value.Remove(trigger.Component.Value);
            });
    }
}
