using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    class MenuGump : Gump
    {
        private readonly ScrollArea _scrollArea;

        public MenuGump(Serial serial, string name) : base(serial, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;

            Add(new GumpPic(0, 0, 0x0910, 0));
            Add(new ColorBox(217, 49, 0, 0xFF000001)
            {
                X = 40,
                Y = 42,
            });

            Label label = new Label(name, false, 0x0386, 200, 1, FontStyle.Fixed)
            {
                X = 39,
                Y = 18
            };

            Add(label);

            _scrollArea = new ScrollArea(40, 42, 217, 49, true);
            
            Add(_scrollArea);
        }

        public void AddItem(Graphic graphic, Hue hue, string name, int x, int y)
        {
            StaticPic pic = new StaticPic(graphic, hue)
            {
                X = x,
                Y = y,
                AcceptMouseInput = true,
            };
            pic.SetTooltip(name);

            _scrollArea.Add(pic);
        }


    }

    class GrayMenuGump : Gump
    {
        public GrayMenuGump(Serial local) : base(local, 0)
        {
        }
    }
}
