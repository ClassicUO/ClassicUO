#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using ClassicUO.Utility;
using System;

namespace ClassicUO.Game
{
    [Flags]
    public enum MessageType : byte
    {
        Regular = 0,
        System = 1,
        Emote = 2,
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

        private static readonly Mobile _system = new Mobile(Serial.Invalid) { Graphic = Graphic.Invariant, Name = "System" };

        //public static void Print(string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => Print(_system, message, hue, type, font);
        //public static void Print(this Entity entity, string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => new PUnicodeSpeechRequest(entity.Serial, entity.Graphic, type, hue, font, _language, entity.Name ?? string.Empty, message).SendToClient();
        public static void Say(string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => NetClient.Socket.Send(new PUnicodeSpeechRequest(message, type, font, hue, "ENU"));
        public static void SayParty(string message) => NetClient.Socket.Send(new PPartyMessage(message, World.Player));


        public static event EventHandler<UOMessageEventArgs> Message;
        public static event EventHandler<UOMessageEventArgs> LocalizedMessage;

        public static void OnMessage(Entity entity, UOMessageEventArgs args)
        {
            if (entity != null)
                entity.AddGameText(args.Type, args.Text, (byte)args.Font, args.Hue, args.IsUnicode);
            else
            {
                Service.Get<Log>().Message(LogTypes.Trace, "On System Message: " + args.Text);
                // ADD TO SYSTEM MESSAGE
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
        public UOMessageEventArgs(string text, Hue hue, MessageType type, MessageFont font,  bool unicode = false, string lang = null)
        {
            Text = text;
            Hue = hue;
            Type = type;
            Font = font;
            Language = lang;
            AffixType = AffixType.None;
            IsUnicode = unicode;
        }

        public UOMessageEventArgs(string text, Hue hue, MessageType type, MessageFont font, uint cliloc,  bool unicode = false, AffixType affixType = AffixType.None, string affix = null)
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