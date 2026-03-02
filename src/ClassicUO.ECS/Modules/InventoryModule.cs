// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.ECS.Systems;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Inventory module: container cascade destruction, equipment reconciliation.
    /// </summary>
    public struct InventoryModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<InventoryModule>();
            InventorySystems.Register(world);
        }
    }
}
