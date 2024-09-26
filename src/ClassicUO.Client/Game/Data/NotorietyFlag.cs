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

using ClassicUO.Configuration;

namespace ClassicUO.Game.Data
{
    public enum NotorietyFlag : byte
    {
        Unknown = 0x00,
        Innocent = 0x01,
        Ally = 0x02,
        Gray = 0x03,
        Criminal = 0x04,
        Enemy = 0x05,
        Murderer = 0x06,
        Invulnerable = 0x07
    }

    public static class Notoriety
    {
        public static ushort GetHue(NotorietyFlag flag)
        {
            switch (flag)
            {
                case NotorietyFlag.Innocent: return ProfileManager.CurrentProfile.InnocentHue;

                case NotorietyFlag.Ally: return ProfileManager.CurrentProfile.FriendHue;

                case NotorietyFlag.Criminal: return ProfileManager.CurrentProfile.CriminalHue;

                case NotorietyFlag.Gray: return ProfileManager.CurrentProfile.CanAttackHue;

                case NotorietyFlag.Enemy: return ProfileManager.CurrentProfile.EnemyHue;

                case NotorietyFlag.Murderer: return ProfileManager.CurrentProfile.MurdererHue;

                case NotorietyFlag.Invulnerable: return 0x0034;

                default: return 0;
            }
        }

        public static string GetHTMLHue(NotorietyFlag flag)
        {
            switch (flag)
            {
                case NotorietyFlag.Innocent: return "<basefont color=\"cyan\">";

                case NotorietyFlag.Ally: return "<basefont color=\"lime\">";

                case NotorietyFlag.Criminal:
                case NotorietyFlag.Gray: return "<basefont color=\"gray\">";

                case NotorietyFlag.Enemy: return "<basefont color=\"orange\">";

                case NotorietyFlag.Murderer: return "<basefont color=\"red\">";

                case NotorietyFlag.Invulnerable: return "<basefont color=\"yellow\">";

                default: return string.Empty;
            }
        }
    }
}