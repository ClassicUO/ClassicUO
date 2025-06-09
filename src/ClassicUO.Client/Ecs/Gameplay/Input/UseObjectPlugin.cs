using System.Runtime.CompilerServices;
using ClassicUO.Network;
using TinyEcs;

namespace ClassicUO.Ecs;


[TinyPlugin]
internal readonly partial struct UseObjectPlugin
{
    public void Build(Scheduler scheduler)
    {

    }


    private static bool IsLeftDoublePressed(Res<MouseContext> mouseCtx) => mouseCtx.Value.IsPressedDouble(Input.MouseButtonType.Left);

    [TinySystem(Stages.Update, ThreadingMode.Single)]
    [RunIf(nameof(IsLeftDoublePressed))]
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
