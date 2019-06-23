﻿#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;

using ClassicUO.Game.GameObjects;

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
                if (_houses.TryGetValue(serial, out var house))
                {
                    house.ClearComponents();
                    _houses.Remove(serial);
                }
                else
                {

                }
            

                return true;
            }

            return false;
        }

        public bool IsHouseInRange(Serial serial, int distance)
        {
            if (TryGetHouse(serial, out _))
            {
                int currX = World.RangeSize.X;
                int currY = World.RangeSize.Y;

                //if (World.Player.IsMoving)
                //{
                //    Mobile.Step step = World.Player.Steps.Back();

                //    currX = step.X;
                //    currY = step.Y;
                //}
                //else
                //{
                //    currX = World.Player.X;
                //    currY = World.Player.Y;
                //}

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

        public bool Exists(Serial serial)
        {
            return _houses.ContainsKey(serial);
        }

        public void Clear()
        {
            foreach (KeyValuePair<Serial, House> house in _houses) house.Value.ClearComponents();
            _houses.Clear();
        }
    }
}