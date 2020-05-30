using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    internal class ModernBookGump : Gump
    {
        internal const int MAX_BOOK_LINES = 8;
        private const int MAX_BOOK_CHARS_PER_LINE = 53;
        private const int BOOK_PAGE_HEIGHT = 166;
        internal string[] BookLines => _bookPage._pageLines;
        internal bool[] _pagesChanged => _bookPage._pagesChanged;

        private GumpPic _forwardGumpPic, _backwardGumpPic;
        private StbTextBox _titleTextBox, _authorTextBox;
        private StbPageTextBox _bookPage;

        public ModernBookGump(uint serial, ushort page_count, string title, string author, bool is_editable, bool old_packet) : base(serial, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;

            BookPageCount = page_count;
            IsEditable = is_editable;
            UseNewHeader = !old_packet;

            BuildGump(title, author);
        }


        public ushort BookPageCount { get; internal set; }
        public static bool IsNewBook => Client.Version > ClientVersion.CV_200;
        public bool UseNewHeader { get; set; } = true;
        public static byte DefaultFont => (byte)(IsNewBook ? 1 : 4);

        internal void SetBookText(string text)
        {
            _bookPage.NoReformat = true;
            _bookPage.Text = text;
            _bookPage.NoReformat = false;
        }

        public bool IntroChanges => _pagesChanged[0];
        internal int MaxPage => (BookPageCount >> 1) + 1;


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

            _bookPage = new StbPageTextBox(DefaultFont, BookPageCount, MAX_BOOK_CHARS_PER_LINE * MAX_BOOK_LINES, 160, IsNewBook, FontStyle.ExtraHeight, 2)
            {
                X = 0,
                Y = 0,
                Height = 166 * BookPageCount,
                Width = 160,
                IsEditable = IsEditable,
                Multiline = true
            };
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
            Add(_authorTextBox = new StbTextBox(DefaultFont, 29, 150, IsNewBook, FontStyle.None, 0)
            {
                X = 40,
                Y = 160,
                Height = 25,
                Width = 155,
                IsEditable = IsEditable,
                Text = author
            }, 1);

            for (int k = 1, x = 38; k <= BookPageCount; k++)
            {
                if (k % 2 == 1)
                    x = 223;//right hand page
                else
                    x = 38;
                int page = k + 1;
                if (page % 2 == 1)
                    page += 1;
                
                page >>= 1;
                Add(new Label(k.ToString(), true, 1) { X = x + 80, Y = 200 }, page);
            }

            ActivePage = 1;
            UpdatePageButtonVisibility();

            Client.Game.Scene.Audio.PlaySound(0x0055);
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
                for (int i = 0; i < _pagesChanged.Length; i++)
                {
                    if (_pagesChanged[i])
                    {
                        _pagesChanged[i] = false;
                        if (i < 1)
                        {
                            if (UseNewHeader)
                                NetClient.Socket.Send(new PBookHeaderChanged(LocalSerial, _titleTextBox.Text, _authorTextBox.Text));
                            else
                                NetClient.Socket.Send(new PBookHeaderChangedOld(LocalSerial, _titleTextBox.Text, _authorTextBox.Text));
                        }
                        else
                        {
                            string[] text = new string[MAX_BOOK_LINES];
                            for(int x = (i - 1) * MAX_BOOK_LINES, l = 0; x < ((i - 1) * MAX_BOOK_LINES) + 8; x++, l++)
                            {
                                text[l] = BookLines[x];
                            }
                            NetClient.Socket.Send(new PBookPageData(LocalSerial, text, i));
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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);
            int startpage = (ActivePage - 1) * 2;
            var t = _bookPage.renderedText;
            if (startpage < BookPageCount)
            {
                t.Draw(batcher, t.Width, _bookPage._pageCoords[startpage, 1], x + 223, y + 34, t.Width, _bookPage._pageCoords[startpage, 1], 0, 0);
            }
            startpage--;
            if(startpage > 0)
            {
                t.Draw(batcher, t.Width, _bookPage._pageCoords[startpage, 1], x + 38, y + 34, t.Width, _bookPage._pageCoords[startpage, 1], 0, 0);
            }
            return true;
        }

        private class StbPageTextBox : StbTextBox
        {
            internal bool NoReformat { get; set; }
            internal readonly int[,] _pageCoords;
            internal readonly string[] _pageLines;
            internal bool[] _pagesChanged;
            internal RenderedText renderedText => _rendererText;

            public StbPageTextBox(byte font, int bookpages, int max_char_count = -1, int maxWidth = 0, bool isunicode = true, FontStyle style = FontStyle.None, ushort hue = 0) : base(font, max_char_count, maxWidth, isunicode, style, hue, TEXT_ALIGN_TYPE.TS_LEFT)
            {
                _pageCoords = new int[bookpages, 2];
                _pageLines = new string[bookpages * MAX_BOOK_LINES];
                _pagesChanged = new bool[bookpages + 1];
            }

            private int GetCharWidth(char c)
            {
                if (IsNewBook)
                    return FontsLoader.Instance.GetCharWidthUnicode(_rendererText.Font, c);
                return FontsLoader.Instance.GetCharWidthASCII(_rendererText.Font, c);
            }

            private void UpdatePageCoords()
            {
                MultilinesFontInfo info = _rendererText.GetInfo();
                for (int page = 0, y = 0; page < _pageCoords.GetLength(0); page++)
                {
                    _pageCoords[page, 0] = y;
                    for (int i = 0; i < MAX_BOOK_LINES; i++)
                    {
                        if (info == null)
                            break;
                        _pageCoords[page, 1] += info.MaxHeight;
                        info = info.Next;
                    }
                    y += _pageCoords[page, 1];
                }
            }

            private static readonly StringBuilder _sb = new StringBuilder();

            protected override void OnTextChanged()
            {
                base.OnTextChanged();
                if (!NoReformat)
                {
                    string[] split = Text.Split('\n');
                    for (int i = 0; i < split.Length; i++, _sb.Append('\n'))
                    {
                        for (int p = 0, w = 0, pw = split[i][p]; p < split[i].Length; p++, pw = GetCharWidth(split[i][p]))
                        {
                            if (w + pw > Width)
                            {
                                _sb.Append('\n');
                                w = 0;
                            }
                            w += pw;
                            _sb.Append(split[i][p]);
                        }
                    }
                    split = _sb.ToString().Split('\n');
                    _sb.Clear();
                    for (int i = 0; i < _pageLines.Length; i++)
                    {
                        if (i < split.Length)
                        {
                            if (!_pagesChanged[(i >> 3) + 1] && split[i] != _pageLines[i])
                                _pagesChanged[(i >> 1) + 1] = true;
                            _sb.Append(_pageLines[i] = split[i]);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(_pageLines[i]))
                                _pagesChanged[(i >> 3) + 1] = true;
                            _pageLines[i] = string.Empty;
                        }
                        if (i + 1 < _pageLines.Length)
                            _sb.Append('\n');
                    }
                    _rendererText.Text = _sb.ToString();//whole reformatted book
                    _sb.Clear();
                }
                UpdatePageCoords();
            }
        }
    }
}
