﻿#region license

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

using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class UseItemQueue
    {
        private readonly Deque<uint> _actions = new Deque<uint>();
        private long _timer;


        public UseItemQueue()
        {
            _timer = Time.Ticks + 1000;
        }

        public void Update()
        {
            if (_timer < Time.Ticks)
            {
                _timer = Time.Ticks + 1000;

                if (_actions.Count == 0)
                {
                    return;
                }

                uint serial = _actions.RemoveFromFront();

                if (World.Get(serial) != null)
                {
                    if (SerialHelper.IsMobile(serial))
                    {
                        serial |= 0x8000_0000;
                    }

                    GameActions.DoubleClick(serial);
                }
            }
        }

        public void Add(uint serial)
        {
            foreach (uint s in _actions)
            {
                if (serial == s)
                {
                    return;
                }
            }

            _actions.AddToBack(serial);
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public void ClearCorpses()
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                Entity entity = World.Get(_actions[i]);

                if (entity == null)
                {
                    continue;
                }

                if (entity is Item it && it.IsCorpse)
                {
                    _actions.RemoveAt(i--);
                }
            }
        }
    }
}