// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.HealthBars
{
    /// <summary>
    /// Draws the over-mobile HP overlay: optional <see cref="Mobile.HitsTexture"/>
    /// label above the head, the new-target-system reticule gump, and the bar
    /// itself. No filtering, no per-mobile state.
    /// </summary>
    internal interface IHealthBarRenderer
    {
        /// <summary>
        /// Renders the HitsTexture label above <paramref name="mobile"/>. May
        /// adjust <paramref name="offsetY"/> when the new-target-system frame
        /// will be drawn on top of it. Caller passes the camera bounds and
        /// already-projected anchor point.
        /// </summary>
        void DrawTopLabel(
            UltimaBatcher2D batcher,
            Mobile mobile,
            in Microsoft.Xna.Framework.Point worldAnchor,
            bool newTargetSystem,
            int mode,
            int showWhen,
            float layerDepth,
            ref int offsetY);

        /// <summary>
        /// Renders the bar (background + HP fill, plus new-target reticule when
        /// requested). Coordinates are already screen-space, bar-anchored.
        /// </summary>
        void DrawHealthLine(
            UltimaBatcher2D batcher,
            Entity entity,
            int x,
            int y,
            int offsetY,
            bool passive,
            bool newTargetSystem,
            float layerDepth);
    }
}
