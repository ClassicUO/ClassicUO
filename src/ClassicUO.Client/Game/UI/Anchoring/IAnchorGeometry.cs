// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.Anchoring
{
    /// <summary>
    /// Pure-geometry helpers used by <see cref="AnchorManager"/> to decide
    /// whether two anchorable gumps overlap. Stateless: no references to
    /// gump trees, profiles or matrix storage live here.
    /// </summary>
    internal interface IAnchorGeometry
    {
        /// <summary>
        /// Returns <c>true</c> when the screen-space bounds of
        /// <paramref name="control"/> and <paramref name="host"/> intersect.
        /// Always returns <c>false</c> if both arguments refer to the same
        /// control.
        /// </summary>
        bool IsOverlapping(AnchorableGump control, AnchorableGump host);
    }
}
