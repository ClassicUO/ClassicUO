using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DestroyResourceCommand
    {
        public int type;

        public IntPtr id;
    }
}