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
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ClassicUO.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class BookGump : Gump
    {
        private const int MAX_BOOK_LINES = 8;
        private const int MAX_BOOK_CHARS_PER_PAGE = 53;


        private readonly StbPageTextBox[] _pagesTextBoxes;
        private GumpPic _forwardGumpPic, _backwardGumpPic;
        private bool[] _pagesChanged;
        private StbTextBox _titleTextBox, _authorTextBox;


        public BookGump(uint serial, ushort page_count, string title, string author, bool is_editable, bool old_packet) : base(serial, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;

            BookPageCount = page_count;
            _pagesTextBoxes = new StbPageTextBox[page_count];
            IsEditable = is_editable;
            UseNewHeader = !old_packet;

            BuildGump(title, author);
        }


        public ushort BookPageCount { get; internal set; }
        public static bool IsNewBook => Client.Version > ClientVersion.CV_200;
        public bool UseNewHeader { get; set; } = true;
        public static byte DefaultFont => (byte) (IsNewBook ? 1 : 4);

       


        public bool IntroChanges => _pagesChanged[0];
        private int MaxPage => (BookPageCount >> 1) + 1;


        private void BuildGump(string title, string author)
        {
            CanCloseWithRightClick = true;
            Add(new GumpPic(0, 0, 0x1FE, 0)
            {
                CanMove = true
            });

            Add(_backwardGumpPic = new GumpPic(0, 0, 0x1FF, 0));

            Add(_forwardGumpPic = new GumpPic(356, 0, 0x200, 0));

            _forwardGumpPic.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl)
                    SetActivePage(ActivePage + 1);
            };

            _forwardGumpPic.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl)
                    SetActivePage(MaxPage);
            };

            _backwardGumpPic.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl)
                    SetActivePage(ActivePage - 1);
            };

            _backwardGumpPic.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl)
                    SetActivePage(1);
            };

            _pagesChanged = new bool[BookPageCount + 1];
            Add(_titleTextBox = new StbTextBox(DefaultFont, 47, 150, IsNewBook, FontStyle.None, 0)
            {
                X = 40,
                Y = 60,
                Height = 25,
                Width = 155,
                IsEditable = IsEditable,
                Text = title
            }, 1);
            Add(new Label("by", true, 1) { X = 40, Y = 130 }, 1);
            Add(_authorTextBox = new StbPageTextBox(DefaultFont, 29, 150, IsNewBook, FontStyle.None, 0)
            {
                X = 40,
                Y = 160,
                Height = 25,
                Width = 155,
                IsEditable = IsEditable,
                Text = author
            }, 1);

            for (int k = 1; k <= BookPageCount; k++)
            {
                int x = 38;
                int y = 34;

                if (k % 2 == 1)
                {
                    x = 223;
                    //right hand page
                }

                int page = k + 1;

                if (page % 2 == 1)
                    page += 1;
                page >>= 1;

                StbPageTextBox tbox = new StbPageTextBox(DefaultFont, MAX_BOOK_CHARS_PER_PAGE * MAX_BOOK_LINES, 155, IsNewBook, FontStyle.ExtraHeight, 2)
                {
                    X = x,
                    Y = y,
                    Height = 166,
                    Width = 160,
                    IsEditable = IsEditable,
                    Multiline = true,
                    Tag = k
                    //MaxLines = MAX_BOOK_LINES
                };
                tbox.TextChanged += OnTextChanged;
                Add(tbox, page);
                _pagesTextBoxes[k - 1] = tbox;

                Add(new Label(k.ToString(), true, 1) { X = x + 80, Y = 200 }, page);
            }

            ActivePage = 1;
            UpdatePageButtonVisibility();

            Client.Game.Scene.Audio.PlaySound(0x0055);
        }


        private void OnTextChanged(object sender, EventArgs e)
        {
            StbPageTextBox c = (StbPageTextBox) sender;

            if (c != null)
            {
                _pagesChanged[(int) c.Tag] = true;
            }
        }

        private int GetActivePage()
        {
            for (int i = 0; i < _pagesTextBoxes.Length; i++)
            {
                var p = _pagesTextBoxes[i];

                if (p != null)
                {
                    if ((IsEditable && p.HasKeyboardFocus) || (!IsEditable && p.MouseIsOver) )
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void UpdatePageButtonVisibility()
        {
            if (ActivePage == 1)
            {
                _backwardGumpPic.IsVisible = false;
                _forwardGumpPic.IsVisible = true;
            }
            else if (ActivePage == MaxPage)
            {
                _forwardGumpPic.IsVisible = false;
                _backwardGumpPic.IsVisible = true;
            }
            else
            {
                _backwardGumpPic.IsVisible = true;
                _forwardGumpPic.IsVisible = true;
            }
        }

        public void SetTile(string title, bool editable)
        {
            _titleTextBox.Text = title;
            _titleTextBox.IsEditable = editable;
        }

        public void SetAuthor(string author, bool editable)
        {
            _authorTextBox.Text = author;
            _authorTextBox.IsEditable = editable;
        }

        public void SetTextToPage(string text, int page)
        {
            if (page >= 0 && page < _pagesTextBoxes.Length && _pagesTextBoxes[page] != null)
            {
                _pagesTextBoxes[page].IsEditable = IsEditable;
                _pagesTextBoxes[page].Text = text;
                _pagesChanged[page + 1] = false;
            }
        }

        public string GetPageText(int page)
        {
            if (page >= 0 && page < _pagesTextBoxes.Length)
            {
                return _pagesTextBoxes[page]?.Text;
            }

            return string.Empty;
        }

        private void SetActivePage(int page)
        {
            page = Math.Min(Math.Max(page, 1), MaxPage); //clamp the value between 1..MaxPage
            if (page != ActivePage)
            {
                Client.Game.Scene.Audio.PlaySound(0x0055);
            }

            //Non-editable books only have page data sent for currently viewed pages
            if (!IsEditable)
            {
                int leftPage = (page - 1) << 1;
                int rightPage = leftPage + 1;
                if (leftPage > 0)
                {
                    NetClient.Socket.Send(new PBookPageDataRequest(LocalSerial, (ushort)leftPage));
                }
                if (leftPage + 1 < MaxPage * 2)
                {
                    NetClient.Socket.Send(new PBookPageDataRequest(LocalSerial, (ushort)rightPage));
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    int real_page = ((ActivePage - 1) << 1) + i;

                    if (real_page < _pagesChanged.Length && _pagesChanged[real_page])
                    {
                        _pagesChanged[real_page] = false;

                        if (real_page < 1)
                        {
                            if (UseNewHeader)
                                NetClient.Socket.Send(new PBookHeaderChanged(LocalSerial, _titleTextBox.Text, _authorTextBox.Text));
                            else
                                NetClient.Socket.Send(new PBookHeaderChangedOld(LocalSerial, _titleTextBox.Text, _authorTextBox.Text));
                        }
                        else
                        {
                            NetClient.Socket.Send(new PBookPageData(LocalSerial, GetPageText(real_page - 1), real_page - 1));
                        }
                    }
                }
                
            }


            ActivePage = page;
            UpdatePageButtonVisibility();


            if (UIManager.KeyboardFocusControl == null || (UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl && UIManager.KeyboardFocusControl.Page != page))
            {
                UIManager.SystemChat.TextBoxControl.SetKeyboardFocus();
            }
        }

        public override void OnButtonClick(int buttonID)
        {
           
        }

        protected override void CloseWithRightClick()
        {
            SetActivePage(0);

            base.CloseWithRightClick();
        }

        public override void Update(double totalMS, double frameMS)
        {

            base.Update(totalMS, frameMS);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            int curpage = GetActivePage();

            var box = curpage >= 0 ? _pagesTextBoxes[curpage] : null;
        }

        private class StbPageTextBox : StbTextBox
        {
            public StbPageTextBox(byte font, int max_char_count = -1, int maxWidth = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0, TEXT_ALIGN_TYPE align = TEXT_ALIGN_TYPE.TS_LEFT) : base(font, max_char_count, maxWidth, isunicode, style, hue, align)
            {
            }


            protected override void OnTextInput(string c)
            {
                MultilinesFontInfo info = CalculateFontInfo(c);

                int lines = 0;
                while (info != null) { lines++; info = info.Next; }

                if (lines > 8)
                {

                }

                base.OnTextInput(c);
            }

           

            protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                base.OnKeyDown(key, mod);
            }
        }
    }
}