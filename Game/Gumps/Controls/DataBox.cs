using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Gumps.Controls
{
    class DataBox : GumpControl
    {
        public DataBox(int x, int y, int w, int h)
        {
            CanMove = false;
            AcceptMouseInput = true;
            X = x;
            Y = y;
            Width = w;
            Height = h;
            WantUpdateSize = false;
        }
    }
}
