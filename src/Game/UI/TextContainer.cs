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

using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI
{
    sealed class TextContainerEntry
    {
        public RenderedText RenderedText;
        public long Time;
        public int X, Y, OffsetY;
        public Serial ItemSerial;
        public float Alpha;
    }
    internal sealed class TextContainer
    {
        private readonly List<TextContainerEntry> _messages = new List<TextContainerEntry>();
        private readonly Rectangle[] _rects = new Rectangle[2];

        private int _lastX, _lastY, _lastHeight;

        public void Add(string text, ushort hue, byte font, bool isunicode, int x, int y, Serial itemSerial)
        {
            var renderedText = RenderedText.Create(text, hue, font, isunicode, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER, 200);


            int offset = 0;
            if (_lastX == x && _lastY == y)
            {
                offset = _lastHeight;

                //var last = _messages[_messages.Count - 1];
                //last.Y -= renderedText.Height;
                //last.OffsetY -= _lastHeight - renderedText.Height;

                _lastHeight += renderedText.Height;

            }
            else
            {
                _lastX = x;
                _lastY = y;
                _lastHeight = renderedText.Height;
            }

            offset -= renderedText.Height;

            TextContainerEntry msg = new TextContainerEntry
            {
                RenderedText = renderedText,
                Time = Engine.Ticks + 4000,
                X = x,
                Y = y,
                OffsetY = -offset,
                ItemSerial = itemSerial
            };

            _messages.Add(msg);
        }


        public void Update()
        {
            long t_delta = Engine.Ticks;

            for (int i = 0; i < _messages.Count; i++)
            {
                var msg = _messages[i];

                if (msg.ItemSerial.IsValid)
                {
                    var entity = World.Get(msg.ItemSerial);
                    if (entity == null || entity.IsDestroyed)
                    {
                        msg.RenderedText.Destroy();
                        _messages.RemoveAt(i--);
                        continue;
                    }
                }

                var time = msg.Time - t_delta;

                if (time > 0 && time < 1000)
                {
                    float alpha = 1f - time / 1000f;

                    if (msg.Alpha < alpha)
                        msg.Alpha = alpha;
                }
                else if (time <= 0)
                {
                    msg.RenderedText.Destroy();
                    _messages.RemoveAt(i--);
                }
                else
                {
                    int count = 0;
                    _rects[0].X = msg.X;
                    _rects[0].Y = msg.Y - msg.OffsetY;
                    _rects[0].Width = msg.RenderedText.Width;
                    _rects[0].Height = msg.RenderedText.Height;
                    _rects[0].X -= _rects[0].Width >> 1;

                    for (int j = i + 1; j < _messages.Count; j++)
                    {
                        var m = _messages[j];

                        if (msg.X == m.X && msg.Y == m.Y)
                            continue;

                        _rects[1].X = m.X;
                        _rects[1].Y = m.Y - msg.OffsetY;
                        _rects[1].Width = m.RenderedText.Width;
                        _rects[1].Height = m.RenderedText.Height;
                        _rects[1].X -= _rects[1].Width >> 1;

                        if (_rects[0].Intersects(_rects[1]))
                        {
                            msg.Alpha = 0.3f + 0.05f * count;
                            count++;
                        }
                    }
                }
            }
        }

        public void Draw(UltimaBatcher2D batcher, int x, int y)
        {
            for (int i = 0; i < _messages.Count; i++)
            {
                var msg = _messages[i];

                int xx = x + msg.X - (msg.RenderedText.Width >> 1);
                int yy = y + (msg.Y - msg.OffsetY) - msg.RenderedText.Height;

                msg.RenderedText.Draw(batcher, xx, yy, msg.Alpha);
            }
        }


        public void Clear()
        {
            _messages.ForEach(s => s.RenderedText.Destroy());
            _messages.Clear();
        }
    }
}