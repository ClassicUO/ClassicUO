using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SetVertexBufferCommand
    {
        public int type;

        public IntPtr id;
    }
}