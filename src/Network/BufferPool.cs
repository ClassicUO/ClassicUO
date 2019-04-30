#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System.Collections.Generic;

namespace ClassicUO.Network
{
    internal class BufferPool
    {
        private readonly int _arraySize;
        private readonly int _capacity;
        private readonly Queue<byte[]> _freeSegment;

        public BufferPool(int capacity, int arraysize)
        {
            _capacity = capacity;
            _arraySize = arraysize;
            _freeSegment = new Queue<byte[]>(capacity);
            for (int i = 0; i < capacity; i++) _freeSegment.Enqueue(new byte[arraysize]);
        }

        public byte[] GetFreeSegment()
        {
            lock (this)
            {
                if (_freeSegment.Count > 0) return _freeSegment.Dequeue();

                for (int i = 0; i < _capacity; i++) _freeSegment.Enqueue(new byte[_arraySize]);

                return _freeSegment.Dequeue();
            }
        }

        public void AddFreeSegment(byte[] segment)
        {
            if (segment == null) return;

            lock (this) _freeSegment.Enqueue(segment);
        }
    }
}