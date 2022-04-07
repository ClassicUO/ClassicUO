#region license

// Copyright (c) 2021, andreakarasho
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
using ClassicUO.Game.UI.Controls;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NameOverHeadHandlerGump : Gump
    {
        public static Point? LastPosition;

        public override GumpType GumpType => GumpType.NameOverHeadHandler;


        public NameOverHeadHandlerGump() : base(0, 0)
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

            AlphaBlendControl alpha;

            Add
            (
                alpha = new AlphaBlendControl(0.7f)
                {
                    Hue = 34
                }
            );

            int biggestWidth = 100;
            for (int i = 0; i < NameOverHeadManager.Options.Count; i++)
            {
                biggestWidth = Math.Max(biggestWidth, AddOverheadOptionButton(NameOverHeadManager.Options[i], i).Width);
            }

            // alpha.Width = Math.Max(mobilesCorpses.Width, Math.Max(items.Width, Math.Max(all.Width, mobiles.Width)));
            // alpha.Height = all.Height + mobiles.Height + items.Height + mobilesCorpses.Height;
            alpha.Width = biggestWidth;
            alpha.Height = Math.Max(30, NameOverHeadManager.Options.Count * 20);

            Width = alpha.Width;
            Height = alpha.Height;
        }

        private RadioButton AddOverheadOptionButton(NameOverheadOption option, int index)
        {
            RadioButton button;

            Add
            (
                button = new RadioButton
                (
                    0, 0x00D0, 0x00D1, option.Name,
                    color: 0xFFFF
                )
                {
                    Y = 20 * index,
                    IsChecked = NameOverHeadManager.LastActiveNameOverheadOption == option.Name,
                }
            );

            button.ValueChanged += (sender, e) =>
            {
                if (button.IsChecked)
                {
                    NameOverHeadManager.ActiveOverheadOptions = (NameOverheadOptions)option.NameOverheadOptionFlags;
                }
            };

            return button;
        }


        protected override void OnDragEnd(int x, int y)
        {
            LastPosition = new Point(ScreenCoordinateX, ScreenCoordinateY);

            SetInScreen();

            base.OnDragEnd(x, y);
        }
    }
}
