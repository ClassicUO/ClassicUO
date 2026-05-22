// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.HealthBars
{
    /// <summary>
    /// Facade over the two HealthBars collaborators
    /// (<see cref="IHealthBarVisibilityFilter"/> and <see cref="IHealthBarRenderer"/>).
    /// Keeps the existing public surface (<c>_world.HealthLinesManager.Draw(...)</c>);
    /// the per-mobile gating and the gump-draw plumbing now live in dedicated
    /// cohesive classes under <see cref="Game.UI.HealthBars"/>.
    /// </summary>
    internal sealed class HealthLinesManager
    {
        private readonly World _world;
        private readonly IHealthBarVisibilityFilter _filter;
        private readonly IHealthBarRenderer _renderer;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public HealthLinesManager(World world)
            : this(world, new HealthBarVisibilityFilter(world), new HealthBarRenderer(world))
        {
        }

        /// <summary>Full DI seam — inject filter + renderer.</summary>
        internal HealthLinesManager(World world, IHealthBarVisibilityFilter filter, IHealthBarRenderer renderer)
        {
            _world = world;
            _filter = filter;
            _renderer = renderer;
        }

        public bool IsEnabled =>
            ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.ShowMobilesHP;

        public void Draw(UltimaBatcher2D batcher, float layerDepth)
        {
            var camera = Client.Game.Scene.Camera;
            int mode = ProfileManager.CurrentProfile.MobileHPType;

            if (mode < 0)
            {
                return;
            }

            int showWhen = ProfileManager.CurrentProfile.MobileHPShowWhen;
            var useNewTargetSystem = ProfileManager.CurrentProfile.UseNewTargetSystem;
            var isEnabled = IsEnabled;

            foreach (Mobile mobile in _world.Mobiles.Values)
            {
                if (!_filter.TryGetDecision(mobile, showWhen, useNewTargetSystem, out var decision))
                {
                    continue;
                }

                Point p = mobile.RealScreenPosition;
                p.X += (int)mobile.Offset.X + 22 + 5;
                p.Y += (int)(mobile.Offset.Y - mobile.Offset.Z) + 22 + 5;
                int offsetY = 0;

                if (isEnabled)
                {
                    _renderer.DrawTopLabel(
                        batcher,
                        mobile,
                        in p,
                        decision.NewTargetSystem,
                        mode,
                        showWhen,
                        layerDepth,
                        ref offsetY);
                }

                p.X -= 5;
                p = camera.WorldToScreen(p, true);

                p.X -= HealthBarRenderer.BAR_WIDTH_HALF;
                p.Y -= HealthBarRenderer.BAR_HEIGHT_HALF;

                if (p.X < 0 || p.X > camera.Bounds.Width - HealthBarRenderer.BAR_WIDTH)
                {
                    continue;
                }

                if (p.Y < 0 || p.Y > camera.Bounds.Height - HealthBarRenderer.BAR_HEIGHT)
                {
                    continue;
                }

                if ((isEnabled && mode >= 1) || decision.NewTargetSystem || decision.ForceDraw)
                {
                    _renderer.DrawHealthLine(
                        batcher,
                        mobile,
                        p.X,
                        p.Y,
                        offsetY,
                        decision.Passive,
                        decision.NewTargetSystem,
                        layerDepth);
                }
            }
        }
    }
}
