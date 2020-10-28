using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SetRasterizerStateCommand
    {
        public int type;

        public IntPtr id;
    }
}