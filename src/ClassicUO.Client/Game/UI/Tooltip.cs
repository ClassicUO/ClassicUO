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

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI
{
    internal class Tooltip
    {
        private uint _hash;
        private uint _lastHoverTime;
        private TextBox _textBox;
        private string _textHTML;
        private bool _dirty = false;

        public static bool IsEnabled = false;

        public static int X, Y;
        public static int Width, Height;

        public string Text { get; protected set; }

        public bool IsEmpty => Text == null;

        public uint Serial { get; private set; }

        public bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (SerialHelper.IsValid(Serial) && World.OPL.TryGetRevision(Serial, out uint revision) && _hash != revision)
            {
                _hash = revision;
                Text = ReadProperties(Serial, out _textHTML);
            }

            if (string.IsNullOrEmpty(Text))
            {
                return false;
            }

            if (_lastHoverTime > Time.Ticks)
            {
                return false;
            }

            float alpha = 0.7f;
            ushort hue = 0xFFFF;
            float zoom = 1;

            if (ProfileManager.CurrentProfile != null)
            {
                alpha = ProfileManager.CurrentProfile.TooltipBackgroundOpacity / 100f;

                if (float.IsNaN(alpha))
                {
                    alpha = 0f;
                }

                hue = ProfileManager.CurrentProfile.TooltipTextHue;
                zoom = ProfileManager.CurrentProfile.TooltipDisplayZoom / 100f;
            }


            if (_textBox == null || _dirty)
            {
                FontStashSharp.RichText.TextHorizontalAlignment align = FontStashSharp.RichText.TextHorizontalAlignment.Center;
                if (ProfileManager.CurrentProfile != null)
                {
                    if (ProfileManager.CurrentProfile.LeftAlignToolTips)
                        align = FontStashSharp.RichText.TextHorizontalAlignment.Left;
                    if (SerialHelper.IsMobile(Serial) && ProfileManager.CurrentProfile.ForceCenterAlignTooltipMobiles)
                        align = FontStashSharp.RichText.TextHorizontalAlignment.Center;
                }

                string finalString = _textHTML;
                if (SerialHelper.IsItem(Serial))
                {
                    finalString = Managers.ToolTipOverrideData.ProcessTooltipText(Serial);
                    finalString ??= _textHTML;
                }

                if (string.IsNullOrEmpty(finalString) && !string.IsNullOrEmpty(_textHTML)) //Fix for vendor search
                    finalString = Managers.ToolTipOverrideData.ProcessTooltipText(_textHTML);

                string font = TrueTypeLoader.EMBEDDED_FONT;
                int fontSize = 15;

                if (ProfileManager.CurrentProfile != null)
                {
                    font = ProfileManager.CurrentProfile.SelectedToolTipFont;
                    fontSize = ProfileManager.CurrentProfile.SelectedToolTipFontSize;
                }

                _textBox = new TextBox(
                    TextBox.ConvertHtmlToFontStashSharpCommand(finalString).Trim(),
                    font,
                    fontSize,
                    600,
                    hue,
                    align,
                    true
                );
                _textBox.Width = _textBox.MeasuredSize.X + 10;
                if (_textBox.Width > 600)
                    _textBox.Width = 600;

                IsEnabled = true;
            }

            if (_textBox == null || _textBox.IsDisposed)
            {
                return false;
            }

            int z_width = _textBox.Width + 8;
            int z_height = _textBox.Height + 8;

            if (x < 0)
            {
                x = 0;
            }
            else if (x > Client.Game.Window.ClientBounds.Width - z_width)
            {
                x = Client.Game.Window.ClientBounds.Width - z_width;
            }

            if (y < 0)
            {
                y = 0;
            }
            else if (y > Client.Game.Window.ClientBounds.Height - z_height)
            {
                y = Client.Game.Window.ClientBounds.Height - z_height;
            }

            X = x - 4;
            Y = y - 2;
            Width = (int)(z_width * zoom) + 1;
            Height = (int)(z_height * zoom) + 1;

            Vector3 hue_vec = ShaderHueTranslator.GetHueVector(1, false, alpha);

            if (ProfileManager.CurrentProfile != null)
                hue_vec.X = ProfileManager.CurrentProfile.ToolTipBGHue - 1;

            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.White),
                new Rectangle
                (
                    x - 4,
                    y - 2,
                    (int)(z_width * zoom),
                    (int)(z_height * zoom)
                ),
                hue_vec
            );

            hue_vec = ShaderHueTranslator.GetHueVector(0, false, alpha);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x - 4,
                y - 2,
                (int)(z_width * zoom),
                (int)(z_height * zoom),
                hue_vec
            );

            _textBox.Draw(batcher, x, y);

            return true;
        }

        public void Clear()
        {
            Serial = 0;
            _hash = 0;
            _textHTML = Text = null;
            _textBox?.Dispose();
            _textBox = null;
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
                    _textBox?.Dispose();
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

            _textBox?.Dispose();
            _textBox = null;
        }
    }
}