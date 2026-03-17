// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Managers
{
    internal interface IActiveSpellIconsManager
    {
        void Add(ushort id);

        void Remove(ushort id);

        bool IsActive(ushort id);

        void Clear();
    }
}
