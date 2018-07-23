using System;

namespace ClassicUO.Game.Network
{
    public abstract class PacketBase
    {
        protected abstract byte this[int index] { get; set; }

        public abstract int Length { get; }
        public byte ID => this[0];
        public bool IsDynamic { get; protected set; }
        public int Position { get; protected set; }
        protected abstract void EnsureSize(in int length);
        public abstract byte[] ToArray();

        public void Skip(in int lengh)
        {
            EnsureSize(lengh);
            Position += lengh;
        }

        public void Seek(in int index)
        {
            Position = index;
            EnsureSize(0);
        }

        public void WriteByte(in byte v)
        {
            EnsureSize(1);
            this[Position++] = v;
        }

        public void WriteSByte(in sbyte v)
        {
            WriteByte((byte) v);
        }

        public void WriteBool(in bool v)
        {
            WriteByte(v ? (byte) 0x01 : (byte) 0x00);
        }

        public void WriteUShort(in ushort v)
        {
            EnsureSize(2);
            WriteByte((byte) (v >> 8));
            WriteByte((byte) v);
        }

        public void WriteUInt(in uint v)
        {
            EnsureSize(4);
            WriteByte((byte) (v >> 24));
            WriteByte((byte) (v >> 16));
            WriteByte((byte) (v >> 8));
            WriteByte((byte) v);
        }

        public void WriteASCII(in string value)
        {
            EnsureSize(value.Length + 1);
            foreach (var c in value)
                WriteByte((byte) c);
            WriteByte(0);
        }

        public void WriteASCII(in string value, in int length)
        {
            EnsureSize(length);
            if (value.Length > length)
                throw new ArgumentOutOfRangeException();

            for (var i = 0; i < value.Length; i++)
                WriteByte((byte) value[i]);

            if (value.Length < length)
            {
                WriteByte(0);
                Position += length - value.Length - 1;
            }
        }

        public void WriteUnicode(in string value)
        {
            EnsureSize((value.Length + 1) * 2);
            foreach (var c in value)
            {
                WriteByte((byte) (c >> 8));
                WriteByte((byte) c);
            }

            WriteUShort(0);
        }

        public void WriteUnicode(in string value, in int length)
        {
            EnsureSize(length);
            if (value.Length > length)
                throw new ArgumentOutOfRangeException();

            for (var i = 0; i < value.Length; i++)
            {
                WriteByte((byte) (value[i] >> 8));
                WriteByte((byte) value[i]);
            }

            if (value.Length < length)
            {
                WriteUShort(0);
                Position += (length - value.Length - 1) * 2;
            }
        }
    }
}