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

using ClassicUO.Resources;
using System;

namespace ClassicUO.Game.Data
{
    public enum Lock : byte
    {
        Up = 0,
        Down = 1,
        Locked = 2
    }

    public sealed class Skill
    {
        public static event EventHandler<SkillChangeArgs> SkillValueChangedEvent;
        public static event EventHandler<SkillChangeArgs> SkillBaseChangedEvent;
        public static event EventHandler<SkillChangeArgs> SkillCapChangedEvent;

        public Skill(string name, int index, bool click)
        {
            Name = name;
            Index = index;
            IsClickable = click;
        }

        public Lock Lock { get; internal set; }

        public ushort ValueFixed { get; internal set; }

        public ushort BaseFixed { get; internal set; }

        public ushort CapFixed { get; internal set; }

        public float Value => ValueFixed / 10.0f;

        public float Base => BaseFixed / 10.0f;

        public float Cap => CapFixed / 10.0f;

        public bool IsClickable { get; }

        public string Name { get; }

        public int Index { get; }

        public static void InvokeSkillValueChanged(int index)
        {
            SkillValueChangedEvent?.Invoke(null, new SkillChangeArgs(index));
        }
        public static void InvokeSkillBaseChanged(int index)
        {
            SkillBaseChangedEvent?.Invoke(null, new SkillChangeArgs(index));
        }
        public static void InvokeSkillCapChanged(int index)
        {
            SkillCapChangedEvent?.Invoke(null, new SkillChangeArgs(index));
        }

        public override string ToString()
        {
            return string.Format(ResGeneral.Name0Val1, Name, Value);
        }

        public class SkillChangeArgs : EventArgs
        {
            public int Index;
            public SkillChangeArgs(int index)
            {
                Index = index;
            }
        }
    }
}