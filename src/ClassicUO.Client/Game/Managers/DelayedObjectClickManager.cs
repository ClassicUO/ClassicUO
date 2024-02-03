#region license

// Copyright (c) 2024, andreakarasho
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
using ClassicUO.Input;

namespace ClassicUO.Game.Managers
{
    internal sealed class DelayedObjectClickManager
    {
        private readonly World _world;

        public DelayedObjectClickManager(World world) { _world = world; }

        public uint Serial { get; private set; }
        public bool IsEnabled { get; private set; }
        public uint Timer { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int LastMouseX { get; set; }
        public int LastMouseY { get; set; }


        public void Update()
        {
            if (!IsEnabled || Timer > Time.Ticks)
            {
                return;
            }

            Entity entity = _world.Get(Serial);

            if (entity != null)
            {
                if (!_world.ClientFeatures.TooltipsEnabled || SerialHelper.IsItem(Serial) && ((Item) entity).IsLocked && ((Item) entity).ItemData.Weight == 255 && !((Item) entity).ItemData.IsContainer)
                {
                    GameActions.SingleClick(_world, Serial);
                }

                if (_world.ClientFeatures.PopupEnabled)
                {
                    GameActions.OpenPopupMenu(Serial);
                }
            }

            Clear();
        }

        public void Set(uint serial, int x, int y, uint timer)
        {
            Serial = serial;
            LastMouseX = Mouse.Position.X;
            LastMouseY = Mouse.Position.Y;
            X = x;
            Y = y;
            Timer = timer;
            IsEnabled = true;
        }

        public void Clear()
        {
            IsEnabled = false;
            Serial = 0xFFFF_FFFF;
            Timer = 0;
        }

        public void Clear(uint serial)
        {
            if (Serial == serial)
            {
                Timer = 0;
                Serial = 0;
                IsEnabled = false;
                X = 0;
                Y = 0;
                LastMouseX = 0;
                LastMouseY = 0;
            }
        }
    }
}