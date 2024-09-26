#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Diagnostics;

namespace ClassicUO.Game
{
    public abstract class LinkedObject
    {
        public bool IsEmpty => Items == null;
        public LinkedObject Previous, Next, Items;

        //~LinkedObject()
        //{
        //    Clear();

        //    LinkedObject item = Next;

        //    while (item != null && item != this)
        //    {
        //        LinkedObject next = item.Next;
        //        item.Next = null;
        //        item = next;
        //    }
        //}

        public void PushToBack(LinkedObject item)
        {
            if (item == null)
            {
                return;
            }

            Remove(item);

            if (Items == null)
            {
                Items = item;
            }
            else
            {
                LinkedObject last = GetLast();
                last.Next = item;

                Debug.Assert(item.Next == null, "[Append to last-next] item must be unlinked before.");
                item.Next = null;
                item.Previous = last;
            }
        }

        public void Remove(LinkedObject item)
        {
            if (item == null)
            {
                return;
            }

            Unlink(item);
            item.Next = null;
            item.Previous = null;
        }

        public void Unlink(LinkedObject item)
        {
            if (item == null)
            {
                return;
            }

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
                LinkedObject next = first.Next;
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
                LinkedObject last = GetLast();

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
            LinkedObject last = Items;

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
                LinkedObject item = Items;
                Items = null;

                while (item != null)
                {
                    LinkedObject next = item.Next;
                    item.Next = null;
                    item = next;
                }
            }
        }

        /// <summary>
        ///     Sort the contents of this LinkedObject using merge sort.
        ///     Adapted from Simon Tatham's C implementation: https://www.chiark.greenend.org.uk/~sgtatham/algorithms/listsort.html
        /// </summary>
        /// <typeparam name="T">Type of the objects being compared.</typeparam>
        /// <param name="comparison">Comparison function to use when sorting.</param>
        public LinkedObject SortContents<T>(Comparison<T> comparison) where T : LinkedObject
        {
            if (Items == null)
            {
                return null;
            }

            int unitsize = 1; //size of the components we are merging; 1 for first iteration, multiplied by 2 after each iteration

            T p = null, q = null, e = null, head = (T) Items, tail = null;

            while (true)
            {
                p = head;
                int nmerges = 0;  //number of merges done this pass
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
                        {
                            break;
                        }
                    }

                    qsize = unitsize;

                    while (psize > 0 || qsize > 0 && q != null)
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
                            p = (T) p.Next;
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

                unitsize *= 2;
            }
        }
    }
}