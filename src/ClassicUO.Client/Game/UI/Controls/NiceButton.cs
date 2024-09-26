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

using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    public class NiceButton : HitBox
    {
        private readonly ButtonAction _action;
        private readonly int _groupnumber;
        private bool _isSelected;

        public bool DisplayBorder;

        public NiceButton
        (
            int x,
            int y,
            int w,
            int h,
            ButtonAction action,
            string text,
            int groupnumber = 0,
            TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_CENTER,
            ushort hue = 0xFFFF,
            bool unicode = true,
            byte font = 0xFF
        ) : base(x, y, w, h)
        {
            _action = action;

            Add
            (
                TextLabel = new Label
                (
                    text,
                    unicode,
                    hue,
                    w,
                    font,
                    FontStyle.BlackBorder | FontStyle.Cropped,
                    align
                )
            );

            TextLabel.Y = (h - TextLabel.Height) >> 1;
            _groupnumber = groupnumber;
        }

        internal Label TextLabel { get; }

        public int ButtonParameter { get; set; }

        public bool IsSelectable { get; set; } = true;

        public bool IsSelected
        {
            get => _isSelected && IsSelectable;
            set
            {
                if (!IsSelectable)
                {
                    return;
                }

                _isSelected = value;

                if (value)
                {
                    Control p = Parent;

                    if (p == null)
                    {
                        return;
                    }

                    IEnumerable<NiceButton> list = p.FindControls<NiceButton>();

                    foreach (NiceButton b in list)
                    {
                        if (b != this && b._groupnumber == _groupnumber)
                        {
                            b.IsSelected = false;
                        }
                    }
                }
            }
        }

        internal static NiceButton GetSelected(Control p, int group)
        {
            IEnumerable<NiceButton> list = p.FindControls<NiceButton>();

            foreach (NiceButton b in list)
            {
                if (b._groupnumber == group && b.IsSelected)
                {
                    return b;
                }
            }

            return null;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsSelected = true;

                if (_action == ButtonAction.SwitchPage)
                {
                    ChangePage(ButtonParameter);
                }
                else
                {
                    OnButtonClick(ButtonParameter);
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsSelected)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, Alpha);

                batcher.Draw
                (
                    _texture,
                    new Vector2(x, y),
                    new Rectangle(0, 0, Width, Height),
                    hueVector
                );
            }

            if (DisplayBorder)
            {
                batcher.DrawRectangle(
                    SolidColorTextureCache.GetTexture(Color.LightGray),
                    x, y,
                    Width, Height,
                    ShaderHueTranslator.GetHueVector(0, false, Alpha)
                    );
            }

            return base.Draw(batcher, x, y);
        }
    }
}