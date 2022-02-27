﻿#region license

// Copyright (c) 2021, andreakarasho
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

using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

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


    internal static class MessageManager
    {
        public static PromptData PromptData { get; set; }

        public static event EventHandler<MessageEventArgs> MessageReceived;

        public static event EventHandler<MessageEventArgs> LocalizedMessageReceived;


        public static void HandleMessage
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
                    if (IgnoreManager.IgnoredCharsList.Contains(parent.Name) && type != MessageType.Spell)
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
                        msg.X = DelayedObjectClickManager.X;
                        msg.Y = DelayedObjectClickManager.Y;
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

        public static void OnLocalizedMessage(Entity entity, MessageEventArgs args)
        {
            LocalizedMessageReceived.Raise(args, entity);
        }

        public static TextObject CreateMessage
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

            TextObject textObject = TextObject.Create();
            textObject.Alpha = 0xFF;
            textObject.Type = type;
            textObject.Hue = fixedColor;
            textObject.Text = msg;
            textObject.ObjectTextType = textType;

            textObject.FontSettings.FontIndex = font;
            textObject.FontSettings.IsUnicode = isunicode;
            textObject.FontSettings.Border = true;

            textObject.MaxTextWidth = 200;
            textObject.TextSize = UOFontRenderer.Shared.MeasureString
            (
                textObject.Text.AsSpan(),
                textObject.FontSettings,
                1f,
                textObject.MaxTextWidth,
                Vector2.Zero
            );

            float fontHeight = UOFontRenderer.Shared.GetFontHeight(textObject.FontSettings);

            textObject.Time = CalculateTimeToLive(Math.Min(1, (int) (textObject.TextSize.Y / fontHeight)));

            return textObject;
        }

        private static long CalculateTimeToLive(int lines)
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

                timeToLive = (long) (4000 * lines * delay / 100.0f);
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