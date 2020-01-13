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

using System;
using System.Collections;
using System.Collections.Generic;

namespace ClassicUO.Utility.Collections
{
    internal static class CollectionHelper
    {
        public static IReadOnlyCollection<T> ReifyCollection<T>(IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var result = source as IReadOnlyCollection<T>;

            if (result != null)
                return result;

            var collection = source as ICollection<T>;

            if (collection != null)
                return new CollectionWrapper<T>(collection);

            var nongenericCollection = source as ICollection;

            if (nongenericCollection != null)
                return new NongenericCollectionWrapper<T>(nongenericCollection);

            return new List<T>(source);
        }

        private sealed class NongenericCollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly ICollection _collection;

            public NongenericCollectionWrapper(ICollection collection)
            {
                if (collection == null)
                    throw new ArgumentNullException(nameof(collection));

                _collection = collection;
            }

            public int Count => _collection.Count;

            public IEnumerator<T> GetEnumerator()
            {
                foreach (T item in _collection)
                    yield return item;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }

        private sealed class CollectionWrapper<T> : IReadOnlyCollection<T>
        {
            private readonly ICollection<T> _collection;

            public CollectionWrapper(ICollection<T> collection)
            {
                if (collection == null)
                    throw new ArgumentNullException(nameof(collection));

                _collection = collection;
            }

            public int Count => _collection.Count;

            public IEnumerator<T> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }
    }
}