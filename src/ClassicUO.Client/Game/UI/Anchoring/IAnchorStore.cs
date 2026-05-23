// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.Anchoring
{
    /// <summary>
    /// Holds the control-to-group association used by
    /// <see cref="AnchorManager"/>. The store is intentionally ignorant of
    /// matrix geometry: it only knows which <see cref="AnchorManager.AnchorGroup"/>
    /// owns a given <see cref="AnchorableGump"/> and how to enumerate the
    /// distinct groups (for save) or the controls inside a group (for
    /// detach / dispose).
    /// </summary>
    internal interface IAnchorStore
    {
        /// <summary>Returns the group that owns the control, or null.</summary>
        AnchorManager.AnchorGroup Get(AnchorableGump control);

        /// <summary>
        /// Associates <paramref name="control"/> with <paramref name="group"/>.
        /// Passing a null group removes any existing association.
        /// </summary>
        void Set(AnchorableGump control, AnchorManager.AnchorGroup group);

        /// <summary>Enumerates every distinct group currently tracked.</summary>
        IEnumerable<AnchorManager.AnchorGroup> DistinctGroups();

        /// <summary>
        /// Returns a snapshot list of every control currently mapped to
        /// <paramref name="group"/>. Callers may mutate the store while
        /// iterating the returned list.
        /// </summary>
        List<AnchorableGump> GetControlsInGroup(AnchorManager.AnchorGroup group);
    }
}
