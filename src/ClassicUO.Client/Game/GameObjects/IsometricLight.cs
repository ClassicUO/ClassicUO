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

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public sealed class IsometricLight
    {
        private float _height = -0.75f;
        private int _overall = 9, _realOveall = 9;
        private int _personal = 9, _realPersonal = 9;

        public int Personal
        {
            get => _personal;
            set
            {
                _personal = value;
                Recalculate();
            }
        }

        public int Overall
        {
            get => _overall;
            set
            {
                _overall = value;
                Recalculate();
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                Recalculate();
            }
        }

        public int RealPersonal
        {
            get => _realPersonal;
            set
            {
                _realPersonal = value;
                Recalculate();
            }
        }

        public int RealOverall
        {
            get => _realOveall;
            set
            {
                _realOveall = value;
                Recalculate();
            }
        }

        public float IsometricLevel { get; private set; }

        public Vector3 IsometricDirection { get; } = new Vector3(-1.0f, -1.0f, .5f);

        private void Recalculate()
        {
            int reverted = 32 - Overall; //if overall is 0, we have MAXIMUM light, if 30, we have the MINIMUM light, so 30 is the max, but we must have some remainder for visibility

            float current = Personal > reverted ? Personal : reverted;
            IsometricLevel = current * 0.03125f;
        }
    }
}