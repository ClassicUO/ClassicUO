// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class RadioButton : Checkbox
    {
        public RadioButton(int group, List<string> parts, string[] lines) : base(parts, lines)
        {
            GroupIndex = group;
            IsFromServer = true;
        }

        public RadioButton
        (
            int group,
            ushort inactive,
            ushort active,
            string text = "",
            byte font = 0,
            ushort color = 0,
            bool isunicode = true,
            int maxWidth = 0
        ) : base
        (
            inactive,
            active,
            text,
            font,
            color,
            isunicode,
            maxWidth
        )
        {
            GroupIndex = group;
        }

        public int GroupIndex { get; set; }

        protected override void OnCheckedChanged()
        {
            if (IsChecked)
            {
                if (HandleClick())
                {
                    base.OnCheckedChanged();
                }
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
            {
                return false;
            }

            foreach (RadioButton button in en)
            {
                button.IsChecked = false;
            }

            return true;
        }
    }
}