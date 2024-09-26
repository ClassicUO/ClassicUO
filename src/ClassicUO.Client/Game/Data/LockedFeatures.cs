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

using System;

namespace ClassicUO.Game.Data
{
    [Flags]
    public enum LockedFeatureFlags : uint
    {
        None = 0x00000000,
        T2A = 0x00000001,
        UOR = 0x00000002, // In later clients, the T2A/UOR flags are negative feature flags to disable body replacement of Pre-AOS graphics.
        UOTD = 0x00000004,
        LBR = 0x00000008,
        AOS = 0x00000010,
        SixthCharacterSlot = 0x00000020,
        SE = 0x00000040,
        ML = 0x00000080,
        EighthAge = 0x00000100,
        NinthAge = 0x00000200, // Crystal/Shadow Custom House Tiles
        TenthAge = 0x00000400,
        IncreasedStorage = 0x00000800, // Increased Housing/Bank Storage
        SeventhCharacterSlot = 0x00001000,
        RoleplayFaces = 0x00002000,
        TrialAccount = 0x00004000,
        LiveAccount = 0x00008000,
        SA = 0x00010000,
        HS = 0x00020000,
        Gothic = 0x00040000,
        Rustic = 0x00080000,
        Jungle = 0x00100000,
        Shadowguard = 0x00200000,
        TOL = 0x00400000,
        EJ = 0x00800000,

        ExpansionNone = None,
        ExpansionT2A = T2A,
        ExpansionUOR = ExpansionT2A | UOR,
        ExpansionUOTD = ExpansionUOR | UOTD,
        ExpansionLBR = ExpansionUOTD | LBR,
        ExpansionAOS = LBR | AOS | LiveAccount,
        ExpansionSE = ExpansionAOS | SE,
        ExpansionML = ExpansionSE | ML | NinthAge,
        ExpansionSA = ExpansionML | SA | Gothic | Rustic,
        ExpansionHS = ExpansionSA | HS,
        ExpansionTOL = ExpansionHS | TOL | Jungle | Shadowguard,
        ExpansionEJ = ExpansionTOL | EJ
    }

    public class LockedFeatures
    {
        public LockedFeatureFlags Flags { get; private set; }

        public void SetFlags(LockedFeatureFlags flags)
        {
            Flags = flags;
        }
    }
}