using System.Runtime.CompilerServices;
using ClassicUO.Network;
using TinyEcs;

namespace ClassicUO.Ecs;


internal readonly struct UseObjectPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        var useObjectFn = UseObject;
        scheduler.OnUpdate(useObjectFn, ThreadingMode.Single)
            .RunIf((Res<MouseContext> mouseCtx) => mouseCtx.Value.IsPressedDouble(Input.MouseButtonType.Left));
    }


    private static void UseObject(
        Res<MouseContext> mouseContext,
        Res<SelectedEntity> selectedEntity,
        Res<NetClient> network,
        Query<
            Data<NetworkSerial>
        > query
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
