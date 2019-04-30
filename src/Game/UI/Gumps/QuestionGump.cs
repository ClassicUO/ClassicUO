#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using ClassicUO.Game.UI.Controls;
using ClassicUO.IO;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class QuestionGump : Gump
    {
        private readonly Action<bool> _result;

        public QuestionGump(string message, Action<bool> result) : base(0, 0)
        {
            Add(new GumpPic(0, 0, 0x0816, 0));

            SpriteTexture t = FileManager.Gumps.GetTexture(0x0816);

            Width = t.Width;
            Height = t.Height;


            Add(new Label(message, false, 0x0386, 165)
            {
                X = 33, Y = 30
            });

            Add(new Button((int) Buttons.Cancel, 0x817, 0x818, 0x0819)
            {
                X = 37, Y = 75, ButtonAction = ButtonAction.Activate
            });

            Add(new Button((int) Buttons.Ok, 0x81A, 0x81B, 0x081C)
            {
                X = 100, Y = 75, ButtonAction = ButtonAction.Activate
            });
            CanMove = false;
            ControlInfo.IsModal = true;

            X = (Engine.WindowWidth - Width) >> 1;
            Y = (Engine.WindowHeight - Height) >> 1;

            WantUpdateSize = false;
            _result = result;
        }


        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    _result(false);
                    Dispose();

                    break;
                case 1:
                    _result(true);
                    Dispose();

                    break;
            }
        }

        private enum Buttons
        {
            Cancel,
            Ok
        }
    }
}