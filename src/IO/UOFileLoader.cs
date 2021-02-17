﻿#region license

// Copyright (c) 2021, andreakarasho
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
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO
{
    internal abstract class UOFileLoader : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            ClearResources();
        }

        public UOFileIndex[] Entries;

        public abstract Task Load();

        public virtual void ClearResources()
        {
        }

        public ref UOFileIndex GetValidRefEntry(int index)
        {
            if (index < 0 || Entries == null || index >= Entries.Length)
            {
                return ref UOFileIndex.Invalid;
            }

            ref UOFileIndex entry = ref Entries[index];

            if (entry.Offset < 0 || entry.Length <= 0 || entry.Offset == 0x0000_0000_FFFF_FFFF)
            {
                return ref UOFileIndex.Invalid;
            }

            return ref entry;
        }
    }

    internal abstract class UOFileLoader<T> : UOFileLoader where T : UOTexture
    {
        private readonly LinkedList<uint> _usedTextures = new LinkedList<uint>();

        protected UOFileLoader(int max)
        {
            Resources = new T[max];
        }

        protected readonly T[] Resources;

        public abstract T GetTexture(uint id);

        protected void SaveId(uint id)
        {
            _usedTextures.AddLast(id);
        }

        public virtual void CleaUnusedResources(int count)
        {
            ClearUnusedResources(Resources, count);
        }

        public override void ClearResources()
        {
            LinkedListNode<uint> first = _usedTextures.First;

            while (first != null)
            {
                LinkedListNode<uint> next = first.Next;
                uint idx = first.Value;

                if (idx < Resources.Length)
                {
                    ref T texture = ref Resources[idx];
                    texture?.Dispose();
                    texture = null;
                }

                _usedTextures.Remove(first);

                first = next;
            }
        }

        public void ClearUnusedResources<T1>(T1[] resourceCache, int maxCount) where T1 : UOTexture
        {
            if (Time.Ticks <= Constants.CLEAR_TEXTURES_DELAY)
            {
                return;
            }

            long ticks = Time.Ticks - Constants.CLEAR_TEXTURES_DELAY;
            int count = 0;

            LinkedListNode<uint> first = _usedTextures.First;

            while (first != null)
            {
                LinkedListNode<uint> next = first.Next;
                uint idx = first.Value;

                if (idx < resourceCache.Length && resourceCache[idx] != null)
                {
                    if (resourceCache[idx].Ticks < ticks)
                    {
                        if (count++ >= maxCount)
                        {
                            break;
                        }

                        resourceCache[idx].Dispose();

                        resourceCache[idx] = null;
                        _usedTextures.Remove(first);
                    }
                }

                first = next;
            }
        }


        public virtual bool TryGetEntryInfo(int entry, out long address, out long size, out long compressedSize)
        {
            address = size = compressedSize = 0;

            return false;
        }
    }
}