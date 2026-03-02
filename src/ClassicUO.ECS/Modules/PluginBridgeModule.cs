// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Plugin bridge module: plugin-facing compatibility layer.
    /// Stub — plugin bridge systems will be added in PRD-06.
    /// </summary>
    public struct PluginBridgeModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<PluginBridgeModule>();
        }
    }
}
