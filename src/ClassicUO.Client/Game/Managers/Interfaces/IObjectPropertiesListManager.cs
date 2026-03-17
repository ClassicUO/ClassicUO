// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Managers
{
    internal interface IObjectPropertiesListManager
    {
        void Add(uint serial, uint revision, string name, string data, int namecliloc);

        bool Contains(uint serial);

        bool IsRevisionEquals(uint serial, uint revision);

        bool TryGetRevision(uint serial, out uint revision);

        bool TryGetNameAndData(uint serial, out string name, out string data);

        int GetNameCliloc(uint serial);

        void Remove(uint serial);

        void Clear();
    }
}
