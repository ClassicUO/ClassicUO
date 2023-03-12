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

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class PopupMenuGump : Gump
    {
        private ushort _selectedItem;
        private readonly PopupMenuData _data;

        public PopupMenuGump(PopupMenuData data) : base(0, 0)
        {
            CanMove = false;
            CanCloseWithRightClick = true;
            _data = data;

            ResizePic pic = new ResizePic(0x0A3C)
            {
                Alpha = 0.75f
            };

            Add(pic);
            int offsetY = 10;
            bool arrowAdded = false;
            int width = 0, height = 20;

            for (int i = 0; i < data.Items.Length; i++)
            {
                ref PopupMenuItem item = ref data.Items[i];

                string text = ClilocLoader.Instance.GetString(item.Cliloc);

                ushort hue = item.Hue;

                if (item.ReplacedHue != 0)
                {
                    uint h = (HuesHelper.Color16To32(item.ReplacedHue) << 8) | 0xFF;
                    
                    FontsLoader.Instance.SetUseHTML(true, h);
                }

                Label label = new Label(text, true, hue, font: 1)
                {
                    X = 10,
                    Y = offsetY
                };

                FontsLoader.Instance.SetUseHTML(false);

                HitBox box = new HitBox(10, offsetY, label.Width, label.Height)
                {
                    Tag = item.Index
                };

                box.MouseEnter += (sender, e) =>
                {
                    _selectedItem = (ushort)(sender as HitBox).Tag;
                };

                Add(box);
                Add(label);

                if ((item.Flags & 0x02) != 0 && !arrowAdded)
                {
                    arrowAdded = true;

                    // TODO: wat?
                    Add
                    (
                        new Button(0, 0x15E6, 0x15E2, 0x15E2)
                        {
                            X = 20,
                            Y = offsetY
                        }
                    );

                    height += 20;
                }

                offsetY += label.Height;

                if (!arrowAdded)
                {
                    height += label.Height;

                    if (width < label.Width)
                    {
                        width = label.Width;
                    }
                }
            }

            width += 20;

            if (height <= 10 || width <= 20)
            {
                Dispose();
            }
            else
            {
                pic.Width = width;
                pic.Height = height;

                foreach (HitBox box in FindControls<HitBox>())
                {
                    box.Width = width - 20;
                }
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                GameActions.ResponsePopupMenu(_data.Serial, _selectedItem);
                Dispose();
            }
        }
    }
}