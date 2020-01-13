#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
        public PopupMenuData(uint serial, PopupMenuItem[] items)
        {
            Serial = serial;
            Items = items;
        }

        public PopupMenuItem[] Items { get; }

        public uint Serial { get; }

        public PopupMenuItem this[int i] => Items[i];

        public static PopupMenuData Parse(Packet p)
        {
            ushort mode = p.ReadUShort();
            bool isNewCliloc = mode >= 2;
            uint serial = p.ReadUInt();
            byte count = p.ReadByte();
            PopupMenuItem[] items = new PopupMenuItem[count];

            for (int i = 0; i < count; i++)
            {
                ushort hue = 0xFFFF, replaced = 0;
                int cliloc;
                ushort index, flags;

                if (isNewCliloc)
                {
                    cliloc = (int) p.ReadUInt();
                    index = p.ReadUShort();
                    flags = p.ReadUShort();
                }
                else
                {
                    index = p.ReadUShort();
                    cliloc = p.ReadUShort() + 3000000;
                    flags = p.ReadUShort();

                    if ((flags & 0x84) != 0)
                        p.Skip(2);

                    if ((flags & 0x40) != 0)
                        p.Skip(2);

                    if ((flags & 0x20) != 0)
                        replaced = (ushort) (p.ReadUShort() & 0x3FFF);
                }

                if ((flags & 0x01) != 0)
                    hue = 0x0386;

                items[i] = new PopupMenuItem(cliloc, index, hue, replaced, flags);
            }

            return new PopupMenuData(serial, items);
        }
    }
}