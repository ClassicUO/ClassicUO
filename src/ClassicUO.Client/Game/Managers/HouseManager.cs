#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal sealed class HouseManager
    {
        private readonly Dictionary<uint, House> _houses = new Dictionary<uint, House>();
        private readonly World _world;

        public HouseManager(World world)
        {
            _world = world;
        }

        public IReadOnlyCollection<House> Houses => _houses.Values;

        public void Add(uint serial, House revision)
        {
            _houses[serial] = revision;
        }

        public bool TryGetHouse(uint serial, out House house)
        {
            return _houses.TryGetValue(serial, out house);
        }

        public bool TryToRemove(uint serial, int distance)
        {
            if (!IsHouseInRange(serial, distance))
            {
                if (_houses.TryGetValue(serial, out House house))
                {
                    house.ClearComponents();
                    _houses.Remove(serial);
                }


                return true;
            }

            return false;
        }

        public bool IsHouseInRange(uint serial, int distance)
        {
            if (TryGetHouse(serial, out _))
            {
                int currX = _world.RangeSize.X;
                int currY = _world.RangeSize.Y;

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

                Item found = _world.Items.Get(serial);

                if (found == null)
                {
                    return true;
                }

                distance += found.MultiDistanceBonus;

                return Math.Abs(found.X - currX) <= distance && Math.Abs(found.Y - currY) <= distance;
            }

            return false;
        }

        public bool EntityIntoHouse(uint house, GameObject obj)
        {
            if (obj != null && TryGetHouse(house, out _))
            {
                Item found = _world.Items.Get(house);

                if (found == null || !found.MultiInfo.HasValue)
                {
                    return true;
                }

                int minX = found.X + found.MultiInfo.Value.X;
                int maxX = found.X + found.MultiInfo.Value.Width;
                int minY = found.Y + found.MultiInfo.Value.Y;
                int maxY = found.Y + found.MultiInfo.Value.Height;

                return obj.X >= minX && obj.X <= maxX && obj.Y >= minY && obj.Y <= maxY;
            }

            return false;
        }

        public void Remove(uint serial)
        {
            if (TryGetHouse(serial, out House house))
            {
                house.ClearComponents();
                _houses.Remove(serial);
            }
        }

        public void RemoveMultiTargetHouse()
        {
            if (_houses.TryGetValue(0, out House house))
            {
                house.ClearComponents();
                _houses.Remove(0);
            }
        }

        public bool Exists(uint serial)
        {
            return _houses.ContainsKey(serial);
        }

        public void Clear()
        {
            foreach (KeyValuePair<uint, House> house in _houses)
            {
                house.Value.ClearComponents();
            }

            _houses.Clear();
        }
    }
}