﻿#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

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

    enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02,
        None = 0xFF
    }



    internal static class MessageManager
    {
        public static PromptData PromptData { get; set; }

        public static event EventHandler<UOMessageEventArgs> MessageReceived;

        public static event EventHandler<UOMessageEventArgs> LocalizedMessageReceived;


        public static void HandleMessage(Entity parent, string text, string name, ushort hue, MessageType type, byte font, TEXT_TYPE text_type, bool unicode = false, string lang = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (ProfileManager.Current != null && ProfileManager.Current.OverrideAllFonts)
            {
                font = ProfileManager.Current.ChatFont;
                unicode = ProfileManager.Current.OverrideAllFontsIsUnicode;
            }

            switch (type)
            {
                case MessageType.Command:
                case MessageType.Encoded:
                case MessageType.System:
                case MessageType.Party:
                case MessageType.Guild:
                case MessageType.Alliance:

                    break;


                case MessageType.Spell:

                {
                    //server hue color per default
                    if (!string.IsNullOrEmpty(text) && SpellDefinition.WordToTargettype.TryGetValue(text, out SpellDefinition spell))
                    {
                        if (ProfileManager.Current != null && ProfileManager.Current.EnabledSpellFormat && !string.IsNullOrWhiteSpace(ProfileManager.Current.SpellDisplayFormat))
                        {
                            StringBuilder sb = new StringBuilder(ProfileManager.Current.SpellDisplayFormat);
                            sb.Replace("{power}", spell.PowerWords);
                            sb.Replace("{spell}", spell.Name);
                            text = sb.ToString().Trim();
                        }

                        //server hue color per default if not enabled
                        if (ProfileManager.Current != null && ProfileManager.Current.EnabledSpellHue)
                        {
                            if (spell.TargetType == TargetType.Beneficial)
                                hue = ProfileManager.Current.BeneficHue;
                            else if (spell.TargetType == TargetType.Harmful)
                                hue = ProfileManager.Current.HarmfulHue;
                            else
                                hue = ProfileManager.Current.NeutralHue;
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
                        break;

                    TextObject msg = CreateMessage(text, hue, font, unicode, type, text_type);
                    msg.Owner = parent;

                    if (parent is Item it && !it.OnGround)
                    {
                        msg.X = DelayedObjectClickManager.X;
                        msg.Y = DelayedObjectClickManager.Y;
                        msg.IsTextGump = true;
                        bool found = false;

                        for (LinkedListNode<Control> gump = UIManager.Gumps.Last; gump != null; gump = gump.Previous)
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
                                    case TradingGump trade when g.LocalSerial == it.Container || trade.ID1 == it.Container || trade.ID2 == it.Container:
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
      

            
                //default:
                //    if (parent == null)
                //        break;

                //    parent.AddMessage(type, text, font, hue, unicode);

                //    break;
            }

            MessageReceived.Raise(new UOMessageEventArgs(parent, text, name, hue, type, font, text_type, unicode, lang), parent);
        }

        public static void OnLocalizedMessage(Entity entity, UOMessageEventArgs args)
        {
            LocalizedMessageReceived.Raise(args, entity);
        }


        public static TextObject CreateMessage(string msg, ushort hue, byte font, bool isunicode, MessageType type, TEXT_TYPE text_type)
        {
            if (ProfileManager.Current != null && ProfileManager.Current.OverrideAllFonts)
            {
                font = ProfileManager.Current.ChatFont;
                isunicode = ProfileManager.Current.OverrideAllFontsIsUnicode;
            }

            int width = isunicode ? FontsLoader.Instance.GetWidthUnicode(font, msg) : FontsLoader.Instance.GetWidthASCII(font, msg);

            if (width > 200)
                width = isunicode ? FontsLoader.Instance.GetWidthExUnicode(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder) : FontsLoader.Instance.GetWidthExASCII(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder);
            else
                width = 0;

            TextObject text_obj = TextObject.Create();
            text_obj.Alpha = 0xFF;
            text_obj.Type = type;
            text_obj.Hue = hue;

            if (!isunicode && text_type == TEXT_TYPE.OBJECT)
            {
                hue = 0;
            }

            text_obj.RenderedText = RenderedText.Create(msg, hue, font, isunicode, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_LEFT, width, 30, false, false, text_type == TEXT_TYPE.OBJECT);
            text_obj.Time = CalculateTimeToLive(text_obj.RenderedText);
            text_obj.RenderedText.Hue = text_obj.Hue;

            return text_obj;
        }

        private static long CalculateTimeToLive(RenderedText rtext)
        {
            long timeToLive;

            if (ProfileManager.Current.ScaleSpeechDelay)
            {
                int delay = ProfileManager.Current.SpeechDelay;

                if (delay < 10)
                    delay = 10;

                timeToLive = (long)(4000 * rtext.LinesCount * delay / 100.0f);
            }
            else
            {
                long delay = (5497558140000 * ProfileManager.Current.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Time.Ticks;

            return timeToLive;
        }


    }

    internal class UOMessageEventArgs : EventArgs
    {
        public UOMessageEventArgs(Entity parent, string text, string name, ushort hue, MessageType type, byte font, TEXT_TYPE text_type, bool unicode = false, string lang = null)
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

        public TEXT_TYPE TextType { get; }
    }
}