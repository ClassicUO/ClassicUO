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

namespace ClassicUO.Game.Data
{
    internal class LockedFeatures
    {
        public LockedFeatureFlags Flags { get; private set; }

        public bool T2A => Flags.HasFlag(LockedFeatureFlags.TheSecondAge);

        public bool UOR => Flags.HasFlag(LockedFeatureFlags.Renaissance);

        public bool ThirdDawn => Flags.HasFlag(LockedFeatureFlags.ThirdDawn);

        public bool LBR => Flags.HasFlag(LockedFeatureFlags.LordBlackthornsRevenge);

        public bool AOS => Flags.HasFlag(LockedFeatureFlags.AgeOfShadows);

        public bool CharSlots6 => Flags.HasFlag(LockedFeatureFlags.CharacterSlot6);

        public bool SE => Flags.HasFlag(LockedFeatureFlags.SameraiEmpire);

        public bool ML => Flags.HasFlag(LockedFeatureFlags.MondainsLegacy);

        public bool Splash8th => Flags.HasFlag(LockedFeatureFlags.Splash8);

        public bool Splash9th => Flags.HasFlag(LockedFeatureFlags.Splash9);

        public bool TenthAge => Flags.HasFlag(LockedFeatureFlags.TenthAge);

        public bool MoreStorage => Flags.HasFlag(LockedFeatureFlags.MoreStorage);

        public bool CharSlots7 => Flags.HasFlag(LockedFeatureFlags.TheSecondAge);

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