using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Gumps.Controls;

namespace ClassicUO.Game.Gumps.UIGumps
{
    public class OptionsGump : Gump
    {


        public OptionsGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = false;

            
        }


    }
}
