#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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
        ShopBuyRestock = 0x1A,
        ShopBuy = 0x1B,
        ShopSell = 0x1C,
        Bank = 0x1D
    }

    public enum TooltipLayers : byte
    {
        Any = 0x00,
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
        Waist = 0x0C,
        Torso = 0x0D,
        Bracelet = 0x0E,
        Tunic = 0x11,
        Earrings = 0x12,
        Arms = 0x13,
        Cloak = 0x14,
        Robe = 0x16,
        Skirt = 0x17,
        Legs = 0x18,
        Body_Group = 0xA1,
        Jewelry_Group = 0xA2,
        Weapon_Group = 0xA3
    }
}