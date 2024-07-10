using System.Collections;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using TinyEcs;

namespace ClassicUO.Ecs;

struct PlayerMovementContext
{
    public byte Sequence;
    public Direction Direction;
    public float LastStep;
}

struct PlayerMovementResponse
{
    public bool Accepted;
    public byte Sequence;
}

readonly struct PlayerMovementPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new PlayerMovementContext());
        scheduler.AddEvent<PlayerMovementResponse>();

        scheduler.AddSystem((
            Res<MouseContext> mouseCtx,
            Res<NetClient> network,
            Res<PlayerMovementContext> moveCtx,
            Time time,
            Res<Queue<(int, int)>> req) =>
        {
            if (mouseCtx.Value.NewState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                if (moveCtx.Value.LastStep < time.Total)
                {
                    network.Value.Send_WalkRequest(
                        moveCtx.Value.Direction & Direction.Running,
                        moveCtx.Value.Sequence,
                        (moveCtx.Value.Direction & Direction.Running) != 0,
                        0
                    );

                    moveCtx.Value.Sequence = (byte)((moveCtx.Value.Sequence % byte.MaxValue) + 1);
                    moveCtx.Value.LastStep = time.Total + 200 / 1000.0f;
                }
            }
        });

        scheduler.AddSystem((EventReader<PlayerMovementResponse> responses) =>
        {
            foreach (var response in responses)
            {
                if (response.Accepted)
                {

                }
            }
        }).RunIf((EventReader<PlayerMovementResponse> responses) => !responses.IsEmpty);
    }
}