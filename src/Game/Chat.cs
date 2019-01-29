#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Utility;

namespace ClassicUO.Game
{
    [Flags]
    public enum MessageType : byte
    {
        Regular = 0,
        System = 1,
        Emote = 2,
        Party = 3,
        Label = 6,
        Focus = 7,
        Whisper = 8,
        Yell = 9,
        Spell = 10,
        Guild = 13,
        Alliance = 14,
        Command = 15,
        Encoded = 0xC0,
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

   

    internal static class Chat
    {
        private const ushort defaultHue = 0x0017;
        private static readonly Mobile _system = new Mobile(Serial.INVALID)
        {
            Graphic = Graphic.INVARIANT, Name = "System"
        };

        public static PromptData PromptData { get; set; }

        public static event EventHandler<UOMessageEventArgs> Message;

        public static event EventHandler<UOMessageEventArgs> LocalizedMessage;

        public static void Print(string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => Print(_system, message, hue, type, font);
        public static void Print(this Entity entity, string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal, bool unicode = true) => OnMessage(entity, message, entity.Name, hue, type, font, unicode, "ENU");

        public static void Say(string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => GameActions.Say(message, hue, type, font);
    
        public static void OnMessage(Entity parent, string text, string name, Hue hue, MessageType type, MessageFont font, bool unicode = false, string lang = null)
        {
			switch (type)
			{
			    case MessageType.Focus:
			    case MessageType.Whisper:
			    case MessageType.Yell:
                case MessageType.Spell:
				case MessageType.Label:
				case MessageType.Regular:
				    parent?.AddOverhead(type, text, (byte)font, hue, unicode);
					break;
				case MessageType.Emote:
				    parent?.AddOverhead(type, $"*{text}*", (byte)font, hue, unicode);
					break;			
				case MessageType.Command:

				    break;
				case MessageType.Encoded:

				    break;
				case MessageType.System:
				case MessageType.Party:
				    //text = $"[Party][{parent.Name}]: {text}";

				    //break;
				case MessageType.Guild:
				    //text = $"[Guild][{parent.Name}]: {text}";

				    //break;
                case MessageType.Alliance:
                    //text = $"[Alliance][{parent.Name}]: {text}";

                    break;
                default:
                    parent?.AddOverhead(type, text, (byte)font, hue, unicode);
                    break;
			}

			Message.Raise(new UOMessageEventArgs(parent, text, name, hue, type, font, unicode, lang), parent ?? _system);
		}

		public static void OnLocalizedMessage(Entity entity, UOMessageEventArgs args)
        {
            LocalizedMessage.Raise(args, entity ?? _system);
        }

	}

	internal class UOMessageEventArgs : EventArgs
    {
        public UOMessageEventArgs(Entity parent, string text, string name, Hue hue, MessageType type, MessageFont font, bool unicode = false, string lang = null)
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
        }

        public UOMessageEventArgs(Entity parent, string text, Hue hue, MessageType type, MessageFont font, uint cliloc, bool unicode = false, AffixType affixType = AffixType.None, string affix = null)
        {
            Parent = parent;
            Text = text;
            Hue = hue;
            Type = type;
            Font = font;
            Cliloc = cliloc;
            AffixType = affixType;
            Affix = affix;
            IsUnicode = unicode;
        }

        public Entity Parent { get; }

        public string Text { get; }

        public string Name { get; }

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