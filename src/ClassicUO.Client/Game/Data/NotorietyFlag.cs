// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;

namespace ClassicUO.Game.Data
{
    internal enum NotorietyFlag : byte
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

    internal static class Notoriety
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