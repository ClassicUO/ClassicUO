// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    internal sealed class ActiveSpellIconsManager
    {
        private readonly HashSet<ushort> _activeIcons = new HashSet<ushort>();

        public void Add(ushort id)
        {
            if (!IsActive(id))
            {
                _activeIcons.Add(id);
            }
        }

        public void Remove(ushort id)
        {
            if (IsActive(id))
            {
                _activeIcons.Remove(id);
            }
        }

        public bool IsActive(ushort id)
        {
            return _activeIcons.Count != 0 && _activeIcons.Contains(id);
        }

        public void Clear()
        {
            _activeIcons.Clear();
        }
    }
}