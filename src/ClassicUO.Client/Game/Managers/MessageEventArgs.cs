// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal class MessageEventArgs : EventArgs
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

        public uint Cliloc { get; }

        public AffixType AffixType { get; }

        public string Affix { get; }

        public bool IsUnicode { get; }

        public TextType TextType { get; }
    }
}