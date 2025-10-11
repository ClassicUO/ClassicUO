using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs.Modding.UI;

internal readonly struct TextPlugin : IPlugin
{
    public void Build(App app)
    {
        var propagateTextConfigFn = PropagateTextConfigToChildTextFragments;
        app.AddSystem(Stage.Update, propagateTextConfigFn);
    }

    private static void PropagateTextConfigToChildTextFragments(
        Query<Data<Text, Children>, Filter<With<PluginEntity>>> query,
        Query<Data<Text>, With<Parent>> queryChildren
    )
    {
        foreach (var (text, children) in query)
        {
            foreach (var childId in children.Ref)
            {
                if (!queryChildren.Contains(childId))
                    continue;

                var (_, textChild) = queryChildren.Get(childId);

                // assign the parent text config to the child text config
                textChild.Ref.TextConfig = text.Ref.TextConfig;
            }
        }
    }
}
