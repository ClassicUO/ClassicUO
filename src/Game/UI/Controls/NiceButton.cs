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

using System.Collections.Generic;
using System.Linq;

using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class NiceButton : HitBox
    {
        private readonly ButtonAction _action;
        private readonly int _groupnumber;
        private bool _isSelected;

        public NiceButton(int x, int y, int w, int h, ButtonAction action, string text, int groupnumber = 0, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_CENTER) : base(x, y, w, h)
        {
            _action = action;
            Label label;

            Add(label = new Label(text, true, 999, w, 0xFF, FontStyle.BlackBorder | FontStyle.Cropped, align)
            {
                X = align == TEXT_ALIGN_TYPE.TS_CENTER ? -2 : 0
            });
            label.Y = (h - label.Height) >> 1;
            _groupnumber = groupnumber;
        }

        public int ButtonParameter { get; set; }

        public bool IsSelectable { get; set; } = true;

        public bool IsSelected
        {
            get => _isSelected && IsSelectable;
            set
            {
                if (!IsSelectable)
                    return;

                _isSelected = value;

                if (value)
                {
                    Control p = Parent;

                    if (p == null)
                        return;

                    IEnumerable<NiceButton> list;

                    if (p is ScrollAreaItem)
                    {
                        p = p.Parent;
                        list = p.FindControls<ScrollAreaItem>().SelectMany(s => s.Children.OfType<NiceButton>());
                    }
                    else
                        list = p.FindControls<NiceButton>();

                    foreach (var b in list)
                        if (b != this && b._groupnumber == _groupnumber)
                            b.IsSelected = false;
                }
            }
        }

        internal static NiceButton GetSelected(Control p, int group)
        {
            IEnumerable<NiceButton> list = p is ScrollArea ? p.FindControls<ScrollAreaItem>().SelectMany(s => s.Children.OfType<NiceButton>()) : p.FindControls<NiceButton>();

            foreach (var b in list)
                if (b._groupnumber == group && b.IsSelected)
                    return b;

            return null;
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                IsSelected = true;

                if (_action == ButtonAction.SwitchPage)
                    ChangePage(ButtonParameter);
                else
                    OnButtonClick(ButtonParameter);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsSelected)
            {
                ResetHueVector();
                ShaderHuesTraslator.GetHueVector(ref _hueVector, 0, false, Alpha);
                batcher.Draw2D(_texture, x, y, 0, 0, Width, Height, ref _hueVector);
            }

            return base.Draw(batcher, x, y);
        }
    }
}