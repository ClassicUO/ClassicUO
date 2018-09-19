using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;

namespace ClassicUO.Game.Gumps
{
    class EquipmentSlot : GumpControl
    {
        private double _frameMS;

        public EquipmentSlot(int x, int y)
            : base()
        {
            AcceptMouseInput = true;
            X = x;
            Y = y;
            AddChildren(new GumpPicTiled(0, 0, 19, 20, 0x243A));
            AddChildren(new GumpPic(0, 0, 0x2344, 0));
            ControlInfo.Layer = UILayer.Over;



        }



    }
}
