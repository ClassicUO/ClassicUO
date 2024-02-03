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

namespace ClassicUO.Network.Encryption
{
    internal sealed class LoginCryptBehaviour
    {
        private uint _k1, _k2, _k3;
        private readonly uint[] _key = new uint[2];
        private readonly byte[] _seed = new byte[4];


        public void Initialize(uint seed, uint k1, uint k2, uint k3)
        {
            _seed[0] = (byte) ((seed >> 24) & 0xFF);
            _seed[1] = (byte) ((seed >> 16) & 0xFF);
            _seed[2] = (byte) ((seed >> 8) & 0xFF);
            _seed[3] = (byte) (seed & 0xFF);

            _k1 = k1;
            _k2 = k2;
            _k3 = k3;

            const uint seed_key = 0x0000_1357;

            _key[0] = ((~seed ^ seed_key) << 16) | ((seed ^ 0xffffaaaa) & 0x0000ffff);
            _key[1] = ((seed ^ 0x43210000) >> 16) | ((~seed ^ 0xabcdffff) & 0xffff0000);
        }


        public void Encrypt(Span<byte> src, Span<byte> dst, int size)
        {
            for (int i = 0; i < size; i++)
            {
                dst[i] = (byte) (src[i] ^ (byte) _key[0]);

                uint table0 = _key[0];
                uint table1 = _key[1];

                _key[1] = (((((table1 >> 1) | (table0 << 31)) ^ _k1) >> 1) | (table0 << 31)) ^ _k2;
                _key[0] = ((table0 >> 1) | (table1 << 31)) ^ _k3;
            }
        }

        public void Encrypt_OLD(Span<byte> src, Span<byte> dst, int size)
        {
            for (int i = 0; i < size; i++)
            {
                dst[i] = (byte) (src[i] ^ (byte) _key[0]);

                uint table0 = _key[0];
                uint table1 = _key[1];

                _key[0] = ((table0 >> 1) | (table1 << 31)) ^ _k2;
                _key[1] = ((table1 >> 1) | (table0 << 31)) ^ _k1;
            }
        }

        public void Encrypt_1_25_36(Span<byte> src, Span<byte> dst, int size)
        {
            for (int i = 0; i < size; i++)
            {
                dst[i] = (byte) (src[i] ^ (byte) _key[0]);

                uint table0 = _key[0];
                uint table1 = _key[1];

                _key[0] = ((table0 >> 1) | (table1 << 31)) ^ _k2;
                _key[1] = ((table1 >> 1) | (table0 << 31)) ^ _k1;

                _key[1] = (_k1 >> (byte) ((5 * table1 * table1) & 0xFF)) + table1 * _k1 + table0 * table0 * 0x35ce9581 + 0x07afcc37;

                _key[0] = (_k2 >> (byte) ((3 * table0 * table0) & 0xFF)) + table0 * _k2 + _key[1] * _key[1] * 0x4c3a1353 + 0x16ef783f;
            }
        }
    }
}