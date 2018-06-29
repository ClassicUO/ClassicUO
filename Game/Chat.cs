using ClassicUO.Game.WorldObjects;
using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Text;

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

       // public static void Print(string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => Print(_system, message, hue, type, font);
       // public static void Print(this Entity entity, string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => new PMessageUnicode(entity.Serial, entity.Graphic, type, hue, font, _language, entity.Name ?? string.Empty, message).SendToClient();
        //public static void Say(string message, ushort hue = defaultHue, MessageType type = MessageType.Regular, MessageFont font = MessageFont.Normal) => new PMessageUnicodeRequest(type, hue, font, _language, message).SendToServer();
        //public static void SayParty(string message) => new PPartyMessage(message).SendToServer();


        public static event EventHandler<UOMessageEventArgs> Message;
        public static event EventHandler<UOMessageEventArgs> LocalizedMessage;
        public static void OnMessage(Entity entity, UOMessageEventArgs args)
        {
            Message.Raise(args, entity ?? _system);
        }

        public static void OnLocalizedMessage(Entity entity, UOMessageEventArgs args)
        {
            LocalizedMessage.Raise(args, entity ?? _system);
        }
    }

    public class UOMessageEventArgs : EventArgs
    {
        public UOMessageEventArgs(string text, Hue hue, MessageType type, MessageFont font, string lang = null)
        {
            Text = text;
            Hue = hue;
            Type = type;
            Font = font;
            Language = lang;
            AffixType = AffixType.None;
        }

        public UOMessageEventArgs(string text, Hue hue, MessageType type, MessageFont font,
            uint cliloc, AffixType affixType = AffixType.None, string affix = null)
        {
            Text = text;
            Hue = hue;
            Type = type;
            Font = font;
            Cliloc = cliloc;
            AffixType = affixType;
            Affix = affix;
        }

        public string Text { get; private set; }
        public Hue Hue { get; private set; }
        public MessageType Type { get; private set; }
        public MessageFont Font { get; private set; }
        public string Language { get; private set; }
        public uint Cliloc { get; private set; }
        public AffixType AffixType { get; private set; }
        public string Affix { get; private set; }
    }

}
