#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Gumps.Controls;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class MessageBoxGump : Gump
    {
        private readonly Action<MessageBoxGump> _action;

        public MessageBoxGump(int x, int y, int w, int h, string message, Action<MessageBoxGump> action) : base(0, 0)
        {
            CanMove = false;
            CanCloseWithRightClick = false;
            CanCloseWithEsc = false;
            AcceptMouseInput = false;

            ControlInfo.IsModal = true;
            ControlInfo.Layer = UILayer.Over;
            WantUpdateSize = false;

            X = x;
            Y = y;
            Width = w;
            Height = h;
            _action = action;

            AddChildren(new ResizePic(0x0A28)
            {
                Width = w, Height = h
            });

            AddChildren(new Label(message, false, 0x0386, Width - 90, 1)
            {
                X = 40,
                Y = 45
            });

            AddChildren(new Button(0, 0x0481, 0x0482, 0x0483)
            {
                X = (w >> 1) - 13,
                Y = h - 45,
                ButtonAction = ButtonAction.Activate,               
            });
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _action(this);
                    break;
            }
        }
    }
}
