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