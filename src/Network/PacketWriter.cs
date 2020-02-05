#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
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

        public PacketWriter(byte[] data)
        {
            Array.Resize(ref _data, data.Length);

            for (int i = 0; i < data.Length; i++)
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
                    SetPacketId(value);
                else
                    _data[index] = value;
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
                Array.Resize(ref _data, Position);

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
                throw new ArgumentOutOfRangeException(nameof(length));

            if (IsDynamic)
            {
                while (Position + length > Length)
                    Array.Resize(ref _data, Length + length * 2);

                return false;
            }

            return Position + length > Length;
        }
    }
}