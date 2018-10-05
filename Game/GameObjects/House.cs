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
using System.Collections.Generic;
using ClassicUO.Game.Map;

namespace ClassicUO.Game.GameObjects
{
    public class House : Item
    {
        public House(Serial serial) : base(serial) => Items = new List<Static>();

        public uint Revision { get; set; }

        public new List<Static> Items { get; }

        public void GenerateCustom()
        {
            foreach (Static s in Items)
            {
                Tile tile = World.Map.GetTile(s.Position.X, s.Position.Y);
                tile.AddGameObject(s);
            }
        }

        public void GenerateOriginal(Multi multi)
        {
            foreach (MultiComponent c in multi.Components)
            {
                Tile tile = World.Map.GetTile(c.Position.X, c.Position.Y);
                tile.AddGameObject(new Static(c.Graphic, 0, 0) {Position = c.Position});
            }
        }

        public override void Dispose()
        {
            Clear();
            base.Dispose();
        }

        public void Clear()
        {
            //Items.ForEach(s => s.Dispose());
            Items.Clear();
        }
    }
}