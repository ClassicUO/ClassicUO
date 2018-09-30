using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Gumps.Controls
{
    class GumpPicContainer : GumpPic
    {
        private Item _item;

        public GumpPicContainer(int x, int y, Graphic graphic, Hue hue, Item item) : base(x, y, graphic, hue)
            => _item = item;
    }
}
