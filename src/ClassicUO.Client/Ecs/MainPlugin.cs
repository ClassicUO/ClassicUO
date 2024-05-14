using TinyEcs;

namespace ClassicUO.Ecs;

readonly struct MainPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new GameContext() { Map = -1 });

        scheduler.AddPlugin(new FnaPlugin() {
            WindowResizable = true,
            MouseVisible = true,
            VSync = true, // don't kill the gpu
        });

        scheduler.AddPlugin<CuoPlugin>();
    }
}