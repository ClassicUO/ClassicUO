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
using System.Linq;
using System.Text;
using ClassicUO.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using SDL2;

namespace ClassicUO.Game.UI.Gumps
{
    internal class BookGump : Gump
    {
        private const int MaxBookLines = 8;
        private const int MaxBookChars = 53;

        private readonly List<MultiLineBox> m_Pages = new List<MultiLineBox>();
        private byte _activated;

        // < 0 == backward
        // > 0 == forward
        // 0 == invariant
        // this is our only place to check for page movement from key
        // or displacement of caretindex from mouse
        private sbyte _AtEnd;

        private bool _scale;
        public MultiLineBox BookTitle, BookAuthor;

        private GumpPic m_Forward, m_Backward;
        public bool[] PageChanged;

        public BookGump(uint serial) : base(serial, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
        }

        public ushort BookPageCount { get; internal set; }
        public static bool IsNewBookD4 => Client.Version > ClientVersion.CV_200;
        public static byte DefaultFont => (byte) (IsNewBookD4 ? 1 : 4);

        public string[] BookPages
        {
            get => null;
            set
            {
                if (value != null)
                {
                    if (_activated > 0)
                    {
                        for (int i = 0; i < m_Pages.Count; i++)
                        {
                            m_Pages[i].IsEditable = IsEditable;
                            m_Pages[i].Text = value[i];
                        }

                        SetActivePage(ActivePage);
                    }
                    else
                    {
                        BuildGump(value);
                        SetActivePage(1);
                    }
                }
            }
        }

        public bool IntroChanges => PageChanged[0];
        private int MaxPage => (BookPageCount >> 1) + 1;
        private int ActiveInternalPage => IsEditable ? m_Pages.FindIndex(t => t.HasKeyboardFocus) : m_Pages.FindIndex(t => t.MouseIsOver);

        private void BuildGump(string[] pages)
        {
            CanCloseWithRightClick = true;
            Add(new GumpPic(0, 0, 0x1FE, 0)
            {
                CanMove = true
            });

            Add(m_Backward = new GumpPic(0, 0, 0x1FF, 0));

            Add(m_Forward = new GumpPic(356, 0, 0x200, 0));

            m_Forward.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl) SetActivePage(ActivePage + 1);
            };

            m_Forward.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl) SetActivePage(MaxPage);
            };

            m_Backward.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl) SetActivePage(ActivePage - 1);
            };

            m_Backward.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left && sender is Control ctrl) SetActivePage(1);
            };

            PageChanged = new bool[BookPageCount + 1];
            Add(BookTitle, 1);
            Add(new Label("by", true, 1) {X = BookAuthor.X, Y = BookAuthor.Y - 30}, 1);
            Add(BookAuthor, 1);

            for (int k = 1; k <= BookPageCount; k++)
            {
                int x = 38;
                int y = 30;

                if (k % 2 == 1)
                {
                    x = 223;
                    //right hand page
                }

                int page = k + 1;

                if (page % 2 == 1)
                    page += 1;
                page >>= 1;

                MultiLineBox tbox = new MultiLineBox(new MultiLineEntry(DefaultFont, MaxBookChars * MaxBookLines, 0, 155, IsNewBookD4, FontStyle.ExtraHeight, 2), IsEditable)
                {
                    X = x,
                    Y = y,
                    Height = 170,
                    Width = 155,
                    IsEditable = IsEditable,
                    Text = pages[k - 1],
                    MaxLines = 8
                };
                Add(tbox, page);
                m_Pages.Add(tbox);

                tbox.MouseUp += (sender, e) =>
                {
                    if (e.Button == MouseButtonType.Left && sender is Control ctrl) OnLeftClick();
                };

                tbox.MouseDoubleClick += (sender, e) =>
                {
                    if (e.Button == MouseButtonType.Left && sender is Control ctrl) OnLeftClick();
                };
                Add(new Label(k.ToString(), true, 1) {X = x + 80, Y = 200}, page);
            }

            _activated = 1;

            Client.Game.Scene.Audio.PlaySound(0x0055);
        }

        private void SetActivePage(int page)
        {
            if (page <= 1)
            {
                m_Backward.IsVisible = false;
                m_Forward.IsVisible = true;
                page = 1;
            }
            else if (page >= MaxPage)
            {
                m_Forward.IsVisible = false;
                m_Backward.IsVisible = true;
                page = MaxPage;
            }
            else
            {
                m_Backward.IsVisible = true;
                m_Forward.IsVisible = true;
            }

            Client.Game.Scene.Audio.PlaySound(0x0055);

            ActivePage = page;

            if (UIManager.KeyboardFocusControl == null || (UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl && UIManager.KeyboardFocusControl.Page != page))
            {
                UIManager.SystemChat.TextBoxControl.SetKeyboardFocus();
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Backwards:

                    return;

                case Buttons.Forward:

                    return;
            }

            base.OnButtonClick(buttonID);
        }

        protected override void CloseWithRightClick()
        {
            if (PageChanged[0])
            {
                if (IsNewBookD4)
                    NetClient.Socket.Send(new PBookHeader(this));
                else
                    NetClient.Socket.Send(new PBookHeaderOld(this));
                PageChanged[0] = false;
            }

            if (PageChanged.Any(t => t)) NetClient.Socket.Send(new PBookData(this));
            base.CloseWithRightClick();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_activated > 1)
            {
                if (!IsDisposed)
                {
                    if (BookAuthor.IsChanged || BookTitle.IsChanged)
                        PageChanged[0] = true;

                    for (int i = m_Pages.Count - 1; i >= 0; --i)
                    {
                        if (m_Pages[i].IsChanged)
                            PageChanged[i + 1] = true;
                    }
                }
            }
            else if (_activated > 0)
                _activated++;

            base.Update(totalMS, frameMS);
        }

        protected override void OnKeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            int curpage = ActiveInternalPage;
            var box = curpage >= 0 ? m_Pages[curpage] : null;
            var entry = box?.TxEntry;

            if (key == SDL.SDL_Keycode.SDLK_BACKSPACE || key == SDL.SDL_Keycode.SDLK_DELETE)
            {
                if (curpage >= 0)
                {
                    if (curpage > 0)
                    {
                        if (key == SDL.SDL_Keycode.SDLK_BACKSPACE)
                        {
                            if (_AtEnd < 0)
                            {
                                if ((curpage + 1) % 2 == 0)
                                    SetActivePage(ActivePage - 1);
                                curpage--;
                                box = m_Pages[curpage];
                                entry = box.TxEntry;
                                RefreshShowCaretPos(entry.Text.Length, box);
                                _AtEnd = 1;
                            }
                            else if (entry != null && entry.CaretIndex == 0) _AtEnd = -1;
                        }
                        else
                            _AtEnd = (sbyte) (entry != null && entry.CaretIndex == 0 ? -1 : entry.CaretIndex + 1 >= entry.Text.Length && curpage < BookPageCount ? 1 : 0);
                    }
                    else
                        _AtEnd = 0;

                    if (!_scale)
                    {
                        _AtEnd = 0;

                        return;
                    }

                    _scale = false;

                    if (entry != null)
                    {
                        int caretpos = entry.CaretIndex, active = curpage;
                        curpage++;

                        if (curpage < BookPageCount) //if we are on the last page it doesn't need the front text backscaling
                        {
                            StringBuilder sb = new StringBuilder();

                            do
                            {
                                entry = m_Pages[curpage].TxEntry;
                                box = m_Pages[curpage];
                                int curlen = entry.Text.Length, prevlen = m_Pages[curpage - 1].Text.Length, chonline = box.GetCharsOnLine(0), prevpage = curpage - 1;
                                m_Pages[prevpage].TxEntry.SetCaretPosition(prevlen);

                                for (int i = MaxBookLines - m_Pages[prevpage].LinesCount; i > 0 && prevlen > 0; --i) sb.Append('\n');

                                sb.Append(entry.Text.Substring(0, chonline));

                                if (curlen > 0)
                                {
                                    sb.Append('\n');

                                    entry.Text = entry.Text.Substring(chonline);
                                }

                                m_Pages[prevpage].TxEntry.InsertString(sb.ToString());
                                curpage++;
                                sb.Clear();
                            } while (curpage < BookPageCount);

                            m_Pages[active].TxEntry.SetCaretPosition(caretpos);
                        }
                    }
                }
            }
            else if (key == SDL.SDL_Keycode.SDLK_RIGHT)
            {
                if (curpage >= 0 && curpage + 1 < BookPageCount)
                {
                    if (entry != null && entry.CaretIndex + 1 >= box.Text.Length)
                    {
                        if (_AtEnd > 0)
                        {
                            if ((curpage + 1) % 2 == 1)
                                SetActivePage(ActivePage + 1);
                            RefreshShowCaretPos(0, m_Pages[curpage + 1]);
                            _AtEnd = -1;
                        }
                        else
                            _AtEnd = 1;

                        return;
                    }
                }

                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_LEFT)
            {
                if (curpage > 0)
                {
                    if (entry != null && entry.CaretIndex == 0)
                    {
                        if (_AtEnd < 0)
                        {
                            if ((curpage + 1) % 2 == 0)
                                SetActivePage(ActivePage - 1);
                            RefreshShowCaretPos(m_Pages[curpage - 1].Text.Length, m_Pages[curpage - 1]);
                            _AtEnd = 1;
                        }
                        else
                            _AtEnd = -1;

                        return;
                    }
                }

                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_UP)
            {
                if (curpage > 0)
                {
                    if (entry != null && entry.CaretIndex == 0)
                    {
                        if (_AtEnd < 0)
                        {
                            if ((curpage + 1) % 2 == 0)
                                SetActivePage(ActivePage - 1);
                            RefreshShowCaretPos(m_Pages[curpage - 1].Text.Length, m_Pages[curpage - 1]);
                            _AtEnd = 1;
                        }
                        else
                            _AtEnd = -1;

                        return;
                    }
                }

                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_DOWN)
            {
                if (curpage + 1 < BookPageCount && curpage >= 0)
                {
                    if (entry != null && entry.CaretIndex + 1 >= box.Text.Length)
                    {
                        if (_AtEnd > 0)
                        {
                            if ((curpage + 1) % 2 == 1)
                                SetActivePage(ActivePage + 1);
                            RefreshShowCaretPos(0, m_Pages[curpage + 1]);
                            _AtEnd = -1;
                        }
                        else
                            _AtEnd = 1;

                        return;
                    }
                }

                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_HOME)
            {
                if (curpage > 0)
                {
                    if (_AtEnd < 0)
                    {
                        if ((curpage + 1) % 2 == 0)
                            SetActivePage(ActivePage - 1);
                        RefreshShowCaretPos(m_Pages[curpage - 1].Text.Length, m_Pages[curpage - 1]);
                        _AtEnd = 1;

                        return;
                    }
                }

                _AtEnd = 0;
            }
            else if (key == SDL.SDL_Keycode.SDLK_END)
            {
                if (curpage >= 0 && curpage + 1 < BookPageCount)
                {
                    if (_AtEnd > 0)
                    {
                        if ((curpage + 1) % 2 == 1)
                            SetActivePage(ActivePage + 1);
                        RefreshShowCaretPos(0, m_Pages[curpage + 1]);
                        _AtEnd = -1;

                        return;
                    }
                }

                _AtEnd = 0;
            }
            else
                _AtEnd = 0;
        }

        public void Scale(MultiLineEntry entry, bool fromleft)
        {
            var linech = entry.GetLinesCharsCount(entry.Text);
            int caretpos = entry.CaretIndex;
            (int, int) selection = entry.GetSelectionArea();
            bool multilinesel = selection.Item1 != -1;

            if (!multilinesel)
            {
                for (int l = 0; l < linech.Length && !_scale; l++)
                {
                    caretpos -= linech[l];
                    _scale = fromleft ? caretpos == -linech[l] : caretpos == 0;
                }

                if (fromleft)
                    _scale = _scale && (ActiveInternalPage > 0 || entry.CaretIndex > 0);
            }

            entry.RemoveChar(fromleft);
        }

        public void OnHomeOrEnd(MultiLineEntry entry, bool home)
        {
            var linech = entry.GetLinesCharsCount();
            // sepos = 1 if at end of whole text, -1 if at begin, else it will always be 0
            sbyte sepos = (sbyte) (entry.CaretIndex == 0 ? -1 : entry.CaretIndex == entry.Text.Length ? 1 : 0);
            int caretpos = entry.CaretIndex;

            for (int l = 0; l < linech.Length; l++)
            {
                caretpos -= linech[l];

                if (!home)
                {
                    int txtlen = entry.Text.Length;

                    if (caretpos == -1 && sepos <= 0 || sepos > 0 || txtlen == 0)
                    {
                        if (entry.CaretIndex == txtlen && ActiveInternalPage + 1 < BookPageCount)
                            _AtEnd = 1;
                        entry.SetCaretPosition(txtlen);

                        break;
                    }

                    if (caretpos < 0 || sepos < 0)
                    {
                        entry.SetCaretPosition(entry.CaretIndex - caretpos - (l + 1 != linech.Length ? 1 : 0));

                        break;
                    }
                }
                else
                {
                    if (caretpos == 0 && (sepos == 0 || l + 2 == linech.Length && linech[l + 1] == 0) || sepos < 0)
                    {
                        if (entry.CaretIndex == 0 && ActiveInternalPage > 0)
                            _AtEnd = -1;
                        entry.SetCaretPosition(0);

                        break;
                    }

                    if (caretpos < 0 || sepos > 0 && caretpos == 0)
                    {
                        entry.SetCaretPosition(entry.CaretIndex - (linech[l] + caretpos));

                        break;
                    }
                }
            }
        }

        private void OnLeftClick()
        {
            var curpage = ActiveInternalPage;

            if (curpage >= 0 && curpage < m_Pages.Count)
            {
                var entry = m_Pages[curpage].TxEntry;
                var caretpos = m_Pages[curpage].TxEntry.CaretIndex;
                _AtEnd = (sbyte) (caretpos == 0 && curpage > 0 ? -1 : caretpos + 1 >= entry.Text.Length && curpage >= 0 && curpage < BookPageCount ? 1 : 0);
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if ((MultiLineBox.PasteRetnCmdID & textID) != 0 && !string.IsNullOrEmpty(text))
            {
                text = text.Replace("\r", string.Empty);
                int curpage = ActiveInternalPage;
                MultiLineBox page;

                if (curpage < 0)
                {
                    if (BookTitle.HasKeyboardFocus)
                        page = BookTitle;
                    else if (BookAuthor.HasKeyboardFocus)
                        page = BookAuthor;
                    else
                        return;
                }
                else
                    page = m_Pages[curpage];

                int oldcaretpos = page.TxEntry.CaretIndex, oldpage = curpage;
                string original = textID == MultiLineBox.PasteCommandID ? text : page.Text;
                text = page.TxEntry.InsertString(text);

                if (curpage >= 0)
                {
                    curpage++;

                    if (curpage % 2 == 1)
                        SetActivePage(ActivePage + 1);

                    while (text != null && curpage < BookPageCount)
                    {
                        var entry = m_Pages[curpage].TxEntry;
                        RefreshShowCaretPos(0, m_Pages[curpage]);
                        /*if(text.Length==0 || text[text.Length - 1] != '\n')
                            text = entry.InsertString(text + "\n");
                        else*/
                        text = entry.InsertString(text);

                        if (!string.IsNullOrEmpty(text))
                        {
                            curpage++;

                            if (curpage < BookPageCount)
                            {
                                if (curpage % 2 == 1)
                                    SetActivePage(ActivePage + 1);
                            }
                            else
                            {
                                --curpage;
                                text = null;
                            }
                        }
                    }

                    if (MultiLineBox.RetrnCommandID == textID)
                    {
                        if (oldcaretpos >= m_Pages[oldpage].Text.Length && original == m_Pages[oldpage].Text && oldpage + 1 < BookPageCount)
                        {
                            oldcaretpos = 0;
                            oldpage++;
                        }
                        else
                            oldcaretpos++;

                        RefreshShowCaretPos(oldcaretpos, m_Pages[oldpage]);
                    }
                    else
                    {
                        int[] linechr = m_Pages[oldpage].TxEntry.GetLinesCharsCount(original);

                        foreach (int t in linechr)
                        {
                            oldcaretpos += t;

                            if (oldcaretpos <= m_Pages[oldpage].Text.Length) continue;

                            oldcaretpos = t;

                            if (oldpage + 1 < BookPageCount)
                                oldpage++;
                            else
                                break;
                        }

                        RefreshShowCaretPos(oldcaretpos, m_Pages[oldpage]);
                    }

                    PageChanged[oldpage + 1] = true; //for the last page we are setting the changed status, this is the page we are on with caret.
                    SetActivePage((oldpage >> 1) + oldpage % 2 + 1);
                }
            }
        }

        private void RefreshShowCaretPos(int pos, MultiLineBox box)
        {
            box.SetKeyboardFocus();
            box.TxEntry.SetCaretPosition(pos);
            box.TxEntry.UpdateCaretPosition();
        }

        internal sealed class PBookHeader : PacketWriter
        {
            public PBookHeader(BookGump gump) : base(0xD4)
            {
                byte[] titleBuffer = Encoding.UTF8.GetBytes(gump.BookTitle.Text);
                byte[] authorBuffer = Encoding.UTF8.GetBytes(gump.BookAuthor.Text);
                EnsureSize(15 + titleBuffer.Length + authorBuffer.Length);
                WriteUInt(gump.LocalSerial);
                WriteByte(gump.BookPageCount > 0 ? (byte) 1 : (byte) 0);
                WriteByte(gump.BookPageCount > 0 ? (byte) 1 : (byte) 0);
                WriteUShort(gump.BookPageCount);

                WriteUShort((ushort) (titleBuffer.Length + 1));
                WriteBytes(titleBuffer, 0, titleBuffer.Length);
                WriteByte(0);
                WriteUShort((ushort) (authorBuffer.Length + 1));
                WriteBytes(authorBuffer, 0, authorBuffer.Length);
                WriteByte(0);
            }
        }

        internal sealed class PBookHeaderOld : PacketWriter
        {
            public PBookHeaderOld(BookGump gump) : base(0x93)
            {
                EnsureSize(15 + 60 + 30);
                WriteUInt(gump.LocalSerial);
                WriteByte(gump.BookPageCount > 0 ? (byte) 1 : (byte) 0);
                WriteByte(gump.BookPageCount > 0 ? (byte) 1 : (byte) 0);

                WriteUShort(gump.BookPageCount);

                WriteASCII(gump.BookTitle.Text, 60);
                WriteASCII(gump.BookAuthor.Text, 30);
            }
        }

        internal sealed class PBookData : PacketWriter
        {
            public PBookData(BookGump gump) : base(0x66)
            {
                EnsureSize(256);

                WriteUInt(gump.LocalSerial);
                List<int> changed = new List<int>();

                for (int i = 1; i < gump.PageChanged.Length; i++)
                {
                    if (gump.PageChanged[i])
                        changed.Add(i);
                }

                WriteUShort((ushort) changed.Count);

                for (int i = changed.Count - 1; i >= 0; --i)
                {
                    WriteUShort((ushort) changed[i]);
                    MultiLineEntry mle = gump.m_Pages[changed[i] - 1].TxEntry;
                    StringBuilder sb = new StringBuilder(mle.Text);
                    int rows = 0;

                    if (sb.Length > 0)
                    {
                        var lcc = mle.GetLinesCharsCount(mle.Text);
                        rows = Math.Min(MaxBookLines, lcc.Length);
                        int pos = lcc.Sum();

                        for (int l = rows - 1; l >= 0; --l)
                        {
                            if (pos > 0 && (lcc[l] == 0 || sb[pos - 1] != '\n')) sb.Insert(pos, '\n');
                            pos -= lcc[l];
                        }
                    }

                    var splits = sb.ToString().Split('\n');
                    int length = splits.Length;
                    WriteUShort((ushort) Math.Min(length, MaxBookLines));
                    if (length > MaxBookLines && changed[i] >= gump.BookPageCount) Log.Error( $"Book page {changed[i]} split into too many lines: {length - MaxBookLines} Additional lines will be lost");

                    for (int j = 0; j < length; j++)
                    {
                        // each line should BE < 53 chars long, even if 80 is admitted (the 'dot' is the least space demanding char, '.',
                        // a page full of dots is 52 chars exactly, but in multibyte things might change in byte size!)
                        if (j < MaxBookLines)
                        {
                            if (IsNewBookD4)
                            {
                                byte[] buf = Encoding.UTF8.GetBytes(splits[j]);

                                if (buf.Length > 79)
                                {
                                    Log.Error( $"Book page {changed[i]} single line too LONG, total lenght -> {buf.Length} vs MAX 79 bytes allowed, some content might get lost");
                                    splits[j] = splits[j].Substring(0, 79);
                                }

                                WriteBytes(buf, 0, buf.Length);
                                WriteByte(0);
                            }
                            else
                            {
                                if (splits[j].Length > 79)
                                {
                                    Log.Error( $"Book page {changed[i]} single line too LONG, total lenght -> {splits[j].Length} vs MAX 79 bytes allowed, some content might get lost");
                                    splits[j] = splits[j].Substring(0, 79);
                                }

                                WriteASCII(splits[j]);
                            }
                        }
                    }
                }
            }
        }

        private enum Buttons
        {
            Closing = 0,
            Forward = 1,
            Backwards = 2
        }
    }
}