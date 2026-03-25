// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Utility.Platforms
{
    public static class Native
    {
        private static readonly NativeLoader _loader;

        static Native()
        {
            if (PlatformHelper.IsWindows)
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
            private const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100;
            private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

            private readonly bool _supportsDefaultDllDirectories;

            [DllImport("kernel32.dll", SetLastError = true)]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            private static extern IntPtr LoadLibraryExW([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName, IntPtr hFile, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetDefaultDllDirectories(uint directoryFlags);

            [DllImport("kernel32", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
            private static extern IntPtr GetProcAddress_WIN(IntPtr module, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32", EntryPoint = "FreeLibrary")]
            private static extern int FreeLibrary_WIN(IntPtr module);

            public WinNativeLoader()
            {
                _supportsDefaultDllDirectories = SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
            }

            public override IntPtr LoadLibrary(string name)
            {
                uint flags = _supportsDefaultDllDirectories
                    ? LOAD_LIBRARY_SEARCH_DEFAULT_DIRS | LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR
                    : LOAD_WITH_ALTERED_SEARCH_PATH;

                IntPtr handle = LoadLibraryExW(name, IntPtr.Zero, flags);

                if (handle == IntPtr.Zero && _supportsDefaultDllDirectories)
                {
                    // Fallback for environments where restricted search flags are unavailable.
                    handle = LoadLibraryExW(name, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
                }

                return handle;
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
