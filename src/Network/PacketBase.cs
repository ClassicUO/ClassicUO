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

using System.Text;

namespace ClassicUO.Network
{
    internal abstract class PacketBase
    {
        public abstract byte this[int index] { get; set; }

        public abstract int Length { get; }

        public byte ID => this[0];

        public bool IsDynamic { get; protected set; }

        public int Position { get; protected set; }

        protected abstract bool EnsureSize(int length);
        public abstract ref byte[] ToArray();

        public void Skip(int length)
        {
            EnsureSize(length);
            Position += length;
        }

        public void Seek(int index)
        {
            Position = index;
            EnsureSize(0);
        }

        public void WriteByte(byte v)
        {
            EnsureSize(1);
            this[Position++] = v;
        }

        public void WriteBytes(byte[] buffer)
        {
            WriteBytes(buffer, 0, buffer.Length);
        }

        public void WriteBytes(byte[] buffer, int offset, int length)
        {
            EnsureSize(length);

            if (buffer == null)
            {
                this[Position++] = 0;

                return;
            }

            for (int i = offset; i < length; i++)
            {
                this[Position++] = buffer[i];
            }
        }

        public void WriteSByte(sbyte v)
        {
            WriteByte((byte) v);
        }

        public void WriteBool(bool v)
        {
            WriteByte(v ? (byte) 0x01 : (byte) 0x00);
        }

        public void WriteUShort(ushort v)
        {
            EnsureSize(2);
            WriteByte((byte) (v >> 8));
            WriteByte((byte) v);
        }

        public void WriteUInt(uint v)
        {
            EnsureSize(4);
            WriteByte((byte) (v >> 24));
            WriteByte((byte) (v >> 16));
            WriteByte((byte) (v >> 8));
            WriteByte((byte) v);
        }

        public void WriteASCII(string value)
        {
            EnsureSize(value.Length + 1);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (c != '\0')
                {
                    WriteByte((byte) c);
                }
            }

            WriteByte(0);
        }

        public void WriteASCII(string value, int length)
        {
            EnsureSize(length);

            for (int i = 0; i < length && i < value.Length; i++)
            {
                char c = value[i];

                if (c != '\0')
                {
                    WriteByte((byte) c);
                }
            }

            if (value.Length < length)
            {
                WriteByte(0);
                Position += length - value.Length - 1;
            }
        }

        public void WriteUnicode(string value)
        {
            EnsureSize((value.Length + 1) * 2);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (c != '\0')
                {
                    WriteUShort(c);
                }
            }

            WriteUShort(0);
        }

        public void WriteUnicode(string value, int length)
        {
            EnsureSize(length);

            for (int i = 0; i < length && i < value.Length; i++)
            {
                char c = value[i];

                if (c != '\0')
                {
                    WriteUShort(c);
                }
            }


            if (value.Length < length)
            {
                WriteUShort(0);
                Position += (length - value.Length - 1) * 2;
            }
        }


        public void WriteUTF8(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(value);
            EnsureSize(buffer.Length + 2);

            for (int i = 0; i < buffer.Length; i++)
            {
                WriteByte(buffer[i]);
            }

            WriteUShort(0);
        }

        public void WriteUTF8(string value, int length)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(value);
            EnsureSize(length);

            for (int i = 0; i < length; i++)
            {
                if (i < buffer.Length)
                {
                    WriteByte(buffer[i]);
                }
                else
                {
                    WriteByte(0); //padding with zero
                }
            }
        }
    }
}