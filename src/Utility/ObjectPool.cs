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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace ClassicUO.Utility
{
    public enum ObjectPoolIsFullPolicy
    {
        ReturnNull,
        IncreaseSize,
        KillExisting
    }

    public class ObjectPool<T> : IEnumerable<T> where T : class, IPoolable
    {
        private readonly Deque<T> _freeItems; // circular buffer for O(1) operations
        private readonly Func<T> _instantiationFunction;
        private readonly ReturnToPoolDelegate _returnToPoolDelegate;
        private T _headNode; // linked list for iteration
        private T _tailNode;

        public ObjectPool(Func<T> instantiationFunc, int capacity = 16, ObjectPoolIsFullPolicy isFullPolicy = ObjectPoolIsFullPolicy.ReturnNull)
        {
            if (instantiationFunc == null)
                throw new ArgumentNullException(nameof(instantiationFunc));
            _returnToPoolDelegate = Return;
            _instantiationFunction = instantiationFunc;
            _freeItems = new Deque<T>(capacity);
            IsFullPolicy = isFullPolicy;

            while (_freeItems.Count < capacity)
            {
                Capacity++;
                _freeItems.AddToBack(CreateObject());
            }
        }

        public ObjectPoolIsFullPolicy IsFullPolicy { get; }

        public int Capacity { get; private set; }

        public int TotalCount { get; private set; }

        public int AvailableCount => _freeItems.Count;

        public int InUseCount => TotalCount - AvailableCount;

        public IEnumerator<T> GetEnumerator()
        {
            T node = _headNode;

            while (node != null)
            {
                yield return node;
                node = (T) node.NextNode;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event Action<T> ItemUsed;

        public event Action<T> ItemReturned;

        public T New()
        {
            if (!_freeItems.RemoveFromFront(out T poolable))
            {
                if (TotalCount <= Capacity)
                    poolable = CreateObject();
                else
                {
                    switch (IsFullPolicy)
                    {
                        case ObjectPoolIsFullPolicy.ReturnNull:

                            return null;
                        case ObjectPoolIsFullPolicy.IncreaseSize:
                            Capacity++;
                            poolable = CreateObject();

                            break;
                        case ObjectPoolIsFullPolicy.KillExisting:

                            if (_headNode == null)
                                return null;
                            T newHeadNode = (T) _headNode.NextNode;
                            _headNode.Return();
                            _freeItems.RemoveFromBack(out poolable);
                            _headNode = newHeadNode;

                            break;
                        default:

                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            Use(poolable);

            return poolable;
        }

        private T CreateObject()
        {
            TotalCount++;
            T item = _instantiationFunction();

            if (item == null)
                throw new NullReferenceException($"The created pooled object of type '{typeof(T).Name}' is null.");
            item.PreviousNode = _tailNode;
            item.NextNode = null;

            if (_headNode == null)
                _headNode = item;

            if (_tailNode != null)
                _tailNode.NextNode = item;
            _tailNode = item;

            return item;
        }

        private void Return(IPoolable item)
        {
            Debug.Assert(item != null);
            T poolable1 = (T) item;
            T previousNode = (T) item.PreviousNode;
            T nextNode = (T) item.NextNode;

            if (previousNode != null)
                previousNode.NextNode = nextNode;

            if (nextNode != null)
                nextNode.PreviousNode = previousNode;

            if (item == _headNode)
                _headNode = nextNode;

            if (item == _tailNode)
                _tailNode = previousNode;

            if (_tailNode != null)
                _tailNode.NextNode = null;
            _freeItems.AddToBack(poolable1);
            ItemReturned?.Invoke((T) item);
        }

        private void Use(T item)
        {
            item.Initialize(_returnToPoolDelegate);
            item.NextNode = null;

            if (item != _tailNode)
            {
                item.PreviousNode = _tailNode;
                _tailNode.NextNode = item;
                _tailNode = item;
            }

            ItemUsed?.Invoke(item);
        }
    }
}