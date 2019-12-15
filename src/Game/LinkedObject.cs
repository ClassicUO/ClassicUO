using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game
{
    abstract class LinkedObject
    {
        public LinkedObject Left, Right, First;
        public bool IsEmpty => First == null;

        ~LinkedObject()
        {
            Clear();

            var item = Right;

            while (item != null && item != this)
            {
                var next = item.Right;
                item.Right = null;
                item = next;
            }
        }

        public void PushToBack(LinkedObject item)
        {
            if (item == null)
                return;

            if (First == null)
            {
                First = item;
            }
            else
            {
                var current = First;
                while (current.Right != null)
                {
                    current = current.Right;
                }

                current.Right = item;
                item.Left = current;
            }
        }

        public void Remove(LinkedObject item)
        {
            if (item == null)
                return;

            Unlink(item);
            item.Right = null;
            item.Left = null;
        }

        public void Unlink(LinkedObject item)
        {
            if (item == null)
                return;

            if (item == First)
            {
                First = First.Right;
                if (First != null)
                {
                    First.Left = null;
                }
            }
            else
            {
                item.Left.Right = item.Right;
                if (item.Right != null)
                {
                    item.Right.Left = item.Left;
                }
            }
        }

        public void Insert(LinkedObject first, LinkedObject item)
        {
            if (first == null)
            {
                item.Right = First;
                item.Left = null;

                if (First != null)
                {
                    first.Left = item;
                }
                First = item;
            }
            else
            {
                var next = first.Right;
                item.Right = next;
                item.Left = first;
                first.Right = item;

                if (next != null)
                {
                    next.Left = item;
                }
            }
        }

        public void MoveToFront(LinkedObject item)
        {
            if (item != null && item != First)
            {
                Unlink(item);

                if (First != null)
                {
                    First.Left = item;
                }

                item.Right = First;
                item.Left = null;
                First = item;
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
                    First = item;
                }
                else
                {
                    last.Right = item;
                }

                item.Left = last;
                item.Right = null;
            }
        }

        public LinkedObject GetLast()
        {
            var last = First;

            while (last != null && last.Right != null)
            {
                last = last.Right;
            }

            return last;
        }

        public void Clear()
        {
            if (First != null)
            {
                var item = First;
                First = null;

                while (item != null)
                {
                    var next = item.Right;
                    item.Right = null;
                    item = next;
                }
            }
        }

    }
}
