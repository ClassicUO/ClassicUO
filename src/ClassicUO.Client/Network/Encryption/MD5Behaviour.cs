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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.Network.Encryption
{
    unsafe sealed class MD5Behaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MD5Context
        {
            public ulong Size;
            public fixed uint _buffer[4];
            public fixed byte _input[64];
            public fixed byte _digest[16];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref uint Buffer(int index)
            {
                fixed (uint* ptr = &_buffer[0])
                {
                    return ref *(ptr + index);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref byte Input(int index)
            {
                fixed (byte* ptr = &_input[0])
                {
                    return ref *(ptr + index);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref byte Digest(int index)
            {
                fixed (byte* ptr = &_digest[0])
                {
                    return ref *(ptr + index);
                }
            }
        }


        const uint A = 0x67452301;
        const uint B = 0xefcdab89;
        const uint C = 0x98badcfe;
        const uint D = 0x10325476;

        static uint[] _s =
        {
            7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
            5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20,
            4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
            6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
        };

        static uint[] _k =
        {
            0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee,
            0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
            0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be,
            0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
            0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa,
            0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
            0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed,
            0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
            0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c,
            0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
            0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05,
            0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
            0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039,
            0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
            0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1,
            0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391
        };

        static byte[] _padding =
        {
            0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint F(uint x, uint y, uint z)
            => ((x & y) | (~x & z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint G(uint x, uint y, uint z)
           => ((x & z) | (y & ~z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint H(uint x, uint y, uint z)
           => (x ^ y ^ z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint I(uint x, uint y, uint z)
           => (y ^ (x | ~z));


        public void Initialize(ref MD5Context ctx)
        {
            ctx.Size = 0;
            ctx.Buffer(0) = A;
            ctx.Buffer(1) = B;
            ctx.Buffer(2) = C;
            ctx.Buffer(3) = D;
        }

        public void Update(ref MD5Context ctx, Span<byte> inputBuffer)
        {
            Span<uint> input = stackalloc uint[16];
            int offset = (int)(ctx.Size % 64);
            ctx.Size += (ulong)inputBuffer.Length;


            for (int i = 0; i < inputBuffer.Length; i++)
            {
                ctx.Input(offset++) = inputBuffer[i];

                if ((offset % 64) == 0)
                {
                    for (int j = 0; j < 16; ++j)
                    {
                        input[j] =
                            (uint)(ctx.Input((j * 4) + 3)) << 24 |
                            (uint)(ctx.Input((j * 4) + 2)) << 16 |
                            (uint)(ctx.Input((j * 4) + 1)) << 8 |
                            (uint)(ctx.Input((j * 4)));
                    }

                    Step(ref ctx, input);
                    offset = 0;
                }
            }
        }

        public void Finalize(ref MD5Context ctx)
        {
            Span<uint> input = stackalloc uint[16];
            int offset = (int)(ctx.Size % 64);

            uint paddingLength = (uint)(offset < 56 ? 56 - offset : (56 + 64) - offset);

            Update(ref ctx, _padding.AsSpan(0, (int)paddingLength));
            ctx.Size -= (ulong)paddingLength;

            for (int j = 0; j < 14; ++j)
            {
                input[j] =
                            (uint)(ctx.Input((j * 4) + 3)) << 24 |
                            (uint)(ctx.Input((j * 4) + 2)) << 16 |
                            (uint)(ctx.Input((j * 4) + 1)) << 8 |
                            (uint)(ctx.Input((j * 4)));
            }

            input[14] = (uint)(ctx.Size * 8);
            input[15] = (uint)((ctx.Size * 8) >> 32);

            Step(ref ctx, input);

            for (int i = 0; i < 4; ++i)
            {
                ctx.Digest((i * 4) + 0) = (byte)((ctx.Buffer(i) & 0x000000FF));
                ctx.Digest((i * 4) + 1) = (byte)((ctx.Buffer(i) & 0x0000FF00) >> 8);
                ctx.Digest((i * 4) + 2) = (byte)((ctx.Buffer(i) & 0x00FF0000) >> 16);
                ctx.Digest((i * 4) + 3) = (byte)((ctx.Buffer(i) & 0xFF000000) >> 24);
            }
        }

        private void Step(ref MD5Context ctx, Span<uint> input)
        {
            uint AA = ctx.Buffer(0);
            uint BB = ctx.Buffer(1);
            uint CC = ctx.Buffer(2);
            uint DD = ctx.Buffer(3);
            uint E;

            int j;

            for (int i = 0; i < 64; ++i)
            {
                switch (i / 16)
                {
                    case 0:
                        E = F(BB, CC, DD);
                        j = i;
                        break;
                    case 1:
                        E = G(BB, CC, DD);
                        j = ((i * 5) + 1) % 16;
                        break;
                    case 2:
                        E = H(BB, CC, DD);
                        j = ((i * 3) + 5) % 16;
                        break;
                    default:
                        E = I(BB, CC, DD);
                        j = (i * 7) % 16;
                        break;
                }

                uint temp = DD;
                DD = CC;
                CC = BB;
                BB = BB + RotateLeft(AA + E + _k[i] + input[j], _s[i]);
                AA = temp;
            }

            ctx.Buffer(0) += AA;
            ctx.Buffer(1) += BB;
            ctx.Buffer(2) += CC;
            ctx.Buffer(3) += DD;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint x, uint n)
            => (uint)((x << (int)n) | (x >> (int)(32 - n)));
    }
}