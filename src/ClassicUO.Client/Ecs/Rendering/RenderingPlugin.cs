using TinyEcs;

namespace ClassicUO.Ecs;


internal readonly struct RenderingPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddPlugin<WorldRenderingPlugin>();
        scheduler.AddPlugin<TextOverheadPlugin>();
    }
}