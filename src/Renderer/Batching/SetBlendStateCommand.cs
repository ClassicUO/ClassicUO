using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SetBlendStateCommand
    {
        public int type;

        public IntPtr id;
    }
}