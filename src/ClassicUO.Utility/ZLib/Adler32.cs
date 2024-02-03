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

namespace ZLibNative
{
    public class Adler32
    {
        private const int _base = 65521;
        private const int _nmax = 5550;
        private uint a = 1;
        private uint b;
        private int pend;

        public void Update(byte data)
        {
            if (pend >= _nmax)
            {
                UpdateModulus();
            }

            a += data;
            b += a;
            pend++;
        }

        public void Update(byte[] data)
        {
            Update(data, 0, data.Length);
        }

        public void Update(byte[] data, int offset, int length)
        {
            int nextJToComputeModulus = _nmax - pend;

            for (int j = 0; j < length; j++)
            {
                if (j == nextJToComputeModulus)
                {
                    UpdateModulus();
                    nextJToComputeModulus = j + _nmax;
                }

                unchecked
                {
                    a += data[j + offset];
                }

                b += a;
                pend++;
            }
        }

        public void Reset()
        {
            a = 1;
            b = 0;
            pend = 0;
        }

        private void UpdateModulus()
        {
            a %= _base;
            b %= _base;
            pend = 0;
        }

        public uint GetValue()
        {
            if (pend > 0)
            {
                UpdateModulus();
            }

            return (b << 16) | a;
        }
    }
}