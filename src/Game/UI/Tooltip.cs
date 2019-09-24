#region license

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
        private static readonly char[] _titleFormatChars = {' ', '-', '\n', '['};
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly StringBuilder _sbHTML = new StringBuilder();
        private Entity _gameObject;
        private uint _hash;
        private float _lastHoverTime;
        private int _maxWidth;
        private RenderedText _renderedText;
        private string _textHTML;

        public string Text { get; protected set; }

        public bool IsEmpty => Text == null;

        public GameObject Object => _gameObject;

        public bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (_gameObject != null && World.OPL.TryGetRevision(_gameObject, out uint revision) && _hash != revision)
            {
                _hash = revision;
                Text = ReadProperties(_gameObject, out _textHTML);
            }

            if (string.IsNullOrEmpty(Text))
                return false;

            if (_lastHoverTime > Engine.Ticks)
                return false;

            if (_renderedText == null)
            {
                _renderedText = RenderedText.Create(string.Empty,font: 1, isunicode: true, style: FontStyle.BlackBorder, cell: 5, isHTML: true, align: TEXT_ALIGN_TYPE.TS_CENTER, recalculateWidthByInfo: true);
            }
            else if (_renderedText.Text != Text)
            {
                if (_maxWidth == 0)
                {
                    FileManager.Fonts.SetUseHTML(true);
                    FileManager.Fonts.RecalculateWidthByInfo = true;

                    int width = FileManager.Fonts.GetWidthUnicode(1, Text);

                    if (width > 600)
                        width = 600;

                    width = FileManager.Fonts.GetWidthExUnicode(1, Text, width, TEXT_ALIGN_TYPE.TS_CENTER, (ushort) FontStyle.BlackBorder);

                    if (width > 600)
                        width = 600;

                    _renderedText.MaxWidth = width;

                    FileManager.Fonts.RecalculateWidthByInfo = false;
                    FileManager.Fonts.SetUseHTML(false);
                }
                else
                    _renderedText.MaxWidth = _maxWidth;

                _renderedText.Text = _textHTML;
            }

            if (x < 0)
                x = 0;
            else if (x > Engine.WindowWidth - (_renderedText.Width + 8))
                x = Engine.WindowWidth - (_renderedText.Width + 8);

            if (y < 0)
                y = 0;
            else if (y > Engine.WindowHeight - (_renderedText.Height + 8))
                y = Engine.WindowHeight - (_renderedText.Height + 8);

            Vector3 hue = Vector3.Zero;
            ShaderHuesTraslator.GetHueVector(ref hue, 0, false, 0.3f, true);

            batcher.Draw2D(Textures.GetTexture(Color.Black), x - 4, y - 2, _renderedText.Width + 8, _renderedText.Height + 4, ref hue);

            return _renderedText.Draw(batcher, x, y);
        }

        public void Clear()
        {
            _gameObject = null;
            _hash = 0;
            _textHTML = Text = null;
            _maxWidth = 0;
        }

        public void SetGameObject(Entity obj)
        {
            if (_gameObject == null || obj != _gameObject)
            {
                uint revision2 = 0;
                if (_gameObject == null || (World.OPL.TryGetRevision(_gameObject, out uint revision) && World.OPL.TryGetRevision(obj, out revision2) && revision != revision2))
                {
                    _maxWidth = 0;
                    _gameObject = obj;
                    _hash = revision2;
                    Text = ReadProperties(obj, out _textHTML);
                    _lastHoverTime = Engine.Ticks + 250;
                }
            }
        }


        private string ReadProperties(Entity obj, out string htmltext)
        {
            _sb.Clear();
            _sbHTML.Clear();

            bool hasStartColor = false;


            if (obj != null && World.OPL.TryGetNameAndData(obj, out string name, out string data))
            {
                if (!string.IsNullOrEmpty(name))
                {
                    if (obj is Item)
                    {
                        _sbHTML.Append("<basefont color=\"yellow\">");
                        hasStartColor = true;
                    }
                    else if (obj is Mobile mob)
                    {
                        _sbHTML.Append(Notoriety.GetHTMLHue(mob.NotorietyFlag));
                        hasStartColor = true;
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



            //for (int i = 0; i < obj.Properties.Count; i++)
            //{
            //    Property property = obj.Properties[i];

            //    if (property.Cliloc <= 0)
            //        continue;

            //    if (i == 0 /*&& !string.IsNullOrEmpty(obj.Name)*/)
            //    {
            //        if (obj is Mobile mobile)
            //        {
            //            //ushort hue = Notoriety.GetHue(mobile.NotorietyFlag);
            //            _sbHTML.Append(Notoriety.GetHTMLHue(mobile.NotorietyFlag));
            //        }
            //        else
            //            _sbHTML.Append("<basefont color=\"yellow\">");

            //        hasStartColor = true;
            //    }

            //    string text = FormatTitle(FileManager.Cliloc.Translate((int) property.Cliloc, property.Args, true));

            //    if (string.IsNullOrEmpty(text)) continue;

            //    _sb.Append(text);
            //    _sbHTML.Append(text);

            //    if (hasStartColor)
            //    {
            //        _sbHTML.Append("<basefont color=\"#FFFFFFFF\">");
            //        hasStartColor = false;
            //    }

            //    if (i < obj.Properties.Count - 1)
            //    {
            //        _sb.Append("\n");
            //        _sbHTML.Append("\n");
            //    }
            //}

            htmltext = _sbHTML.ToString();
            string result = _sb.ToString();

            return string.IsNullOrEmpty(result) ? null : result;
        }

        public unsafe string FormatTitle(string text)
        {
            if (text != null)
            {
                int index = 0;

                fixed (char* value = text)
                {
                    while (index < text.Length)
                    {
                        if (index <= 0 || _titleFormatChars.Contains(value[index - 1]))
                            value[index] = char.ToUpper(value[index]);

                        index++;
                    }

                    return new string(value);
                }
            }

            return text;
        }

        public void SetText(string text, int maxWidth = 0)
        {
            _maxWidth = maxWidth;
            _gameObject = null;
            Text = _textHTML = text;
        }
    }
}