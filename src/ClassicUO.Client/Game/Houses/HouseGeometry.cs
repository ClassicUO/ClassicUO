// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Houses
{
    /// <summary>
    /// Pure spatial queries on houses. Reads <see cref="IHouseStore"/> and the
    /// <see cref="World"/> for player range + item lookup, but mutates nothing
    /// except through the store.
    /// </summary>
    internal sealed class HouseGeometry : IHouseGeometry
    {
        private readonly World _world;
        private readonly IHouseStore _store;

        public HouseGeometry(World world, IHouseStore store)
        {
            _world = world;
            _store = store;
        }

        public bool IsHouseInRange(uint serial, int distance)
        {
            if (!_store.TryGetHouse(serial, out _)) return false;

            int currX = _world.RangeSize.X;
            int currY = _world.RangeSize.Y;

            Item found = _world.Items.Get(serial);
            if (found == null) return true;

            distance += found.MultiDistanceBonus;

            return Math.Abs(found.X - currX) <= distance && Math.Abs(found.Y - currY) <= distance;
        }

        public bool EntityIntoHouse(uint house, GameObject obj)
        {
            if (obj == null || !_store.TryGetHouse(house, out _)) return false;

            Item found = _world.Items.Get(house);
            if (found == null || !found.MultiInfo.HasValue) return true;

            var multi = found.MultiInfo.Value;
            int minX = found.X + multi.X;
            int maxX = found.X + multi.Width;
            int minY = found.Y + multi.Y;
            int maxY = found.Y + multi.Height;

            return obj.X >= minX && obj.X <= maxX && obj.Y >= minY && obj.Y <= maxY;
        }

        public bool TryToRemove(uint serial, int distance)
        {
            if (IsHouseInRange(serial, distance)) return false;
            _store.Remove(serial);
            return true;
        }
    }
}
