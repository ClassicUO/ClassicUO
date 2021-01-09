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

using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Controls
{
    internal class Panel : Control
    {
        private readonly UOTexture[] _frame = new UOTexture[9];

        public Panel(ushort background)
        {
            for (int i = 0; i < _frame.Length; i++)
            {
                _frame[i] = GumpsLoader.Instance.GetTexture((ushort) (background + i));
            }
        }

        public override void Update(double totalTime, double frameTime)
        {
            foreach (UOTexture t in _frame)
            {
                if (t != null)
                {
                    t.Ticks = (long) totalTime;
                }
            }

            base.Update(totalTime, frameTime);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();

            int centerWidth = Width - _frame[0].Width - _frame[2].Width;

            int centerHeight = Height - _frame[0].Height - _frame[6].Height;

            int line2Y = y + _frame[0].Height;

            int line3Y = y + Height - _frame[6].Height;

            // top row
            batcher.Draw2D(_frame[0], x, y, ref HueVector);

            batcher.Draw2DTiled
            (
                _frame[1],
                x + _frame[0].Width,
                y,
                centerWidth,
                _frame[0].Height,
                ref HueVector
            );

            batcher.Draw2D(_frame[2], x + Width - _frame[2].Width, y, ref HueVector);

            // middle
            batcher.Draw2DTiled
            (
                _frame[3],
                x,
                line2Y,
                _frame[3].Width,
                centerHeight,
                ref HueVector
            );

            batcher.Draw2DTiled
            (
                _frame[4],
                x + _frame[3].Width,
                line2Y,
                centerWidth,
                centerHeight,
                ref HueVector
            );

            batcher.Draw2DTiled
            (
                _frame[5],
                x + Width - _frame[5].Width,
                line2Y,
                _frame[5].Width,
                centerHeight,
                ref HueVector
            );

            // bottom
            batcher.Draw2D(_frame[6], x, line3Y, ref HueVector);

            batcher.Draw2DTiled
            (
                _frame[7],
                x + _frame[6].Width,
                line3Y,
                centerWidth,
                _frame[6].Height,
                ref HueVector
            );

            batcher.Draw2D(_frame[8], x + Width - _frame[8].Width, line3Y, ref HueVector);

            return base.Draw(batcher, x, y);
        }
    }
}