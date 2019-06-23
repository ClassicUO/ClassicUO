#region license

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

using ClassicUO.Game.Managers;

namespace ClassicUO.Game.GameObjects
{
    internal sealed class House : IEquatable<Serial>
    {
        public House(Serial serial, uint revision, bool isCustom)
        {
            Serial = serial;
            Revision = revision;
            IsCustom = isCustom;
        }

        public Serial Serial { get; }
        public uint Revision { get; set; }
        public List<Multi> Components { get; } = new List<Multi>();
        public bool IsCustom { get; set; }


        public bool Equals(Serial other)
        {
            return Serial == other;
        }

        public void Generate(bool recalculate = false)
        {
            Item item = World.Items.Get(Serial);

            foreach (Multi s in Components)
            {
                if (item != null)
                {
                    if (recalculate)
                        s.Position = new Position((ushort) (item.X + s.MultiOffsetX), (ushort) (item.Y + s.MultiOffsetY), (sbyte) (item.Position.Z + s.MultiOffsetZ));
                    s.Hue = item.Hue;
                }

                s.AddToTile();
            }
        }

        public void ClearComponents()
        {
            Item item = World.Items.Get(Serial);

            if (item != null && !item.IsDestroyed)
                item.WantUpdateMulti = true;


            foreach (Multi s in Components)
            {
                s.Destroy();
            }

            Components.Clear();
        }
    }
}