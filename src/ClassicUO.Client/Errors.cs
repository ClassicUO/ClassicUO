using System;
using SDL2;

namespace ClassicUO;

internal static class Errors
{
    public static void ShowErrorMessage(string msg)
    {
        SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "ERROR", msg, IntPtr.Zero);
    }
}