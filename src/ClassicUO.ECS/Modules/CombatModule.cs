// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.ECS.Systems;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Combat module: attack target tracking, swing handling, damage display.
    /// </summary>
    public struct CombatModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<CombatModule>();

            // ── Combat components ────────────────────────────────────
            world.Component<AttackTarget>();
            world.Component<OverheadDamage>();

            // ── Combat systems ───────────────────────────────────────
            CombatSystems.Register(world);
        }
    }
}
