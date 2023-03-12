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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class CorpseManager
    {
        private readonly Deque<CorpseInfo> _corpses = new Deque<CorpseInfo>();

        public void Add(uint corpse, uint obj, Direction dir, bool run)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse)
                {
                    return;
                }
            }

            _corpses.AddToBack(new CorpseInfo(corpse, obj, dir, run));
        }

        public void Remove(uint corpse, uint obj)
        {
            for (int i = 0; i < _corpses.Count;)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse || c.ObjectSerial == obj)
                {
                    if (corpse != 0)
                    {
                        Item item = World.Items.Get(corpse);

                        if (item != null)
                        {
                            item.Layer = (Layer) ((c.Direction & Direction.Mask) | (c.IsRunning ? Direction.Running : 0));
                        }
                    }

                    _corpses.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public bool Exists(uint corpse, uint obj)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse || c.ObjectSerial == obj)
                {
                    return true;
                }
            }

            return false;
        }

        public Item GetCorpseObject(uint serial)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.ObjectSerial == serial)
                {
                    return World.Items.Get(c.CorpseSerial);
                }
            }

            return null;
        }

        public void Clear()
        {
            _corpses.Clear();
        }
    }

    internal struct CorpseInfo
    {
        public CorpseInfo(uint corpseSerial, uint objectSerial, Direction direction, bool isRunning)
        {
            CorpseSerial = corpseSerial;
            ObjectSerial = objectSerial;
            Direction = direction;
            IsRunning = isRunning;
        }

        public uint CorpseSerial, ObjectSerial;
        public Direction Direction;
        public bool IsRunning;
    }
}