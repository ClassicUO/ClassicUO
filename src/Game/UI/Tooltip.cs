#region license
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

using System.Linq;
using System.Text;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI
{
    internal class Tooltip
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly StringBuilder _sbHTML = new StringBuilder();
        private uint _serial;
        private uint _hash;
        private uint _lastHoverTime;
        private int _maxWidth;
        private RenderedText _renderedText;
        private string _textHTML;

        public string Text { get; protected set; }

        public bool IsEmpty => Text == null;

        public uint Serial => _serial;

        public bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (SerialHelper.IsValid(_serial) && World.OPL.TryGetRevision(_serial, out uint revision) && _hash != revision)
            {
                _hash = revision;
                Text = ReadProperties(_serial, out _textHTML);
            }

            if (string.IsNullOrEmpty(Text))
                return false;

            if (_lastHoverTime > Time.Ticks)
                return false;

            if (_renderedText == null)
            {
                _renderedText = RenderedText.Create(string.Empty,font: 1, isunicode: true, style: FontStyle.BlackBorder, cell: 5, isHTML: true, align: TEXT_ALIGN_TYPE.TS_CENTER, recalculateWidthByInfo: true);
            }
            else if (_renderedText.Text != Text)
            {
                if (_maxWidth == 0)
                {
                    FontsLoader.Instance.SetUseHTML(true);
                    FontsLoader.Instance.RecalculateWidthByInfo = true;

                    int width = FontsLoader.Instance.GetWidthUnicode(1, Text);

                    if (width > 600)
                        width = 600;

                    width = FontsLoader.Instance.GetWidthExUnicode(1, Text, width, TEXT_ALIGN_TYPE.TS_CENTER, (ushort) FontStyle.BlackBorder);

                    if (width > 600)
                        width = 600;

                    _renderedText.MaxWidth = width;

                    FontsLoader.Instance.RecalculateWidthByInfo = false;
                    FontsLoader.Instance.SetUseHTML(false);
                }
                else
                    _renderedText.MaxWidth = _maxWidth;

                _renderedText.Text = _textHTML;
            }

            if (x < 0)
                x = 0;
            else if (x > Client.Game.Window.ClientBounds.Width - (_renderedText.Width + 8))
                x = Client.Game.Window.ClientBounds.Width - (_renderedText.Width + 8);

            if (y < 0)
                y = 0;
            else if (y > Client.Game.Window.ClientBounds.Height - (_renderedText.Height + 8))
                y = Client.Game.Window.ClientBounds.Height - (_renderedText.Height + 8);

            Vector3 hue = Vector3.Zero;
            ShaderHuesTraslator.GetHueVector(ref hue, 0, false, 0.3f, true);

            batcher.Draw2D(Texture2DCache.GetTexture(Color.Black), x - 4, y - 2, _renderedText.Width + 8, _renderedText.Height + 4, ref hue);
            batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Gray), x - 4, y - 2, _renderedText.Width + 8, _renderedText.Height + 4, ref hue);

            return _renderedText.Draw(batcher, x + 3, y);
        }

        public void Clear()
        {
            _serial = 0;
            _hash = 0;
            _textHTML = Text = null;
            _maxWidth = 0;
        }

        public void SetGameObject(uint serial)
        {
            if (_serial == 0 || serial != _serial)
            {
                uint revision2 = 0;
                if (_serial == 0 || (World.OPL.TryGetRevision(_serial, out uint revision) && World.OPL.TryGetRevision(serial, out revision2) && revision != revision2))
                {
                    _maxWidth = 0;
                    _serial = serial;
                    _hash = revision2;
                    Text = ReadProperties(serial, out _textHTML);
                    _lastHoverTime = Time.Ticks + 250;
                }
            }
        }


        private string ReadProperties(uint serial, out string htmltext)
        {
            _sb.Clear();
            _sbHTML.Clear();

            bool hasStartColor = false;

            if (SerialHelper.IsValid(serial) && 
                World.OPL.TryGetNameAndData(serial, out string name, out string data))
            {
                if (!string.IsNullOrEmpty(name))
                {
                    if (SerialHelper.IsItem(serial))
                    {
                        _sbHTML.Append("<basefont color=\"yellow\">");
                        hasStartColor = true;
                    }
                    else
                    {
                        Mobile mob = World.Mobiles.Get(serial);

                        if (mob != null)
                        {
                            _sbHTML.Append(Notoriety.GetHTMLHue(mob.NotorietyFlag));
                            hasStartColor = true;
                        }
                    }

                    _sb.Append(name);
                    _sbHTML.Append(name);

                    if (hasStartColor)
                    {
                        _sbHTML.Append("<basefont color=\"#FFFFFFFF\">");
                    }
                }


                if (!string.IsNullOrEmpty(data))
                {
                    string s = $"\n{data}";
                    _sb.Append(s);
                    _sbHTML.Append(s);
                }
            }


            htmltext = _sbHTML.ToString();
            string result = _sb.ToString();

            return string.IsNullOrEmpty(result) ? null : result;
        }

        public void SetText(string text, int maxWidth = 0)
        {
            //if (Text != text)
            {
                _maxWidth = maxWidth;
                _serial = 0;
                Text = _textHTML = text;
                _lastHoverTime = Time.Ticks + 250;
            }
        }
    }
}