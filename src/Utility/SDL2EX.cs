using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Utility.Platforms;

namespace ClassicUO.Utility
{
    unsafe static class SDL2EX
    {
        public delegate IntPtr OnSDLLoadObject(StringBuilder sb);
        public delegate IntPtr OnLoadFunction(IntPtr module, StringBuilder sb);

        public delegate void glClearColor(float r, float g, float b, float a);
        public delegate void glClear(uint bit);

        public delegate IntPtr OnGetProcAddr(byte* s);

        private static readonly OnGetProcAddr _getProcAddr;
     
        private static readonly OnSDLLoadObject _loadObject;
        private static readonly OnLoadFunction _loadFunction;
        private static readonly glClearColor _clearColor;
        private static readonly glClear _clear;

        static SDL2EX()
        {
            IntPtr sdl;

            if (Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                sdl = Native.LoadLibrary("libSDL2-2.0.0.dylib");
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                sdl = Native.LoadLibrary("libSDL2-2.0.so.0");
            }
            else
            {
                sdl = Native.LoadLibrary("SDL2.dll");
            }

            IntPtr loadLib = Native.GetProcessAddress(sdl, "SDL_LoadObject");
            _loadObject = Marshal.GetDelegateForFunctionPointer<OnSDLLoadObject>(loadLib);

            IntPtr loadFunc = Native.GetProcessAddress(sdl, "SDL_LoadFunction");
            _loadFunction = Marshal.GetDelegateForFunctionPointer<OnLoadFunction>(loadFunc);


            IntPtr func = Native.GetProcessAddress(sdl, "SDL_GL_GetProcAddress");
            _getProcAddr = Marshal.GetDelegateForFunctionPointer<OnGetProcAddr>(func);



            fixed (byte* ptr = System.Text.Encoding.UTF8.GetBytes("glClearColor" + '\0'))
                _clearColor = Marshal.GetDelegateForFunctionPointer<glClearColor>(_getProcAddr(ptr));

            fixed (byte* ptr = System.Text.Encoding.UTF8.GetBytes("glClear" + '\0'))
                _clear = Marshal.GetDelegateForFunctionPointer<glClear>(_getProcAddr(ptr));

        }

        public static IntPtr SDL_LoadObject(string name)
        {
            return _loadObject(new StringBuilder(name));
        }

        public static IntPtr SDL_LoadFunction(IntPtr module, string name)
        {
            return _loadFunction(module, new StringBuilder(name));
        }


        public static void ClearColor(float r, float g, float b, float a)
        {
            _clearColor(r, g, b, a);
        }

        public static void Clearr(uint bit)
        {
            _clear(bit);
        }
    }
}
