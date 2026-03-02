// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// UI bridge module: UI query adapters and command translators.
    /// Stub — UI bridge systems will be added in PRD-06.
    /// </summary>
    public struct UiBridgeModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<UiBridgeModule>();
        }
    }
}
