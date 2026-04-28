// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL3;
using System;
using System.Collections.Generic;
using System.Text;

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

        public void SetTitle(string title, bool editable)
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

        public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
        {
            base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
            float layerDepth = layerDepthRef;

            // Render path is intentionally PURE — no state mutation, no SetActivePage,
            // no _caretPage/_focusPage writes. Caret/page realignment runs from input
            // handlers via _bookPage.RealignCaretAndActivePage(), so a single mismatch
            // between _caretScreenPosition and _pageCoords cannot spin SetActivePage
            // every frame.
            renderLists.PushClip(new Rectangle(x, y, Width, Height));

            EmitBookPage(renderLists, x, y, layerDepth, (ActivePage - 1) * 2, RIGHT_X);
            EmitBookPage(renderLists, x, y, layerDepth, (ActivePage - 1) * 2 - 1, LEFT_X);

            renderLists.PopClip();

            return true;
        }

        private void EmitBookPage(RenderLists renderLists, int x, int y, float layerDepth, int pageIndex, int pageX)
        {
            if (pageIndex < 0 || pageIndex >= BookPageCount)
            {
                return;
            }

            int poy = _bookPage._pageCoords[pageIndex, 0];
            int phy = _bookPage._pageCoords[pageIndex, 1];
            RenderedText t = _bookPage.renderedText;

            _bookPage.EmitSelection(renderLists, x + pageX, y + UPPER_MARGIN, poy, poy + phy, layerDepth);

            renderLists.AddGumpNoAtlasScrolled(
                t,
                x + pageX,
                y + UPPER_MARGIN,
                new Rectangle(0, poy, t.Width, phy),
                layerDepth
            );

            if (pageIndex == _bookPage._caretPage
                && _bookPage.HasKeyboardFocus
                && _bookPage._caretPos.Y >= poy
                && _bookPage._caretPos.Y < poy + phy)
            {
                renderLists.AddGumpNoAtlasScrolled(
                    _bookPage.renderedCaret,
                    _bookPage._caretPos.X + x + pageX,
                    _bookPage._caretPos.Y + y + UPPER_MARGIN - poy,
                    new Rectangle(0, 0, _bookPage.renderedCaret.Width, _bookPage.renderedCaret.Height),
                    layerDepth
                );
            }
        }

        // Re-snap ActivePage to whichever page the caret is actually on. Called from
        // _bookPage's input handlers (mouse click, key, text change) — never from the
        // render path. If the caret didn't move into a different page, this is a no-op.
        internal void RealignCaretAndActivePage()
        {
            if (_bookPage == null || IsDisposed)
            {
                return;
            }

            int newCaretPage = _bookPage.GetCaretPage();

            if (newCaretPage != _bookPage._caretPage)
            {
                _bookPage._focusPage = _bookPage._caretPage;
                _bookPage._caretPage = newCaretPage;
            }

            // Page-to-ActivePage mapping: page 0 is the right page of AP 1, then
            // each subsequent ActivePage shows {left=odd, right=even} pairs.
            //   page 0           → ActivePage 1
            //   pages 1,2        → ActivePage 2
            //   pages 3,4        → ActivePage 3
            //   pages 2k-1, 2k   → ActivePage k+1
            // So target = (page + 1) / 2 + 1 with integer division.
            int targetActivePage = (newCaretPage + 1) / 2 + 1;
            int maxActivePage = MaxPage;

            if (targetActivePage != ActivePage
                && targetActivePage >= 1
                && targetActivePage <= maxActivePage)
            {
                SetActivePage(targetActivePage);
            }
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

            // _bookPage is intentionally NOT in the gump tree (the parent renders
            // it manually so it can clip against the book artwork). The default
            // NotifyRenderDirty walks _parent → null and never reaches the
            // ModernBookGump, so caret moves and selection drags never invalidate
            // the gump cache. Redirect explicitly to the owning gump.
            protected internal override void NotifyRenderDirty()
            {
                _gump?.InvalidateRenderCache();
            }

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
                    _gump.RealignCaretAndActivePage();
                }
            }

            protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
            {
                base.OnKeyDown(key, mod);
                _gump.RealignCaretAndActivePage();
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

                    int prevSelStart = Stb.SelectStart;
                    int prevSelEnd = Stb.SelectEnd;
                    Stb.Drag(x, y);

                    if (Stb.SelectStart != prevSelStart || Stb.SelectEnd != prevSelEnd)
                    {
                        NotifyRenderDirty();
                    }
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

            internal void EmitSelection(RenderLists renderLists, int x, int y, int starty, int endy, float layerDepth)
            {
                Texture2D selectionTex = SolidColorTextureCache.GetTexture(SELECTION_COLOR);
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
                                    renderLists.AddGumpSprite(
                                        selectionTex,
                                        new Rectangle(
                                            x + drawX,
                                            y + drawY - starty,
                                            endX,
                                            info.MaxHeight + 1
                                        ),
                                        hueVector,
                                        layerDepth
                                    );
                                }

                                break;
                            }


                            // do the whole line
                            if (drawY >= starty && drawY <= endy)
                            {
                                renderLists.AddGumpSprite(
                                    selectionTex,
                                    new Rectangle(
                                        x + drawX,
                                        y + drawY - starty,
                                        info.Width - drawX,
                                        info.MaxHeight + 1
                                    ),
                                    hueVector,
                                    layerDepth
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

            protected override void OnTextChanged(string previousText)
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

                base.OnTextChanged(previousText);
                _is_writing = false;

                if (!_ServerUpdate)
                {
                    _gump.RealignCaretAndActivePage();
                }
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
