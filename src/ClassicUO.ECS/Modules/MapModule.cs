// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.ECS.Systems;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Map module: map index changes, season updates, ground entity cleanup.
    /// </summary>
    public struct MapModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<MapModule>();

            // ── Command types ─────────────────────────────────────────
            world.Component<CmdChangeSeason>();

            // ── Register map systems ──────────────────────────────────
            MapSystems.Register(world);
        }
    }
}
