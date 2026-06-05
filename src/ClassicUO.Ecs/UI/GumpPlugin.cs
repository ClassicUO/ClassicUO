using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Composition root for all gump windows + the shared window infra they rely
// on (drag/right-click-close/stacking). Gump content is fed by gameplay and
// network state, so GameplayPlugin owns this; the plugins live under UI/.
internal readonly struct GumpPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddPlugin<WindowDragPlugin>();
        app.AddPlugin<ContainerGumpPlugin>();
        app.AddPlugin<PaperdollPlugin>();
        app.AddPlugin<ServerGumpPlugin>();
        app.AddPlugin<TextEntryDialogPlugin>();
    }
}
