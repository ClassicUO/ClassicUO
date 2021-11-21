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
using System.Collections.Generic;
using System.Text;
using ClassicUO.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ModernBookGump : Gump
    {
        internal const int MAX_BOOK_LINES = 8;
        private const int MAX_BOOK_CHARS_PER_LINE = 53;
        private const int LEFT_X = 38;
        private const int RIGHT_X = 223;
        private const int UPPER_MARGIN = 34;
        private const int PAGE_HEIGHT = 166;

        private GumpPic _forwardGumpPic, _backwardGumpPic;
        private StbTextBox _titleTextBox, _authorTextBox, _bookPageLeft, _bookPageRight;
        private string[] _pagesText;
        private bool[] _pagesChanged;

        public ModernBookGump
        (
            uint serial,
            ushort page_count,
            string title,
            string author,
            bool is_editable,
            bool old_packet
        ) : base(serial, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;

            BookPageCount = page_count;
            IsEditable = is_editable;
            UseNewHeader = !old_packet;

            _pagesText = new string[page_count];
            _pagesChanged = new bool[page_count + 1];

            BuildGump(title, author);
        }


        public ushort BookPageCount { get; internal set; }
        public HashSet<int> KnownPages { get; internal set; } = new HashSet<int>();
        public static bool IsNewBook => Client.Version > ClientVersion.CV_200;
        public bool UseNewHeader { get; set; } = true;
        public static byte DefaultFont => (byte) (IsNewBook ? 1 : 4);

        public bool IntroChanges => _pagesChanged?[0] ?? false;
        internal int MaxPage => (BookPageCount >> 1) + 1;


        public bool SetPageText(string text, int page)
        {
            if (page >= 0 && page < BookPageCount)
            {
                _pagesText[page] = text;

                return true;
            }

            return false;
        }    

        private void BuildGump(string title, string author)
        {
            CanCloseWithRightClick = true;
            WantUpdateSize = false;

            var background = new GumpPic(0, 0, 0x1FE, 0)
            {
                CanMove = true
            };

            Width = background.Width;
            Height = background.Height;

            Add(background);

            Add(_backwardGumpPic = new GumpPic(0, 0, 0x1FF, 0));
            Add(_forwardGumpPic = new GumpPic(356, 0, 0x200, 0));

            _forwardGumpPic.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl)
                {
                    SetActivePage(ActivePage + 1);
                }
            };

            _forwardGumpPic.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl)
                {
                    SetActivePage(MaxPage);
                }
            };

            _backwardGumpPic.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl)
                {
                    SetActivePage(ActivePage - 1);
                }
            };

            _backwardGumpPic.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl)
                {
                    SetActivePage(1);
                }
            };

            const int MAX_WIDTH = 140;

            _bookPageLeft = new StbTextBox
            (
                DefaultFont,
                MAX_BOOK_CHARS_PER_LINE * MAX_BOOK_LINES,
                MAX_WIDTH,
                hue: 1
            )
            {
                X = LEFT_X,
                Y = UPPER_MARGIN,
                Width = MAX_WIDTH + 8,
                Height = PAGE_HEIGHT,
                IsEditable = IsEditable,
                Multiline = true
            };

            _bookPageRight = new StbTextBox
            (
                DefaultFont,
                MAX_BOOK_CHARS_PER_LINE * MAX_BOOK_LINES,
                MAX_WIDTH,
                hue: 1
            )
            {
                X = LEFT_X / 2 + background.Width / 2,
                Y = UPPER_MARGIN,
                Width = MAX_WIDTH + 8,
                Height = PAGE_HEIGHT,
                IsEditable = IsEditable,
                Multiline = true
            };

            _bookPageLeft.TextChanged += BookPageLeft_TextChanged;
            _bookPageRight.TextChanged += BookPageRight_TextChanged;

            Add(_bookPageLeft);
            Add(_bookPageRight);


            Add
            (
                _titleTextBox = new StbTextBox(DefaultFont, 47, 150, IsNewBook, hue: 1)
                {
                    X = 40,
                    Y = 60,
                    Height = 25,
                    Width = 155,
                    IsEditable = IsEditable
                },
                1
            );

            _titleTextBox.SetText(title);
            _titleTextBox.TextChanged += PageZero_TextChanged;
            Add(new Label(ResGumps.By, true, hue: 1) { X = 40, Y = 130 }, 1);

            Add
            (
                _authorTextBox = new StbTextBox(DefaultFont, 29, 150, IsNewBook, hue: 1)
                {
                    X = 40,
                    Y = 160,
                    Height = 25,
                    Width = 155,
                    IsEditable = IsEditable,
                },
                1
            );

            _authorTextBox.SetText(author);
            _authorTextBox.TextChanged += PageZero_TextChanged;

            for (int k = 1, x = 38; k <= BookPageCount; k++)
            {
                if (k % 2 == 1)
                {
                    x = 223; //right hand page
                }
                else
                {
                    x = 38;
                }

                int page = k + 1;

                if (page % 2 == 1)
                {
                    page += 1;
                }

                page >>= 1;
                Add(new Label(k.ToString(), true, 1) { X = x + 80, Y = 200 }, page);
            }

            ActivePage = 1;
            UpdatePageButtonVisibility();

            Client.Game.Scene.Audio.PlaySound(0x0055);
        }

        private void BookPageLeft_TextChanged(object sender, EventArgs e)
        {
            int index = ActivePage - 1;

            if (index >= 0 && index < _pagesChanged.Length)
            {
                _pagesChanged[index] = true;
                _pagesText[index - 1] = _bookPageLeft.Text;
            }
        }

        private void BookPageRight_TextChanged(object sender, EventArgs e)
        {
            int index = ActivePage;

            if (index >= 0 && index < _pagesChanged.Length)
            {
                _pagesChanged[index] = true;
                _pagesText[index - 1] = _bookPageRight.Text;
            }
        }

        public override void OnPageChanged()
        {
            if (ActivePage == 1)
            {
                _bookPageLeft.IsVisible = false;
                _bookPageRight.IsVisible = true;

                _bookPageRight.SetText(_pagesText[ActivePage - 1]);
            }
            else
            {
                _bookPageLeft.IsVisible = true;
                _bookPageRight.IsVisible = true;

                _bookPageLeft.SetText(_pagesText[ActivePage - 1]);
                _bookPageRight.SetText(_pagesText[ActivePage]);
            }

            base.OnPageChanged();
        }

        private void PageZero_TextChanged(object sender, EventArgs e)
        {
            _pagesChanged[0] = true;
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
            _titleTextBox.SetText(title);
            _titleTextBox.IsEditable = editable;
        }

        public void SetAuthor(string author, bool editable)
        {
            _authorTextBox.SetText(author);
            _authorTextBox.IsEditable = editable;
        }

        private void SetActivePage(int page)
        {
            page = Math.Min(Math.Max(page, 1), MaxPage); //clamp the value between 1..MaxPage

            if (page != ActivePage)
            {
                Client.Game.Scene.Audio.PlaySound(0x0055);
            }

            //Non-editable books may only have data for the currently displayed pages,
            //but some servers send their entire contents in one go so we need to keep track of which pages we know
            if (!IsEditable)
            {
                int leftPage = (page - 1) << 1;
                int rightPage = leftPage + 1;

                if (leftPage > 0 && !KnownPages.Contains(leftPage))
                {
                    NetClient.Socket.Send_BookPageDataRequest(LocalSerial, (ushort)leftPage);
                }

                if (rightPage < MaxPage * 2 && !KnownPages.Contains(rightPage))
                {
                    NetClient.Socket.Send_BookPageDataRequest(LocalSerial, (ushort)rightPage);
                }
            }
            else
            {
                for (int i = 0; i < _pagesChanged.Length; i++)
                {
                    if (_pagesChanged[i])
                    {
                        _pagesChanged[i] = false;

                        if (i < 1)
                        {
                            if (UseNewHeader)
                            {
                                NetClient.Socket.Send_BookHeaderChanged(LocalSerial, _titleTextBox.Text, _authorTextBox.Text);
                            }
                            else
                            {
                                NetClient.Socket.Send_BookHeaderChanged_Old(LocalSerial, _titleTextBox.Text, _authorTextBox.Text);
                            }
                        }
                        else
                        {
                            NetClient.Socket.Send_BookPageData(LocalSerial, _pagesText[i - 1], i);
                        }
                    }
                }
            }

            ActivePage = page;
            UpdatePageButtonVisibility();

            //if (UIManager.KeyboardFocusControl == null || UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl && UIManager.KeyboardFocusControl != _bookPage && page != _bookPage._focusPage / 2 + 1)
            //{
            //    UIManager.SystemChat.TextBoxControl.SetKeyboardFocus();
            //}
        }

        public override void OnButtonClick(int buttonID)
        {
        }

        protected override void CloseWithRightClick()
        {
            SetActivePage(0);

            base.CloseWithRightClick();
        }


        //public override void OnHitTestSuccess(int x, int y, ref Control res)
        //{
        //    if (!IsDisposed)
        //    {
        //        int page = -1;

        //        if (ActivePage > 1 && x >= LEFT_X + X && x <= LEFT_X + X + _bookPage.Width)
        //        {
        //            page = (ActivePage - 1) * 2 - 1;
        //        }
        //        else if (ActivePage - 1 < BookPageCount >> 1 && x >= RIGHT_X + X && x <= RIGHT_X + _bookPage.Width + X)
        //        {
        //            page = (ActivePage - 1) * 2;
        //        }

        //        if (page >= 0 && page < BookPageCount && y >= UPPER_MARGIN + Y && y <= UPPER_MARGIN + PAGE_HEIGHT + Y)
        //        {
        //            _bookPage._focusPage = page;
        //            res = _bookPage;
        //        }
        //    }
        //}
    }
}