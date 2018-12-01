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
using ClassicUO.Network;

namespace ClassicUO.Game.Data
{
    internal class PopupMenuData
    {
        public PopupMenuData(Serial serial, PopupMenuItem[] items)
        {
            Serial = serial;
            Items = items;
        }

        public PopupMenuItem[] Items { get; }

        public Serial Serial { get; }

        public PopupMenuItem this[int i] => Items[i];

        public static PopupMenuData Parse(Packet p)
        {
            ushort mode = p.ReadUShort();
            bool isNewCliloc = mode >= 2;
            Serial serial = p.ReadUInt();
            byte count = p.ReadByte();
            PopupMenuItem[] items = new PopupMenuItem[count];

            for (int i = 0; i < count; i++)
            {
                ref PopupMenuItem item = ref items[i];
                item.Hue = 0xFFFF;

                if (isNewCliloc)
                {
                    item.Cliloc = (int) p.ReadUInt();
                    item.Index = p.ReadUShort();
                    item.Flags = p.ReadUShort();
                }
                else
                {
                    item.Index = p.ReadUShort();
                    item.Cliloc = p.ReadUShort() + 3000000;
                    item.Flags = p.ReadUShort();

                    if ((item.Flags & 0x84) != 0)
                        p.Skip(2);

                    if ((item.Flags & 0x40) != 0)
                        p.Skip(2);

                    if ((item.Flags & 0x20) != 0)
                        item.ReplacedHue = (Hue) (p.ReadUShort() & 0x3FFF);
                }

                if ((item.Flags & 0x01) != 0)
                    item.Hue = 0x0386;
            }

            return new PopupMenuData(serial, items);
            ;
        }
    }
}