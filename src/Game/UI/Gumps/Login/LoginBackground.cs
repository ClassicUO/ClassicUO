#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;

namespace ClassicUO.Game.UI.Gumps.Login
{
    internal class LoginBackground : Gump
    {
        public LoginBackground() : base(0, 0)
        {
            if (Client.Version >= ClientVersion.CV_706400)
            {
                // Background
                Add(new GumpPicTiled(0, 0, 640, 480, 0x0150) { AcceptKeyboardInput = false });
                // UO Flag
                Add(new GumpPic(0, 4, 0x0151, 0) { AcceptKeyboardInput = false });
            }
            else
            {
                // Background
                Add(new GumpPicTiled(0, 0, 640, 480, 0x0E14) { AcceptKeyboardInput = false });
                // Border
                Add(new GumpPic(0, 0, 0x157C, 0) { AcceptKeyboardInput = false });
                // UO Flag
                Add(new GumpPic(0, 4, 0x15A0, 0) { AcceptKeyboardInput = false });

                // Quit Button
                Add(new Button(0, 0x1589, 0x158B, 0x158A)
                {
                    X = 555,
                    Y = 4,
                    ButtonAction = ButtonAction.Activate,
                    AcceptKeyboardInput = false
                });
            }


            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptKeyboardInput = false;

            ControlInfo.Layer = UILayer.Under;
        }

        public override void OnButtonClick(int buttonID)
        {
            Client.Game.Exit();
        }
    }
}