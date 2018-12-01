#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
    public class Features
    {
        public FeatureFlags Flags { get; private set; }

        public bool T2A => Flags.HasFlag(FeatureFlags.TheSecondAge);

        public bool UOR => Flags.HasFlag(FeatureFlags.Renaissance);

        public bool ThirdDawn => Flags.HasFlag(FeatureFlags.ThirdDawn);

        public bool LBR => Flags.HasFlag(FeatureFlags.LordBlackthornsRevenge);

        public bool AOS => Flags.HasFlag(FeatureFlags.AgeOfShadows);

        public bool CharSlots6 => Flags.HasFlag(FeatureFlags.CharacterSlot6);

        public bool SE => Flags.HasFlag(FeatureFlags.SameraiEmpire);

        public bool ML => Flags.HasFlag(FeatureFlags.MondainsLegacy);

        public bool Splash8th => Flags.HasFlag(FeatureFlags.Splash8);

        public bool Splash9th => Flags.HasFlag(FeatureFlags.Splash9);

        public bool TenthAge => Flags.HasFlag(FeatureFlags.TenthAge);

        public bool MoreStorage => Flags.HasFlag(FeatureFlags.MoreStorage);

        public bool CharSlots7 => Flags.HasFlag(FeatureFlags.TheSecondAge);

        public bool TenthAgeFaces => Flags.HasFlag(FeatureFlags.TenthAgeFaces);

        public bool TrialAccount => Flags.HasFlag(FeatureFlags.TrialAccount);

        public bool EleventhAge => Flags.HasFlag(FeatureFlags.EleventhAge);

        public bool SA => Flags.HasFlag(FeatureFlags.StygianAbyss);

        public bool TooltipsEnabled => AOS;

        public void SetFlags(FeatureFlags flags)
        {
            Flags |= flags;
        }
    }
}