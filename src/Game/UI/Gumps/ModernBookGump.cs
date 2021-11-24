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
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ModernBookGump : Gump
    {
        private const int MAX_BOOK_LINES = 8;
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

            _bookPageLeft.KeyDown += (sender, e) => 
            {
                if (e.Key == SDL2.SDL.SDL_Keycode.SDLK_BACKSPACE)
                {
                    if (_bookPageLeft.CaretIndex <= 0)
                    {
                        SetActivePage(Math.Max(1, ActivePage - 1));
                        _bookPageRight?.SetKeyboardFocus();
                    }
                }
            };

            _bookPageRight.KeyDown += (sender, e) =>
            {
                if (e.Key == SDL2.SDL.SDL_Keycode.SDLK_BACKSPACE)
                {
                    if (_bookPageRight.CaretIndex <= 0)
                    {
                        SetActivePage(Math.Max(1, ActivePage - 1));
                        _bookPageLeft?.SetKeyboardFocus();
                    }
                }
            };

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

        [Flags]
        enum TextBoxFlag
        {
            None,
            WorkingOnLeft,
            WorkingOnRight
        }

        private TextBoxFlag _textboxFlag;

        private void BookPageLeft_TextChanged(object sender, EventArgs e)
        {
            if ((_textboxFlag & TextBoxFlag.WorkingOnLeft) != 0)
            {
                return;
            }

            _textboxFlag |= TextBoxFlag.WorkingOnLeft;

            int index = Math.Min(Math.Max(ActivePage, 1), MaxPage);
            int rightPage = ((index - 1) << 1);
            int leftPage = rightPage - 1;

            if (leftPage >= 0 && leftPage < _pagesChanged.Length)
            {
                _pagesChanged[leftPage + 1] = true;
                _pagesText[leftPage] = _bookPageLeft.Text;
            
                if (leftPage + 1 < _pagesText.Length)
                {
                    var i = GetIndexOfLargeText(_bookPageLeft.Text.AsSpan(), _bookPageLeft.FontSettings, _bookPageLeft.MaxWidth, _bookPageLeft.Height);

                    if (i >= 0)
                    {
                        var span = _bookPageLeft.Text.AsSpan(i);

                        if (!span.IsEmpty)
                        {
                            _pagesText[leftPage] = _bookPageLeft.Text.Substring(0, i);
                            _pagesText[leftPage + 1] = $"{span.ToString()}{_bookPageRight.Text}";
                            _pagesChanged[leftPage + 2] = true;

                            _bookPageLeft.Text = (_pagesText[leftPage]);
                            _bookPageRight.Text = (_pagesText[leftPage + 1]);

                            if (_bookPageLeft.CaretIndex >= i)
                            {
                                _bookPageRight.CaretIndex = 0;
                                _bookPageRight.SetKeyboardFocus();
                            }  
                        }
                    }
                }
            }

            _textboxFlag &= ~TextBoxFlag.WorkingOnLeft;
        }
        
        private void BookPageRight_TextChanged(object sender, EventArgs e)
        {
            if ((_textboxFlag & TextBoxFlag.WorkingOnRight) != 0)
            {
                return;
            }

            _textboxFlag |= TextBoxFlag.WorkingOnRight;

            int index = Math.Min(Math.Max(ActivePage, 1), MaxPage);
            int rightPage = ((index - 1) << 1);
            int leftPage = rightPage - 1;

            if (rightPage >= 0 && rightPage < _pagesChanged.Length)
            {
                _pagesChanged[rightPage + 1] = true;
                _pagesText[rightPage] = _bookPageRight.Text;

                if (rightPage + 1 < _pagesText.Length)
                {
                    var i = GetIndexOfLargeText(_bookPageRight.Text.AsSpan(), _bookPageRight.FontSettings, _bookPageRight.MaxWidth, _bookPageRight.Height);

                    if (i >= 0)
                    {
                        var span = _bookPageRight.Text.AsSpan(i);

                        if (!span.IsEmpty)
                        {
                            _pagesText[rightPage] = _bookPageRight.Text.Substring(0, i);
                            _pagesText[rightPage + 1] = $"{span.ToString()}{_bookPageLeft.Text}";
                            _pagesChanged[rightPage + 2] = true;

                            _bookPageRight.Text = (_pagesText[rightPage]);
                            _bookPageLeft.Text = (_pagesText[rightPage + 1]);

                            if (_bookPageRight.CaretIndex >= i)
                            {
                                if (ActivePage + 1 < BookPageCount)
                                {
                                    ActivePage++;
                                    _bookPageLeft.CaretIndex = 0;
                                    _bookPageLeft.SetKeyboardFocus();
                                }
                            }
                        }
                    }
                }
            }

            _textboxFlag &= ~TextBoxFlag.WorkingOnRight;
        }

        private int GetIndexOfLargeText(ReadOnlySpan<char> text, in FontSettings fs, float maxWidth, float maxHeight)
        {
            var fontHeight = UOFontRenderer.Shared.GetFontHeight(fs);

            Vector2 size = new Vector2(0, fontHeight);

            for (int i = 0; i < text.Length; ++i)
            {
                var charsize = text[i] == '\n' ? 0.0f : UOFontRenderer.Shared.MeasureString(text.Slice(i, 1), fs, 1f).X;

                size.X += charsize;

                if (text[i] == '\n' || (maxWidth > 0.0f && size.X > maxWidth))
                {
                    size.X = charsize;
                    size.Y += fontHeight;

                    if (size.Y > maxHeight || size.Y / fontHeight > MAX_BOOK_LINES)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public override void OnPageChanged()
        {
            UpdatePageButtonVisibility();

            int index = Math.Min(Math.Max(ActivePage, 1), MaxPage);
            int rightPage = ((index - 1) << 1);
            int leftPage = rightPage - 1;

            _bookPageLeft.IsVisible = leftPage >= 0 && leftPage < _pagesText.Length;
            _bookPageRight.IsVisible = rightPage >= 0 && rightPage < _pagesText.Length;

            if (_bookPageLeft.IsVisible)
            {
                _textboxFlag |= TextBoxFlag.WorkingOnLeft;
                _bookPageLeft.SetText(_pagesText[leftPage]);
                _textboxFlag &= ~TextBoxFlag.WorkingOnLeft;
            }

            if (_bookPageRight.IsVisible)
            {
                _textboxFlag |= TextBoxFlag.WorkingOnRight;
                _bookPageRight.SetText(_pagesText[rightPage]);
                _textboxFlag &= ~TextBoxFlag.WorkingOnRight;
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
                            ValueStringBuilder sb = new ValueStringBuilder();
                            var span = _pagesText[i - 1].AsSpan();
                            var fontSettings = (i - 1) % 2 == 0 ? _bookPageLeft.FontSettings : _bookPageRight.FontSettings;
                            float width = 0.0f;

                            for (int j = 0; j < span.Length; ++j)
                            {
                                var c = span[j];
                                var size = UOFontRenderer.Shared.MeasureString(span.Slice(j, 1), fontSettings, 1f);

                                width += size.X;

                                if ((_bookPageLeft.MaxWidth > 0.0f && width > _bookPageLeft.MaxWidth))
                                {
                                    width = size.X;

                                    sb.Append('\n');
                                }

                                sb.Append(c);
                            }


                            NetClient.Socket.Send_BookPageData(LocalSerial, sb.ToString(), i);

                            sb.Dispose();
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
    }
}