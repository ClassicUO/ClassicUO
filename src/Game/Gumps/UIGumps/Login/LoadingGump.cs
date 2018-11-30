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

using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class LoadingGump : Gump
    {
        [Flags]
        public enum Buttons
        {
            None = 1,
            OK = 2,
            Cancel = 4
        }

        private readonly Action<int> _buttonClick;
        private Buttons _showButtons;

        public LoadingGump(string labelText, Buttons showButtons, Action<int> buttonClick = null) : base(0, 0)
        {
            _showButtons = showButtons;
            _buttonClick = buttonClick;

            AddChildren(new ResizePic(0x0A28)
            {
                X = 142, Y = 134, Width = 356, Height = 212
            });

            AddChildren(new Label(labelText, false, 0x0386, 326, 2, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 162, Y = 178
            });

            if (showButtons == Buttons.OK)
            {
                AddChildren(new Button((int) Buttons.OK, 0x0481, 0x0483, 0x0482)
                {
                    X = 306, Y = 304, ButtonAction = ButtonAction.Activate
                });
            }
            else if (showButtons == (Buttons.OK | Buttons.Cancel))
            {
                AddChildren(new Button((int) Buttons.OK, 0x0481, 0x0483, 0x0482)
                {
                    X = 264, Y = 304, ButtonAction = ButtonAction.Activate
                });

                AddChildren(new Button((int) Buttons.Cancel, 0x047E, 0x0480, 0x047F)
                {
                    X = 348, Y = 304, ButtonAction = ButtonAction.Activate
                });
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            _buttonClick?.Invoke(buttonID);
            base.OnButtonClick(buttonID);
        }
    }
}