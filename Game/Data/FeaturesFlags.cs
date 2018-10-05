using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Data
{
    [Flags]
    public enum FeatureFlags : uint
    {
        TheSecondAge = 0x1,
        Renaissance = 0x2,
        ThirdDawn = 0x4,
        LordBlackthornsRevenge = 0x8,
        AgeOfShadows = 0x10,
        CharacterSlot6 = 0x20,
        SameraiEmpire = 0x40,
        MondainsLegacy = 0x80,
        Splash8 = 0x100,
        Splash9 = 0x200,            // Ninth Age splash screen, crystal/shadow housing tiles
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
}
