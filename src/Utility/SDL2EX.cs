using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Utility.Platforms;

using SDL2;

namespace ClassicUO.Utility
{
    static class SDL2EX
    {
        public delegate IntPtr OnSDLLoadObject(StringBuilder sb);
        public delegate IntPtr OnLoadFunction(IntPtr module, StringBuilder sb);

        public delegate void OnGlColor4f(float r, float g, float b, float a);


        private static readonly OnSDLLoadObject _loadObject;
        private static readonly OnLoadFunction _loadFunction;

        private static readonly OnGlColor4f _glColor4F;

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

            _glColor4F = Marshal.GetDelegateForFunctionPointer<OnGlColor4f>(SDL.SDL_GL_GetProcAddress("glColor4f"));

        }

        public static void glColor4f(float r, float g, float b, float a)
            => _glColor4F(r, g, b, a);

        public static IntPtr SDL_LoadObject(string name)
        {
            return _loadObject(new StringBuilder(name));
        }

        public static IntPtr SDL_LoadFunction(IntPtr module, string name)
        {
            return _loadFunction(module, new StringBuilder(name));
        }
    }
}
