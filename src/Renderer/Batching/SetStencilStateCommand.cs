using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SetStencilStateCommand
    {
        public int type;

        public IntPtr id;
    }
}