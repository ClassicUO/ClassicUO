// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Plugin bridge module: plugin-facing compatibility layer.
    /// Registers components used by plugin bridge APIs on EcsRuntimeHost.
    /// Plugin queries are served via EcsRuntimeHost snapshot methods
    /// (GetMobileSnapshot, GetItemSnapshot, GetPlayerPosition, etc.)
    /// rather than direct ECS access.
    /// </summary>
    public struct PluginBridgeModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<PluginBridgeModule>();

            // Plugin bridge uses snapshot-based query APIs on EcsRuntimeHost.
            // No additional components or systems needed — all plugin reads
            // go through EcsRuntimeHost.GetMobileSnapshot / GetItemSnapshot /
            // GetPlayerPosition / IsMobile / IsItem.
        }
    }
}
