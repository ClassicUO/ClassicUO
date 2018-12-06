#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

namespace ClassicUO.Game.GameObjects
{
    public static class HouseManager
    {
        private static readonly Dictionary<Serial, House> _houses = new Dictionary<Serial, House>();

        public static void Add(Serial serial, House revision)
        {
            _houses[serial] = revision;
        }

        public static bool TryGetHouse(Serial serial, out House house)
        {
            return _houses.TryGetValue(serial, out house);
        }

        public static void Remove(Serial serial) => _houses.Remove(serial);

        public static bool Exists(Serial serial) => _houses.ContainsKey(serial);

        public static void Clear() => _houses.Clear();
    }

    public sealed class House : IEquatable<Serial>, IDisposable
    {
        public House(Serial serial, uint revision, bool isCustom)
        {
            Serial = serial;
            Revision = revision;
            IsCustom = isCustom;
        }

        public Serial Serial { get; }
        public uint Revision { get; private set; }
        public List<Static> Components { get; } = new List<Static>();
        public bool IsCustom { get; }

        public void SetRevision(uint revision)
        {
            Revision = revision;
        }

        public void Generate()
        {
            if (IsCustom)
            {
                //foreach (Static component in Components)
                //{
                    
                //}
            }
            else
            {
               
                Item item = World.Items.Get(Serial);

                if (item != null)
                {
                    ClearComponents();

                    foreach (MultiComponent component in item.Multi.Components)
                    {
                        new Static(component.Graphic, 0, 0)
                        {
                            Position = component.Position
                        };                        
                    }
                }
            }
        }

        public bool Equals(Serial other)
        {
            return Serial == other;
        }

        public void ClearComponents()
        {
            Components.ForEach(s => s.Dispose());
        }

        public void Dispose()
        {
            Components.ForEach(s => s.Dispose());
            Components.Clear();
        }
    }
}