﻿#region license

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
    internal sealed class TextContainer
    {
        private readonly List<MessageInfo> _messages = new List<MessageInfo>();
        private readonly Rectangle[] _rects = new Rectangle[2];


        public void Add(string text, ushort hue, byte font, bool isunicode, int x, int y)
        {
            int offset = _messages.Where(s => s.X /*+ (s.RenderedText.Width >> 1)*/ == x && s.Y == y)
                                  .Sum(s => s.RenderedText.Height);

            MessageInfo msg = new MessageInfo
            {
                RenderedText = new RenderedText
                {
                    Font = font,
                    FontStyle = FontStyle.BlackBorder,
                    Hue = hue,
                    IsUnicode = isunicode,
                    MaxWidth = 200,
                    Align = TEXT_ALIGN_TYPE.TS_CENTER,
                    Text = text
                },
                Time = Engine.Ticks + 4000,
                X = x,
                Y = y,
                OffsetY = offset
            };

            //msg.X -= msg.RenderedText.Width >> 1;

            _messages.Add(msg);
        }


        public void Update()
        {
            long t_delta = Engine.Ticks;

            for (int i = 0; i < _messages.Count; i++)
            {
                var msg = _messages[i];
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
                    _rects[0].Y = msg.Y;
                    _rects[0].Width = msg.RenderedText.Width;
                    _rects[0].Height = msg.RenderedText.Height;
                    _rects[0].X -= _rects[0].Width >> 1;

                    for (int j = i + 1; j < _messages.Count; j++)
                    {
                        var m = _messages[j];

                        if (msg.X == m.X && msg.Y == m.Y)
                            continue;

                        _rects[1].X = m.X;
                        _rects[1].Y = m.Y;
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

                msg.RenderedText.Draw(batcher, msg.X + x - (msg.RenderedText.Width >> 1), msg.Y + y + msg.OffsetY, msg.Alpha);
            }
        }


        public void Clear()
        {
            _messages.ForEach(s => s.RenderedText.Destroy());
            _messages.Clear();
        }
    }
}