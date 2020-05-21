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
using System.Collections.Generic;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO
{
    internal abstract class UOFileLoader : IDisposable
    {
        public UOFileIndex[] Entries;

        public bool IsDisposed { get; private set; }

        public abstract Task Load();

        public ref readonly UOFileIndex GetValidRefEntry(int index)
        {
            if (index < 0 || Entries == null || index >= Entries.Length)
                return ref UOFileIndex.Invalid;

            ref UOFileIndex entry = ref Entries[index];

            if (entry.Offset < 0 || entry.Length <= 0)
                return ref UOFileIndex.Invalid;

            return ref entry;
        }

        public abstract void CleanResources();

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            CleanResources();
        }
    }

    internal abstract class UOFileLoader<T> : UOFileLoader where T : UOTexture
    {
        protected readonly LinkedList<uint> _usedTextures = new LinkedList<uint>();

        protected UOFileLoader(int max)
        {
            Resources = new T[max];
        }

        protected readonly T[] Resources;
        public abstract T GetTexture(uint id);

        protected void SaveID(uint id)
            => _usedTextures.AddLast(id);

        public virtual void CleaUnusedResources(int count)
        {
            ClearUnusedResources(Resources, count);
        }

        public override void CleanResources()
        {
            var first = _usedTextures.First;

            while (first != null)
            {
                var next = first.Next;

                uint idx = first.Value;

                if (idx < Resources.Length)
                {
                    ref var texture = ref Resources[idx];
                    texture?.Dispose();
                    texture = null;
                }

                _usedTextures.Remove(first);

                first = next;
            }
        }

        public void ClearUnusedResources<T1>(T1[] resource_cache, int maxCount) where T1 : UOTexture
        {
            long ticks = Time.Ticks - Constants.CLEAR_TEXTURES_DELAY;
            int count = 0;

            var first = _usedTextures.First;

            while (first != null)
            {
                var next = first.Next;

                uint idx = first.Value;

                if (idx < resource_cache.Length && resource_cache[idx] != null)
                {
                    if (resource_cache[idx].Ticks < ticks)
                    {
                        if (count++ >= maxCount)
                            break;

                        resource_cache[idx].Dispose();
                        resource_cache[idx] = null;
                        _usedTextures.Remove(first);
                    }
                }

                first = next;
            }
        }


        public virtual bool TryGetEntryInfo(int entry, out long address, out long size, out long compressedsize)
        {
            address = size = compressedsize = 0;

            return false;
        }
    }
}