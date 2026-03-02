// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Combat module: vitals, warmode, notoriety, attack state.
    /// Stub — combat systems will be added in PRD-04.
    /// </summary>
    public struct CombatModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<CombatModule>();
        }
    }
}
