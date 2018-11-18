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
using ClassicUO.IO;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal abstract class StatusGumpBase : Gump
    {
        public StatusGumpBase() : base(0, 0)
        {
            CanMove = true;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonType)buttonID)
            {
                case ButtonType.BuffIcon:
                    BuffGump.Toggle();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buttonID), buttonID, null);
            }
        }

        protected enum ButtonType
        {
            BuffIcon,
            MinimizeMaximize
        }

        protected enum StatType
        {
            Str,
            Dex,
            Int
        }

        protected Label[] _labels;
        protected double _refreshTime;
        protected Point _point;
        protected readonly bool _useUOPGumps = FileManager.UseUOPGumps;
        protected readonly GumpPic[] _lockers = new GumpPic[3];
    }
}
