// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Map module: map/chunk scope and visibility metadata.
    /// Stub — map systems will be added in PRD-05.
    /// </summary>
    public struct MapModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<MapModule>();
        }
    }
}
