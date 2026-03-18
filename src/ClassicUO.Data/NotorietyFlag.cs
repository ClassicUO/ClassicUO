// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    public enum NotorietyFlag : byte
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

    public static class Notoriety
    {
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
