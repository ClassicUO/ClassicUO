using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class TradingGump : Gump
    {
        public TradingGump(Serial local) : base(local, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
        }


    }
}
