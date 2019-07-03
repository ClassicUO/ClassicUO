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

using System;

using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class RenderedTextList : Control
    {
        private readonly Deque<RenderedText> _entries, _hours;
        private readonly IScrollBar _scrollBar;

        public RenderedTextList(int x, int y, int width, int height, IScrollBar scrollBarControl)
        {
            _scrollBar = scrollBarControl;
            _scrollBar.IsVisible = false;
            AcceptMouseInput = true;
            CanMove = true;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            _entries = new Deque<RenderedText>();
            _hours = new Deque<RenderedText>();

            WantUpdateSize = false;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            int mx = x;
            int my = y;

            int height = 0;
            int maxheight = _scrollBar.Value + _scrollBar.Height;

            for (int i = 0; i < _entries.Count; i++)
            {
                var t = _entries[i];
                var hour = _hours[i];

                if (height + t.Height <= _scrollBar.Value)
                {
                    // this entry is above the renderable area.
                    height += t.Height;
                }
                else if (height + t.Height <= maxheight)
                {
                    int yy = height - _scrollBar.Value;

                    if (yy < 0)
                    {
                        // this entry starts above the renderable area, but exists partially within it.
                        hour.Draw(batcher, mx, y, t.Width, t.Height + yy, 0, -yy);
                        t.Draw(batcher, mx + hour.Width, y, t.Width, t.Height + yy, 0, -yy);
                        my += t.Height + yy;
                    }
                    else
                    {
                        // this entry is completely within the renderable area.
                        hour.Draw(batcher, mx, my);
                        t.Draw(batcher, mx + hour.Width, my);
                        my += t.Height;
                    }

                    height += t.Height;
                }
                else
                {
                    int yyy = maxheight - height;
                    hour.Draw(batcher, mx, y + _scrollBar.Height - yyy, t.Width, yyy, 0, 0);
                    t.Draw(batcher, mx + hour.Width, y + _scrollBar.Height - yyy, t.Width, yyy, 0, 0);
                   
                    // can't fit any more entries - so we break!
                    break;
                }
            }

            return true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            _scrollBar.X = X + Width - (_scrollBar.Width >> 1) + 5;
            _scrollBar.Height = Height;
            CalculateScrollBarMaxValue();
            _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
        }

        private void CalculateScrollBarMaxValue()
        {
            bool maxValue = _scrollBar.Value == _scrollBar.MaxValue;
            int height = 0;

            foreach (RenderedText t in _entries)
                height += t.Height;

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

        public void AddEntry(string text, int font, Hue hue, bool isUnicode)
        {
            bool maxScroll = _scrollBar.Value == _scrollBar.MaxValue;

            while (_entries.Count > 99)
            {
                _entries.RemoveFromFront().Destroy();
                _hours.RemoveFromFront().Destroy();
            }

            var h = new RenderedText()
            {
                IsUnicode = isUnicode,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                FontStyle = FontStyle.BlackBorder,
                Hue = 1150,
                Font = 1,
                Text = $"{DateTime.Now:t} "
            };

            _hours.AddToBack(h);

            _entries.AddToBack(new RenderedText
            {
                MaxWidth = Width - (18 + h.Width),
                IsUnicode = isUnicode,
                Align = TEXT_ALIGN_TYPE.TS_LEFT,
                FontStyle = FontStyle.Indention | FontStyle.BlackBorder,
                Hue = hue,
                Font = (byte) font,
                Text = text
            });

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
            foreach (RenderedText t in _entries)
                t?.Destroy();

            base.Dispose();
        }
    }
}