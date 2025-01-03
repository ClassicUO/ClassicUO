// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Assets;
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
        private readonly World _world;

        public Tooltip(World world) => _world = world;

        public string Text { get; protected set; }

        public bool IsEmpty => Text == null;

        public uint Serial { get; private set; }

        public bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (SerialHelper.IsValid(Serial) && _world.OPL.TryGetRevision(Serial, out uint revision) && _hash != revision)
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
            float alpha = 0.7f;
            ushort hue = 0xFFFF;
            float zoom = 1;

            if (ProfileManager.CurrentProfile != null)
            {
                font = ProfileManager.CurrentProfile.TooltipFont;
                alpha = ProfileManager.CurrentProfile.TooltipBackgroundOpacity / 100f;

                if (float.IsNaN(alpha))
                {
                    alpha = 0f;
                }

                hue = ProfileManager.CurrentProfile.TooltipTextHue;
                zoom = ProfileManager.CurrentProfile.TooltipDisplayZoom / 100f;
            }

            Client.Game.UO.FileManager.Fonts.SetUseHTML(true);
            Client.Game.UO.FileManager.Fonts.RecalculateWidthByInfo = true;

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
                    int width = Client.Game.UO.FileManager.Fonts.GetWidthUnicode(font, Text);

                    if (width > 600)
                    {
                        width = 600;
                    }

                    width = Client.Game.UO.FileManager.Fonts.GetWidthExUnicode
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

            Client.Game.UO.FileManager.Fonts.RecalculateWidthByInfo = false;
            Client.Game.UO.FileManager.Fonts.SetUseHTML(false);

            if (_renderedText.Texture == null || _renderedText.Texture.IsDisposed)
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


            Vector3 hue_vec = ShaderHueTranslator.GetHueVector(0, false, alpha);

            batcher.Draw
            (
                SolidColorTextureCache.GetTexture(Color.Black),
                new Rectangle
                (
                    x - 4,
                    y - 2,
                    (int)(z_width * zoom),
                    (int)(z_height * zoom)
                ),
                hue_vec
            );


            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x - 4,
                y - 2,
                (int) (z_width * zoom),
                (int) (z_height * zoom),
                hue_vec
            );

            batcher.Draw
            (
                _renderedText.Texture,
                new Rectangle
                (
                    x + 3,
                    y + 3,
                    (int)(_renderedText.Texture.Width * zoom),
                    (int)(_renderedText.Texture.Height * zoom)
                ),
                null,
                Vector3.UnitZ
            );

            return true;
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

                if (Serial == 0 || Serial != serial || _world.OPL.TryGetRevision(Serial, out uint revision) && _world.OPL.TryGetRevision(serial, out revision2) && revision != revision2)
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

            if (SerialHelper.IsValid(serial) && _world.OPL.TryGetNameAndData(serial, out string name, out string data))
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
                                Mobile mob = _world.Mobiles.Get(serial);

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