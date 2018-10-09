using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Data
{
    public class Features
    {
        private FeatureFlags _flags;

        public void SetFlags(FeatureFlags flags) => _flags |= flags;

        public FeatureFlags Flags => _flags;

        public bool T2A => _flags.HasFlag(FeatureFlags.TheSecondAge);
        public bool UOR => _flags.HasFlag(FeatureFlags.Renaissance);
        public bool ThirdDawn => _flags.HasFlag(FeatureFlags.ThirdDawn);
        public bool LBR => _flags.HasFlag(FeatureFlags.LordBlackthornsRevenge);
        public bool AOS => _flags.HasFlag(FeatureFlags.AgeOfShadows);
        public bool CharSlots6 => _flags.HasFlag(FeatureFlags.CharacterSlot6);
        public bool SE => _flags.HasFlag(FeatureFlags.SameraiEmpire);
        public bool ML => _flags.HasFlag(FeatureFlags.MondainsLegacy);
        public bool Splash8th => _flags.HasFlag(FeatureFlags.Splash8);
        public bool Splash9th => _flags.HasFlag(FeatureFlags.Splash9);
        public bool TenthAge => _flags.HasFlag(FeatureFlags.TenthAge);
        public bool MoreStorage => _flags.HasFlag(FeatureFlags.MoreStorage);
        public bool CharSlots7 => _flags.HasFlag(FeatureFlags.TheSecondAge);
        public bool TenthAgeFaces => _flags.HasFlag(FeatureFlags.TenthAgeFaces);
        public bool TrialAccount => _flags.HasFlag(FeatureFlags.TrialAccount);
        public bool EleventhAge => _flags.HasFlag(FeatureFlags.EleventhAge);
        public bool SA => _flags.HasFlag(FeatureFlags.StygianAbyss);

        public bool TooltipsEnabled => AOS;
    }
}
