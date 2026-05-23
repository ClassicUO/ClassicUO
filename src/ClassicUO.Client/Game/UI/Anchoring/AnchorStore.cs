// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.Anchoring
{
    /// <summary>
    /// Dictionary-backed default implementation of <see cref="IAnchorStore"/>.
    /// Mirrors the legacy <c>reverseMap</c> field that previously lived on
    /// <see cref="AnchorManager"/>.
    /// </summary>
    internal sealed class AnchorStore : IAnchorStore
    {
        private readonly Dictionary<AnchorableGump, AnchorManager.AnchorGroup> _reverseMap =
            new Dictionary<AnchorableGump, AnchorManager.AnchorGroup>();

        public AnchorManager.AnchorGroup Get(AnchorableGump control)
        {
            _reverseMap.TryGetValue(control, out AnchorManager.AnchorGroup group);
            return group;
        }

        public void Set(AnchorableGump control, AnchorManager.AnchorGroup group)
        {
            if (_reverseMap.ContainsKey(control) && group == null)
            {
                _reverseMap.Remove(control);
            }
            else
            {
                _reverseMap.Add(control, group);
            }
        }

        public IEnumerable<AnchorManager.AnchorGroup> DistinctGroups()
        {
            return _reverseMap.Values.Distinct();
        }

        public List<AnchorableGump> GetControlsInGroup(AnchorManager.AnchorGroup group)
        {
            return _reverseMap.Where(o => o.Value == group).Select(o => o.Key).ToList();
        }
    }
}
