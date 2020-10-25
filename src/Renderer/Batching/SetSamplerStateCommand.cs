using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SetSamplerStateCommand
    {
        public int type;

        public IntPtr id;
        public int index;
    }
}