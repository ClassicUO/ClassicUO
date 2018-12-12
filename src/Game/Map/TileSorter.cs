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
using System.Collections.Generic;

using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.Map
{
    public static class TileSorter
    {
        //public static void Sort(ref GameObject first)
        //{
        //    //MergeSort(ref first);

        //    first = MergeSort(first);
        //}

        // https://www.geeksforgeeks.org/merge-sort-for-doubly-linked-list/

        public static GameObject Sort(GameObject first)
            => MergeSort(first);

        private static GameObject Merge(GameObject first, GameObject second)
        {
            if (first == null)
                return second;

            if (second == null)
                return first;

            if (Compare(first, second) < 0)
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
            if (head == null || head.Right == null)
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

            while (fast.Right != null && fast.Right.Right != null)
            {
                fast = fast.Right.Right;
                slow = slow.Right;
            }

            GameObject temp = slow.Right;
            slow.Right = null;

            return temp;
        }

        //private static void Split(GameObject head, out GameObject front, out GameObject back)
        //{
        //    if (head?.Right == null)
        //    {
        //        front = head;
        //        back = null;
        //    }
        //    else
        //    {
        //        GameObject slow = head;
        //        GameObject fast = head.Right;

        //        while (fast != null)
        //        {
        //            fast = fast.Right;

        //            if (fast != null)
        //            {
        //                slow = slow.Right;
        //                fast = fast.Right;
        //            }
        //        }

        //        front = head;
        //        back = slow.Right;
        //        back.Left = null;
        //        slow.Right = null;
        //    }
        //}

        //private static void Merge(ref GameObject head, ref GameObject l1, ref GameObject l2)
        //{
        //    GameObject newHead;

        //    if (l1 == null)
        //        newHead = l2;
        //    else if (l2 == null)
        //        newHead = l1;
        //    else
        //    {
        //        if (Compare(l2, l1) < 0)
        //        {
        //            newHead = l2;
        //            l2 = l2.Right;
        //        }
        //        else
        //        {
        //            newHead = l1;
        //            l1 = l1.Right;
        //        }

        //        newHead.Left = null;
        //        GameObject curr = newHead;

        //        while (l1 != null && l2 != null)
        //        {
        //            if (Compare(l2, l1) < 0)
        //            {
        //                curr.Right = l2;
        //                l2.Left = curr;
        //                l2 = l2.Right;
        //            }
        //            else
        //            {
        //                curr.Right = l1;
        //                l1.Left = curr;
        //                l1 = l1.Right;
        //            }

        //            curr = curr.Right;
        //        }

        //        while (l1 != null)
        //        {
        //            curr.Right = l1;
        //            l1.Left = curr;
        //            l1 = l1.Right;
        //            curr = curr.Right;
        //        }

        //        while (l2 != null)
        //        {
        //            curr.Right = l2;
        //            l2.Left = curr;
        //            l2 = l2.Right;
        //            curr = curr.Right;
        //        }
        //    }

        //    head = newHead;
        //}

        //private static void MergeSort(ref GameObject first)
        //{
        //    if (first?.Right != null)
        //    {
        //        Split(first, out GameObject h1, out GameObject h2);
        //        MergeSort(ref h1);
        //        MergeSort(ref h2);
        //        Merge(ref first, ref h1, ref h2);
        //    }
        //}




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
                case Static staticitem:

                    return (staticitem.Z, 1, (staticitem.ItemData.Height > 0 ? 1 : 0) + (TileData.IsBackground(staticitem.ItemData.Flags) ? 0 : 1), staticitem.Index);
                case Item item:

                    return (item.Z, item.IsCorpse ? 4 : 2, (item.ItemData.Height > 0 ? 1 : 0) + (TileData.IsBackground(item.ItemData.Flags) ? 0 : 1), (int) item.Serial.Value);
                default:

                    return (0, 0, 0, 0);
            }
        }
    }
}