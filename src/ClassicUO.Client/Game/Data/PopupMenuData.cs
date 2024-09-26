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

using ClassicUO.IO;
using ClassicUO.Network;

namespace ClassicUO.Game.Data
{
    public class PopupMenuData
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