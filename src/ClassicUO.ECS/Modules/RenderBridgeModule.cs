// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.ECS.Systems;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Render bridge module: render extraction systems that populate
    /// frame-local render components from simulation state.
    /// Runs in the RenderExtract phase each frame.
    /// </summary>
    public struct RenderBridgeModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<RenderBridgeModule>();
            RenderExtractSystems.Register(world);
        }
    }
}
