// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.UI.HealthBars
{
    /// <summary>
    /// Per-mobile gate for the health-line overlay. One responsibility:
    /// decide whether a mobile should have its bar drawn and produce the
    /// flags the renderer needs (passive / new-target-system / force-draw).
    /// </summary>
    internal interface IHealthBarVisibilityFilter
    {
        /// <summary>
        /// Returns <c>true</c> when <paramref name="mobile"/> qualifies for a
        /// health bar this frame. <paramref name="decision"/> carries the
        /// notoriety / target / draw-style flags the renderer needs.
        /// </summary>
        bool TryGetDecision(
            Mobile mobile,
            int showWhen,
            bool useNewTargetSystem,
            out HealthBarDecision decision);
    }

    /// <summary>
    /// Outcome of <see cref="IHealthBarVisibilityFilter.TryGetDecision"/>.
    /// Pure data; renderer reads it as-is.
    /// </summary>
    internal readonly struct HealthBarDecision
    {
        public HealthBarDecision(bool passive, bool newTargetSystem, bool forceDraw)
        {
            Passive = passive;
            NewTargetSystem = newTargetSystem;
            ForceDraw = forceDraw;
        }

        /// <summary>Half-alpha overlay (not the player and not a target).</summary>
        public bool Passive { get; }

        /// <summary>Render new-target-system gump frame on top of bar.</summary>
        public bool NewTargetSystem { get; }

        /// <summary>Always render even when the mobile-HP mode would skip it.</summary>
        public bool ForceDraw { get; }
    }
}
