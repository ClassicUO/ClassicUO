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

using System;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TipNoticeGump : Gump
    {
        internal static TipNoticeGump _tips;
        private readonly ExpandableScroll _background;
        private readonly OrderedDictionary<uint, string> _pages;
        private readonly Button _prev, _next;
        private readonly ScrollArea _scrollArea;
        private readonly MultiLineBox _textBox;
        private int _idx;

        public TipNoticeGump(byte type, string page) : base(0, 0)
        {
            Height = 300;
            CanMove = true;
            CanCloseWithRightClick = true;
            _scrollArea = new ScrollArea(0, 32, 272, Height - 96, false);

            _textBox = new MultiLineBox(new MultiLineEntry(1, -1, 0, 220, true, hue: 0), false)
            {
                Height = 20,
                X = 35,
                Y = 0,
                Text = page
            };
            Add(_background = new ExpandableScroll(0, 0, Height, 0x0820));
            _scrollArea.Add(_textBox);
            Add(_scrollArea);

            if (type == 0)
            {
                _pages = new OrderedDictionary<uint, string>();
                _tips = this;
                _background.TitleGumpID = 0x9CA;
                _idx = 0;
                Add(_prev = new Button(0, 0x9cc, 0x9cc) {X = 35, ContainsByBounds = true});

                _prev.MouseUp += (o, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                        SetPage(_idx - 1);
                };
                Add(_next = new Button(0, 0x9cd, 0x9cd) {X = 240, ContainsByBounds = true});

                _next.MouseUp += (o, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                        SetPage(_idx + 1);
                };
            }
            else
                _background.TitleGumpID = 0x9D2;
        }

        //public override GUMP_TYPE GumpType => GUMP_TYPE.GT_TIPNOTICE;

        public override void OnButtonClick(int buttonID)
        {
            // necessary to avoid closing
        }

        public override void Dispose()
        {
            _tips = null;
            base.Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (!_textBox.IsDisposed && _textBox.IsChanged)
            {
                _textBox.Height = Math.Max(FontsLoader.Instance.GetHeightUnicode(1, _textBox.TxEntry.Text, 220, TEXT_ALIGN_TYPE.TS_LEFT, 0x0) + 20, 40);

                foreach (Control c in _scrollArea.Children)
                {
                    if (c is ScrollAreaItem)
                        c.OnPageChanged();
                }
            }

            base.Update(totalMS, frameMS);
        }

        public override void OnPageChanged()
        {
            Height = _background.SpecialHeight;
            _scrollArea.Height = _background.SpecialHeight - 96;

            foreach (Control c in _scrollArea.Children)
            {
                if (c is ScrollAreaItem)
                    c.OnPageChanged();
            }

            if (_prev != null && _next != null) _prev.Y = _next.Y = _background.SpecialHeight - 53;
        }

        internal void AddTip(uint tipnum, string entry)
        {
            _pages.SetValue(tipnum, entry);
        }

        private void SetPage(int page)
        {
            if (page >= 0 && page < _pages.Count)
            {
                _idx = page;
                _textBox.Text = _pages[_idx];
            }
        }
    }
}