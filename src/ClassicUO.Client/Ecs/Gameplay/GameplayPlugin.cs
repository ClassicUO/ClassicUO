using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct GameplayPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddPlugin<ChatPlugin>();
        app.AddPlugin<TextOverheadPlugin>();
        app.AddPlugin<PickupPlugin>();
        app.AddPlugin<UseObjectPlugin>();
        app.AddPlugin<MobAnimationsPlugin>();
        app.AddPlugin<PlayerMovementPlugin>();
        app.AddPlugin<ContainersPlugin>();
        app.AddPlugin<NetworkEntitiesMapPlugin>();
    }
}
