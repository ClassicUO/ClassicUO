#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Configuration;

namespace ClassicUO.Game.Data
{
    enum NotorietyFlag : byte
    {
        Unknown = 0x00,
        Innocent = 0x01,
        Ally = 0x02,
        Gray = 0x03,
        Criminal = 0x04,
        Enemy = 0x05,
        Murderer = 0x06,
        Invulnerable = 0x07,
    }

    internal static class Notoriety
    {
        public static ushort GetHue(NotorietyFlag flag)
        {
            switch (flag)
            {
                case NotorietyFlag.Innocent:

                    return ProfileManager.Current.InnocentHue;

                case NotorietyFlag.Ally:

                    return ProfileManager.Current.FriendHue;

                case NotorietyFlag.Criminal:
                case NotorietyFlag.Gray:

                    return ProfileManager.Current.CriminalHue;

                case NotorietyFlag.Enemy:

                    return ProfileManager.Current.EnemyHue;

                case NotorietyFlag.Murderer:

                    return ProfileManager.Current.MurdererHue;

                case NotorietyFlag.Invulnerable:

                    return 0x0034;

                default:

                    return 0;
            }
        }

        public static string GetHTMLHue(NotorietyFlag flag)
        {
            switch (flag)
            {
                case NotorietyFlag.Innocent:

                    return "<basefont color=\"cyan\">";

                case NotorietyFlag.Ally:

                    return "<basefont color=\"lime\">";

                case NotorietyFlag.Criminal:
                case NotorietyFlag.Gray:

                    return "<basefont color=\"gray\">";

                case NotorietyFlag.Enemy:

                    return "<basefont color=\"orange\">";

                case NotorietyFlag.Murderer:

                    return "<basefont color=\"red\">";

                case NotorietyFlag.Invulnerable:

                    return "<basefont color=\"yellow\">";

                default:

                    return string.Empty;
            }
        }
    }
}