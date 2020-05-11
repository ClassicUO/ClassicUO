﻿#region license
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


using System;

namespace ClassicUO.Game
{
    abstract class LinkedObject
    {
        public LinkedObject Previous, Next, Items;
        public bool IsEmpty => Items == null;

        ~LinkedObject()
        {
            Clear();

            var item = Next;

            while (item != null && item != this)
            {
                var next = item.Next;
                item.Next = null;
                item = next;
            }
        }

        public void PushToBack(LinkedObject item)
        {
            if (item == null)
                return;

            Remove(item);

            if (Items == null)
            {
                Items = item;
            }
            else
            {
                var current = Items;
                while (current.Next != null)
                {
                    current = current.Next;
                }

                current.Next = item;
                item.Previous = current;
            }
        }

        public void Remove(LinkedObject item)
        {
            if (item == null)
                return;

            Unlink(item);
            item.Next = null;
            item.Previous = null;
        }

        public void Unlink(LinkedObject item)
        {
            if (item == null)
                return;

            if (item == Items)
            {
                Items = Items.Next;
                if (Items != null)
                {
                    Items.Previous = null;
                }
            }
            else
            {
                if (item.Previous != null)
                {
                    item.Previous.Next = item.Next;
                }

                if (item.Next != null)
                {
                    item.Next.Previous = item.Previous;
                }
            }
        }

        public void Insert(LinkedObject first, LinkedObject item)
        {
            if (first == null)
            {
                item.Next = Items;
                item.Previous = null;

                if (Items != null)
                {
                    Items.Previous = item;
                }
                Items = item;
            }
            else
            {
                var next = first.Next;
                item.Next = next;
                item.Previous = first;
                first.Next = item;

                if (next != null)
                {
                    next.Previous = item;
                }
            }
        }

        public void MoveToFront(LinkedObject item)
        {
            if (item != null && item != Items)
            {
                Unlink(item);

                if (Items != null)
                {
                    Items.Previous = item;
                }

                item.Next = Items;
                item.Previous = null;
                Items = item;
            }
        }

        public void MoveToBack(LinkedObject item)
        {
            if (item != null)
            {
                Unlink(item);
                var last = GetLast();

                if (last == null)
                {
                    Items = item;
                }
                else
                {
                    last.Next = item;
                }

                item.Previous = last;
                item.Next = null;
            }
        }

        public LinkedObject GetLast()
        {
            var last = Items;

            while (last != null && last.Next != null)
            {
                last = last.Next;
            }

            return last;
        }

        public void Clear()
        {
            if (Items != null)
            {
                var item = Items;
                Items = null;

                while (item != null)
                {
                    var next = item.Next;
                    item.Next = null;
                    item = next;
                }
            }
        }

        /// <summary>
        /// Sort the contents of this LinkedObject using merge sort.
        /// Adapted from Simon Tatham's C implementation: https://www.chiark.greenend.org.uk/~sgtatham/algorithms/listsort.html
        /// </summary>
        /// <typeparam name="T">Type of the objects being compared.</typeparam>
        /// <param name="comparison">Comparison function to use when sorting.</param>
        public LinkedObject SortContents<T>(Comparison<T> comparison) where T : LinkedObject
        {
            if (Items == null)
                return null;

            int unitsize = 1; //size of the components we are merging; 1 for first iteration, multiplied by 2 after each iteration
            T p = null, q = null, e = null, head = (T) Items, tail = null;

            while (true)
            {
                p = head;
                int nmerges = 0; //number of merges done this pass
                int psize, qsize; //lengths of the components we are merging
                head = null;
                tail = null;

                while (p != null)
                {
                    nmerges++;
                    q = p;
                    psize = 0;
                    for (int i = 0; i < unitsize; i++)
                    {
                        psize++;
                        q = (T) q.Next;
                        if (q == null)
                            break;
                    }

                    qsize = unitsize;
                    while (psize > 0 || (qsize > 0 && q != null))
                    {
                        if (psize == 0)
                        {
                            e = q;
                            q = (T) q.Next;
                            qsize--;
                        }
                        else if (qsize == 0 || q == null)
                        {
                            e = p;
                            p = (T) p.Next;
                            psize--;
                        }
                        else if (comparison(p, q) <= 0)
                        {
                            e = p;
                            p = (T)p.Next;
                            psize--;
                        }
                        else
                        {
                            e = q;
                            q = (T) q.Next;
                            qsize--;
                        }

                        if (tail != null)
                        {
                            tail.Next = e;
                        }
                        else
                        {
                            head = e;
                        }
                        e.Previous = tail;
                        tail = e;
                    }
                    p = q;
                }
                tail.Next = null;
                if (nmerges <= 1)
                {
                    Items = head;
                    return head;
                }
                else
                    unitsize *= 2;
            }
        }

    }
}
