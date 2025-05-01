using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct GameplayPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddPlugin<ChatPlugin>();
        scheduler.AddPlugin<PickupPlugin>();
        scheduler.AddPlugin<UseObjectPlugin>();
        scheduler.AddPlugin<MobAnimationsPlugin>();
        scheduler.AddPlugin<PlayerMovementPlugin>();
    }
}