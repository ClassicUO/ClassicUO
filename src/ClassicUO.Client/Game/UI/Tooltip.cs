#region license

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
using System.Text.RegularExpressions;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using FontStyle = ClassicUO.Game.FontStyle;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI
{
    internal class Tooltip
    {
        private uint _hash;
        private uint _lastHoverTime;
        private RenderedText _renderedText;
        private string _textHTML;
        private bool _dirty;
        private ushort _lastTooltipHue = 0xFFFF;

        public static bool IsEnabled;

        public static int X, Y;
        public static int Width, Height;

        public string Text { get; protected set; }

        public bool IsEmpty => Text == null;

        public uint Serial { get; private set; }

        public bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (ProfileManager.CurrentProfile != null && !ProfileManager.CurrentProfile.UseTooltip)
                return false;

            if (SerialHelper.IsValid(Serial) && World.OPL.TryGetRevision(Serial, out uint revision) && _hash != revision)
            {
                _hash = revision;
                Text = ReadProperties(Serial, out _textHTML);
            }

            if (string.IsNullOrEmpty(Text))
                return false;

            if (_lastHoverTime > Time.Ticks)
                return false;

            float alpha = 0.7f;
            float zoom = 1f;
            byte font = 1;
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_CENTER;

            if (ProfileManager.CurrentProfile != null)
            {
                alpha = ProfileManager.CurrentProfile.TooltipBackgroundOpacity / 100f;
                if (float.IsNaN(alpha))
                    alpha = 0f;
                zoom = ProfileManager.CurrentProfile.TooltipDisplayZoom / 100f;
                font = ProfileManager.CurrentProfile.SelectedToolTipFont;
                if (ProfileManager.CurrentProfile.LeftAlignToolTips)
                    align = TEXT_ALIGN_TYPE.TS_LEFT;
                if (SerialHelper.IsMobile(Serial) && ProfileManager.CurrentProfile.ForceCenterAlignTooltipMobiles)
                    align = TEXT_ALIGN_TYPE.TS_CENTER;
            }

            string finalString = _textHTML;
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableTooltipOverride && SerialHelper.IsItem(Serial))
            {
                finalString = Managers.ToolTipOverrideData.ProcessTooltipText(Serial);
                finalString ??= _textHTML;
            }

            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableTooltipOverride && string.IsNullOrEmpty(finalString) && !string.IsNullOrEmpty(_textHTML))
                finalString = Managers.ToolTipOverrideData.ProcessTooltipText(_textHTML);

            string displayText = Regex.Replace(finalString ?? string.Empty, @"/c\[(black|#000000|#000)\]", "/c[white]", RegexOptions.IgnoreCase);
            displayText = HtmlTextHelper.ConvertUoColorCodesToHtml(displayText).Trim();
            displayText = Regex.Replace(displayText, @"color\s*=\s*[\""']?(black|#000000|#000)[\""']?", "color=\"#FFFFFF\"", RegexOptions.IgnoreCase);
            if (!displayText.StartsWith("<basefont", StringComparison.OrdinalIgnoreCase) && !displayText.StartsWith("<font", StringComparison.OrdinalIgnoreCase) && !displayText.StartsWith("/c["))
                displayText = "<basefont color=\"#FFFFFF\">" + displayText;

            ushort hue = 0xFFFF;
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TooltipTextHue != 0xFFFF)
                hue = ProfileManager.CurrentProfile.TooltipTextHue;

            if (_lastTooltipHue != hue)
            {
                _lastTooltipHue = hue;
                _dirty = true;
            }

            FontStyle textStyle = (hue != 0xFFFF) ? FontStyle.None : FontStyle.BlackBorder;

            if (_renderedText == null || _dirty)
            {
                _renderedText?.Destroy();
                _renderedText = RenderedText.Create
                (
                    displayText,
                    hue,
                    font,
                    isunicode: true,
                    style: textStyle,
                    align,
                    maxWidth: 600,
                    cell: 5,
                    isHTML: true,
                    recalculateWidthByInfo: true
                );
                _renderedText.HTMLColor = 0xFFFFFFFF;
                _dirty = false;
                IsEnabled = true;
            }
            else if (_renderedText.Text != displayText)
            {
                _renderedText.MaxWidth = 600;
                _renderedText.Font = font;
                _renderedText.Hue = hue;
                _renderedText.Text = displayText;
            }

            if (_renderedText == null || _renderedText.Texture == null || _renderedText.Texture.IsDisposed)
                return false;

            int z_width = _renderedText.Width + 8;
            int z_height = _renderedText.Height + 8;

            if (x < 0) x = 0;
            else if (x > Client.Game.Window.ClientBounds.Width - z_width) x = Client.Game.Window.ClientBounds.Width - z_width;
            if (y < 0) y = 0;
            else if (y > Client.Game.Window.ClientBounds.Height - z_height) y = Client.Game.Window.ClientBounds.Height - z_height;

            X = x - 4;
            Y = y - 2;
            Width = (int)(z_width * zoom) + 1;
            Height = (int)(z_height * zoom) + 1;

            int bgHue = ProfileManager.CurrentProfile != null ? ProfileManager.CurrentProfile.ToolTipBGHue : 0;
            bool useBgHue = bgHue > 0;
            Vector3 bgHueVec = useBgHue
                ? ShaderHueTranslator.GetHueVector(bgHue, false, alpha, true)
                : ShaderHueTranslator.GetHueVector(0, false, alpha);
            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(useBgHue ? Color.White : Color.Black),
                new Rectangle(x - 4, y - 2, (int)(z_width * zoom), (int)(z_height * zoom)),
                null,
                bgHueVec
            );

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x - 4,
                y - 2,
                (int)(z_width * zoom),
                (int)(z_height * zoom),
                ShaderHueTranslator.GetHueVector(0, false, alpha)
            );

            batcher.Draw
            (
                _renderedText.Texture,
                new Rectangle(x + 4, y + 4, (int)(_renderedText.Width * zoom), (int)(_renderedText.Height * zoom)),
                null,
                ShaderHueTranslator.GetHueVector(0, false, 1f, true)
            );

            return true;
        }

        public void Clear()
        {
            Serial = 0;
            _hash = 0;
            _textHTML = Text = null;
            _renderedText?.Destroy();
            _renderedText = null;
            IsEnabled = false;
        }

        public void SetGameObject(uint serial)
        {
            if (Serial == 0 || serial != Serial)
            {
                uint revision2 = 0;

                if (Serial == 0 || Serial != serial || World.OPL.TryGetRevision(Serial, out uint revision) && World.OPL.TryGetRevision(serial, out revision2) && revision != revision2)
                {
                    Serial = serial;
                    _hash = revision2;
                    Text = ReadProperties(serial, out _textHTML);
                    _renderedText?.Destroy();
                    _dirty = true;

                    _lastHoverTime = (uint)(Time.Ticks + (ProfileManager.CurrentProfile != null ? ProfileManager.CurrentProfile.TooltipDelayBeforeDisplay : 250));
                }
            }
        }


        private string ReadProperties(uint serial, out string htmltext)
        {
            bool hasStartColor = false;

            string result = null;
            htmltext = string.Empty;

            if (SerialHelper.IsValid(serial) && World.OPL.TryGetNameAndData(serial, out string name, out string data))
            {
                ValueStringBuilder sbHTML = new ValueStringBuilder();
                {
                    ValueStringBuilder sb = new ValueStringBuilder();
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            if (SerialHelper.IsItem(serial))
                            {
                                sbHTML.Append("<basefont color=\"yellow\">");
                                hasStartColor = true;
                            }
                            else
                            {
                                Mobile mob = World.Mobiles.Get(serial);

                                if (mob != null)
                                {
                                    sbHTML.Append(Notoriety.GetHTMLHue(mob.NotorietyFlag));
                                    hasStartColor = true;
                                }
                            }

                            sb.Append(name);
                            sbHTML.Append(name);

                            if (hasStartColor)
                            {
                                sbHTML.Append("<basefont color=\"#FFFFFFFF\">");
                            }
                        }

                        if (!string.IsNullOrEmpty(data))
                        {
                            sb.Append('\n');
                            sb.Append(data);
                            sbHTML.Append('\n');
                            sbHTML.Append(data);
                        }

                        htmltext = sbHTML.ToString();
                        result = sb.ToString();

                        sb.Dispose();
                        sbHTML.Dispose();
                    }
                }
            }
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public void SetText(string text, int maxWidth = 0)
        {
            if (ProfileManager.CurrentProfile != null && !ProfileManager.CurrentProfile.UseTooltip)
            {
                return;
            }

            Serial = 0;
            Text = _textHTML = text;

            _dirty = true;

            _lastHoverTime = (uint)(Time.Ticks + (ProfileManager.CurrentProfile != null ? ProfileManager.CurrentProfile.TooltipDelayBeforeDisplay : 250));

            _renderedText?.Destroy();
            _renderedText = null;
        }
    }
}