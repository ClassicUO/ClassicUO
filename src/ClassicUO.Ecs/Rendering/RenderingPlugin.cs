using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;


internal readonly struct RenderingPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddPlugin<WorldRenderingPlugin>();
        app.AddPlugin<GuiRenderingPlugin>();
        app.AddPlugin<CursorPlugin>();
    }
}
