﻿#region license

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
using System.Linq;
using System.Text;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game
{
    [Flags]
    public enum MessageType : byte
    {
        Regular = 0,
        System = 1,
        Emote = 2,
        Limit3Spell = 3, // Sphere style shards use this to limit to 3 of these message types showing overhead.
        Label = 6,
        Focus = 7,
        Whisper = 8,
        Yell = 9,
        Spell = 10,
        Guild = 13,
        Alliance = 14,
        Command = 15,
        Encoded = 0xC0,
        Party = 0xFF // This is a CUO assigned type, value is unimportant
    }

    //public enum MessageFont : byte
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

    public enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02,
        None = 0xFF
    }



    internal static class Chat
    {
        public static PromptData PromptData { get; set; }

        public static event EventHandler<UOMessageEventArgs> MessageReceived;

        public static event EventHandler<UOMessageEventArgs> LocalizedMessageReceived;


        public static void HandleMessage(Entity parent, string text, string name, Hue hue, MessageType type, byte font, bool unicode = false, string lang = null)
        {
            if (Engine.Profile.Current != null && Engine.Profile.Current.OverrideAllFonts)
            {
                font = Engine.Profile.Current.ChatFont;
                unicode = Engine.Profile.Current.OverrideAllFontsIsUnicode;
            }

            switch (type)
            {
                case MessageType.Spell:

                {
                    //server hue color per default
                    if (!string.IsNullOrEmpty(text) && SpellDefinition.WordToTargettype.TryGetValue(text, out SpellDefinition spell))
                    {
                        if (Engine.Profile.Current != null && Engine.Profile.Current.EnabledSpellFormat && !string.IsNullOrWhiteSpace(Engine.Profile.Current.SpellDisplayFormat))
                        {
                            StringBuilder sb = new StringBuilder(Engine.Profile.Current.SpellDisplayFormat);
                            sb.Replace("{power}", spell.PowerWords);
                            sb.Replace("{spell}", spell.Name);
                            text = sb.ToString().Trim();
                        }

                        //server hue color per default if not enabled
                        if (Engine.Profile.Current != null && Engine.Profile.Current.EnabledSpellHue)
                        {
                            if (spell.TargetType == TargetType.Beneficial)
                                hue = Engine.Profile.Current.BeneficHue;
                            else if (spell.TargetType == TargetType.Harmful)
                                hue = Engine.Profile.Current.HarmfulHue;
                            else
                                hue = Engine.Profile.Current.NeutralHue;
                        }
                    }

                    goto case MessageType.Label;
                }

                case MessageType.Focus:
                case MessageType.Whisper:
                case MessageType.Yell:
                case MessageType.Regular:
                case MessageType.Label:

                    if (parent == null)
                        break;

                    MessageInfo msg = CreateMessage(text, hue, font, unicode, type);
                    msg.Owner = parent;

                    if (parent is Item it && !it.OnGround)
                    {
                        msg.X = Mouse.LastClickPosition.X;
                        msg.Y = Mouse.LastClickPosition.Y;

                        Gump gump = Engine.UI.GetGump<Gump>(it.Container);

                        if (gump is PaperDollGump paperDoll)
                        {
                            msg.X -= paperDoll.ScreenCoordinateX;
                            msg.Y -= paperDoll.ScreenCoordinateY;
                            paperDoll.AddText(msg);
                        }
                        else if (gump is ContainerGump container)
                        {
                            msg.X -= container.ScreenCoordinateX;
                            msg.Y -= container.ScreenCoordinateY;
                            container.AddText(msg);
                        }
                        else
                        {
                            Entity ent = World.Get(it.RootContainer);

                            if (ent == null || ent.IsDestroyed)
                                break;

                            var trade = Engine.UI.GetGump<TradingGump>(ent);

                            if (trade == null)
                            {
                                Item item = ent.Items.FirstOrDefault(s => s.Graphic == 0x1E5E);

                                if (item == null)
                                    break;

                                trade = Engine.UI.Gumps.OfType<TradingGump>().FirstOrDefault(s => s.ID1 == item || s.ID2 == item);
                            }

                            if (trade != null)
                            {
                                msg.X -= trade.ScreenCoordinateX;
                                msg.Y -= trade.ScreenCoordinateY;
                                trade.AddText(msg);
                            }
                            else
                                Log.Message(LogTypes.Warning, "Missing label handler for this control: 'UNKNOWN'. Report it!!");
                        }
                    }

                    parent.AddMessage(msg);

                    break;

                case MessageType.Emote:
                    if (parent == null)
                        break;

                    msg = CreateMessage($"*{text}*", hue, font, unicode, type);

                    parent.AddMessage(msg);

                    break;

                case MessageType.Command:

                case MessageType.Encoded:
                case MessageType.System:
                case MessageType.Party:
                case MessageType.Guild:
                case MessageType.Alliance:

                    break;

                default:
                    if (parent == null)
                        break;

                    msg = CreateMessage(text, hue, font, unicode, type);

                    parent.AddMessage(msg);

                    break;
            }

            MessageReceived.Raise(new UOMessageEventArgs(parent, text, name, hue, type, font, unicode, lang), parent);
        }

        public static void OnLocalizedMessage(Entity entity, UOMessageEventArgs args)
        {
            LocalizedMessageReceived.Raise(args, entity);
        }


        private static MessageInfo CreateMessage(string msg, ushort hue, byte font, bool isunicode, MessageType type)
        {
            if (Engine.Profile.Current != null && Engine.Profile.Current.OverrideAllFonts)
            {
                font = Engine.Profile.Current.ChatFont;
                isunicode = Engine.Profile.Current.OverrideAllFontsIsUnicode;
            }

            int width = isunicode ? FileManager.Fonts.GetWidthUnicode(font, msg) : FileManager.Fonts.GetWidthASCII(font, msg);

            if (width > 200)
                width = isunicode ? FileManager.Fonts.GetWidthExUnicode(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder) : FileManager.Fonts.GetWidthExASCII(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder);
            else
                width = 0;

            RenderedText rtext = RenderedText.Create(msg, hue, font, isunicode, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_LEFT, width, 30, false, false, true);

            return new MessageInfo
            {
                Alpha = 255,
                RenderedText = rtext,
                Time = CalculateTimeToLive(rtext),
                Type = type,
                Hue = hue,
            };
        }

        private static long CalculateTimeToLive(RenderedText rtext)
        {
            long timeToLive;

            if (Engine.Profile.Current.ScaleSpeechDelay)
            {
                int delay = Engine.Profile.Current.SpeechDelay;

                if (delay < 10)
                    delay = 10;

                timeToLive = (long)(4000 * rtext.LinesCount * delay / 100.0f);
            }
            else
            {
                long delay = (5497558140000 * Engine.Profile.Current.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Engine.Ticks;

            return timeToLive;
        }


    }

    internal class UOMessageEventArgs : EventArgs
    {
        public UOMessageEventArgs(Entity parent, string text, string name, Hue hue, MessageType type, byte font, bool unicode = false, string lang = null)
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

        public UOMessageEventArgs(Entity parent, string text, Hue hue, MessageType type, byte font, uint cliloc, bool unicode = false, AffixType affixType = AffixType.None, string affix = null)
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

        public byte Font { get; }

        public string Language { get; }

        public uint Cliloc { get; }

        public AffixType AffixType { get; }

        public string Affix { get; }

        public bool IsUnicode { get; }
    }
}