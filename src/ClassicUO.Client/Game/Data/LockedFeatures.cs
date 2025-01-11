// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Game.Data
{
    [Flags]
    internal enum LockedFeatureFlags : uint
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

    internal class LockedFeatures
    {
        public LockedFeatureFlags Flags { get; private set; }

        public void SetFlags(LockedFeatureFlags flags)
        {
            Flags = flags;
        }
    }
}