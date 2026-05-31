using System.Runtime.CompilerServices;
using ClassicUO.Network;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct UseObjectPlugin : IPlugin
{
    public void Build(App app)
    {
        var useObjectFn = UseObject;

        app
            .AddSystem(useObjectFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            // A double-click while a target cursor is up belongs to targeting,
            // not double-click-to-use.
            .RunIf((Res<TargetingState> targeting) => !targeting.Value.IsTargeting)
            .RunIf((Res<MouseContext> mouseCtx) => mouseCtx.Value.IsPressedDouble(Input.MouseButtonType.Left))
            .Build();
    }

    private static void UseObject(
        Res<MouseContext> mouseContext,
        Res<SelectedEntity> selectedEntity,
        Res<NetClient> network,
        Query<Data<NetworkSerial>> query,
        Query<Data<ContainerItemUI>> uiItemQ
    )
    {
        var target = selectedEntity.Value.Entity;

        // Container slot UIs aren't game entities — pull the serial straight
        // off ContainerItemUI and double-click that (mirrors legacy
        // ItemGump.OnMouseDoubleClick which calls GameActions.DoubleClick
        // on LocalSerial).
        if (uiItemQ.Contains(target))
        {
            var (_, link) = uiItemQ.Get(target);
            if (link.Ref.Serial != 0)
                network.Value.Send_DoubleClick(link.Ref.Serial);
            return;
        }

        if (!query.Contains(target))
            return;

        (var ent, var serial) = query.Get(target);
        if (serial.IsValid())
        {
            network.Value.Send_DoubleClick(serial.Ref.Value);
        }
    }
}
