// SPDX-License-Identifier: BSD-2-Clause

using System;

namespace ClassicUO.Sdk.Assets
{
    public abstract class UOFileLoader : IDisposable
    {
        protected UOFileLoader(UOFileManager fileManager)
        {
            FileManager = fileManager;
        }

        public UOFileManager FileManager { get; }

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


        public abstract void Load();

        public virtual void ClearResources()
        {
        }
    }
}