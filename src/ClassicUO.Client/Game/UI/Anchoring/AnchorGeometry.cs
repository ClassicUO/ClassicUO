// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.Anchoring
{
    /// <summary>
    /// Default implementation of <see cref="IAnchorGeometry"/>: an
    /// axis-aligned bounding-box overlap test that mirrors the logic
    /// previously inlined in <see cref="AnchorManager"/>.
    /// </summary>
    internal sealed class AnchorGeometry : IAnchorGeometry
    {
        public bool IsOverlapping(AnchorableGump control, AnchorableGump host)
        {
            if (control == host)
            {
                return false;
            }

            if (control.Bounds.Top > host.Bounds.Bottom || control.Bounds.Bottom < host.Bounds.Top)
            {
                return false;
            }

            if (control.Bounds.Right < host.Bounds.Left || control.Bounds.Left > host.Bounds.Right)
            {
                return false;
            }

            return true;
        }
    }
}
