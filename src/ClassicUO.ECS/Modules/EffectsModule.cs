// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.ECS.Systems;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Effects module: effect lifecycle, animation cycling,
    /// source dependency cleanup, distance pruning.
    /// </summary>
    public struct EffectsModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<EffectsModule>();
            EffectSystems.Register(world);
        }
    }
}
