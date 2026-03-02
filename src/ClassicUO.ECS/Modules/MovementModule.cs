// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.ECS.Systems;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Movement module: step queue processing, position integration,
    /// walk confirm/deny, server-initiated movement.
    /// </summary>
    public struct MovementModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<MovementModule>();
            MovementSystems.Register(world);
        }
    }
}
