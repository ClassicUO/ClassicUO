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

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Map
{
    internal static class TileSorter
    {
        // https://www.geeksforgeeks.org/merge-sort-for-doubly-linked-list/

        public static GameObject Sort(GameObject first)
        {
            return MergeSort(first);
        }

        private static GameObject Merge(GameObject first, GameObject second)
        {
            if (first == null)
                return second;

            if (second == null)
                return first;

            if (Compare(first, second) <= 0)
            {
                first.Right = Merge(first.Right, second);
                first.Right.Left = first;
                first.Left = null;

                return first;
            }

            second.Right = Merge(first, second.Right);
            second.Right.Left = second;
            second.Left = null;

            return second;
        }

        private static GameObject MergeSort(GameObject head)
        {
            if (head?.Right == null)
                return head;

            GameObject second = Split(head);

            head = MergeSort(head);
            second = MergeSort(second);

            return Merge(head, second);
        }

        private static GameObject Split(GameObject head)
        {
            GameObject fast = head;
            GameObject slow = head;

            while (fast.Right?.Right != null)
            {
                fast = fast.Right.Right;
                slow = slow.Right;
            }

            GameObject temp = slow.Right;
            slow.Right = null;

            return temp;
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

            if (comparison == 0)
                comparison = x.PriorityZ - y.PriorityZ;

            return comparison;
        }

        private static (int, int, int, int) GetSortValues(GameObject e)
        {
            switch (e)
            {
                case GameEffect effect:

                    return (effect.Z, effect.IsItemEffect ? 2 : 4, 2, 0);

                case Mobile mobile:

                    return (mobile.Z, 3 /* is sitting */, 2, mobile == World.Player ? 0x40000000 : (int) mobile.Serial.Value);

                case Land tile:

                    return (tile.AverageZ, 0, 0, 0);

                case Multi multi:

                    return (multi.Z, 1, (multi.ItemData.Height > 0 ? 1 : 0) + (multi.ItemData.IsBackground ? 0 : 1), 0);

                case Static staticitem:

                    return (staticitem.Z, 1, (staticitem.ItemData.Height > 0 ? 1 : 0) + (staticitem.ItemData.IsBackground ? 0 : 1), staticitem.Index);

                case Item item:

                    return (item.Z, item.IsCorpse ? 4 : 2, (item.ItemData.Height > 0 ? 1 : 0) + (item.ItemData.IsBackground ? 0 : 1), (int) item.Serial.Value);

                default:

                    return (0, 0, 0, 0);
            }
        }
    }
}