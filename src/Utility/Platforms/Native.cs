#region license

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
using System.Runtime.InteropServices;
using SDL2;

namespace ClassicUO.Utility.Platforms
{
    internal static class Native
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
            [DllImport("kernel32", EntryPoint = "LoadLibrary")]
            private static extern IntPtr LoadLibrary_WIN(string fileName);

            [DllImport("kernel32", EntryPoint = "GetProcAddress")]
            private static extern IntPtr GetProcAddress_WIN(IntPtr module, string procName);

            [DllImport("kernel32", EntryPoint = "FreeLibrary")]
            private static extern int FreeLibrary_WIN(IntPtr module);


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