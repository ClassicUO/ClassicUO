#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NameOverHeadHandlerGump : Gump
    {
        public static Point? LastPosition;

        public override GumpType GumpType => GumpType.NameOverHeadHandler;


        public NameOverHeadHandlerGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            if (LastPosition == null)
            {
                X = 100;
                Y = 100;
            }
            else
            {
                X = LastPosition.Value.X;
                Y = LastPosition.Value.Y;
            }

            WantUpdateSize = false;

            LayerOrder = UILayer.Over;

            RadioButton all, mobiles, items, mobilesCorpses;
            AlphaBlendControl alpha;

            Add
            (
                alpha = new AlphaBlendControl(0.7f)
                {
                    Hue = 34
                }
            );


            Add
            (
                all = new RadioButton
                (
                    0,
                    0x00D0,
                    0x00D1,
                    ResGumps.All,
                    color: 0xFFFF
                )
                {
                    IsChecked = World.NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.All
                }
            );

            Add
            (
                mobiles = new RadioButton
                (
                    0,
                    0x00D0,
                    0x00D1,
                    ResGumps.MobilesOnly,
                    color: 0xFFFF
                )
                {
                    Y = all.Y + all.Height,
                    IsChecked = World.NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.Mobiles
                }
            );

            Add
            (
                items = new RadioButton
                (
                    0,
                    0x00D0,
                    0x00D1,
                    ResGumps.ItemsOnly,
                    color: 0xFFFF
                )
                {
                    Y = mobiles.Y + mobiles.Height,
                    IsChecked = World.NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.Items
                }
            );

            Add
            (
                mobilesCorpses = new RadioButton
                (
                    0,
                    0x00D0,
                    0x00D1,
                    ResGumps.MobilesAndCorpsesOnly,
                    color: 0xFFFF
                )
                {
                    Y = items.Y + items.Height,
                    IsChecked = World.NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.MobilesCorpses
                }
            );

            alpha.Width = Math.Max(mobilesCorpses.Width, Math.Max(items.Width, Math.Max(all.Width, mobiles.Width)));
            alpha.Height = all.Height + mobiles.Height + items.Height + mobilesCorpses.Height;

            Width = alpha.Width;
            Height = alpha.Height;

            all.ValueChanged += (sender, e) =>
            {
                if (all.IsChecked)
                {
                    World.NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.All;
                }
            };

            mobiles.ValueChanged += (sender, e) =>
            {
                if (mobiles.IsChecked)
                {
                    World.NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.Mobiles;
                }
            };

            items.ValueChanged += (sender, e) =>
            {
                if (items.IsChecked)
                {
                    World.NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.Items;
                }
            };

            mobilesCorpses.ValueChanged += (sender, e) =>
            {
                if (mobilesCorpses.IsChecked)
                {
                    World.NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.MobilesCorpses;
                }
            };
        }


        protected override void OnDragEnd(int x, int y)
        {
            LastPosition = new Point(ScreenCoordinateX, ScreenCoordinateY);

            SetInScreen();

            base.OnDragEnd(x, y);
        }
    }
}
