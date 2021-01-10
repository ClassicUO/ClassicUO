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

using System;

namespace ClassicUO.Network
{
    internal class PacketWriter : PacketBase
    {
        private byte[] _data;

        public PacketWriter(byte id)
        {
            this[0] = id;
        }

        public PacketWriter(byte[] data, int length)
        {
            Array.Resize(ref _data, length);

            for (int i = 0; i < length; i++)
            {
                _data[i] = data[i];
            }
        }

        public override byte this[int index]
        {
            get => _data[index];
            set
            {
                if (index == 0)
                {
                    SetPacketId(value);
                }
                else
                {
                    _data[index] = value;
                }
            }
        }

        public override int Length => _data.Length;

        private void SetPacketId(byte id)
        {
            short len = PacketsTable.GetPacketLength(id);
            IsDynamic = len < 0;
            _data = new byte[IsDynamic ? 32 : len];
            _data[0] = id;
            Position = IsDynamic ? 3 : 1;
        }

        public override ref byte[] ToArray()
        {
            if (IsDynamic && Length != Position)
            {
                Array.Resize(ref _data, Position);
            }

            WriteSize();

            return ref _data;
        }

        public void WriteSize()
        {
            if (IsDynamic)
            {
                this[1] = (byte) (Position >> 8);
                this[2] = (byte) Position;
            }
        }

        protected override bool EnsureSize(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (IsDynamic)
            {
                while (Position + length > Length)
                {
                    Array.Resize(ref _data, Length + length * 2);
                }

                return false;
            }

            return Position + length > Length;
        }
    }
}