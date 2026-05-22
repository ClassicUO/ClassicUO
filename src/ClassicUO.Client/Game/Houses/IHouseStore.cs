// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Houses
{
    /// <summary>House dictionary store. One responsibility: add / remove / lookup.</summary>
    internal interface IHouseStore
    {
        IReadOnlyCollection<House> Houses { get; }
        bool TryGetHouse(uint serial, out House house);
        bool Exists(uint serial);
        void Add(uint serial, House house);
        void Remove(uint serial);
        void RemoveMultiTargetHouse();
        void Clear();
    }
}
