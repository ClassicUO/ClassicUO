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

using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NameOverHeadHandlerGump : Gump
    {
        private static int _lastX = 100, _lastY = 100;

        public NameOverHeadHandlerGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;

            X = _lastX;
            Y = _lastY;
            WantUpdateSize = false;

            ControlInfo.Layer = UILayer.Over;

            RadioButton all, mobiles, items, mobilesCorpses;
            AlphaBlendControl alpha;

            Add(alpha = new AlphaBlendControl(0.2f)
            {
                Hue = 34
            });


            Add(all = new RadioButton(0, 0x00D0, 0x00D1, "All", color: 0xFFFF)
            {
                IsChecked = NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.All
            });

            Add(mobiles = new RadioButton(0, 0x00D0, 0x00D1, "Mobiles only", color: 0xFFFF)
            {
                Y = all.Y + all.Height,
                IsChecked = NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.Mobiles
            });

            Add(items = new RadioButton(0, 0x00D0, 0x00D1, "Items only", color: 0xFFFF)
            {
                Y = mobiles.Y + mobiles.Height,
                IsChecked = NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.Items
            });

            Add(mobilesCorpses = new RadioButton(0, 0x00D0, 0x00D1, "Mobiles and Corpses only", color: 0xFFFF)
            {
                Y = items.Y + items.Height,
                IsChecked = NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.MobilesCorpses
            });

            alpha.Width = Math.Max(mobilesCorpses.Width, Math.Max(items.Width, Math.Max(all.Width, mobiles.Width)));
            alpha.Height = all.Height + mobiles.Height + items.Height + mobilesCorpses.Height;

            Width = alpha.Width;
            Height = alpha.Height;

            all.ValueChanged += (sender, e) =>
            {
                if (all.IsChecked) NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.All;
            };

            mobiles.ValueChanged += (sender, e) =>
            {
                if (mobiles.IsChecked) NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.Mobiles;
            };

            items.ValueChanged += (sender, e) =>
            {
                if (items.IsChecked) NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.Items;
            };

            mobilesCorpses.ValueChanged += (sender, e) =>
            {
                if (mobilesCorpses.IsChecked) NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.MobilesCorpses;
            };
        }


        protected override void OnDragEnd(int x, int y)
        {
            _lastX = ScreenCoordinateX;
            _lastY = ScreenCoordinateY;
            SetInScreen();

            base.OnDragEnd(x, y);
        }
    }
}