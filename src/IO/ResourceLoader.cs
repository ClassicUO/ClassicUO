using System;
using System.Collections.Generic;
using System.Linq;

using ClassicUO.Game;
using ClassicUO.Renderer;

namespace ClassicUO.IO
{
    internal abstract class ResourceLoader<T> where T : SpriteTexture, IDisposable
    {
        private readonly string[] _paths;

        protected ResourceLoader(string path) : this(new[] {path})
        {
        }

        protected ResourceLoader(string[] paths)
        {
            _paths = paths;
        }


        protected ResourceLoader()
        {
        }

        protected Dictionary<uint, T> ResourceDictionary { get; } = new Dictionary<uint, T>();

        public bool IsDisposed { get; private set; }


        public abstract void Load();

        public abstract T GetTexture(uint id);

        public abstract void CleanResources();

        public virtual void CleaUnusedResources()
        {
            long ticks = Engine.Ticks - Constants.CLEAR_TEXTURES_DELAY;

            ResourceDictionary
               .Where(s => s.Value.Ticks < ticks)
               .Take(Constants.MAX_GUMP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR)
               .ToList()
               .ForEach(s =>
                {
                    s.Value.Dispose();
                    ResourceDictionary.Remove(s.Key);
                });
        }

        public virtual bool TryGetEntryInfo(int entry, out long address, out long size, out long compressedsize)
        {
            address = size = compressedsize = 0;

            return false;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            CleanResources();
        }
    }

    internal abstract class ResourceLoader : IDisposable
    {
        private readonly string[] _paths;

        protected ResourceLoader(string path) : this(new[] {path})
        {
        }

        protected ResourceLoader(string[] paths)
        {
            _paths = paths;
        }


        protected ResourceLoader()
        {
        }

        public bool IsDisposed { get; private set; }


        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            CleanResources();
        }

        public abstract void Load();

        protected abstract void CleanResources();
    }
}