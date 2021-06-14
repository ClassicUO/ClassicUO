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

using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI
{
    internal class Tooltip
    {
        private uint _hash;
        private uint _lastHoverTime;
        private int _maxWidth;
        private RenderedText _renderedText;
        private string _textHTML;

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


            byte font = 1;
            float alpha = 0.3f;
            ushort hue = 0xFFFF;
            float zoom = 1;

            if (ProfileManager.CurrentProfile != null)
            {
                font = ProfileManager.CurrentProfile.TooltipFont;
                alpha = 1f - ProfileManager.CurrentProfile.TooltipBackgroundOpacity / 100f;

                if (float.IsNaN(alpha))
                {
                    alpha = 1f;
                }

                hue = ProfileManager.CurrentProfile.TooltipTextHue;
                zoom = ProfileManager.CurrentProfile.TooltipDisplayZoom / 100f;
            }

            FontsLoader.Instance.SetUseHTML(true);
            FontsLoader.Instance.RecalculateWidthByInfo = true;

            if (_renderedText == null)
            {
                _renderedText = RenderedText.Create
                (
                    null,
                    font: font,
                    isunicode: true,
                    style: FontStyle.BlackBorder,
                    cell: 5,
                    isHTML: true,
                    align: TEXT_ALIGN_TYPE.TS_CENTER,
                    recalculateWidthByInfo: true,
                    hue: hue
                );
            }

            if (_renderedText.Text != Text)
            {
                if (_maxWidth == 0)
                {
                    int width = FontsLoader.Instance.GetWidthUnicode(font, Text);

                    if (width > 600)
                    {
                        width = 600;
                    }

                    width = FontsLoader.Instance.GetWidthExUnicode
                    (
                        font,
                        Text,
                        width,
                        TEXT_ALIGN_TYPE.TS_CENTER,
                        (ushort) FontStyle.BlackBorder
                    );

                    if (width > 600)
                    {
                        width = 600;
                    }

                    _renderedText.MaxWidth = width;
                }
                else
                {
                    _renderedText.MaxWidth = _maxWidth;
                }

                _renderedText.Font = font;
                _renderedText.Hue = hue;
                _renderedText.Text = _textHTML;
            }

            FontsLoader.Instance.RecalculateWidthByInfo = false;
            FontsLoader.Instance.SetUseHTML(false);

            if (_renderedText.Texture == null)
            {
                return false;
            }

            int z_width = _renderedText.Width + 8;
            int z_height = _renderedText.Height + 8;

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


            Vector3 hue_vec = Vector3.Zero;
            ShaderHueTranslator.GetHueVector(ref hue_vec, 0, false, alpha);

            batcher.Draw2D
            (
                SolidColorTextureCache.GetTexture(Color.Black),
                x - 4,
                y - 2,
                z_width * zoom,
                z_height * zoom,
                ref hue_vec
            );

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x - 4,
                y - 2,
                (int) (z_width * zoom),
                (int) (z_height * zoom),
                ref hue_vec
            );

            hue_vec.X = 0;
            hue_vec.Y = 0;
            hue_vec.Z = 0;

            return batcher.Draw2D
            (
                _renderedText.Texture,
                x + 3,
                y + 3,
                z_width * zoom,
                z_height * zoom,
                0,
                0,
                z_width,
                z_height,
                ref hue_vec
            );
        }

        public void Clear()
        {
            Serial = 0;
            _hash = 0;
            _textHTML = Text = null;
            _maxWidth = 0;
        }

        public void SetGameObject(uint serial)
        {
            if (Serial == 0 || serial != Serial)
            {
                uint revision2 = 0;

                if (Serial == 0 || Serial != serial || World.OPL.TryGetRevision(Serial, out uint revision) && World.OPL.TryGetRevision(serial, out revision2) && revision != revision2)
                {
                    _maxWidth = 0;
                    Serial = serial;
                    _hash = revision2;
                    Text = ReadProperties(serial, out _textHTML);

                    _lastHoverTime = (uint) (Time.Ticks + (ProfileManager.CurrentProfile != null ? ProfileManager.CurrentProfile.TooltipDelayBeforeDisplay : 250));
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

            //if (Text != text)
            {
                _maxWidth = maxWidth;
                Serial = 0;
                Text = _textHTML = text;

                _lastHoverTime = (uint) (Time.Ticks + (ProfileManager.CurrentProfile != null ? ProfileManager.CurrentProfile.TooltipDelayBeforeDisplay : 250));
            }
        }
    }
}