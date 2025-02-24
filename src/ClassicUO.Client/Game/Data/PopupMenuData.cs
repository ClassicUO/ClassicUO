// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
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

        public static PopupMenuData Parse(ref StackDataReader p)
        {
            ushort mode = p.ReadUInt16BE();
            bool isNewCliloc = mode >= 2;
            uint serial = p.ReadUInt32BE();
            byte count = p.ReadUInt8();
            PopupMenuItem[] items = new PopupMenuItem[count];

            for (int i = 0; i < count; i++)
            {
                ushort hue = 0xFFFF, replaced = 0;
                int cliloc;
                ushort index, flags;

                if (isNewCliloc)
                {
                    cliloc = (int) p.ReadUInt32BE();
                    index = p.ReadUInt16BE();
                    flags = p.ReadUInt16BE();
                }
                else
                {
                    index = p.ReadUInt16BE();
                    cliloc = p.ReadUInt16BE() + 3000000;
                    flags = p.ReadUInt16BE();

                    if ((flags & 0x84) != 0)
                    {
                        p.Skip(2);
                    }

                    if ((flags & 0x40) != 0)
                    {
                        p.Skip(2);
                    }

                    if ((flags & 0x20) != 0)
                    {
                        replaced = (ushort) (p.ReadUInt16BE() );
                    }
                }

                if ((flags & 0x01) != 0)
                {
                    hue = 0x0386;
                }

                items[i] = new PopupMenuItem
                (
                    cliloc,
                    index,
                    hue,
                    replaced,
                    flags
                );
            }

            return new PopupMenuData(serial, items);
        }
    }
}