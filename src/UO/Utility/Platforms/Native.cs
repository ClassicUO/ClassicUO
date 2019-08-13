using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Environment = System.Environment;

namespace ClassicUO.Utility.Platforms
{
    static class Native
    {
        private static readonly NativeLoader _loader;

        static Native()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                _loader = new UnixNativeLoader();
            }
            else
            {
                _loader = new WinNativeLoader();
            }
        }

        public static IntPtr LoadLibrary(string name)
        {
            return _loader.LoadLibrary(name);
        }

        public static IntPtr GetProcessAddress(IntPtr module, string name)
        {
            return _loader.GetProcessAddress(module, name);
        }

        public static int FreeLibrary(IntPtr module)
        {
            return _loader.FreeLibrary(module);
        }


        abstract class NativeLoader
        {
            public abstract IntPtr LoadLibrary(string name);
            public abstract IntPtr GetProcessAddress(IntPtr module, string name);
            public abstract int FreeLibrary(IntPtr module);
        }

        class WinNativeLoader : NativeLoader
        {
            [DllImport("kernel32", EntryPoint = "LoadLibrary")]
            static extern IntPtr LoadLibrary_WIN(string fileName);

            [DllImport("kernel32", EntryPoint = "GetProcAddress")]
            static extern IntPtr GetProcAddress_WIN(IntPtr module, string procName);

            [DllImport("kernel32", EntryPoint = "FreeLibrary")]
            static extern int FreeLibrary_WIN(IntPtr module);


            public override IntPtr LoadLibrary(string name)
            {
                return LoadLibrary_WIN(name);
            }

            public override IntPtr GetProcessAddress(IntPtr module, string name)
            {
                return GetProcAddress_WIN(module, name);
            }

            public override int FreeLibrary(IntPtr module)
            {
                return FreeLibrary_WIN(module);
            }
        }

        class UnixNativeLoader : NativeLoader
        {
            private const string LibName = "libdl";

            public const int RTLD_NOW = 0x002;

            [DllImport(LibName)]
            static extern IntPtr dlopen(string fileName, int flags);

            [DllImport(LibName)]
            static extern IntPtr dlsym(IntPtr handle, string name);

            [DllImport(LibName)]
            static extern int dlclose(IntPtr handle);

            [DllImport(LibName)]
            static extern string dlerror();

            public override IntPtr LoadLibrary(string name)
            {
                return dlopen(name, RTLD_NOW);
            }

            public override IntPtr GetProcessAddress(IntPtr module, string name)
            {
                return dlsym(module, name);
            }

            public override int FreeLibrary(IntPtr module)
            {
                return dlclose(module);
            }
        }
    }
}
