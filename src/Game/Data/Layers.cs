using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    public enum Layer : byte
    {
        Invalid = 0x00,
        OneHanded = 0x01,
        TwoHanded = 0x02,
        Shoes = 0x03,
        Pants = 0x04,
        Shirt = 0x05,
        Helmet = 0x06,
        Gloves = 0x07,
        Ring = 0x08,
        Talisman = 0x09,
        Necklace = 0x0A,
        Hair = 0x0B,
        Waist = 0x0C,
        Torso = 0x0D,
        Bracelet = 0x0E,
        Face = 0x0F,
        Beard = 0x10,
        Tunic = 0x11,
        Earrings = 0x12,
        Arms = 0x13,
        Cloak = 0x14,
        Backpack = 0x15,
        Robe = 0x16,
        Skirt = 0x17,
        Legs = 0x18,
        Mount = 0x19,
        ShopBuy = 0x1A,
        ShopResale = 0x1B,
        ShopSell = 0x1C,
        Bank = 0x1D
    }
}
