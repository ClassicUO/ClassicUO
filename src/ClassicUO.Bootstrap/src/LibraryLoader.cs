using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
<<<<<<< HEAD
using System.Runtime.InteropServices.ComTypes;
=======
>>>>>>> + classicuo.bootstrap app
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO
{
    public static class Native
    {
        private static readonly NativeLoader _loader;

        static Native()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                _loader = new WinNativeLoader();
            }
            else
            {
                _loader = new UnixNativeLoader();
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

        private class WinNativeLoader : NativeLoader
        {
<<<<<<< HEAD
            private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
            [DllImport("kernel32.dll", SetLastError = true)]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            private static extern IntPtr LoadLibraryExW([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName, IntPtr hFile, uint dwFlags);

            [DllImport("kernel32", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
            private static extern IntPtr GetProcAddress_WIN(IntPtr module, [MarshalAs(UnmanagedType.LPStr)] string procName);
=======
            [DllImport("kernel32", EntryPoint = "LoadLibrary")]
            private static extern IntPtr LoadLibrary_WIN(string fileName);

            [DllImport("kernel32", EntryPoint = "GetProcAddress")]
            private static extern IntPtr GetProcAddress_WIN(IntPtr module, string procName);
>>>>>>> + classicuo.bootstrap app

            [DllImport("kernel32", EntryPoint = "FreeLibrary")]
            private static extern int FreeLibrary_WIN(IntPtr module);


            public override IntPtr LoadLibrary(string name)
            {
<<<<<<< HEAD
                return LoadLibraryExW(name, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
=======
                return LoadLibrary_WIN(name);
>>>>>>> + classicuo.bootstrap app
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

        private class UnixNativeLoader : NativeLoader
        {
<<<<<<< HEAD

            public const int RTLD_NOW = 0x002;


            private static class Libdl1
            {
                private const string LibName = "libdl";

                [DllImport(LibName)]
                public static extern IntPtr dlopen(string fileName, int flags);

                [DllImport(LibName)]
                public static extern IntPtr dlsym(IntPtr handle, string name);

                [DllImport(LibName)]
                public static extern int dlclose(IntPtr handle);

                [DllImport(LibName)]
                public static extern int dlerror();
            }

            private static class Libdl2
            {
                private const string LibName = "libdl.so.2";

                [DllImport(LibName)]
                public static extern IntPtr dlopen(string fileName, int flags);

                [DllImport(LibName)]
                public static extern IntPtr dlsym(IntPtr handle, string name);

                [DllImport(LibName)]
                public static extern int dlclose(IntPtr handle);

                [DllImport(LibName)]
                public static extern int dlerror();
            }

            static UnixNativeLoader()
            {
                try
                {
                    Libdl1.dlerror();
                    m_useLibdl1 = true;
                }
                catch
                {
                }
            }

            private static bool m_useLibdl1;

            public override IntPtr LoadLibrary(string name)
            {
                return m_useLibdl1? Libdl1.dlopen(name, RTLD_NOW) : Libdl2.dlopen(name, RTLD_NOW);
=======
            private const string LibName = "libdl";

            public const int RTLD_NOW = 0x002;

            [DllImport(LibName)]
            private static extern IntPtr dlopen(string fileName, int flags);

            [DllImport(LibName)]
            private static extern IntPtr dlsym(IntPtr handle, string name);

            [DllImport(LibName)]
            private static extern int dlclose(IntPtr handle);

            [DllImport(LibName)]
            private static extern string dlerror();

            public override IntPtr LoadLibrary(string name)
            {
                return dlopen(name, RTLD_NOW);
>>>>>>> + classicuo.bootstrap app
            }

            public override IntPtr GetProcessAddress(IntPtr module, string name)
            {
<<<<<<< HEAD
                return m_useLibdl1 ? Libdl1.dlsym(module, name) : Libdl2.dlsym(module, name);
=======
                return dlsym(module, name);
>>>>>>> + classicuo.bootstrap app
            }

            public override int FreeLibrary(IntPtr module)
            {
<<<<<<< HEAD
                return m_useLibdl1 ? Libdl1.dlerror() : Libdl2.dlerror();
=======
                return dlclose(module);
>>>>>>> + classicuo.bootstrap app
            }
        }
    }
}
