using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Network;

namespace ClassicUO.Game.Data
{
    class PopupMenuData
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
                        item.ReplacedHue = (Hue)(p.ReadUShort() & 0x3FFF);
                }

                if ((item.Flags & 0x01) != 0)
                    item.Hue = 0x0386;
            }

            return new PopupMenuData(serial, items); ;
        }
    }
}
