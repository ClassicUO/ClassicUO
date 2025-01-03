#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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