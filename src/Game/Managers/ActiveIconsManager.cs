using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    class ActiveIconsManager
    {
        private readonly HashSet<ushort> _activeIcons = new HashSet<ushort>();

        public void Add(ushort id)
        {
            if (!IsActive(id))
                _activeIcons.Add(id);
        }

        public void Remove(ushort id)
        {
            if (IsActive(id))
                _activeIcons.Remove(id);
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
