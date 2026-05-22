// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Opl
{
    /// <summary>
    /// Per-serial Object Property List cache. One responsibility: store and
    /// look up Name / Properties / Revision / NameCliloc keyed by entity
    /// serial. No event subscriptions, no network, no UI.
    /// </summary>
    internal interface IOplCache
    {
        void Set(uint serial, uint revision, string name, string data, int nameCliloc);
        bool Contains(uint serial);
        bool IsRevisionEquals(uint serial, uint revision);
        bool TryGetRevision(uint serial, out uint revision);
        bool TryGetNameAndData(uint serial, out string name, out string data);
        int GetNameCliloc(uint serial);
        void Remove(uint serial);
        void Clear();
    }
}
