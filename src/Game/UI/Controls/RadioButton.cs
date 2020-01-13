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

using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class RadioButton : Checkbox
    {
        public RadioButton(int group, List<string> parts, string[] lines) : base(parts, lines)
        {
            GroupIndex = group;
        }

        public RadioButton(int group, ushort inactive, ushort active, string text = "", byte font = 0, ushort color = 0, bool isunicode = true, int maxWidth = 0) : base(inactive, active, text, font, color, isunicode, maxWidth)
        {
            GroupIndex = group;
        }

        public int GroupIndex { get; set; }

        protected override void OnCheckedChanged()
        {
            if (IsChecked)
            {
                if (HandleClick())
                    base.OnCheckedChanged();
            }
        }

        //protected override void OnMouseClick(int x, int y, MouseButton button)
        //{
        //    if (Parent?.FindControls<RadioButton>().Any( s => s.GroupIndex == GroupIndex && s.IsChecked && s != this) == true)
        //        base.OnMouseClick(x, y, button);
        //}

        private bool HandleClick()
        {
            IEnumerable<RadioButton> en = Parent?.FindControls<RadioButton>().Where(s => s.GroupIndex == GroupIndex && s != this);

            if (en == null)
                return false;

            foreach (RadioButton button in en)
                button.IsChecked = false;

            return true;
        }
    }
}