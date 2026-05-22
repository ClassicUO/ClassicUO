// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.UI.HealthBars
{
    /// <summary>
    /// Implements the per-mobile gating rules used by
    /// <see cref="HealthLinesManager"/>:
    /// destroyed-check, target overrides, show-when mode + max-HP guard.
    /// Off-screen culling stays in the renderer because it depends on
    /// camera transforms; this class is purely about logical visibility.
    /// </summary>
    internal sealed class HealthBarVisibilityFilter : IHealthBarVisibilityFilter
    {
        private readonly World _world;

        public HealthBarVisibilityFilter(World world)
        {
            _world = world;
        }

        public bool TryGetDecision(
            Mobile mobile,
            int showWhen,
            bool useNewTargetSystem,
            out HealthBarDecision decision)
        {
            decision = default;

            if (mobile == null || mobile.IsDestroyed)
            {
                return false;
            }

            bool newTargSystem = false;
            bool forceDraw = false;
            bool passive = mobile.Serial != _world.Player.Serial;

            var tm = _world.TargetManager;
            if (tm.LastTargetInfo.Serial == mobile ||
                tm.LastAttack == mobile ||
                tm.SelectedTarget == mobile ||
                tm.NewTargetSystemSerial == mobile)
            {
                newTargSystem = useNewTargetSystem && tm.NewTargetSystemSerial == mobile;
                passive = false;
                forceDraw = true;
            }

            if (!newTargSystem)
            {
                int current = mobile.Hits;
                int max = mobile.HitsMax;

                if (max == 0)
                {
                    return false;
                }

                if (showWhen == 1 && current == max)
                {
                    return false;
                }
            }

            decision = new HealthBarDecision(passive, newTargSystem, forceDraw);
            return true;
        }
    }
}
