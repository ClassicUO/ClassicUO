using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CreateEffectCommand
    {
        public int type;

        public IntPtr id;
        public IntPtr code;
        public int Length;
    }
}