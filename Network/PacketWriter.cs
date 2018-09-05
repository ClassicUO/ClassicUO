using System;

namespace ClassicUO.Network
{
    public class PacketWriter : PacketBase
    {
        private byte[] _data;

        public PacketWriter(byte id)
        {
            short len = PacketsTable.GetPacketLength(id);
            IsDynamic = len < 0;
            _data = new byte[IsDynamic ? 3 : len];
            _data[0] = id;
            Position = IsDynamic ? 3 : 1;
        }

        protected override byte this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }

        public override int Length => _data.Length;

        public override byte[] ToArray()
        {
            if (Length > Position)
            {
                Array.Resize(ref _data, Position);
            }

            WriteSize();
            return _data;
        }

        public void WriteSize()
        {
            if (IsDynamic)
            {
                this[1] = (byte)(Position >> 8);
                this[2] = (byte)Position;
            }
        }

        protected override void EnsureSize(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            if (IsDynamic)
            {
                while (Position + length > Length)
                {
                    Array.Resize(ref _data, Position + length);
                }
            }
            else if (Position + length > Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }
        }

        public void SendToClient()
        {
            WriteSize();
            //WOrion.SendToClient(this.Data, ref len);
        }

        public void SendToServer()
        {
            WriteSize();
            NetClient.Socket.Send(this);
            //WOrion.SendToServer(this.Data, ref len);
        }
    }
}