// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Houses
{
    /// <summary>Spatial queries against the house store.</summary>
    internal interface IHouseGeometry
    {
        bool IsHouseInRange(uint serial, int distance);
        bool EntityIntoHouse(uint house, GameObject obj);
        bool TryToRemove(uint serial, int distance);
    }
}
