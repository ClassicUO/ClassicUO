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