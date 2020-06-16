using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility
{
    unsafe class StbTextEdit
    {
        const int STB_TEXTEDIT_UNDOSTATECOUNT = 99;
        const int STB_TEXTEDIT_UNDOCHARCOUNT = 999;

        struct StbUndoRecord
        {
            public int where, insert_length, delete_length, char_storage;
        }

        struct StbUndoState
        {
            public StbUndoRecord undo_rec;
            public int[] undo_char;
            public short undo_point, redo_point;
            public int undo_char_point, redo_char_point;
        }

        struct STB_TexteditState
        {
            public int cursor, selected_start, selected_end;
            public byte insert_mode;
            public byte cursor_at_end_of_line, initialized, single_line, padding1, padding2, padding3;
            public float preferred_x;
            public StbUndoState undostate;
        }

        struct StbTexteditRow
        {
            public float x0, x1, baseline_y_delta, ymin, ymax;
            public int num_chars;
        }


        //public static int stb_text_locate_coord(string str, float x, float y)
        //{
        //    StbTexteditRow r;
        //    int n = str.Length;
        //    float base_y = 0, prev_x;
        //    int i = 0, k;

        //    r.x0 = r.x1 = 0;
        //    r.ymin = r.ymax = 0;
        //    r.num_chars = 0;

        //    while (i < n)
        //    {

        //    }
        //}
    }
}
