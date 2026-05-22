// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Houses
{
    /// <summary>
    /// Owns the in-memory dictionary of houses keyed by foundation serial.
    /// Single responsibility: add / remove / lookup. No geometry, no events,
    /// no network.
    /// </summary>
    internal sealed class HouseStore : IHouseStore
    {
        private readonly Dictionary<uint, House> _houses = new();

        public IReadOnlyCollection<House> Houses => _houses.Values;

        public bool TryGetHouse(uint serial, out House house) => _houses.TryGetValue(serial, out house);
        public bool Exists(uint serial) => _houses.ContainsKey(serial);

        public void Add(uint serial, House house) => _houses[serial] = house;

        public void Remove(uint serial)
        {
            if (_houses.TryGetValue(serial, out House house))
            {
                house.ClearComponents();
                _houses.Remove(serial);
            }
        }

        public void RemoveMultiTargetHouse() => Remove(0);

        public void Clear()
        {
            foreach (var (_, house) in _houses)
            {
                house.ClearComponents();
            }

            _houses.Clear();
        }
    }
}
