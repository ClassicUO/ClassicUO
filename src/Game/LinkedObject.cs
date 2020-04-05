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

    }
}
