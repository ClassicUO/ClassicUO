using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    class BuffIcon
    {
        public BuffIcon(Graphic graphic, ushort timer, string text)
        {
            Graphic = graphic;
            Timer = timer;
            Text = text;
        }

        public Graphic Graphic { get; }
        public ushort Timer { get; }
        public string Text { get; }
    }
}
