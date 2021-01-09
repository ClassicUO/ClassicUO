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

using System.Collections;
using System.Collections.Generic;

namespace ClassicUO.Game.GameObjects
{
    internal class EntityCollection<T> : IEnumerable<T> where T : Entity
    {
        private readonly Dictionary<uint, T> _entities = new Dictionary<uint, T>();

        public int Count => _entities.Count;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _entities.Values.GetEnumerator();
        }


        public bool Contains(uint serial)
        {
            return _entities.ContainsKey(serial);
        }

        public T Get(uint serial)
        {
            _entities.TryGetValue(serial, out T entity);

            return entity;
        }

        public bool Add(T entity)
        {
            if (_entities.ContainsKey(entity.Serial))
            {
                return false;
            }

            _entities[entity.Serial] = entity;

            return true;
        }

        public void Remove(uint serial)
        {
            _entities.Remove(serial);
        }

        public void Replace(T entity, uint newSerial)
        {
            if (_entities.Remove(entity.Serial))
            {
                for (LinkedObject i = entity.Items; i != null; i = i.Next)
                {
                    Item it = (Item) i;
                    it.Container = newSerial;
                }

                _entities[newSerial] = entity;
                entity.Serial = newSerial;
            }
        }

        public void Clear()
        {
            _entities.Clear();
        }
    }
}