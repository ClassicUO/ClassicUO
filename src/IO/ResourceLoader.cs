#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO
{
    internal abstract class ResourceLoader : IDisposable
    {
        public UOFileIndex[] Entries;

        public bool IsDisposed { get; private set; }

        public abstract Task Load();

        public ref readonly UOFileIndex GetValidRefEntry(int index)
        {
            if (index < 0 || Entries == null || index >= Entries.Length)
                return ref UOFileIndex.Invalid;

            ref readonly UOFileIndex entry = ref Entries[index];

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

    internal abstract class ResourceLoader<T> : ResourceLoader where T : UOTexture
    {
        private readonly List<uint> _texturesToClear = new List<uint>();


        protected Dictionary<uint, T> ResourceDictionary { get; } = new Dictionary<uint, T>();
        public abstract T GetTexture(uint id);


        public virtual void CleaUnusedResources()
        {
            ClearUnusedResources(ResourceDictionary, Constants.MAX_GUMP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR);
        }

        public void ClearUnusedResources<T1>(Dictionary<uint, T1> dict, int maxCount) where T1 : UOTexture
        {
            long ticks = Engine.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            int count = 0;
            foreach (var p in dict)
            {
                if (p.Value.Ticks < ticks)
                {
                    if (count++ >= maxCount)
                        break;

                    _texturesToClear.Add(p.Key);
                }
            }

            foreach (uint key in _texturesToClear)
            {
                dict[key].Dispose();
                dict.Remove(key);
            }

            if (_texturesToClear.Count != 0)
                _texturesToClear.Clear();
        }

        public virtual bool TryGetEntryInfo(int entry, out long address, out long size, out long compressedsize)
        {
            address = size = compressedsize = 0;

            return false;
        }
    }
}