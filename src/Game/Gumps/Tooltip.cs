#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using System.Text;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    public class Tooltip : IDrawableUI
    {
        private Entity _gameObject;
        private uint _hash;
        private RenderedText _renderedText;
        private string _textHTML;
        private float _lastHoverTime;

        public string Text { get; protected set; }

        public bool IsEmpty => Text == null;

        public GameObject Object => _gameObject;

        public bool AllowedToDraw { get; set; } = true;

        public SpriteTexture Texture { get; set; }

        public bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            if (_lastHoverTime > Engine.Ticks)
                return false;

            if (_gameObject != null && _hash != _gameObject.PropertiesHash)
            {
                _hash = _gameObject.PropertiesHash;
                Text = ReadProperties(_gameObject, out _textHTML);
            }

            if (string.IsNullOrEmpty(Text))
                return false;

            if (_renderedText == null)
            {
                _renderedText = new RenderedText
                {
                    Align = TEXT_ALIGN_TYPE.TS_CENTER,
                    Font = 1,
                    IsUnicode = true,
                    IsHTML = true,
                    Cell = 5,
                    FontStyle = FontStyle.BlackBorder
                };
            }
            else if (_renderedText.Text != Text)
            {
                Fonts.RecalculateWidthByInfo = true;
                int width = Fonts.GetWidthUnicode(1, Text);

                if (width > 600)
                    width = 600;
                _renderedText.MaxWidth = width;
                _renderedText.Text = _textHTML;
                Fonts.RecalculateWidthByInfo = false;
            }


            if (position.X < 0)
                position.X = 0;
            else if (position.X > Engine.WindowWidth - (_renderedText.Width + 8))
                position.X = Engine.WindowWidth - (_renderedText.Width + 8);

            if (position.Y < 0)
                position.Y = 0;
            else if (position.Y > Engine.WindowHeight - (_renderedText.Height + 8))
                position.Y = Engine.WindowHeight - (_renderedText.Height + 8);
            batcher.Draw2D(CheckerTrans.TransparentTexture, new Rectangle(position.X - 4, position.Y - 2, _renderedText.Width + 8, _renderedText.Height + 4), ShaderHuesTraslator.GetHueVector(0, false, 0.3f, false));

            return _renderedText.Draw(batcher, position);
        }

        public void Clear()
        {
            _textHTML = Text = null;
        }

        public void SetGameObject(Entity obj)
        {
            if (_gameObject == null || obj != _gameObject || obj.PropertiesHash != _gameObject.PropertiesHash)
            {
                _gameObject = obj;
                _hash = obj.PropertiesHash;
                Text = ReadProperties(obj, out _textHTML);
                _lastHoverTime = Engine.Ticks + 250;
            }
        }

        private StringBuilder _sb = new StringBuilder();
        private StringBuilder _sbHTML = new StringBuilder();

        private string ReadProperties(Entity obj, out string htmltext)
        {
            _sb.Clear();
            _sbHTML.Clear();

            bool hasStartColor = false;

            for (int i = 0; i < obj.Properties.Count; i++)
            {
                Property property = obj.Properties[i];

                if (property.Cliloc <= 0)
                    continue;

                if (i == 0 /*&& !string.IsNullOrEmpty(obj.Name)*/)
                {
                    if (obj.Serial.IsMobile)
                    {
                        Mobile mobile = (Mobile) obj;
                        //ushort hue = Notoriety.GetHue(mobile.NotorietyFlag);
                        _sbHTML.Append(Notoriety.GetHTMLHue(mobile.NotorietyFlag));
                    }
                    else
                        _sbHTML.Append("<basefont color=\"yellow\">");

                    hasStartColor = true;
                }

                string text = Cliloc.Translate((int) property.Cliloc, property.Args, true);

                if (string.IsNullOrEmpty(text)) continue;
                _sb.Append(text);
                _sbHTML.Append(text);

                if (hasStartColor)
                {
                    _sbHTML.Append("<basefont color=\"#FFFFFFFF\">");
                    hasStartColor = false;
                }

                if (i < obj.Properties.Count - 1)
                {
                    _sb.Append("\n");
                    _sbHTML.Append("\n");
                }
            }

            htmltext = _sbHTML.ToString();
            string result = _sb.ToString();

            return string.IsNullOrEmpty(result) ? null : result;
        }

        public void SetText(string text)
        {
            _gameObject = null;
            Text = _textHTML = text;
        }
    }
}