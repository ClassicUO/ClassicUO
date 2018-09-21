#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
            if (Length > Position) Array.Resize(ref _data, Position);

            WriteSize();
            return _data;
        }

        public void WriteSize()
        {
            if (IsDynamic)
            {
                this[1] = (byte) (Position >> 8);
                this[2] = (byte) Position;
            }
        }

        protected override void EnsureSize(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length");

            if (IsDynamic)
                while (Position + length > Length) Array.Resize(ref _data, Position + length);
            else if (Position + length > Length) throw new ArgumentOutOfRangeException("length");
        }
    }
}