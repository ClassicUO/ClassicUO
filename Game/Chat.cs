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

using System;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Utility;

namespace ClassicUO.Game
{
    [Flags]
    public enum MessageType : byte
    {
        Regular = 0,
        System = 1,
        Emote = 2,
        Party = 0x10,
        Label = 6,
        Focus = 7,
        Whisper = 8,
        Yell = 9,
        Spell = 10,
        Guild = 13,
        Alliance = 14,
        Command = 15,
        Encoded = 0xC0
    }

    public enum MessageFont : ushort
    {
        INVALID = 0xFFFF,
        Bold = 0,
        Shadow = 1,
        BoldShadow = 2,
        Normal = 3,
        Gothic = 4,
        Italic = 5,
        SmallDark = 6,
        Colorful = 7,
        Rune = 8,
        SmallLight = 9
    }

    public enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02,
        None = 0xFF
    }

    public static class Chat
    {
        private const ushort defaultHue = 0x0017;
        private static readonly Mobile _system = new Mobile(Serial.Invalid) {Graphic = Graphic.Invariant, Name = "System"};

        //public static void Print(string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => Print(_system, message, hue, type, font);
        //public static void Print(this Entity entity, string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => new PUnicodeSpeechRequest(entity.Serial, entity.Graphic, type, hue, font, _language, entity.Name ?? string.Empty, message).SendToClient();
        public static void Say(string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal)
        {
            GameActions.Say(message, hue, type, font);
        }

        public static event EventHandler<UOMessageEventArgs> Message;

        public static event EventHandler<UOMessageEventArgs> LocalizedMessage;

        public static void OnMessage(Entity entity, UOMessageEventArgs args)
        {
            switch (args.Type)
            {
                case MessageType.Regular:

                    if (entity != null && entity.Serial.IsValid)
                    {
                        entity.AddGameText(args.Type, args.Text, (byte) args.Font, args.Hue, args.IsUnicode);
                        Service.Get<JournalData>().AddEntry(args.Text, (byte) args.Font, args.Hue, entity.Name);
                    }
                    else
                    {
                        Service.Get<ChatControl>().AddLine(args.Text, (byte) args.Font, args.Hue, args.IsUnicode);
                        Service.Get<JournalData>().AddEntry(args.Text, (byte) args.Font, args.Hue, "System");
                    }

                    break;
                case MessageType.System:
                    Service.Get<ChatControl>().AddLine(args.Text, (byte) args.Font, args.Hue, args.IsUnicode);
                    Service.Get<JournalData>().AddEntry(args.Text, (byte) args.Font, args.Hue, "System");

                    break;
                case MessageType.Emote:

                    if (entity != null && entity.Serial.IsValid)
                    {
                        entity.AddGameText(args.Type, $"*{args.Text}*", (byte) args.Font, args.Hue, args.IsUnicode);
                        Service.Get<JournalData>().AddEntry($"*{args.Text}*", (byte) args.Font, args.Hue, entity.Name);
                    }
                    else
                    {
                        Service.Get<JournalData>().AddEntry($"*{args.Text}*", (byte) args.Font, args.Hue, "System");
                    }

                    break;
                case MessageType.Label:

                    if (entity != null && entity.Serial.IsValid)
                        entity.AddGameText(args.Type, args.Text, (byte) args.Font, args.Hue, args.IsUnicode);
                    Service.Get<JournalData>().AddEntry(args.Text, (byte) args.Font, args.Hue, "You see");

                    break;
                case MessageType.Focus:

                    break;
                case MessageType.Whisper:

                    break;
                case MessageType.Yell:

                    break;
                case MessageType.Spell:

                    if (entity != null && entity.Serial.IsValid)
                    {
                        entity.AddGameText(args.Type, args.Text, (byte) args.Font, args.Hue, args.IsUnicode);
                        Service.Get<JournalData>().AddEntry(args.Text, (byte) args.Font, args.Hue, entity.Name);
                    }

                    break;
                case MessageType.Party:
                    Service.Get<ChatControl>().AddLine($"[Party] [{entity.Name}]: {args.Text}", (byte) args.Font, args.Hue, args.IsUnicode);
                    Service.Get<JournalData>().AddEntry(args.Text, (byte) args.Font, args.Hue, "Party");

                    break;
                case MessageType.Guild:

                    break;
                case MessageType.Alliance:

                    break;
                case MessageType.Command:

                    break;
                case MessageType.Encoded:

                    break;
                default:

                    throw new ArgumentOutOfRangeException();
            }

            Message.Raise(args, entity ?? _system);
        }

        public static void OnLocalizedMessage(Entity entity, UOMessageEventArgs args)
        {
            LocalizedMessage.Raise(args, entity ?? _system);
        }
    }

    public class UOMessageEventArgs : EventArgs
    {
        public UOMessageEventArgs(string text, Hue hue, MessageType type, MessageFont font, bool unicode = false, string lang = null)
        {
            Text = text;
            Hue = hue;
            Type = type;
            Font = font;
            Language = lang;
            AffixType = AffixType.None;
            IsUnicode = unicode;
        }

        public UOMessageEventArgs(string text, Hue hue, MessageType type, MessageFont font, uint cliloc, bool unicode = false, AffixType affixType = AffixType.None, string affix = null)
        {
            Text = text;
            Hue = hue;
            Type = type;
            Font = font;
            Cliloc = cliloc;
            AffixType = affixType;
            Affix = affix;
            IsUnicode = unicode;
        }

        public string Text { get; }

        public Hue Hue { get; }

        public MessageType Type { get; }

        public MessageFont Font { get; }

        public string Language { get; }

        public uint Cliloc { get; }

        public AffixType AffixType { get; }

        public string Affix { get; }

        public bool IsUnicode { get; }
    }
}