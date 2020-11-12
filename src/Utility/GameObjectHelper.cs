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

using System.Runtime.CompilerServices;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO.Resources;

namespace ClassicUO.Utility
{
    internal static class GameObjectHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoDrawable(ushort g)
        {
            switch (g)
            {
                case 0x0001:
                case 0x21BC:
                    //case 0x5690:
                    return true;

                case 0x9E4C:
                case 0x9E64:
                case 0x9E65:
                case 0x9E7D:
                    ref StaticTiles data = ref TileDataLoader.Instance.StaticData[g];

                    return data.IsBackground || data.IsSurface;
            }

            if (g != 0x63D3)
            {
                if (g >= 0x2198 && g <= 0x21A4)
                {
                    return true;
                }

                // Easel fix.
                // In older clients the tiledata flag for this 
                // item contains NoDiagonal for some reason.
                // So the next check will make the item invisible.
                if (g == 0x0F65 && Client.Version < ClientVersion.CV_60144)
                {
                    return false;
                }

                if (g < TileDataLoader.Instance.StaticData.Length)
                {
                    ref StaticTiles data = ref TileDataLoader.Instance.StaticData[g];

                    if (!data.IsNoDiagonal || data.IsAnimated && World.Player != null &&
                        World.Player.Race == RaceType.GARGOYLE)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}