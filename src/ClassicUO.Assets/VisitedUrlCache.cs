// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Assets
{
    // Bounded LRU of visited URLs. Replaces the unbounded
    // Dictionary<ushort, WebLink> that previously coupled compact-id encoding
    // to visited-state tracking. Intrusive doubly-linked list — the Node *is*
    // the entry, no LinkedListNode<T> wrapper.
    internal sealed class VisitedUrlCache
    {
        private sealed class Node
        {
            public string Url;
            public Node Prev;
            public Node Next;
        }

        private readonly int _capacity;
        private readonly Dictionary<string, Node> _map;
        private Node _head; // most recent
        private Node _tail; // least recent

        public VisitedUrlCache(int capacity)
        {
            _capacity = capacity;
            _map = new Dictionary<string, Node>(capacity);
        }

        // Exposed for tests and diagnostics.
        public int Count => _map.Count;

        public bool IsVisited(string url)
        {
            if (!_map.TryGetValue(url, out Node node))
            {
                return false;
            }

            MoveToFront(node);
            return true;
        }

        public void Mark(string url)
        {
            if (_map.TryGetValue(url, out Node node))
            {
                MoveToFront(node);
                return;
            }

            if (_map.Count >= _capacity)
            {
                Node evict = _tail;
                _tail = evict.Prev;

                if (_tail != null)
                {
                    _tail.Next = null;
                }
                else
                {
                    _head = null;
                }

                _map.Remove(evict.Url);
            }

            node = new Node { Url = url, Next = _head };

            if (_head != null)
            {
                _head.Prev = node;
            }

            _head = node;
            _tail ??= node;
            _map[url] = node;
        }

        private void MoveToFront(Node node)
        {
            if (node == _head)
            {
                return;
            }

            // Detach: node.Prev is non-null since node != _head.
            node.Prev.Next = node.Next;

            if (node.Next != null)
            {
                node.Next.Prev = node.Prev;
            }
            else
            {
                _tail = node.Prev;
            }

            // Insert at head.
            node.Prev = null;
            node.Next = _head;
            _head.Prev = node;
            _head = node;
        }
    }
}
