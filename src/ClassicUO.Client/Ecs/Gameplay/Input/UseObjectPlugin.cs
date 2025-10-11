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
            .RunIf((Res<MouseContext> mouseCtx) => mouseCtx.Value.IsPressedDouble(Input.MouseButtonType.Left))
            .Build();
    }

    private static void UseObject(
        Res<MouseContext> mouseContext,
        Res<SelectedEntity> selectedEntity,
        Res<NetClient> network,
        Query<Data<NetworkSerial>> query
    )
    {
        if (!query.Contains(selectedEntity.Value.Entity))
            return;

        (var ent, var serial) = query.Get(selectedEntity.Value.Entity);
        if (serial.IsValid())
        {
            network.Value.Send_DoubleClick(serial.Ref.Value);
        }
    }
}
