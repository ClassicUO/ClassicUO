using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Managers
{
    internal class HouseManager
    {
        private readonly Dictionary<Serial, House> _houses = new Dictionary<Serial, House>();

        public IReadOnlyCollection<House> Houses => _houses.Values;

        public void Add(Serial serial, House revision)
        {
            _houses[serial] = revision;
        }

        public bool TryGetHouse(Serial serial, out House house)
        {
            return _houses.TryGetValue(serial, out house);
        }

        public bool TryToRemove(Serial serial, int distance)
        {
            if (!IsHouseInRange(serial, distance))
            {
                _houses[serial].ClearComponents();
                _houses.Remove(serial);

                return true;
            }
            return false;
        }

        public bool IsHouseInRange(Serial serial, int distance)
        {
            if (TryGetHouse(serial, out _))
            {
                int currX = World.Player.X;
                int currY = World.Player.Y;

                Item found = World.Items.Get(serial);

                distance += found.MultiDistanceBonus;

                return Math.Abs(found.X - currX) <= distance && Math.Abs(found.Y - currY) <= distance;
            }

            return false;
        }

        public void Remove(Serial serial)
        {
            if (TryGetHouse(serial, out House house))
            {
                house.ClearComponents();
                _houses.Remove(serial);
            }
        }

        public bool Exists(Serial serial) => _houses.ContainsKey(serial);

        public void Clear()
        {
            foreach (KeyValuePair<Serial, House> house in _houses)
            {
                house.Value.ClearComponents();
            }
            _houses.Clear();
        }
    }

}
