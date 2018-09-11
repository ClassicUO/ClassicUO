#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using System.Collections.Generic;

namespace ClassicUO.Game.Map
{
    public static class TileSorter
    {
        public static void Sort(List<GameObject> objects)
        {
            for (int i = 0; i < objects.Count - 1; i++)
            {
                int j = i + 1;
                while (j > 0)
                {
                    int result = Compare(objects[j - 1], objects[j]);
                    if (result > 0)
                    {
                        GameObject temp = objects[j - 1];
                        objects[j - 1] = objects[j];
                        objects[j] = temp;
                    }

                    j--;
                }
            }
        }

        private static int Compare(GameObject x, GameObject y)
        {
            (int xZ, int xType, int xThreshold, int xTierbreaker) = GetSortValues(x);
            (int yZ, int yType, int yThreshold, int yTierbreaker) = GetSortValues(y);

            xZ += xThreshold;
            yZ += yThreshold;

            int comparison = xZ - yZ;
            if (comparison == 0)
                comparison = xType - yType;

            if (comparison == 0)
                comparison = xThreshold - yThreshold;

            if (comparison == 0)
                comparison = xTierbreaker - yTierbreaker;

            return comparison;
        }

        private static (int, int, int, int) GetSortValues(GameObject e)
        {
            switch (e)
            {
                case GameEffect effect:
                    return (effect.Position.Z, 4, 2, 0);
                case DeferredEntity def:
                    return (def.Position.Z, 2, 1, 0);
                case Mobile mobile:
                    return (mobile.Position.Z, 3 /* is sitting */, 2, mobile == World.Player ? 0x40000000 : (int)mobile.Serial.Value);
                case Tile tile:
                    return (tile.View.SortZ, 0, 0, 0);
                case Static staticitem:
                    return (staticitem.Position.Z, 1, (staticitem.ItemData.Height > 0 ? 1 : 0) + (TileData.IsBackground((long)staticitem.ItemData.Flags) ? 0 : 1), staticitem.Index);
                case Item item:
                    return (item.Position.Z, item.IsCorpse ? 4 : 2, (item.ItemData.Height > 0 ? 1 : 0) + (TileData.IsBackground((long)item.ItemData.Flags) ? 0 : 1), (int)item.Serial.Value);
                default:
                    return (0, 0, 0, 0);
            }
        }
    }
}