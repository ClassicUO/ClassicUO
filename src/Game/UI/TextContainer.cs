using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace ClassicUO.Game.UI
{
    sealed class TextContainer
    {
        private readonly List<MessageInfo> _messages = new List<MessageInfo>();


        public void Add(string text, ushort hue, byte font, bool isunicode, int x, int y)
        {
            int offset = _messages.Where(s => s.X /*+ (s.RenderedText.Width >> 1)*/ == x && s.Y == y)
                .Sum(s => s.RenderedText.Height);

            MessageInfo msg = new MessageInfo()
            {
                RenderedText = new RenderedText()
                {
                    Font = font,
                    FontStyle = FontStyle.BlackBorder,
                    Hue = hue,
                    IsUnicode = isunicode,
                    MaxWidth = 200,
                    Align = TEXT_ALIGN_TYPE.TS_CENTER,
                    Text = text,
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
                    Rectangle r1 = new Rectangle(msg.X, msg.Y, msg.RenderedText.Width, msg.RenderedText.Height);
                    r1.X -= r1.Width >> 1;

                    for (int j = i + 1; j < _messages.Count; j++)
                    {
                        var m = _messages[j];

                        if (msg.X == m.X && msg.Y == m.Y)
                            continue;
                        
                        Rectangle r2 = new Rectangle(m.X, m.Y, m.RenderedText.Width, m.RenderedText.Height);
                        r2.X -= r2.Width >> 1;

                        if (r1.Intersects(r2))
                        {
                            msg.Alpha = 0.3f + (0.05f * count);
                            count++;
                        }
                    }

                }
            }
        }

        public void Draw(Batcher2D batcher, int x, int y)
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
