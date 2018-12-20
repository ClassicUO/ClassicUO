using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Utility;

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.IO
{
    abstract class ResourceLoader<T> where T : GraphicsResource, IDisposable
    {
        private readonly string[] _paths;

        protected ResourceLoader(string path) : this(new [] { path })
        {

        }

        protected ResourceLoader(string[] paths)
        {
            _paths = paths;
        }
        

        protected ResourceLoader() { }

        protected Dictionary<uint, T> ResourceDictionary { get; } = new Dictionary<uint, T>();

        public bool IsDisposed { get; private set; }


        public abstract void Load();

        public abstract T GetTexture(uint id);

        protected abstract void CleanResources();


        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            CleanResources();
        }
    }

    abstract class ResourceLoader :  IDisposable
    {
        private readonly string[] _paths;

        protected ResourceLoader(string path) : this(new[] { path })
        {

        }

        protected ResourceLoader(string[] paths)
        {
            _paths = paths;
        }


        protected ResourceLoader() { }

        public bool IsDisposed { get; private set; }

        public abstract void Load();

        protected abstract void CleanResources();


        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            CleanResources();
        }
    }
}
