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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using System;

namespace ClassicUO.Game.Managers
{
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs
        (
            Entity parent,
            string text,
            string name,
            ushort hue,
            MessageType type,
            byte font,
            TextType text_type,
            bool unicode = false,
            string lang = null
        )
        {
            Parent = parent;
            Text = text;
            Name = name;
            Hue = hue;
            Type = type;
            Font = font;
            Language = lang;
            AffixType = AffixType.None;
            IsUnicode = unicode;
            TextType = text_type;
        }


        public Entity Parent { get; }

        public string Text { get; }

        public string Name { get; }

        public ushort Hue { get; }

        public MessageType Type { get; }

        public byte Font { get; }

        public string Language { get; }

        public uint Cliloc { get; set; }

        public AffixType AffixType { get; }

        public string Affix { get; }

        public bool IsUnicode { get; }

        public TextType TextType { get; }
    }
}