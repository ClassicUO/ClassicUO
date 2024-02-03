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

namespace StbTextEditSharp
{
    public class UndoState
    {
        public int redo_char_point;
        public short redo_point;
        public int[] undo_char = new int[999];
        public int undo_char_point;
        public short undo_point;
        public UndoRecord[] undo_rec = new UndoRecord[99];

        public void FlushRedo()
        {
            redo_point = 99;
            redo_char_point = 999;
        }

        public void DiscardUndo()
        {
            if (undo_point > 0)
            {
                if (undo_rec[0].char_storage >= 0)
                {
                    int n = undo_rec[0].insert_length;

                    undo_char_point -= n;

                    Array.Copy
                    (
                        undo_char,
                        n,
                        undo_char,
                        0,
                        undo_char_point
                    );

                    for (int i = 0; i < undo_point; ++i)
                    {
                        if (undo_rec[i].char_storage >= 0)
                        {
                            undo_rec[i].char_storage -= n;
                        }
                    }
                }

                --undo_point;

                Array.Copy
                (
                    undo_rec,
                    1,
                    undo_rec,
                    0,
                    undo_point
                );
            }
        }

        public void DiscardRedo()
        {
            int num;
            int k = 99 - 1;

            if (redo_point <= k)
            {
                if (undo_rec[k].char_storage >= 0)
                {
                    int n = undo_rec[k].insert_length;

                    int i;
                    redo_char_point += n;
                    num = 999 - redo_char_point;

                    Array.Copy
                    (
                        undo_char,
                        redo_char_point - n,
                        undo_char,
                        redo_char_point,
                        num
                    );

                    for (i = (int) redo_point; i < k; ++i)
                    {
                        if (undo_rec[i].char_storage >= 0)
                        {
                            undo_rec[i].char_storage += n;
                        }
                    }
                }

                ++redo_point;
                num = 99 - redo_point;

                if (num != 0)
                {
                    Array.Copy
                    (
                        undo_rec,
                        redo_point,
                        undo_rec,
                        redo_point - 1,
                        num
                    );
                }
            }
        }

        public int? CreateUndoRecord(int numchars)
        {
            FlushRedo();

            if (undo_point == 99)
            {
                DiscardUndo();
            }

            if (numchars > 999)
            {
                undo_point = 0;
                undo_char_point = 0;

                return null;
            }

            while (undo_char_point + numchars > 999)
            {
                DiscardUndo();
            }

            return undo_point++;
        }

        public int? CreateUndo(int pos, int insert_len, int delete_len)
        {
            int? rpos = CreateUndoRecord(insert_len);

            if (rpos == null)
            {
                return null;
            }

            int rposv = rpos.Value;

            undo_rec[rposv].where = pos;

            undo_rec[rposv].insert_length = (short) insert_len;

            undo_rec[rposv].delete_length = (short) delete_len;

            if (insert_len == 0)
            {
                undo_rec[rposv].char_storage = -1;

                return null;
            }

            undo_rec[rposv].char_storage = (short) undo_char_point;

            undo_char_point = (short) (undo_char_point + insert_len);

            return undo_rec[rposv].char_storage;
        }
    }
}