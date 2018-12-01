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
using System.Collections.Generic;

using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class RenderedTextList : GumpControl
    {
        private readonly List<RenderedText> _entries;
        private readonly IScrollBar _scrollBar;

        public RenderedTextList(int x, int y, int width, int height, IScrollBar scrollBarControl)
        {
            _scrollBar = scrollBarControl;
            _scrollBar.IsVisible = false;
            AcceptMouseInput = false;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _entries = new List<RenderedText>();
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            base.Draw(spriteBatch, position, hue);
            Point p = new Point(position.X, position.Y);
            int height = 0;
            int maxheight = _scrollBar.Value + _scrollBar.Height;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (height + _entries[i].Height <= _scrollBar.Value)
                {
                    // this entry is above the renderable area.
                    height += _entries[i].Height;
                }
                else if (height + _entries[i].Height <= maxheight)
                {
                    int y = height - _scrollBar.Value;

                    if (y < 0)
                    {
                        // this entry starts above the renderable area, but exists partially within it.
                        _entries[i].Draw(spriteBatch, new Rectangle(p.X, position.Y, _entries[i].Width, _entries[i].Height + y), 0, -y);
                        p.Y += _entries[i].Height + y;
                    }
                    else
                    {
                        // this entry is completely within the renderable area.
                        _entries[i].Draw(spriteBatch, p);
                        p.Y += _entries[i].Height;
                    }

                    height += _entries[i].Height;
                }
                else
                {
                    int y = maxheight - height;
                    _entries[i].Draw(spriteBatch, new Rectangle(p.X, position.Y + _scrollBar.Height - y, _entries[i].Width, y), 0, 0);

                    // can't fit any more entries - so we break!
                    break;
                }
            }

            return true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            _scrollBar.Location = new Point(X + Width - 14, Y);
            _scrollBar.Height = Height;
            CalculateScrollBarMaxValue();
            _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
        }

        private void CalculateScrollBarMaxValue()
        {
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;
            int height = 0;
            for (int i = 0; i < _entries.Count; i++) height += _entries[i].Height;
            height -= _scrollBar.Height;

            if (height > 0)
            {
                _scrollBar.MaxValue = height;

                if (maxValue)
                    _scrollBar.Value = _scrollBar.MaxValue;
            }
            else
            {
                _scrollBar.MaxValue = 0;
                _scrollBar.Value = 0;
            }
        }

        public void AddEntry(string text, int font, Hue hue)
        {
            bool maxScroll = _scrollBar.Value == _scrollBar.MaxValue;
            while (_entries.Count > 99) _entries.RemoveAt(0);

            RenderedText entry = new RenderedText
            {
                MaxWidth = Width - 18,
                IsUnicode = true,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                FontStyle = FontStyle.Indention | FontStyle.BlackBorder,
                Hue = hue,
                Font = (byte) font,
                Text = text
            };
            _entries.Add(entry);
            _scrollBar.MaxValue += _entries[_entries.Count - 1].Height;
            if (maxScroll) _scrollBar.Value = _scrollBar.MaxValue;
        }

        public void UpdateEntry(int index, string text)
        {
            if (index < 0 || index >= _entries.Count)
            {
                Log.Message(LogTypes.Error, $"Bad index in RenderedTextList.UpdateEntry: {index.ToString()}");

                return;
            }

            _entries[index].Text = text;
            CalculateScrollBarMaxValue();
        }

        public override void Dispose()
        {
            for (int i = 0; i < _entries.Count; i++) _entries[i]?.Dispose();
            base.Dispose();
        }
    }
}