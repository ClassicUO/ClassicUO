// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
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
        private StbPageTextBox _bookPage;

        private GumpPic _forwardGumpPic, _backwardGumpPic;
        private StbTextBox _titleTextBox, _authorTextBox;

        public ModernBookGump
        (
            World world,
            uint serial,
            ushort page_count,
            string title,
            string author,
            bool is_editable,
            bool old_packet
        ) : base(world, serial, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;

            BookPageCount = page_count;
            IsEditable = is_editable;
            UseNewHeader = !old_packet;

            BuildGump(title, author);
        }

        internal string[] BookLines => _bookPage._pageLines;
        internal bool[] _pagesChanged => _bookPage._pagesChanged;


        public ushort BookPageCount { get; internal set; }
        public HashSet<int> KnownPages { get; internal set; } = new HashSet<int>();
        public static bool IsNewBook => Client.Game.UO.Version > ClientVersion.CV_200;
        public bool UseNewHeader { get; set; } = true;
        public static byte DefaultFont => (byte) (IsNewBook ? 1 : 4);

        public bool IntroChanges => _pagesChanged[0];
        internal int MaxPage => (BookPageCount >> 1) + 1;

        internal void ServerSetBookText()
        {
            if (BookLines == null || BookLines.Length <= 0)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            int sw = _bookPage.renderedText.GetCharWidth(' ');

            for (int i = 0, l = BookLines.Length; i < l; i++)
            {
                if (BookLines[i] != null && BookLines[i].Contains("\n"))
                {
                    BookLines[i] = BookLines[i].Replace("\n", "");
                }
            }

            for (int i = 0, l = BookLines.Length; i < l; i++)
            {
                int w = IsNewBook ? Client.Game.UO.FileManager.Fonts.GetWidthUnicode(_bookPage.renderedText.Font, BookLines[i]) : Client.Game.UO.FileManager.Fonts.GetWidthASCII(_bookPage.renderedText.Font, BookLines[i]);

                sb.Append(BookLines[i]);

                if (BookLines[i] == null)
                    continue;

                if (i + 1 < l && (string.IsNullOrWhiteSpace(BookLines[i]) && !BookLines[i].Contains("\n") || w + sw < _bookPage.renderedText.MaxWidth))
                {
                    sb.Append('\n');
                    BookLines[i] += '\n';
                }
            }

            _bookPage._ServerUpdate = true;
            _bookPage.SetText(sb.ToString());
            _bookPage.CaretIndex = 0;
            _bookPage.UpdatePageCoords();
            _bookPage._ServerUpdate = false;
        }


        private void BuildGump(string title, string author)
        {
            CanCloseWithRightClick = true;

            Add
            (
                new GumpPic(0, 0, 0x1FE, 0)
                {
                    CanMove = true
                }
            );

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

            _bookPage = new StbPageTextBox
            (
                DefaultFont,
                BookPageCount,
                this,
                MAX_BOOK_CHARS_PER_LINE * MAX_BOOK_LINES * BookPageCount,
                156,
                IsNewBook,
                FontStyle.ExtraHeight,
                2
            )
            {
                X = 0,
                Y = 0,
                Height = PAGE_HEIGHT * BookPageCount,
                Width = 156,
                IsEditable = IsEditable,
                Multiline = true
            };

            Add
            (
                _titleTextBox = new StbTextBox(DefaultFont, 47, 150, IsNewBook)
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
            Add(new Label(ResGumps.By, true, 1) { X = 40, Y = 130 }, 1);

            Add
            (
                _authorTextBox = new StbTextBox(DefaultFont, 29, 150, IsNewBook)
                {
                    X = 40,
                    Y = 160,
                    Height = 25,
                    Width = 155,
                    IsEditable = IsEditable
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

            Client.Game.Audio.PlaySound(0x0055);
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
                Client.Game.Audio.PlaySound(0x0055);
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
                            string[] text = new string[MAX_BOOK_LINES];

                            for (int x = (i - 1) * MAX_BOOK_LINES, l = 0; x < (i - 1) * MAX_BOOK_LINES + 8; x++, l++)
                            {
                                text[l] = BookLines[x];
                            }

                            NetClient.Socket.Send_BookPageData(LocalSerial, text, i);
                        }
                    }
                }
            }

            ActivePage = page;
            UpdatePageButtonVisibility();

            if (UIManager.KeyboardFocusControl == null || UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl && UIManager.KeyboardFocusControl != _bookPage && page != _bookPage._focusPage / 2 + 1)
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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (batcher.ClipBegin(x, y, Width, Height))
            {
                RenderedText t = _bookPage.renderedText;
                int startpage = (ActivePage - 1) * 2;

                if (startpage < BookPageCount)
                {
                    int poy = _bookPage._pageCoords[startpage, 0], phy = _bookPage._pageCoords[startpage, 1];

                    _bookPage.DrawSelection
                    (
                        batcher,
                        x + RIGHT_X,
                        y + UPPER_MARGIN,
                        poy,
                        poy + phy
                    );

                    t.Draw
                    (
                        batcher,
                        x + RIGHT_X,
                        y + UPPER_MARGIN,
                        0,
                        poy,
                        t.Width,
                        phy
                    );

                    if (startpage == _bookPage._caretPage)
                    {
                        if (_bookPage._caretPos.Y < poy + phy)
                        {
                            if (_bookPage._caretPos.Y >= poy)
                            {
                                if (_bookPage.HasKeyboardFocus)
                                {
                                    _bookPage.renderedCaret.Draw
                                    (
                                        batcher,
                                        _bookPage._caretPos.X + x + RIGHT_X,
                                        _bookPage._caretPos.Y + y + UPPER_MARGIN - poy,
                                        0,
                                        0,
                                        _bookPage.renderedCaret.Width,
                                        _bookPage.renderedCaret.Height
                                    );
                                }
                            }
                            else
                            {
                                _bookPage._caretPage = _bookPage.GetCaretPage();
                            }
                        }
                        else if (_bookPage._caretPos.Y <= _bookPage.Height)
                        {
                            if (_bookPage._caretPage + 2 < _bookPage._pagesChanged.Length)
                            {
                                _bookPage._focusPage = _bookPage._caretPage++;
                                SetActivePage(_bookPage._caretPage / 2 + 2);
                            }
                        }
                    }
                }

                startpage--;

                if (startpage > 0)
                {
                    int poy = _bookPage._pageCoords[startpage, 0], phy = _bookPage._pageCoords[startpage, 1];

                    _bookPage.DrawSelection
                    (
                        batcher,
                        x + LEFT_X,
                        y + UPPER_MARGIN,
                        poy,
                        poy + phy
                    );

                    t.Draw
                    (
                        batcher,
                        x + LEFT_X,
                        y + UPPER_MARGIN,
                        0,
                        poy,
                        t.Width,
                        phy
                    );

                    if (startpage == _bookPage._caretPage)
                    {
                        if (_bookPage._caretPos.Y < poy + phy)
                        {
                            if (_bookPage._caretPos.Y >= poy)
                            {
                                if (_bookPage.HasKeyboardFocus)
                                {
                                    _bookPage.renderedCaret.Draw
                                    (
                                        batcher,
                                        _bookPage._caretPos.X + x + LEFT_X,
                                        _bookPage._caretPos.Y + y + UPPER_MARGIN - poy,
                                        0,
                                        0,
                                        _bookPage.renderedCaret.Width,
                                        _bookPage.renderedCaret.Height
                                    );
                                }
                            }
                            else if (_bookPage._caretPage > 0)
                            {
                                _bookPage._focusPage = _bookPage._caretPage--;
                                SetActivePage(_bookPage._caretPage / 2 + 1);
                            }
                        }
                        else if (_bookPage._caretPos.Y <= _bookPage.Height)
                        {
                            if (_bookPage._caretPage + 2 < _bookPage._pagesChanged.Length)
                            {
                                _bookPage._caretPage++;
                            }
                        }
                    }
                }

                batcher.ClipEnd();
            }

            return true;
        }

        public override void Dispose()
        {
            base.Dispose();
            _bookPage?.Dispose();
        }

        public override void OnHitTestSuccess(int x, int y, ref Control res)
        {
            if (!IsDisposed)
            {
                int page = -1;

                if (ActivePage > 1 && x >= LEFT_X + X && x <= LEFT_X + X + _bookPage.Width)
                {
                    page = (ActivePage - 1) * 2 - 1;
                }
                else if (ActivePage - 1 < BookPageCount >> 1 && x >= RIGHT_X + X && x <= RIGHT_X + _bookPage.Width + X)
                {
                    page = (ActivePage - 1) * 2;
                }

                if (page >= 0 && page < BookPageCount && y >= UPPER_MARGIN + Y && y <= UPPER_MARGIN + PAGE_HEIGHT + Y)
                {
                    _bookPage._focusPage = page;
                    res = _bookPage;
                }
            }
        }

        private class StbPageTextBox : StbTextBox
        {
            private static readonly StringBuilder _sb = new StringBuilder();
            private static string[] _handler;
            private readonly ModernBookGump _gump;

            public StbPageTextBox
            (
                byte font,
                int bookpages,
                ModernBookGump gump,
                int max_char_count = -1,
                int maxWidth = 0,
                bool isunicode = true,
                FontStyle style = FontStyle.None,
                ushort hue = 0
            ) : base
            (
                font,
                max_char_count,
                maxWidth,
                isunicode,
                style,
                hue
            )
            {
                _pageCoords = new int[bookpages, 2];
                _pageLines = new string[bookpages * MAX_BOOK_LINES];
                _pagesChanged = new bool[bookpages + 1];
                Priority = ClickPriority.High;
                _gump = gump;
            }

            internal Point _caretPos => _caretScreenPosition;

            internal RenderedText renderedText => _rendererText;
            internal RenderedText renderedCaret => _rendererCaret;
            internal int _caretPage, _focusPage;
            internal readonly int[,] _pageCoords;
            internal readonly string[] _pageLines;
            internal readonly bool[] _pagesChanged;

            internal bool _ServerUpdate;

            internal int GetCaretPage()
            {
                Point p = _rendererText.GetCaretPosition(CaretIndex);

                for (int i = 0, l = _pageCoords.GetLength(0); i < l; i++)
                {
                    if (p.Y >= _pageCoords[i, 0] && p.Y < _pageCoords[i, 0] + _pageCoords[i, 1])
                    {
                        return i;
                    }
                }

                return 0;
            }

            protected override void OnMouseDown(int x, int y, MouseButtonType button)
            {
                if (button == MouseButtonType.Left)
                {
                    if (IsEditable)
                    {
                        SetKeyboardFocus();
                    }

                    if (!NoSelection)
                    {
                        _leftWasDown = true;
                    }

                    if (_focusPage >= 0 && _focusPage < _pageCoords.GetLength(0))
                    {
                        if (_focusPage % 2 == 0)
                        {
                            x -= RIGHT_X + _gump.X;
                        }
                        else
                        {
                            x -= LEFT_X + _gump.X;
                        }

                        y += _pageCoords[_focusPage, 0] - (UPPER_MARGIN + _gump.Y);
                    }

                    Stb.Click(x, y);
                    UpdateCaretScreenPosition();
                    _caretPage = GetCaretPage();
                }
            }

            protected override void OnMouseOver(int x, int y)
            {
                if (_leftWasDown)
                {
                    if (_focusPage >= 0 && _focusPage < _pageCoords.GetLength(0))
                    {
                        if (_focusPage % 2 == 0)
                        {
                            x -= RIGHT_X + _gump.X;
                        }
                        else
                        {
                            x -= LEFT_X + _gump.X;
                        }

                        y += _pageCoords[_focusPage, 0] - (UPPER_MARGIN + _gump.Y);
                    }

                    Stb.Drag(x, y);
                }
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                if (_focusPage >= 0 && _focusPage < _pageCoords.GetLength(0))
                {
                    if (_focusPage % 2 == 0)
                    {
                        x -= RIGHT_X + _gump.X;
                    }
                    else
                    {
                        x -= LEFT_X + _gump.X;
                    }

                    y += _pageCoords[_focusPage, 0] - (UPPER_MARGIN + _gump.Y);
                }

                base.OnMouseUp(x, y, button);
            }

            internal void UpdatePageCoords()
            {
                MultilinesFontInfo info = _rendererText.GetInfo();

                for (int page = 0, y = 0; page < _pageCoords.GetLength(0); page++)
                {
                    _pageCoords[page, 0] = y;
                    _pageCoords[page, 1] = 0;

                    for (int i = 0; i < MAX_BOOK_LINES; i++)
                    {
                        if (info == null)
                        {
                            break;
                        }

                        _pageCoords[page, 1] += info.MaxHeight;
                        info = info.Next;
                    }

                    y += _pageCoords[page, 1];
                }
            }

            internal void DrawSelection(UltimaBatcher2D batcher, int x, int y, int starty, int endy)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0, false, 0.5f);

                int selectStart = Math.Min(Stb.SelectStart, Stb.SelectEnd);
                int selectEnd = Math.Max(Stb.SelectStart, Stb.SelectEnd);

                if (selectStart < selectEnd)
                {
                    MultilinesFontInfo info = _rendererText.GetInfo();

                    int drawY = 1;
                    int start = 0;

                    while (info != null && selectStart < selectEnd)
                    {
                        // ok we are inside the selection
                        if (selectStart >= start && selectStart < start + info.CharCount)
                        {
                            int startSelectionIndex = selectStart - start;

                            // calculate offset x
                            int drawX = 0;

                            for (int i = 0; i < startSelectionIndex; i++)
                            {
                                drawX += _rendererText.GetCharWidth(info.Data[i].Item);
                            }

                            // selection is gone. Bye bye
                            if (selectEnd >= start && selectEnd < start + info.CharCount)
                            {
                                int count = selectEnd - selectStart;

                                int endX = 0;

                                // calculate width
                                for (int k = 0; k < count; k++)
                                {
                                    endX += _rendererText.GetCharWidth(info.Data[startSelectionIndex + k].Item);
                                }

                                if (drawY >= starty && drawY <= endy)
                                {
                                    batcher.Draw
                                    (
                                        SolidColorTextureCache.GetTexture(SELECTION_COLOR),
                                        new Rectangle
                                        (
                                            x + drawX,
                                            y + drawY - starty,
                                            endX,
                                            info.MaxHeight + 1
                                        ),
                                        hueVector
                                    );
                                }

                                break;
                            }


                            // do the whole line
                            if (drawY >= starty && drawY <= endy)
                            {
                                batcher.Draw
                                (
                                    SolidColorTextureCache.GetTexture(SELECTION_COLOR),
                                    new Rectangle
                                    (
                                        x + drawX,
                                        y + drawY - starty,
                                        info.Width - drawX,
                                        info.MaxHeight + 1
                                    ),
                                    hueVector
                                );
                            }

                            // first selection is gone. M
                            selectStart = start + info.CharCount;
                        }

                        start += info.CharCount;
                        drawY += info.MaxHeight;
                        info = info.Next;
                    }
                }
            }

            protected override void OnTextChanged()
            {
                _is_writing = true;

                if (!_ServerUpdate)
                {
                    if (_handler == null || _handler.Length < _pageLines.Length)
                    {
                        _handler = new string[_pageLines.Length];
                    }

                    string[] split = Text.Split('\n');

                    for (int i = 0, l = 0; i < split.Length && l < _pageLines.Length; i++)
                    {
                        if (split[i].Length > 0)
                        {
                            for (int p = 0, w = 0, pw = _rendererText.GetCharWidth(split[i][p]);; pw = _rendererText.GetCharWidth(split[i][p]))
                            {
                                if (w + pw > _rendererText.MaxWidth)
                                {
                                    _handler[l] = _sb.ToString();
                                    _sb.Clear();
                                    l++;
                                    //CaretIndex++;
                                    w = 0;

                                    if (l >= _pageLines.Length)
                                    {
                                        break;
                                    }
                                }

                                w += pw;
                                _sb.Append(split[i][p]);
                                p++;

                                if (p >= split[i].Length)
                                {
                                    _sb.Append('\n');
                                    _handler[l] = _sb.ToString();
                                    _sb.Clear();
                                    l++;

                                    break;
                                }
                            }
                        }
                        else
                        {
                            _handler[l] = "\n";
                            l++;
                            //_sb.Append('\n');
                        }
                    }

                    _sb.Clear();

                    for (int i = 0; i < _pageLines.Length; i++)
                    {
                        if (!_pagesChanged[(i >> 3) + 1] && _handler[i] != _pageLines[i])
                        {
                            _pagesChanged[(i >> 3) + 1] = true;
                        }

                        _sb.Append(_pageLines[i] = _handler[i]);
                    }

                    _rendererText.Text = _sb.ToString(); //whole reformatted book
                    _sb.Clear();
                    UpdatePageCoords();
                }

                base.OnTextChanged();
                _is_writing = false;
            }

            protected override void CloseWithRightClick()
            {
                if (_gump != null && !_gump.IsDisposed)
                {
                    _gump.CloseWithRightClick();
                }
                else
                {
                    base.CloseWithRightClick();
                }
            }
        }
    }
}