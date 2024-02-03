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

namespace StbTextEditSharp
{
    public struct FindState
    {
        public float x;
        public float y;
        public float height;
        public int first_char;
        public int length;
        public int prev_first;

        public void FindCharPosition(TextEdit str, int n, bool single_line)
        {
            TextEditRow r = new TextEditRow();
            int prev_start = 0;
            int z = str.Length;
            int i = 0;
            int first = 0;

            if (n == z)
            {
                if (single_line)
                {
                    r = str.Handler.LayoutRow(0);
                    y = 0;
                    first_char = 0;
                    length = z;
                    height = r.ymax - r.ymin;
                    x = r.x1;
                }
                else
                {
                    y = 0;
                    x = 0;
                    height = 1;

                    while (i < z)
                    {
                        r = str.Handler.LayoutRow(i);
                        prev_start = i;
                        i += r.num_chars;
                    }

                    first_char = i;
                    length = 0;
                    prev_first = prev_start;
                }

                return;
            }

            y = 0;

            for (;;)
            {
                r = str.Handler.LayoutRow(i);

                if (n < i + r.num_chars)
                {
                    break;
                }

                prev_start = i;
                i += r.num_chars;
                y += r.baseline_y_delta;
            }

            first_char = first = i;
            length = r.num_chars;
            height = r.ymax - r.ymin;
            prev_first = prev_start;
            x = r.x0;

            for (i = 0; first + i < n; ++i)
            {
                x += 1;
            }
        }
    }
}