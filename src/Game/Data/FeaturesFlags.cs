#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;

namespace ClassicUO.Game.Data
{
    [Flags]
    public enum ClientFlags : uint
    {
        CF_T2A = 0x00,
        CF_RE = 0x01,
        CF_TD = 0x02,
        CF_LBR = 0x04,
        CF_AOS = 0x08,
        CF_SE = 0x10,
        CF_SA = 0x20,
        CF_UO3D = 0x40,
        CF_RESERVED = 0x80,
        CF_3D = 0x100,
        CF_UNDEFINED = 0xFFFF,
    }

    [Flags]
    public enum LockedFeatureFlags : uint
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

    [Flags]
    public enum CharacterListFlags
    {
        CLF_UNKNOWN = 0x01,
        CLF_OWERWRITE_CONFIGURATION_BUTTON = 0x02,
        CLF_ONE_CHARACTER_SLOT = 0x04,
        CLF_CONTEXT_MENU = 0x08,
        CLF_LIMIT_CHARACTER_SLOTS = 0x10,
        CLF_PALADIN_NECROMANCER_TOOLTIPS = 0x20,
        CLF_6_CHARACTER_SLOT = 0x40,
        CLF_SAMURAI_NINJA = 0x80,
        CLF_ELVEN_RACE = 0x100,
        CLF_UNKNOWN_1 = 0x200,
        CLF_UO3D = 0x400,
        CLF_UNKNOWN_2 = 0x800,
        CLF_7_CHARACTER_SLOT = 0x1000,
        CLF_UNKNOWN_3 = 0x2000,
        CLF_NEW_MOVEMENT_SYSTEM = 0x4000,
        CLF_UNLOCK_FELUCCA_AREAS = 0x8000
    }
}