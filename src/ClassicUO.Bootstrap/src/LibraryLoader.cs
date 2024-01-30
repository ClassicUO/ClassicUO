using System;
using System.Runtime.InteropServices;

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
            private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
            [DllImport("kernel32.dll", SetLastError = true)]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            private static extern IntPtr LoadLibraryExW([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName, IntPtr hFile, uint dwFlags);

            [DllImport("kernel32", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
            private static extern IntPtr GetProcAddress_WIN(IntPtr module, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32", EntryPoint = "FreeLibrary")]
            private static extern int FreeLibrary_WIN(IntPtr module);


            public override IntPtr LoadLibrary(string name)
            {
                return LoadLibraryExW(name, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
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
            }

            public override IntPtr GetProcessAddress(IntPtr module, string name)
            {
                return m_useLibdl1 ? Libdl1.dlsym(module, name) : Libdl2.dlsym(module, name);
            }

            public override int FreeLibrary(IntPtr module)
            {
                return m_useLibdl1 ? Libdl1.dlerror() : Libdl2.dlerror();
            }
        }
    }
}
