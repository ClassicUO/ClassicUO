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

namespace ClassicUO.Game.GameObjects
{
    public sealed class Multi
    {
        public Multi(Item parent) => Parent = parent;

        public Item Parent { get; }

        public short MinX { get; set; }
        public short MaxX { get; set; }
        public short MinY { get; set; }
        public short MaxY { get; set; }

        public MultiComponent[] Components { get; set; }
    }

    public struct MultiComponent
    {
        public MultiComponent(Graphic graphic, ushort x, ushort y, sbyte z, uint flags)
        {
            Graphic = graphic;
            Position = new Position(x, y, z);
            Flags = flags;
        }

        public Graphic Graphic { get; }
        public uint Flags { get; }
        public Position Position { get; set; }
    }
}