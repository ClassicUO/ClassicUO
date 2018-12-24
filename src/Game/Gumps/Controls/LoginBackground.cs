using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Gumps.UIGumps;

namespace ClassicUO.Game.Gumps.Controls
{
    class LoginBackground : Gump
    {
        public LoginBackground() : base (0, 0)
        {
            // Background
            AddChildren(new GumpPicTiled(0, 0, 640, 480, 0x0E14) { AcceptKeyboardInput = false });
            // Border
            AddChildren(new GumpPic(0, 0, 0x157C, 0) { AcceptKeyboardInput = false });

            // UO Flag
            AddChildren(new GumpPic(0, 4, 0x15A0, 0) { AcceptKeyboardInput = false });

            
            // Quit Button
            AddChildren(new Button(0, 0x1589, 0x158B, 0x158A)
            {
                X = 555,
                Y = 4,
                ButtonAction = ButtonAction.Activate,
                AcceptKeyboardInput = false
            });
            

            AcceptKeyboardInput = false;

            ControlInfo.Layer = UILayer.Under;
        }

        public override void OnButtonClick(int buttonID)
        {
            Engine.Quit();
        }
    }
}
