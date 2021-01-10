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

using System;

namespace ClassicUO.Game.Data
{
    [Flags]
    internal enum LockedFeatureFlags : uint
    {
        TheSecondAge = 0x1,
        Renaissance = 0x2,
        ThirdDawn = 0x4,
        LordBlackthornsRevenge = 0x8,
        AgeOfShadows = 0x10,
        CharacterSlot6 = 0x20,
        SamuraiEmpire = 0x40,
        MondainsLegacy = 0x80,
        Splash8 = 0x100,
        Splash9 = 0x200, // Ninth Age splash screen, crystal/shadow housing tiles
        TenthAge = 0x400,
        MoreStorage = 0x800,
        CharacterSlot7 = 0x1000,
        TenthAgeFaces = 0x2000,
        TrialAccount = 0x4000,
        EleventhAge = 0x8000,
        StygianAbyss = 0x10000,
        HighSeas = 0x20000,
        GothicHousing = 0x40000,
        RusticHousing = 0x80000
    }

    internal class LockedFeatures
    {
        public LockedFeatureFlags Flags { get; private set; }

        public bool T2A => Flags.HasFlag(LockedFeatureFlags.TheSecondAge);

        public bool UOR => Flags.HasFlag(LockedFeatureFlags.Renaissance);

        public bool ThirdDawn => Flags.HasFlag(LockedFeatureFlags.ThirdDawn);

        public bool LBR => Flags.HasFlag(LockedFeatureFlags.LordBlackthornsRevenge);

        public bool AOS => Flags.HasFlag(LockedFeatureFlags.AgeOfShadows);

        public bool CharSlots6 => Flags.HasFlag(LockedFeatureFlags.CharacterSlot6);

        public bool SE => Flags.HasFlag(LockedFeatureFlags.SamuraiEmpire);

        public bool ML => Flags.HasFlag(LockedFeatureFlags.MondainsLegacy);

        public bool Splash8th => Flags.HasFlag(LockedFeatureFlags.Splash8);

        public bool Splash9th => Flags.HasFlag(LockedFeatureFlags.Splash9);

        public bool TenthAge => Flags.HasFlag(LockedFeatureFlags.TenthAge);

        public bool MoreStorage => Flags.HasFlag(LockedFeatureFlags.MoreStorage);

        public bool CharSlots7 => Flags.HasFlag(LockedFeatureFlags.CharacterSlot7);

        public bool TenthAgeFaces => Flags.HasFlag(LockedFeatureFlags.TenthAgeFaces);

        public bool TrialAccount => Flags.HasFlag(LockedFeatureFlags.TrialAccount);

        public bool EleventhAge => Flags.HasFlag(LockedFeatureFlags.EleventhAge);

        public bool SA => Flags.HasFlag(LockedFeatureFlags.StygianAbyss);


        public void SetFlags(LockedFeatureFlags flags)
        {
            Flags = flags;
        }
    }
}