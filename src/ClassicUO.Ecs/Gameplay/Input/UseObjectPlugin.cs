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

        // Container slot UIs are owned by ContainerGumpPlugin's
        // OnContainerItemDoubleClick observer (On<UiDoubleClick>), which targets
        // the exact element the gesture landed on. Handling them here too — off
        // the current topmost pick — double-fires on a different item when the
        // cursor drifts between the two clicks (visible with tightly-packed grid
        // cells). So bail: world objects only.
        if (uiItemQ.Contains(target))
            return;

        if (!query.TryGet(target, out var serialRow))
            return;

        (var ent, var serial) = serialRow;
        if (serial.IsValid())
        {
            network.Value.Send_DoubleClick(serial.Ref.Value);
        }
    }
}
