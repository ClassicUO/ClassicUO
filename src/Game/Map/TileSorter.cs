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
        public static void Sort(ref List<GameObject> objects)
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


        private static void Split(GameObject head, ref GameObject front, ref GameObject back)
        {
            if (head?.Right == null)
            {
                front = head;
                back = null;
            }
            else
            {
                GameObject slow = head;
                GameObject fast = head.Right;

                while (fast!= null)
                {
                    fast = fast.Right;

                    if (fast != null)
                    {
                        slow = slow.Right;
                        fast = fast.Right;
                    }
                }

                front = head;
                back = slow.Right;
                back.Left = null;
                slow.Right = null;
            }
        }

        private static void Merge(ref GameObject head, ref GameObject l1, ref GameObject l2)
        {
            GameObject newHead;

            if (l1 == null)
                newHead = l2;
            else if (l2 == null)
                newHead = l1;
            else
            {
                if (Compare(l2, l1) < 0)
                {
                    newHead = l2;
                    l2 = l2.Right;
                }
                else
                {
                    newHead = l1;
                    l1 = l1.Right;
                }

                newHead.Left = null;
                GameObject curr = newHead;

                while (l1 != null && l2 != null)
                {
                    if (Compare(l2, l1) < 0)
                    {
                        curr.Right = l2;
                        l2.Left = curr;
                        l2 = l2.Right;
                    }
                    else
                    {
                        curr.Right = l1;
                        l1.Left = curr;
                        l1 = l1.Right;
                    }

                    curr = curr.Right;
                }

                while (l1 != null)
                {
                    curr.Right = l1;
                    l1.Left = curr;
                    l1 = l1.Right;
                    curr = curr.Right;
                }

                while (l2 != null)
                {
                    curr.Right = l2;
                    l2.Left = curr;
                    l2 = l2.Right;
                    curr = curr.Right;
                }
            }

            head = newHead;
        }

        private static void MergeSort(ref GameObject first)
        {
            GameObject h1 = null;
            GameObject h2 = null;

            if (first?.Right != null)
            {
                Split(first, ref h1, ref h2);
                MergeSort(ref h1);
                MergeSort( ref h2);
                Merge(ref first, ref h1, ref h2);
            }

        }

        public static void Sort(GameObject first)
        {

            //bool swapped = false;
            //GameObject lptr = null;

            MergeSort(ref first);
          

            //do
            //{
            //    GameObject current = first;
            //    GameObject previous = null;
            //    GameObject next = first.Right;
            //    changed = false;

            //    while (next != null)
            //    {
            //        int result = Compare(current, next);

            //        if (result > 0)
            //        {
            //            changed = true;

            //            if (previous != null)
            //            {
            //                GameObject sig = next.Right;
            //                previous.Right = next;
            //                next.Right = current;
            //                current.Right = sig;
            //            }
            //            else
            //            {
            //                GameObject sig = next.Right;
            //                first = next;
            //                next.Right = current;
            //                current.Right = sig;
            //            }

            //            previous = next;
            //            next = current.Right;
            //        }
            //        else
            //        {
            //            previous = current;
            //            current = next;
            //            next = next.Right;
            //        }
            //    }


            //} while (changed);

            //do
            //{

            //    swapped = false;

            //    GameObject ptr1 = first;

            //    while (ptr1.Right != lptr)
            //    {
            //        int result = Compare(ptr1, ptr1.Right);

            //        if (result > 0)
            //        {
            //            GameObject temp = ptr1;
            //            ptr1 = ptr1.Right;
            //            ptr1.Right = temp;

            //            swapped = true;
            //        }
            //        else
            //            ptr1 = ptr1.Right;
            //    }

            //    lptr = ptr1;

            //} while (swapped);

            //if (first?.Right == null)
            //    return;

            ////GameObject found = null;
            //GameObject start = first;

            //while (first?.Right != null)
            //{
            //    GameObject prev = first;
            //    first = first.Right;

            //    while (prev != null)
            //    {
            //        int result = Compare(prev, first);

            //        if (result > 0)
            //        {
            //            GameObject left = prev.Left;
            //            GameObject right = first.Right;

            //            prev.Left = first;
            //            prev.Right = right;

            //            first.Left = left;
            //            first.Right = prev;

            //        }

            //        GameObject p = prev;
            //        prev = first?.Left;
            //        first = p.Left;
            //    }

            //    //first = first.Right;
            //    //if (first?.Right == null)
            //    //    break;
            //}

            //int count = 0;

            //GameObject right = first.Right;
            //while (right != null)
            //{
            //    right = right.Right;
            //    count++;
            //}

            //for (int i = 0; i < count; i++)
            //{
            //    right = first.Right;

            //}

            //GameObject next = first.Right;

            //if (next == null)
            //    return;



            //if (first == null)
            //    return;

            //GameObject second = first.Right;

            //if (second == null)
            //    return;

            //int result = Compare(first, second);

            //if (result > 0)
            //{
            //    GameObject left = first.Left;
            //    GameObject right = second.Right;

            //    first.Left = second;
            //    first.Right = right;

            //    second.Left = left;
            //    second.Right = first;
            //}

            //Sort(second.Right);


            //for (GameObject obj = first; obj?.Right != null; obj = obj.Right)
            //{
            //    GameObject j = obj.Right;

            //    while (j != null)
            //    {
            //        int result = Compare(obj, j);

            //        if (result > 0)
            //        {
            //            GameObject left = obj.Left;
            //            GameObject right = j.Right;

            //            obj.Left = j;
            //            obj.Right = right;

            //            j.Left = left;
            //            j.Right = obj;
            //        }

            //        j = j.Left;

            //    }
            //}

            //GameObject second = first;

            //while (second?.Right != null)
            //{
            //    second = second.Right;
            //    int result = Compare(first, second);

            //    if (result > 0)
            //    {
            //        GameObject left = first.Left; // estremo minore
            //        GameObject right = second.Right; // estremo maggiore


            //        first.Left = second;
            //        first.Right = right;

            //        second.Left = left;
            //        second.Right = first;


            //        //second = first;
            //        second = second.Left;
            //    }
            //    else
            //        first = second;
            //}
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

                    return (effect.Position.Z, effect.IsItemEffect ? 2 : 4, 2, 0);

                case Mobile mobile:

                    return (mobile.Position.Z, 3 /* is sitting */, 2, mobile == World.Player ? 0x40000000 : (int) mobile.Serial.Value);
                case Land tile:

                    return (tile.AverageZ, 0, 0, 0);
                case Static staticitem:

                    return (staticitem.Position.Z, 1, (staticitem.ItemData.Height > 0 ? 1 : 0) + (TileData.IsBackground(staticitem.ItemData.Flags) ? 0 : 1), staticitem.Index);
                case Item item:

                    return (item.Position.Z, item.IsCorpse ? 4 : 2, (item.ItemData.Height > 0 ? 1 : 0) + (TileData.IsBackground(item.ItemData.Flags) ? 0 : 1), (int) item.Serial.Value);
                default:

                    return (0, 0, 0, 0);
            }
        }
    }
}