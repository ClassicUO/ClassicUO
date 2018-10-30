using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    class BuffIcon
    {
        public BuffIcon(Graphic graphic, long timer, string text)
        {
            Graphic = graphic;
            Timer = timer;
            Text = text;
        }

        public Graphic Graphic { get; }
        public long Timer { get; }
        public string Text { get; }
    }
}
