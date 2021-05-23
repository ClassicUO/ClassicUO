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

using ClassicUO.Game.Managers;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal sealed class FixedEffect : GameEffect
    {
        public FixedEffect(EffectManager manager, ushort graphic, ushort hue, int duration, byte speed) 
            : base(manager, graphic, hue, duration, speed)
        {
            
        }

        public FixedEffect
        (
            EffectManager manager,
            int sourceX,
            int sourceY,
            int sourceZ,
            ushort graphic,
            ushort hue,
            int duration,
            byte speed
        ) : this(manager, graphic, hue, duration, speed)
        {
            SetSource(sourceX, sourceY, sourceZ);
        }

        public FixedEffect
        (
            EffectManager manager,
            uint sourceSerial,
            int sourceX,
            int sourceY,
            int sourceZ,
            ushort graphic,
            ushort hue,
            int duration,
            byte speed
        ) : this(manager, graphic, hue, duration, speed)
        {
            Entity source = World.Get(sourceSerial);

            if (source != null && SerialHelper.IsValid(sourceSerial))
            {
                SetSource(source);
            }
            else
            {
                SetSource(sourceX, sourceY, sourceZ);
            }
        }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (!IsDestroyed)
            {
                (int x, int y, int z) = GetSource();

                if (Source != null)
                {
                    Offset = Source.Offset;
                }

                if (X != x || Y != y || Z != z)
                {
                    X = (ushort) x;
                    Y = (ushort) y;
                    Z = (sbyte) z;
                    UpdateScreenPosition();
                    AddToTile();
                }
            }
        }
    }
}