// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Game.Managers
{
    //enum MessageFont : byte
    //{
    //    INVALID = 0xFF,
    //    Bold = 0,
    //    Shadow = 1,
    //    BoldShadow = 2,
    //    Normal = 3,
    //    Gothic = 4,
    //    Italic = 5,
    //    SmallDark = 6,
    //    Colorful = 7,
    //    Rune = 8,
    //    SmallLight = 9
    //}

    internal enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02,
        None = 0xFF
    }


    internal sealed class MessageManager
    {
        private readonly World _world;

        public MessageManager(World world) => _world = world;


        public PromptData PromptData { get; set; }

        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<MessageEventArgs> LocalizedMessageReceived;


        public void HandleMessage
        (
            Entity parent,
            string text,
            string name,
            ushort hue,
            MessageType type,
            byte font,
            TextType textType,
            bool unicode = false,
            string lang = null
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;

            if (currentProfile != null && currentProfile.OverrideAllFonts)
            {
                font = currentProfile.ChatFont;
                unicode = currentProfile.OverrideAllFontsIsUnicode;
            }

            switch (type)
            {
                case MessageType.Command:
                case MessageType.Encoded:
                case MessageType.System:
                case MessageType.Party:
                    if (!currentProfile.OverheadPartyMessages)
                        break;

                    if (parent == null) //No parent entity, need to check party members by name
                    {
                        foreach (PartyMember member in _world.Party.Members)
                            if (member != null)
                                if (member.Name == name) //Name matches message from server
                                {
                                    Mobile m = _world.Mobiles.Get(member.Serial);
                                    if (m != null) //Mobile exists
                                    {
                                        parent = m;
                                        break;
                                    }
                                }
                    }

                    if (type != MessageType.Spell && parent != null && _world.IgnoreManager.IgnoredCharsList.Contains(parent.Name))
                        break;

                    //Add null check in case parent was not found above.
                    parent?.AddMessage(CreateMessage
                    (
                        text,
                        hue,
                        font,
                        unicode,
                        type,
                        textType
                    ));
                    break;

                case MessageType.Guild:
                    if (currentProfile.IgnoreGuildMessages) return;
                    break;

                case MessageType.Alliance:
                    if (currentProfile.IgnoreAllianceMessages) return;
                    break;

                case MessageType.Spell:
                {
                    //server hue color per default
                    if (!string.IsNullOrEmpty(text) && SpellDefinition.WordToTargettype.TryGetValue(text, out SpellDefinition spell))
                    {
                        if (currentProfile != null && currentProfile.EnabledSpellFormat && !string.IsNullOrWhiteSpace(currentProfile.SpellDisplayFormat))
                        {
                            ValueStringBuilder sb = new ValueStringBuilder(currentProfile.SpellDisplayFormat.AsSpan());
                            {
                                sb.Replace("{power}".AsSpan(), spell.PowerWords.AsSpan());
                                sb.Replace("{spell}".AsSpan(), spell.Name.AsSpan());

                                text = sb.ToString().Trim();
                            }
                            sb.Dispose();
                        }

                        //server hue color per default if not enabled
                        if (currentProfile != null && currentProfile.EnabledSpellHue)
                        {
                            if (spell.TargetType == TargetType.Beneficial)
                            {
                                hue = currentProfile.BeneficHue;
                            }
                            else if (spell.TargetType == TargetType.Harmful)
                            {
                                hue = currentProfile.HarmfulHue;
                            }
                            else
                            {
                                hue = currentProfile.NeutralHue;
                            }
                        }
                    }

                    goto case MessageType.Label;
                }

                default:
                case MessageType.Focus:
                case MessageType.Whisper:
                case MessageType.Yell:
                case MessageType.Regular:
                case MessageType.Label:
                case MessageType.Limit3Spell:

                    if (parent == null)
                    {
                        break;
                    }

                    // If person who send that message is in ignores list - but filter out Spell Text
                    if (_world.IgnoreManager.IgnoredCharsList.Contains(parent.Name) && type != MessageType.Spell)
                        break;

                    TextObject msg = CreateMessage
                    (
                        text,
                        hue,
                        font,
                        unicode,
                        type,
                        textType
                    );

                    msg.Owner = parent;

                    if (parent is Item it && !it.OnGround)
                    {
                        msg.X = _world.DelayedObjectClickManager.X;
                        msg.Y = _world.DelayedObjectClickManager.Y;
                        msg.IsTextGump = true;
                        bool found = false;

                        for (LinkedListNode<Gump> gump = UIManager.Gumps.Last; gump != null; gump = gump.Previous)
                        {
                            Control g = gump.Value;

                            if (!g.IsDisposed)
                            {
                                switch (g)
                                {
                                    case PaperDollGump paperDoll when g.LocalSerial == it.Container:
                                        paperDoll.AddText(msg);
                                        found = true;

                                        break;

                                    case ContainerGump container when g.LocalSerial == it.Container:
                                        container.AddText(msg);
                                        found = true;

                                        break;

                                    case TradingGump trade when trade.ID1 == it.Container || trade.ID2 == it.Container:
                                        trade.AddText(msg);
                                        found = true;

                                        break;
                                }
                            }

                            if (found)
                            {
                                break;
                            }
                        }
                    }

                    parent.AddMessage(msg);

                    break;
            }

            MessageReceived.Raise
            (
                new MessageEventArgs
                (
                    parent,
                    text,
                    name,
                    hue,
                    type,
                    font,
                    textType,
                    unicode,
                    lang
                ),
                parent
            );
        }

        public void OnLocalizedMessage(Entity entity, MessageEventArgs args)
        {
            LocalizedMessageReceived.Raise(args, entity);
        }

        public TextObject CreateMessage
        (
            string msg,
            ushort hue,
            byte font,
            bool isunicode,
            MessageType type,
            TextType textType
        )
        {
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.OverrideAllFonts)
            {
                font = ProfileManager.CurrentProfile.ChatFont;
                isunicode = ProfileManager.CurrentProfile.OverrideAllFontsIsUnicode;
            }

            int width = isunicode ? Client.Game.UO.FileManager.Fonts.GetWidthUnicode(font, msg) : Client.Game.UO.FileManager.Fonts.GetWidthASCII(font, msg);

            if (width > 200)
            {
                width = isunicode ?
                    Client.Game.UO.FileManager.Fonts.GetWidthExUnicode
                    (
                        font,
                        msg,
                        200,
                        TEXT_ALIGN_TYPE.TS_LEFT,
                        (ushort) FontStyle.BlackBorder
                    ) :
                    Client.Game.UO.FileManager.Fonts.GetWidthExASCII
                    (
                        font,
                        msg,
                        200,
                        TEXT_ALIGN_TYPE.TS_LEFT,
                        (ushort) FontStyle.BlackBorder
                    );
            }
            else
            {
                width = 0;
            }


            ushort fixedColor = (ushort)(hue & 0x3FFF);

            if (fixedColor != 0)
            {
                if (fixedColor >= 0x0BB8)
                {
                    fixedColor = 1;
                }

                fixedColor |= (ushort)(hue & 0xC000);
            }
            else
            {
                fixedColor = (ushort)(hue & 0x8000);
            }


            TextObject textObject = TextObject.Create(_world);
            textObject.Alpha = 0xFF;
            textObject.Type = type;
            textObject.Hue = fixedColor;

            if (!isunicode && textType == TextType.OBJECT)
            {
                fixedColor = 0x7FFF;
            }

            textObject.RenderedText = RenderedText.Create
            (
                msg,
                fixedColor,
                font,
                isunicode,
                FontStyle.BlackBorder,
                TEXT_ALIGN_TYPE.TS_LEFT,
                width,
                30,
                false,
                false,
                textType == TextType.OBJECT
            );

            textObject.Time = CalculateTimeToLive(textObject.RenderedText);
            textObject.RenderedText.Hue = textObject.Hue;

            return textObject;
        }

        private static long CalculateTimeToLive(RenderedText rtext)
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            if (currentProfile == null)
            {
                return 0;
            }

            long timeToLive;

            if (currentProfile.ScaleSpeechDelay)
            {
                int delay = currentProfile.SpeechDelay;

                if (delay < 10)
                {
                    delay = 10;
                }

                timeToLive = (long) (4000 * rtext.LinesCount * delay / 100.0f);
            }
            else
            {
                long delay = (5497558140000 * currentProfile.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Time.Ticks;

            return timeToLive;
        }
    }
}