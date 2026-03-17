// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.Controls
{
    internal class RadioButton : Checkbox
    {
        public RadioButton(int group, List<string> parts, string[] lines, GameContext context) : base(parts, lines, context)
        {
            GroupIndex = group;
            IsFromServer = true;
        }

        public RadioButton
        (
            GameContext context,
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
            context,
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
