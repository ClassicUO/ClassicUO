#region license

// Copyright (c) 2024, andreakarasho
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
using System.Text;
using ClassicUO.Utility;

namespace StbTextEditSharp
{
    public class TextEdit
    {
        public TextEdit(ITextEditHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            Handler = handler;

            UndoState = new UndoState();
            ClearState(false);
        }

        public int Length => Handler.Length;

        public string text
        {
            get => Handler.Text;

            set => Handler.Text = value;
        }

        public bool CursorAtEndOfLine;

        public int CursorIndex;
        public bool HasPreferredX;
        public bool InsertMode;
        public float PreferredX;
        public int SelectEnd;
        public int SelectStart;
        public bool SingleLine;
        public UndoState UndoState;
        public readonly ITextEditHandler Handler;

        public void SortSelection()
        {
            if (SelectEnd < SelectStart)
            {
                int temp = SelectEnd;
                SelectEnd = SelectStart;
                SelectStart = temp;
            }
        }

        public void MoveToFirst()
        {
            if (SelectStart != SelectEnd)
            {
                SortSelection();
                CursorIndex = SelectStart;
                SelectEnd = SelectStart;
                HasPreferredX = false;
            }
        }

        public void PrepareSelectionAtCursor()
        {
            if (!(SelectStart != SelectEnd))
            {
                SelectStart = SelectEnd = CursorIndex;
            }
            else
            {
                CursorIndex = SelectEnd;
            }
        }

        public void MakeUndoInsert(int where, int length)
        {
            UndoState.CreateUndo(where, 0, length);
        }

        public void ClearState(bool is_single_line)
        {
            UndoState.undo_point = 0;
            UndoState.undo_char_point = 0;
            UndoState.redo_point = 99;
            UndoState.redo_char_point = 999;
            SelectEnd = SelectStart = 0;
            CursorIndex = 0;
            HasPreferredX = false;
            PreferredX = 0;
            CursorAtEndOfLine = false;
            SingleLine = is_single_line;
            InsertMode = false;
        }

        public void DeleteChars(int pos, int l)
        {
            if (l == 0)
            {
                return;
            }

            text = text.Substring(0, pos) + text.Substring(pos + l);
        }

        public int InsertChars(int pos, int[] codepoints, int start, int length)
        {
            int count = start + length;

            ValueStringBuilder sb = new ValueStringBuilder(count);
            for (int i = start; i < count; ++i)
            {
                sb.Append(char.ConvertFromUtf32(codepoints[i]));
            }

            InsertChars(pos, sb.ToString());

            sb.Dispose();

            return length;
        }

        public int InsertChars(int pos, string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return 0;
            }

            if (text == null)
            {
                text = s;
            }
            else
            {
                text = text.Substring(0, pos) + s + text.Substring(pos);
            }

            return s.Length;
        }

        public int InsertChar(int pos, int codepoint)
        {
            string s = char.ConvertFromUtf32(codepoint);

            return InsertChars(pos, s);
        }

        public int LocateCoord(float x, float y)
        {
            TextEditRow r = new TextEditRow();
            int n = Length;
            float base_y = 0;
            int i = 0;
            int k = 0;
            r.x0 = r.x1 = 0;
            r.ymin = r.ymax = 0;
            r.num_chars = 0;

            while (i < n)
            {
                r = Handler.LayoutRow(i);

                if (r.num_chars <= 0)
                {
                    return n;
                }

                if (i == 0 && y < base_y + r.ymin)
                {
                    return 0;
                }

                if (y < base_y + r.ymax)
                {
                    break;
                }

                i += r.num_chars;
                base_y += r.baseline_y_delta;
            }

            if (i >= n)
            {
                return n;
            }

            if (x < r.x0)
            {
                return i;
            }

            if (x < r.x1)
            {
                float prev_x = r.x0;

                for (k = 0; k < r.num_chars; ++k)
                {
                    float w = Handler.GetWidth(i + k);

                    if (x < prev_x + w)
                    {
                        if (x < prev_x + w / 2)
                        {
                            return k + i;
                        }

                        return k + i + 1;
                    }

                    prev_x += w;
                }
            }

            if (text[i + r.num_chars - 1] == '\n')
            {
                return i + r.num_chars - 1;
            }

            return i + r.num_chars;
        }

        public void Click(float x, float y)
        {
            /*if (SingleLine)
            {
                var r = new TextEditRow();
                r = Handler.LayoutRow(0);
                y = r.ymin;
            }*/

            CursorIndex = LocateCoord(x, y);
            SelectStart = CursorIndex;
            SelectEnd = CursorIndex;
            HasPreferredX = false;
        }

        public void Drag(float x, float y)
        {
            int p = 0;
            /*if (SingleLine)
            {
                var r = new TextEditRow();
                r = Handler.LayoutRow(0);
                y = r.ymin;
            }*/

            if (SelectStart == SelectEnd)
            {
                SelectStart = CursorIndex;
            }

            p = LocateCoord(x, y);
            CursorIndex = SelectEnd = p;
        }

        public void Clamp()
        {
            int n = Length;

            if (SelectStart != SelectEnd)
            {
                if (SelectStart > n)
                {
                    SelectStart = n;
                }

                if (SelectEnd > n)
                {
                    SelectEnd = n;
                }

                if (SelectStart == SelectEnd)
                {
                    CursorIndex = SelectStart;
                }
            }

            if (CursorIndex > n)
            {
                CursorIndex = n;
            }
        }

        public void Delete(int where, int len)
        {
            MakeUndoDelete(where, len);
            DeleteChars(where, len);
            HasPreferredX = false;
        }

        public void DeleteSelection()
        {
            Clamp();

            if (SelectStart != SelectEnd)
            {
                if (SelectStart < SelectEnd)
                {
                    Delete(SelectStart, SelectEnd - SelectStart);
                    SelectEnd = CursorIndex = SelectStart;
                }
                else
                {
                    Delete(SelectEnd, SelectStart - SelectEnd);
                    SelectStart = CursorIndex = SelectEnd;
                }

                HasPreferredX = false;
            }
        }

        public void MoveToLast()
        {
            if (SelectStart != SelectEnd)
            {
                SortSelection();
                Clamp();
                CursorIndex = SelectEnd;
                SelectStart = SelectEnd;
                HasPreferredX = false;
            }
        }

        private static bool IsSpace(int codepoint)
        {
            return char.IsWhiteSpace((char) codepoint);
        }

        public bool IsWordBoundary(int idx)
        {
            return idx > 0 ? IsSpace(text[idx - 1]) && !IsSpace(text[idx]) : true;
        }

        public int MoveToPreviousWord(int c)
        {
            --c;

            while (c >= 0 && !IsWordBoundary(c))
            {
                --c;
            }

            if (c < 0)
            {
                c = 0;
            }

            return c;
        }

        public int MoveToNextWord(int c)
        {
            int len = Length;
            ++c;

            while (c < len && !IsWordBoundary(c))
            {
                ++c;
            }

            if (c > len)
            {
                c = len;
            }

            return c;
        }

        public int Cut()
        {
            if (SelectStart != SelectEnd)
            {
                DeleteSelection();
                HasPreferredX = false;

                return 1;
            }

            return 0;
        }

        private int PasteInternal(string text)
        {
            Clamp();
            DeleteSelection();

            if (InsertChars(CursorIndex, text) != 0)
            {
                MakeUndoInsert(CursorIndex, text.Length);
                CursorIndex += text.Length;
                HasPreferredX = false;

                return 1;
            }

            if (UndoState.undo_point != 0)
            {
                --UndoState.undo_point;
            }

            return 0;
        }

        public void InputChar(char ch)
        {
            if (ch == '\n' && SingleLine)
            {
                return;
            }

            if (InsertMode && !(SelectStart != SelectEnd) && CursorIndex < Length)
            {
                MakeUndoReplace(CursorIndex, 1, 1);
                DeleteChars(CursorIndex, 1);

                if (InsertChar(CursorIndex, ch) != 0)
                {
                    ++CursorIndex;
                    HasPreferredX = false;
                }
            }
            else
            {
                DeleteSelection();

                if (InsertChar(CursorIndex, ch) != 0)
                {
                    MakeUndoInsert(CursorIndex, 1);
                    ++CursorIndex;
                    HasPreferredX = false;
                }
            }
        }

        public void Key(ControlKeys key)
        {
            retry:

            switch (key)
            {
                case ControlKeys.InsertMode:
                    InsertMode = !InsertMode;

                    break;

                case ControlKeys.Undo:
                    Undo();
                    HasPreferredX = false;

                    break;

                case ControlKeys.Redo:
                    Redo();
                    HasPreferredX = false;

                    break;

                case ControlKeys.Left:
                    if (SelectStart != SelectEnd)
                    {
                        MoveToFirst();
                    }
                    else if (CursorIndex > 0)
                    {
                        --CursorIndex;
                    }

                    HasPreferredX = false;

                    break;

                case ControlKeys.Right:
                    if (SelectStart != SelectEnd)
                    {
                        MoveToLast();
                    }
                    else
                    {
                        ++CursorIndex;
                    }

                    Clamp();
                    HasPreferredX = false;

                    break;

                case ControlKeys.Left | ControlKeys.Shift:
                    Clamp();
                    PrepareSelectionAtCursor();

                    if (SelectEnd > 0)
                    {
                        --SelectEnd;
                    }

                    CursorIndex = SelectEnd;
                    HasPreferredX = false;

                    break;

                case ControlKeys.WordLeft:
                    if (SelectStart != SelectEnd)
                    {
                        MoveToFirst();
                    }
                    else
                    {
                        CursorIndex = MoveToPreviousWord(CursorIndex);
                        Clamp();
                    }

                    break;

                case ControlKeys.WordLeft | ControlKeys.Shift:
                    if (SelectStart == SelectEnd)
                    {
                        PrepareSelectionAtCursor();
                    }

                    CursorIndex = MoveToPreviousWord(CursorIndex);
                    SelectEnd = CursorIndex;
                    Clamp();

                    break;

                case ControlKeys.WordRight:
                    if (SelectStart != SelectEnd)
                    {
                        MoveToLast();
                    }
                    else
                    {
                        CursorIndex = MoveToNextWord(CursorIndex);
                        Clamp();
                    }

                    break;

                case ControlKeys.WordRight | ControlKeys.Shift:
                    if (SelectStart == SelectEnd)
                    {
                        PrepareSelectionAtCursor();
                    }

                    CursorIndex = MoveToNextWord(CursorIndex);
                    SelectEnd = CursorIndex;
                    Clamp();

                    break;

                case ControlKeys.Right | ControlKeys.Shift:
                    PrepareSelectionAtCursor();
                    ++SelectEnd;
                    Clamp();
                    CursorIndex = SelectEnd;
                    HasPreferredX = false;

                    break;

                case ControlKeys.Down:
                case ControlKeys.Down | ControlKeys.Shift:
                {
                    FindState find = new FindState();
                    TextEditRow row = new TextEditRow();
                    bool sel = (key & ControlKeys.Shift) != 0;

                    if (SingleLine)
                    {
                        key = ControlKeys.Right | (key & ControlKeys.Shift);

                        goto retry;
                    }

                    if (sel)
                    {
                        PrepareSelectionAtCursor();
                    }
                    else if (SelectStart != SelectEnd)
                    {
                        MoveToLast();
                    }

                    Clamp();
                    find.FindCharPosition(this, CursorIndex, SingleLine);

                    if (find.length != 0)
                    {
                        float goal_x = HasPreferredX ? PreferredX : find.x;
                        float x = 0;
                        int start = find.first_char + find.length;
                        CursorIndex = start;
                        row = Handler.LayoutRow(CursorIndex);
                        x = row.x0;

                        for (int i = 0; i < row.num_chars; ++i)
                        {
                            float dx = 1;
                            x += dx;

                            if (x > goal_x)
                            {
                                break;
                            }

                            ++CursorIndex;
                        }

                        Clamp();
                        HasPreferredX = true;
                        PreferredX = goal_x;

                        if (sel)
                        {
                            SelectEnd = CursorIndex;
                        }
                    }

                    break;
                }

                case ControlKeys.Up:
                case ControlKeys.Up | ControlKeys.Shift:
                {
                    FindState find = new FindState();
                    TextEditRow row = new TextEditRow();
                    int i = 0;
                    bool sel = (key & ControlKeys.Shift) != 0;

                    if (SingleLine)
                    {
                        key = ControlKeys.Left | (key & ControlKeys.Shift);

                        goto retry;
                    }

                    if (sel)
                    {
                        PrepareSelectionAtCursor();
                    }
                    else if (SelectStart != SelectEnd)
                    {
                        MoveToFirst();
                    }

                    Clamp();
                    find.FindCharPosition(this, CursorIndex, SingleLine);

                    if (find.prev_first != find.first_char)
                    {
                        float goal_x = HasPreferredX ? PreferredX : find.x;
                        float x = 0;
                        CursorIndex = find.prev_first;
                        row = Handler.LayoutRow(CursorIndex);
                        x = row.x0;

                        for (i = 0; i < row.num_chars; ++i)
                        {
                            float dx = 1;
                            x += dx;

                            if (x > goal_x)
                            {
                                break;
                            }

                            ++CursorIndex;
                        }

                        Clamp();
                        HasPreferredX = true;
                        PreferredX = goal_x;

                        if (sel)
                        {
                            SelectEnd = CursorIndex;
                        }
                    }

                    break;
                }

                case ControlKeys.Delete:
                case ControlKeys.Delete | ControlKeys.Shift:
                    if (SelectStart != SelectEnd)
                    {
                        DeleteSelection();
                    }
                    else
                    {
                        int n = Length;

                        if (CursorIndex < n)
                        {
                            Delete(CursorIndex, 1);
                        }
                    }

                    HasPreferredX = false;

                    break;

                case ControlKeys.BackSpace:
                case ControlKeys.BackSpace | ControlKeys.Shift:
                    if (SelectStart != SelectEnd)
                    {
                        DeleteSelection();
                    }
                    else
                    {
                        Clamp();

                        if (CursorIndex > 0)
                        {
                            Delete(CursorIndex - 1, 1);
                            --CursorIndex;
                        }
                    }

                    HasPreferredX = false;

                    break;

                case ControlKeys.TextStart:
                    CursorIndex = SelectStart = SelectEnd = 0;
                    HasPreferredX = false;

                    break;

                case ControlKeys.TextEnd:
                    CursorIndex = Length;
                    SelectStart = SelectEnd = 0;
                    HasPreferredX = false;

                    break;

                case ControlKeys.TextStart | ControlKeys.Shift:
                    PrepareSelectionAtCursor();
                    CursorIndex = SelectEnd = 0;
                    HasPreferredX = false;

                    break;

                case ControlKeys.TextEnd | ControlKeys.Shift:
                    PrepareSelectionAtCursor();
                    CursorIndex = SelectEnd = Length;
                    HasPreferredX = false;

                    break;

                case ControlKeys.LineStart:
                    Clamp();
                    MoveToFirst();

                    if (SingleLine)
                    {
                        CursorIndex = 0;
                    }
                    else
                    {
                        while (CursorIndex > 0 && text[CursorIndex - 1] != '\n')
                        {
                            --CursorIndex;
                        }
                    }

                    HasPreferredX = false;

                    break;

                case ControlKeys.LineEnd:
                {
                    int n = Length;
                    Clamp();
                    MoveToFirst();

                    if (SingleLine)
                    {
                        CursorIndex = n;
                    }
                    else
                    {
                        while (CursorIndex < n && text[CursorIndex] != '\n')
                        {
                            ++CursorIndex;
                        }
                    }

                    HasPreferredX = false;

                    break;
                }

                case ControlKeys.LineStart | ControlKeys.Shift:
                    Clamp();
                    PrepareSelectionAtCursor();

                    if (SingleLine)
                    {
                        CursorIndex = 0;
                    }
                    else
                    {
                        while (CursorIndex > 0 && text[CursorIndex - 1] != '\n')
                        {
                            --CursorIndex;
                        }
                    }

                    SelectEnd = CursorIndex;
                    HasPreferredX = false;

                    break;

                case ControlKeys.LineEnd | ControlKeys.Shift:
                {
                    int n = Length;
                    Clamp();
                    PrepareSelectionAtCursor();

                    if (SingleLine)
                    {
                        CursorIndex = n;
                    }
                    else
                    {
                        while (CursorIndex < n && text[CursorIndex] != '\n')
                        {
                            ++CursorIndex;
                        }
                    }

                    SelectEnd = CursorIndex;
                    HasPreferredX = false;

                    break;
                }
            }
        }

        public void Undo()
        {
            UndoState s = UndoState;

            if (s.undo_point == 0)
            {
                return;
            }

            UndoRecord u = new UndoRecord();
            u = s.undo_rec[s.undo_point - 1];
            int rpos = s.redo_point - 1;

            s.undo_rec[rpos].char_storage = -1;

            s.undo_rec[rpos].insert_length = u.delete_length;

            s.undo_rec[rpos].delete_length = u.insert_length;

            s.undo_rec[rpos].where = u.where;

            if (u.delete_length != 0)
            {
                if (s.undo_char_point + u.delete_length >= 999)
                {
                    s.undo_rec[rpos].insert_length = 0;
                }
                else
                {
                    int i = 0;

                    while (s.undo_char_point + u.delete_length > s.redo_char_point)
                    {
                        if (s.redo_point == 99)
                        {
                            return;
                        }

                        s.DiscardRedo();
                    }

                    rpos = s.redo_point - 1;

                    s.undo_rec[rpos].char_storage = s.redo_char_point - u.delete_length;

                    s.redo_char_point = s.redo_char_point - u.delete_length;

                    for (i = 0; i < u.delete_length; ++i)
                    {
                        s.undo_char[s.undo_rec[rpos].char_storage + i] = (sbyte) text[u.where + i];
                    }
                }

                DeleteChars(u.where, u.delete_length);
            }

            if (u.insert_length != 0)
            {
                InsertChars(u.where, s.undo_char, u.char_storage, u.insert_length);
                s.undo_char_point -= u.insert_length;
            }

            CursorIndex = u.where + u.insert_length;
            s.undo_point--;
            s.redo_point--;
        }

        public void Redo()
        {
            UndoState s = UndoState;
            UndoRecord r = new UndoRecord();

            if (s.redo_point == 99)
            {
                return;
            }

            int upos = s.undo_point;
            r = s.undo_rec[s.redo_point];

            s.undo_rec[upos].delete_length = r.insert_length;

            s.undo_rec[upos].insert_length = r.delete_length;

            s.undo_rec[upos].where = r.where;

            s.undo_rec[upos].char_storage = -1;

            UndoRecord u = s.undo_rec[upos];

            if (r.delete_length != 0)
            {
                if (s.undo_char_point + u.insert_length > s.redo_char_point)
                {
                    s.undo_rec[upos].insert_length = 0;

                    s.undo_rec[upos].delete_length = 0;
                }
                else
                {
                    int i = 0;

                    s.undo_rec[upos].char_storage = s.undo_char_point;

                    s.undo_char_point = s.undo_char_point + u.insert_length;
                    u = s.undo_rec[upos];

                    for (i = 0; i < u.insert_length; ++i)
                    {
                        s.undo_char[u.char_storage + i] = text[u.where + i];
                    }
                }

                DeleteChars(r.where, r.delete_length);
            }

            if (r.insert_length != 0)
            {
                InsertChars(r.where, s.undo_char, r.char_storage, r.insert_length);
                s.redo_char_point += r.insert_length;
            }

            CursorIndex = r.where + r.insert_length;
            s.undo_point++;
            s.redo_point++;
        }

        public void MakeUndoDelete(int where, int length)
        {
            int i;
            int? p = UndoState.CreateUndo(where, length, 0);

            if (p != null)
            {
                for (i = 0; i < length; ++i)
                {
                    UndoState.undo_char[p.Value + i] = text[where + i];
                }
            }
        }

        public void MakeUndoReplace(int where, int old_length, int new_length)
        {
            int i;
            int? p = UndoState.CreateUndo(where, old_length, new_length);

            if (p != null)
            {
                for (i = 0; i < old_length; ++i)
                {
                    UndoState.undo_char[p.Value + i] = text[where + i];
                }
            }
        }

        public int Paste(string ctext)
        {
            return PasteInternal(ctext);
        }
    }
}